namespace ChessEngine;

/// <summary>Piece codes. Values 1-6 are White, 7-12 are Black; 0 is empty.</summary>
public enum Piece : byte
{
    None = 0,
    WhitePawn = 1,
    WhiteKnight = 2,
    WhiteBishop = 3,
    WhiteRook = 4,
    WhiteQueen = 5,
    WhiteKing = 6,
    BlackPawn = 7,
    BlackKnight = 8,
    BlackBishop = 9,
    BlackRook = 10,
    BlackQueen = 11,
    BlackKing = 12,
}

public enum Color : byte
{
    White = 0,
    Black = 1,
}

/// <summary>Helpers over <see cref="Piece"/> and square indices (0 = a1, 63 = h8).</summary>
public static class PieceUtil
{
    public static Color ColorOf(Piece piece) => piece switch
    {
        Piece.WhitePawn or Piece.WhiteKnight or Piece.WhiteBishop or Piece.WhiteRook
            or Piece.WhiteQueen or Piece.WhiteKing => Color.White,
        Piece.BlackPawn or Piece.BlackKnight or Piece.BlackBishop or Piece.BlackRook
            or Piece.BlackQueen or Piece.BlackKing => Color.Black,
        _ => throw new ArgumentException($"Piece {piece} has no color.", nameof(piece)),
    };

    /// <summary>1 = pawn, 2 = knight, 3 = bishop, 4 = rook, 5 = queen, 6 = king.</summary>
    public static int TypeOf(Piece piece) => piece switch
    {
        Piece.WhitePawn or Piece.BlackPawn => 1,
        Piece.WhiteKnight or Piece.BlackKnight => 2,
        Piece.WhiteBishop or Piece.BlackBishop => 3,
        Piece.WhiteRook or Piece.BlackRook => 4,
        Piece.WhiteQueen or Piece.BlackQueen => 5,
        Piece.WhiteKing or Piece.BlackKing => 6,
        _ => 0,
    };

    public static bool IsWhite(Piece piece) => piece is >= Piece.WhitePawn and <= Piece.WhiteKing;
    public static bool IsBlack(Piece piece) => piece is >= Piece.BlackPawn and <= Piece.BlackKing;

    public static Piece For(Color color, int type) => (color, type) switch
    {
        (Color.White, 1) => Piece.WhitePawn,
        (Color.White, 2) => Piece.WhiteKnight,
        (Color.White, 3) => Piece.WhiteBishop,
        (Color.White, 4) => Piece.WhiteRook,
        (Color.White, 5) => Piece.WhiteQueen,
        (Color.White, 6) => Piece.WhiteKing,
        (Color.Black, 1) => Piece.BlackPawn,
        (Color.Black, 2) => Piece.BlackKnight,
        (Color.Black, 3) => Piece.BlackBishop,
        (Color.Black, 4) => Piece.BlackRook,
        (Color.Black, 5) => Piece.BlackQueen,
        (Color.Black, 6) => Piece.BlackKing,
        _ => throw new ArgumentException($"No piece for color {color}, type {type}."),
    };

    public static Color Opponent(Color color) => color == Color.White ? Color.Black : Color.White;

    public static int FileOf(int square) => square & 7;
    public static int RankOf(int square) => square >> 3;
    public static int Square(int file, int rank) => rank * 8 + file;
    public static bool OnBoard(int file, int rank) => file is >= 0 and < 8 && rank is >= 0 and < 8;

    /// <summary>Parses an algebraic square such as "e4" (0-63), or -1 for "-".</summary>
    public static int ParseSquare(string text)
    {
        if (text == "-") return -1;
        if (text.Length != 2) throw new FormatException($"Bad square '{text}'.");
        var file = text[0] - 'a';
        var rank = text[1] - '1';
        if (!OnBoard(file, rank)) throw new FormatException($"Bad square '{text}'.");
        return Square(file, rank);
    }

    public static string SquareName(int square) =>
        $"{(char)('a' + FileOf(square))}{(char)('1' + RankOf(square))}";

    public static char ToFenChar(Piece piece) => piece switch
    {
        Piece.WhitePawn => 'P',
        Piece.WhiteKnight => 'N',
        Piece.WhiteBishop => 'B',
        Piece.WhiteRook => 'R',
        Piece.WhiteQueen => 'Q',
        Piece.WhiteKing => 'K',
        Piece.BlackPawn => 'p',
        Piece.BlackKnight => 'n',
        Piece.BlackBishop => 'b',
        Piece.BlackRook => 'r',
        Piece.BlackQueen => 'q',
        Piece.BlackKing => 'k',
        _ => throw new ArgumentException($"Cannot render piece {piece} as FEN.", nameof(piece)),
    };

    public static Piece FromFenChar(char c) => c switch
    {
        'P' => Piece.WhitePawn,
        'N' => Piece.WhiteKnight,
        'B' => Piece.WhiteBishop,
        'R' => Piece.WhiteRook,
        'Q' => Piece.WhiteQueen,
        'K' => Piece.WhiteKing,
        'p' => Piece.BlackPawn,
        'n' => Piece.BlackKnight,
        'b' => Piece.BlackBishop,
        'r' => Piece.BlackRook,
        'q' => Piece.BlackQueen,
        'k' => Piece.BlackKing,
        _ => throw new FormatException($"Bad FEN piece character '{c}'."),
    };
}
