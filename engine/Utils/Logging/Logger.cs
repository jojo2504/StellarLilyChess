using ChessEngine.Utils.Logging.Events;
using System;
using System.Text.Json;

namespace ChessEngine.Utils.Logging {
    public class LoggerConfig {
        public bool active { get; set; }
    }

    public class Config {
        public LoggerConfig Logger { get; set; }
    }

    public static class Logger {
        //private static string LogFilePath = Path.Combine($"{EnvironmentMethods.TryGetSolutionDirectoryInfo().FullName}/engine/Utils/Logging", "logs.log");
        static string LogFilePath = Path.Combine($"{AppDomain.CurrentDomain.BaseDirectory}", "Logs", "logs.log");
        static string jsonFile = System.IO.File.ReadAllText(@$"{AppDomain.CurrentDomain.BaseDirectory}/Config/config.json");
        static bool Active { get; set; }

        static Logger() {
            Config config = JsonSerializer.Deserialize<Config>(jsonFile);
            Active = config?.Logger?.active ?? false; // Default to false if not found

            InitLogFolder();
            ClearLog();
        }

        static void InitLogFolder() {
            var logPath = Path.Combine($"{AppDomain.CurrentDomain.BaseDirectory}", "Logs");
            if (!Directory.Exists(logPath))
                Directory.CreateDirectory(logPath);
        }

        private static void LogMessage(LogEventLevel level = LogEventLevel.Information, bool force = false, params object[] Objects) {
            if (Active || force) {
                // Open the StreamWriter inside the using statement
                using (StreamWriter writer = new StreamWriter(LogFilePath, append: true)) {
                    string prefix = $"[{level}] ";
                    writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {force} {Active} {prefix}{string.Join(" ", Objects.Select(x => x.ToString()))}");
                }
                if (level.Equals(LogEventLevel.Fatal)) {
                    Environment.Exit(1);
                }
            }
        }

        private static void LogMessage(string message, LogEventLevel level = LogEventLevel.Information, bool force = false) {
            if (Active || force) {
                // Open the StreamWriter inside the using statement
                using (StreamWriter writer = new StreamWriter(LogFilePath, append: true)) {
                    string prefix = $"[{level}] ";
                    writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {prefix}{message}");
                }
                if (level.Equals(LogEventLevel.Fatal)) {
                    Environment.Exit(1);
                }
            }
        }

        private static void LogMessage(Exception exception, LogEventLevel level = LogEventLevel.Information, bool force = false) {
            if (Active || force) {
                // Open the StreamWriter inside the using statement
                using (StreamWriter writer = new StreamWriter(LogFilePath, append: true)) {
                    string prefix = $"[{level}] ";
                    writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {prefix}");

                    if (exception != null) {
                        // Log the exception details (if any)
                        writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - Exception: {exception.Message}");
                        writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - Stack Trace: {exception.StackTrace}");
                    }
                }
                if (level.Equals(LogEventLevel.Fatal)) {
                    Environment.Exit(1);
                }
            }
        }

        // Convenience methods
        public static void Log(params object[] objects) => LogMessage(LogEventLevel.Information, false, objects);
        public static void LogForce(params object[] objects) => LogMessage(LogEventLevel.Information, true, objects);
        public static void Log(Exception exception) => LogMessage(exception, LogEventLevel.Information);
        public static void Important(string message = "") => LogMessage(message, LogEventLevel.Important);

        public static void Success(string message = "") => LogMessage(message, LogEventLevel.Success);

        public static void Warning(string message = "") => LogMessage(message, LogEventLevel.Warning);
        public static void Warning(Exception exception) => LogMessage(exception, LogEventLevel.Warning);

        public static void Error(string message = "") => LogMessage(message, LogEventLevel.Error);
        public static void Error(Exception exception) => LogMessage(exception, LogEventLevel.Error);

        public static void Fatal(string message = "") => LogMessage(message, LogEventLevel.Fatal);
        public static void Fatal(Exception exception) => LogMessage(exception, LogEventLevel.Fatal);

        public static void ClearLog() {
            try {
                if (System.IO.File.Exists(LogFilePath)) {
                    using (StreamWriter writer = new StreamWriter(LogFilePath, append: true)) {
                        writer.WriteLine($"Log file cleared at {DateTime.Now}\n");
                    }
                    System.IO.File.WriteAllText(LogFilePath, $"Log file cleared at {DateTime.Now}\n");
                }
            }
            catch (Exception ex) {
                Log($"Failed to clear log file: {ex.Message}");
            }
        }
    }
}