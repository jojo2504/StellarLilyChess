using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ChessEngine.Utils;

namespace ChessEngine {
    public static class ZobristHashing {
        // Zobrist array
        // 1 number for each piece at each square                                                   (2 * 6 * 64)
        // 1 number to indicate the side to move is black                                           (1)
        // 4 numbers to indicate the castling rights, though usually 16 (2^4) are used for speed    (16)
        // 8 numbers to indicate the file of a valid En passant square, if any                      (8)
        // This leaves us with an array with 793 (12*64 + 1 + 16 + 8) random numbers.
        public static readonly ulong[] pieceSquare = new ulong[12 * 64]; // 1D array

        // Index | WK | WQ | BK | BQ | Binary
        // ------|----|----|----|----|-------
        // 0     | F  | F  | F  | F  | 0000
        // 1     | T  | F  | F  | F  | 0001
        // 2     | F  | T  | F  | F  | 0010
        // 3     | T  | T  | F  | F  | 0011
        // 4     | F  | F  | T  | F  | 0100
        // 5     | T  | F  | T  | F  | 0101
        // 6     | F  | T  | T  | F  | 0110
        // 7     | T  | T  | T  | F  | 0111
        // 8     | F  | F  | F  | T  | 1000
        // 9     | T  | F  | F  | T  | 1001
        // 10    | F  | T  | F  | T  | 1010
        // 11    | T  | T  | F  | T  | 1011
        // 12    | F  | F  | T  | T  | 1100
        // 13    | T  | F  | T  | T  | 1101
        // 14    | F  | T  | T  | T  | 1110
        // 15    | T  | T  | T  | T  | 1111
        public static readonly ulong[] castlingRights = new ulong[16];   // all combinations

        public static readonly ulong[] enPassantFile = new ulong[8];     // files a-h
        public static readonly ulong sideToMove;                         // single value

        static ZobristHashing() {
            // Initialize piece-square table
            for (int i = 0; i < 768; i++) {
                pieceSquare[i] = BitOperations.random_Bitboard();
            }

            // Initialize castling rights
            for (int i = 0; i < 16; i++) {
                castlingRights[i] = BitOperations.random_Bitboard();
            }

            // Initialize en passant files
            for (int i = 0; i < 8; i++) {
                enPassantFile[i] = BitOperations.random_Bitboard();
            }

            sideToMove = BitOperations.random_Bitboard();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ComputeCastlingRightsHash(in State state) {
            int index = 0;
            if (state.CanWhiteKingCastle) index |= 1;   // 0001
            if (state.CanWhiteQueenCastle) index |= 2;  // 0010
            if (state.CanBlackKingCastle) index |= 4;   // 0100
            if (state.CanBlackQueenCastle) index |= 8;  // 1000
            return castlingRights[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetPieceSquareIndex(TurnColor color, PieceType piece, int square) {
            return ((int)color * 6 + (int)piece) * 64 + square;
        }
    }
}