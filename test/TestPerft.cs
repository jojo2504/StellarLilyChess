using ChessEngine;
using ChessEngine.Utils.Logging;

namespace test;

public class TestPerft {
    [Theory]
    [InlineData(1, 20UL)]
    [InlineData(2, 400UL)]
    [InlineData(3, 8902UL)]
    [InlineData(4, 197281UL)]
    [InlineData(5, 4865609UL)]
    //[InlineData(6, 119060324UL)]
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
        Logger.Log(Channel.Debug, chessboard.State);
        Logger.Log(Channel.Debug, chessboard);
        Assert.Equal(expected, chessboard.Perft(depth));
    }

    [Theory]
    [InlineData(1, 14UL)]
    [InlineData(2, 191UL)]
    [InlineData(3, 2812UL)]
    [InlineData(4, 43238UL)]
    [InlineData(5, 674624UL)]
    public void TestPerft3(int depth, ulong expected) {
        var fen = "8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1";
        Chessboard chessboard = new(fen);
        Logger.Log(Channel.Debug, chessboard);
        Assert.Equal(expected, chessboard.Perft(depth));
        Logger.Log(Channel.Debug, "--------------------------");
    }

    [Theory]
    [InlineData(1, 6UL)]
    [InlineData(2, 264UL)]
    [InlineData(3, 9467UL)]
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
    [InlineData(3, 89890UL)]
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

    [Theory]
    [InlineData(1, 43UL)]
    public void TestPerft2Custom1(int depth, ulong expected) {
        var fen = "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R b KQkq - 0 1";
        Chessboard chessboard = new(fen);
        Logger.Log(Channel.Debug, "searching depth:", depth, chessboard);
        Assert.Equal(expected, chessboard.Perft(depth));
        Logger.Log(Channel.Debug, "--------------------------");
    }

    [Theory]
    [InlineData(1, 17UL)]
    public void TestPerft3Custom1(int depth, ulong expected) {
        var fen = "8/2p5/3p4/KP5r/1R3pPk/8/4P3/8 b - g3 0 1";
        Chessboard chessboard = new(fen);
        Logger.Log(Channel.Debug, "searching depth:", depth, chessboard);
        Assert.Equal(expected, chessboard.Perft(depth));
        Logger.Log(Channel.Debug, "--------------------------");
    }

    [Theory]
    [InlineData(1, 12UL)]
    public void TestPerftCanBlackKingCastle(int depth, ulong expected) {
        var fen = "r3k2r/p6p/P6P/8/8/8/8/4K3 b kq - 0 1";
        Chessboard chessboard = new(fen);
        Logger.Log(Channel.Debug, "searching depth:", depth, chessboard);
        Assert.Equal(expected, chessboard.Perft(depth));
        Logger.Log(Channel.Debug, "--------------------------");
    }

    [Theory]
    [InlineData(2, 20UL)]
    public void TestPerftCheckmate(int depth, ulong expected) {
        var fen = "5k2/7R/5K1P/5P2/8/8/8/8 w - - 0 1";
        Chessboard chessboard = new(fen);
        Logger.Log(Channel.Debug, "searching depth:", depth, chessboard);
        Assert.Equal(expected, chessboard.Perft(depth));
        Logger.Log(Channel.Debug, "--------------------------");
    }

    [Theory]
    [InlineData(1, 45UL)]
    [InlineData(2, 1623UL)]
    public void TestPerft4Custom1(int depth, ulong expected) {
        var fen = "r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P1RPP/R2Q2K1 b kq - 0 1";
        Chessboard chessboard = new(fen);
        Logger.Log(Channel.Debug, "searching depth:", depth, chessboard);
        Assert.Equal(expected, chessboard.Perft(depth));
        Logger.Log(Channel.Debug, "--------------------------");
    }

    [Theory]
    [InlineData(1, 34UL)]
    [InlineData(2, 1373UL)]
    public void TestPerft5Custom1(int depth, ulong expected) {
        var fen = "rnbq1k1r/pp1Pbppp/2p5/8/2B5/P7/1PP1NnPP/RNBQK2R b KQ - 1 8";
        Chessboard chessboard = new(fen);
        Logger.Log(Channel.Debug, "searching depth:", depth, chessboard);
        Assert.Equal(expected, chessboard.Perft(depth));
        Logger.Log(Channel.Debug, "--------------------------");
    }

    [Theory]
    [InlineData(1, 4UL)]
    public void TestPromotion(int depth, ulong expected) {
        var fen = "K6k/8/6Q1/8/8/8/3p4/8 b - - 0 1";
        Chessboard chessboard = new(fen);
        Logger.Log(Channel.Debug, "searching depth:", depth, chessboard);
        Assert.Equal(expected, chessboard.Perft(depth));
        Logger.Log(Channel.Debug, "--------------------------");
    }

    [Theory]
    [InlineData(4, 10087UL)]
    public void TestCustomwtfisgoingon(int depth, ulong expected) {
        var fen = "7k/8/8/8/1p6/7p/P5P1/R3K3 w Q - 0 1";
        Chessboard chessboard = new(fen);
        Logger.Log(Channel.Debug, "searching depth:", depth, chessboard);
        Assert.Equal(expected, chessboard.Perft(depth));
        Logger.Log(Channel.Debug, "--------------------------");
    }

    [Theory]
    [InlineData(2, 3UL)]
    public void TestCapturePerft2(int depth, ulong expected) {
        var fen = "8/1p3p1p/5PkP/5pPp/P4PpP/5pKp/5P1P/8 b - - 0 2";
        Chessboard chessboard = new(fen);
        Logger.Log(Channel.Debug, "searching depth:", depth, chessboard);
        Assert.Equal(expected, chessboard.Perft(depth));
        Logger.Log(Channel.Debug, "--------------------------");
    }
}