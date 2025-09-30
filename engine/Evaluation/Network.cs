using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ChessEngine.Evaluation {
    public struct Accumulator { // 1 neuron, many of them forms a network
        const int HL_SIZE = 2048; // hidden layer size
        public short[] values = new short[HL_SIZE];

        public Accumulator() {
        }
    }

    public struct Network {
        const int INPUT_SIZE = 768; // // relative to king position
        const int HL_SIZE = 2048; // hiden layer size

        // These parameters are part of the training configuration
        // These are the commonly used values as of 2024
        const int SCALE = 400;
        const short QA = 255;
        const short QB = 64;

        readonly Accumulator[] FeatureWeights = new Accumulator[INPUT_SIZE]; // 2048 * 768 // for each input, it has its own weight
        readonly Accumulator FeatureBias = new Accumulator(); // 2048;
        readonly short[] outputWeights = new short[2 * HL_SIZE]; // one for each perspective

        short outputBias;

        public AccumulatorPair AccumulatorPair { get; set; }

        public Network() {
            AccumulatorPair = new();
            Console.WriteLine("Init Network");
        }

        public static Network LoadNetwork(string filePath) {
            var network = new Network();

            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (var reader = new BinaryReader(fileStream)) {
                try {
                    Logger.Log(Channel.Debug, $"Loading features weights");
                    // Load accumulator weights (768 * 2048 shorts) - CORRECTED to signed
                    for (int i = 0; i < INPUT_SIZE; i++) {
                        network.FeatureWeights[i] = new Accumulator();
                        for (int j = 0; j < HL_SIZE; j++) {
                            network.FeatureWeights[i].values[j] = reader.ReadInt16(); // Changed from ReadUInt16
                        }
                    }

                    // Load accumulator biases (2048 shorts) - CORRECTED to signed
                    Logger.Log(Channel.Debug, "Loading accumulator biases:");
                    for (int i = 0; i < HL_SIZE; i++) {
                        network.FeatureBias.values[i] = reader.ReadInt16(); // Changed from ReadUInt16
                        network.AccumulatorPair.White.values[i] = network.FeatureBias.values[i];
                        network.AccumulatorPair.Black.values[i] = network.FeatureBias.values[i];
                    }

                    // Load output weights (4096 shorts) - CORRECTED to signed
                    for (int i = 0; i < 2 * HL_SIZE; i++) {
                        network.outputWeights[i] = reader.ReadInt16(); // Changed from ReadUInt16
                    }

                    // Load output bias (1 short) - CORRECTED to signed
                    network.outputBias = reader.ReadInt16(); // Changed from ReadUInt16

                    Logger.Log(Channel.Debug, $"Network loaded successfully from {filePath}");
                    Logger.Log(Channel.Debug, $"File size: {fileStream.Length} bytes");

                    return network;
                }
                catch (EndOfStreamException) {
                    throw new InvalidDataException("Unexpected end of file while reading network data");
                }
            }
        }

        public void AccumulatorAdd(Accumulator accumulator, int index) {
            var vectorSize = Vector<short>.Count; // Usually 8 or 16 shorts per vector
            var remainder = HL_SIZE % vectorSize;
            var vectorCount = HL_SIZE - remainder;

            var weightSpan = GetWeightSpan(index);

            // Process vectors
            for (int i = 0; i < vectorCount; i += vectorSize) {
                var accVector = new Vector<short>(accumulator.values, i);
                var weightVector = new Vector<short>(weightSpan.Slice(i, vectorSize));
                var result = accVector + weightVector;
                result.CopyTo(accumulator.values, i);
            }

            // Handle remainder
            for (int i = vectorCount; i < HL_SIZE; i++) {
                accumulator.values[i] += FeatureWeights[index].values[i];
            }
        }

        public void AccumulatorSub(Accumulator accumulator, int index) {
            var vectorSize = Vector<short>.Count; // Usually 8 or 16 shorts per vector
            var remainder = HL_SIZE % vectorSize;
            var vectorCount = HL_SIZE - remainder;

            var weightSpan = GetWeightSpan(index);

            // Process vectors
            for (int i = 0; i < vectorCount; i += vectorSize) {
                var accVector = new Vector<short>(accumulator.values, i);
                var weightVector = new Vector<short>(weightSpan.Slice(i, vectorSize));
                var result = accVector - weightVector;
                result.CopyTo(accumulator.values, i);
            }

            // Handle remainder
            for (int i = vectorCount; i < HL_SIZE; i++) {
                accumulator.values[i] -= FeatureWeights[index].values[i];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Span<short> GetWeightSpan(int index) {
            // Create a span for the weight row
            return MemoryMarshal.CreateSpan(
                ref FeatureWeights[index].values[0], HL_SIZE);
        }

        public int CalculateIndex(TurnColor perspective, Square square, PieceType pieceType, TurnColor side) {
            if (perspective == TurnColor.Black) {
                side = (TurnColor)(1 - (int)side); ;          // Flip side
                square = (Square)((int)square ^ 0b111000); // Vertically flip the square
            }

            // // return (int)KingPosition * (int)side * 64 * 6 + (int)pieceType * 64 + (int)square;
            return (int)side * 64 * 6 + (int)pieceType * 64 + (int)square;
        }

        int CReLU(short value, short min, short max) {
            return Math.Clamp(value, min, max);
        }
        
        int SCReLU(short value, short min, short max) {
            return Math.Clamp(value, min, max) * Math.Clamp(value, min, max);
        }

        int Activation(short value) {
            return SCReLU(value, 0, QA);
            //return CReLU(value, 0, QA);
        }

        public int Evaluate(Chessboard chessboard) {
            int colorIndex = (int)chessboard.State.TurnColor;
            Logger.Log(Channel.Debug, colorIndex);
            Logger.Log(Channel.Debug, (TurnColor)colorIndex);
            var accPair = AccumulatorPair;
            return Forward(
            colorIndex == 0 ? accPair.White : accPair.Black,
            colorIndex == 0 ? accPair.Black : accPair.White
            );
        }

        public int Forward(Accumulator stm_accumulator, Accumulator nstm_accumulator) {
            int eval = 0;

            // Dot product to the weights
            for (int i = 0; i < HL_SIZE; i++) {
                // BEWARE of integer overflows here.
                eval += Activation(stm_accumulator.values[i]) * Convert.ToInt32(outputWeights[i]);
                eval += Activation(nstm_accumulator.values[i]) * Convert.ToInt32(outputWeights[i + HL_SIZE]);
            }

            // Uncomment the following dequantization step when using SCReLU
            eval /= QA;
            eval += outputBias;

            eval *= SCALE;
            eval /= QA * QB;

            return eval;
        }
    }

    public struct AccumulatorPair {
        public Accumulator White;
        public Accumulator Black;

        public AccumulatorPair() {
            White = new Accumulator();
            Black = new Accumulator();
        }
    };
}