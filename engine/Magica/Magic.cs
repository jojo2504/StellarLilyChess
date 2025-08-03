using Bitboard = ulong;
using ChessEngine.Utils.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using ChessEngine.Pieces;
using System.Reflection;

namespace ChessEngine.Magica {
    public static class Magic {
        public struct SMagic {
            public Square square;
            public Bitboard magicNumber;
            public Bitboard mask;

            public override string ToString() {
                return $"{square}: {{ {magicNumber}, {mask} }}";
            }
        }

        public static readonly JsonSerializerOptions jsonOptions = new() {
            Converters = { new JsonStringEnumConverter() },
            IncludeFields = true, // This is the key addition!
            WriteIndented = true  // Optional: makes JSON readable} // Serialize enums as strings
        };

        public static SMagic[]? LoadMagicTable(JsonSerializerOptions options, string filePath) {
            try {
                var assembly = Assembly.GetExecutingAssembly();

                string jsonString = System.IO.File.ReadAllText(filePath);
                //Logger.Log(jsonString);

                SMagic[] magicTable = JsonSerializer.Deserialize<SMagic[]>(jsonString, options);
                return magicTable;
            }
            catch (FileNotFoundException ex) {
                Logger.Log(ex.Message);
                return null;
            }
            catch (JsonException ex) {
                Logger.Log($"Error parsing JSON: {ex.Message}");
                return null;
            }
        }
    }
}