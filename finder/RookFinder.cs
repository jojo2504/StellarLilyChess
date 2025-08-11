using System.Runtime.CompilerServices;
using Bitboard = ulong;

namespace Finder {
    static class RookFinder {
        public static Bitboard rmask(int sq) {
            Bitboard result = 0UL;
            int rk = sq / 8, fl = sq % 8, r, f;
            for (r = rk + 1; r <= 6; r++) result |= (1UL << (fl + r * 8));
            for (r = rk - 1; r >= 1; r--) result |= (1UL << (fl + r * 8));
            for (f = fl + 1; f <= 6; f++) result |= (1UL << (f + rk * 8));
            for (f = fl - 1; f >= 1; f--) result |= (1UL << (f + rk * 8));
            return result;
        }

        public static Bitboard Ratt(int square, Bitboard block) {
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

        public static Bitboard FindMagic(int sq, int relevantBitsNumber) {
            Bitboard mask, magic;
            Bitboard[] a = new Bitboard[4096];
            Bitboard[] b = new Bitboard[4096];
            Bitboard[] used = new Bitboard[4096];
            int i, j, k;
            bool fail;

            mask = rmask(sq);

            for (i = 0; i < (1 << relevantBitsNumber); i++) {
                b[i] = BitOperations.IndexToBitboard(i, relevantBitsNumber, mask);
                a[i] = Ratt(sq, b[i]);
            }

            for (k = 0; k < 100000000; k++) {
                magic = BitOperations.random_Bitboard_fewbits();
                if (BitOperations.CountBits((mask * magic) & 0xFF00000000000000UL) < 6) continue;
                for (i = 0; i < 4096; i++) used[i] = 0UL;
                for (i = 0, fail = false; !fail && i < (1 << relevantBitsNumber); i++) {
                    j = BitOperations.Transform(b[i], magic, relevantBitsNumber);
                    if (used[j] == 0UL) used[j] = a[i];
                    else if (used[j] != a[i]) fail = true;
                }
                if (!fail) return magic;
            }
            Console.WriteLine("***Failed***\n");
            return 0UL;
        }
    }
}
