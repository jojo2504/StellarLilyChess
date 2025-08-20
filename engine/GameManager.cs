using System.Diagnostics;
using System.IO.Pipelines;
using ChessEngine.Pieces;
using ChessEngine.Utils;
using ChessEngine.Utils.Logging;

using Bitboard = ulong;

namespace ChessEngine {
    class GameManager {
        Chessboard chessboard = new();
        string command;
        string[] remaining;

        public GameManager() {
        }

        public void StartGame() {
            Logger.Log(Channel.Game, "starting game");
            while (true) {
                var input = Console.ReadLine();
                string[] parts = input.Split(' ');
                command = parts[0];
                remaining = parts.Skip(1).ToArray();

                Logger.Log(Channel.Game, command);

                if (command == "quit") {
                    Logger.Log(Channel.Game, "quitting");
                    break;
                }

                if (command == "isready") {
                    Console.WriteLine("readyok");
                }

                // ie: position startpos moves e2e4
                else if (command == "position") {
                    // should reconstruct the whole game before continuing (optional)

                    Logger.Log(Channel.Game, $"Received {remaining[remaining.Length - 1]}");
                    chessboard.PushUci(remaining[remaining.Length - 1]);
                    Logger.Log(Channel.Game, chessboard.ToString());

                    Logger.Log(Channel.Game, "new finished (stack):");
                    Logger.Log(Channel.Game, chessboard.State);

                    Logger.Log(Channel.Game, "------------------------");
                    Logger.Log(Channel.Game, "preview of next turn state (unfinished)");
                    Logger.Log(Channel.Game, chessboard.State);
                }

                // should start with white, compute move as white, then change the turn color after the 
                else if (command == "go") {
                    List<Move> allLegalMoves = chessboard.GenerateLegalMoves();
                    int nMoves = allLegalMoves.Count;

                    /*Logger.Log(Channel.Game, $"{nMoves} possible moves");
                    foreach (Move legalMove in allLegalMoves) {
                        Logger.Log(Channel.Game, legalMove);
                    }*/

                    Random random = new();
                    int r = random.Next(nMoves);
                    Move move = allLegalMoves[r];

                    Move.MakeMove(chessboard, move);
                    //Logger.Log(Channel.Game, $"Playing {move.ToString().ToLower()}");
                    //Logger.Log(Channel.Game, chessboard.ToString());

                    //Logger.Log(Channel.Game, "new finished (stack):");
                    //Logger.Log(Channel.Game, chessboard.State);

                    //Logger.Log(Channel.Game, "-------------------------------------------------");
                    //Logger.Log(Channel.Game, "preview of next turn state (unfinished)");
                    //Logger.Log(Channel.Game, chessboard.State);

                    Console.WriteLine($"bestmove {move.ToString().ToLower()}");
                }
            }
        }
    }
}