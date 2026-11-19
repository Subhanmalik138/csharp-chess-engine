namespace ChessEngine.Cli;

/// <summary>
/// The Universal Chess Interface (UCI) loop: the protocol every chess GUI speaks.
/// Reads commands from <paramref name="input"/> and writes replies to
/// <paramref name="output"/>, so the loop is testable without a real console.
/// </summary>
public sealed class Uci
{
    private readonly TextReader _input;
    private readonly TextWriter _output;
    private Board _board = Board.StartingPosition();
    private int _defaultDepth = 5;

    public Uci(TextReader input, TextWriter output)
    {
        _input = input;
        _output = output;
    }

    public void Run()
    {
        string? line;
        while ((line = _input.ReadLine()) is not null)
        {
            var tokens = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0) continue;

            switch (tokens[0])
            {
                case "uci":
                    _output.WriteLine("id name ChessEngine 1.0");
                    _output.WriteLine("id author Subhan Malik");
                    _output.WriteLine("uciok");
                    break;

                case "isready":
                    _output.WriteLine("readyok");
                    break;

                case "ucinewgame":
                    _board = Board.StartingPosition();
                    break;

                case "setoption":
                    ApplyOption(tokens);
                    break;

                case "position":
                    ApplyPosition(tokens);
                    break;

                case "go":
                    RunGo(tokens);
                    break;

                case "d":
                    _output.Write(_board.PrettyPrint());
                    _output.WriteLine("Fen: " + _board.ToFen());
                    break;

                case "perft":
                    RunPerft(tokens);
                    break;

                case "bench":
                    RunBench();
                    break;

                case "quit":
                    return;

                default:
                    // Unknown commands are ignored, per the UCI spec's spirit:
                    // a GUI may send extensions this engine does not understand.
                    break;
            }

            _output.Flush();
        }
    }

    private void ApplyOption(string[] tokens)
    {
        // setoption name <name> [value <value>] — currently only Depth is honoured.
        var nameIndex = Array.IndexOf(tokens, "name");
        var valueIndex = Array.IndexOf(tokens, "value");
        if (nameIndex < 0 || valueIndex < 0 || valueIndex <= nameIndex + 1) return;
        var name = string.Join(" ", tokens[(nameIndex + 1)..valueIndex]);
        if (name.Equals("Depth", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(tokens[valueIndex + 1], out var depth) && depth > 0)
        {
            _defaultDepth = depth;
        }
    }

    private void ApplyPosition(string[] tokens)
    {
        // position (startpos | fen <fen...>) [moves <move>...]
        var movesIndex = Array.IndexOf(tokens, "moves");
        var fenTokens = movesIndex < 0 ? tokens[1..] : tokens[1..movesIndex];

        if (fenTokens is ["startpos"])
        {
            _board = Board.StartingPosition();
        }
        else if (fenTokens.Length > 0 && fenTokens[0] == "fen")
        {
            _board = Board.FromFen(string.Join(" ", fenTokens[1..]));
        }
        else
        {
            return;
        }

        if (movesIndex < 0) return;
        foreach (var text in tokens[(movesIndex + 1)..])
        {
            var parsed = Move.Parse(text);
            // Resolve the parsed move against the legal list so flags (capture,
            // castling, en passant, promotion) are filled in correctly.
            var legal = MoveGenerator.GenerateLegal(_board);
            Move? match = null;
            foreach (var m in legal)
            {
                if (m.From == parsed.From && m.To == parsed.To && m.Promotion == parsed.Promotion)
                {
                    match = m;
                    break;
                }
            }

            if (match is null)
            {
                throw new InvalidOperationException($"Illegal move '{text}' in position {_board.ToFen()}.");
            }

            _board.Make(match.Value, out _);
        }
    }

    private void RunGo(string[] tokens)
    {
        var depth = _defaultDepth;
        TimeSpan? budget = null;

        var depthIndex = Array.IndexOf(tokens, "depth");
        if (depthIndex >= 0 && depthIndex + 1 < tokens.Length && int.TryParse(tokens[depthIndex + 1], out var d))
        {
            depth = d;
        }

        var moveTimeIndex = Array.IndexOf(tokens, "movetime");
        if (moveTimeIndex >= 0 && moveTimeIndex + 1 < tokens.Length
            && int.TryParse(tokens[moveTimeIndex + 1], out var ms))
        {
            budget = TimeSpan.FromMilliseconds(ms);
        }
        else
        {
            // wtime/btime: spend roughly a thirtieth of the remaining clock.
            var clockIndex = Array.IndexOf(tokens, _board.SideToMove == Color.White ? "wtime" : "btime");
            if (clockIndex >= 0 && clockIndex + 1 < tokens.Length
                && long.TryParse(tokens[clockIndex + 1], out var remaining))
            {
                budget = TimeSpan.FromMilliseconds(Math.Max(50, remaining / 30));
            }
        }

        var searcher = new Searcher(_board);
        var best = searcher.FindBestMove(depth, budget);
        _output.WriteLine(best is null ? "bestmove 0000" : $"bestmove {best.Value}");
    }

    private void RunPerft(string[] tokens)
    {
        if (tokens.Length < 2 || !int.TryParse(tokens[1], out var depth)) return;
        _output.WriteLine($"Nodes: {Perft.Count(_board, depth)}");
    }

    public void RunBench()
    {
        var positions = new[]
        {
            Board.StartFen,
            "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1", // Kiwipete
            "8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1",
            "r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1",
        };

        var totalNodes = 0L;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        foreach (var fen in positions)
        {
            var board = Board.FromFen(fen);
            totalNodes += Perft.Count(board, 4);
        }

        stopwatch.Stop();
        var nps = totalNodes / Math.Max(0.001, stopwatch.Elapsed.TotalSeconds);
        _output.WriteLine($"Bench: {totalNodes} nodes in {stopwatch.Elapsed.TotalSeconds:F2}s ({nps:F0} nps)");
    }
}
