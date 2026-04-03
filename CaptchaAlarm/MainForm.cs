using System.Drawing;
using System.Drawing.Drawing2D;
using CaptchaAlarm.Models;
using CaptchaAlarm.Services;

namespace CaptchaAlarm
{
    /// <summary>
    /// Main application window. Hosts monitoring controls, detection log, settings,
    /// statistics, and system-tray integration.
    /// </summary>
    public partial class MainForm : Form
    {
        // ── Services ────────────────────────────────────────────────────────────
        private readonly ScreenCaptureService _capture = new();
        private readonly CaptchaDetectionService _detection = new();
        private readonly AlarmService _alarm = new();

        // ── State ────────────────────────────────────────────────────────────────
        private AppSettings _settings = SettingsManager.Load();
        private System.Windows.Forms.Timer? _monitorTimer;
        private bool _isMonitoring;
        private bool _isPaused;
        private bool _isProcessing;

        // Statistics
        private int _totalDetections;
        private DateTime _sessionStart;
        private DateTime _lastDetection;
        private DateTime _lastAlarmTime = DateTime.MinValue;
        private const int AlarmCooldownSeconds = 30;

        // Flash animation for status indicator
        private System.Windows.Forms.Timer? _flashTimer;
        private bool _flashState;
        private bool _captchaDetectedState;

        public MainForm()
        {
            InitializeComponent();
            ApplySettings();
            SetupSystemTray();
            SetupTimers();
            UpdateStatusDisplay(MonitorStatus.Stopped);
            NotificationService.Register();
            AppLogger.Log("[App] CaptchaAlarm started.");
        }

        // ── Monitoring ──────────────────────────────────────────────────────────

        private void StartMonitoring()
        {
            if (_isMonitoring) return;
            _isMonitoring = true;
            _isPaused = false;
            _captchaDetectedState = false;
            _sessionStart = DateTime.Now;

            _monitorTimer!.Interval = _settings.MonitoringInterval;
            _monitorTimer.Start();
            _flashTimer!.Stop();

            UpdateStatusDisplay(MonitorStatus.Active);
            btnStart.Text = "⏹ Stop Monitoring";
            btnPause.Enabled = true;
            AppLogger.Log("[Monitor] Started.");
        }

        private void StopMonitoring()
        {
            if (!_isMonitoring) return;
            _isMonitoring = false;
            _isPaused = false;

            _monitorTimer!.Stop();
            _flashTimer!.Stop();
            _alarm.Stop();

            UpdateStatusDisplay(MonitorStatus.Stopped);
            btnStart.Text = "▶ Start Monitoring";
            btnPause.Enabled = false;
            btnPause.Text = "⏸ Pause";
            AppLogger.Log("[Monitor] Stopped.");
        }

        private void PauseResume()
        {
            if (!_isMonitoring) return;

            _isPaused = !_isPaused;
            if (_isPaused)
            {
                _monitorTimer!.Stop();
                btnPause.Text = "▶ Resume";
                UpdateStatusDisplay(MonitorStatus.Paused);
                AppLogger.Log("[Monitor] Paused.");
            }
            else
            {
                _monitorTimer!.Start();
                btnPause.Text = "⏸ Pause";
                UpdateStatusDisplay(MonitorStatus.Active);
                AppLogger.Log("[Monitor] Resumed.");
            }
        }

        private async void OnMonitorTick(object? sender, EventArgs e)
        {
            if (_isProcessing) return;
            _isProcessing = true;

            try
            {
                await Task.Run(() => ProcessFrame());
            }
            finally
            {
                _isProcessing = false;
            }
        }

        private void ProcessFrame()
        {
            Bitmap? screenshot = null;
            try
            {
                screenshot = _capture.CaptureScreen();
                if (screenshot == null) return;

                var result = _detection.Analyse(screenshot, _settings.ConfidenceThreshold);

                if (result.IsDetected)
                {
                    // Cooldown check to avoid spam
                    var elapsed = (DateTime.Now - _lastAlarmTime).TotalSeconds;
                    if (elapsed >= AlarmCooldownSeconds)
                    {
                        _lastAlarmTime = DateTime.Now;

                        // Save screenshot if configured
                        string? screenshotPath = null;
                        if (_settings.SaveScreenshots)
                            screenshotPath = _capture.SaveScreenshot(screenshot, _settings.ScreenshotPath);

                        result.ScreenshotPath = screenshotPath;

                        // UI + alarm on UI thread
                        BeginInvoke(() => OnCaptchaDetected(result));
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Log($"[Monitor] ProcessFrame error: {ex.Message}");
            }
            finally
            {
                screenshot?.Dispose();
                GC.Collect(0, GCCollectionMode.Optimized);
            }
        }

        private void OnCaptchaDetected(DetectionResult result)
        {
            _totalDetections++;
            _lastDetection = DateTime.Now;
            _captchaDetectedState = true;

            // Update status
            UpdateStatusDisplay(MonitorStatus.Detected);

            // Log entry
            var logEntry = new DetectionLog
            {
                Timestamp = _lastDetection,
                ConfidenceScore = result.ConfidenceScore,
                Message = result.MatchedKeywords.Count > 0
                    ? $"Keywords: {string.Join(", ", result.MatchedKeywords)}"
                    : "Colour/layout pattern matched",
                MatchedKeywords = result.MatchedKeywords
            };

            listBoxLog.Items.Add(logEntry.ToString());
            if (listBoxLog.Items.Count > 0)
                listBoxLog.SelectedIndex = listBoxLog.Items.Count - 1;

            UpdateStats();

            // Alarm sound
            if (_settings.EnableSound)
                _alarm.Play(_settings.AlarmSoundPath, _settings.LoopSound, _settings.LoopCount);

            // Toast notification
            if (_settings.EnableNotifications)
                NotificationService.Notify(result.Timestamp, result.ConfidenceScore);

            // Tray balloon
            if (trayIcon.Visible)
                trayIcon.ShowBalloonTip(5000, "⚠️ CAPTCHA DETECTED!",
                    $"Daeva Verification detected!\nScore: {result.ConfidenceScore}", ToolTipIcon.Warning);

            AppLogger.Log($"[Monitor] CAPTCHA DETECTED! Score={result.ConfidenceScore}, " +
                          $"Keywords=[{string.Join(",", result.MatchedKeywords)}]");

            // Start flash
            _flashTimer!.Start();
        }

        // ── Status display ───────────────────────────────────────────────────────

        private enum MonitorStatus { Stopped, Active, Paused, Detected }

        private void UpdateStatusDisplay(MonitorStatus status)
        {
            switch (status)
            {
                case MonitorStatus.Stopped:
                    lblStatus.Text = "● Stopped";
                    lblStatus.ForeColor = Color.FromArgb(220, 80, 80);
                    panelStatus.BackColor = Color.FromArgb(45, 45, 45);
                    break;
                case MonitorStatus.Active:
                    lblStatus.Text = "● Monitoring Active";
                    lblStatus.ForeColor = Color.FromArgb(80, 200, 120);
                    panelStatus.BackColor = Color.FromArgb(35, 55, 35);
                    break;
                case MonitorStatus.Paused:
                    lblStatus.Text = "● Paused";
                    lblStatus.ForeColor = Color.FromArgb(220, 180, 60);
                    panelStatus.BackColor = Color.FromArgb(50, 45, 25);
                    break;
                case MonitorStatus.Detected:
                    lblStatus.Text = "⚠ CAPTCHA DETECTED!";
                    lblStatus.ForeColor = Color.Yellow;
                    panelStatus.BackColor = Color.FromArgb(80, 50, 0);
                    break;
            }
        }

        private void OnFlashTick(object? sender, EventArgs e)
        {
            if (!_captchaDetectedState) { _flashTimer!.Stop(); return; }

            _flashState = !_flashState;
            lblStatus.ForeColor = _flashState ? Color.Yellow : Color.OrangeRed;
        }

        // ── Settings ─────────────────────────────────────────────────────────────

        private void ApplySettings()
        {
            // Interval
            trackInterval.Value = Math.Clamp(_settings.MonitoringInterval / 1000, 1, 10);
            lblIntervalValue.Text = $"{trackInterval.Value}s";

            // Threshold
            trackThreshold.Value = Math.Clamp(_settings.ConfidenceThreshold, 50, 100);
            lblThresholdValue.Text = trackThreshold.Value.ToString();

            // Toggles
            chkNotifications.Checked = _settings.EnableNotifications;
            chkSound.Checked = _settings.EnableSound;
            chkLoopSound.Checked = _settings.LoopSound;
            chkSaveScreenshots.Checked = _settings.SaveScreenshots;
            chkMinimizeToTray.Checked = _settings.MinimizeToTray;

            // Sound file
            txtSoundPath.Text = _settings.AlarmSoundPath;

            if (_monitorTimer != null)
                _monitorTimer.Interval = _settings.MonitoringInterval;
        }

        private void SaveSettings()
        {
            _settings.MonitoringInterval = trackInterval.Value * 1000;
            _settings.ConfidenceThreshold = trackThreshold.Value;
            _settings.EnableNotifications = chkNotifications.Checked;
            _settings.EnableSound = chkSound.Checked;
            _settings.LoopSound = chkLoopSound.Checked;
            _settings.SaveScreenshots = chkSaveScreenshots.Checked;
            _settings.MinimizeToTray = chkMinimizeToTray.Checked;
            _settings.AlarmSoundPath = txtSoundPath.Text;

            SettingsManager.Save(_settings);

            if (_monitorTimer != null)
                _monitorTimer.Interval = _settings.MonitoringInterval;

            MessageBox.Show("Settings saved successfully.", "CaptchaAlarm",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ── Statistics ───────────────────────────────────────────────────────────

        private void UpdateStats()
        {
            lblTotalDetections.Text = $"Total Detections: {_totalDetections}";
            lblSessionStart.Text = $"Session Start: {_sessionStart:HH:mm:ss}";
            lblLastDetection.Text = $"Last Detection: {_lastDetection:HH:mm:ss}";
        }

        // ── System tray ──────────────────────────────────────────────────────────

        private void SetupSystemTray()
        {
            trayIcon.Text = "CaptchaAlarm";
            trayIcon.Visible = false;

            var ctxMenu = new ContextMenuStrip();
            ctxMenu.Items.Add("Show / Hide", null, (_, _) => ToggleVisibility());
            ctxMenu.Items.Add("Start / Stop", null, (_, _) => ToggleMonitoring());
            ctxMenu.Items.Add(new ToolStripSeparator());
            ctxMenu.Items.Add("Exit", null, (_, _) => ExitApplication());
            trayIcon.ContextMenuStrip = ctxMenu;

            trayIcon.DoubleClick += (_, _) => ToggleVisibility();
        }

        private void ToggleVisibility()
        {
            if (Visible)
            {
                Hide();
                trayIcon.Visible = true;
            }
            else
            {
                Show();
                WindowState = FormWindowState.Normal;
                BringToFront();
                Activate();
            }
        }

        private void ToggleMonitoring()
        {
            if (_isMonitoring) StopMonitoring(); else StartMonitoring();
        }

        // ── Timers setup ─────────────────────────────────────────────────────────

        private void SetupTimers()
        {
            _monitorTimer = new System.Windows.Forms.Timer { Interval = _settings.MonitoringInterval };
            _monitorTimer.Tick += OnMonitorTick;

            _flashTimer = new System.Windows.Forms.Timer { Interval = 400 };
            _flashTimer.Tick += OnFlashTick;
        }

        // ── Exit ─────────────────────────────────────────────────────────────────

        private void ExitApplication()
        {
            StopMonitoring();
            NotificationService.Unregister();
            trayIcon.Visible = false;
            _capture.Dispose();
            _detection.Dispose();
            _alarm.Dispose();
            AppLogger.Log("[App] CaptchaAlarm exited.");
            Application.Exit();
        }

        // ── Event handlers (wired in Designer) ───────────────────────────────────

        private void BtnStart_Click(object sender, EventArgs e)
        {
            if (_isMonitoring) StopMonitoring(); else StartMonitoring();
        }

        private void BtnPause_Click(object sender, EventArgs e) => PauseResume();

        private void BtnClearLog_Click(object sender, EventArgs e)
        {
            listBoxLog.Items.Clear();
            _captchaDetectedState = false;
            _flashTimer!.Stop();
            if (_isMonitoring) UpdateStatusDisplay(MonitorStatus.Active);
        }

        private void BtnBrowseSound_Click(object sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog
            {
                Title = "Select Alarm Sound",
                Filter = "Audio Files (*.wav;*.mp3)|*.wav;*.mp3|All Files (*.*)|*.*",
                InitialDirectory = AppDomain.CurrentDomain.BaseDirectory
            };
            if (dlg.ShowDialog() == DialogResult.OK)
                txtSoundPath.Text = dlg.FileName;
        }

        private void BtnTestSound_Click(object sender, EventArgs e)
        {
            var path = txtSoundPath.Text;
            if (!File.Exists(path))
            {
                MessageBox.Show($"Sound file not found:\n{path}", "CaptchaAlarm",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            _alarm.TestSound(path);
        }

        private void BtnSaveSettings_Click(object sender, EventArgs e) => SaveSettings();

        private void TrackInterval_Scroll(object sender, EventArgs e)
        {
            lblIntervalValue.Text = $"{trackInterval.Value}s";
        }

        private void TrackThreshold_Scroll(object sender, EventArgs e)
        {
            lblThresholdValue.Text = trackThreshold.Value.ToString();
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (_settings.MinimizeToTray && WindowState == FormWindowState.Minimized)
            {
                Hide();
                trayIcon.Visible = true;
                trayIcon.ShowBalloonTip(2000, "CaptchaAlarm",
                    "Application minimised to tray. Double-click to restore.", ToolTipIcon.Info);
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing && _settings.MinimizeToTray)
            {
                e.Cancel = true;
                Hide();
                trayIcon.Visible = true;
            }
            else
            {
                ExitApplication();
            }
        }
    }
}
