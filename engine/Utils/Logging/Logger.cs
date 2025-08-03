using ChessEngine.Utils.Logging.Events;

namespace ChessEngine.Utils.Logging {
    public static class Logger {
        static Logger () {
            ClearLog();
        }
        private static string LogFilePath = Path.Combine("/home/jojo/Documents/c#/StellarLilyChess/engine/Utils/Logging", "logs.log");

        private static void LogMessage(LogEventLevel level = LogEventLevel.Information, params object[] Objects) {
            // Open the StreamWriter inside the using statement
            using (StreamWriter writer = new StreamWriter(LogFilePath, append: true)) {
                string prefix = $"[{level}] ";
                writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {prefix}{string.Join(" ", Objects.Select(x => x.ToString()))}");
            }
            if (level.Equals(LogEventLevel.Fatal)) {
                System.Environment.Exit(1);
            }
        }
        private static void LogMessage(string message, LogEventLevel level = LogEventLevel.Information) {
            // Open the StreamWriter inside the using statement
            using (StreamWriter writer = new StreamWriter(LogFilePath, append: true)) {
                string prefix = $"[{level}] ";
                writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {prefix}{message}");
            }
            if (level.Equals(LogEventLevel.Fatal)) {
                System.Environment.Exit(1);
            }
        }

        private static void LogMessage(Exception exception, LogEventLevel level = LogEventLevel.Information) {
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
                System.Environment.Exit(1);
            }
        }

        // Convenience methods
        public static void Log(string message = "") => LogMessage(message, LogEventLevel.Information);
        public static void Log(params object[] Objects) => LogMessage(LogEventLevel.Information, Objects);
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
                    System.IO.File.WriteAllText(LogFilePath, $"Log file cleared at {DateTime.Now}\n");
                }
            }
            catch (Exception ex) {
                Log($"Failed to clear log file: {ex.Message}");
            }
        }
    }
}