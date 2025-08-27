using System.Diagnostics;
using System.IO.Pipelines;
using ChessEngine.Evaluation;
using ChessEngine.Pieces;
using ChessEngine.Utils;
using ChessEngine.Utils.Logging;

namespace ChessEngine {
    class GameManager {
        public Chessboard chessboard = new();
        public DrawDetector drawDetector = new();
        public GameResult gameResult = new();
        string command;
        string[] remaining;

        public GameManager() {
        }

        public void StartUCIGame() {
            Logger.Log(Channel.Game, "starting game");

            Span<Move> allLegalMoves = stackalloc Move[256];
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


                    //allLegalMoves.Clear();
                    //Console.WriteLine($"bestmove {move.ToString().ToLower()}");
                }
            }
        }

        public void StartSelfGame() {
            while (!chessboard.State.Checkmated && !chessboard.State.Stalemated && !drawDetector.IsGameDraw(chessboard)) {
                gameResult.PositionList.Add(chessboard.Position);

                Span<Move> allLegalMoves = stackalloc Move[218];
                int n_moves = chessboard.GenerateLegalMoves(allLegalMoves);
                if (n_moves == 0) {
                    if (chessboard.IsInCheck(chessboard.State.TurnColor)) {
                        chessboard.State.Checkmated = true;
                    }
                    else {
                        chessboard.State.Stalemated = true;
                    }
                    break;
                }

                Random rand = new Random();
                int moveIndex = rand.Next(n_moves);
                Move.MakeMove(chessboard, allLegalMoves[moveIndex]);
            }

            if (chessboard.State.Checkmated) {
                gameResult.result = chessboard.State.TurnColor == TurnColor.White ? 0.0f : 1.0f;
            }
            else {
                gameResult.result = 0.5f;
            }
        }
    }
}