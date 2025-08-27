using System.Text;
using ChessEngine.Utils.Logging;

namespace ChessEngine.Utils {
    public static class StringHelper {
        public static string ToBinary(Bitboard number) {
            string binary = "";
            while (number > 0) {
                binary = (number % 2) + binary;
                number /= 2;
            }
            return binary.PadLeft(64, '0');
        }

        public static string MergeStrings(string str1, string str2) {
            int length = Math.Min(str1.Length, str2.Length);
            Span<char> result = stackalloc char[length];

            for (int i = 0; i < length; i++) {
                result[i] = str2[i] != '0' ? str2[i] : str1[i];
            }

            return new string(result);
        }

        public static string FormatAsChessboard(string ChessboardAsBinaryString) {
            var result = new StringBuilder();
            for (int i = 0; i < ChessboardAsBinaryString.Length; i++) {
                if (i % 8 == 0) {
                    result.AppendLine();
                }
                // Within each rank, reverse the bit order
                int rankStart = i / 8 * 8;
                int linePosition = i % 8;
                int reversedIndex = rankStart + (7 - linePosition);

                result.Append(ChessboardAsBinaryString[reversedIndex]);
            }
            return result.ToString();
        }

        public static string FormatAsChessboard(Bitboard bitboard) {
            return FormatAsChessboard(ToBinary(bitboard));
        }
    }
}