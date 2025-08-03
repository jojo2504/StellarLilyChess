using ChessEngine.Magica;
using ChessEngine.Utils;
using Bitboard = ulong;

namespace ChessEngine.Pieces {
    public static class Bishop {
        static readonly Magic.SMagic[] BishopMagicTable = new Magic.SMagic[64];
        static readonly Bitboard[,] MagicBishopAttacks = new Bitboard[64, 512]; // 256 K

        static Bishop() {
            var json_path = "/home/jojo/Documents/c#/StellarLilyChess/engine/Resources/BMagicTable.json";
            BishopMagicTable = Magic.LoadMagicTable(options: Magic.jsonOptions, filePath: json_path);
            InitBishopAttacks();
        }

        static Bitboard bmask(int sq) {
            Bitboard result = 0UL;
            int rk = sq / 8, fl = sq % 8, r, f;
            for (r = rk + 1, f = fl + 1; r <= 6 && f <= 6; r++, f++) result |= (1UL << (f + r * 8));
            for (r = rk + 1, f = fl - 1; r <= 6 && f >= 1; r++, f--) result |= (1UL << (f + r * 8));
            for (r = rk - 1, f = fl + 1; r >= 1 && f <= 6; r--, f++) result |= (1UL << (f + r * 8));
            for (r = rk - 1, f = fl - 1; r >= 1 && f >= 1; r--, f--) result |= (1UL << (f + r * 8));
            return result;
        }

        static Bitboard Batt(int square, Bitboard block) {
            Bitboard result = 0UL;
            int rk = square / 8, fl = square % 8, r, f;
            for (r = rk + 1, f = fl + 1; r <= 7 && f <= 7; r++, f++) {
                result |= (1UL << (f + r * 8));
                if ((block & (1UL << (f + r * 8))) != 0) break;
            }
            for (r = rk + 1, f = fl - 1; r <= 7 && f >= 0; r++, f--) {
                result |= (1UL << (f + r * 8));
                if ((block & (1UL << (f + r * 8))) != 0) break;
            }
            for (r = rk - 1, f = fl + 1; r >= 0 && f <= 7; r--, f++) {
                result |= (1UL << (f + r * 8));
                if ((block & (1UL << (f + r * 8))) != 0) break;
            }
            for (r = rk - 1, f = fl - 1; r >= 0 && f >= 0; r--, f--) {
                result |= (1UL << (f + r * 8));
                if ((block & (1UL << (f + r * 8))) != 0) break;
            }
            return result;
        }

        static void InitBishopAttacks() {
            for (int sq = 0; sq < 64; sq++) {
                Bitboard mask = BishopMagicTable[sq].mask;
                int relevantBitsNumber = BitOperations.CountBits(mask);

                for (int i = 0; i < (1 << relevantBitsNumber); i++) {
                    Bitboard occupancy = BitOperations.IndexToBitboard(i, relevantBitsNumber, mask);
                    Bitboard attacks = Batt(sq, occupancy);

                    // Transform occupancy to magic index
                    Bitboard maskedOcc = occupancy & mask;
                    int magicIndex = BitOperations.Transform(maskedOcc, BishopMagicTable[sq].magicNumber, 9);

                    // Store the attacks in your lookup table
                    MagicBishopAttacks[sq, magicIndex] = attacks;
                }
            }
        }

        public static Bitboard ComputePossibleMoves(Square square, Chessboard chessboard, TurnColor? turnColor = null) {
            var sq = (int)square;
            var occ = chessboard.AllPieces;

            occ &= BishopMagicTable[sq].mask;
            occ *= BishopMagicTable[sq].magicNumber;
            occ >>= 55; //64-9

            var ownSide = ((turnColor ?? chessboard.State.TurnColor) == TurnColor.White) ? chessboard.AllWhitePieces : chessboard.AllBlackPieces;
            return MagicBishopAttacks[sq, occ] & ~ownSide;
        }
    }
}