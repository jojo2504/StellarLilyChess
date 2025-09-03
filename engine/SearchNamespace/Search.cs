using System.Runtime.CompilerServices;
using ChessEngine.Evaluation;
using static ChessEngine.Move;

namespace ChessEngine.SearchNamespace {
    public enum NodeType {
        Exact,
        LowerBound,
        UpperBound
    }

    public static class Search {
        static readonly TranspositionTable tt = new();


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Move BestMove(Chessboard chessboard) {
            //Console.WriteLine("searching best move");
            (int, Move?) result = AlphaBetaNegaMax(chessboard, depth: 5, alpha: int.MinValue, beta: int.MaxValue);
            if (result.Item2 == null) {
                throw new Exception("BestMove returned null move");
            }   
            return result.Item2.Value;
        }

        static (int, Move?) AlphaBetaNegaMax(Chessboard chessboard, int depth, int alpha, int beta) {
            if (depth == 0) {
                ////var exactScore = Quiesce(chessboard, alpha, beta);
                //return (Quiesce(chessboard, alpha, beta), null);
                var exactScore = NNUE.Network.Evaluate(chessboard);
                //Logger.Log(Channel.Debug, $"{chessboard} NNUE eval: {exactScore}");
                return (exactScore, null);
            }

            int originalAlpha = alpha;
            Move? bestMove = null;
            int bestValue = int.MinValue;

            Span<Move> legalMoves = stackalloc Move[256];
            int n_moves = chessboard.GenerateLegalMoves(legalMoves);

            legalMoves = MoveOrdering(chessboard, legalMoves[..n_moves]).AsSpan();
            //if (n_moves > legalMoves.Length) throw new Exception("AlphaBetaNegaMax Move count exceeds buffer size");
            //Console.WriteLine(n_moves);
            for (int i = 0; i < n_moves; i++) {
                MakeMove(chessboard, legalMoves[i]);
                int score = -AlphaBetaNegaMax(chessboard, depth - 1, -beta, -alpha).Item1;
                UnmakeMove(chessboard, legalMoves[i]);

                if (score > bestValue) {
                    bestValue = score;
                    bestMove = legalMoves[i]; // alpha acts like max in MiniMax
                    if (score > alpha)
                        alpha = score;
                }

                if (score >= beta) { // fail high 
                    tt.Store(chessboard.stateStack[chessboard.plyIndex].ZobristHashKey, (byte)depth, (short)beta, legalMoves[i], NodeType.LowerBound);
                    return (score, legalMoves[i]);   //  fail soft beta-cutoff, existing the loop here is also fine
                }

                // alpha beta prunning when we already found a solution that is at least as good as the current one
                // those branches won't be able to influence the final decision so we don't need to waste time analyzing them
                //if (alpha >= beta)
                //    break;
                // After searching ALL moves
                if (bestMove == null) {
                    throw new Exception("bestMove is null after searching all moves");
                }

                if (alpha == originalAlpha) {
                    tt.Store(chessboard.stateStack[chessboard.plyIndex].ZobristHashKey, (byte)depth, (short)bestValue, (Move)bestMove, NodeType.UpperBound);
                }
                else {
                    tt.Store(chessboard.stateStack[chessboard.plyIndex].ZobristHashKey, (byte)depth, (short)bestValue, (Move)bestMove, NodeType.Exact);
                }
            }
            
            if (n_moves == 0) {
                // No legal moves available
                if (chessboard.IsInCheck(chessboard.State.TurnColor)) {
                    // Checkmate
                    return (-99999 - depth, null); // Depth is subtracted to prefer faster checkmates
                }
                else {
                    // Stalemate
                    return (0, null);
                }
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

        static Move[] MoveOrdering(Chessboard chessboard, Span<Move> LegalMoves) {
            List<Move> PV = new(); // principal variation moves
            List<Move> TT = new(); // transposition table moves
            List<Move> WCP = new(); // winning captures
            List<Move> ECP = new(); // equal captures
            List<Move> KM = new(); // killer moves
            List<Move> NC = new(); // non-captures
            List<Move> LC = new(); // losing captures

            foreach (var move in LegalMoves) {
                MakeMove(chessboard, move);
                if (tt.table.ContainsKey(chessboard.stateStack[chessboard.plyIndex].ZobristHashKey)) {
                    TT.Add(move);
                }
                else if (move.CAPTURE_FLAG) {
                    ECP.Add(move);
                }
                else {
                    NC.Add(move);
                }
                UnmakeMove(chessboard, move);
            }
            
            return PV.Concat(TT).Concat(WCP).Concat(ECP).Concat(KM).Concat(NC).Concat(LC).ToArray();
        }
    }
}