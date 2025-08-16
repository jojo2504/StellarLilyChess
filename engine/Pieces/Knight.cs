using System.Runtime.CompilerServices;
using ChessEngine.Utils;
using ChessEngine.Utils.Logging;
using Bitboard = ulong;

namespace ChessEngine.Pieces {
    public static class Knight {
        public static Bitboard[] KnightAttackMasks = new Bitboard[64];

        static Knight() {
            InitKnightAttacks();
        }

        static void InitKnightAttacks() {
            for (int i = 0; i < 64; i++) {
                var knightLocation = 1UL << i;
                Bitboard spot_1_clip = LookupTables.GetFileClear(File.FILE_A) & LookupTables.GetFileClear(File.FILE_B);
                Bitboard spot_2_clip = LookupTables.GetFileClear(File.FILE_A);
                Bitboard spot_3_clip = LookupTables.GetFileClear(File.FILE_H);
                Bitboard spot_4_clip = LookupTables.GetFileClear(File.FILE_H) & LookupTables.GetFileClear(File.FILE_G);

                Bitboard spot_5_clip = LookupTables.GetFileClear(File.FILE_H) & LookupTables.GetFileClear(File.FILE_G);
                Bitboard spot_6_clip = LookupTables.GetFileClear(File.FILE_H);
                Bitboard spot_7_clip = LookupTables.GetFileClear(File.FILE_A);
                Bitboard spot_8_clip = LookupTables.GetFileClear(File.FILE_A) & LookupTables.GetFileClear(File.FILE_B);

                /* The clipping masks we just created will be used to ensure that no
                    under or overflow positions are computed when calculating the
                    possible moves of the knight in certain files. */

                Bitboard spot_1 = (knightLocation & spot_1_clip) << 6;
                Bitboard spot_2 = (knightLocation & spot_2_clip) << 15;
                Bitboard spot_3 = (knightLocation & spot_3_clip) << 17;
                Bitboard spot_4 = (knightLocation & spot_4_clip) << 10;

                Bitboard spot_5 = (knightLocation & spot_5_clip) >> 6;
                Bitboard spot_6 = (knightLocation & spot_6_clip) >> 15;
                Bitboard spot_7 = (knightLocation & spot_7_clip) >> 17;
                Bitboard spot_8 = (knightLocation & spot_8_clip) >> 10;

                KnightAttackMasks[i] = spot_1 | spot_2 | spot_3 | spot_4 | spot_5 | spot_6 | spot_7 | spot_8;
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bitboard ComputePossibleMoves(Bitboard knightLocation, Chessboard chessboard, TurnColor? turnColor = null) {
            var ownSide = ((turnColor ?? chessboard.State.TurnColor) == TurnColor.White) ? chessboard.AllWhitePieces : chessboard.AllBlackPieces;
            return KnightAttackMasks[BitOperations.ToIndex(knightLocation)] & ~ownSide;
        }
    }
}