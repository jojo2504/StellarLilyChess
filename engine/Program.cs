global using Bitboard = ulong;
global using ChessEngine.Utils.Logging;

using ChessEngine.Evaluation;
using ChessEngine.SearchNamespace;

namespace ChessEngine {
    class Program {
        static void Main(string[] args) {
            // dotnet-trace entry
            /*if (args.Length == 0) {
                Negamax negamax = new(new Chessboard());
                Console.WriteLine(negamax.NegaMax(5));
                //Console.WriteLine("Perft completed for depth 5");
            }*/
            if (args.Length == 0) {
                //Chessboard chessboard = new();
                //chessboard.Perft(4);
                GameManager gameManager = new();
                gameManager.StartSelfGame();
                Console.WriteLine("game finished");
            }

            // This is the entry point of the application for the perftree tests 
            else if (args.Length > 0 && args.Length < 3) {
                var depth = int.Parse(args[0]);
                var fen = args[1];
                //var moves = ulong.Parse(args[2]); // Ignore this value, it's not used in the current context

                Chessboard chessboard = new(fen);
                chessboard.Perftree(depth);
            }
            // This is the entry point of the application for the UCI protocol.
            else {
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
                    gameManager.StartUCIGame();
                }
                catch (Exception e) {
                    Logger.Log([Channel.General, Channel.Game], e);
                }
            }
        }
    }
}