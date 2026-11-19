namespace ChessEngine.Tests;

public sealed class SearchTests
{
    [Fact]
    public void FindsScholarsMate()
    {
        // 1. e4 e5 2. Bc4 Nc6 3. Qh5 — Qxf7 is mate in one.
        var board = Board.FromFen("r1bqkbnr/pppp1ppp/2n5/4p2Q/2B1P3/8/PPPP1PPP/RNB1K1NR w KQkq - 0 1");
        var best = new Searcher(board).FindBestMove(3);
        Assert.Equal("h5f7", best?.ToString());
    }

    [Fact]
    public void FindsBackRankMate()
    {
        var board = Board.FromFen("6k1/5ppp/8/8/8/8/8/R5K1 w - - 0 1");
        var best = new Searcher(board).FindBestMove(3);
        Assert.Equal("a1a8", best?.ToString());
    }

    [Fact]
    public void StartPositionBestMoveIsLegal()
    {
        var board = Board.StartingPosition();
        var best = new Searcher(board).FindBestMove(3);
        Assert.NotNull(best);
        Assert.Contains(best.Value, MoveGenerator.GenerateLegal(board));
    }

    [Fact]
    public void StalemateHasNoMove()
    {
        // Black to move, no legal moves, not in check.
        var board = Board.FromFen("7k/5Q2/6K1/8/8/8/8/8 b - - 0 1");
        var best = new Searcher(board).FindBestMove(2);
        Assert.Null(best);
    }

    [Fact]
    public void EvaluationStartPositionIsBalanced()
    {
        Assert.Equal(0, Evaluation.Evaluate(Board.StartingPosition()));
    }

    [Fact]
    public void EvaluationRewardsMaterial()
    {
        // White is a queen up.
        var board = Board.FromFen("4k3/8/8/8/8/8/8/3QK3 w - - 0 1");
        Assert.True(Evaluation.Evaluate(board) > 800);
    }

    [Fact]
    public void SearchRespectsTimeBudget()
    {
        var board = Board.StartingPosition();
        var searcher = new Searcher(board);
        var best = searcher.FindBestMove(10, TimeSpan.FromMilliseconds(200));
        Assert.NotNull(best);
        Assert.True(searcher.MaxDepthReached >= 1);
    }
}
