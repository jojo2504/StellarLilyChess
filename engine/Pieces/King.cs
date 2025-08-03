using ChessEngine.Utils;
using ChessEngine.Utils.Logging;
using Bitboard = ulong;

namespace ChessEngine.Pieces {
    public static class King {
        public static Bitboard ComputePossibleMoves(Square square, Chessboard chessboard, TurnColor? turnColor = null) {
            var kingLocation = BitOperations.ToBitboard(square);

            Bitboard kingClipFileA = kingLocation & LookupTables.GetFileClear(File.FILE_A);
            Bitboard kingClipFileH = kingLocation & LookupTables.GetFileClear(File.FILE_H);

            /* remember the representation of the board in relation to the bitindex 
                when looking at these shifts.... */
            Bitboard spot_1 = kingLocation << 8;    // king moves top
            Bitboard spot_2 = kingLocation >> 8;    // king moves bot

            Bitboard spot_3 = kingClipFileA << 7;   // if king not on file A, moves topleft
            Bitboard spot_4 = kingClipFileA >> 1; 	// if king not on file A, moves left
            Bitboard spot_5 = kingClipFileA >> 9; 	// if king not on file A, moves bottomleft

            Bitboard spot_6 = kingClipFileH << 9; 	// if king not on file H, moves topright
            Bitboard spot_7 = kingClipFileH << 1; 	// if king not on file H, moves right
            Bitboard spot_8 = kingClipFileH >> 7; 	// if king not on file H, moves bottomright

            Bitboard castle_king = 0UL;
            Bitboard castle_queen = 0UL;
            
            if (chessboard.State.TurnColor == TurnColor.White) {
                if (chessboard.State.CanWhiteKingCastle &
                !chessboard.AreSquaresAttackedByColor([Square.E1, Square.F1, Square.G1], TurnColor.Black)) {
                    castle_king = kingLocation << 2;
                }
                else if (chessboard.State.CanWhiteQueenCastle &
                !chessboard.AreSquaresAttackedByColor([Square.E1, Square.D1, Square.C1], TurnColor.Black)) {
                    castle_queen = kingLocation >> 2;
                }
            }
            else { 
                if (chessboard.State.CanBlackKingCastle &
                !chessboard.AreSquaresAttackedByColor([Square.E8, Square.F8, Square.G8], TurnColor.White)) {
                    castle_king = kingLocation << 2;
                }
                else if (chessboard.State.CanBlackQueenCastle &
                !chessboard.AreSquaresAttackedByColor([Square.E8, Square.D8, Square.C8], TurnColor.White)) {
                    castle_queen = kingLocation >> 2;
                }
            }

            Bitboard kingMoves = spot_1 | spot_2 | spot_3 | spot_4 | spot_5 | spot_6 | spot_7 | spot_8 | castle_king | castle_queen;

            var ownSide = ((turnColor ?? chessboard.State.TurnColor) == TurnColor.White) ? chessboard.AllWhitePieces : chessboard.AllBlackPieces;
            return kingMoves & ~ownSide;
        }
    }
}