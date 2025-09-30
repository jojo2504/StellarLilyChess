using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChessEngine.SearchNamespace {
    internal class TranspositionTable {
        internal readonly Dictionary<ulong, TranspositionData> table;
        private readonly int maxSize;

        internal TranspositionTable(int maxSizeInMB) {
            maxSize = maxSizeInMB * 1024 * 1024 / (sizeof(ulong) + sizeof(int) * 3 + sizeof(byte)); // Rough estimate of entry size
            table = new Dictionary<ulong, TranspositionData>(maxSize);
        }

        internal TranspositionTable() {
            table = new Dictionary<ulong, TranspositionData>();
        }

        internal void Store(ulong key, TranspositionData data) {
            table[key] = data;
        }

        internal void Store(ulong key, int depth, int score, Move? bestMove, NodeType nodeType) {
            if (bestMove == null) {
                throw new ArgumentNullException(nameof(bestMove), "bestMove cannot be null");
            }

            TranspositionData data = new(depth, score, (Move)bestMove, nodeType);
            table[key] = data;
        }
    }
}