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
    public sealed class RefBitboard {
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

        public Chessboard(string fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1") {
            ParseFEN(fen);
            InitializeState();
            stateStack.Push(State); // halfmove 0

            InitializeChessBoard();
        }

        void InitializeState() {
            State.Checkmated = false;
            State.Stalemated = false;
            State.OwnKingInCheck = false; // (might be temp)
            State.EnemyKingInCheck = false; // (might be temp)
        }

        void ParseFEN(string fen) {
            var pieceSetters = new Dictionary<char, Action<int>> {
                { 'P', idx => WhitePawns.BitboardValue |= 1UL << idx },
                { 'N', idx => WhiteKnights.BitboardValue |= 1UL << idx },
                { 'B', idx => WhiteBishops.BitboardValue |= 1UL << idx },
                { 'R', idx => WhiteRooks.BitboardValue |= 1UL << idx },
                { 'Q', idx => WhiteQueens.BitboardValue |= 1UL << idx },
                { 'K', idx => WhiteKing.BitboardValue |= 1UL << idx },
                { 'p', idx => BlackPawns.BitboardValue |= 1UL << idx },
                { 'n', idx => BlackKnights.BitboardValue |= 1UL << idx },
                { 'b', idx => BlackBishops.BitboardValue |= 1UL << idx },
                { 'r', idx => BlackRooks.BitboardValue |= 1UL << idx },
                { 'q', idx => BlackQueens.BitboardValue |= 1UL << idx },
                { 'k', idx => BlackKing.BitboardValue |= 1UL << idx },
            };

            var parts = fen.Split(" ");

            var piecePlacement = parts[0];
            var turnColor = parts[1];
            var castlingAbility = parts[2];
            var epSquare = parts[3];
            var halfMove = parts[4];
            var FullMove = parts[5];

            // init state
            if (turnColor == "w") State.TurnColor = TurnColor.White;
            else if (turnColor == "b") State.TurnColor = TurnColor.Black;

            State.CanBlackKingCastle = false;
            State.CanBlackQueenCastle = false;
            State.CanWhiteKingCastle = false;
            State.CanWhiteQueenCastle = false;
            foreach (var letter in castlingAbility) {
                switch (letter) {
                    case 'K':
                        State.CanWhiteKingCastle = true;
                        break;
                    case 'Q':
                        State.CanWhiteQueenCastle = true;
                        break;
                    case 'k':
                        State.CanBlackKingCastle = true;
                        break;
                    case 'q':
                        State.CanBlackQueenCastle = true;
                        break;
                }
            }

            if (Enum.TryParse<Square>(epSquare, out var square))
                State.EnPassantSquare = square;
            else
                State.EnPassantSquare = null;

            State.HalfMoveClock = Convert.ToInt32(halfMove);
            State.FullMoveNumber = Convert.ToInt32(FullMove);

            WhitePawns = new() { BitboardValue = 0UL };
            WhiteRooks = new() { BitboardValue = 0UL };
            WhiteKnights = new() { BitboardValue = 0UL };
            WhiteBishops = new() { BitboardValue = 0UL };
            WhiteQueens = new() { BitboardValue = 0UL };
            WhiteKing = new() { BitboardValue = 0UL };
            BlackPawns = new() { BitboardValue = 0UL };
            BlackRooks = new() { BitboardValue = 0UL };
            BlackKnights = new() { BitboardValue = 0UL };
            BlackBishops = new() { BitboardValue = 0UL };
            BlackQueens = new() { BitboardValue = 0UL };
            BlackKing = new() { BitboardValue = 0UL };

            var ranks = piecePlacement.Split("/");
            var overallIndexSquare = 0;
            foreach (var rank in ranks) {
                for (int i = rank.Length - 1; i >= 0; i--) {
                    var letter = rank[i];
                    if (char.IsDigit(letter)) {
                        overallIndexSquare += letter - '0';
                    }
                    else if (pieceSetters.TryGetValue(letter, out var setPiece)) {
                        setPiece(63 - overallIndexSquare); // or whatever your index calculation is
                        overallIndexSquare++;
                    }
                }
            }
        }

        void InitializeChessBoard() {
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
            return StringHelper.FormatAsChessboard(ChessBoardState.Replace('0', '.'));
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
        // except for king which can't castle for direct attack pattern
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
                        var possibleAttacks = King.ComputePossibleAttacks(square, this, (TurnColor)colorIndex);
                        AttackMatrix[colorIndex, (int)PieceType.King] |= possibleAttacks;
                    }
                    break;
            }
        }

        public List<Move> GenerateMoves() {
            List<Move> allPseudoLegalMoves = new();

            // first opponant color, then own color to check for attacked/unattacked squares => populate the attack matrix
            for (int pieceTypeIndex = 0; pieceTypeIndex < Position.GetLength(1); pieceTypeIndex++) {
                PopulatePieceAttacks((int)State.TurnColor ^ 1, pieceTypeIndex);
            }

            for (int pieceTypeIndex = 0; pieceTypeIndex < Position.GetLength(1); pieceTypeIndex++) {
                GetAllPossiblePieceMoves((int)State.TurnColor, pieceTypeIndex, ref allPseudoLegalMoves);
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
                    //Logger.Log($"KING POSITION IS NOT IN CHECK AT {BitOperations.ToSquare(Position[(int)State.TurnColor, (int)PieceType.King].BitboardValue)} AFTER {allPseudoLegalMoves[i]}");
                    LegalMoves.AddRange(allPseudoLegalMoves[i]);
                }
                else {
                    //Logger.Log($"KING POSITION IS IN CHECK AT {BitOperations.ToSquare(Position[(int)State.TurnColor, (int)PieceType.King].BitboardValue)} AFTER {allPseudoLegalMoves[i]}");
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
        public bool IsIncheck(TurnColor? turncolor = null) {
            Bitboard AllAttackedSquares;

            if ((turncolor ?? State.TurnColor) == TurnColor.White) {
                //check if white king is in check
                AllAttackedSquares = GenerateAttacks(TurnColor.Black);
                //Logger.Log("black attacks squares");
                //Logger.Log(StringHelper.FormatAsChessboard(AllAttackedSquares));
                return (AllAttackedSquares & WhiteKing.BitboardValue) != 0;
            }
            else {
                //check if black king is in check
                Logger.Log("checking if black king is in check");
                Logger.Log("current chessboard");
                Logger.Log(this);

                AllAttackedSquares = GenerateAttacks(TurnColor.White);
                Logger.Log("white attacks squares");
                Logger.Log(StringHelper.FormatAsChessboard(AllAttackedSquares));

                Logger.Log("black king bitboard value");
                Logger.Log(StringHelper.FormatAsChessboard(Position[(int)TurnColor.Black, (int)PieceType.King]));

                Logger.Log("is in check ?");
                bool isInCheck = (AllAttackedSquares & BlackKing.BitboardValue) != 0;
                Logger.Log(isInCheck);

                return isInCheck;
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

        public bool AreSquaresOccupiedByColor(Square[] squares, TurnColor turnColor) {
            return false;
        }

        public ulong Perft(int depth) {
            int nMoves, i;
            ulong nodes = 0;

            if (depth == 0)
                return 1UL;

            List<Move> allPseudoLegalMoves = GenerateMoves();
            nMoves = allPseudoLegalMoves.Count;
            //Logger.Log(nMoves);

            for (i = 0; i < nMoves; i++) {
                Logger.Log("trying to do this move:", allPseudoLegalMoves[i]);
                Move.MakeMove(this, allPseudoLegalMoves[i]);
                bool isInCheckPerft = IsIncheck(stateStack.ElementAt(0).TurnColor);
                Logger.Log("returned isInCheck", isInCheckPerft);
                if (!isInCheckPerft) {
                    Logger.Log("made move because black king not in check after", allPseudoLegalMoves[i]);
                    Logger.Log(this);
                    Logger.Log("---------------------------");
                    nodes += Perft(depth - 1);
                }
                else {
                    Logger.Log($"skipped {allPseudoLegalMoves[i]} because it leaves the king in check");
                }
                Move.UnmakeMove(this, allPseudoLegalMoves[i]);
            }

            //Logger.Log("got", nodes);

            return nodes;
        }

        public void PushUci(string move) {
            Move.MakeMove(this, Move.DecodeUciMove(this, move));
        }
    }
}