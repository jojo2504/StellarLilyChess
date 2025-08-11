using System.Text.Json;
using System.Text.Json.Serialization;
using Bitboard = ulong;

namespace Finder {
    /// <summary>
    /// This is a rank --> --- <br/>
    /// 1 to 8
    /// </summary>
    public enum Rank : int {
        RANK_1, RANK_2, RANK_3, RANK_4, RANK_5, RANK_6, RANK_7, RANK_8
    };

    /// <summary>
    /// This is a file --> | <br/>
    /// A to H
    /// </summary>
    public enum File : int {
        FILE_A, FILE_B, FILE_C, FILE_D, FILE_E, FILE_F, FILE_G, FILE_H
    };

    public enum Square : int {
        A1, B1, C1, D1, E1, F1, G1, H1,
        A2, B2, C2, D2, E2, F2, G2, H2,
        A3, B3, C3, D3, E3, F3, G3, H3,
        A4, B4, C4, D4, E4, F4, G4, H4,
        A5, B5, C5, D5, E5, F5, G5, H5,
        A6, B6, C6, D6, E6, F6, G6, H6,
        A7, B7, C7, D7, E7, F7, G7, H7,
        A8, B8, C8, D8, E8, F8, G8, H8
    }


    internal class Program {

        struct Magic {
            public Square square;
            public Bitboard magicNumber;
            public Bitboard mask;

            public override string ToString() {
                return $"{square}: {{ {magicNumber}, {mask} }}";
            }
        }

        static readonly JsonSerializerOptions jsonOptions = new() {
            Converters = { new JsonStringEnumConverter() },
            IncludeFields = true, // This is the key addition!
            WriteIndented = true  // Optional: makes JSON readable} // Serialize enums as strings
        };

        static Magic[]? LoadMagicTable(JsonSerializerOptions options, string filePath = @"RMagicTable.json") {
            try {
                string jsonString = System.IO.File.ReadAllText(filePath);
                //Console.WriteLine(jsonString);

                Magic[] magicTable = JsonSerializer.Deserialize<Magic[]>(jsonString, options);
                return magicTable;
            }
            catch (FileNotFoundException) {
                Console.WriteLine("Magic.json file not found.");
                return null;
            }
            catch (JsonException ex) {
                Console.WriteLine($"Error parsing JSON: {ex.Message}");
                return null;
            }
        }

        static void SerializeMagicTable(Magic[] magicTable, JsonSerializerOptions options, string filePath = @"RMagicTable.json") {
            string jsonString = JsonSerializer.Serialize(magicTable, options);

            try {
                //write string to file
                System.IO.File.WriteAllText(filePath, jsonString);
            }
            catch (Exception exception) {
                Console.WriteLine(exception.Message);
            }
        }

        public static void Main(string[] args) {
            int[] RBits = [
                12, 11, 11, 11, 11, 11, 11, 12,
                11, 10, 10, 10, 10, 10, 10, 11,
                11, 10, 10, 10, 10, 10, 10, 11,
                11, 10, 10, 10, 10, 10, 10, 11,
                11, 10, 10, 10, 10, 10, 10, 11,
                11, 10, 10, 10, 10, 10, 10, 11,
                11, 10, 10, 10, 10, 10, 10, 11,
                12, 11, 11, 11, 11, 11, 11, 12
            ];

            int[] BBits = [
                6, 5, 5, 5, 5, 5, 5, 6,
                5, 5, 5, 5, 5, 5, 5, 5,
                5, 5, 7, 7, 7, 7, 5, 5,
                5, 5, 7, 9, 9, 7, 5, 5,
                5, 5, 7, 9, 9, 7, 5, 5,
                5, 5, 7, 7, 7, 7, 5, 5,
                5, 5, 5, 5, 5, 5, 5, 5,
                6, 5, 5, 5, 5, 5, 5, 6
            ];

            Magic[] RookMagicTable = new Magic[64];
            Magic[] BishopMagicTable = new Magic[64];
            for (int i = 0; i < 64; i++) {
                RookMagicTable[i] = new Magic { square = (Square)i, magicNumber = 0 };
                BishopMagicTable[i] = new Magic { square = (Square)i, magicNumber = 0 };
            }

            int squareIndex;
            for(squareIndex = 0; squareIndex < 64; squareIndex++) {
                RookMagicTable[squareIndex].magicNumber = RookFinder.FindMagic(squareIndex, RBits[squareIndex]);
                RookMagicTable[squareIndex].mask = RookFinder.rmask(squareIndex);

                BishopMagicTable[squareIndex].magicNumber = BishopFinder.FindMagic(squareIndex, BBits[squareIndex]);
                BishopMagicTable[squareIndex].mask = BishopFinder.bmask(squareIndex);
            }

            SerializeMagicTable(RookMagicTable, jsonOptions, @"RMagicTable.json");
            SerializeMagicTable(BishopMagicTable, jsonOptions, @"BMagicTable.json");
        }
    }
}