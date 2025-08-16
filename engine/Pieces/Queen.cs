using System.Runtime.CompilerServices;
using ChessEngine.Utils;
using ChessEngine.Utils.Logging;
using Bitboard = ulong;

namespace ChessEngine.Pieces {
    public static class Queen {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bitboard ComputePossibleMoves(Bitboard square, Chessboard chessboard, TurnColor? turnColor = null) {
            return Rook.ComputePossibleMoves(square, chessboard, turnColor) | Bishop.ComputePossibleMoves(square, chessboard, turnColor);
        }
    } 
}