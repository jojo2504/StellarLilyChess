using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ChessEngine.Evaluation {
    public struct Network {
        public const int INPUT_SIZE = 768; // // relative to king position
        public const int HL_SIZE = 2048; // hiden layer size

        // These parameters are part of the training configuration
        // These are the commonly used values as of 2024
        const int SCALE = 400;
        const int QA = 255;
        const int QB = 64;

        public readonly short[,] accumulatorWeights = new short[INPUT_SIZE, HL_SIZE]; // 768, 2048 // for each input, it has its own weight
        public readonly short[] accumulatorBiases = new short[HL_SIZE];
        public readonly short[] outputWeights = new short[2 * HL_SIZE]; // one for each perspective
        public short outputBias;

        public AccumulatorPair AccumulatorPair { get; set; }

        public Network() {
            AccumulatorPair = new();
            Console.WriteLine("Init Network");
        }

        public static Network LoadFromBinary(string filePath) {
            var network = new Network();

            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (var reader = new BinaryReader(fileStream)) {
                try {
                    // Load accumulator weights (768 * 2048 shorts) - CORRECTED to signed
                    for (int i = 0; i < INPUT_SIZE; i++) {
                        for (int j = 0; j < HL_SIZE; j++) {
                            network.accumulatorWeights[i, j] = reader.ReadInt16(); // Changed from ReadUInt16
                        }
                    }

                    // Load accumulator biases (2048 shorts) - CORRECTED to signed
                    for (int i = 0; i < HL_SIZE; i++) {
                        network.accumulatorBiases[i] = reader.ReadInt16(); // Changed from ReadUInt16
                    }

                    // Load output weights (4096 shorts) - CORRECTED to signed
                    for (int i = 0; i < 2 * HL_SIZE; i++) {
                        network.outputWeights[i] = reader.ReadInt16(); // Changed from ReadUInt16
                    }

                    // Load output bias (1 short) - CORRECTED to signed
                    network.outputBias = reader.ReadInt16(); // Changed from ReadUInt16

                    Console.WriteLine($"Network loaded successfully from {filePath}");
                    Console.WriteLine($"File size: {fileStream.Length} bytes");

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
                accumulator.values[i] += accumulatorWeights[index, i];
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
                accumulator.values[i] -= accumulatorWeights[index, i];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Span<short> GetWeightSpan(int index) {
            // Create a span for the weight row
            return MemoryMarshal.CreateSpan(
                ref accumulatorWeights[index, 0], HL_SIZE);
        }

        public int CalculateIndex(TurnColor perspective, Square square, PieceType pieceType, TurnColor side) {
            if (perspective == TurnColor.Black) {
                side = 1 - side;          // Flip side
                square = (Square)((int)square ^ 0b111000); // Vertically flip the square
            }

            // // return (int)KingPosition * (int)side * 64 * 6 + (int)pieceType * 64 + (int)square;
            return (int)side * 64 * 6 + (int)pieceType * 64 + (int)square;
        }

        short CReLU(short value, short min, short max) {
            if (value <= min)
                return min;

            if (value >= max)
                return max;

            return value;
        }

        int Activation(short value) {
            //return SCReLU(value, 0, QA);
            return CReLU(value, 0, QA);
        }

        public int Evaluate(Chessboard chessboard) {
            int colorIndex = (int)chessboard.State.TurnColor;
            var accPair = NNUE.Network.AccumulatorPair;
            return NNUE.Network.Forward(
            colorIndex == 0 ? accPair.White : accPair.Black,
            colorIndex == 0 ? accPair.Black : accPair.White
            );
        }

        public int Forward(Accumulator stm_accumulator, Accumulator nstm_accumulator) {
            int eval = 0;

            // Dot product to the weights
            for (int i = 0; i < HL_SIZE; i++) {
                // BEWARE of integer overflows here.
                eval += Activation(stm_accumulator.values[i]) * outputWeights[i];
                eval += Activation(nstm_accumulator.values[i]) * outputWeights[i + HL_SIZE];
            }

            // Uncomment the following dequantization step when using SCReLU
            // eval /= QA;
            eval += outputBias;

            eval *= SCALE;
            eval /= QA * QB;

            return eval;
        }
    }

    public struct Accumulator { // 1 neuron, many of them forms a network
        const int HL_SIZE = 2048; // hiden layer size
        public short[] values = new short[HL_SIZE];

        public Accumulator() {
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