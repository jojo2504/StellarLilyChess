using System.Runtime.CompilerServices;
using ChessEngine.Pieces;
using ChessEngine.Utils;
using static ChessEngine.Move;

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

    public enum BSquare : Bitboard {
        A1 = 1UL << 0, B1 = 1UL << 1, C1 = 1UL << 2, D1 = 1UL << 3, E1 = 1UL << 4, F1 = 1UL << 5, G1 = 1UL << 6, H1 = 1UL << 7,
        A2 = 1UL << 8, B2 = 1UL << 9, C2 = 1UL << 10, D2 = 1UL << 11, E2 = 1UL << 12, F2 = 1UL << 13, G2 = 1UL << 14, H2 = 1UL << 15,
        A3 = 1UL << 16, B3 = 1UL << 17, C3 = 1UL << 18, D3 = 1UL << 19, E3 = 1UL << 20, F3 = 1UL << 21, G3 = 1UL << 22, H3 = 1UL << 23,
        A4 = 1UL << 24, B4 = 1UL << 25, C4 = 1UL << 26, D4 = 1UL << 27, E4 = 1UL << 28, F4 = 1UL << 29, G4 = 1UL << 30, H4 = 1UL << 31,
        A5 = 1UL << 32, B5 = 1UL << 33, C5 = 1UL << 34, D5 = 1UL << 35, E5 = 1UL << 36, F5 = 1UL << 37, G5 = 1UL << 38, H5 = 1UL << 39,
        A6 = 1UL << 40, B6 = 1UL << 41, C6 = 1UL << 42, D6 = 1UL << 43, E6 = 1UL << 44, F6 = 1UL << 45, G6 = 1UL << 46, H6 = 1UL << 47,
        A7 = 1UL << 48, B7 = 1UL << 49, C7 = 1UL << 50, D7 = 1UL << 51, E7 = 1UL << 52, F7 = 1UL << 53, G7 = 1UL << 54, H7 = 1UL << 55,
        A8 = 1UL << 56, B8 = 1UL << 57, C8 = 1UL << 58, D8 = 1UL << 59, E8 = 1UL << 60, F8 = 1UL << 61, G8 = 1UL << 62, H8 = 1UL << 63
    }

    public enum PieceType {
        Pawn,
        Knight,
        Bishop,
        Rook,
        Queen,
        King,
        None
    }

    public enum TurnColor {
        White,
        Black
    }

    public class Chessboard {
        public Bitboard[,] Position = new Bitboard[2, 6];
        public Bitboard AllWhitePieces = 0UL;
        public Bitboard AllBlackPieces = 0UL;
        public Bitboard AllPieces => AllWhitePieces | AllBlackPieces;

        public State State = new();
        public const ushort MaxPly = 2048;
        public State[] stateStack = new State[MaxPly];
        public ushort plyIndex = 0;

        public ref Bitboard WhitePawns => ref Position[(int)TurnColor.White, (int)PieceType.Pawn];
        public ref Bitboard WhiteRooks => ref Position[(int)TurnColor.White, (int)PieceType.Rook];
        public ref Bitboard WhiteKnights => ref Position[(int)TurnColor.White, (int)PieceType.Knight];
        public ref Bitboard WhiteBishops => ref Position[(int)TurnColor.White, (int)PieceType.Bishop];
        public ref Bitboard WhiteQueens => ref Position[(int)TurnColor.White, (int)PieceType.Queen];
        public ref Bitboard WhiteKing => ref Position[(int)TurnColor.White, (int)PieceType.King];
        public ref Bitboard BlackPawns => ref Position[(int)TurnColor.Black, (int)PieceType.Pawn];
        public ref Bitboard BlackRooks => ref Position[(int)TurnColor.Black, (int)PieceType.Rook];
        public ref Bitboard BlackKnights => ref Position[(int)TurnColor.Black, (int)PieceType.Knight];
        public ref Bitboard BlackBishops => ref Position[(int)TurnColor.Black, (int)PieceType.Bishop];
        public ref Bitboard BlackQueens => ref Position[(int)TurnColor.Black, (int)PieceType.Queen];
        public ref Bitboard BlackKing => ref Position[(int)TurnColor.Black, (int)PieceType.King];

        public Chessboard(string fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1") {
            ParseFEN(fen);

            // init game state not in fen
            State.Checkmated = false;
            State.Stalemated = false;
            stateStack[plyIndex++] = State; // halfmove 0
        }

        void ParseFEN(string fen) {
            var pieceSetters = new Dictionary<char, Action<int>> {
                { 'P', idx => {
                    UpdatePieceBitboard(ref WhitePawns, 1UL << idx, this, TurnColor.White, PieceType.Pawn);
                }},
                { 'N', idx => {
                    UpdatePieceBitboard(ref WhiteKnights, 1UL << idx, this, TurnColor.White, PieceType.Knight);
                }},
                { 'B', idx => {
                    UpdatePieceBitboard(ref WhiteBishops, 1UL << idx, this, TurnColor.White, PieceType.Bishop);
                }},
                { 'R', idx => {
                    UpdatePieceBitboard(ref WhiteRooks, 1UL << idx, this, TurnColor.White, PieceType.Rook);
                }},
                { 'Q', idx => {
                    UpdatePieceBitboard(ref WhiteQueens, 1UL << idx, this, TurnColor.White, PieceType.Queen);
                }},
                { 'K', idx => {
                    UpdatePieceBitboard(ref WhiteKing, 1UL << idx, this, TurnColor.White, PieceType.King);
                }},
                { 'p', idx => {
                    UpdatePieceBitboard(ref BlackPawns, 1UL << idx, this, TurnColor.Black, PieceType.Pawn);
                }},
                { 'n', idx => {
                    UpdatePieceBitboard(ref BlackKnights, 1UL << idx, this, TurnColor.Black, PieceType.Knight);
                }},
                { 'b', idx => {
                    UpdatePieceBitboard(ref BlackBishops, 1UL << idx, this, TurnColor.Black, PieceType.Bishop);
                }},
                { 'r', idx => {
                    UpdatePieceBitboard(ref BlackRooks, 1UL << idx, this, TurnColor.Black, PieceType.Rook);
                }},
                { 'q', idx => {
                    UpdatePieceBitboard(ref BlackQueens, 1UL << idx, this, TurnColor.Black, PieceType.Queen);
                }},
                { 'k', idx => {
                    UpdatePieceBitboard(ref BlackKing, 1UL << idx, this, TurnColor.Black, PieceType.King);
                }},
            };

            var parts = fen.Split(" ");

            var piecePlacement = parts[0];
            var turnColor = parts[1];
            var castlingAbility = parts[2];
            var epSquare = parts[3].ToUpper();
            var halfMove = int.TryParse(parts[4], out var halfMoveValue) ? halfMoveValue : 0;
            var fullMove = int.TryParse(parts[5], out var fullMoveValue) ? fullMoveValue : 0;

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

            State.ZobristHashKey ^= ZobristHashing.ComputeCastlingRightsHash(State);

            if (Enum.TryParse<Square>(epSquare, out var square)) {
                State.EnPassantSquare = square;
                ZobristHashing.enPassantFileIndex = (int)square % 8;
                Console.WriteLine($"En passant file index: {ZobristHashing.enPassantFileIndex}");
                State.ZobristHashKey ^= ZobristHashing.enPassantFile[ZobristHashing.enPassantFileIndex];
            }
            else {
                State.EnPassantSquare = null;
            }

            State.HalfMoveClock = Convert.ToInt32(halfMove);
            State.FullMoveNumber = Convert.ToInt32(fullMove);

            var ranks = piecePlacement.Split("/");
            var overallIndexSquare = 0;
            foreach (var rank in ranks) {
                for (int i = rank.Length - 1; i >= 0; i--) {
                    var letter = rank[i];
                    if (char.IsDigit(letter)) {
                        overallIndexSquare += letter - '0';
                    }
                    else if (pieceSetters.TryGetValue(letter, out var setPiece)) {
                        setPiece(63 - overallIndexSquare);
                        overallIndexSquare++;
                    }
                }
            }
        }

        public override string ToString() {
            // TODO : rework this absolute mess
            var ChessBoardState = new string('0', 64);
            ChessBoardState = StringHelper.MergeStrings(StringHelper.ToBinary(WhitePawns).Replace('1', 'P'), ChessBoardState);
            ChessBoardState = StringHelper.MergeStrings(StringHelper.ToBinary(WhiteRooks).Replace('1', 'R'), ChessBoardState);
            ChessBoardState = StringHelper.MergeStrings(StringHelper.ToBinary(WhiteKnights).Replace('1', 'N'), ChessBoardState);
            ChessBoardState = StringHelper.MergeStrings(StringHelper.ToBinary(WhiteBishops).Replace('1', 'B'), ChessBoardState);
            ChessBoardState = StringHelper.MergeStrings(StringHelper.ToBinary(WhiteQueens).Replace('1', 'Q'), ChessBoardState);
            ChessBoardState = StringHelper.MergeStrings(StringHelper.ToBinary(WhiteKing).Replace('1', 'K'), ChessBoardState);
            ChessBoardState = StringHelper.MergeStrings(StringHelper.ToBinary(BlackPawns).Replace('1', 'p'), ChessBoardState);
            ChessBoardState = StringHelper.MergeStrings(StringHelper.ToBinary(BlackRooks).Replace('1', 'r'), ChessBoardState);
            ChessBoardState = StringHelper.MergeStrings(StringHelper.ToBinary(BlackKnights).Replace('1', 'n'), ChessBoardState);
            ChessBoardState = StringHelper.MergeStrings(StringHelper.ToBinary(BlackBishops).Replace('1', 'b'), ChessBoardState);
            ChessBoardState = StringHelper.MergeStrings(StringHelper.ToBinary(BlackQueens).Replace('1', 'q'), ChessBoardState);
            ChessBoardState = StringHelper.MergeStrings(StringHelper.ToBinary(BlackKing).Replace('1', 'k'), ChessBoardState);
            return StringHelper.FormatAsChessboard(ChessBoardState.Replace('0', '.'));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ShouldCheckCastling() {
            // Quick checks before expensive castling computation
            return (State.TurnColor == TurnColor.White && (State.CanWhiteKingCastle || State.CanWhiteQueenCastle))
                || (State.TurnColor == TurnColor.Black && (State.CanBlackKingCastle || State.CanBlackQueenCastle));
        }

        private void AddAllPossibleMovesKing(Bitboard fromBitboard, Bitboard possibleMoves, Span<Move> allPseudoLegalMoves, PieceType pieceType, ref int moveCount) {
            var fromBitboardIndex = BitOperations.ToIndex(fromBitboard);  // Compute once
            var from = fromBitboardIndex << 10;

            while (possibleMoves != 0) {
                var toBitboard = BitOperations.LsbIndexBitboard(possibleMoves);
                BitOperations.del_1st_bit(ref possibleMoves);

                var toBitboardIndex = BitOperations.ToIndex(toBitboard);
                ushort word = (ushort)(from | (toBitboardIndex << 4));
                if (Math.Abs(fromBitboardIndex - toBitboardIndex) == 2) {
                    if (toBitboard == (Bitboard)BSquare.C1 || toBitboard == (Bitboard)BSquare.C8) {
                        word |= (byte)MoveKind.QueenCastle;
                    }
                    else if (toBitboard == (Bitboard)BSquare.G1 || toBitboard == (Bitboard)BSquare.G8) {
                        word |= (byte)MoveKind.KingCastle;
                    }
                }

                else if ((toBitboard & AllPieces) != 0) {
                    word |= (ushort)MoveKind.Captures;
                }

                var move = new Move(word: word, pieceType: pieceType);
                allPseudoLegalMoves[moveCount++] = move;
            }
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)] not good for performance
        private void AddAllPossibleMovesPawn(Bitboard fromBitboard, Bitboard possibleMoves, Span<Move> allPseudoLegalMoves, ref int moveCount) {
            int fromBitboardIndex = BitOperations.ToIndex(fromBitboard);  // Compute once
            var from = fromBitboardIndex << 10;

            while (possibleMoves != 0) {
                int toIndex = BitOperations.ToIndex(possibleMoves);
                BitOperations.del_1st_bit(ref possibleMoves);

                ushort word = (ushort)(from | (toIndex << 4));
                // Promotion moves
                if (toIndex < 8 || toIndex >= 56) {
                    bool isCapture = ((1UL << toIndex) & AllPieces) != 0;
                    var promotionArray = isCapture ? Pawn.CapturePromotions : Pawn.QuietPromotions;

                    foreach (var promotionCode in promotionArray) {
                        allPseudoLegalMoves[moveCount++] = new Move((ushort)(word | (byte)promotionCode), PieceType.Pawn);
                    }
                }
                else {
                    // Regular pawn moves - determine special code DIRECTLY
                    bool isCapture = ((1UL << toIndex) & AllPieces) != 0;
                    int distance = Math.Abs(fromBitboardIndex - toIndex);

                    MoveKind specialCode;
                    if (isCapture) {
                        specialCode = MoveKind.Captures;
                    }
                    else if (distance == 16) {
                        specialCode = MoveKind.DoublePawnPush;
                    }
                    else if (distance == 7 || distance == 9) {
                        specialCode = MoveKind.EpCapture;
                    }
                    else {
                        specialCode = MoveKind.QuietMoves;
                    }

                    // Create move directly without calling FindSpecialMoveCode
                    word |= (byte)specialCode;
                    allPseudoLegalMoves[moveCount++] = new Move(word, PieceType.Pawn);
                }
            }
        }

        private void AddAllPossibleMoves(Bitboard fromBitboard, Bitboard possibleMoves, Span<Move> allPseudoLegalMoves, PieceType pieceType,ref int moveCount) {
            int fromBitboardIndex = BitOperations.ToIndex(fromBitboard);  // Compute once
            var from = fromBitboardIndex << 10;

            while (possibleMoves != 0) {
                var toBitboard = BitOperations.LsbIndexBitboard(possibleMoves);
                BitOperations.del_1st_bit(ref possibleMoves);

                ushort word = (ushort)(from | (BitOperations.ToIndex(toBitboard) << 4));
                if ((toBitboard & AllPieces) != 0) {
                    word |= (ushort)MoveKind.Captures;
                }

                var move = new Move(word, pieceType: pieceType);
                allPseudoLegalMoves[moveCount++] = move;
            }
        }

        void GetAllPossiblePieceMoves(int colorIndex, int pieceTypeIndex, Span<Move> allPseudoLegalMoves, ref int moveCount) {
            //Console.WriteLine("{0} {1} {2}", colorIndex, pieceTypeIndex, moveCount);
            var pieceBitboard = Position[colorIndex, pieceTypeIndex];
            Bitboard possibleMoves;

            switch ((PieceType)pieceTypeIndex) {
                case PieceType.Pawn:
                    while (pieceBitboard != 0) {
                        var fromBitboard = BitOperations.LsbIndexBitboard(pieceBitboard);
                        BitOperations.del_1st_bit(ref pieceBitboard);

                        possibleMoves = Pawn.ComputePossibleMoves(fromBitboard, this, (TurnColor)colorIndex);
                        AddAllPossibleMovesPawn(fromBitboard, possibleMoves, allPseudoLegalMoves, ref moveCount);
                    }

                    break;

                case PieceType.Rook:
                    while (pieceBitboard != 0) {
                        var fromBitboard = BitOperations.LsbIndexBitboard(pieceBitboard);
                        BitOperations.del_1st_bit(ref pieceBitboard);

                        possibleMoves = Rook.ComputePossibleMoves(fromBitboard, this, (TurnColor)colorIndex);
                        AddAllPossibleMoves(fromBitboard, possibleMoves, allPseudoLegalMoves, PieceType.Rook, ref moveCount);
                    }

                    break;

                case PieceType.Knight:
                    while (pieceBitboard != 0) {
                        var fromBitboard = BitOperations.LsbIndexBitboard(pieceBitboard);
                        BitOperations.del_1st_bit(ref pieceBitboard);

                        possibleMoves = Knight.ComputePossibleMoves(fromBitboard, this, (TurnColor)colorIndex);
                        AddAllPossibleMoves(fromBitboard, possibleMoves, allPseudoLegalMoves, PieceType.Knight, ref moveCount);
                    }

                    break;

                case PieceType.Bishop:
                    while (pieceBitboard != 0) {
                        var fromBitboard = BitOperations.LsbIndexBitboard(pieceBitboard);
                        BitOperations.pop_1st_bit(ref pieceBitboard);

                        possibleMoves = Bishop.ComputePossibleMoves(fromBitboard, this, (TurnColor)colorIndex);
                        AddAllPossibleMoves(fromBitboard, possibleMoves, allPseudoLegalMoves, PieceType.Bishop, ref moveCount);
                    }

                    break;

                case PieceType.Queen:
                    while (pieceBitboard != 0) {
                        var fromBitboard = BitOperations.LsbIndexBitboard(pieceBitboard);
                        BitOperations.pop_1st_bit(ref pieceBitboard);

                        possibleMoves = Queen.ComputePossibleMoves(fromBitboard, this, (TurnColor)colorIndex);
                        AddAllPossibleMoves(fromBitboard, possibleMoves, allPseudoLegalMoves, PieceType.Queen, ref moveCount);
                    }

                    break;

                case PieceType.King:
                    possibleMoves = King.ComputePossibleAttacks(pieceBitboard, this, (TurnColor)colorIndex);

                    // check if castling is possible
                    if (ShouldCheckCastling()) {
                        possibleMoves |= King.ComputePossibleCastlingMoves(pieceBitboard, this, (TurnColor)colorIndex);
                    }

                    AddAllPossibleMovesKing(pieceBitboard, possibleMoves, allPseudoLegalMoves, PieceType.King, ref moveCount);

                    break;
            }
        }

        //check if the king from a color is in check
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsInCheck(TurnColor turnColor) {
            var kingBitboard = Position[(int)turnColor, (int)PieceType.King];
            return IsSquareAttackedByColor(kingBitboard, turnColor ^ TurnColor.Black);

            // only works if the king is not already in check, need to fond a solution for that or drop this optimization
            // this skips most of the maskattacks  pieces because only the bishop, rook and queen can give check from a distance
            /*
            if (lastMove.pieceType == PieceType.King)
                return IsSquareAttackedByColor(kingBitboard, turnColor ^ TurnColor.Black);

            var opponantTurnColor = turnColor ^ TurnColor.Black;
            int squareIndex = BitOperations.ToIndex(kingBitboard);
            var colorIndex = (int)opponantTurnColor;
            // Check rook and queen attacks (straight lines)
            Bitboard rooksQueens = Position[colorIndex, (int)PieceType.Queen] |
                                   Position[colorIndex, (int)PieceType.Rook];
            if (((SuperPiece.RookAttacks[squareIndex] & rooksQueens) != 0)
                && (Rook.ComputePossibleAttacks(kingBitboard, this, opponantTurnColor ^ TurnColor.Black) & rooksQueens) != 0) {
                return true;
            }

            Bitboard bishopsQueens = Position[colorIndex, (int)PieceType.Queen] |
                                    Position[colorIndex, (int)PieceType.Bishop];
            // Check bishop and queen attacks (diagonal)
            if (((SuperPiece.BishopAttacks[squareIndex] & bishopsQueens) != 0)
                && ((Bishop.ComputePossibleAttacks(kingBitboard, this, opponantTurnColor ^ TurnColor.Black) & bishopsQueens) != 0)) {
                return true;
            }

            return false;
            */
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public bool IsSquareAttackedByColor(in Bitboard square, TurnColor turnColor) {
            int squareIndex = BitOperations.ToIndex(square);
            var colorIndex = (int)turnColor;

            // Check knight attacks
            Bitboard knights = Position[colorIndex, (int)PieceType.Knight];
            if ((Knight.KnightAttackMasks[squareIndex] & knights) != 0) {
                return true;
            }

            // Check rook and queen attacks (straight lines)
            Bitboard rooksQueens = Position[colorIndex, (int)PieceType.Queen] |
                                   Position[colorIndex, (int)PieceType.Rook];
            if (((SuperPiece.RookAttacks[squareIndex] & rooksQueens) != 0)
                && (Rook.ComputePossibleAttacks(square, this, turnColor ^ TurnColor.Black) & rooksQueens) != 0) {
                return true;
            }

            Bitboard bishopsQueens = Position[colorIndex, (int)PieceType.Queen] |
                                    Position[colorIndex, (int)PieceType.Bishop];
            // Check bishop and queen attacks (diagonal)
            if (((SuperPiece.BishopAttacks[squareIndex] & bishopsQueens) != 0)
                && ((Bishop.ComputePossibleAttacks(square, this, turnColor ^ TurnColor.Black) & bishopsQueens) != 0)) {
                return true;
            }

            // Check pawn attacks
            Bitboard pawns = Position[colorIndex, (int)PieceType.Pawn];
            if ((Pawn.PawnAttackMasks[colorIndex ^ 1, squareIndex] & pawns) != 0) {
                return true;
            }

            // Check king attacks
            Bitboard king = Position[colorIndex, (int)PieceType.King];
            if ((King.KingAttackMasks[squareIndex] & king) != 0) {
                return true;
            }

            return false;
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AreAnySquaresAttackedByColor(Square[] squares, TurnColor turnColor) {
            foreach (var square in squares) {
                if (IsSquareAttackedByColor(BitOperations.ToBitboard(square), turnColor)) {
                    return true;
                }
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AreAnySquaresOccupied(Bitboard squares) {
            return (squares & AllPieces) != 0UL;
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GenerateMoves(Span<Move> allPseudoLegalMoves) {
            int moveCount = 0;
            for (int pieceTypeIndex = 0; pieceTypeIndex < 6; pieceTypeIndex++) {
                GetAllPossiblePieceMoves((int)State.TurnColor, pieceTypeIndex, allPseudoLegalMoves, ref moveCount);
            }

            return moveCount;
        }

        public int GenerateLegalCaptureMoves(Span<Move> LegalMoves) {
            int nMoves, i;
            int lMoves = 0;

            Span<Move> allPseudoLegalMoves = stackalloc Move[256];
            nMoves = GenerateMoves(allPseudoLegalMoves);
            int nCapMoves = 0;
            for (i = 0; i < nMoves; i++) {
                if ((allPseudoLegalMoves[i].bitboardTo & AllPieces) != 0) {
                    MakeMove(this, allPseudoLegalMoves[i]);
                    if (!IsInCheck(stateStack[plyIndex].TurnColor)) {
                        LegalMoves[lMoves++] = allPseudoLegalMoves[i];
                        nCapMoves++;
                    }
                    UnmakeMove(this, allPseudoLegalMoves[i]);
                }
            }
            return nCapMoves;
        }

        public int GenerateLegalMoves(Span<Move> LegalMoves) {
            int nMoves, i;
            int lMoves = 0;
            Span<Move> allPseudoLegalMoves = stackalloc Move[256];

            nMoves = GenerateMoves(allPseudoLegalMoves);
            for (i = 0; i < nMoves; i++) {
                MakeMove(this, allPseudoLegalMoves[i]);
                if (!IsInCheck(stateStack[plyIndex].TurnColor)) {
                    LegalMoves[lMoves++] = allPseudoLegalMoves[i];
                }
                UnmakeMove(this, allPseudoLegalMoves[i]);
            }

            return lMoves;
        }

        public ulong Perft(int depth) {
            //return DrawPerftTree(depth, indent: "");
            if (depth == 0)
                return 1UL;

            Span<Move> allPseudoLegalMoves = stackalloc Move[256];
            ulong nodes = 0;
            int n_moves, i;

            n_moves = GenerateMoves(allPseudoLegalMoves);
            for (i = 0; i < n_moves; i++) {
                MakeMove(this, allPseudoLegalMoves[i]);
                if (!IsInCheck(stateStack[plyIndex].TurnColor)) {
                    nodes += Perft(depth - 1);
                }
                UnmakeMove(this, allPseudoLegalMoves[i]);
            }

            return nodes;
        }

        public ulong DrawPerftTree(int depth, string indent = "") {
            if (depth == 0) {
                Logger.Log(Channel.Debug, $"{indent}└─ leaf: 1");
                return 1UL;
            }

            Span<Move> allPseudoLegalMoves = stackalloc Move[256];
            ulong totalNodes = 0;
            int n_moves, i;

            n_moves = GenerateMoves(allPseudoLegalMoves);
            for (i = 0; i < n_moves; i++) {
                var move = allPseudoLegalMoves[i];
                bool isLastMove = (i == allPseudoLegalMoves.Length - 1);
                string branch = isLastMove ? "└─" : "├─";
                string newIndent = indent + (isLastMove ? "   " : "│  ");

                Logger.Log(Channel.Debug, $"{indent}{branch} {State.TurnColor} {move} {(MoveKind)move.MoveKindCode}");

                Move.MakeMove(this, move);
                bool isInCheck = IsInCheck(stateStack[plyIndex].TurnColor);
                if (!isInCheck) {
                    //Logger.Log(Channel.Debug, "AllWhitePieces", StringHelper.FormatAsChessboard(AllWhitePieces));
                    //Logger.Log(Channel.Debug, "AllBlackPieces", StringHelper.FormatAsChessboard(AllBlackPieces));
                    //Logger.Log(Channel.Debug, "AllPieces", StringHelper.FormatAsChessboard(AllPieces));
                    Logger.Log(Channel.Debug, this);
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
            if (depth == 0)
                return 1UL;

            Span<Move> allPseudoLegalMoves = stackalloc Move[256];
            ulong nodes = 0;
            int nMoves, i;

            nMoves = GenerateMoves(allPseudoLegalMoves);

            for (i = 0; i < nMoves; i++) {
                Move.MakeMove(this, allPseudoLegalMoves[i]);
                if (!IsInCheck(stateStack[plyIndex - 1].TurnColor)) {
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
    }
}