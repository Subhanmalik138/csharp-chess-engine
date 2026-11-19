namespace ChessEngine;

/// <summary>
/// Perft ("performance test"): counts the leaf nodes of the move tree to a given
/// depth. The counts are known constants for standard test positions, so perft is
/// the standard way to prove a move generator is correct.
/// </summary>
public static class Perft
{
    public static long Count(Board board, int depth)
    {
        if (depth == 0) return 1;

        var moves = MoveGenerator.GenerateLegal(board);
        if (depth == 1) return moves.Count;

        long nodes = 0;
        foreach (var move in moves)
        {
            board.Make(move, out var undo);
            nodes += Count(board, depth - 1);
            board.Unmake(move, undo);
        }

        return nodes;
    }

    /// <summary>Perft with a per-root-move breakdown (the "divide" listing).</summary>
    public static Dictionary<string, long> Divide(Board board, int depth)
    {
        var result = new Dictionary<string, long>();
        foreach (var move in MoveGenerator.GenerateLegal(board))
        {
            board.Make(move, out var undo);
            result[move.ToString()] = Count(board, depth - 1);
            board.Unmake(move, undo);
        }

        return result;
    }
}
