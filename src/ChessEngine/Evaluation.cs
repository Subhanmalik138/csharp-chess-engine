namespace ChessEngine;

/// <summary>
/// Static evaluation: material plus piece-square tables, scored in centipawns from
/// the side-to-move's perspective. The tables are the standard simplified
/// evaluation tables, written from White's point of view (a1 = index 0) and
/// mirrored for Black.
/// </summary>
public static class Evaluation
{
    public const int MateScore = 100_000;

    private static readonly int[] Material = { 0, 100, 320, 330, 500, 900, 0 };

    // Tables indexed by square 0 (a1) .. 63 (h8), written for White.
    private static readonly int[] PawnTable =
    {
          0,  0,  0,  0,  0,  0,  0,  0,
         50, 50, 50, 50, 50, 50, 50, 50,
         10, 10, 20, 30, 30, 20, 10, 10,
          5,  5, 10, 25, 25, 10,  5,  5,
          0,  0,  0, 20, 20,  0,  0,  0,
          5, -5,-10,  0,  0,-10, -5,  5,
          5, 10, 10,-20,-20, 10, 10,  5,
          0,  0,  0,  0,  0,  0,  0,  0,
    };

    private static readonly int[] KnightTable =
    {
        -50,-40,-30,-30,-30,-30,-40,-50,
        -40,-20,  0,  0,  0,  0,-20,-40,
        -30,  0, 10, 15, 15, 10,  0,-30,
        -30,  5, 15, 20, 20, 15,  5,-30,
        -30,  0, 15, 20, 20, 15,  0,-30,
        -30,  5, 10, 15, 15, 10,  5,-30,
        -40,-20,  0,  5,  5,  0,-20,-40,
        -50,-40,-30,-30,-30,-30,-40,-50,
    };

    private static readonly int[] BishopTable =
    {
        -20,-10,-10,-10,-10,-10,-10,-20,
        -10,  0,  0,  0,  0,  0,  0,-10,
        -10,  0,  5, 10, 10,  5,  0,-10,
        -10,  5,  5, 10, 10,  5,  5,-10,
        -10,  0, 10, 10, 10, 10,  0,-10,
        -10, 10, 10, 10, 10, 10, 10,-10,
        -10,  5,  0,  0,  0,  0,  5,-10,
        -20,-10,-10,-10,-10,-10,-10,-20,
    };

    private static readonly int[] RookTable =
    {
          0,  0,  0,  0,  0,  0,  0,  0,
          5, 10, 10, 10, 10, 10, 10,  5,
         -5,  0,  0,  0,  0,  0,  0, -5,
         -5,  0,  0,  0,  0,  0,  0, -5,
         -5,  0,  0,  0,  0,  0,  0, -5,
         -5,  0,  0,  0,  0,  0,  0, -5,
         -5,  0,  0,  0,  0,  0,  0, -5,
          0,  0,  0,  5,  5,  0,  0,  0,
    };

    private static readonly int[] QueenTable =
    {
        -20,-10,-10, -5, -5,-10,-10,-20,
        -10,  0,  0,  0,  0,  0,  0,-10,
        -10,  0,  5,  5,  5,  5,  0,-10,
         -5,  0,  5,  5,  5,  5,  0, -5,
          0,  0,  5,  5,  5,  5,  0, -5,
        -10,  5,  5,  5,  5,  5,  0,-10,
        -10,  0,  5,  0,  0,  0,  0,-10,
        -20,-10,-10, -5, -5,-10,-10,-20,
    };

    private static readonly int[] KingTable =
    {
        -30,-40,-40,-50,-50,-40,-40,-30,
        -30,-40,-40,-50,-50,-40,-40,-30,
        -30,-40,-40,-50,-50,-40,-40,-30,
        -30,-40,-40,-50,-50,-40,-40,-30,
        -20,-30,-30,-40,-40,-30,-30,-20,
        -10,-20,-20,-20,-20,-20,-20,-10,
         20, 20,  0,  0,  0,  0, 20, 20,
         20, 30, 10,  0,  0, 10, 30, 20,
    };

    private static int[] Table(int type) => type switch
    {
        1 => PawnTable,
        2 => KnightTable,
        3 => BishopTable,
        4 => RookTable,
        5 => QueenTable,
        6 => KingTable,
        _ => throw new ArgumentException($"No table for piece type {type}."),
    };

    /// <summary>Static score in centipawns, positive when the side to move is ahead.</summary>
    public static int Evaluate(Board board)
    {
        var score = 0;
        for (var sq = 0; sq < 64; sq++)
        {
            var piece = board.Squares[sq];
            if (piece == Piece.None) continue;

            var type = PieceUtil.TypeOf(piece);
            var value = Material[type];
            if (PieceUtil.IsWhite(piece))
            {
                score += value + Table(type)[sq];
            }
            else
            {
                // Mirror the square vertically: a black piece on a8 reads the table as a1.
                score -= value + Table(type)[sq ^ 56];
            }
        }

        return board.SideToMove == Color.White ? score : -score;
    }
}
