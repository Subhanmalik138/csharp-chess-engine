namespace ChessEngine;

/// <summary>Everything needed to undo one <see cref="Board.Make"/> call.</summary>
public struct Undo
{
    public Piece Moved;
    public Piece Captured;
    public int CapturedSquare;
    public int PreviousCastlingRights;
    public int PreviousEnPassantSquare;
    public int PreviousHalfmoveClock;
}

/// <summary>
/// A chess position: a 64-square mailbox board plus side to move, castling rights,
/// en-passant square and the move clocks. Squares are indexed 0 (a1) to 63 (h8).
/// </summary>
public sealed class Board
{
    public const int WhiteKingside = 1;
    public const int WhiteQueenside = 2;
    public const int BlackKingside = 4;
    public const int BlackQueenside = 8;

    public const string StartFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    public Piece[] Squares { get; } = new Piece[64];
    public Color SideToMove { get; set; } = Color.White;
    public int CastlingRights { get; set; }
    public int EnPassantSquare { get; set; } = -1;
    public int HalfmoveClock { get; set; }
    public int FullmoveNumber { get; set; } = 1;

    public static Board FromFen(string fen)
    {
        var board = new Board();
        var fields = fen.Trim().Split(' ');
        if (fields.Length < 4)
        {
            throw new FormatException($"FEN needs at least 4 fields: '{fen}'.");
        }

        var rows = fields[0].Split('/');
        if (rows.Length != 8)
        {
            throw new FormatException($"FEN placement needs 8 ranks: '{fen}'.");
        }

        for (var r = 0; r < 8; r++)
        {
            var rank = 7 - r; // FEN lists rank 8 first.
            var file = 0;
            foreach (var c in rows[r])
            {
                if (char.IsDigit(c))
                {
                    file += c - '0';
                }
                else
                {
                    if (file >= 8) throw new FormatException($"Too many pieces on rank {rank + 1}.");
                    board.Squares[PieceUtil.Square(file, rank)] = PieceUtil.FromFenChar(c);
                    file++;
                }
            }

            if (file != 8) throw new FormatException($"Rank {rank + 1} does not add up to 8 squares.");
        }

        board.SideToMove = fields[1] switch
        {
            "w" => Color.White,
            "b" => Color.Black,
            _ => throw new FormatException($"Bad side to move '{fields[1]}'."),
        };

        board.CastlingRights = 0;
        if (fields[2] != "-")
        {
            foreach (var c in fields[2])
            {
                board.CastlingRights |= c switch
                {
                    'K' => WhiteKingside,
                    'Q' => WhiteQueenside,
                    'k' => BlackKingside,
                    'q' => BlackQueenside,
                    _ => throw new FormatException($"Bad castling flag '{c}'."),
                };
            }
        }

        board.EnPassantSquare = PieceUtil.ParseSquare(fields[3]);
        if (fields.Length > 4) board.HalfmoveClock = int.Parse(fields[4]);
        if (fields.Length > 5) board.FullmoveNumber = int.Parse(fields[5]);
        return board;
    }

    public static Board StartingPosition() => FromFen(StartFen);

    public string ToFen()
    {
        var placement = new System.Text.StringBuilder();
        for (var rank = 7; rank >= 0; rank--)
        {
            var empty = 0;
            for (var file = 0; file < 8; file++)
            {
                var piece = Squares[PieceUtil.Square(file, rank)];
                if (piece == Piece.None)
                {
                    empty++;
                    continue;
                }

                if (empty > 0) placement.Append(empty);
                empty = 0;
                placement.Append(PieceUtil.ToFenChar(piece));
            }

            if (empty > 0) placement.Append(empty);
            if (rank > 0) placement.Append('/');
        }

        var castling = "";
        if ((CastlingRights & WhiteKingside) != 0) castling += "K";
        if ((CastlingRights & WhiteQueenside) != 0) castling += "Q";
        if ((CastlingRights & BlackKingside) != 0) castling += "k";
        if ((CastlingRights & BlackQueenside) != 0) castling += "q";
        if (castling.Length == 0) castling = "-";

        var ep = EnPassantSquare < 0 ? "-" : PieceUtil.SquareName(EnPassantSquare);
        return $"{placement} {(SideToMove == Color.White ? "w" : "b")} {castling} {ep} {HalfmoveClock} {FullmoveNumber}";
    }

    public Board Clone()
    {
        var copy = new Board
        {
            SideToMove = SideToMove,
            CastlingRights = CastlingRights,
            EnPassantSquare = EnPassantSquare,
            HalfmoveClock = HalfmoveClock,
            FullmoveNumber = FullmoveNumber,
        };
        Array.Copy(Squares, copy.Squares, 64);
        return copy;
    }

    public int KingSquare(Color color)
    {
        var king = color == Color.White ? Piece.WhiteKing : Piece.BlackKing;
        for (var i = 0; i < 64; i++)
        {
            if (Squares[i] == king) return i;
        }

        throw new InvalidOperationException($"No {color} king on the board.");
    }

    public bool IsInCheck(Color color) => MoveGenerator.IsSquareAttacked(this, KingSquare(color), PieceUtil.Opponent(color));

    /// <summary>
    /// Plays a (legal) move in place. Call <see cref="Unmake"/> with the returned
    /// <see cref="Undo"/> to restore the position exactly.
    /// </summary>
    public void Make(Move move, out Undo undo)
    {
        undo = new Undo
        {
            Moved = Squares[move.From],
            PreviousCastlingRights = CastlingRights,
            PreviousEnPassantSquare = EnPassantSquare,
            PreviousHalfmoveClock = HalfmoveClock,
        };

        var mover = SideToMove;
        var moved = Squares[move.From];
        var capturedSquare = move.To;
        Piece captured;

        if (move.IsEnPassant)
        {
            // The captured pawn sits one rank behind the landing square.
            capturedSquare = mover == Color.White ? move.To - 8 : move.To + 8;
            captured = Squares[capturedSquare];
            Squares[capturedSquare] = Piece.None;
        }
        else
        {
            captured = Squares[move.To];
        }

        undo.Captured = captured;
        undo.CapturedSquare = capturedSquare;

        Squares[move.From] = Piece.None;
        Squares[move.To] = move.IsPromotion ? move.Promotion : moved;

        if (move.IsCastle)
        {
            // Move the rook as well: K-side h-file -> f-file, Q-side a-file -> d-file.
            var rank = mover == Color.White ? 0 : 7;
            if ((move.Flags & MoveFlags.CastleKingside) != 0)
            {
                Squares[PieceUtil.Square(5, rank)] = Squares[PieceUtil.Square(7, rank)];
                Squares[PieceUtil.Square(7, rank)] = Piece.None;
            }
            else
            {
                Squares[PieceUtil.Square(3, rank)] = Squares[PieceUtil.Square(0, rank)];
                Squares[PieceUtil.Square(0, rank)] = Piece.None;
            }
        }

        // Castling rights: a king move forfeits both rights for that colour, a rook
        // leaving (or being captured on) its home square forfeits the matching one.
        if (PieceUtil.TypeOf(moved) == 6)
        {
            CastlingRights &= mover == Color.White
                ? ~(WhiteKingside | WhiteQueenside)
                : ~(BlackKingside | BlackQueenside);
        }

        CastlingRights &= move.From switch
        {
            0 => ~WhiteQueenside, // a1
            7 => ~WhiteKingside, // h1
            56 => ~BlackQueenside, // a8
            63 => ~BlackKingside, // h8
            _ => ~0,
        };
        CastlingRights &= capturedSquare switch
        {
            0 => ~WhiteQueenside,
            7 => ~WhiteKingside,
            56 => ~BlackQueenside,
            63 => ~BlackKingside,
            _ => ~0,
        };

        EnPassantSquare = (move.Flags & MoveFlags.DoublePush) != 0 ? (move.From + move.To) / 2 : -1;
        HalfmoveClock = captured != Piece.None || PieceUtil.TypeOf(moved) == 1 ? 0 : HalfmoveClock + 1;

        if (mover == Color.Black) FullmoveNumber++;
        SideToMove = PieceUtil.Opponent(mover);
    }

    public void Unmake(Move move, Undo undo)
    {
        SideToMove = PieceUtil.Opponent(SideToMove);
        if (SideToMove == Color.Black) FullmoveNumber--;

        CastlingRights = undo.PreviousCastlingRights;
        EnPassantSquare = undo.PreviousEnPassantSquare;
        HalfmoveClock = undo.PreviousHalfmoveClock;

        Squares[move.From] = undo.Moved;
        Squares[move.To] = Piece.None;
        if (undo.Captured != Piece.None)
        {
            Squares[undo.CapturedSquare] = undo.Captured;
        }

        if (move.IsCastle)
        {
            var rank = SideToMove == Color.White ? 0 : 7;
            if ((move.Flags & MoveFlags.CastleKingside) != 0)
            {
                Squares[PieceUtil.Square(7, rank)] = Squares[PieceUtil.Square(5, rank)];
                Squares[PieceUtil.Square(5, rank)] = Piece.None;
            }
            else
            {
                Squares[PieceUtil.Square(0, rank)] = Squares[PieceUtil.Square(3, rank)];
                Squares[PieceUtil.Square(3, rank)] = Piece.None;
            }
        }
    }

    /// <summary>Human-readable board, White at the bottom.</summary>
    public string PrettyPrint()
    {
        var sb = new System.Text.StringBuilder();
        for (var rank = 7; rank >= 0; rank--)
        {
            sb.Append($"{rank + 1} ");
            for (var file = 0; file < 8; file++)
            {
                var piece = Squares[PieceUtil.Square(file, rank)];
                sb.Append(piece == Piece.None ? ". " : $"{PieceUtil.ToFenChar(piece)} ");
            }

            sb.AppendLine();
        }

        sb.AppendLine("  a b c d e f g h");
        sb.AppendLine($"{(SideToMove == Color.White ? "White" : "Black")} to move");
        return sb.ToString();
    }
}
