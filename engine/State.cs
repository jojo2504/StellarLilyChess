namespace ChessEngine {
    public struct State {
        // Fields that can't be altered during the move but can be altered between moves
        // Which means that we restore a position from these fields
        // These fields indicate that at a given state, this move with this state has been played
        // example : at white turn, halfmoveclock is 10, can white king castle, he decided to capture a piece, and didnt have en passant available 
        // So we need to push to the stack the state before updating anything 
        public TurnColor TurnColor;
        public int FullMoveNumber;
        public int HalfMoveClock;
        public bool CanWhiteKingCastle;
        public bool CanWhiteQueenCastle;
        public bool CanBlackKingCastle;
        public bool CanBlackQueenCastle;
        public Square? EnPassantSquare; // null if none

        // Fields that can be altered right during the move
        // Which means that after making a move, the new state has these fields updated, we need to push after this
        public PieceType? CapturedPiece;  // null if none

        // Fields that doesn't affect restoration
        public bool Checkmated;
        public bool Stalemated;
        public ulong ZobristHashKey; // Zobrist hash key for the position

        public override string ToString() {
            return
                $"Turn: {TurnColor}, " +
                $"FullMove: {FullMoveNumber}, " +
                $"HalfMove: {HalfMoveClock}, " +
                $"Castling: " +
                    $"WK={CanWhiteKingCastle}, " +
                    $"WQ={CanWhiteQueenCastle}, " +
                    $"BK={CanBlackKingCastle}, " +
                    $"BQ={CanBlackQueenCastle}, " +
                $"EnPassant: {(EnPassantSquare.HasValue ? EnPassantSquare.ToString() : "None")}, " +
                $"Captured: {(CapturedPiece.HasValue ? CapturedPiece.ToString() : "None")}, " +
                $"Checkmated: {Checkmated}, " +
                $"Stalemated: {Stalemated}";
        }
    }
}