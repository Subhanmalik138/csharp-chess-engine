namespace ChessEngine;

/// <summary>
/// Negamax search with alpha-beta pruning, iterative deepening, quiescence search
/// and MVV-LVA capture ordering. Scores are in centipawns from the side-to-move's
/// perspective; mate scores are offset by ply so faster mates score higher.
/// </summary>
public sealed class Searcher
{
    private const int Infinity = 1_000_000;
    private const int CheckNodeInterval = 2048;

    private readonly Board _board;
    private DateTime _deadline = DateTime.MaxValue;
    private int _nodesSinceCheck;

    public long Nodes { get; private set; }
    public int MaxDepthReached { get; private set; }

    public Searcher(Board board)
    {
        _board = board;
    }

    /// <summary>
    /// Searches to <paramref name="depth"/> with iterative deepening, stopping early
    /// if <paramref name="timeBudget"/> elapses. Returns the best move found, or null
    /// when the position has no legal moves.
    /// </summary>
    public Move? FindBestMove(int depth, TimeSpan? timeBudget = null)
    {
        Nodes = 0;
        MaxDepthReached = 0;
        _deadline = timeBudget.HasValue ? DateTime.UtcNow + timeBudget.Value : DateTime.MaxValue;

        Move? best = null;
        for (var d = 1; d <= depth; d++)
        {
            var (move, _) = SearchRoot(d);
            if (move is null) break; // No legal moves, or the search was aborted at depth 1.
            best = move;
            MaxDepthReached = d;
            if (TimedOut()) break;
        }

        return best;
    }

    private (Move? Move, int Score) SearchRoot(int depth)
    {
        var moves = MoveGenerator.GenerateLegal(_board);
        if (moves.Count == 0) return (null, 0);

        OrderMoves(_board, moves);
        var alpha = -Infinity;
        Move? best = null;
        var bestScore = -Infinity;

        foreach (var move in moves)
        {
            _board.Make(move, out var undo);
            var score = -Negamax(depth - 1, -Infinity, -alpha, 1);
            _board.Unmake(move, undo);

            if (TimedOut()) return (best, bestScore);
            if (score > bestScore)
            {
                bestScore = score;
                best = move;
            }

            alpha = Math.Max(alpha, score);
        }

        return (best, bestScore);
    }

    private int Negamax(int depth, int alpha, int beta, int ply)
    {
        Nodes++;
        if ((Nodes & (CheckNodeInterval - 1)) == 0 && TimedOut())
        {
            return 0; // Aborted; the caller discards this iteration's result.
        }

        if (depth == 0) return Quiescence(alpha, beta, ply);

        var moves = MoveGenerator.GenerateLegal(_board);
        if (moves.Count == 0)
        {
            // Checkmate (side to move is in check with no moves) or stalemate.
            return _board.IsInCheck(_board.SideToMove) ? -Evaluation.MateScore + ply : 0;
        }

        OrderMoves(_board, moves);
        var best = -Infinity;
        foreach (var move in moves)
        {
            _board.Make(move, out var undo);
            var score = -Negamax(depth - 1, -beta, -alpha, ply + 1);
            _board.Unmake(move, undo);

            if (TimedOut()) return 0;
            best = Math.Max(best, score);
            alpha = Math.Max(alpha, score);
            if (alpha >= beta) break; // Beta cutoff.
        }

        return best;
    }

    /// <summary>
    /// Captures-only search at the horizon, so the engine does not stop mid-exchange
    /// and misevaluate a hanging queen as a win.
    /// </summary>
    private int Quiescence(int alpha, int beta, int ply)
    {
        var standPat = Evaluation.Evaluate(_board);
        if (standPat >= beta) return beta;
        if (standPat > alpha) alpha = standPat;

        var moves = MoveGenerator.GenerateLegal(_board);
        OrderMoves(_board, moves);
        foreach (var move in moves)
        {
            if (!move.IsCapture) continue;
            _board.Make(move, out var undo);
            var score = -Quiescence(-beta, -alpha, ply + 1);
            _board.Unmake(move, undo);

            if (TimedOut()) return 0;
            if (score >= beta) return beta;
            alpha = Math.Max(alpha, score);
        }

        return alpha;
    }

    /// <summary>
    /// Orders captures first by most-valuable-victim / least-valuable-attacker, then
    /// promotions, then quiet moves. A good order is what makes alpha-beta pruning work.
    /// </summary>
    private static void OrderMoves(Board board, List<Move> moves)
    {
        moves.Sort((a, b) => MoveScore(board, b).CompareTo(MoveScore(board, a)));
    }

    private static int MoveScore(Board board, Move move)
    {
        var score = 0;
        if (move.IsCapture)
        {
            var victim = move.IsEnPassant
                ? (board.SideToMove == Color.White ? Piece.BlackPawn : Piece.WhitePawn)
                : board.Squares[move.To];
            var attacker = board.Squares[move.From];
            score += 10 * PieceValue(victim) - PieceValue(attacker) / 10;
        }

        if (move.IsPromotion) score += 900;
        return score;
    }

    private static int PieceValue(Piece piece) => PieceUtil.TypeOf(piece) switch
    {
        1 => 100,
        2 => 320,
        3 => 330,
        4 => 500,
        5 => 900,
        _ => 0,
    };

    private bool TimedOut() => DateTime.UtcNow >= _deadline;
}
