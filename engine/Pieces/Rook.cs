using ChessEngine.Magica;
using ChessEngine.Utils;
using Bitboard = ulong;

namespace ChessEngine.Pieces {
    public static class Rook {
        static readonly Bitboard[] rookBlockerMask = new Bitboard[64];
        static readonly Magic.SMagic[] RookMagicTable = new Magic.SMagic[64];
        static readonly Bitboard[,] MagicRookAttacks = new Bitboard[64, 4096]; // 2048K

        static Rook() {
            for (int i = 0; i < 64; i++) {
                InitBlockerMask(i);
            }
            var json_path = @$"{AppDomain.CurrentDomain.BaseDirectory}/Resources/RMagicTable.json";
            RookMagicTable = Magic.LoadMagicTable(options: Magic.jsonOptions, filePath: json_path);
            InitRookAttacks();
        }

        static void InitBlockerMask(int i) {
            // i reprensents the current square bit we are working with
            // create the blocker mask at the ith bit

            int fileIndex = i % 8;
            int rankIndex = i / 8;

            Bitboard blockerMask = 0UL;
            blockerMask |= LookupTables.GetFileMask((File)fileIndex);
            blockerMask ^= LookupTables.GetRankMask((Rank)rankIndex);

            // remove the 4 corners
            blockerMask &= LookupTables.CornerClear;

            // checks if not on border
            if (((1UL << i) & LookupTables.AllBordersMask) != 0) {
                blockerMask &= LookupTables.AllBordersClear;
            }

            rookBlockerMask[i] = blockerMask;
        }

        static Bitboard Ratt(int square, Bitboard block) {
            Bitboard result = 0UL;
            int rk = square / 8, fl = square % 8;
            int r, f;

            for (r = rk + 1; r <= 7; r++) {
                result |= (1UL << (fl + r * 8));
                if ((block & (1UL << (fl + r * 8))) != 0) break;
            }
            for (r = rk - 1; r >= 0; r--) {
                result |= (1UL << (fl + r * 8));
                if ((block & (1UL << (fl + r * 8))) != 0) break;
            }
            for (f = fl + 1; f <= 7; f++) {
                result |= (1UL << (f + rk * 8));
                if ((block & (1UL << (f + rk * 8))) != 0) break;
            }
            for (f = fl - 1; f >= 0; f--) {
                result |= (1UL << (f + rk * 8));
                if ((block & (1UL << (f + rk * 8))) != 0) break;
            }
            return result;
        }

        static void InitRookAttacks() {
            for (int sq = 0; sq < 64; sq++) {
                Bitboard mask = RookMagicTable[sq].mask;
                int relevantBitsNumber = BitOperations.CountBits(mask);

                for (int i = 0; i < (1 << relevantBitsNumber); i++) {
                    Bitboard occupancy = BitOperations.IndexToBitboard(i, relevantBitsNumber, mask);
                    Bitboard attacks = Ratt(sq, occupancy);

                    // Transform occupancy to magic index
                    Bitboard maskedOcc = occupancy & mask;
                    int magicIndex = BitOperations.Transform(maskedOcc, RookMagicTable[sq].magicNumber, 12);

                    // Store the attacks in your lookup table
                    MagicRookAttacks[sq, magicIndex] = attacks;
                }
            }
        }

        public static Bitboard ComputePossibleMoves(Bitboard rookLocation, Chessboard chessboard, TurnColor? turnColor = null) {
            var sq = BitOperations.ToIndex(rookLocation);
            var occ = chessboard.AllPieces;

            occ &= RookMagicTable[sq].mask;
            occ *= RookMagicTable[sq].magicNumber;
            occ >>= 52; //64-12

            var ownSide = ((turnColor ?? chessboard.State.TurnColor) == TurnColor.White) ? chessboard.AllWhitePieces : chessboard.AllBlackPieces;
            return MagicRookAttacks[sq, occ] & ~ownSide;
        }
    }
}