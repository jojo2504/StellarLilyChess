using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessEngine.Records {
    public record struct GameRecord {
        string moves = "";
        public readonly string Moves => moves;

        public GameRecord() {
        }

        public void AddMove(Move move) {
            moves += $"{move} ";
        }

        public string ConvertToPGN() {
            var moveArray = moves.Split(' ');
            StringBuilder stringBuilder = new();
            for (int i = 0; i < moveArray.Length; i++) {
                if ((i & 1) == 0) {
                    stringBuilder.Append($"{i / 2}.{moveArray[i]}");
                }
                else {
                    stringBuilder.AppendLine($" {moveArray[i]}");
                }
            }

            return stringBuilder.ToString();
        }
    }
}