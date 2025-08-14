using ChessEngine.Utils;
using ChessEngine.Utils.Logging;
using Bitboard = ulong;
using ChessEngine.Pieces;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ChessEngine {

    public class Move {
        public ushort word;

        public Move(ushort word) {
            this.word = word;
        }

        public Move(Bitboard from, Bitboard to, Chessboard chessboard) {
            word = (ushort)((BitOperations.ToIndex(from) << 10) | (BitOperations.ToIndex(to) << 4));
            word |= FindSpecialMoveCode(from, to, chessboard);
        }

        public Move(Bitboard from, Bitboard to, SpecialMovesCode promotionCode) {
            word = (ushort)((BitOperations.ToIndex(from) << 10) | (BitOperations.ToIndex(to) << 4) | (ushort)promotionCode);
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
        public static void UpdatePieceBitboard(ref Bitboard bitboard, Bitboard from, Bitboard to) {
            bitboard ^= from ^ to;
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
                if ((chessboard.Position[(int)chessboard.State.TurnColor, pieceTypeIndex].BitboardValue & from) != 0) {
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
            Stopwatch stopwatch = Stopwatch.StartNew();

            char? promotionChar = null;
            if (uciMove.Length == 5) promotionChar = uciMove[^1];

            var fromSquare = (Square)((uciMove[1] - '1') * 8 + (uciMove[0] - 'a'));
            var toSquare = (Square)((uciMove[3] - '1') * 8 + (uciMove[2] - 'a'));

            var fromBitboard = BitOperations.ToBitboard(fromSquare);
            var toBitboard = BitOperations.ToBitboard(toSquare);

            var word = (ushort)((ushort)(((int)fromSquare) << 10) | (ushort)(((int)toSquare) << 4));

            // Determine the special move code
            word |= FindSpecialMoveCode(fromBitboard, toBitboard, chessboard, promotionChar);

            stopwatch.Stop();
            //Logger.Log(Channel.Benchmark, $"decoding uci {uciMove} in {stopwatch.Elapsed.TotalNanoseconds}ns");

            return new Move(word);
        }

        public static void MakeMove(Chessboard chessboard, Move move) {
            Stopwatch stopwatch = Stopwatch.StartNew();

            var bitboardFrom = move.FromBitboard();
            var bitboardTo = move.ToBitboard();

            // precheck castling
            if (move.CASTLE_FLAG) {
                chessboard.State.HalfMoveClock++;
                chessboard.stateStack.Push(chessboard.State);

                if (chessboard.State.TurnColor == TurnColor.White) {
                    UpdatePieceBitboard(ref chessboard.Position[(int)TurnColor.White, (int)PieceType.King].BitboardValue, bitboardFrom, bitboardTo);

                    chessboard.State.CanWhiteKingCastle = false;
                    chessboard.State.CanWhiteQueenCastle = false;
                }
                else {
                    UpdatePieceBitboard(ref chessboard.Position[(int)TurnColor.Black, (int)PieceType.King].BitboardValue, bitboardFrom, bitboardTo);

                    chessboard.State.CanBlackKingCastle = false;
                    chessboard.State.CanBlackQueenCastle = false;
                }

                //precheck castle => jump the rook over the king
                if (move.SpecialCode == (int)SpecialMovesCode.KingCastle) {
                    if (chessboard.State.TurnColor == TurnColor.White) {
                        UpdatePieceBitboard(ref chessboard.Position[(int)TurnColor.White, (int)PieceType.Rook].BitboardValue, BitOperations.ToBitboard(Square.H1), BitOperations.ToBitboard(Square.F1));
                    }
                    else {
                        UpdatePieceBitboard(ref chessboard.Position[(int)TurnColor.Black, (int)PieceType.Rook].BitboardValue, BitOperations.ToBitboard(Square.H8), BitOperations.ToBitboard(Square.F8));
                    }
                }
                else if (move.SpecialCode == (int)SpecialMovesCode.QueenCastle) {
                    if (chessboard.State.TurnColor == TurnColor.White) {
                        UpdatePieceBitboard(ref chessboard.Position[(int)TurnColor.White, (int)PieceType.Rook].BitboardValue, BitOperations.ToBitboard(Square.A1), BitOperations.ToBitboard(Square.D1));
                    }
                    else {
                        UpdatePieceBitboard(ref chessboard.Position[(int)TurnColor.Black, (int)PieceType.Rook].BitboardValue, BitOperations.ToBitboard(Square.A8), BitOperations.ToBitboard(Square.D8));
                    }
                }

                chessboard.State.EnPassantSquare = null;
            }

            // precheck if it's en passant first
            else if (move.SpecialCode == (int)SpecialMovesCode.EpCapture) {
                chessboard.State.HalfMoveClock = 0;
                chessboard.stateStack.Push(chessboard.State);

                if (chessboard.State.TurnColor == TurnColor.White) {
                    chessboard.Position[(int)TurnColor.Black, (int)PieceType.Pawn].BitboardValue ^= bitboardTo >> 8; // remove captured piece
                    Logger.Log(Channel.Debug, $"en passant capture, removing pawn on {BitOperations.ToSquare(bitboardTo >> 8)}");
                }
                else {
                    chessboard.Position[(int)TurnColor.White, (int)PieceType.Pawn].BitboardValue ^= bitboardTo << 8; // remove captured piece
                    Logger.Log(Channel.Debug, $"en passant capture by black, removing pawn on {BitOperations.ToSquare(bitboardTo << 8)}");
                }

                UpdatePieceBitboard(ref chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Pawn].BitboardValue, bitboardFrom, bitboardTo); // move pawn
                chessboard.State.EnPassantSquare = null;
            }

            // no special move, brute force to find correct piece to move
            else {
                for (int pieceTypeIndex = 0; pieceTypeIndex < 6; pieceTypeIndex++) {
                    if ((chessboard.Position[(int)chessboard.State.TurnColor, pieceTypeIndex].BitboardValue & bitboardFrom) != 0) {
                        // quiet moves
                        if ((chessboard.AllPieces & bitboardTo) == 0) {
                            if (pieceTypeIndex == (int)PieceType.Pawn) {
                                chessboard.State.HalfMoveClock = 0;
                                // check promotion
                                switch (move.SpecialCode) {
                                    case (int)SpecialMovesCode.BishopPromotion:
                                        chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Pawn].BitboardValue ^= bitboardFrom; // remove promoting pawn
                                        chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Knight].BitboardValue ^= bitboardTo; // spawn new knight
                                        break;

                                    case (int)SpecialMovesCode.KnightPromotion:
                                        chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Pawn].BitboardValue ^= bitboardFrom; // remove promoting pawn
                                        chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Bishop].BitboardValue ^= bitboardTo; // spawn new bishop
                                        break;

                                    case (int)SpecialMovesCode.RookPromotion:
                                        chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Pawn].BitboardValue ^= bitboardFrom; // remove promoting pawn
                                        chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Rook].BitboardValue ^= bitboardTo; // spawn new rook
                                        break;

                                    case (int)SpecialMovesCode.QueenPromotion:
                                        chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Pawn].BitboardValue ^= bitboardFrom; // remove promoting pawn
                                        chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Queen].BitboardValue ^= bitboardTo; // spawn new queen
                                        break;
                                    default: // no promotion
                                        chessboard.Position[(int)chessboard.State.TurnColor, pieceTypeIndex].BitboardValue ^= bitboardFrom; // remove old position
                                        chessboard.Position[(int)chessboard.State.TurnColor, pieceTypeIndex].BitboardValue ^= bitboardTo; // set new position
                                        break;
                                }
                            }
                            else {
                                chessboard.State.HalfMoveClock++;
                                chessboard.Position[(int)chessboard.State.TurnColor, pieceTypeIndex].BitboardValue ^= bitboardFrom; // remove old position
                                chessboard.Position[(int)chessboard.State.TurnColor, pieceTypeIndex].BitboardValue ^= bitboardTo; // set new position
                            }
                            chessboard.State.CapturedPiece = null;
                        }
                        // capture
                        else {
                            chessboard.State.HalfMoveClock = 0;

                            // check which capture piece it is
                            for (int opponantPieceTypeIndex = 0; opponantPieceTypeIndex < 6; opponantPieceTypeIndex++) {
                                if ((chessboard.Position[(int)chessboard.State.TurnColor ^ 1, opponantPieceTypeIndex].BitboardValue & bitboardTo) != 0) {
                                    // update piece positions and existance
                                    chessboard.Position[(int)chessboard.State.TurnColor ^ 1, opponantPieceTypeIndex].BitboardValue ^= bitboardTo; // remove captured piece
                                    chessboard.Position[(int)chessboard.State.TurnColor, pieceTypeIndex].BitboardValue ^= bitboardFrom; // remove old position
                                    chessboard.Position[(int)chessboard.State.TurnColor, pieceTypeIndex].BitboardValue ^= bitboardTo; // set new position

                                    // check promotion
                                    switch (move.SpecialCode) {
                                        case (int)SpecialMovesCode.BishopPromotionCapture:
                                            chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Pawn].BitboardValue ^= bitboardTo; // remove promoting pawn
                                            chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Bishop].BitboardValue ^= bitboardTo; // spawn new knight
                                            break;

                                        case (int)SpecialMovesCode.KnightPromotionCapture:
                                            chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Pawn].BitboardValue ^= bitboardTo; // remove promoting pawn
                                            chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Knight].BitboardValue ^= bitboardTo; // spawn new bishop
                                            break;

                                        case (int)SpecialMovesCode.RookPromotionCapture:
                                            chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Pawn].BitboardValue ^= bitboardTo; // remove promoting pawn
                                            chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Rook].BitboardValue ^= bitboardTo; // spawn new rook
                                            break;

                                        case (int)SpecialMovesCode.QueenPromotionCapture:
                                            chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Pawn].BitboardValue ^= bitboardTo; // remove promoting pawn
                                            chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Queen].BitboardValue ^= bitboardTo; // spawn new queen
                                            break;
                                    }

                                    chessboard.State.CapturedPiece = (PieceType)opponantPieceTypeIndex;
                                }
                            }
                        }

                        //push the current state of the position onto the stack
                        chessboard.stateStack.Push(chessboard.State);

                        // reset en passant square
                        chessboard.State.EnPassantSquare = null;

                        var whiteRooks = chessboard.Position[(int)TurnColor.White, (int)PieceType.Rook].BitboardValue;
                        var blackRooks = chessboard.Position[(int)TurnColor.Black, (int)PieceType.Rook].BitboardValue;

                        chessboard.State.CanWhiteQueenCastle &= (whiteRooks & King.CastlingRookMasks[0]) != 0;
                        chessboard.State.CanWhiteKingCastle &= (whiteRooks & King.CastlingRookMasks[1]) != 0;
                        chessboard.State.CanBlackQueenCastle &= (blackRooks & King.CastlingRookMasks[2]) != 0;
                        chessboard.State.CanBlackKingCastle &= (blackRooks & King.CastlingRookMasks[3]) != 0;

                        // (right to castle)
                        switch (pieceTypeIndex) {
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

            stopwatch.Stop();
            //Logger.Log(Channel.Benchmark, $"making move {move} in {stopwatch.ElapsedTicks} ns");
        }

        public static void UnmakeMove(Chessboard chessboard, Move move) {
            Stopwatch stopwatch = Stopwatch.StartNew();

            var bitboardFrom = move.FromBitboard();
            var bitboardTo = move.ToBitboard();
            //Logger.Log(Channel.Benchmark, $"getting bitboard for unmake in {stopwatch.Elapsed.TotalNanoseconds}ns");

            //Logger.Log(Channel.Benchmark, $"before pop unmaking move {move} in {stopwatch.Elapsed.TotalNanoseconds}ns");
            //stopwatch.Restart();
            //restore the previous state before latest move pushed move, the latest state provide the turn to play,
            var latestState = chessboard.stateStack.Pop(); // remove the latest move from the stack
            //Logger.Log(Channel.Benchmark, $"after pop unmaking move {move} in {stopwatch.Elapsed.TotalNanoseconds}ns");
            //stopwatch.Restart();

            chessboard.State.TurnColor = latestState.TurnColor; //get the state where at the start of the position before the white move => get back the state before the makemove
            // this means that every state is back to default except the color which goes back to before the makemove
            chessboard.State.CapturedPiece = null;
            //Logger.Log(Channel.Benchmark, $"before state on top unmaking move {move} in {stopwatch.Elapsed.TotalNanoseconds}ns");
            //stopwatch.Restart();

            var stateOnTop = chessboard.stateStack.Peek();
            chessboard.State.EnPassantSquare = stateOnTop.EnPassantSquare;
            chessboard.State.HalfMoveClock = stateOnTop.HalfMoveClock; // restore the halfmove to the state of the previous turn(end) before we play

            //Logger.Log(Channel.Benchmark, $"before enpassant ? unmaking move {move} in {stopwatch.Elapsed.TotalNanoseconds}ns");
            //var a = move.SpecialCode == (byte)SpecialMovesCode.EpCapture;
            //Logger.Log(Channel.Benchmark, $"enpassant ? unmaking move {move} in {stopwatch.Elapsed.TotalNanoseconds}ns");
            // restore castling right
            chessboard.State.CanWhiteKingCastle = latestState.CanWhiteKingCastle;
            chessboard.State.CanWhiteQueenCastle = latestState.CanWhiteQueenCastle;
            chessboard.State.CanBlackKingCastle = latestState.CanBlackKingCastle;
            chessboard.State.CanBlackQueenCastle = latestState.CanBlackQueenCastle;
            //Logger.Log(Channel.Benchmark, $"unmaking state in {stopwatch.Elapsed.TotalNanoseconds}ns");

            if (move.PROMOTION_FLAG) {
                if (move.CAPTURE_FLAG) { // restore captured piece
                    chessboard.Position[(int)latestState.TurnColor ^ 1, (int)latestState.CapturedPiece!].BitboardValue ^= bitboardTo;
                }
                //remove new piece
                for (int pieceTypeIndex = 0; pieceTypeIndex < chessboard.Position.GetLength(1); pieceTypeIndex++) {
                    if ((chessboard.Position[(int)latestState.TurnColor, pieceTypeIndex].BitboardValue & bitboardTo) != 0) {
                        chessboard.Position[(int)latestState.TurnColor, pieceTypeIndex].BitboardValue ^= bitboardTo;
                        break;
                    }
                }
                //restore pawn
                chessboard.Position[(int)latestState.TurnColor, (int)PieceType.Pawn].BitboardValue ^= bitboardFrom;
            }

            else if (move.CASTLE_FLAG) {
                UpdatePieceBitboard(ref chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.King].BitboardValue, bitboardTo, bitboardFrom); // restore king position

                //precheck castle => jump the rook over the king
                if (move.SpecialCode == (int)SpecialMovesCode.KingCastle) {
                    if (chessboard.State.TurnColor == TurnColor.White) {
                        UpdatePieceBitboard(ref chessboard.Position[(int)TurnColor.White, (int)PieceType.Rook].BitboardValue, BitOperations.ToBitboard(Square.F1), BitOperations.ToBitboard(Square.H1));
                    }
                    else {
                        UpdatePieceBitboard(ref chessboard.Position[(int)TurnColor.Black, (int)PieceType.Rook].BitboardValue, BitOperations.ToBitboard(Square.F8), BitOperations.ToBitboard(Square.H8));
                    }
                }
                else if (move.SpecialCode == (int)SpecialMovesCode.QueenCastle) {
                    if (chessboard.State.TurnColor == TurnColor.White) {
                        UpdatePieceBitboard(ref chessboard.Position[(int)TurnColor.White, (int)PieceType.Rook].BitboardValue, BitOperations.ToBitboard(Square.D1), BitOperations.ToBitboard(Square.A1));
                    }
                    else {
                        UpdatePieceBitboard(ref chessboard.Position[(int)TurnColor.Black, (int)PieceType.Rook].BitboardValue, BitOperations.ToBitboard(Square.D8), BitOperations.ToBitboard(Square.A8));
                    }
                }
            }

            else if (move.SpecialCode == (int)SpecialMovesCode.EpCapture) {
                UpdatePieceBitboard(ref chessboard.Position[(int)chessboard.State.TurnColor, (int)PieceType.Pawn].BitboardValue, bitboardTo, bitboardFrom); // restore pawn position
                if (chessboard.State.TurnColor == TurnColor.White) {
                    chessboard.Position[(int)TurnColor.Black, (int)PieceType.Pawn].BitboardValue ^= bitboardTo >> 8;
                }
                else {
                    chessboard.Position[(int)TurnColor.White, (int)PieceType.Pawn].BitboardValue ^= bitboardTo << 8;
                }
            }

            else {
                //Logger.Log(Channel.Benchmark, $"before unmaking move {move} in {stopwatch.Elapsed.TotalNanoseconds}ns");
                // restore last moved piece from latestState
                for (int pieceTypeIndex = 0; pieceTypeIndex < chessboard.Position.GetLength(1); pieceTypeIndex++) {
                    ref var piece = ref chessboard.Position[(int)chessboard.State.TurnColor, pieceTypeIndex];
                    if ((piece.BitboardValue & bitboardTo) != 0) {
                        UpdatePieceBitboard(ref piece.BitboardValue, bitboardTo, bitboardFrom); // restore piece position
                        break;
                    }
                }
                if (latestState.CapturedPiece.HasValue) {
                    PieceType capturedPiece = (PieceType)latestState.CapturedPiece;
                    chessboard.Position[(int)latestState.TurnColor ^ 1, (int)capturedPiece].BitboardValue ^= bitboardTo;
                }
            }

            stopwatch.Stop();
            //Logger.Log(Channel.Benchmark, $"unmaking move {move} in {stopwatch.ElapsedTicks} ns");
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