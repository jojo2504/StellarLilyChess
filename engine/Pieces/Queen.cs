using ChessEngine.Utils;
using ChessEngine.Utils.Logging;
using Bitboard = ulong;

namespace ChessEngine.Pieces {
    public static class Queen {
        public static Bitboard ComputePossibleMoves(Square square, Chessboard chessboard, TurnColor? turnColor = null) {
            return Rook.ComputePossibleMoves(square, chessboard, turnColor) | Bishop.ComputePossibleMoves(square, chessboard, turnColor);
        }
    } 
}