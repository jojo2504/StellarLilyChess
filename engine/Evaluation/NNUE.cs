using System.Runtime.InteropServices;
using System.Text;
using ChessEngine.Utils;
using ChessEngine.Utils.Logging;

namespace ChessEngine.Evaluation {
    public static class NNUE {
        static Network network;
        static public Network Network { get { return network; } set { network = value; } }

        static NNUE() {
            try {
                network = Network.LoadNetwork("/home/jojo/Documents/c#/StellarLilyChess/engine/Evaluation/TrainingData/checkpoints/simple-40/quantised.bin");
            } catch (Exception ex) {
                Logger.Error(Channel.Debug, $"Failed to load NNUE network: {ex.Message}");
                throw;
            }
        }        
    }
}