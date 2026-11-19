namespace ChessEngine.Tests;

public sealed class BoardTests
{
    [Fact]
    public void FenRoundTripStartPosition()
    {
        var board = Board.StartingPosition();
        Assert.Equal(Board.StartFen, board.ToFen());
    }

    [Fact]
    public void FenRoundTripKiwipete()
    {
        const string fen = "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1";
        Assert.Equal(fen, Board.FromFen(fen).ToFen());
    }

    [Fact]
    public void FenRoundTripWithEnPassant()
    {
        const string fen = "rnbqkbnr/pppp1ppp/8/4p3/4P3/8/PPPP1PPP/RNBQKBNR w KQkq e6 0 2";
        Assert.Equal(fen, Board.FromFen(fen).ToFen());
    }

    [Fact]
    public void MakeAndUnmakeRestoresPositionExactly()
    {
        var board = Board.FromFen("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1");
        var before = board.ToFen();

        foreach (var move in MoveGenerator.GenerateLegal(board))
        {
            board.Make(move, out var undo);
            board.Unmake(move, undo);
            Assert.Equal(before, board.ToFen());
        }
    }

    [Fact]
    public void DoublePushSetsEnPassantSquare()
    {
        var board = Board.StartingPosition();
        var e4 = MoveGenerator.GenerateLegal(board).Single(m => m.ToString() == "e2e4");
        board.Make(e4, out _);
        Assert.Equal(PieceUtil.ParseSquare("e3"), board.EnPassantSquare);
        Assert.Equal("rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1", board.ToFen());
    }

    [Fact]
    public void RookMoveForfeitsCastlingRight()
    {
        // 1. a4 a5 2. Ra3 — the rook has left a1, so White loses queenside castling.
        var board = Board.StartingPosition();
        Play(board, "a2a4", "a7a5", "a1a3");
        Assert.Equal(0, board.CastlingRights & Board.WhiteQueenside);
        Assert.NotEqual(0, board.CastlingRights & Board.WhiteKingside);
    }

    [Fact]
    public void KingMoveForfeitsBothCastlingRights()
    {
        // 1. e4 e5 2. Ke2 — the king has moved, both White rights are gone.
        var board = Board.StartingPosition();
        Play(board, "e2e4", "e7e5", "e1e2");
        Assert.Equal(0, board.CastlingRights & (Board.WhiteKingside | Board.WhiteQueenside));
        Assert.NotEqual(0, board.CastlingRights & Board.BlackKingside);
    }

    [Fact]
    public void CastlingMovesTheRookToo()
    {
        // 1. e4 e5 2. Nf3 Nc6 3. Bc4 Bc5 — then O-O.
        var board = Board.StartingPosition();
        Play(board, "e2e4", "e7e5", "g1f3", "b8c6", "f1c4", "f8c5");
        var castle = MoveGenerator.GenerateLegal(board).Single(m => m.ToString() == "e1g1");
        board.Make(castle, out _);
        Assert.Equal(Piece.WhiteKing, board.Squares[PieceUtil.ParseSquare("g1")]);
        Assert.Equal(Piece.WhiteRook, board.Squares[PieceUtil.ParseSquare("f1")]);
        Assert.Equal(Piece.None, board.Squares[PieceUtil.ParseSquare("e1")]);
        Assert.Equal(Piece.None, board.Squares[PieceUtil.ParseSquare("h1")]);
    }

    [Fact]
    public void EnPassantCaptureRemovesThePawn()
    {
        // 1. e4 a6 2. e5 d5 3. exd6 e.p.
        var board = Board.StartingPosition();
        Play(board, "e2e4", "a7a6", "e4e5", "d7d5");
        var ep = MoveGenerator.GenerateLegal(board).Single(m => m.ToString() == "e5d6");
        Assert.True(ep.IsEnPassant);
        board.Make(ep, out _);
        Assert.Equal(Piece.None, board.Squares[PieceUtil.ParseSquare("d5")]);
        Assert.Equal(Piece.WhitePawn, board.Squares[PieceUtil.ParseSquare("d6")]);
    }

    [Fact]
    public void CheckDetectionFoolsMate()
    {
        // 1. f3 e5 2. g4 Qh4# — Black is not in check, White is checkmated.
        var board = Board.StartingPosition();
        Play(board, "f2f3", "e7e5", "g2g4", "d8h4");
        Assert.True(board.IsInCheck(Color.White));
        Assert.False(board.IsInCheck(Color.Black));
        Assert.Empty(MoveGenerator.GenerateLegal(board));
    }

    [Fact]
    public void PinnedPieceCannotMove()
    {
        // White rook on e2 is pinned to the king by a black rook on e8.
        var board = Board.FromFen("4r3/8/8/8/8/8/4R3/4K3 w - - 0 1");
        var rookMoves = MoveGenerator.GenerateLegal(board)
            .Where(m => m.From == PieceUtil.ParseSquare("e2"));
        // The rook may slide along the e-file (still blocking) but never leave it.
        Assert.All(rookMoves, m => Assert.Equal(4, PieceUtil.FileOf(m.To)));
        Assert.DoesNotContain(rookMoves, m => m.ToString() == "e2a2");
    }

    [Fact]
    public void MoveParsingRoundTrips()
    {
        Assert.Equal("e2e4", Move.Parse("e2e4").ToString());
        Assert.Equal("e7e8q", Move.Parse("e7e8q").ToString());
        Assert.Equal(Piece.WhiteQueen, Move.Parse("e7e8q").Promotion);
    }

    private static void Play(Board board, params string[] uciMoves)
    {
        foreach (var text in uciMoves)
        {
            var parsed = Move.Parse(text);
            var legal = MoveGenerator.GenerateLegal(board);
            var match = legal.Single(m => m.From == parsed.From && m.To == parsed.To
                && m.Promotion == parsed.Promotion);
            board.Make(match, out _);
        }
    }
}
