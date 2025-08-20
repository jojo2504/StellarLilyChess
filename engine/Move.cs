using ChessEngine.Utils;
using ChessEngine.Utils.Logging;
using Bitboard = ulong;
using ChessEngine.Pieces;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ChessEngine {

    public readonly struct Move {
        public readonly ushort word;

        public Move(ushort word) {
            this.word = word;
        }

        public Move(Bitboard from, Bitboard to, Chessboard chessboard) {
            word = (ushort)((BitOperations.ToIndex(from) << 10) | (BitOperations.ToIndex(to) << 4));
            word |= FindSpecialMoveCode(from, to, chessboard);
        }

        public Square From {
            get {
                return (Square)((word & 0xFC00) >> 10);
            }
        }

        public Square To {
            get {
                return (Square)((word & 0x3F0) >> 4);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Bitboard FromBitboard() => 1UL << ((word & 0xFC00) >> 10);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Bitboard ToBitboard() => 1UL << ((word & 0x3F0) >> 4);

        public byte SpecialCode {
            get {
                return (byte)(word & 0xF);
            }
        }

        public bool CASTLE_FLAG {
            get {
                // return true if the 2th bit of the nibble is 1 and the 4th is 0
                var flag = ((word >> 1) & 1) == 1 && !PROMOTION_FLAG;
                return flag;
            }
        }

        public bool CAPTURE_FLAG {
            get {
                // return true if the 3th bit of the nibble is 1
                return ((word >> 2) & 1) == 1;
            }
        }

        public bool PROMOTION_FLAG {
            get {
                // return true if the 4th bit of the nibble is 1
                return ((word >> 3) & 1) == 1;
            }
        }

        //example: A1A2
        public override string ToString() {
            char promoKey;
            var SpecialCode = this.SpecialCode;
            if (CAPTURE_FLAG)
                promoKey = Pawn.PromotionDict.FirstOrDefault(x => x.Value == (byte)(SpecialMovesCode)SpecialCode - 4).Key;
            else
                promoKey = Pawn.PromotionDict.FirstOrDefault(x => x.Value == (byte)(SpecialMovesCode)SpecialCode).Key;
            if (promoKey == '\0')
                return $"{From}{To}".ToLower();
            return $"{From}{To}{promoKey}".ToLower();
        }

        //if the code is not 0, then it's an irreversible move, except for regular pawn push
        public enum SpecialMovesCode : byte {
            QuietMoves = 0,
            DoublePawnPush = 1,
            KingCastle = 2,
            QueenCastle = 3,
            Captures = 4,
            EpCapture = 5,
            KnightPromotion = 8,
            BishopPromotion = 9,
            RookPromotion = 10,
            QueenPromotion = 11,
            KnightPromotionCapture = 12,
            BishopPromotionCapture = 13,
            RookPromotionCapture = 14,
            QueenPromotionCapture = 15
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<string> GetUcis(Bitboard possibleMoves, Bitboard fromSquareBitboard) {
            List<string> ucis = new();

            // Compute once outside loop
            string fromSquareStr = BitOperations.ToSquare(fromSquareBitboard).ToString().ToLower();

            while (possibleMoves != 0) {
                int lsbIndex = BitOperations.pop_1st_bit(ref possibleMoves);
                string toSquareStr = ((Square)lsbIndex).ToString().ToLower();
                ucis.Add(fromSquareStr + toSquareStr);
            }
            return ucis;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UpdatePieceBitboard(ref Bitboard bitboard, Bitboard from, Bitboard to, Chessboard chessboard, TurnColor turnColor) {
            bitboard ^= from ^ to;
            if (turnColor == TurnColor.White) {
                chessboard.AllWhitePieces ^= from ^ to;
            }
            else {
                chessboard.AllBlackPieces ^= from ^ to;
            }
            chessboard.AllPieces ^= from ^ to;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UpdatePieceBitboard(ref Bitboard bitboard, Bitboard square, Chessboard chessboard, TurnColor turnColor) {
            bitboard ^= square;
            if (turnColor == TurnColor.White) {
                chessboard.AllWhitePieces ^= square;
            }
            else {
                chessboard.AllBlackPieces ^= square;
            }
            chessboard.AllPieces ^= square;
        }

        /// <summary>
        /// used by UCI to decode a move from a string when played from the GUI, do not touch the pawn logic here
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="chessboard"></param>
        /// <param name="promotionChar"></param>
        /// <returns></returns>
        private static byte FindSpecialMoveCode(Bitboard from, Bitboard to, Chessboard chessboard, char? promotionChar = null) {
            var fromSquare = BitOperations.ToSquare(from);
            var toSquare = BitOperations.ToSquare(to);

            byte smc;
            if (promotionChar.HasValue) {
                // if promotionChar is not null, then it's a promotion move
                smc = Pawn.PromotionDict[(char)promotionChar];
                Logger.Log(Channel.Debug, $"promotion move {fromSquare} to {toSquare} with promotion {promotionChar}");
            }
            else {
                smc = (byte)SpecialMovesCode.QuietMoves;
            }
            // find the special move code:
            for (int pieceTypeIndex = 0; pieceTypeIndex < 6; pieceTypeIndex++) {
                if ((chessboard.Position[(int)chessboard.State.TurnColor, pieceTypeIndex] & from) != 0) {
                    if ((to & chessboard.AllPieces) != 0) {
                        smc = (byte)SpecialMovesCode.Captures;
                        if (promotionChar.HasValue) {
                            smc += Pawn.PromotionDict[(char)promotionChar];
                        }
                    }
                    else {
                        switch ((PieceType)pieceTypeIndex) {
                            case PieceType.Pawn:
                                // DoublePawnPush or EpCapture
                                if (Math.Abs((int)fromSquare - (int)toSquare) == 16) {
                                    smc = (byte)SpecialMovesCode.DoublePawnPush;
                                }
                                var distance = Math.Abs((int)fromSquare - (int)toSquare);
                                if ((distance == 7 || distance == 9) && ((to & chessboard.AllPieces) == 0)) {
                                    smc = (byte)SpecialMovesCode.EpCapture;
                                }
                                break;

                            case PieceType.King:
                                // castle
                                if (Math.Abs((int)fromSquare - (int)toSquare) == 2) {
                                    if (toSquare == Square.C1 || toSquare == Square.C8) {
                                        smc = (byte)SpecialMovesCode.QueenCastle;
                                    }
                                    else if (toSquare == Square.G1 || toSquare == Square.G8) {
                                        smc = (byte)SpecialMovesCode.KingCastle;
                                    }
                                }
                                break;
                        }
                    }
                    break;
                }
            }
            //Logger.Log((SpecialMovesCode)smc);
            return smc;
        }

        public static Move DecodeUciMove(Chessboard chessboard, string uciMove) {
            char? promotionChar = null;
            if (uciMove.Length == 5) promotionChar = uciMove[^1];

            var fromSquare = (Square)((uciMove[1] - '1') * 8 + (uciMove[0] - 'a'));
            var toSquare = (Square)((uciMove[3] - '1') * 8 + (uciMove[2] - 'a'));

            var fromBitboard = BitOperations.ToBitboard(fromSquare);
            var toBitboard = BitOperations.ToBitboard(toSquare);

            var word = (ushort)((ushort)(((int)fromSquare) << 10) | (ushort)(((int)toSquare) << 4));

            // Determine the special move code
            word |= FindSpecialMoveCode(fromBitboard, toBitboard, chessboard, promotionChar);

            return new Move(word);
        }

        public static void MakeMove(Chessboard chessboard, Move move) {
            var bitboardFrom = move.FromBitboard();
            var bitboardTo = move.ToBitboard();

            // precheck castling
            if (move.CASTLE_FLAG) {
                chessboard.stateStack[++chessboard.plyIndex] = chessboard.State;
                chessboard.State.HalfMoveClock++;

                if (chessboard.State.TurnColor == TurnColor.White) {
                    UpdatePieceBitboard(ref chessboard.WhiteKing, bitboardFrom, bitboardTo, chessboard, TurnColor.White); // move king

                    chessboard.State.CanWhiteKingCastle = false;
                    chessboard.State.CanWhiteQueenCastle = false;
                }
                else {
                    UpdatePieceBitboard(ref chessboard.BlackKing, bitboardFrom, bitboardTo, chessboard, TurnColor.Black); // move king

                    chessboard.State.CanBlackKingCastle = false;
                    chessboard.State.CanBlackQueenCastle = false;
                }

                //precheck castle => jump the rook over the king
                if (move.SpecialCode == (int)SpecialMovesCode.KingCastle) {
                    if (chessboard.State.TurnColor == TurnColor.White) {
                        UpdatePieceBitboard(ref chessboard.WhiteRooks, BitOperations.ToBitboard(Square.H1), BitOperations.ToBitboard(Square.F1), chessboard, TurnColor.White);
                    }
                    else {
                        UpdatePieceBitboard(ref chessboard.BlackRooks, BitOperations.ToBitboard(Square.H8), BitOperations.ToBitboard(Square.F8), chessboard, TurnColor.Black);
                    }
                }
                else if (move.SpecialCode == (int)SpecialMovesCode.QueenCastle) {
                    if (chessboard.State.TurnColor == TurnColor.White) {
                        UpdatePieceBitboard(ref chessboard.WhiteRooks, BitOperations.ToBitboard(Square.A1), BitOperations.ToBitboard(Square.D1), chessboard, TurnColor.White);
                    }
                    else {
                        UpdatePieceBitboard(ref chessboard.BlackRooks, BitOperations.ToBitboard(Square.A8), BitOperations.ToBitboard(Square.D8), chessboard, TurnColor.Black);
                    }
                }

                chessboard.State.EnPassantSquare = null;
            }

            // precheck if it's en passant first
            else if (move.SpecialCode == (int)SpecialMovesCode.EpCapture) {
                chessboard.stateStack[++chessboard.plyIndex] = chessboard.State;
                chessboard.State.HalfMoveClock = 0;

                if (chessboard.State.TurnColor == TurnColor.White) {
                    UpdatePieceBitboard(ref chessboard.BlackPawns, bitboardTo >> 8, chessboard, TurnColor.Black);
                    //Logger.Log(Channel.Debug, $"en passant capture, removing pawn on {BitOperations.ToSquare(bitboardTo >> 8)}");
                }
                else {
                    UpdatePieceBitboard(ref chessboard.WhitePawns, bitboardTo << 8, chessboard, TurnColor.White);
                    //Logger.Log(Channel.Debug, $"en passant capture by black, removing pawn on {BitOperations.ToSquare(bitboardTo << 8)}");
                }

                UpdatePieceBitboard(ref chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Pawn], bitboardFrom, bitboardTo, chessboard, chessboard.State.TurnColor); // move pawn
                chessboard.State.EnPassantSquare = null;

                //Logger.Log(Channel.Debug, $"en passant capture {move} by {chessboard.State.TurnColor}");
                //Logger.Log(Channel.Debug, "AllWhitePieces", StringHelper.FormatAsChessboard(chessboard.AllWhitePieces));
                //Logger.Log(Channel.Debug, "AllBlackPieces", StringHelper.FormatAsChessboard(chessboard.AllBlackPieces));
                //Logger.Log(Channel.Debug, "AllPieces", StringHelper.FormatAsChessboard(chessboard.AllPieces));
            }

            // no special move, brute force to find correct piece to move
            else {
                for (int pieceTypeIndex = 0; pieceTypeIndex < 6; pieceTypeIndex++) {
                    if ((chessboard.Position[(int)chessboard.State.TurnColor, pieceTypeIndex] & bitboardFrom) != 0) {
                        // quiet moves
                        if ((chessboard.AllPieces & bitboardTo) == 0) {
                            if (pieceTypeIndex == (int)PieceType.Pawn) {
                                chessboard.State.HalfMoveClock = 0;
                                // check promotion
                                switch (move.SpecialCode) {
                                    case (int)SpecialMovesCode.KnightPromotion:
                                        UpdatePieceBitboard(ref chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Pawn], bitboardFrom, chessboard, chessboard.State.TurnColor); // remove promoting pawn
                                        UpdatePieceBitboard(ref chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Knight], bitboardTo, chessboard, chessboard.State.TurnColor); // spawn new knight
                                        break;

                                    case (int)SpecialMovesCode.BishopPromotion:
                                        UpdatePieceBitboard(ref chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Pawn], bitboardFrom, chessboard, chessboard.State.TurnColor); // remove promoting pawn
                                        UpdatePieceBitboard(ref chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Bishop], bitboardTo, chessboard, chessboard.State.TurnColor); // spawn new bishop
                                        break;

                                    case (int)SpecialMovesCode.RookPromotion:
                                        UpdatePieceBitboard(ref chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Pawn], bitboardFrom, chessboard, chessboard.State.TurnColor); // remove promoting pawn
                                        UpdatePieceBitboard(ref chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Rook], bitboardTo, chessboard, chessboard.State.TurnColor); // spawn new rook
                                        break;

                                    case (int)SpecialMovesCode.QueenPromotion:
                                        UpdatePieceBitboard(ref chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Pawn], bitboardFrom, chessboard, chessboard.State.TurnColor); // remove promoting pawn
                                        UpdatePieceBitboard(ref chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Queen], bitboardTo, chessboard, chessboard.State.TurnColor); // spawn new queen
                                        break;

                                    default: // no promotion
                                        UpdatePieceBitboard(ref chessboard.Position[(int)chessboard.State.TurnColor, pieceTypeIndex], bitboardFrom, bitboardTo, chessboard, chessboard.State.TurnColor); // spawn new queen
                                        break;
                                }
                            }
                            else {
                                chessboard.State.HalfMoveClock++;
                                UpdatePieceBitboard(ref chessboard.Position[(int)chessboard.State.TurnColor, pieceTypeIndex], bitboardFrom, bitboardTo, chessboard, chessboard.State.TurnColor); // move piece
                            }
                            chessboard.State.CapturedPiece = null;
                        }
                        // capture
                        else {
                            chessboard.State.HalfMoveClock = 0;

                            // check which capture piece it is
                            for (int opponantPieceTypeIndex = 0; opponantPieceTypeIndex < 6; opponantPieceTypeIndex++) {
                                if ((chessboard.Position[(int)chessboard.State.TurnColor ^ 1, opponantPieceTypeIndex] & bitboardTo) != 0) {
                                    // update piece positions and existance
                                    UpdatePieceBitboard(ref chessboard.Position[(int)chessboard.State.TurnColor ^ 1, opponantPieceTypeIndex], bitboardTo, chessboard, chessboard.State.TurnColor ^ TurnColor.Black); // remove captured piece
                                    UpdatePieceBitboard(ref chessboard.Position[(int)chessboard.State.TurnColor, pieceTypeIndex], bitboardFrom, bitboardTo, chessboard, chessboard.State.TurnColor); // move piece

                                    // check promotion
                                    switch (move.SpecialCode) {
                                        case (int)SpecialMovesCode.KnightPromotionCapture:
                                            UpdatePieceBitboard(ref chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Pawn], bitboardTo, chessboard, chessboard.State.TurnColor); // remove promoting pawn
                                            UpdatePieceBitboard(ref chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Knight], bitboardTo, chessboard, chessboard.State.TurnColor); // spawn new knight
                                            break;

                                        case (int)SpecialMovesCode.BishopPromotionCapture:
                                            UpdatePieceBitboard(ref chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Pawn], bitboardTo, chessboard, chessboard.State.TurnColor); // remove promoting pawn
                                            UpdatePieceBitboard(ref chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Bishop], bitboardTo, chessboard, chessboard.State.TurnColor); // spawn new bishop
                                            break;

                                        case (int)SpecialMovesCode.RookPromotionCapture:
                                            UpdatePieceBitboard(ref chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Pawn], bitboardTo, chessboard, chessboard.State.TurnColor); // remove promoting pawn
                                            UpdatePieceBitboard(ref chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Rook], bitboardTo, chessboard, chessboard.State.TurnColor); // spawn new rook
                                            break;

                                        case (int)SpecialMovesCode.QueenPromotionCapture:
                                            UpdatePieceBitboard(ref chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Pawn], bitboardTo, chessboard, chessboard.State.TurnColor); // remove promoting pawn
                                            UpdatePieceBitboard(ref chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Queen], bitboardTo, chessboard, chessboard.State.TurnColor); // spawn new queen
                                            break;
                                    }

                                    chessboard.State.CapturedPiece = (PieceType)opponantPieceTypeIndex;
                                }
                            }
                        }

                        //push the current state of the position onto the stack
                        chessboard.stateStack[++chessboard.plyIndex] = chessboard.State;

                        chessboard.State.CanWhiteQueenCastle &= (chessboard.WhiteRooks & King.CastlingRookMasks[0]) != 0;
                        chessboard.State.CanWhiteKingCastle &= (chessboard.WhiteRooks & King.CastlingRookMasks[1]) != 0;
                        chessboard.State.CanBlackQueenCastle &= (chessboard.BlackRooks & King.CastlingRookMasks[2]) != 0;
                        chessboard.State.CanBlackKingCastle &= (chessboard.BlackRooks & King.CastlingRookMasks[3]) != 0;

                        // reset en passant square
                        chessboard.State.EnPassantSquare = null;

                        switch (pieceTypeIndex) {
                            // (right to castle)
                            case (int)PieceType.King:
                                if (move.From == Square.E1) {
                                    chessboard.State.CanWhiteKingCastle = false;
                                    chessboard.State.CanWhiteQueenCastle = false;
                                }
                                else if (move.From == Square.E8) {
                                    chessboard.State.CanBlackKingCastle = false;
                                    chessboard.State.CanBlackQueenCastle = false;
                                }
                                break;

                            case (int)PieceType.Pawn:
                                // double pawn push
                                if (move.SpecialCode == (int)SpecialMovesCode.DoublePawnPush) {
                                    if (chessboard.State.TurnColor == TurnColor.White) {
                                        chessboard.State.EnPassantSquare = BitOperations.ToSquare(BitOperations.ToBitboard(move.To) >> 8);
                                    }
                                    else {
                                        chessboard.State.EnPassantSquare = BitOperations.ToSquare(BitOperations.ToBitboard(move.To) << 8);
                                    }
                                    //Logger.Log(Channel.Debug, "Setting en passant on square:", chessboard.State.EnPassantSquare);
                                }
                                break;
                        }
                        break;
                    }
                }
            }
            
            chessboard.State.TurnColor ^= TurnColor.Black; // toggle color
        }

        public static void UnmakeMove(Chessboard chessboard, Move move) {
            var bitboardFrom = move.FromBitboard();
            var bitboardTo = move.ToBitboard();
            
            //restore the previous state before latest move pushed move, the latest state provide the turn to play,
            ref var latestState = ref chessboard.stateStack[chessboard.plyIndex--]; // pop the latest move form the stack then decrement
            chessboard.State = latestState;
            //Logger.Log(Channel.Debug, chessboard.State.TurnColor);

            //restore position from latest state in the stack
            if (move.PROMOTION_FLAG) {
                if (move.CAPTURE_FLAG) { // restore captured piece
                    UpdatePieceBitboard(ref chessboard.Position[(int)latestState.TurnColor ^ 1, (int)latestState.CapturedPiece!], bitboardTo, chessboard, latestState.TurnColor ^ TurnColor.Black);
                }
                //remove new piece
                for (int pieceTypeIndex = 0; pieceTypeIndex < chessboard.Position.GetLength(1); pieceTypeIndex++) {
                    if ((chessboard.Position[(int)latestState.TurnColor, pieceTypeIndex] & bitboardTo) != 0) {
                        UpdatePieceBitboard(ref chessboard.Position[(int)latestState.TurnColor, pieceTypeIndex], bitboardTo, chessboard, latestState.TurnColor); // restore piece position
                        break;
                    }
                }
                //restore pawn
                UpdatePieceBitboard(ref chessboard.Position[(int)latestState.TurnColor, (int)PieceType.Pawn], bitboardFrom, chessboard, latestState.TurnColor);
            }

            else if (move.CASTLE_FLAG) {
                UpdatePieceBitboard(ref chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.King], bitboardTo, bitboardFrom, chessboard, chessboard.State.TurnColor); // restore king position

                //precheck castle => jump the rook over the king
                if (move.SpecialCode == (int)SpecialMovesCode.KingCastle) {
                    if (chessboard.State.TurnColor == TurnColor.White) {
                        UpdatePieceBitboard(ref chessboard.WhiteRooks, BitOperations.ToBitboard(Square.F1), BitOperations.ToBitboard(Square.H1), chessboard, TurnColor.White);
                    }
                    else {
                        UpdatePieceBitboard(ref chessboard.BlackRooks, BitOperations.ToBitboard(Square.F8), BitOperations.ToBitboard(Square.H8), chessboard, TurnColor.Black);
                    }
                }
                else if (move.SpecialCode == (int)SpecialMovesCode.QueenCastle) {
                    if (chessboard.State.TurnColor == TurnColor.White) {
                        UpdatePieceBitboard(ref chessboard.WhiteRooks, BitOperations.ToBitboard(Square.D1), BitOperations.ToBitboard(Square.A1), chessboard, TurnColor.White);
                    }
                    else {
                        UpdatePieceBitboard(ref chessboard.BlackRooks, BitOperations.ToBitboard(Square.D8), BitOperations.ToBitboard(Square.A8), chessboard, TurnColor.Black);
                    }
                }
            }

            else if (move.SpecialCode == (int)SpecialMovesCode.EpCapture) {
                UpdatePieceBitboard(ref chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Pawn], bitboardTo, bitboardFrom, chessboard, chessboard.State.TurnColor); // restore pawn position
                if (chessboard.State.TurnColor == TurnColor.White) {
                    UpdatePieceBitboard(ref chessboard.BlackPawns, bitboardTo >> 8, chessboard, TurnColor.Black);
                }
                else {
                    UpdatePieceBitboard(ref chessboard.WhitePawns, bitboardTo << 8, chessboard, TurnColor.White);
                }
            }

            else {
                // restore last moved piece from latestState
                for (int pieceTypeIndex = 0; pieceTypeIndex < chessboard.Position.GetLength(1); pieceTypeIndex++) {
                    ref var piece = ref chessboard.Position[(int)chessboard.State.TurnColor, pieceTypeIndex];
                    if ((piece & bitboardTo) != 0) {
                        UpdatePieceBitboard(ref piece, bitboardTo, bitboardFrom, chessboard, chessboard.State.TurnColor); // restore piece position
                        break;
                    }
                }
                if (chessboard.State.CapturedPiece.HasValue) { // restore opponant piece
                    UpdatePieceBitboard(ref chessboard.Position[(int)chessboard.State.TurnColor ^ 1, (int)chessboard.State.CapturedPiece], bitboardTo, chessboard, chessboard.State.TurnColor ^ TurnColor.Black);
                }
            }
            //Logger.Log(Channel.Debug, "plyIndex after unmaking move:", chessboard.plyIndex);
        }

        public override bool Equals(object obj) {
            if (obj is Move other)
                return this.word == other.word;
            return false;
        }

        public override int GetHashCode() {
            return word.GetHashCode();
        }
    }
}