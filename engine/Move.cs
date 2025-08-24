using ChessEngine.Utils;
using ChessEngine.Utils.Logging;
using Bitboard = ulong;
using ChessEngine.Pieces;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ChessEngine {

    public readonly struct Move {
        public readonly ushort word;
        public readonly PieceType pieceType;
        private readonly Bitboard bitboardFrom;
        private readonly Bitboard bitboardTo;

        public Move(ushort word, PieceType pieceType) {
            this.word = word;
            this.pieceType = pieceType;
            bitboardFrom = 1UL << (word >> 10);
            bitboardTo = 1UL << ((word >> 4) & 0x3F);
        }
        
        public Square From => (Square)(word >> 10);
        public Square To => (Square)((word >> 4) & 0x3F);

        public byte SpecialCode => (byte)(word & 0b1111);  // Expression body
        public bool CASTLE_FLAG => (word & 0b1010) == 0b0010;
        public bool CAPTURE_FLAG => (word & 0b0100) != 0;
        public bool PROMOTION_FLAG => (word & 0b1000) != 0;

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

        //if the code is not 0, then it's an irreversible move
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
        public static void UpdatePieceBitboard(ref Bitboard bitboard, Bitboard from, Bitboard to, Chessboard chessboard, TurnColor turnColor, PieceType pieceType) {
            bitboard ^= from ^ to;
            ref Bitboard colorPieces = ref ((turnColor == TurnColor.White) ?
                ref chessboard.AllWhitePieces : ref chessboard.AllBlackPieces);
            colorPieces ^= from ^ to;

            var fromIndex = BitOperations.ToIndex(from);
            var index = ZobristHashing.GetPieceSquareIndex(turnColor, pieceType, fromIndex);
            chessboard.State.ZobristHashKey ^= ZobristHashing.pieceSquare[index];
            chessboard.State.ZobristHashKey ^= ZobristHashing.pieceSquare[index - (fromIndex - BitOperations.ToIndex(to))];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UpdatePieceBitboard(ref Bitboard bitboard, Bitboard square, Chessboard chessboard, TurnColor turnColor, PieceType pieceType) {
            bitboard ^= square;
            ref Bitboard colorPieces = ref ((turnColor == TurnColor.White) ?
                ref chessboard.AllWhitePieces : ref chessboard.AllBlackPieces);
            colorPieces ^= square;
            chessboard.State.ZobristHashKey ^= ZobristHashing.pieceSquare[ZobristHashing.GetPieceSquareIndex(turnColor, pieceType, BitOperations.ToIndex(square))];
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

            for (int pieceTypeIndex = 0; pieceTypeIndex < 6; pieceTypeIndex++) {
                if ((chessboard.Position[(int)chessboard.State.TurnColor, pieceTypeIndex] & fromBitboard) != 0) {
                    return new Move(word, (PieceType)pieceTypeIndex);
                }
            }

            return new Move(word, PieceType.Pawn); // Default to Pawn if no piece found, should not happen
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static void MakeMove(Chessboard chessboard, in Move move) {
            //Stopwatch stopwatch = Stopwatch.StartNew();
            // precheck castling
            if (move.CASTLE_FLAG) {
                chessboard.stateStack[++chessboard.plyIndex] = chessboard.State;

                if (chessboard.State.TurnColor == TurnColor.White) {
                    UpdatePieceBitboard(ref chessboard.WhiteKing, move.bitboardFrom, move.bitboardTo, chessboard, TurnColor.White, PieceType.King); // move king

                    chessboard.State.CanWhiteKingCastle = false;
                    chessboard.State.CanWhiteQueenCastle = false;
                }
                else {
                    UpdatePieceBitboard(ref chessboard.BlackKing, move.bitboardFrom, move.bitboardTo, chessboard, TurnColor.Black, PieceType.King); // move king

                    chessboard.State.CanBlackKingCastle = false;
                    chessboard.State.CanBlackQueenCastle = false;
                }

                //precheck castle => jump the rook over the king
                switch (move.SpecialCode) {
                    case (byte)SpecialMovesCode.KingCastle:
                        if (chessboard.State.TurnColor == TurnColor.White) {
                            UpdatePieceBitboard(ref chessboard.WhiteRooks, King.CastleBitboards.F1, King.CastleBitboards.H1, chessboard, TurnColor.White, PieceType.Rook);
                        }
                        else {
                            UpdatePieceBitboard(ref chessboard.BlackRooks, King.CastleBitboards.F8, King.CastleBitboards.H8, chessboard, TurnColor.Black, PieceType.Rook);
                        }
                        break;
                    case (byte)SpecialMovesCode.QueenCastle:
                        if (chessboard.State.TurnColor == TurnColor.White) {
                            UpdatePieceBitboard(ref chessboard.WhiteRooks, King.CastleBitboards.A1, King.CastleBitboards.D1, chessboard, TurnColor.White, PieceType.Rook);
                        }
                        else {
                            UpdatePieceBitboard(ref chessboard.BlackRooks, King.CastleBitboards.A8, King.CastleBitboards.D8, chessboard, TurnColor.Black, PieceType.Rook);
                        }
                        break;
                }

                chessboard.State.EnPassantSquare = null;
            }

            // precheck if it's en passant first
            else if (move.SpecialCode == (int)SpecialMovesCode.EpCapture) {
                chessboard.stateStack[++chessboard.plyIndex] = chessboard.State;

                if (chessboard.State.TurnColor == TurnColor.White) {
                    UpdatePieceBitboard(ref chessboard.BlackPawns, move.bitboardTo >> 8, chessboard, TurnColor.Black, PieceType.Pawn); // remove black pawn
                }
                else {
                    UpdatePieceBitboard(ref chessboard.WhitePawns, move.bitboardTo << 8, chessboard, TurnColor.White, PieceType.Pawn); // remove white pawn
                }

                UpdatePieceBitboard(ref chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Pawn], move.bitboardFrom, move.bitboardTo, chessboard, chessboard.State.TurnColor, PieceType.Pawn); // move pawn
                chessboard.State.EnPassantSquare = null;
            }

            // no special move, brute force to find correct piece to move
            else {
                // quiet moves
                if ((chessboard.AllPieces & move.bitboardTo) == 0) {
                    if (move.pieceType == PieceType.Pawn) {
                        // check promotion
                        switch (move.SpecialCode) {
                            case (byte)SpecialMovesCode.KnightPromotion:
                                UpdatePieceBitboard(ref chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Pawn], move.bitboardFrom, chessboard, chessboard.State.TurnColor, PieceType.Pawn); // remove promoting pawn
                                UpdatePieceBitboard(ref chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Knight], move.bitboardTo, chessboard, chessboard.State.TurnColor, PieceType.Knight); // spawn new knight
                                break;

                            case (byte)SpecialMovesCode.BishopPromotion:
                                UpdatePieceBitboard(ref chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Pawn], move.bitboardFrom, chessboard, chessboard.State.TurnColor, PieceType.Pawn); // remove promoting pawn
                                UpdatePieceBitboard(ref chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Bishop], move.bitboardTo, chessboard, chessboard.State.TurnColor, PieceType.Bishop); // spawn new bishop
                                break;

                            case (byte)SpecialMovesCode.RookPromotion:
                                UpdatePieceBitboard(ref chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Pawn], move.bitboardFrom, chessboard, chessboard.State.TurnColor, PieceType.Pawn); // remove promoting pawn
                                UpdatePieceBitboard(ref chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Rook], move.bitboardTo, chessboard, chessboard.State.TurnColor, PieceType.Rook); // spawn new rook
                                break;

                            case (byte)SpecialMovesCode.QueenPromotion:
                                UpdatePieceBitboard(ref chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Pawn], move.bitboardFrom, chessboard, chessboard.State.TurnColor, PieceType.Pawn); // remove promoting pawn
                                UpdatePieceBitboard(ref chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Queen], move.bitboardTo, chessboard, chessboard.State.TurnColor, PieceType.Queen); // spawn new queen
                                break;

                            default: // no promotion
                                UpdatePieceBitboard(ref chessboard.Position[(int)chessboard.State.TurnColor, (int)move.pieceType], move.bitboardFrom, move.bitboardTo, chessboard, chessboard.State.TurnColor, move.pieceType);
                                break;
                        }
                    }
                    else {
                        UpdatePieceBitboard(ref chessboard.Position[(int)chessboard.State.TurnColor, (int)move.pieceType], move.bitboardFrom, move.bitboardTo, chessboard, chessboard.State.TurnColor, move.pieceType); // move piece
                    }
                    chessboard.State.CapturedPiece = null;

                    //stopwatch.Stop();
                    //Logger.Log(Channel.Debug, "MakeMove time:", stopwatch.ElapsedTicks, "ns");
                }
                // capture
                else {
                    // check which capture piece it is
                    for (int opponantPieceTypeIndex = 0; opponantPieceTypeIndex < 6; opponantPieceTypeIndex++) {
                        if ((chessboard.Position[(int)chessboard.State.TurnColor ^ 1, opponantPieceTypeIndex] & move.bitboardTo) != 0) {
                            // update piece positions and existance
                            UpdatePieceBitboard(ref chessboard.Position[(int)chessboard.State.TurnColor ^ 1, opponantPieceTypeIndex], move.bitboardTo, chessboard, chessboard.State.TurnColor ^ TurnColor.Black, (PieceType)opponantPieceTypeIndex); // remove captured piece
                            UpdatePieceBitboard(ref chessboard.Position[(int)chessboard.State.TurnColor, (int)move.pieceType], move.bitboardFrom, move.bitboardTo, chessboard, chessboard.State.TurnColor, move.pieceType); // move piece

                            // check promotion
                            switch (move.SpecialCode) {
                                case (byte)SpecialMovesCode.KnightPromotionCapture:
                                    UpdatePieceBitboard(ref chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Pawn], move.bitboardTo, chessboard, chessboard.State.TurnColor, PieceType.Pawn); // remove promoting pawn
                                    UpdatePieceBitboard(ref chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Knight], move.bitboardTo, chessboard, chessboard.State.TurnColor, PieceType.Knight); // spawn new knight
                                    break;

                                case (byte)SpecialMovesCode.BishopPromotionCapture:
                                    UpdatePieceBitboard(ref chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Pawn], move.bitboardTo, chessboard, chessboard.State.TurnColor, PieceType.Pawn); // remove promoting pawn
                                    UpdatePieceBitboard(ref chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Bishop], move.bitboardTo, chessboard, chessboard.State.TurnColor, PieceType.Bishop); // spawn new bishop
                                    break;

                                case (byte)SpecialMovesCode.RookPromotionCapture:
                                    UpdatePieceBitboard(ref chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Pawn], move.bitboardTo, chessboard, chessboard.State.TurnColor, PieceType.Pawn); // remove promoting pawn
                                    UpdatePieceBitboard(ref chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Rook], move.bitboardTo, chessboard, chessboard.State.TurnColor, PieceType.Rook); // spawn new rook
                                    break;

                                case (byte)SpecialMovesCode.QueenPromotionCapture:
                                    UpdatePieceBitboard(ref chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Pawn], move.bitboardTo, chessboard, chessboard.State.TurnColor, PieceType.Pawn); // remove promoting pawn
                                    UpdatePieceBitboard(ref chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Queen], move.bitboardTo, chessboard, chessboard.State.TurnColor, PieceType.Queen); // spawn new queen
                                    break;
                            }

                            chessboard.State.CapturedPiece = (PieceType)opponantPieceTypeIndex;
                            break;
                        }
                    }
                }

                //push the current state of the position onto the stack
                chessboard.stateStack[++chessboard.plyIndex] = chessboard.State;


                if (chessboard.State.CapturedPiece.HasValue || move.pieceType == PieceType.Pawn) {
                    chessboard.State.HalfMoveClock = 0;
                }
                else {
                    chessboard.State.HalfMoveClock++;
                }

                // Single batch check
                ulong whiteCheck = chessboard.WhiteRooks & Rook.WHITE_CASTLING_MASK;
                ulong blackCheck = chessboard.BlackRooks & Rook.BLACK_CASTLING_MASK;

                chessboard.State.CanWhiteKingCastle &= (whiteCheck & (Bitboard)BSquare.H1) != 0;
                chessboard.State.CanWhiteQueenCastle &= (whiteCheck & (Bitboard)BSquare.A1) != 0;
                chessboard.State.CanBlackKingCastle &= (blackCheck & (Bitboard)BSquare.H8) != 0;
                chessboard.State.CanBlackQueenCastle &= (blackCheck & (Bitboard)BSquare.A8) != 0;

                //stopwatch.Stop();
                //Logger.Log(Channel.Debug, "MakeMove time:", stopwatch.ElapsedTicks, "ns");

                // reset en passant square
                chessboard.State.EnPassantSquare = null;

                switch (move.pieceType) {
                    // (right to castle)
                    case PieceType.King:
                        if (move.From == Square.E1) {
                            chessboard.State.CanWhiteKingCastle = false;
                            chessboard.State.CanWhiteQueenCastle = false;
                        }
                        else if (move.From == Square.E8) {
                            chessboard.State.CanBlackKingCastle = false;
                            chessboard.State.CanBlackQueenCastle = false;
                        }
                        break;

                    case PieceType.Pawn:
                        // double pawn push
                        if (move.SpecialCode == (int)SpecialMovesCode.DoublePawnPush) {
                            if (chessboard.State.TurnColor == TurnColor.White) {
                                chessboard.State.EnPassantSquare = BitOperations.ToSquare(move.bitboardTo >> 8);
                            }
                            else {
                                chessboard.State.EnPassantSquare = BitOperations.ToSquare(move.bitboardTo << 8);
                            }
                        }
                        break;
                }
            }

            //chessboard.State.ZobristHashKey ^= ZobristHashing.sideToMove;
            //chessboard.State.ZobristHashKey ^= ZobristHashing.ComputeCastlingRightsHash(in chessboard.State);
            chessboard.State.TurnColor ^= TurnColor.Black; // toggle color

            //stopwatch.Stop();
            //Logger.Log(Channel.Debug, "MakeMove time:", stopwatch.ElapsedTicks, "ns");
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static void UnmakeMove(Chessboard chessboard, in Move move) {
            //restore the previous state before latest move pushed move, the latest state provide the turn to play,
            ref var latestState = ref chessboard.stateStack[chessboard.plyIndex--]; // pop the latest move form the stack then decrement
            chessboard.State = latestState;
            //Logger.Log(Channel.Debug, chessboard.State.TurnColor);

            //restore position from latest state in the stack
            if (move.PROMOTION_FLAG) {
                var color = (int)latestState.TurnColor;
                if (move.CAPTURE_FLAG) { // restore captured piece
                    UpdatePieceBitboard(ref chessboard.Position[color ^ 1, (int)latestState.CapturedPiece!], move.bitboardTo, chessboard, latestState.TurnColor ^ TurnColor.Black, (PieceType)latestState.CapturedPiece);
                }
                //remove new piece
                for (int pieceTypeIndex = 0; pieceTypeIndex < chessboard.Position.GetLength(1); pieceTypeIndex++) {
                    if ((chessboard.Position[(int)latestState.TurnColor, pieceTypeIndex] & move.bitboardTo) != 0) {
                        UpdatePieceBitboard(ref chessboard.Position[(int)latestState.TurnColor, pieceTypeIndex], move.bitboardTo, chessboard, latestState.TurnColor, (PieceType)pieceTypeIndex); // restore piece position
                        break;
                    }
                }
                //restore pawn
                UpdatePieceBitboard(ref chessboard.Position[color, (int)PieceType.Pawn], move.bitboardFrom, chessboard, latestState.TurnColor, PieceType.Pawn);
            }

            else if (move.CASTLE_FLAG) {
                UpdatePieceBitboard(ref chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.King], move.bitboardTo, move.bitboardFrom, chessboard, chessboard.State.TurnColor, PieceType.King); // restore king position

                //precheck castle => jump the rook over the king
                if (move.SpecialCode == (byte)SpecialMovesCode.KingCastle) {
                    if (chessboard.State.TurnColor == TurnColor.White) {
                        UpdatePieceBitboard(ref chessboard.WhiteRooks, King.CastleBitboards.F1, King.CastleBitboards.H1, chessboard, TurnColor.White, PieceType.Rook);
                    }
                    else {
                        UpdatePieceBitboard(ref chessboard.BlackRooks, King.CastleBitboards.F8, King.CastleBitboards.H8, chessboard, TurnColor.Black, PieceType.Rook);
                    }
                }
                else if (move.SpecialCode == (byte)SpecialMovesCode.QueenCastle) {
                    if (chessboard.State.TurnColor == TurnColor.White) {
                        UpdatePieceBitboard(ref chessboard.WhiteRooks, King.CastleBitboards.D1, King.CastleBitboards.A1, chessboard, TurnColor.White, PieceType.Rook);
                    }
                    else {
                        UpdatePieceBitboard(ref chessboard.BlackRooks, King.CastleBitboards.D8, King.CastleBitboards.A8, chessboard, TurnColor.Black, PieceType.Rook);
                    }
                }
            }

            else if (move.SpecialCode == (byte)SpecialMovesCode.EpCapture) {
                UpdatePieceBitboard(ref chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Pawn], move.bitboardTo, move.bitboardFrom, chessboard, chessboard.State.TurnColor, PieceType.Pawn); // restore pawn position
                if (chessboard.State.TurnColor == TurnColor.White) {
                    UpdatePieceBitboard(ref chessboard.BlackPawns, move.bitboardTo >> 8, chessboard, TurnColor.Black, PieceType.Pawn);
                }
                else {
                    UpdatePieceBitboard(ref chessboard.WhitePawns, move.bitboardTo << 8, chessboard, TurnColor.White, PieceType.Pawn);
                }
            }

            else {
                // restore last moved piece from latestState
                ref Bitboard lastMovedPieceBitboard = ref chessboard.Position[(int)chessboard.State.TurnColor, (int)move.pieceType];
                UpdatePieceBitboard(ref lastMovedPieceBitboard, move.bitboardTo, move.bitboardFrom, chessboard, chessboard.State.TurnColor, move.pieceType); // restore piece position
                
                if (chessboard.State.CapturedPiece.HasValue) { // restore opponant piece
                    UpdatePieceBitboard(ref chessboard.Position[(int)chessboard.State.TurnColor ^ 1, (int)chessboard.State.CapturedPiece], move.bitboardTo, chessboard, chessboard.State.TurnColor ^ TurnColor.Black, (PieceType)chessboard.State.CapturedPiece);
                }
            }
            //Logger.Log(Channel.Debug, "plyIndex after unmaking move:", chessboard.plyIndex);
        }
    }
}