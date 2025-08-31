using System.Runtime.InteropServices;
using System.Text;
using ChessEngine.Utils;
using ChessEngine.Utils.Logging;

namespace ChessEngine.Evaluation {
    public record struct GameResult {
        public float result; // 1.0 = White wins, 0.5 = Draw, 0.0 = Black wins
    }

    public static class NNUE {
        static Network network;
        static public Network Network { get { return network; } set { network = value; } }

        static NNUE() {
            Network = Network.LoadFromBinary("/home/jojo/Documents/c#/StellarLilyChess/engine/Evaluation/TrainingData/quantised.bin");
        }        
    }
}