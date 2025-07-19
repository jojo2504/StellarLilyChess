using Bitboard = ulong;
using System.Numerics;

namespace Finder {
    public static class BitOperations {
        static Random random = new();

        public static int CountBits(Bitboard number) {
            int count = 0;
            while (number != 0) {
                if ((number & 1) == 1)
                    count++;
                number >>= 1;
            }
            return count;
        }

        public static Bitboard random_Bitboard() {
            Bitboard u1, u2, u3, u4;
            u1 = (Bitboard)(random.Next()) & 0xFFFF; u2 = (Bitboard)(random.Next()) & 0xFFFF;
            u3 = (Bitboard)(random.Next()) & 0xFFFF; u4 = (Bitboard)(random.Next()) & 0xFFFF;
            return u1 | (u2 << 16) | (u3 << 32) | (u4 << 48);
        }

        public static Bitboard random_Bitboard_fewbits() {
            return random_Bitboard() & random_Bitboard() & random_Bitboard();
        }

        public static int Transform(Bitboard bitboard, Bitboard magic, int bits) {
            return (int)((bitboard * magic) >> (64 - bits));
        }

        public static int pop_1st_bit(ref Bitboard bitboard) {
            int pos = System.Numerics.BitOperations.TrailingZeroCount(bitboard);
            bitboard &= (bitboard - 1);  // Remove the rightmost bit
            return pos;
        }

        public static Bitboard IndexToBitboard(int index, int bits, Bitboard m) {
            int i, j;
            Bitboard result = 0UL;
            for(i = 0; i < bits; i++) {
                j = pop_1st_bit(ref m);
                if((index & (1 << i)) != 0) result |= (1UL << j);
            }
            return result;
        }
    }
}