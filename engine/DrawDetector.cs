using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChessEngine.Utils;

namespace ChessEngine {
    public class DrawDetector {
        public Dictionary<ulong, int> PositionsSeen = new Dictionary<ulong, int>(); // store zobrist hashes of seen positions 

        public DrawDetector() {

        }

        public bool IsGameDraw(Chessboard chessboard) {
            return DrawByThreefoldRepetition(chessboard) || DrawByFiftyMove(chessboard) || DrawByInsufficientMaterials(chessboard);
        }

        private bool DrawByThreefoldRepetition(Chessboard chessboard) {
            if (PositionsSeen.ContainsKey(chessboard.State.ZobristHashKey))
                PositionsSeen[chessboard.State.ZobristHashKey]++;
            else
                PositionsSeen[chessboard.State.ZobristHashKey] = 1;
            return PositionsSeen[chessboard.State.ZobristHashKey] >= 3;
        }

        private bool DrawByFiftyMove(Chessboard chessboard) {
            return chessboard.State.HalfMoveClock >= 100;
        }

        private bool DrawByInsufficientMaterials(Chessboard chessboard) {
            var allPieceNumber = BitOperations.CountBits(chessboard.AllPieces);
            if (allPieceNumber >= 5) {
                return false;
            }
            var whiteKingAlone = chessboard.AllWhitePieces == chessboard.WhiteKing;
            var blackKingAlone = chessboard.AllBlackPieces == chessboard.BlackKing;
            var whiteBishopNumber = BitOperations.CountBits(chessboard.WhiteBishops);
            var blackBishopNumber = BitOperations.CountBits(chessboard.BlackBishops);
            var whiteKnightNumber = BitOperations.CountBits(chessboard.WhiteKnights);
            var blackKnightNumber = BitOperations.CountBits(chessboard.BlackKnights);
            var allWhitePiecesNumber = BitOperations.CountBits(chessboard.AllWhitePieces);
            var allBlackPiecesNumber = BitOperations.CountBits(chessboard.AllBlackPieces);
            
            // King vs. king
            if (whiteKingAlone && blackKingAlone) {
                return true;
            }

                // King and bishop vs. king
                // King and knight vs. king
                if (whiteKingAlone &&
                    ((blackBishopNumber == 1) || blackKnightNumber == 1) &&
                    allBlackPiecesNumber == 2) {
                    return true;
                }
                if (blackKingAlone &&
                    ((whiteBishopNumber == 1) || whiteKnightNumber == 1) &&
                    allWhitePiecesNumber == 2) {
                    return true;
                }

                // King and bishop vs. king and bishop of the same color as the opponent's bishop
                if ((whiteBishopNumber == 1) &&
                    (blackBishopNumber == 1) &&
                    (allWhitePiecesNumber == 2) &&
                    (allBlackPiecesNumber == 2)) {
                    var WhiteBishopIndex = BitOperations.ToIndex(chessboard.WhiteBishops);
                    var BlackBishopIndex = BitOperations.ToIndex(chessboard.BlackBishops);
                    return (((WhiteBishopIndex / 8) + (WhiteBishopIndex % 8)) & 1) == (((BlackBishopIndex / 8) + (BlackBishopIndex % 8)) & 1);
                }   

            return false;
        }
    }
}