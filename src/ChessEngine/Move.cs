namespace ChessEngine;

[Flags]
public enum MoveFlags
{
    None = 0,
    Capture = 1,
    DoublePush = 2,
    EnPassant = 4,
    CastleKingside = 8,
    CastleQueenside = 16,
    Promotion = 32,
}

/// <summary>A chess move: from/to squares, optional promotion piece, and flags describing its kind.</summary>
public readonly struct Move : IEquatable<Move>
{
    public int From { get; }
    public int To { get; }
    public Piece Promotion { get; }
    public MoveFlags Flags { get; }

    public Move(int from, int to, Piece promotion = Piece.None, MoveFlags flags = MoveFlags.None)
    {
        From = from;
        To = to;
        Promotion = promotion;
        Flags = flags;
    }

    public bool IsCapture => (Flags & MoveFlags.Capture) != 0;
    public bool IsPromotion => (Flags & MoveFlags.Promotion) != 0;
    public bool IsEnPassant => (Flags & MoveFlags.EnPassant) != 0;
    public bool IsCastle => (Flags & (MoveFlags.CastleKingside | MoveFlags.CastleQueenside)) != 0;

    /// <summary>UCI notation, e.g. "e2e4" or "e7e8q".</summary>
    public override string ToString()
    {
        var text = PieceUtil.SquareName(From) + PieceUtil.SquareName(To);
        if (IsPromotion)
        {
            text += char.ToLowerInvariant(PieceUtil.ToFenChar(Promotion));
        }
        return text;
    }

    /// <summary>Parses UCI notation ("e2e4", "e7e8q").</summary>
    public static Move Parse(string text)
    {
        if (text.Length is not (4 or 5))
        {
            throw new FormatException($"Bad move '{text}'.");
        }

        var from = PieceUtil.ParseSquare(text[..2]);
        var to = PieceUtil.ParseSquare(text[2..4]);
        if (text.Length == 5)
        {
            var promotion = PieceUtil.FromFenChar(char.ToUpperInvariant(text[4]));
            return new Move(from, to, promotion, MoveFlags.Promotion);
        }

        return new Move(from, to);
    }

    public bool Equals(Move other) =>
        From == other.From && To == other.To && Promotion == other.Promotion;

    public override bool Equals(object? obj) => obj is Move other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(From, To, Promotion);
    public static bool operator ==(Move left, Move right) => left.Equals(right);
    public static bool operator !=(Move left, Move right) => !left.Equals(right);
}
