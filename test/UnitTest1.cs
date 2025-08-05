using ChessEngine;
using ChessEngine.Utils.Logging;

namespace test;

public class UnitTest1 {
    [Fact]
    public void TestGame1() {
        var gameRecord = System.IO.File.ReadLines("C:/Users/Jojo/Documents/c#/StellarLilyChess/engine/records/gamebugged.txt");
        var bugged = false;
        // iterate through each element within the array and
        // print it out
        //
        try {
            Chessboard chessboard = new();
            foreach (var move in gameRecord) {
                chessboard.PushUci(move);
                Logger.Log(chessboard);
                Logger.Log(chessboard.stateStack.ElementAt(0));
                Logger.Log("--------------------------------------");
                if (chessboard.stateStack.ElementAt(0).OwnKingInCheck) {
                    bugged = true;
                    break;
                }
            }
        }
        catch (Exception e) {
            Logger.Log(e);
        }
        Assert.True(bugged);
    }
}
