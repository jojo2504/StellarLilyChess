namespace ChessEngine.SearchNamespace {
    internal record struct TranspositionData {
        internal int depth;
        internal int score;
        internal Move bestMove;
        internal NodeType nodeType;

        internal TranspositionData(int depth, int score, Move bestMove, NodeType nodeType) {
            this.depth = depth;
            this.score = score;
            this.bestMove = bestMove;
            this.nodeType = nodeType;
        }
    }
}