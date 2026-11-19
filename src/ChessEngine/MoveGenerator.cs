namespace ChessEngine;

/// <summary>
/// Generates legal chess moves: pseudo-legal generation for every piece, then a
/// make/unmake legality filter (the king must not be left in check). Also answers
/// "is this square attacked by this colour", which castling and check detection need.
/// </summary>
public static class MoveGenerator
{
    // (file delta, rank delta)
    private static readonly (int, int)[] KnightJumps =
    {
        (1, 2), (2, 1), (2, -1), (1, -2), (-1, -2), (-2, -1), (-2, 1), (-1, 2),
    };

    private static readonly (int, int)[] KingSteps =
    {
        (1, 0), (1, 1), (0, 1), (-1, 1), (-1, 0), (-1, -1), (0, -1), (1, -1),
    };

    private static readonly (int, int)[] BishopDirections = { (1, 1), (1, -1), (-1, 1), (-1, -1) };
    private static readonly (int, int)[] RookDirections = { (1, 0), (-1, 0), (0, 1), (0, -1) };

    /// <summary>All legal moves for the side to move, in no particular order.</summary>
    public static List<Move> GenerateLegal(Board board)
    {
        var pseudo = new List<Move>(64);
        GeneratePseudoLegal(board, pseudo);

        var legal = new List<Move>(pseudo.Count);
        foreach (var move in pseudo)
        {
            board.Make(move, out var undo);
            // The move is legal only if our own king is safe afterwards. Board.Make
            // flips the side to move, so the king to test belongs to the mover.
            var mover = PieceUtil.Opponent(board.SideToMove);
            if (!board.IsInCheck(mover))
            {
                legal.Add(move);
            }

            board.Unmake(move, undo);
        }

        return legal;
    }

    public static void GeneratePseudoLegal(Board board, List<Move> moves)
    {
        var side = board.SideToMove;
        for (var sq = 0; sq < 64; sq++)
        {
            var piece = board.Squares[sq];
            if (piece == Piece.None || PieceUtil.ColorOf(piece) != side) continue;

            switch (PieceUtil.TypeOf(piece))
            {
                case 1: GeneratePawnMoves(board, sq, moves); break;
                case 2: GenerateJumpingMoves(board, sq, KnightJumps, moves); break;
                case 3: GenerateSlidingMoves(board, sq, BishopDirections, moves); break;
                case 4: GenerateSlidingMoves(board, sq, RookDirections, moves); break;
                case 5:
                    GenerateSlidingMoves(board, sq, BishopDirections, moves);
                    GenerateSlidingMoves(board, sq, RookDirections, moves);
                    break;
                case 6:
                    GenerateJumpingMoves(board, sq, KingSteps, moves);
                    GenerateCastlingMoves(board, sq, moves);
                    break;
            }
        }
    }

    private static void GeneratePawnMoves(Board board, int from, List<Move> moves)
    {
        var white = board.SideToMove == Color.White;
        var dir = white ? 8 : -8;
        var file = PieceUtil.FileOf(from);
        var rank = PieceUtil.RankOf(from);
        var startRank = white ? 1 : 6;
        var promotionRank = white ? 7 : 0;

        var one = from + dir;
        if (board.Squares[one] == Piece.None)
        {
            if (PieceUtil.RankOf(one) == promotionRank)
            {
                AddPromotions(board, moves, from, one, MoveFlags.None);
            }
            else
            {
                moves.Add(new Move(from, one));
                var two = from + 2 * dir;
                if (rank == startRank && board.Squares[two] == Piece.None)
                {
                    moves.Add(new Move(from, two, Piece.None, MoveFlags.DoublePush));
                }
            }
        }

        foreach (var df in new[] { -1, 1 })
        {
            var toFile = file + df;
            if (toFile is < 0 or > 7) continue;
            var to = PieceUtil.Square(toFile, rank + (white ? 1 : -1));
            var target = board.Squares[to];

            if (target != Piece.None && PieceUtil.ColorOf(target) != board.SideToMove)
            {
                if (PieceUtil.RankOf(to) == promotionRank)
                {
                    AddPromotions(board, moves, from, to, MoveFlags.Capture);
                }
                else
                {
                    moves.Add(new Move(from, to, Piece.None, MoveFlags.Capture));
                }
            }
            else if (to == board.EnPassantSquare)
            {
                moves.Add(new Move(from, to, Piece.None, MoveFlags.Capture | MoveFlags.EnPassant));
            }
        }
    }

    private static void AddPromotions(Board board, List<Move> moves, int from, int to, MoveFlags flags)
    {
        var color = board.SideToMove;
        foreach (var type in new[] { 5, 4, 3, 2 }) // queen, rook, bishop, knight
        {
            moves.Add(new Move(from, to, PieceUtil.For(color, type), flags | MoveFlags.Promotion));
        }
    }

    private static void GenerateJumpingMoves(Board board, int from, (int, int)[] steps, List<Move> moves)
    {
        var file = PieceUtil.FileOf(from);
        var rank = PieceUtil.RankOf(from);
        foreach (var (df, dr) in steps)
        {
            var toFile = file + df;
            var toRank = rank + dr;
            if (!PieceUtil.OnBoard(toFile, toRank)) continue;
            var to = PieceUtil.Square(toFile, toRank);
            AddQuietOrCapture(board, from, to, moves);
        }
    }

    private static void GenerateSlidingMoves(Board board, int from, (int, int)[] directions, List<Move> moves)
    {
        var file = PieceUtil.FileOf(from);
        var rank = PieceUtil.RankOf(from);
        foreach (var (df, dr) in directions)
        {
            var toFile = file + df;
            var toRank = rank + dr;
            while (PieceUtil.OnBoard(toFile, toRank))
            {
                var to = PieceUtil.Square(toFile, toRank);
                var target = board.Squares[to];
                if (target == Piece.None)
                {
                    moves.Add(new Move(from, to));
                }
                else
                {
                    if (PieceUtil.ColorOf(target) != board.SideToMove)
                    {
                        moves.Add(new Move(from, to, Piece.None, MoveFlags.Capture));
                    }

                    break;
                }

                toFile += df;
                toRank += dr;
            }
        }
    }

    private static void GenerateCastlingMoves(Board board, int kingSquare, List<Move> moves)
    {
        var white = board.SideToMove == Color.White;
        var rank = white ? 0 : 7;
        var kingHome = PieceUtil.Square(4, rank);
        if (kingSquare != kingHome) return;

        var rights = board.CastlingRights;
        var kingSide = white ? Board.WhiteKingside : Board.BlackKingside;
        var queenSide = white ? Board.WhiteQueenside : Board.BlackQueenside;
        var opponent = PieceUtil.Opponent(board.SideToMove);

        if ((rights & kingSide) != 0
            && board.Squares[PieceUtil.Square(5, rank)] == Piece.None
            && board.Squares[PieceUtil.Square(6, rank)] == Piece.None
            && !IsSquareAttacked(board, kingHome, opponent)
            && !IsSquareAttacked(board, PieceUtil.Square(5, rank), opponent)
            && !IsSquareAttacked(board, PieceUtil.Square(6, rank), opponent))
        {
            moves.Add(new Move(kingHome, PieceUtil.Square(6, rank), Piece.None, MoveFlags.CastleKingside));
        }

        if ((rights & queenSide) != 0
            && board.Squares[PieceUtil.Square(3, rank)] == Piece.None
            && board.Squares[PieceUtil.Square(2, rank)] == Piece.None
            && board.Squares[PieceUtil.Square(1, rank)] == Piece.None
            && !IsSquareAttacked(board, kingHome, opponent)
            && !IsSquareAttacked(board, PieceUtil.Square(3, rank), opponent)
            && !IsSquareAttacked(board, PieceUtil.Square(2, rank), opponent))
        {
            moves.Add(new Move(kingHome, PieceUtil.Square(2, rank), Piece.None, MoveFlags.CastleQueenside));
        }
    }

    private static void AddQuietOrCapture(Board board, int from, int to, List<Move> moves)
    {
        var target = board.Squares[to];
        if (target == Piece.None)
        {
            moves.Add(new Move(from, to));
        }
        else if (PieceUtil.ColorOf(target) != board.SideToMove)
        {
            moves.Add(new Move(from, to, Piece.None, MoveFlags.Capture));
        }
    }

    /// <summary>True if any piece of <paramref name="by"/> attacks <paramref name="square"/>.</summary>
    public static bool IsSquareAttacked(Board board, int square, Color by)
    {
        var file = PieceUtil.FileOf(square);
        var rank = PieceUtil.RankOf(square);

        // Pawns: a white pawn attacks from one rank below, a black pawn from one above.
        if (by == Color.White && rank > 0)
        {
            if (file > 0 && board.Squares[PieceUtil.Square(file - 1, rank - 1)] == Piece.WhitePawn) return true;
            if (file < 7 && board.Squares[PieceUtil.Square(file + 1, rank - 1)] == Piece.WhitePawn) return true;
        }

        if (by == Color.Black && rank < 7)
        {
            if (file > 0 && board.Squares[PieceUtil.Square(file - 1, rank + 1)] == Piece.BlackPawn) return true;
            if (file < 7 && board.Squares[PieceUtil.Square(file + 1, rank + 1)] == Piece.BlackPawn) return true;
        }

        var knight = by == Color.White ? Piece.WhiteKnight : Piece.BlackKnight;
        var king = by == Color.White ? Piece.WhiteKing : Piece.BlackKing;
        var bishop = by == Color.White ? Piece.WhiteBishop : Piece.BlackBishop;
        var rook = by == Color.White ? Piece.WhiteRook : Piece.BlackRook;
        var queen = by == Color.White ? Piece.WhiteQueen : Piece.BlackQueen;

        foreach (var (df, dr) in KnightJumps)
        {
            var f = file + df;
            var r = rank + dr;
            if (PieceUtil.OnBoard(f, r) && board.Squares[PieceUtil.Square(f, r)] == knight) return true;
        }

        foreach (var (df, dr) in KingSteps)
        {
            var f = file + df;
            var r = rank + dr;
            if (PieceUtil.OnBoard(f, r) && board.Squares[PieceUtil.Square(f, r)] == king) return true;
        }

        foreach (var (df, dr) in BishopDirections)
        {
            if (RayHits(board, file, rank, df, dr, bishop, queen)) return true;
        }

        foreach (var (df, dr) in RookDirections)
        {
            if (RayHits(board, file, rank, df, dr, rook, queen)) return true;
        }

        return false;
    }

    private static bool RayHits(Board board, int file, int rank, int df, int dr, Piece slider, Piece queen)
    {
        var f = file + df;
        var r = rank + dr;
        while (PieceUtil.OnBoard(f, r))
        {
            var piece = board.Squares[PieceUtil.Square(f, r)];
            if (piece != Piece.None)
            {
                return piece == slider || piece == queen;
            }

            f += df;
            r += dr;
        }

        return false;
    }
}
