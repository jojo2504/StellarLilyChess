using Bitboard = ulong;
using System.Numerics;
using ChessEngine.Utils.Logging;
using System.Runtime.CompilerServices;

namespace ChessEngine.Utils {
    public static class BitOperations {
        static readonly Random random = new();

        static BitOperations() {
        }

        public static int CountBits(Bitboard number) {
            int count = 0;
            while (number != 0) {
                number &= number - 1; // Clear the least significant bit set
                count++;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int pop_1st_bit(ref Bitboard bitboard) {
            int pos = System.Numerics.BitOperations.TrailingZeroCount(bitboard);
            bitboard &= (bitboard - 1);  // Remove the rightmost bit
            return pos;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void del_1st_bit(ref Bitboard bitboard) {
            bitboard &= bitboard - 1;  // Remove the rightmost bit
        }

        public static Bitboard IndexToBitboard(int index, int bits, Bitboard m) {
            int i, j;
            Bitboard result = 0UL;
            for (i = 0; i < bits; i++) {
                j = pop_1st_bit(ref m);
                if ((index & (1 << i)) != 0) result |= (1UL << j);
            }
            return result;
        }

        public static List<Square> GetSquaresFromBits(Bitboard bitboard) {
            List<Square> result = new();

            while (bitboard != 0) {
                int lsbIndex = System.Numerics.BitOperations.TrailingZeroCount(bitboard);
                result.AddRange((Square)lsbIndex);
                bitboard &= bitboard - 1; // Clear the least significant bit set
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bitboard ToBitboard(int index) {
            return 1UL << index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bitboard ToBitboard(Square square) {
            return 1UL << (int)square;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bitboard ToBitboard(Square? square) {
            if (square.HasValue) {
                return 1UL << (int)square;
            }
            return 0UL;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToIndex(Bitboard bitboard) {
            return System.Numerics.BitOperations.TrailingZeroCount(bitboard);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bitboard LsbIndexBitboard(Bitboard bitboard) {
            return bitboard & (0UL - bitboard);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Square ToSquare(Bitboard bitboard) {
            return (Square)System.Numerics.BitOperations.TrailingZeroCount(bitboard);
        }
    }
}