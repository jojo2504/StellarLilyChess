using ChessEngine;
using ChessEngine.Utils.Logging;

namespace test;

public class TestGames {
    [Fact]
    public void TestGame1() {
        var gameRecord = System.IO.File.ReadLines(@$"{AppDomain.CurrentDomain.BaseDirectory}/Records/gamebugged.txt");
        var bugged = false;
        // iterate through each element within the array and
        // print it out
        //
        try {
            Chessboard chessboard = new();
            foreach (var move in gameRecord) {
                chessboard.PushUci(move);
                Logger.Log(Channel.Debug, chessboard);
                Logger.Log(Channel.Debug, chessboard.stateStack.ElementAt(0));
                Logger.Log(Channel.Debug, "--------------------------------------");
                if (chessboard.stateStack.ElementAt(0).OwnKingInCheck) {
                    bugged = true;
                    break;
                }
            }
        }
        catch (Exception e) {
            Logger.Log(Channel.Debug, e);
        }
        Assert.True(bugged);
    }
}
