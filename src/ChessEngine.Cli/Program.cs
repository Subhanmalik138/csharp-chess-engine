namespace ChessEngine.Cli;

/// <summary>
/// ChessEngine CLI.
///   (no args)            — start the UCI loop (for chess GUIs)
///   perft &lt;depth&gt; [fen] — count leaf nodes (move-generator correctness check)
///   bestmove [fen] [depth] — print the engine's best move for a position
///   bench                — perft benchmark over four standard test positions
/// </summary>
public static class Program
{
    public static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            new Uci(Console.In, Console.Out).Run();
            return;
        }

        switch (args[0])
        {
            case "perft":
            {
                var depth = int.Parse(args[1]);
                var board = args.Length > 2 ? Board.FromFen(string.Join(" ", args[2..])) : Board.StartingPosition();
                Console.WriteLine($"Nodes: {Perft.Count(board, depth)}");
                break;
            }

            case "bestmove":
            {
                var depth = 5;
                var rest = args[1..];
                if (rest.Length > 0 && int.TryParse(rest[^1], out var d))
                {
                    depth = d;
                    rest = rest[..^1];
                }

                var board = rest.Length == 0 ? Board.StartingPosition() : Board.FromFen(string.Join(" ", rest));
                var best = new Searcher(board).FindBestMove(depth);
                Console.WriteLine(best is null ? "bestmove 0000" : $"bestmove {best.Value}");
                break;
            }

            case "bench":
                new Uci(Console.In, Console.Out).RunBench();
                break;

            default:
                Console.WriteLine("Usage:");
                Console.WriteLine("  ChessEngine.Cli                 Start the UCI loop");
                Console.WriteLine("  ChessEngine.Cli perft <depth> [fen]");
                Console.WriteLine("  ChessEngine.Cli bestmove [fen] [depth]");
                Console.WriteLine("  ChessEngine.Cli bench");
                break;
        }
    }
}
