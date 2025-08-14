using ChessEngine.Utils.Logging.Events;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChessEngine.Utils.Logging {
    public enum Channel : byte {
        General,
        Debug,
        Game,
        Benchmark,
    }

    public class ChannelConfig {
        public bool Active { get; set; }
    }

    public class Config {
        [JsonConverter(typeof(ChannelEnumDictionaryConverter))]
        public required Dictionary<Channel, ChannelConfig> Channels { get; set; }
    }

    public class ChannelEnumDictionaryConverter : JsonConverter<Dictionary<Channel, ChannelConfig>> {
        public override Dictionary<Channel, ChannelConfig> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            var dict = new Dictionary<Channel, ChannelConfig>();
            using (var jsonDoc = JsonDocument.ParseValue(ref reader)) {
                foreach (var prop in jsonDoc.RootElement.EnumerateObject()) {
                    if (Enum.TryParse<Channel>(prop.Name, out var channel)) {
                        var config = prop.Value.Deserialize<ChannelConfig>(options);
                        dict[channel] = config!;
                    }
                }
            }
            return dict;
        }
    
        public override void Write(Utf8JsonWriter writer, Dictionary<Channel, ChannelConfig> value, JsonSerializerOptions options) {
            writer.WriteStartObject();
            foreach (var kvp in value) {
                writer.WritePropertyName(kvp.Key.ToString());
                JsonSerializer.Serialize(writer, kvp.Value, options);
            }
            writer.WriteEndObject();
        }
    }

    public static class Logger {
        //private static string LogFilePath = Path.Combine($"{EnvironmentMethods.TryGetSolutionDirectoryInfo().FullName}/engine/Utils/Logging", "logs.log");
        static readonly string LogPath = Path.Combine($"{AppDomain.CurrentDomain.BaseDirectory}", "Logs");
        static readonly string jsonFile = System.IO.File.ReadAllText(@$"{AppDomain.CurrentDomain.BaseDirectory}/Config/config.json");
        static readonly Config _config;
        public static Config Config => _config;

        static Logger() {
            InitLogFolder();
            ClearLogs();

            _config = JsonSerializer.Deserialize<Config>(jsonFile);
        }

        static void InitLogFolder() {
            var logPath = Path.Combine($"{AppDomain.CurrentDomain.BaseDirectory}", "Logs");
            if (!Directory.Exists(logPath))
                Directory.CreateDirectory(logPath);
        }

        private static void LogMessage(Channel channel, LogEventLevel level = LogEventLevel.Information, params object[] Objects) {
            if (_config.Channels[channel].Active) {
                // Open the StreamWriter inside the using statement
                using (StreamWriter writer = new(Path.Combine(LogPath, @$"{channel}-logs.log"), append: true)) {
                    string prefix = $"[{level}]";
                    writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {prefix} {string.Join(" ", Objects.Select(x => x.ToString()))}");
                }
                if (level.Equals(LogEventLevel.Fatal)) {
                    Environment.Exit(1);
                }
            }
        }

        private static void LogMessage(Channel[] channels, LogEventLevel level = LogEventLevel.Information, params object[] Objects) {
            foreach (Channel channel in channels) {
                if (_config.Channels[channel].Active) {
                    // Open the StreamWriter inside the using statement
                    using (StreamWriter writer = new(Path.Combine(LogPath, @$"{channel}-logs.log"), append: true)) {
                        string prefix = $"[{level}]";
                        writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {prefix} {string.Join(" ", Objects.Select(x => x.ToString()))}");
                    }
                    if (level.Equals(LogEventLevel.Fatal)) {
                        Environment.Exit(1);
                    }
                }
            }
        }

        // Convenience methods
        public static void Log(Channel channel = Channel.General, params object[] objects) => LogMessage(channel, LogEventLevel.Information, objects);
        public static void Log(Channel[] channels, params object[] objects) => LogMessage(channels, LogEventLevel.Information, objects);

        public static void Important(Channel channel = Channel.General, params object[] objects) => LogMessage(channel, LogEventLevel.Important, objects);
        public static void Important(Channel[] channels, params object[] objects) => LogMessage(channels, LogEventLevel.Important, objects);

        public static void Success(Channel channel = Channel.General, params object[] objects) => LogMessage(channel, LogEventLevel.Success, objects);
        public static void Success(Channel[] channels, params object[] objects) => LogMessage(channels, LogEventLevel.Success, objects);

        public static void Warning(Channel channel = Channel.General, params object[] objects) => LogMessage(channel, LogEventLevel.Warning, objects);
        public static void Warning(Channel[] channels, params object[] objects) => LogMessage(channels, LogEventLevel.Warning, objects);

        public static void Error(Channel channel = Channel.General, params object[] objects) => LogMessage(channel, LogEventLevel.Error, objects);
        public static void Error(Channel[] channels, params object[] objects) => LogMessage(channels, LogEventLevel.Error, objects);

        public static void Fatal(Channel channel = Channel.General, params object[] objects) => LogMessage(channel, LogEventLevel.Fatal, objects);
        public static void Fatal(Channel[] channels, params object[] objects) => LogMessage(channels, LogEventLevel.Fatal, objects);

        public static void ClearLogs() {
            foreach (string channel in Enum.GetNames<Channel>()) {
                try {
                    if (System.IO.File.Exists(Path.Combine(LogPath, @$"{channel}-logs.log"))) {
                        using (StreamWriter writer = new StreamWriter(Path.Combine(LogPath, @$"{channel}-logs.log"), append: true)) {
                            writer.WriteLine($"Log file cleared at {DateTime.Now}\n");
                        }
                        System.IO.File.WriteAllText(Path.Combine(LogPath, @$"{channel}-logs.log"), $"Log file cleared at {DateTime.Now}\n");
                    }
                }
                catch (Exception ex) {
                    Log(Channel.General, $"Failed to clear log file: {ex.Message}");
                }
            }
        }
    }

    public class LoggerScope : IDisposable {
        private readonly bool _previousState;
        private readonly Channel _targetChannel;

        public LoggerScope(Channel channel, bool active = true) {
            // Save target channel
            _targetChannel = channel;
            // Save the previous state
            _previousState = Logger.Config.Channels[channel].Active;
            // Set the new state
            Logger.Config.Channels[channel].Active = active;
        }

        public void Dispose() {
            // Restore the previous state
            Logger.Config.Channels[_targetChannel].Active = _previousState;
        }
    }
}