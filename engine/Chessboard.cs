using System.Diagnostics;
using ChessEngine.Pieces;
using ChessEngine.Utils;
using ChessEngine.Utils.Logging;
using Bitboard = ulong;

namespace ChessEngine {
    /// <summary>
    /// This is a rank --> --- <br/>
    /// 1 to 8
    /// </summary>
    public enum Rank : int {
        RANK_1, RANK_2, RANK_3, RANK_4, RANK_5, RANK_6, RANK_7, RANK_8
    };

    /// <summary>
    /// This is a file --> | <br/>
    /// A to H
    /// </summary>
    public enum File : int {
        FILE_A, FILE_B, FILE_C, FILE_D, FILE_E, FILE_F, FILE_G, FILE_H
    };

    public enum Square : byte {
        A1, B1, C1, D1, E1, F1, G1, H1,
        A2, B2, C2, D2, E2, F2, G2, H2,
        A3, B3, C3, D3, E3, F3, G3, H3,
        A4, B4, C4, D4, E4, F4, G4, H4,
        A5, B5, C5, D5, E5, F5, G5, H5,
        A6, B6, C6, D6, E6, F6, G6, H6,
        A7, B7, C7, D7, E7, F7, G7, H7,
        A8, B8, C8, D8, E8, F8, G8, H8
    }

    public enum PieceType {
        Pawn,
        Rook,
        Knight,
        Bishop,
        Queen,
        King
    }

    public enum TurnColor {
        White,
        Black
    }

    //just a simple reference wrapper class
    public class RefBitboard {
        public Bitboard BitboardValue;
    }

    public class Chessboard {
        public RefBitboard WhitePawns;
        public RefBitboard WhiteRooks;
        public RefBitboard WhiteKnights;
        public RefBitboard WhiteBishops;
        public RefBitboard WhiteQueens;
        public RefBitboard WhiteKing;

        public RefBitboard BlackPawns;
        public RefBitboard BlackRooks;
        public RefBitboard BlackKnights;
        public RefBitboard BlackBishops;
        public RefBitboard BlackQueens;
        public RefBitboard BlackKing;

        public Bitboard AllWhitePieces {
            get {
                return WhitePawns.BitboardValue
                     | WhiteRooks.BitboardValue
                     | WhiteKnights.BitboardValue
                     | WhiteBishops.BitboardValue
                     | WhiteQueens.BitboardValue
                     | WhiteKing.BitboardValue;
            }
        }

        public Bitboard AllBlackPieces {
            get {
                return BlackPawns.BitboardValue
                    | BlackRooks.BitboardValue
                    | BlackKnights.BitboardValue
                    | BlackBishops.BitboardValue
                    | BlackQueens.BitboardValue
                    | BlackKing.BitboardValue;
            }
        }

        public Bitboard AllPieces {
            get {
                return AllWhitePieces | AllBlackPieces;
            }
        }

        public RefBitboard[,] Position = new RefBitboard[2, 6];
        public Bitboard[,] AttackMatrix = new Bitboard[2, 6];
        public State State = new();
        public Stack<State> stateStack = new();

        public Chessboard() {
            InitializeState();
            stateStack.Push(State); // halfmove 0

            InitializeChessBoard();
        }

        void InitializeState() {
            State.TurnColor = TurnColor.White;
            State.CanBlackKingCastle = true;
            State.CanBlackQueenCastle = true;
            State.CanWhiteKingCastle = true;
            State.CanWhiteQueenCastle = true;
            State.FullMoveNumber = 0;
            State.HalfMoveClock = 0;
            State.Checkmated = false;
            State.Stalemated = false;
        }

        void InitializeChessBoard() {
            WhitePawns = new() { BitboardValue = 65280UL };
            WhiteRooks = new() { BitboardValue = 129UL };
            WhiteKnights = new() { BitboardValue = 66UL };
            WhiteBishops = new() { BitboardValue = 36UL };
            WhiteQueens = new() { BitboardValue = 8UL };
            WhiteKing = new() { BitboardValue = 16UL };
            BlackPawns = new() { BitboardValue = 71776119061217280UL };
            BlackRooks = new() { BitboardValue = 9295429630892703744UL };
            BlackKnights = new() { BitboardValue = 4755801206503243776UL };
            BlackBishops = new() { BitboardValue = 2594073385365405696UL };
            BlackQueens = new() { BitboardValue = 576460752303423488UL };
            BlackKing = new() { BitboardValue = 1152921504606846976UL };

            Position[(int)TurnColor.White, (int)PieceType.Pawn] = WhitePawns;
            Position[(int)TurnColor.White, (int)PieceType.Rook] = WhiteRooks;
            Position[(int)TurnColor.White, (int)PieceType.Knight] = WhiteKnights;
            Position[(int)TurnColor.White, (int)PieceType.Bishop] = WhiteBishops;
            Position[(int)TurnColor.White, (int)PieceType.Queen] = WhiteQueens;
            Position[(int)TurnColor.White, (int)PieceType.King] = WhiteKing;
            Position[(int)TurnColor.Black, (int)PieceType.Pawn] = BlackPawns;
            Position[(int)TurnColor.Black, (int)PieceType.Rook] = BlackRooks;
            Position[(int)TurnColor.Black, (int)PieceType.Knight] = BlackKnights;
            Position[(int)TurnColor.Black, (int)PieceType.Bishop] = BlackBishops;
            Position[(int)TurnColor.Black, (int)PieceType.Queen] = BlackQueens;
            Position[(int)TurnColor.Black, (int)PieceType.King] = BlackKing;
        }

        public override string ToString() {
            // TODO : rework this absolute mess
            var ChessBoardState = new string('0', 64);
            ChessBoardState = StringHelper.MergeStrings(StringHelper.ToBinary(WhitePawns.BitboardValue).Replace('1', 'P'), ChessBoardState);
            ChessBoardState = StringHelper.MergeStrings(StringHelper.ToBinary(WhiteRooks.BitboardValue).Replace('1', 'R'), ChessBoardState);
            ChessBoardState = StringHelper.MergeStrings(StringHelper.ToBinary(WhiteKnights.BitboardValue).Replace('1', 'N'), ChessBoardState);
            ChessBoardState = StringHelper.MergeStrings(StringHelper.ToBinary(WhiteBishops.BitboardValue).Replace('1', 'B'), ChessBoardState);
            ChessBoardState = StringHelper.MergeStrings(StringHelper.ToBinary(WhiteQueens.BitboardValue).Replace('1', 'Q'), ChessBoardState);
            ChessBoardState = StringHelper.MergeStrings(StringHelper.ToBinary(WhiteKing.BitboardValue).Replace('1', 'K'), ChessBoardState);
            ChessBoardState = StringHelper.MergeStrings(StringHelper.ToBinary(BlackPawns.BitboardValue).Replace('1', 'p'), ChessBoardState);
            ChessBoardState = StringHelper.MergeStrings(StringHelper.ToBinary(BlackRooks.BitboardValue).Replace('1', 'r'), ChessBoardState);
            ChessBoardState = StringHelper.MergeStrings(StringHelper.ToBinary(BlackKnights.BitboardValue).Replace('1', 'n'), ChessBoardState);
            ChessBoardState = StringHelper.MergeStrings(StringHelper.ToBinary(BlackBishops.BitboardValue).Replace('1', 'b'), ChessBoardState);
            ChessBoardState = StringHelper.MergeStrings(StringHelper.ToBinary(BlackQueens.BitboardValue).Replace('1', 'q'), ChessBoardState);
            ChessBoardState = StringHelper.MergeStrings(StringHelper.ToBinary(BlackKing.BitboardValue).Replace('1', 'k'), ChessBoardState);
            return StringHelper.FormatAsChessboard(ChessBoardState);
        }

        void GetAllPossiblePieceMoves(int colorIndex, int pieceTypeIndex, ref List<Move> allPseudoLegalMoves) {
            var pieceBitboard = Position[colorIndex, pieceTypeIndex].BitboardValue;
            var squares = BitOperations.GetSquaresFromBits(pieceBitboard);

            switch ((PieceType)pieceTypeIndex) {
                case PieceType.Pawn:
                    foreach (var square in squares) {
                        var possibleMoves = Pawn.ComputePossibleMoves(square, this, (TurnColor)colorIndex);
                        Move.EncodePossibleMoves(possibleMoves, square, ref allPseudoLegalMoves);
                    }
                    break;
                case PieceType.Rook:
                    foreach (var square in squares) {
                        var possibleMoves = Rook.ComputePossibleMoves(square, this, (TurnColor)colorIndex);
                        Move.EncodePossibleMoves(possibleMoves, square, ref allPseudoLegalMoves);
                    }
                    break;
                case PieceType.Knight:
                    foreach (var square in squares) {
                        var possibleMoves = Knight.ComputePossibleMoves(square, this, (TurnColor)colorIndex);
                        Move.EncodePossibleMoves(possibleMoves, square, ref allPseudoLegalMoves);
                    }
                    break;
                case PieceType.Bishop:
                    foreach (var square in squares) {
                        var possibleMoves = Bishop.ComputePossibleMoves(square, this, (TurnColor)colorIndex);
                        Move.EncodePossibleMoves(possibleMoves, square, ref allPseudoLegalMoves);
                    }
                    break;
                case PieceType.Queen:
                    foreach (var square in squares) {
                        var possibleMoves = Queen.ComputePossibleMoves(square, this, (TurnColor)colorIndex);
                        Move.EncodePossibleMoves(possibleMoves, square, ref allPseudoLegalMoves);
                    }
                    break;
                case PieceType.King:
                    foreach (var square in squares) {
                        var possibleMoves = King.ComputePossibleMoves(square, this, (TurnColor)colorIndex);
                        Move.EncodePossibleMoves(possibleMoves, square, ref allPseudoLegalMoves);
                    }
                    break;
            }
        }

        // for all pieces, move/attack are the same, except for the pawn, which can attack but not move if the square is empty or has an ally piece. 
        void PopulatePieceAttacks(int colorIndex, int pieceTypeIndex) {
            var pieceBitboard = Position[colorIndex, pieceTypeIndex].BitboardValue;
            var squares = BitOperations.GetSquaresFromBits(pieceBitboard);
            AttackMatrix[colorIndex, pieceTypeIndex] = 0UL; // reset the corresponding matrix attack of the targeted piece type and color

            switch ((PieceType)pieceTypeIndex) {
                case PieceType.Pawn:
                    foreach (var square in squares) {
                        var possibleMoves = Pawn.ComputePossibleAttacks(square, this, (TurnColor)colorIndex);
                        AttackMatrix[colorIndex, (int)PieceType.Pawn] |= possibleMoves;
                    }
                    break;
                case PieceType.Rook:
                    foreach (var square in squares) {
                        var possibleAttacks = Rook.ComputePossibleMoves(square, this, (TurnColor)colorIndex);
                        AttackMatrix[colorIndex, (int)PieceType.Rook] |= possibleAttacks;
                    }
                    break;
                case PieceType.Knight:
                    foreach (var square in squares) {
                        var possibleAttacks = Knight.ComputePossibleMoves(square, this, (TurnColor)colorIndex);
                        AttackMatrix[colorIndex, (int)PieceType.Knight] |= possibleAttacks;
                    }
                    break;
                case PieceType.Bishop:
                    foreach (var square in squares) {
                        var possibleAttacks = Bishop.ComputePossibleMoves(square, this, (TurnColor)colorIndex);
                        AttackMatrix[colorIndex, (int)PieceType.Bishop] |= possibleAttacks;
                    }
                    break;
                case PieceType.Queen:
                    foreach (var square in squares) {
                        var possibleAttacks = Queen.ComputePossibleMoves(square, this, (TurnColor)colorIndex);
                        AttackMatrix[colorIndex, (int)PieceType.Queen] |= possibleAttacks;
                    }
                    break;
                case PieceType.King:
                    foreach (var square in squares) {
                        var possibleAttacks = King.ComputePossibleMoves(square, this, (TurnColor)colorIndex);
                        AttackMatrix[colorIndex, (int)PieceType.King] |= possibleAttacks;
                    }
                    break;
            }
        }

        public List<Move> GenerateMoves(TurnColor? turnColor = null) {
            List<Move> allPseudoLegalMoves = new();

            if (turnColor is not null) {
                for (int pieceTypeIndex = 0; pieceTypeIndex < Position.GetLength(1); pieceTypeIndex++) {
                    //int localPieceTypeIndex = pieceTypeIndex; // capture the value otherwise the Task just skips it and we'll have an index error
                    GetAllPossiblePieceMoves((int)turnColor, pieceTypeIndex, ref allPseudoLegalMoves);
                }
            }
            else {
                // first opponant color, then own color to check for attacked/unattacked squares => populate the attack matrix
                for (int pieceTypeIndex = 0; pieceTypeIndex < Position.GetLength(1); pieceTypeIndex++) {
                    PopulatePieceAttacks((int)State.TurnColor ^ 1, pieceTypeIndex);
                }

                for (int pieceTypeIndex = 0; pieceTypeIndex < Position.GetLength(1); pieceTypeIndex++) {
                    GetAllPossiblePieceMoves((int)State.TurnColor, pieceTypeIndex, ref allPseudoLegalMoves);
                }
            }

            return allPseudoLegalMoves;
        }

        public List<Move> GenerateLegalMoves() {
            List<Move> LegalMoves = [];
            int nMoves, i;

            List<Move> allPseudoLegalMoves = GenerateMoves();
            nMoves = allPseudoLegalMoves.Count;

            for (i = 0; i < nMoves; i++) {
                Move.MakeMove(this, allPseudoLegalMoves[i]);
                if (!IsIncheck()) {
                    Logger.Log($"KING POSITION IS NOT IN CHECK AT {BitOperations.ToSquare(Position[(int)State.TurnColor, (int)PieceType.King].BitboardValue)} AFTER {allPseudoLegalMoves[i]}");
                    LegalMoves.AddRange(allPseudoLegalMoves[i]);
                }
                else {
                    Logger.Log($"KING POSITION IS IN CHECK AT {BitOperations.ToSquare(Position[(int)State.TurnColor, (int)PieceType.King].BitboardValue)} AFTER {allPseudoLegalMoves[i]}");
                }
                Move.UnmakeMove(this, allPseudoLegalMoves[i]);
            }

            return LegalMoves;
        }

        public Bitboard GenerateAttacks(TurnColor turnColor) {
            Bitboard allAttackedSquares = 0UL;

            for (int pieceTypeIndex = 0; pieceTypeIndex < Position.GetLength(1); pieceTypeIndex++) {
                PopulatePieceAttacks((int)turnColor, pieceTypeIndex);
                allAttackedSquares |= AttackMatrix[(int)turnColor, pieceTypeIndex];
            }
            return allAttackedSquares;
        }

        //check if the king from a color is in check
        public bool IsIncheck() {
            Bitboard AllAttackedSquares;

            if (stateStack.ElementAt(0).TurnColor == TurnColor.White) {
                //check if white king is in check
                AllAttackedSquares = GenerateAttacks(TurnColor.Black);
                return (AllAttackedSquares & WhiteKing.BitboardValue) != 0;
            }
            else {
                //check if black king is in check
                AllAttackedSquares = GenerateAttacks(TurnColor.White);
                return (AllAttackedSquares & BlackKing.BitboardValue) != 0;
            }
        }

        //check if given square is attacked by a color 
        public bool IsSquareAttackedByColor(Square square, TurnColor turnColor) {
            Bitboard AllAttackedSquares = 0UL;

            for (int pieceTypeIndex = 0; pieceTypeIndex < Position.GetLength(1); pieceTypeIndex++) {
                AllAttackedSquares |= AttackMatrix[(int)turnColor ^ 1, pieceTypeIndex];
            }

            return (AllAttackedSquares & BitOperations.ToBitboard(square)) != 0;
        }

        public bool AreSquaresAttackedByColor(Square[] squares, TurnColor turnColor) {
            Bitboard AllAttackedSquares = 0UL;

            for (int pieceTypeIndex = 0; pieceTypeIndex < Position.GetLength(1); pieceTypeIndex++) {
                AllAttackedSquares |= AttackMatrix[(int)turnColor ^ 1, pieceTypeIndex];
            }

            foreach (Square square in squares) {
                if ((AllAttackedSquares & BitOperations.ToBitboard(square)) != 0) {
                    return true;
                }
            }

            return false;
        }

        public ulong Perft(int depth) {
            int nMoves, i;
            ulong nodes = 0;

            if (depth == 0)
                return 1UL;

            List<Move> allPseudoLegalMoves = GenerateMoves();
            nMoves = allPseudoLegalMoves.Count;
            for (i = 0; i < nMoves; i++) {
                Move.MakeMove(this, allPseudoLegalMoves[i]);
                if (!IsIncheck()) {
                    nodes += Perft(depth - 1);
                }
                Move.UnmakeMove(this, allPseudoLegalMoves[i]);
            }
            return nodes;
        }

        public void PushUci(string move) {
            Move.MakeMove(this, Move.DecodeUciMove(this, move), displayComputationLogs: true);
        }
    }
}