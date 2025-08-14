using ChessEngine.Utils;
using ChessEngine.Utils.Logging;
using static ChessEngine.Move;
using Bitboard = ulong;

namespace ChessEngine.Pieces {
    public static class Pawn {
        public static readonly Dictionary<char, byte> PromotionDict = new() {
            {'n', (byte)SpecialMovesCode.KnightPromotion},
            {'b', (byte)SpecialMovesCode.BishopPromotion},
            {'r', (byte)SpecialMovesCode.RookPromotion},
            {'q', (byte)SpecialMovesCode.QueenPromotion},
        };
        
        // Precomputed promotion special codes
        public static readonly SpecialMovesCode[] QuietPromotions = {
            SpecialMovesCode.QueenPromotion,
            SpecialMovesCode.RookPromotion, 
            SpecialMovesCode.BishopPromotion,
            SpecialMovesCode.KnightPromotion
        };

        public static readonly SpecialMovesCode[] CapturePromotions = {
            SpecialMovesCode.QueenPromotionCapture,  // Queen
            SpecialMovesCode.RookPromotionCapture,  // Rook
            SpecialMovesCode.BishopPromotionCapture,  // Bishop  
            SpecialMovesCode.KnightPromotionCapture   // Knight
        };

        public static Bitboard ComputePossibleMoves(Bitboard pawnLocation, Chessboard chessboard, TurnColor? turnColor = null) {
            if ((turnColor ?? chessboard.State.TurnColor) == TurnColor.White) {
                // check the single space infront of the white pawn

                Bitboard pawn_one_step = (pawnLocation << 8) & ~chessboard.AllPieces;
                Bitboard pawn_two_steps = ((pawn_one_step & LookupTables.GetRankMask(Rank.RANK_3)) << 8) & ~chessboard.AllPieces;
                // the union of the movements dictate the possible moves forward available
                Bitboard pawn_valid_moves = pawn_one_step | pawn_two_steps;

                /*  now we calculate the attack moves
                    check the left side of the pawn, minding the underflow File A */
                Bitboard pawn_left_attack = (pawnLocation & LookupTables.GetFileClear(File.FILE_A)) << 7;
                // then check the right side of the pawn, minding the overflow File H
                Bitboard pawn_right_attack = (pawnLocation & LookupTables.GetFileClear(File.FILE_H)) << 9;
                // Calculate where I can actually attack something + en passant
                //Logger.Log(BitOperations.ToSquare(pawn_left_attack), BitOperations.ToSquare(pawn_right_attack));

                Bitboard pawn_valid_attacks = (pawn_left_attack | pawn_right_attack) &
                                                (chessboard.AllBlackPieces | BitOperations.ToBitboard(chessboard.State.EnPassantSquare));
                return pawn_valid_moves | pawn_valid_attacks;
            }
            else {
                Bitboard pawn_one_step = (pawnLocation >> 8) & ~chessboard.AllPieces;
                Bitboard pawn_two_steps = ((pawn_one_step & LookupTables.GetRankMask(Rank.RANK_6)) >> 8) & ~chessboard.AllPieces;
                Bitboard pawn_valid_moves = pawn_one_step | pawn_two_steps;

                Bitboard pawn_left_attack = (pawnLocation & LookupTables.GetFileClear(File.FILE_A)) >> 9;
                Bitboard pawn_right_attack = (pawnLocation & LookupTables.GetFileClear(File.FILE_H)) >> 7;

                Bitboard pawn_valid_attacks = (pawn_left_attack | pawn_right_attack) &
                                                (chessboard.AllWhitePieces | BitOperations.ToBitboard(chessboard.State.EnPassantSquare));
                return pawn_valid_moves | pawn_valid_attacks;
            }

        }

        public static Bitboard ComputePossibleAttacks(Bitboard pawnLocation, Chessboard chessboard, TurnColor? turnColor = null) {
            Bitboard pawn_left_attack;
            Bitboard pawn_right_attack;
            var color = turnColor ?? chessboard.stateStack.ElementAt(0).TurnColor;

            if (color == TurnColor.White) {
                pawn_left_attack = (pawnLocation & LookupTables.GetFileClear(File.FILE_A)) << 7;
                pawn_right_attack = (pawnLocation & LookupTables.GetFileClear(File.FILE_H)) << 9;
            }
            else {
                pawn_left_attack = (pawnLocation & LookupTables.GetFileClear(File.FILE_A)) >> 9;
                pawn_right_attack = (pawnLocation & LookupTables.GetFileClear(File.FILE_H)) >> 7;
            }

            var ownSide = (color == TurnColor.White) ? chessboard.AllWhitePieces : chessboard.AllBlackPieces;
            return (pawn_left_attack | pawn_right_attack) & ~ownSide;
        }
    }
}