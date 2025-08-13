namespace ChessEngine {
    public struct State {
        public TurnColor TurnColor;
        public int FullMoveNumber;
        public int HalfMoveClock;
        public bool CanWhiteKingCastle;
        public bool CanWhiteQueenCastle;
        public bool CanBlackKingCastle;
        public bool CanBlackQueenCastle;
        public Square? EnPassantSquare; // null if none
        public PieceType? CapturedPiece;  // null if none
        public bool Checkmated;
        public bool Stalemated;
        public Move? Move; // no move at the starting position

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
                $"Stalemated: {Stalemated}, " +
                $"Move: {((Move is not null) ? Move.ToString() : "None")}";
        }
    }
}