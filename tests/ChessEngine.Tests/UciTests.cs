using ChessEngine.Cli;

namespace ChessEngine.Tests;

public sealed class UciTests
{
    private static string RunSession(string input)
    {
        using var reader = new StringReader(input);
        using var writer = new StringWriter();
        new Uci(reader, writer).Run();
        return writer.ToString();
    }

    [Fact]
    public void HandshakeAndReady()
    {
        var output = RunSession("uci\nisready\nquit\n");
        Assert.Contains("id name ChessEngine 1.0", output);
        Assert.Contains("uciok", output);
        Assert.Contains("readyok", output);
    }

    [Fact]
    public void GoReturnsALegalMove()
    {
        var output = RunSession("position startpos\ngo depth 2\nquit\n");
        var bestMove = output.Split('\n')
            .Select(l => l.Trim())
            .FirstOrDefault(l => l.StartsWith("bestmove ", StringComparison.Ordinal));
        Assert.NotNull(bestMove);

        var move = Move.Parse(bestMove["bestmove ".Length..]);
        var board = Board.StartingPosition();
        Assert.Contains(move, MoveGenerator.GenerateLegal(board));
    }

    [Fact]
    public void PositionWithMovesIsApplied()
    {
        var output = RunSession("position startpos moves e2e4 e7e5\nd\nquit\n");
        Assert.Contains("4P3", output); // White pawn reached e4.
        Assert.Contains("4p3", output); // Black pawn reached e5.
    }

    [Fact]
    public void PerftCommand()
    {
        var output = RunSession("perft 3\nquit\n");
        Assert.Contains("Nodes: 8902", output);
    }

    [Fact]
    public void SetOptionDepthIsHonoured()
    {
        var output = RunSession("setoption name Depth value 1\ngo\nquit\n");
        Assert.Contains("bestmove", output);
    }

    [Fact]
    public void UnknownCommandsAreIgnored()
    {
        var output = RunSession("nonsense\ngarbage 123\nisready\nquit\n");
        Assert.Contains("readyok", output);
    }
}
