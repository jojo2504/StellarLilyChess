using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ChessEngine.Pieces;
using ChessEngine.Utils;
using ChessEngine.Utils.Logging;
using static ChessEngine.Move;
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
            var epSquare = parts[3].ToUpper();
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

        private bool ShouldCheckCastling() {
            // Quick checks before expensive castling computation
            return (State.TurnColor == TurnColor.White && (State.CanWhiteKingCastle || State.CanWhiteQueenCastle))
                || (State.TurnColor == TurnColor.Black && (State.CanBlackKingCastle || State.CanBlackQueenCastle));
        }

        private void AddAllPossibleMoves(Bitboard fromBitboard, Bitboard possibleMoves, ref List<Move> allPseudoLegalMoves) {
            Stopwatch stopwatch = Stopwatch.StartNew();
            while (possibleMoves != 0) {
                var toBitboard = BitOperations.LsbIndexBitboard(possibleMoves);
                BitOperations.del_1st_bit(ref possibleMoves);
                //possibleMoves ^= toBitboard;  // Remove the first bit

                var move = new Move(from: fromBitboard, to: toBitboard, chessboard: this);
                allPseudoLegalMoves.Add(move);
            }
            stopwatch.Stop();
            //Logger.Log(Channel.Benchmark, $"AddAllPossibleMoves {allPseudoLegalMoves.Count} pseudo legal moves in {stopwatch.ElapsedTicks} ns");
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)] not good for performance
        private void AddAllPossibleMovesPawn(Bitboard fromBitboard, Bitboard possibleMoves, ref List<Move> allPseudoLegalMoves) {
            int fromIndex = BitOperations.ToIndex(fromBitboard);  // Compute once

            while (possibleMoves != 0) {
                int toIndex = BitOperations.ToIndex(possibleMoves);
                BitOperations.del_1st_bit(ref possibleMoves);

                // Promotion moves
                if (toIndex < 8 || toIndex >= 56) {
                    bool isCapture = ((1UL << toIndex) & AllPieces) != 0;
                    var promotionArray = isCapture ? Pawn.CapturePromotions : Pawn.QuietPromotions;

                    foreach (var promotionCode in promotionArray) {
                        ushort moveWord = (ushort)((fromIndex << 10) | (toIndex << 4) | (int)promotionCode);
                        allPseudoLegalMoves.Add(new Move(moveWord));
                    }
                }
                else {
                    // Regular pawn moves - determine special code DIRECTLY
                    bool isCapture = ((1UL << toIndex) & AllPieces) != 0;
                    int distance = Math.Abs(fromIndex - toIndex);

                    SpecialMovesCode specialCode;
                    if (isCapture) {
                        specialCode = SpecialMovesCode.Captures;
                    }
                    else if (distance == 16) {
                        specialCode = SpecialMovesCode.DoublePawnPush;
                    }
                    else if ((distance == 7 || distance == 9)) {
                        specialCode = SpecialMovesCode.EpCapture;
                    }
                    else {
                        specialCode = SpecialMovesCode.QuietMoves;
                    }

                    // Create move directly without calling FindSpecialMoveCode
                    ushort moveWord = (ushort)((fromIndex << 10) | (toIndex << 4) | (int)specialCode);
                    allPseudoLegalMoves.Add(new Move(moveWord));
                }
            }
        }

        void GetAllPossiblePieceMoves(int colorIndex, int pieceTypeIndex, ref List<Move> allPseudoLegalMoves) {
            Stopwatch stopwatch = Stopwatch.StartNew();

            var pieceBitboard = Position[colorIndex, pieceTypeIndex].BitboardValue;
            Bitboard possibleMoves;

            switch ((PieceType)pieceTypeIndex) {
                case PieceType.Pawn:
                    while (pieceBitboard != 0) {
                        var fromBitboard = BitOperations.LsbIndexBitboard(pieceBitboard);
                        BitOperations.del_1st_bit(ref pieceBitboard);

                        possibleMoves = Pawn.ComputePossibleMoves(fromBitboard, this, (TurnColor)colorIndex);
                        AddAllPossibleMovesPawn(fromBitboard, possibleMoves, ref allPseudoLegalMoves);
                    }

                    stopwatch.Stop();
                    //Logger.Log(Channel.Benchmark, $"GetAllPossiblePieceMoves {allPseudoLegalMoves.Count} pseudo legal moves for {(PieceType)pieceTypeIndex} in {stopwatch.Elapsed.TotalNanoseconds} ns");
                    break;

                case PieceType.Rook:
                    while (pieceBitboard != 0) {
                        var fromBitboard = BitOperations.LsbIndexBitboard(pieceBitboard);
                        BitOperations.del_1st_bit(ref pieceBitboard);

                        possibleMoves = Rook.ComputePossibleMoves(fromBitboard, this, (TurnColor)colorIndex);
                        stopwatch.Stop();
                        //Logger.Log(Channel.Benchmark, $"GetAllPossiblePieceMoves {allPseudoLegalMoves.Count} pseudo legal moves for {(PieceType)pieceTypeIndex} in {stopwatch.ElapsedTicks} ns");
                        AddAllPossibleMoves(fromBitboard, possibleMoves, ref allPseudoLegalMoves);
                    }

                    stopwatch.Stop();
                    //Logger.Log(Channel.Benchmark, $"GetAllPossiblePieceMoves {allPseudoLegalMoves.Count} pseudo legal moves for {(PieceType)pieceTypeIndex} in {stopwatch.ElapsedTicks} ns");
                    break;

                case PieceType.Knight:
                    while (pieceBitboard != 0) {
                        var fromBitboard = BitOperations.LsbIndexBitboard(pieceBitboard);
                        BitOperations.del_1st_bit(ref pieceBitboard);

                        possibleMoves = Knight.ComputePossibleMoves(fromBitboard, this, (TurnColor)colorIndex);
                        AddAllPossibleMoves(fromBitboard, possibleMoves, ref allPseudoLegalMoves);
                    }

                    stopwatch.Stop();
                    //Logger.Log(Channel.Benchmark, $"GetAllPossiblePieceMoves {allPseudoLegalMoves.Count} pseudo legal moves for {(PieceType)pieceTypeIndex} in {stopwatch.Elapsed.TotalNanoseconds} ns");
                    break;

                case PieceType.Bishop:
                    while (pieceBitboard != 0) {
                        var fromBitboard = BitOperations.LsbIndexBitboard(pieceBitboard);
                        BitOperations.pop_1st_bit(ref pieceBitboard);

                        possibleMoves = Bishop.ComputePossibleMoves(fromBitboard, this, (TurnColor)colorIndex);
                        AddAllPossibleMoves(fromBitboard, possibleMoves, ref allPseudoLegalMoves);
                    }

                    stopwatch.Stop();
                    //Logger.Log(Channel.Benchmark, $"GetAllPossiblePieceMoves {allPseudoLegalMoves.Count} pseudo legal moves for {(PieceType)pieceTypeIndex} in {stopwatch.Elapsed.TotalNanoseconds} ns");
                    break;

                case PieceType.Queen:
                    while (pieceBitboard != 0) {
                        var fromBitboard = BitOperations.LsbIndexBitboard(pieceBitboard);
                        BitOperations.pop_1st_bit(ref pieceBitboard);

                        possibleMoves = Queen.ComputePossibleMoves(fromBitboard, this, (TurnColor)colorIndex);
                        AddAllPossibleMoves(fromBitboard, possibleMoves, ref allPseudoLegalMoves);
                    }

                    stopwatch.Stop();
                    //Logger.Log(Channel.Benchmark, $"GetAllPossiblePieceMoves {allPseudoLegalMoves.Count} pseudo legal moves for {(PieceType)pieceTypeIndex} in {stopwatch.Elapsed.TotalNanoseconds} ns");
                    break;

                case PieceType.King:
                    possibleMoves = King.ComputePossibleAttacks(pieceBitboard, this, (TurnColor)colorIndex);

                    // check if castling is possible
                    if (ShouldCheckCastling()) {
                        possibleMoves |= King.ComputePossibleCastlingMoves(pieceBitboard, this, (TurnColor)colorIndex);
                    }

                    AddAllPossibleMoves(pieceBitboard, possibleMoves, ref allPseudoLegalMoves);

                    stopwatch.Stop();
                    //Logger.Log(Channel.Benchmark, $"GetAllPossiblePieceMoves {allPseudoLegalMoves.Count} pseudo legal moves for {(PieceType)pieceTypeIndex} in {stopwatch.ElapsedTicks} ns");
                    break;

            }

            stopwatch.Stop();
            //Logger.Log(Channel.Benchmark, $"GetAllPossiblePieceMoves {allPseudoLegalMoves.Count} pseudo legal moves for {(PieceType)pieceTypeIndex} in {stopwatch.ElapsedTicks} ns");
        }

        List<Move> GenerateMoves() {
            Stopwatch stopwatch = Stopwatch.StartNew();

            List<Move> allPseudoLegalMoves = new();
            for (int pieceTypeIndex = 0; pieceTypeIndex < 6; pieceTypeIndex++) {
                GetAllPossiblePieceMoves((int)State.TurnColor, pieceTypeIndex, ref allPseudoLegalMoves);
            }

            stopwatch.Stop();
            //sLogger.Log(Channel.Benchmark, $"GenerateMoves {allPseudoLegalMoves.Count} pseudo legal moves in {stopwatch.ElapsedTicks} ns");
            //stopwatch.Stop();
            return allPseudoLegalMoves;
        }

        public List<Move> GenerateLegalMoves() {
            List<Move> LegalMoves = [];
            int nMoves, i;

            List<Move> allPseudoLegalMoves = GenerateMoves();
            nMoves = allPseudoLegalMoves.Count;

            for (i = 0; i < nMoves; i++) {
                Move.MakeMove(this, allPseudoLegalMoves[i]);
                if (!IsInCheck(stateStack.Peek().TurnColor)) {
                    LegalMoves.Add(allPseudoLegalMoves[i]);
                }
                Move.UnmakeMove(this, allPseudoLegalMoves[i]);
            }

            return LegalMoves;
        }

        //check if the king from a color is in check
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsInCheck(TurnColor turncolor) {
            Stopwatch stopwatch = Stopwatch.StartNew();

            var kingBitboard = Position[(int)turncolor, (int)PieceType.King].BitboardValue;
            return IsSquareAttackedByColor(BitOperations.ToSquare(kingBitboard), turncolor ^ TurnColor.Black);
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSquareAttackedByColor(Square square, TurnColor turnColor) {
            int squareIndex = (int)square;
            //Logger.Log(Channel.Debug, $"Checking if square {square} is attacked by {turnColor}");

            // Check pawn attacks
            Bitboard pawns = Position[(int)turnColor, (int)PieceType.Pawn].BitboardValue;
            if ((Pawn.PawnAttackMasks[(int)turnColor ^ 1, squareIndex] & pawns) != 0) {
                //Logger.Log(Channel.Debug, $"Square {square} is attacked by {turnColor} pawn");
                return true;
            }

            // Check knight attacks
            Bitboard knights = Position[(int)turnColor, (int)PieceType.Knight].BitboardValue;
            if ((Knight.KnightAttackMasks[squareIndex] & knights) != 0) {
                //Logger.Log(Channel.Debug, $"Square {square} is attacked by {turnColor} knight");
                return true;
            }

            // Check king attacks
            Bitboard king = Position[(int)turnColor, (int)PieceType.King].BitboardValue;
            if ((King.KingAttackMasks[squareIndex] & king) != 0) {
                //Logger.Log(Channel.Debug, $"Square {square} is attacked by {turnColor} king");
                return true;
            }

            // Check bishop and queen attacks (diagonal)
            //Logger.Log(Channel.Debug, StringHelper.FormatAsChessboard(Bishop.ComputePossibleMoves(BitOperations.ToBitboard(square), this, turnColor)));
            Bitboard bishopsQueens = Position[(int)turnColor, (int)PieceType.Queen].BitboardValue |
                                     Position[(int)turnColor, (int)PieceType.Bishop].BitboardValue;
            //Logger.Log(Channel.Debug, StringHelper.FormatAsChessboard(bishopsQueens));
            if ((Bishop.ComputePossibleMoves(BitOperations.ToBitboard(square), this, turnColor ^ TurnColor.Black) & bishopsQueens) != 0) {
                //Logger.Log(Channel.Debug, $"Square {square} is attacked by {turnColor} bishopQueen");
                return true;
            }

            // Check rook and queen attacks (straight lines)
            Bitboard rooksQueens = Position[(int)turnColor, (int)PieceType.Queen].BitboardValue |
                                   Position[(int)turnColor, (int)PieceType.Rook].BitboardValue;
            if ((Rook.ComputePossibleMoves(BitOperations.ToBitboard(square), this, turnColor ^ TurnColor.Black) & rooksQueens) != 0) {
                //Logger.Log(Channel.Debug, $"Square {square} is attacked by {turnColor} bishopQueen");
                return true;
            }

            //Logger.Log(Channel.Debug, $"Square {square} is not attacked by {turnColor}");
            return false;
        }

        public bool AreAnySquaresAttackedByColor(Square[] squares, TurnColor turnColor) {
            Stopwatch stopwatch = Stopwatch.StartNew();
            foreach (var square in squares) {
                if (IsSquareAttackedByColor(square, turnColor)) {
                    return true;
                }
            }
            stopwatch.Stop();
            //Logger.Log(Channel.Benchmark, $"AreAnySquaresAttackedByColor {squares.Length} squares checked in {stopwatch.ElapsedTicks} ns");
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AreAnySquaresOccupied(Bitboard squares) {
            return (squares & AllPieces) != 0UL;
        }

        public ulong Perft(int depth) {
            //return DrawPerftTree(depth, indent: "");

            if (depth == 0)
                return 1UL;

            ulong nodes = 0;

            List<Move> allPseudoLegalMoves = GenerateMoves(); // 3000 ns
            //stopwatch.Stop();
            //Logger.Log(Channel.Benchmark, $"GenerateMoves {allPseudoLegalMoves.Count} pseudo legal moves in {stopwatch.ElapsedTicks} ns");
            foreach (var move in CollectionsMarshal.AsSpan(allPseudoLegalMoves)) {  // 7500 ns
                Stopwatch stopwatch = Stopwatch.StartNew();
                Move.MakeMove(this, move);
                if (!IsInCheck(stateStack.Peek().TurnColor)) {
                    nodes += Perft(depth - 1);
                }
                Move.UnmakeMove(this, move);
                stopwatch.Stop();
                Logger.Log(Channel.Benchmark, $"Move/ischeck/unmake {move} in {stopwatch.ElapsedTicks} ns");
            }
            //stopwatch.Stop();
            //Logger.Log(Channel.Benchmark, $"Perft {allPseudoLegalMoves.Count} pseudo legal moves in {stopwatch.ElapsedTicks} ns");

            /*for (i = 0; i < nMoves; i++) {
                Move.MakeMove(this, allPseudoLegalMoves[i]);
                bool isInCheckPerft = IsInCheck(stateStack.Peek().TurnColor);
                if (!isInCheckPerft) {
                    nodes += Perft(depth - 1);
                }
                Move.UnmakeMove(this, allPseudoLegalMoves[i]);
            }*/

            return nodes;
        }

        public ulong DrawPerftTree(int depth, string indent = "") {
            if (depth == 0) {
                Logger.Log(Channel.Debug, $"{indent}└─ leaf: 1");
                return 1UL;
            }

            List<Move> allPseudoLegalMoves = GenerateMoves();
            //foreach (var move in allPseudoLegalMoves) {
            //    Logger.Log(Channel.Debug, $"{indent}└─ {State.TurnColor} {move} {(SpecialMovesCode)move.SpecialCode}");
            //}
            ulong totalNodes = 0;

            for (int i = 0; i < allPseudoLegalMoves.Count; i++) {
                var move = allPseudoLegalMoves[i];
                bool isLastMove = (i == allPseudoLegalMoves.Count - 1);
                string branch = isLastMove ? "└─" : "├─";
                string newIndent = indent + (isLastMove ? "   " : "│  ");

                Logger.Log(Channel.Debug, $"{indent}{branch} {State.TurnColor} {move} {(SpecialMovesCode)move.SpecialCode}");

                Move.MakeMove(this, move);
                bool isInCheck = IsInCheck(stateStack.Peek().TurnColor);

                if (!isInCheck) {
                    ulong subtreeNodes = DrawPerftTree(depth - 1, newIndent);
                    //Logger.Log(Channel.Debug, $"{newIndent}└─ nodes: {subtreeNodes}");
                    totalNodes += subtreeNodes;
                }
                else {
                    Logger.Log(Channel.Debug, $"{newIndent}└─ illegal (in check)");
                }

                Move.UnmakeMove(this, move);
            }
            Logger.Log(Channel.Debug, $"└─ nodes: {totalNodes}");
            return totalNodes;
        }

        public ulong Perftree(int depth) {
            int nMoves, i;
            ulong nodes = 0;

            if (depth == 0)
                return 1UL;

            List<Move> allPseudoLegalMoves = GenerateMoves();
            nMoves = allPseudoLegalMoves.Count;

            for (i = 0; i < nMoves; i++) {
                Move.MakeMove(this, allPseudoLegalMoves[i]);
                if (!IsInCheck(stateStack.Peek().TurnColor)) {
                    ulong moveNodes = Perft(depth - 1);  // Store individual result
                    nodes += moveNodes;                   // Add to total
                    Move.UnmakeMove(this, allPseudoLegalMoves[i]);
                    Console.WriteLine($"{allPseudoLegalMoves[i]} {moveNodes}"); // Print individual
                }
                else {
                    Move.UnmakeMove(this, allPseudoLegalMoves[i]);
                    Console.WriteLine($"{allPseudoLegalMoves[i]} 0"); // Illegal move
                }
            }

            Console.WriteLine(); // Empty line before total
            Console.WriteLine(nodes); // Print total
            return nodes;
        }

        public void PushUci(string move) {
            Move.MakeMove(this, Move.DecodeUciMove(this, move));
        }
    }
}