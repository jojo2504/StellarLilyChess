using System.Diagnostics;
using System.IO.Pipelines;
using ChessEngine.Evaluation;
using ChessEngine.Pieces;
using ChessEngine.Records;
using ChessEngine.Utils;
using ChessEngine.Utils.Logging;
using ChessEngine.SearchNamespace;

namespace ChessEngine {
    internal record struct GameResult {
        public float result; // 1.0 = white win, 0.5 = draw, 0.0 = black win
    }

    class GameManager {
        public Chessboard chessboard;
        public GameResult gameResult = new();
        public GameRecord gameRecord = new();
        string command;
        string[] remaining;

        public GameManager(string fen) {
            chessboard = new(fen);
        }

        public GameManager() {
            chessboard = new();
        }

        public void PushUci(string move) {
            Move.MakeMove(chessboard, Move.DecodeUciMove(chessboard, move));
            Logger.Log(Channel.Debug, chessboard);
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
                    PushUci(remaining[remaining.Length - 1]);
                    Logger.Log(Channel.Game, chessboard.ToString());

                    Logger.Log(Channel.Game, "new finished (stack):");
                    Logger.Log(Channel.Game, chessboard.State);

                    Logger.Log(Channel.Game, "------------------------");
                    Logger.Log(Channel.Game, "preview of next turn state (unfinished)");
                    Logger.Log(Channel.Game, chessboard.State);
                }

                // should start with white, compute move as white, then change the turn color after the 
                else if (command == "go") {
                    Move? bestMove = Search.BestMove(chessboard);
                    Move.MakeMove(chessboard, (Move)bestMove);
                    gameRecord.AddMove((Move)bestMove);
                    Console.WriteLine($"bestmove {bestMove.ToString().ToLower()}");
                }
            }

            if (chessboard.State.Checkmated) {
                gameResult.result = chessboard.State.TurnColor == TurnColor.White ? 0.0f : 1.0f;
            }
            else {
                gameResult.result = 0.5f;
            }

            Console.WriteLine(gameRecord.ConvertToPGN());
        }

        public void StartSelfGame() {
            Console.WriteLine("starting game against self");
            //while (!chessboard.State.Checkmated && !chessboard.State.Stalemated && !DrawDetector.IsGameDraw(chessboard)) {
            for (int turn = 0; turn < 20; turn++) {
                Move? bestMove = Search.BestMove(chessboard);
                if (bestMove is null) {
                    if (chessboard.State.Checkmated) {
                        gameResult.result = chessboard.State.TurnColor == TurnColor.White ? 0.0f : 1.0f;
                    }
                    else {
                        gameResult.result = 0.5f;
                    }
                    break;
                }
                Move.MakeMove(chessboard, (Move)bestMove);
                Console.WriteLine($"{turn} {chessboard.stateStack[chessboard.plyIndex].TurnColor} played {(Move)bestMove}");
                gameRecord.AddMove((Move)bestMove);
            }

            Console.WriteLine(gameRecord.ConvertToPGN());
        }

        public void StartSelfUCIGame() {
            Console.WriteLine("starting game against self UCI");
            //while (!chessboard.State.Checkmated && !chessboard.State.Stalemated && !DrawDetector.IsGameDraw(chessboard)) {
            for (int turn = 0; turn < 100; turn++) {
                Move? bestMove = Search.BestMove(chessboard);
                if (bestMove is null) {
                    if (chessboard.State.Checkmated) {
                        gameResult.result = chessboard.State.TurnColor == TurnColor.White ? 0.0f : 1.0f;
                    }
                    else {
                        gameResult.result = 0.5f;
                    }
                    break;
                }
                PushUci(bestMove.ToString().ToLower());
                Console.WriteLine($"{turn} played {bestMove}");
                gameRecord.AddMove((Move)bestMove);
            }

            Console.WriteLine(gameRecord.ConvertToPGN());
        }

        public void AccumulatorDebug() {
            Console.WriteLine("Accumulator debug:");
            Console.WriteLine(NNUE.Network.Evaluate(chessboard));
        }

        public void PVLine() {
            Search.BestMove(chessboard);
        }
    }
}