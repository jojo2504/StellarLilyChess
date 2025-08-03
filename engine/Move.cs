using ChessEngine.Utils;
using ChessEngine.Utils.Logging;
using Bitboard = ulong;

namespace ChessEngine {

    public class Move {
        public ushort word;

        public Move(ushort word) {
            this.word = word;
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

        public byte SpecialCode {
            get {
                return (byte)(word & 0xF);
            }
        }

        //example: A1A2
        public override string ToString() {
            return $"{From}{To}{promotionDict.FirstOrDefault(x => x.Value == (byte)(SpecialMovesCode)SpecialCode).Key}";
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

        static readonly Dictionary<string, int> promotionDict = new() {
            {"n", (byte)SpecialMovesCode.KnightPromotion},
            {"b", (byte)SpecialMovesCode.BishopPromotion},
            {"r", (byte)SpecialMovesCode.RookPromotion},
            {"q", (byte)SpecialMovesCode.QueenPromotion},
        };
        
        public static void EncodePossibleMoves(Bitboard possibleMoves, Square fromSquare, ref List<Move> allPseudoLegalMoves) {
            int index = 0;
            while (possibleMoves != 0) {
                if ((possibleMoves & 1) == 1) {
                    var move = EncodeMove(fromSquare, (Square)index, 0);
                    allPseudoLegalMoves.AddRange(move);
                }
                index++;
                possibleMoves >>= 1;
            }
        }

        //16 bits
        //6 from
        //6 to
        //4 nibble
        // from 10 -> 001010
        // to 18 -> 010010
        // code 0 -> 0000
        // 001010_010010_0000
        // 111111_000000_0000 | 0xFC00
        // 0xFFF  0xF
        public static Move EncodeMove(Square from, Square to, byte specialMovesCode) {
            ushort word = (ushort)((ushort)(((int)from) << 10) | (ushort)(((int)to) << 4) | (specialMovesCode & 0xF));

            //Logger.Log($"{(int)from}, {(int)to}");
            //Logger.Log($"{(Square)((word & 0xFC00) >> 10)}{(Square)((word & 0x3F0) >> 4)}");
            return new Move(word);
        }

        public static Move DecodeUciMove(Chessboard chessboard, string UciMove) {
            Logger.Log("decoding uci", UciMove);
            var fromCode = UciMove.Substring(0, 2);
            var toCode = UciMove.Substring(2, 2);
            string? promotion = null;
            if (UciMove.Length == 5) promotion = UciMove[^1].ToString();

            var fromSquare = Enum.Parse<Square>(fromCode.ToUpper());
            var toSquare = Enum.Parse<Square>(toCode.ToUpper());

            var word = (ushort)((ushort)(((int)fromSquare) << 10) | (ushort)(((int)toSquare) << 4));

            byte smc = (byte)SpecialMovesCode.QuietMoves;
            // find the special move code:
            for (int pieceTypeIndex = 0; pieceTypeIndex < chessboard.Position.GetLength(1); pieceTypeIndex++) {
                if ((chessboard.Position[(int)chessboard.State.TurnColor, pieceTypeIndex].BitboardValue & BitOperations.ToBitboard(fromSquare)) != 0) {
                    if ((BitOperations.ToBitboard(toSquare) & chessboard.AllPieces) != 0) {
                        smc = (byte)SpecialMovesCode.Captures;
                    }
                    switch ((PieceType)pieceTypeIndex) {
                        case PieceType.Pawn:
                            // promotion and capture? or en passant
                            if (promotion is not null) {
                                smc += (byte)promotionDict[promotion];
                            }
                            if (Math.Abs((int)fromSquare - (int)toSquare) == 16) {
                                smc = (byte)SpecialMovesCode.DoublePawnPush;
                            }
                            if ((Math.Abs((int)fromSquare - (int)toSquare) == 9) && ((BitOperations.ToBitboard(toSquare) & chessboard.AllPieces) == 0)) {
                                smc = (byte)SpecialMovesCode.EpCapture;
                            }
                            break;

                        case PieceType.King:
                            // castle or capture
                            if (Math.Abs((int)fromSquare - (int)toSquare) == 2) {
                                if (toSquare == Square.C1 || toSquare == Square.C8) {
                                    smc = (byte)SpecialMovesCode.QueenCastle;
                                }
                                else if (toSquare == Square.G1 || toSquare == Square.G8) {
                                    smc = (byte)SpecialMovesCode.KingCastle;
                                }
                            }
                            break;

                        default:
                            break;
                    }
                }
            }
            Logger.Log((SpecialMovesCode)smc);
            
            word |= (ushort)(smc & 0xF);

            return new Move(word);
        }

        public static void MakeMove(Chessboard chessboard, Move move, bool displayComputationLogs = false) {
            var bitboardFrom = BitOperations.ToBitboard(move.From);
            var bitboardTo = BitOperations.ToBitboard(move.To);
            
            chessboard.State.Move = move;
            
            if (move.SpecialCode == (int)SpecialMovesCode.KingCastle || move.SpecialCode == (int)SpecialMovesCode.QueenCastle) {
                chessboard.State.HalfMoveClock++;
                chessboard.stateStack.Push(chessboard.State);

                if (chessboard.State.TurnColor == TurnColor.White) {
                    chessboard.Position[(int)TurnColor.White, (int)PieceType.King].BitboardValue ^= bitboardFrom; // remove old position
                    chessboard.Position[(int)TurnColor.White, (int)PieceType.King].BitboardValue ^= bitboardTo; // set new position

                    chessboard.State.CanWhiteKingCastle = false;
                    chessboard.State.CanWhiteQueenCastle = false;
                }
                else {
                    chessboard.Position[(int)TurnColor.Black, (int)PieceType.King].BitboardValue ^= bitboardFrom; // remove old position
                    chessboard.Position[(int)TurnColor.Black, (int)PieceType.King].BitboardValue ^= bitboardTo; // set new position

                    chessboard.State.CanBlackKingCastle = false;
                    chessboard.State.CanBlackQueenCastle = false;
                }

                //precheck castle => jump the rook over the king
                if (move.SpecialCode == (int)SpecialMovesCode.KingCastle) {
                    if (chessboard.State.TurnColor == TurnColor.White) {
                        chessboard.Position[(int)TurnColor.White, (int)PieceType.Rook].BitboardValue ^= BitOperations.ToBitboard(Square.H1); // remove old position
                        chessboard.Position[(int)TurnColor.White, (int)PieceType.Rook].BitboardValue ^= bitboardTo >> 1; // set new position
                    }
                    else {
                        chessboard.Position[(int)TurnColor.Black, (int)PieceType.Rook].BitboardValue ^= BitOperations.ToBitboard(Square.H8); // remove old position
                        chessboard.Position[(int)TurnColor.Black, (int)PieceType.Rook].BitboardValue ^= bitboardTo << 1; // set new position
                    }
                }
                else if (move.SpecialCode == (int)SpecialMovesCode.QueenCastle){
                    if (chessboard.State.TurnColor == TurnColor.White) {
                        chessboard.Position[(int)TurnColor.White, (int)PieceType.Rook].BitboardValue ^= BitOperations.ToBitboard(Square.A1); // remove old position
                        chessboard.Position[(int)TurnColor.White, (int)PieceType.Rook].BitboardValue ^= bitboardTo << 1; // set new position
                    }
                    else {
                        chessboard.Position[(int)TurnColor.Black, (int)PieceType.Rook].BitboardValue ^= BitOperations.ToBitboard(Square.A8); // remove old position
                        chessboard.Position[(int)TurnColor.Black, (int)PieceType.Rook].BitboardValue ^= bitboardTo >> 1; // set new position
                    }
                }
            }

            // precheck if it's en passant first
            else if (move.SpecialCode == (int)SpecialMovesCode.EpCapture) {
                chessboard.State.HalfMoveClock = 0;
                chessboard.stateStack.Push(chessboard.State);

                if (chessboard.State.TurnColor == TurnColor.White) {
                    chessboard.Position[(int)TurnColor.Black, (int)PieceType.Pawn].BitboardValue ^= bitboardTo >> 8; // remove captured piece
                }
                else {
                    chessboard.Position[(int)TurnColor.White, (int)PieceType.Pawn].BitboardValue ^= bitboardTo << 8; // remove captured piece
                }

                chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Pawn].BitboardValue ^= bitboardFrom; // delete old position
                chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Pawn].BitboardValue ^= bitboardTo; // set new position
                chessboard.State.EnPassantSquare = null;
            }

            // no special move, brute force to find correct piece to move
            else {
                for (int pieceTypeIndex = 0; pieceTypeIndex < chessboard.Position.GetLength(1); pieceTypeIndex++) {
                    if ((chessboard.Position[(int)chessboard.State.TurnColor, pieceTypeIndex].BitboardValue & bitboardFrom) != 0) {
                        // quiet moves
                        if ((chessboard.AllPieces & bitboardTo) == 0) {
                            if (pieceTypeIndex == (int)PieceType.Pawn) {
                                chessboard.State.HalfMoveClock = 0;
                            }
                            else {
                                chessboard.State.HalfMoveClock++;
                            }

                            chessboard.Position[(int)chessboard.State.TurnColor, pieceTypeIndex].BitboardValue ^= bitboardFrom; // remove old position
                            chessboard.Position[(int)chessboard.State.TurnColor, pieceTypeIndex].BitboardValue ^= bitboardTo; // set new position

                            chessboard.State.CapturedPiece = null;
                        }
                        // capture
                        else {
                            chessboard.State.HalfMoveClock = 0;

                            // check which capture piece it is
                            for (int opponantPieceTypeIndex = 0; opponantPieceTypeIndex < chessboard.Position.GetLength(1); opponantPieceTypeIndex++) {
                                if ((chessboard.Position[(int)chessboard.State.TurnColor ^ 1, opponantPieceTypeIndex].BitboardValue & bitboardTo) != 0) {
                                    if (displayComputationLogs) Logger.Log("capturing a", (PieceType)opponantPieceTypeIndex);
                                    // update piece positions and existance
                                    chessboard.Position[(int)chessboard.State.TurnColor ^ 1, opponantPieceTypeIndex].BitboardValue ^= bitboardTo; // remove captured piece
                                    chessboard.Position[(int)chessboard.State.TurnColor, pieceTypeIndex].BitboardValue ^= bitboardFrom; // remove old position
                                    chessboard.Position[(int)chessboard.State.TurnColor, pieceTypeIndex].BitboardValue ^= bitboardTo; // set new position
                                    if (displayComputationLogs) Logger.Log("finished capturing");
                                    // check promotion
                                    switch (move.SpecialCode) {
                                        case (int)SpecialMovesCode.BishopPromotionCapture:
                                            chessboard.Position[(int)chessboard.State.TurnColor, pieceTypeIndex].BitboardValue ^= bitboardTo; // remove promoting pawn
                                            chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Knight].BitboardValue ^= bitboardTo; // spawn new knight
                                            break;

                                        case (int)SpecialMovesCode.KnightPromotionCapture:
                                            chessboard.Position[(int)chessboard.State.TurnColor, pieceTypeIndex].BitboardValue ^= bitboardTo; // remove promoting pawn
                                            chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Bishop].BitboardValue ^= bitboardTo; // spawn new bishop
                                            break;

                                        case (int)SpecialMovesCode.RookPromotionCapture:
                                            chessboard.Position[(int)chessboard.State.TurnColor, pieceTypeIndex].BitboardValue ^= bitboardTo; // remove promoting pawn
                                            chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Rook].BitboardValue ^= bitboardTo; // spawn new rook
                                            break;
                                        case (int)SpecialMovesCode.QueenPromotionCapture:
                                            chessboard.Position[(int)chessboard.State.TurnColor, pieceTypeIndex].BitboardValue ^= bitboardTo; // remove promoting pawn
                                            chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Queen].BitboardValue ^= bitboardTo; // spawn new queen
                                            break;
                                    }

                                    // update chessboard state 
                                    chessboard.State.CapturedPiece = (PieceType)opponantPieceTypeIndex;
                                }
                            }
                        }

                        //push the current state of the position onto the stack
                        chessboard.stateStack.Push(chessboard.State);

                        // reset en passant square
                        chessboard.State.EnPassantSquare = null;

                        // (right to castle)
                        if (pieceTypeIndex == (int)PieceType.Rook) {
                            if (move.From == Square.A1) chessboard.State.CanWhiteQueenCastle = false;
                            else if (move.From == Square.H1) chessboard.State.CanWhiteKingCastle = false;
                            else if (move.From == Square.A8) chessboard.State.CanBlackQueenCastle = false;
                            else if (move.From == Square.H8) chessboard.State.CanBlackKingCastle = false;
                        }

                        else if (pieceTypeIndex == (int)PieceType.King) {
                            if (move.From == Square.E1) {
                                chessboard.State.CanWhiteKingCastle = false;
                                chessboard.State.CanWhiteQueenCastle = false;
                            }
                            else if (move.From == Square.E8) {
                                chessboard.State.CanBlackKingCastle = false;
                                chessboard.State.CanBlackQueenCastle = false;
                            }
                        }

                        // set en passant square
                        else if (pieceTypeIndex == (int)PieceType.Pawn) {
                            // double pawn push
                            if (move.SpecialCode == (int)SpecialMovesCode.DoublePawnPush) {
                                if (chessboard.State.TurnColor == TurnColor.White) {
                                    chessboard.State.EnPassantSquare = BitOperations.ToSquare(BitOperations.ToBitboard(move.To) >> 8);
                                }
                                else {
                                    chessboard.State.EnPassantSquare = BitOperations.ToSquare(BitOperations.ToBitboard(move.To) << 8);
                                }
                                Logger.Log("Setting en passant on square:", chessboard.State.EnPassantSquare);
                            }
                        }

                        return;
                    }
                }
            }

            chessboard.stateStack.Push(chessboard.State);
            if (chessboard.stateStack.Count < 2) Logger.Fatal("ah bah ok");
        }

        public static void UnmakeMove(Chessboard chessboard, Move move) {
            var bitboardFrom = BitOperations.ToBitboard(move.From);
            var bitboardTo = BitOperations.ToBitboard(move.To);

            //restore the previous state before latest move
            var latestState = chessboard.stateStack.Pop();
            chessboard.State = chessboard.stateStack.ElementAt(0);
            chessboard.State.TurnColor = (TurnColor)(((int)chessboard.State.TurnColor)^1);

            // TODO: implemented promotion unmake, en passant unmake and castling unmake

            // restore last moved piece from latestState
            foreach (var pieceBitboard in chessboard.Position) {
                if ((pieceBitboard.BitboardValue & bitboardTo) != 0) {
                    pieceBitboard.BitboardValue ^= bitboardFrom;
                    pieceBitboard.BitboardValue ^= bitboardTo;
                    break;
                }
            }
            // restore captured piece
            if (latestState.CapturedPiece.HasValue) {
                PieceType capturedPiece = (PieceType)latestState.CapturedPiece;
                //restore captured piece
                chessboard.Position[(int)latestState.TurnColor ^ 1, (int)capturedPiece].BitboardValue |= bitboardTo;

                Logger.Log($"restored {capturedPiece} on {BitOperations.ToSquare(bitboardTo)} from {BitOperations.ToSquare(bitboardFrom)}");
            }
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