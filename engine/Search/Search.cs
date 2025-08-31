using System.Runtime.CompilerServices;
using ChessEngine.Evaluation;
using static ChessEngine.Move;

namespace ChessEngine.SearchNamespace {
    public static class Search {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Move BestMove(Chessboard chessboard) {
            //Console.WriteLine("searching best move");
            return (Move)AlphaBetaNegaMax(chessboard, depth: 5, alpha: int.MinValue, beta: int.MaxValue).Item2;
        }

        static (int, Move?) AlphaBetaNegaMax(Chessboard chessboard, int depth, int alpha, int beta) {
            if (depth == 0) {
                return (Quiesce(chessboard, alpha, beta), null);
            }

            Move? bestMove = null;
            int bestValue = int.MinValue;
            Span<Move> legalSpeudoLegalMoves = stackalloc Move[256];
            int n_moves = chessboard.GenerateMoves(legalSpeudoLegalMoves);
            if (n_moves > legalSpeudoLegalMoves.Length) throw new Exception("AlphaBetaNegaMax Move count exceeds buffer size");
            //Console.WriteLine(n_moves);

            for (int i = 0; i < n_moves; i++) {
                Logger.Log(Channel.Debug, $"searching move {i + 1}/{n_moves} at depth {-depth+4}: {legalSpeudoLegalMoves[i]} {chessboard.stateStack[^1].TurnColor} {legalSpeudoLegalMoves[i].pieceType}");
                MakeMove(chessboard, legalSpeudoLegalMoves[i]);
                Logger.Log(Channel.Debug, $"after move: {chessboard.State}");
                Logger.Log(Channel.Debug, chessboard);
                if (chessboard.IsInCheck(chessboard.stateStack[chessboard.plyIndex].TurnColor)) {
                    UnmakeMove(chessboard, legalSpeudoLegalMoves[i]);
                    continue; // skip illegal moves that leave king in check
                }
                int score = -AlphaBetaNegaMax(chessboard, depth - 1, -beta, -alpha).Item1;
                UnmakeMove(chessboard, legalSpeudoLegalMoves[i]);

                if (score >= beta) {
                    return (score, legalSpeudoLegalMoves[i]);   //  fail soft beta-cutoff, existing the loop here is also fine
                }
                if (score > bestValue) {
                    bestValue = score;
                    bestMove = legalSpeudoLegalMoves[i];
                }
                alpha = Math.Max(alpha, score);

                // alpha beta prunning when we already found a solution that is at least as good as the current one
                // those branches won't be able to influence the final decision so we don't need to waste time analyzing them
                if (alpha >= beta)
                    break;
            }

            return (bestValue, bestMove);
        }

        static int Quiesce(Chessboard chessboard, int alpha, int beta) {
            int static_eval = (int)NNUE.Network.Evaluate(chessboard);

            // Stand Pat
            int best_value = static_eval;
            if (best_value >= beta)
                return best_value;
            if (best_value > alpha)
                alpha = best_value;

            Span<Move> allLegalCaptureMoves = stackalloc Move[218];
            int n_moves = chessboard.GenerateLegalCaptureMoves(allLegalCaptureMoves);
            if (n_moves > allLegalCaptureMoves.Length) throw new Exception("Quiesce Move count exceeds buffer size");

            for (int i = 0; i < n_moves; i++) {
                MakeMove(chessboard, allLegalCaptureMoves[i]);
                int score = -Quiesce(chessboard, -beta, -alpha);
                UnmakeMove(chessboard, allLegalCaptureMoves[i]);

                if (score >= beta)
                    return score;
                if (score > best_value)
                    best_value = score;
                if (score > alpha)
                    alpha = score;
            }

            return best_value;
        }
    }
}