using System.Diagnostics;
using System.Runtime.CompilerServices;
using ChessEngine.Utils;
using ChessEngine.Utils.Logging;
using Bitboard = ulong;

namespace ChessEngine.Pieces {
    public static class King {
        static Bitboard[] KingAttackMasks = new Bitboard[64];

        public static readonly ulong[] CastlingRookMasks = [
            1UL << (int)Square.A1,  // White Queen
            1UL << (int)Square.H1,  // White King  
            1UL << (int)Square.A8,  // Black Queen
            1UL << (int)Square.H8   // Black King
        ];

        // Precomputed constants - no array allocation!
        private static class CastlingMasks {
            public const ulong WhiteKingSideAttack = (1UL << (int)Square.E1) | (1UL << (int)Square.F1) | (1UL << (int)Square.G1);
            public const ulong WhiteKingSideEmpty = (1UL << (int)Square.F1) | (1UL << (int)Square.G1);

            public const ulong WhiteQueenSideAttack = (1UL << (int)Square.E1) | (1UL << (int)Square.D1) | (1UL << (int)Square.C1);
            public const ulong WhiteQueenSideEmpty = (1UL << (int)Square.D1) | (1UL << (int)Square.C1) | (1UL << (int)Square.B1);

            public const ulong BlackKingSideAttack = (1UL << (int)Square.E8) | (1UL << (int)Square.F8) | (1UL << (int)Square.G8);
            public const ulong BlackKingSideEmpty = (1UL << (int)Square.F8) | (1UL << (int)Square.G8);

            public const ulong BlackQueenSideAttack = (1UL << (int)Square.E8) | (1UL << (int)Square.D8) | (1UL << (int)Square.C8);
            public const ulong BlackQueenSideEmpty = (1UL << (int)Square.D8) | (1UL << (int)Square.C8) | (1UL << (int)Square.B8);
        }

        static King() {
            InitKingAttacks();
        }

        static void InitKingAttacks() {
            for (int i = 0; i < 64; i++) {
                var kingLocation = 1UL << i;
                Bitboard kingClipFileA = kingLocation & LookupTables.GetFileClear(File.FILE_A);
                Bitboard kingClipFileH = kingLocation & LookupTables.GetFileClear(File.FILE_H);

                /* remember the representation of the board in relation to the bitindex 
                    when looking at these shifts.... */
                Bitboard spot_1 = kingLocation << 8;    // king moves top
                Bitboard spot_2 = kingLocation >> 8;    // king moves bot

                Bitboard spot_3 = kingClipFileA << 7;   // if king not on file A, moves topleft
                Bitboard spot_4 = kingClipFileA >> 1;   // if king not on file A, moves left
                Bitboard spot_5 = kingClipFileA >> 9;   // if king not on file A, moves bottomleft

                Bitboard spot_6 = kingClipFileH << 9;   // if king not on file H, moves topright
                Bitboard spot_7 = kingClipFileH << 1;   // if king not on file H, moves right
                Bitboard spot_8 = kingClipFileH >> 7;   // if king not on file H, moves bottomright

                KingAttackMasks[i] = spot_1 | spot_2 | spot_3 | spot_4 | spot_5 | spot_6 | spot_7 | spot_8;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bitboard ComputePossibleAttacks(Bitboard kingLocation, Chessboard chessboard, TurnColor? turnColor = null) {
            Bitboard kingMoves = KingAttackMasks[BitOperations.ToIndex(kingLocation)];
            var ownSide = ((turnColor ?? chessboard.State.TurnColor) == TurnColor.White) ? chessboard.AllWhitePieces : chessboard.AllBlackPieces;
            return kingMoves & ~ownSide;
        }

        public static Bitboard ComputePossibleCastlingMoves(Bitboard kingLocation, Chessboard chessboard, TurnColor? turnColor = null) {
            Bitboard castle_king = 0UL;
            Bitboard castle_queen = 0UL;

            if ((turnColor ?? chessboard.State.TurnColor) == TurnColor.White) {
                if (chessboard.State.CanWhiteKingCastle &&
                !chessboard.AreSquaresAttackedByColor(CastlingMasks.WhiteKingSideAttack, TurnColor.Black) &&
                !chessboard.AreAnySquaresOccupied(CastlingMasks.WhiteKingSideEmpty)) {
                    castle_king = kingLocation << 2;
                }

                if (chessboard.State.CanWhiteQueenCastle &&
                !chessboard.AreSquaresAttackedByColor(CastlingMasks.WhiteQueenSideAttack, TurnColor.Black) &&
                !chessboard.AreAnySquaresOccupied(CastlingMasks.WhiteQueenSideEmpty)) {
                    castle_queen = kingLocation >> 2;
                }
            }
            else {
                if (chessboard.State.CanBlackKingCastle &&
                !chessboard.AreSquaresAttackedByColor(CastlingMasks.BlackKingSideAttack, TurnColor.White) &&
                !chessboard.AreAnySquaresOccupied(CastlingMasks.BlackKingSideEmpty)) {
                    castle_king = kingLocation << 2;
                }
                if (chessboard.State.CanBlackQueenCastle &&
                !chessboard.AreSquaresAttackedByColor(CastlingMasks.BlackQueenSideAttack, TurnColor.White) &&
                !chessboard.AreAnySquaresOccupied(CastlingMasks.BlackQueenSideEmpty)) {
                    castle_queen = kingLocation >> 2;
                }
            }

            return castle_king | castle_queen;
        }
    }
}