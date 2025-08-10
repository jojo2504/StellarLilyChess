using ChessEngine.Utils.Logging;

namespace ChessEngine {
    class Program {
        static void Main(string[] args) {
            try {
                var protocol = Console.ReadLine();
                Logger.Log([Channel.General, Channel.Game], protocol);

                if (protocol != "uci") {
                    Logger.Log([Channel.General, Channel.Game], $"Using something else than uci protocol: {protocol}");
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
                Logger.Log([Channel.General, Channel.Game], e);
            }
        }
    }
}