using Bitboard = ulong;

namespace Finder {
    static class BishopFinder {
        public static Bitboard bmask(int sq) {
            Bitboard result = 0UL;
            int rk = sq / 8, fl = sq % 8, r, f;
            for (r = rk + 1, f = fl + 1; r <= 6 && f <= 6; r++, f++) result |= (1UL << (f + r * 8));
            for (r = rk + 1, f = fl - 1; r <= 6 && f >= 1; r++, f--) result |= (1UL << (f + r * 8));
            for (r = rk - 1, f = fl + 1; r >= 1 && f <= 6; r--, f++) result |= (1UL << (f + r * 8));
            for (r = rk - 1, f = fl - 1; r >= 1 && f >= 1; r--, f--) result |= (1UL << (f + r * 8));
            return result;
        }

        public static Bitboard Batt(int square, Bitboard block) {
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

        public static Bitboard FindMagic(int sq, int relevantBits) {
            Bitboard mask, magic;
            Bitboard[] a = new Bitboard[4096];
            Bitboard[] b = new Bitboard[4096];
            Bitboard[] used = new Bitboard[4096];
            int i, j, k;
            bool fail;

            mask = bmask(sq);

            for (i = 0; i < (1 << relevantBits); i++) {
                b[i] = BitOperations.IndexToBitboard(i, relevantBits, mask);
                a[i] = Batt(sq, b[i]);
            }
            for (k = 0; k < 100000000; k++) {
                magic = BitOperations.random_Bitboard_fewbits();
                if (BitOperations.CountBits((mask * magic) & 0xFF00000000000000UL) < 6) continue;
                for (i = 0; i < 4096; i++) used[i] = 0UL;
                for (i = 0, fail = false; !fail && i < (1 << relevantBits); i++) {
                    j = BitOperations.Transform(b[i], magic, relevantBits);
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