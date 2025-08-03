using Bitboard = ulong;

namespace ChessEngine {
    public static class LookupTables {
        private static readonly Bitboard[] rankMask = new Bitboard[8];
        private static readonly Bitboard[] fileMask = new Bitboard[8];
        private static readonly Bitboard[] rankClear = new Bitboard[8];
        private static readonly Bitboard[] fileClear = new Bitboard[8];

        static LookupTables() {
            InitMaskClearRankFile();
        }

        private static Bitboard RankMask(int rank) {
            return 255UL << (rank * 8);
        }

        private static Bitboard FileMask(int file) {
            return 72340172838076673UL << file;
        }

        private static Bitboard RankClear(int rank) {
            return ~RankMask(rank);
        }

        private static Bitboard FileClear(int file) {
            return ~FileMask(file);
        }

        private static void InitMaskClearRankFile() {
            for (int i = 0; i < 8; ++i){
                rankMask[i] = RankMask(i);
                fileMask[i] = FileMask(i);
                rankClear[i] = RankClear(i);
                fileClear[i] = FileClear(i);
            }
        }

        public static Bitboard GetRankMask(Rank rank) => rankMask[(int)rank];
        public static Bitboard GetFileMask(File file) => fileMask[(int)file];
        public static Bitboard GetRankClear(Rank rank) => rankClear[(int)rank];
        public static Bitboard GetFileClear(File file) => fileClear[(int)file];
        public static Bitboard CornerMask => 9295429630892703873UL;
        public static Bitboard CornerClear => ~CornerMask;
        public static Bitboard AllBordersMask => 18411139144890810879UL;
        public static Bitboard AllBordersClear => ~AllBordersMask;
    }
}