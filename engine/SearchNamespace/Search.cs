using System.Runtime.CompilerServices;
using ChessEngine.Evaluation;
using static ChessEngine.Move;

namespace ChessEngine.SearchNamespace {
    internal enum NodeType {
        Exact,
        LowerBound,
        UpperBound
    }

    internal struct Line {
        internal int cmove;              // Number of moves in the line.
        internal Move[] argmove;  // The line.

        public Line(int maxMoves) {
            argmove = new Move[maxMoves];
        }
    }

    public static class Search {
        static readonly TranspositionTable tt = new();
        const int SearchDepth = 6;
        static Line PV;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public Move? BestMove(Chessboard chessboard) {
            //Console.WriteLine("searching best move");
            Line line = new(SearchDepth);
            (int, Move?, Line?) result = AlphaBetaNegamax(chessboard, depth: SearchDepth, alpha: int.MinValue, beta: int.MaxValue, pline: ref line);

            if (result.Item2 == null) {
                //throw new Exception("BestMove returned null move, end of the game by checkmate or statemate");
                return null;
            }

            // print PV
            Move move = (Move)result.Item2;
            Logger.Log(Channel.Debug, $"Best Move: {move} Score: {result.Item1}");
            Console.WriteLine($"Best Move: {move} Score: {result.Item1}");
            PV = line;
            Logger.Log(Channel.Debug, $"PV : {string.Join(", ", PV.argmove.Take(PV.cmove))}");
            Console.WriteLine($"PV : {string.Join(", ", PV.argmove.Take(PV.cmove))}");

            return result.Item2.Value;
        }

        internal static (int, Move?, Line?) AlphaBetaNegamax(Chessboard chessboard, int depth, long alpha, long beta, ref Line pline) {
            Logger.Log(Channel.Debug, $"alpha: {alpha}, beta: {beta}");

            if (depth == 0) {
                pline.cmove = 0;
                ////var exactScore = Quiesce(chessboard, alpha, beta);
                //return (Quiesce(chessboard, alpha, beta), null);
                var exactScore = NNUE.Network.Evaluate(chessboard);
                Logger.Log(Channel.Debug, $"{chessboard} NNUE eval: {exactScore}");
                return (exactScore, null, pline);
            }

            long originalAlpha = alpha;
            int bestValue = int.MinValue;
            Line line = new(SearchDepth);

            Span<Move> legalMoves = stackalloc Move[256];
            int n_moves = chessboard.GenerateLegalMoves(legalMoves);

            if (n_moves == 0) {
                // No legal moves available
                if (chessboard.IsInCheck(chessboard.State.TurnColor)) {
                    // Checkmate
                    return (-99999 - depth, null, pline); // Depth is subtracted to prefer faster checkmates
                }
                else {
                    // Stalemate
                    return (0, null, pline);
                }
            }

            Move? bestMove = legalMoves[0];
            //Logger.Log(Channel.Debug, $"{n_moves} available moves to play in the tree");
            for (int i = 0; i < n_moves; i++) {
                MakeMove(chessboard, legalMoves[i]);
                int score = -AlphaBetaNegamax(chessboard, depth - 1, -beta, -alpha, ref line).Item1;
                UnmakeMove(chessboard, legalMoves[i]);

                if (score >= beta) { // fail high 
                    //Logger.Log(Channel.Debug, $"fail high, score: {score}, beta: {beta}");
                    tt.Store(chessboard.stateStack[chessboard.plyIndex].ZobristHashKey, (byte)depth, (short)beta, legalMoves[i], NodeType.LowerBound);

                    pline.argmove[0] = legalMoves[i];
                    Array.Copy(line.argmove, 0, pline.argmove, 1, line.cmove);
                    pline.cmove = line.cmove + 1;

                    return (score, legalMoves[i], pline);   //  fail soft beta-cutoff, existing the loop here is also fine
                }

                if (score > bestValue) {
                    //Logger.Log(Channel.Debug, $"score > bestValue, score: {score}, bestValue: {bestValue}");

                    bestValue = score;
                    bestMove = legalMoves[i]; // alpha acts like max in MiniMax

                    pline.argmove[0] = legalMoves[i];
                    Array.Copy(line.argmove, 0, pline.argmove, 1, line.cmove);
                    pline.cmove = line.cmove + 1;

                    //Console.WriteLine($"Depth: {depth} New Best Move: {bestMove} Score: {bestValue} PV: {string.Join(", ", pline.argmove.Take(pline.cmove))}");

                    if (score > alpha) {
                        //Logger.Log(Channel.Debug, $"score > alpha, score: {score}, alpha: {alpha}");
                        alpha = score;
                    }
                }
            }

            // alpha beta prunning when we already found a solution that is at least as good as the current one
            // those branches won't be able to influence the final decision so we don't need to waste time analyzing them
            if (alpha >= beta)
                throw new Exception("alpha over beta");

            // After searching ALL moves
            if (bestMove == null) {
                //bestMove = legalMoves[0];
                throw new Exception("bestMove is null after searching all moves");
            }

            if (alpha == originalAlpha) {
                tt.Store(chessboard.stateStack[chessboard.plyIndex].ZobristHashKey, (byte)depth, (short)bestValue, (Move)bestMove, NodeType.UpperBound);
            }
            else {
                tt.Store(chessboard.stateStack[chessboard.plyIndex].ZobristHashKey, (byte)depth, (short)bestValue, (Move)bestMove, NodeType.Exact);
            }

            //Console.WriteLine($"PV : {string.Join(", ", line.argmove.Take(line.cmove))}");

            return (bestValue, bestMove, pline);
        }

        internal static int Quiesce(Chessboard chessboard, int alpha, int beta) {
            int static_eval = NNUE.Network.Evaluate(chessboard);

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

        internal static Move[] MoveOrdering(Chessboard chessboard, Span<Move> LegalMoves) {
            List<Move> PV = new(); // principal variation moves
            List<Move> TT = new(); // transposition table moves
            //List<Move> WCP = new(); // winning captures
            List<Move> ECP = new(); // equal captures
            //List<Move> KM = new(); // killer moves
            List<Move> NC = new(); // non-captures
            //List<Move> LC = new(); // losing captures

            foreach (var move in LegalMoves) {
                if (PV.Count > 0 && PV[0].Equals(move)) {
                    PV.Add(move);
                    continue;
                }

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

            return PV.Concat(TT).Concat(ECP).Concat(NC).ToArray();
            //return PV.Concat(TT).Concat(WCP).Concat(ECP).Concat(KM).Concat(NC).Concat(LC).ToArray();
        }
    }
}