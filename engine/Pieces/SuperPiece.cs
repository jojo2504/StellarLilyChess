using Bitboard = ulong;

namespace ChessEngine.Pieces {
    public static class SuperPiece {
        public static Bitboard[] RookAttacks = new Bitboard[64]; // provide Rook attacks for each square with empty blockers
        public static Bitboard[] BishopAttacks = new Bitboard[64]; // provide Bishop attacks for each square with empty blockers
        
        static SuperPiece() {
            for (int i = 0; i < 64; i++) {
                RookAttacks[i] = Rook.Ratt(i, 0UL);
                BishopAttacks[i] = Bishop.Batt(i, 0UL);
            }
        }
    }
}