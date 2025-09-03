namespace ChessEngine.SearchNamespace {
    public record struct TranspositionData {
        public int depth;
        public int score;
        public Move bestMove;
        public NodeType nodeType;

        public TranspositionData(int depth, int score, Move bestMove, NodeType nodeType) {
            this.depth = depth;
            this.score = score;
            this.bestMove = bestMove;
            this.nodeType = nodeType;
        }
    }
}