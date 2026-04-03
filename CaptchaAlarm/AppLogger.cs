namespace CaptchaAlarm
{
    /// <summary>
    /// Simple thread-safe file logger for debugging.
    /// Writes to <c>log.txt</c> in the application directory.
    /// </summary>
    public static class AppLogger
    {
        private static readonly string LogPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.txt");

        private static readonly object _lock = new();

        /// <summary>Appends a timestamped line to the log file.</summary>
        public static void Log(string message)
        {
            try
            {
                lock (_lock)
                {
                    File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}");
                }
            }
            catch
            {
                // Logging must never throw
            }
        }
    }
}
