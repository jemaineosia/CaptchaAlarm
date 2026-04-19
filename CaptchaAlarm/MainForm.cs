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
        private const int AlarmCooldownSeconds = 60;

        // Flash animation for status indicator
        private System.Windows.Forms.Timer? _flashTimer;
        private bool _flashState;
        private bool _captchaDetectedState;

        public MainForm()
        {
            InitializeComponent();

            // Adjust splitter distance after form is shown and fully laid out
            Shown += (s, e) => AdjustSplitterDistance();

            ApplySettings();
            SetupSystemTray();
            SetupTimers();
            UpdateStatusDisplay(MonitorStatus.Stopped);
            lblTrial.Visible = false;
            txtHwidStat.Text = TrialService.GetHwidPublic();
            NotificationService.Register();
            AppLogger.Log("[App] AlarmTool started.");
        }

        // ── Trial ────────────────────────────────────────────────────────────────

        private int _daysLeft;

        private void UpdateTrialLabel(int daysLeft)
        {
            _daysLeft = daysLeft;
            if (daysLeft <= 0)
            {
                lblTrial.Text = "⚠ Trial expired";
                lblTrial.ForeColor = System.Drawing.Color.FromArgb(220, 80, 80);
            }
            else if (daysLeft <= 2)
            {
                lblTrial.Text = $"⚠ Trial: {daysLeft} day(s) left";
                lblTrial.ForeColor = System.Drawing.Color.FromArgb(255, 120, 60);
            }
            else
            {
                lblTrial.Text = $"Trial: {daysLeft} day(s) left";
                lblTrial.ForeColor = System.Drawing.Color.FromArgb(255, 185, 60);
            }
        }

        private void AdjustSplitterDistance()
        {
            try
            {
                // Set min sizes first - now the form is shown and has valid dimensions
                splitMain.Panel1MinSize = 320;
                splitMain.Panel2MinSize = 220;

                // Now adjust splitter to be proportional to actual width
                int containerWidth = splitMain.Width;
                if (containerWidth > 0)
                {
                    int minDistance = splitMain.Panel1MinSize;
                    int maxDistance = containerWidth - splitMain.Panel2MinSize - splitMain.SplitterWidth;

                    // Set to 54% of container width, clamped to valid range
                    int desiredDistance = (int)(containerWidth * 0.54);
                    if (desiredDistance >= minDistance && desiredDistance <= maxDistance)
                    {
                        splitMain.SplitterDistance = desiredDistance;
                    }
                }
            }
            catch (Exception ex)
            {
                // If adjustment fails, log it but don't crash
                AppLogger.Log($"[UI] Failed to set splitter distance: {ex.Message}");
            }
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

// Adaptive threshold: 200 with OCR active, 80 on colour/layout only
int alarmThreshold = _detection.IsOcrAvailable ? 200 : 80;

if (result.IsDetected && result.ConfidenceScore >= alarmThreshold)
{
    var elapsed = (DateTime.Now - _lastAlarmTime).TotalSeconds;
    if (elapsed >= AlarmCooldownSeconds)
    {
        _lastAlarmTime = DateTime.Now;
        if (_settings.SaveScreenshots)
            result.ScreenshotPath = _capture.SaveScreenshot(screenshot, _settings.ScreenshotPath);

        BeginInvoke(() => OnCaptchaDetected(result, true));
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

        private void OnCaptchaDetected(DetectionResult result, bool triggerAlarm)
        {
            _totalDetections++;
            _lastDetection = DateTime.Now;

            if (triggerAlarm)
            {
                _captchaDetectedState = true;
                UpdateStatusDisplay(MonitorStatus.Detected);
            }

            // Always add to the Detection Log listbox
            var logEntry = new DetectionLog
            {
                Timestamp = _lastDetection,
                ConfidenceScore = result.ConfidenceScore,
                AlarmTriggered = triggerAlarm,
                Message = result.MatchedKeywords.Count > 0
                    ? $"Keywords: {string.Join(", ", result.MatchedKeywords)}"
                    : "Colour/layout pattern matched",
                MatchedKeywords = result.MatchedKeywords
            };

            listBoxLog.Items.Add(logEntry.ToString());
            if (listBoxLog.Items.Count > 0)
                listBoxLog.SelectedIndex = listBoxLog.Items.Count - 1;

            UpdateStats();

            if (triggerAlarm)
            {
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

                AppLogger.Log($"[Monitor] *** ALARM *** Score={result.ConfidenceScore}, " +
                              $"Keywords=[{string.Join(",", result.MatchedKeywords)}]");

                // Auto-solve via OpenAI
                if (_settings.Enable2Captcha && !string.IsNullOrWhiteSpace(_settings.OpenAiApiKey))
                    _ = SolveWith2CaptchaAsync(result);

                _flashTimer!.Start();
            }
            else
            {
                AppLogger.Log($"[Monitor] Logged (no alarm) Score={result.ConfidenceScore}");
            }
        }

        // ── 2captcha auto-solve ──────────────────────────────────────────────────

        private async Task SolveWith2CaptchaAsync(DetectionResult result)
        {
            var region = result.CaptchaRegion;
            if (region == null)
            {
                BeginInvoke(() => AddRawLogEntry("🔍 Auto-solve: could not locate CAPTCHA image region."));
                return;
            }

            // Save debug crop
            try
            {
                string debugPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "captcha_region_debug.png");
                region.Save(debugPath, System.Drawing.Imaging.ImageFormat.Png);
                AppLogger.Log($"[Solver] Debug crop saved: {debugPath}");
            }
            catch { /* non-critical */ }

            string mode = "OpenAI";
            BeginInvoke(() => AddRawLogEntry("⏳ Auto-solve (OpenAI GPT-4o): submitting CAPTCHA..."));

            (string? solution, string? error) = (null, null);

            if (string.IsNullOrWhiteSpace(_settings.OpenAiApiKey))
                error = "OpenAI API key not set.";
            else
                (solution, error) = await Services.OpenAiCaptchaSolver.SolveAsync(_settings.OpenAiApiKey, region);

            region.Dispose();

            if (!string.IsNullOrWhiteSpace(solution))
            {
                BeginInvoke(() =>
                {
                    Clipboard.SetText(solution);
                    AddRawLogEntry($"✅ Solved: \"{solution}\"  ← copied to clipboard");
                    if (trayIcon.Visible)
                        trayIcon.ShowBalloonTip(5000, "✅ CAPTCHA Solved!",
                            $"Answer: {solution}\n(Copied to clipboard)", ToolTipIcon.Info);
                });
            }
            else
            {
                BeginInvoke(() => AddRawLogEntry($"❌ Auto-solve failed: {error}"));
            }
        }

        private void AddRawLogEntry(string message)
        {
            string entry = $"[{DateTime.Now:HH:mm:ss}] {message}";
            listBoxLog.Items.Add(entry);
            listBoxLog.SelectedIndex = listBoxLog.Items.Count - 1;
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
            chk2Captcha.Checked = _settings.Enable2Captcha;
            txtOpenAiKey.Text   = _settings.OpenAiApiKey;

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
            _settings.Enable2Captcha    = chk2Captcha.Checked;
            _settings.OpenAiApiKey      = txtOpenAiKey.Text.Trim();

            SettingsManager.Save(_settings);

            if (_monitorTimer != null)
                _monitorTimer.Interval = _settings.MonitoringInterval;

            MessageBox.Show("Settings saved successfully.", "AlarmTool",
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
            trayIcon.Text = "AlarmTool";
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
            AppLogger.Log("[App] AlarmTool exited.");
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
                MessageBox.Show($"Sound file not found:\n{path}", "AlarmTool",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            _alarm.TestSound(path);
        }

        private void BtnSaveSettings_Click(object sender, EventArgs e) => SaveSettings();

        private void BtnCopyHwid_Click(object sender, EventArgs e)
        {
            var hwid = txtHwidStat.Text;
            if (string.IsNullOrWhiteSpace(hwid) || hwid == "...") return;
            Clipboard.SetText(hwid);
            btnCopyHwid.Text = "✔";
            var t = new System.Windows.Forms.Timer { Interval = 1500 };
            t.Tick += (_, _) => { btnCopyHwid.Text = "📋"; t.Stop(); t.Dispose(); };
            t.Start();
        }

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
            // Ensure splitter distance is valid on resize
            if (splitMain != null && ClientSize.Width > 0)
            {
                int minDistance = splitMain.Panel1MinSize;
                int maxDistance = ClientSize.Width - splitMain.Panel2MinSize;

                // Only adjust if current distance is out of valid range
                if (splitMain.SplitterDistance < minDistance || splitMain.SplitterDistance > maxDistance)
                {
                    splitMain.SplitterDistance = Math.Clamp(splitMain.SplitterDistance, minDistance, maxDistance);
                }
            }

            if (_settings.MinimizeToTray && WindowState == FormWindowState.Minimized)
            {
                Hide();
                trayIcon.Visible = true;
                trayIcon.ShowBalloonTip(2000, "AlarmTool",
                    "Application minimised to tray. Double-click to restore.", ToolTipIcon.Info);
            }
        }

        private void SplitMain_SplitterMoved(object sender, EventArgs e)
        {
            // This event handler is wired in the designer to track splitter movements
            // No action needed here, but it prevents any future issues
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
