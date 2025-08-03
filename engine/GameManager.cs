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
        string specType;
        string movesLabel;
        string[] remaining;
        string[] moveList;

        public GameManager() {
        }

        public void StartGame() {
            Logger.Log("starting game");
            while (true) {
                var input = Console.ReadLine();
                string[] parts = input.Split(' ');
                command = parts[0];
                remaining = parts.Skip(1).ToArray();

                Logger.Log(command);

                if (command == "quit") {
                    Logger.Log("quitting");
                    break;
                }

                if (command == "isready") {
                    Console.WriteLine("readyok");
                }

                // ie: position startpos moves e2e4
                else if (command == "position") {
                    chessboard.State.TurnColor = (TurnColor)(((int)chessboard.State.TurnColor)^1);
                    
                    Logger.Log($"Received {remaining[remaining.Length - 1]}");
                    chessboard.PushUci(remaining[remaining.Length - 1]);

                    Logger.Log("new state:");
                    Logger.Log(chessboard.State);

                    Logger.Log(chessboard.ToString());
                }

                else if (command == "go") {
                    chessboard.State.TurnColor = (TurnColor)(((int)chessboard.State.TurnColor)^1);
                    
                    List<Move> allLegalMoves = chessboard.GenerateLegalMoves();
                    int nMoves = allLegalMoves.Count;

                    Logger.Log($"{nMoves} possible moves");
                    /*foreach (Move legalMove in allLegalMoves) {
                        Logger.Log(legalMove);
                    }*/

                    Random random = new();
                    int r = random.Next(nMoves);
                    Move move = allLegalMoves[r];

                    Move.MakeMove(chessboard, move, displayComputationLogs: true);
                    Logger.Log($"Playing {move.ToString().ToLower()}");
                    Logger.Log(chessboard.ToString());

                    Logger.Log("new state:");
                    Logger.Log(chessboard.State);

                    Console.WriteLine($"bestmove {move.ToString().ToLower()}");
                }
            }
        }
    }
}