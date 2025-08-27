using System.Runtime.CompilerServices;
using ChessEngine.Utils;

namespace ChessEngine.Pieces {
    public static class Queen {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bitboard ComputePossibleMoves(Bitboard square, Chessboard chessboard, TurnColor? turnColor = null) {
            return Rook.ComputePossibleMoves(square, chessboard, turnColor) | Bishop.ComputePossibleMoves(square, chessboard, turnColor);
        }
    } 
}