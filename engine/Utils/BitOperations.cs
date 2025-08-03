using Bitboard = ulong;
using System.Numerics;
using ChessEngine.Utils.Logging;

namespace ChessEngine.Utils {
    public static class BitOperations {
        static readonly Random random = new();
        readonly static Bitboard[] indexedBitboard = new Bitboard[64];
        public static Bitboard[] IndexedBitboard => indexedBitboard;

        static BitOperations() {
            InitializeIndexedBitboard();
        }

        static void InitializeIndexedBitboard() {
            for (int i = 0; i < 64; i++) {
                indexedBitboard[i] = 1UL << i;
            }
        }

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

        public static int RightestBitPosition(Bitboard bitboard) {
            return System.Numerics.BitOperations.TrailingZeroCount(bitboard);
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

        public static List<Square> GetSquaresFromBits(Bitboard bitboard) {
            List<Square> result = new();

            int index = 0;
            while (bitboard != 0) {
                if ((bitboard & 1) == 1)
                    result.Add((Square)index);
                bitboard >>= 1;
                index += 1;
            }

            return result;
        }

        public static Bitboard ToBitboard(Square square) {
            return indexedBitboard[(int)square];
        }

        public static Bitboard ToBitboard(Square? square) {
            if (square is not null) {
                return indexedBitboard[(int)square];
            }
            return 0UL;
        }

        public static Square ToSquare(Bitboard bitboard) {
            return (Square)System.Numerics.BitOperations.TrailingZeroCount(bitboard);
        }
    }
}