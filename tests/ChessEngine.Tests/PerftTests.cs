namespace ChessEngine.Tests;

/// <summary>
/// Perft counts are known constants for standard test positions — if any of these
/// fail, the move generator (castling, en passant, promotion, pins) is wrong.
/// </summary>
public sealed class PerftTests
{
    [Theory]
    [InlineData(1, 20)]
    [InlineData(2, 400)]
    [InlineData(3, 8902)]
    [InlineData(4, 197281)]
    [InlineData(5, 4865609)]
    public void StartPosition(int depth, long expected)
    {
        Assert.Equal(expected, Perft.Count(Board.StartingPosition(), depth));
    }

    // Kiwipete: castling, both en-passant captures and promotions all in one position.
    [Theory]
    [InlineData(1, 48)]
    [InlineData(2, 2039)]
    [InlineData(3, 97862)]
    public void Kiwipete(int depth, long expected)
    {
        var board = Board.FromFen("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1");
        Assert.Equal(expected, Perft.Count(board, depth));
    }

    // En-passant pins: the only legal way to catch a horizontal rook pin.
    [Theory]
    [InlineData(1, 14)]
    [InlineData(2, 191)]
    [InlineData(3, 2812)]
    [InlineData(4, 43238)]
    public void EnPassantPinPosition(int depth, long expected)
    {
        var board = Board.FromFen("8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1");
        Assert.Equal(expected, Perft.Count(board, depth));
    }

    // Heavy castling + promotion position.
    [Theory]
    [InlineData(1, 6)]
    [InlineData(2, 264)]
    [InlineData(3, 9467)]
    public void CastlingAndPromotionPosition(int depth, long expected)
    {
        var board = Board.FromFen("r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1");
        Assert.Equal(expected, Perft.Count(board, depth));
    }

    [Theory]
    [InlineData(1, 44)]
    [InlineData(2, 1486)]
    [InlineData(3, 62379)]
    public void PromotionPosition(int depth, long expected)
    {
        var board = Board.FromFen("rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8");
        Assert.Equal(expected, Perft.Count(board, depth));
    }

    [Fact]
    public void DivideSumsToTotal()
    {
        var board = Board.StartingPosition();
        var divide = Perft.Divide(board, 3);
        Assert.Equal(20, divide.Count);
        Assert.Equal(8902, divide.Values.Sum());
        Assert.Equal(600, divide["e2e4"]);
    }
}
