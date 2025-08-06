using ChessEngine;
using ChessEngine.Utils.Logging;

namespace test;

public class TestPerft {
    /*public void TestPerft1() {
        var expectedResult = new Dictionary<int, ulong>() { 
            {1, 20},
            {2, 400},
            {3, 8902},
            {4, 197281}
        };
        for (int depth = 1; depth <= 4; depth++) {
            var fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
            Chessboard chessboard = new(fen);
            Assert.Equal(expectedResult[depth], chessboard.Perft(depth));
        }
    }*/

    [Theory]
    [InlineData(1, 20UL)]
    [InlineData(2, 400UL)]
    [InlineData(3, 8902UL)]
    [InlineData(4, 197281UL)]
    public void TestPerft1(int depth, ulong expected)
    {
        var fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
        Chessboard chessboard = new(fen);
        Logger.Log(chessboard);
        Assert.Equal(expected, chessboard.Perft(depth));
    }

    [Theory]
    [InlineData(1, 48UL)]
    //[InlineData(2, 2039UL)]
    //[InlineData(3, 97862UL)]
    //[InlineData(4, 4085603UL)]
    public void TestPerft2(int depth, ulong expected)
    {
        var fen = "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1";
        Chessboard chessboard = new(fen);
        Logger.Log(chessboard);
        Assert.Equal(expected, chessboard.Perft(depth));
    }

    [Theory]
    //[InlineData(1, 14UL)]
    [InlineData(2, 191UL)]
    //[InlineData(3, 2812UL)]
    //[InlineData(4, 4085603UL)]
    public void TestPerft3(int depth, ulong expected)
    {
        var fen = "8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1 ";
        Chessboard chessboard = new(fen);
        Logger.Log(chessboard);
        Assert.Equal(expected, chessboard.Perft(depth));
        Logger.Log("--------------------------");
    }
}
