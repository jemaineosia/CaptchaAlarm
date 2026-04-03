using CaptchaAlarm.Services;

namespace CaptchaAlarm
{
    /// <summary>
    /// Application entry point.
    /// </summary>
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Register notification handler before showing any UI
            NotificationService.Register();

            try
            {
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                AppLogger.Log($"[Program] Unhandled exception: {ex}");
                MessageBox.Show(
                    $"An unexpected error occurred:\n\n{ex.Message}\n\nCheck log.txt for details.",
                    "CaptchaAlarm – Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
