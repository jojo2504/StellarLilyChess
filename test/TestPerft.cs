using ChessEngine;
using ChessEngine.Utils.Logging;

namespace test;

public class TestPerft {
    [Theory]
    [InlineData(1, 20UL)]
    [InlineData(2, 400UL)]
    [InlineData(3, 8902UL)]
    [InlineData(4, 197281UL)]
    public void TestPerft1(int depth, ulong expected) {
        var fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
        Chessboard chessboard = new(fen);
        Logger.Log(Channel.Debug, chessboard);
        Assert.Equal(expected, chessboard.Perft(depth));
    }

    [Theory]
    [InlineData(1, 48UL)]
    [InlineData(2, 2039UL)]
    [InlineData(3, 97862UL)]
    //[InlineData(4, 4085603UL)]
    public void TestPerft2(int depth, ulong expected) {
        var fen = "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1";
        Chessboard chessboard = new(fen);
        Logger.Log(Channel.Debug, chessboard);
        Assert.Equal(expected, chessboard.Perft(depth));
    }
    
    [Theory]
    [InlineData(1, 14UL)]
    //[InlineData(2, 191UL)]
    //[InlineData(3, 2812UL)]
    //[InlineData(4, 4085603UL)]
    public void TestPerft3(int depth, ulong expected) {
        var fen = "8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1";
        Chessboard chessboard = new(fen);
        Logger.Log(Channel.Debug, chessboard);
        Assert.Equal(expected, chessboard.Perft(depth));
        Logger.Log(Channel.Debug, "--------------------------");
    }

    [Theory]
    //[InlineData(1, 6UL)]
    [InlineData(2, 264UL)]
    //[InlineData(3, 9467UL)]
    public void TestPerft4(int depth, ulong expected) {
        var fen = "r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1";
        Chessboard chessboard = new(fen);

        Logger.Log(Channel.Debug, "test perft position 4");
        Logger.Log(Channel.Debug, chessboard);
                    
        Assert.Equal(expected, chessboard.Perft(depth));
        Logger.Log(Channel.Debug, "--------------------------");
    }

    [Theory]
    [InlineData(1, 44UL)]
    [InlineData(2, 1486UL)]
    [InlineData(3, 62379UL)]
    public void TestPerft5(int depth, ulong expected) {
        var fen = "rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8";
        Chessboard chessboard = new(fen);
        Logger.Log(Channel.Debug, chessboard);
        Assert.Equal(expected, chessboard.Perft(depth));
        Logger.Log(Channel.Debug, "--------------------------");
    }

    [Theory]
    [InlineData(1, 46UL)]
    [InlineData(2, 2079UL)]
    [InlineData(3, 898909UL)]
    public void TestPerft6(int depth, ulong expected) {
        var fen = "r4rk1/1pp1qppp/p1np1n2/2b1p1B1/2B1P1b1/P1NP1N2/1PP1QPPP/R4RK1 w - - 0 10";
        Chessboard chessboard = new(fen);
        Logger.Log(Channel.Debug, chessboard);
        Assert.Equal(expected, chessboard.Perft(depth));
        Logger.Log(Channel.Debug, "--------------------------");
    }

    [Theory]
    [InlineData(1, 4UL)]
    [InlineData(2, 16UL)]
    //[InlineData(3, 898909UL)]
    public void TestPerftCustom1(int depth, ulong expected) {
        var fen = "k7/8/8/8/p7/8/7P/7K w - - 1 1";
        Chessboard chessboard = new(fen);
        Logger.Log(Channel.Debug, chessboard);
        Assert.Equal(expected, chessboard.Perft(depth));
        Logger.Log(Channel.Debug, "--------------------------");
    }

    [Theory]
    //[InlineData(1, 4UL)]
    [InlineData(2, 16UL)]
    public void TestPerftCustom2(int depth, ulong expected) {
        var fen = "k7/8/8/8/p6P/8/8/7K b - H3 1 1";
        Chessboard chessboard = new(fen);
        Logger.Log(Channel.Debug, chessboard);
        Assert.Equal(expected, chessboard.Perft(depth));
        Logger.Log(Channel.Debug, "--------------------------");
    }

    [Theory]
    [InlineData(1, 4UL)]
    [InlineData(2, 16UL)]
    public void TestPerftCustom3(int depth, ulong expected) {
        var fen = "k7/8/8/8/p7/8/7P/7K b - - 1 1";
        Chessboard chessboard = new(fen);
        Logger.Log(Channel.Debug, "searching depth:", depth, chessboard);
        Assert.Equal(expected, chessboard.Perft(depth));
        Logger.Log(Channel.Debug, "--------------------------");
    }

    [Theory]
    [InlineData(1, 12UL)]
    public void TestPerftCustom4(int depth, ulong expected) {
        var fen = "r3k2r/p6p/P6P/8/8/p6p/P6P/R3K2R w KQkq - 0 1";
        Chessboard chessboard = new(fen);
        Logger.Log(Channel.Debug, "searching depth:", depth, chessboard);
        Assert.Equal(expected, chessboard.Perft(depth));
        Logger.Log(Channel.Debug, "--------------------------");
    }
}
