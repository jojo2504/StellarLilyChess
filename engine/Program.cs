using ChessEngine.Utils.Logging;

namespace ChessEngine {
    class Program {
        static void Main(string[] args) {
            try {
                var protocol = Console.ReadLine();
                Logger.Log(protocol);

                if (protocol == "perft") {
                    for (int depth = 0; depth <= 5; depth++) {
                        Chessboard chessboard = new();
                        Logger.Log($"perft({depth}): {chessboard.Perft(depth)}");
                    }
                    return;
                }
                if (protocol != "uci") {
                    Logger.Log($"Using something else than uci protocol: {protocol}");
                    return;
                }

                Console.WriteLine("id name StellarLilyChess");
                Console.WriteLine("id author Jojo");
                Console.WriteLine("option name Move Overhead type spin default 30 min 0 max 5000");
                Console.WriteLine("option name Threads type spin default 4 min 1 max 12");
                Console.WriteLine("option name Hash type spin default 512");
                Console.WriteLine("option name SyzygyPath type string default './syzygy/'");
                Console.WriteLine("option name UCI_ShowWDL type check default true");
                Console.WriteLine("uciok");

                GameManager gameManager = new GameManager();
                gameManager.StartGame();
            }
            catch (Exception e) {
                Logger.Log(e);
            }
        }
    }
}