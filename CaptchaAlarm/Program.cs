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
                // Check if disclaimer has been accepted
                var settings = SettingsManager.Load();

                if (!settings.DisclaimerAccepted)
                {
                    // Show disclaimer on first run
                    using var disclaimerForm = new DisclaimerForm();
                    var result = disclaimerForm.ShowDialog();

                    if (result != DialogResult.OK)
                    {
                        // User declined - exit application
                        AppLogger.Log("[Program] User declined disclaimer. Application exiting.");
                        return;
                    }

                    // User accepted - save to settings
                    settings.DisclaimerAccepted = true;
                    SettingsManager.Save(settings);
                    AppLogger.Log("[Program] User accepted disclaimer.");
                }

                // Run main application
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                AppLogger.Log($"[Program] Unhandled exception: {ex}");
                MessageBox.Show(
                    $"An unexpected error occurred:\n\n{ex.Message}\n\nCheck log.txt for details.",
                    "AlarmTool – Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
