namespace CaptchaAlarm
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        // ── Controls ──────────────────────────────────────────────────────────
        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.Label lblAppTitle;
        private System.Windows.Forms.Panel panelStatus;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Button btnPause;
        private System.Windows.Forms.SplitContainer splitMain;
        private System.Windows.Forms.GroupBox grpLog;
        private System.Windows.Forms.ListBox listBoxLog;
        private System.Windows.Forms.Button btnClearLog;
        private System.Windows.Forms.GroupBox grpStats;
        private System.Windows.Forms.Label lblTotalDetections;
        private System.Windows.Forms.Label lblSessionStart;
        private System.Windows.Forms.Label lblLastDetection;
        private System.Windows.Forms.GroupBox grpSettings;
        private System.Windows.Forms.Label lblIntervalLabel;
        private System.Windows.Forms.TrackBar trackInterval;
        private System.Windows.Forms.Label lblIntervalValue;
        private System.Windows.Forms.Label lblThresholdLabel;
        private System.Windows.Forms.TrackBar trackThreshold;
        private System.Windows.Forms.Label lblThresholdValue;
        private System.Windows.Forms.CheckBox chkNotifications;
        private System.Windows.Forms.CheckBox chkSound;
        private System.Windows.Forms.CheckBox chkLoopSound;
        private System.Windows.Forms.CheckBox chkSaveScreenshots;
        private System.Windows.Forms.CheckBox chkMinimizeToTray;
        private System.Windows.Forms.Label lblSoundFileLabel;
        private System.Windows.Forms.TextBox txtSoundPath;
        private System.Windows.Forms.Button btnBrowseSound;
        private System.Windows.Forms.Button btnTestSound;
        private System.Windows.Forms.Button btnSaveSettings;
        private System.Windows.Forms.NotifyIcon trayIcon;
        private System.Windows.Forms.TableLayoutPanel tblControls;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();

            // ── Tray icon ────────────────────────────────────────────────────
            trayIcon = new System.Windows.Forms.NotifyIcon(components)
            {
                Text = "AlarmTool",
                Visible = false
            };

            // Try to load icon; fall back to no icon if resource missing
            try
            {
                var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "icon.ico");
                if (File.Exists(iconPath))
                    trayIcon.Icon = new System.Drawing.Icon(iconPath);
            }
            catch { /* ignore */ }

            // ── Form ────────────────────────────────────────────────────────
            SuspendLayout();
            Text = "AlarmTool – Aion Daeva Verification Monitor";
            MinimumSize = new System.Drawing.Size(700, 560);
            Size = new System.Drawing.Size(800, 650);
            BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
            ForeColor = System.Drawing.Color.WhiteSmoke;
            Font = new System.Drawing.Font("Segoe UI", 9f);
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Resize += MainForm_Resize;
            FormClosing += MainForm_FormClosing;

            try
            {
                var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "icon.ico");
                if (File.Exists(iconPath))
                    Icon = new System.Drawing.Icon(iconPath);
            }
            catch { /* ignore */ }

            // ── Top panel (title + status) ───────────────────────────────────
            panelTop = new System.Windows.Forms.Panel
            {
                Dock = System.Windows.Forms.DockStyle.Top,
                Height = 70,
                BackColor = System.Drawing.Color.FromArgb(20, 20, 20),
                Padding = new System.Windows.Forms.Padding(10, 5, 10, 5)
            };

            lblAppTitle = new System.Windows.Forms.Label
            {
                Text = "🎮 CaptchaAlarm",
                Font = new System.Drawing.Font("Segoe UI", 18f, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.FromArgb(100, 180, 255),
                AutoSize = true,
                Location = new System.Drawing.Point(10, 15)
            };

            panelStatus = new System.Windows.Forms.Panel
            {
                Anchor = System.Windows.Forms.AnchorStyles.Right | System.Windows.Forms.AnchorStyles.Top,
                Size = new System.Drawing.Size(260, 50),
                BackColor = System.Drawing.Color.FromArgb(45, 45, 45),
                Location = new System.Drawing.Point(480, 10)
            };

            lblStatus = new System.Windows.Forms.Label
            {
                Text = "● Stopped",
                Font = new System.Drawing.Font("Segoe UI", 11f, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.FromArgb(220, 80, 80),
                Dock = System.Windows.Forms.DockStyle.Fill,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter
            };

            panelStatus.Controls.Add(lblStatus);
            panelTop.Controls.Add(lblAppTitle);
            panelTop.Controls.Add(panelStatus);

            // ── Button bar ───────────────────────────────────────────────────
            tblControls = new System.Windows.Forms.TableLayoutPanel
            {
                Dock = System.Windows.Forms.DockStyle.Top,
                Height = 50,
                BackColor = System.Drawing.Color.FromArgb(35, 35, 35),
                ColumnCount = 2,
                RowCount = 1,
                Padding = new System.Windows.Forms.Padding(8, 6, 8, 6)
            };
            tblControls.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 70f));
            tblControls.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30f));

            btnStart = MakeButton("▶ Start Monitoring", System.Drawing.Color.FromArgb(40, 130, 60));
            btnStart.Click += BtnStart_Click;
            btnStart.Dock = System.Windows.Forms.DockStyle.Fill;
            btnStart.Font = new System.Drawing.Font("Segoe UI", 11f, System.Drawing.FontStyle.Bold);
            btnStart.Height = 38;

            btnPause = MakeButton("⏸ Pause", System.Drawing.Color.FromArgb(140, 110, 20));
            btnPause.Click += BtnPause_Click;
            btnPause.Dock = System.Windows.Forms.DockStyle.Fill;
            btnPause.Enabled = false;

            tblControls.Controls.Add(btnStart, 0, 0);
            tblControls.Controls.Add(btnPause, 1, 0);

            // ── Main split ───────────────────────────────────────────────────
            splitMain = new System.Windows.Forms.SplitContainer
            {
                Dock = System.Windows.Forms.DockStyle.Fill,
                Orientation = System.Windows.Forms.Orientation.Vertical,
                BackColor = System.Drawing.Color.FromArgb(30, 30, 30)
            };
            ((System.ComponentModel.ISupportInitialize)splitMain).BeginInit();
            splitMain.SuspendLayout();
            splitMain.SplitterMoved += SplitMain_SplitterMoved;

            // ── Left panel: Log + Stats ──────────────────────────────────────
            grpLog = MakeGroupBox("Detection Log");
            grpLog.Dock = System.Windows.Forms.DockStyle.Fill;

            listBoxLog = new System.Windows.Forms.ListBox
            {
                Dock = System.Windows.Forms.DockStyle.Fill,
                BackColor = System.Drawing.Color.FromArgb(25, 25, 25),
                ForeColor = System.Drawing.Color.FromArgb(180, 240, 180),
                Font = new System.Drawing.Font("Consolas", 8.5f),
                BorderStyle = System.Windows.Forms.BorderStyle.None,
                IntegralHeight = false
            };

            btnClearLog = MakeButton("🗑 Clear Log", System.Drawing.Color.FromArgb(80, 30, 30));
            btnClearLog.Click += BtnClearLog_Click;
            btnClearLog.Dock = System.Windows.Forms.DockStyle.Bottom;
            btnClearLog.Height = 28;

            grpLog.Controls.Add(listBoxLog);
            grpLog.Controls.Add(btnClearLog);

            grpStats = MakeGroupBox("Statistics");
            grpStats.Dock = System.Windows.Forms.DockStyle.Bottom;
            grpStats.Height = 90;

            lblTotalDetections = MakeStatLabel("Total Detections: 0");
            lblSessionStart = MakeStatLabel("Session Start: —");
            lblLastDetection = MakeStatLabel("Last Detection: —");

            lblTotalDetections.Location = new System.Drawing.Point(10, 18);
            lblSessionStart.Location = new System.Drawing.Point(10, 38);
            lblLastDetection.Location = new System.Drawing.Point(10, 58);

            grpStats.Controls.Add(lblTotalDetections);
            grpStats.Controls.Add(lblSessionStart);
            grpStats.Controls.Add(lblLastDetection);

            splitMain.Panel1.Controls.Add(grpLog);
            splitMain.Panel1.Controls.Add(grpStats);

            // ── Right panel: Settings ────────────────────────────────────────
            grpSettings = MakeGroupBox("Settings");
            grpSettings.Dock = System.Windows.Forms.DockStyle.Fill;

            int y = 22;

            // Interval slider
            lblIntervalLabel = MakeLabel("Monitoring Interval:", 10, y);
            lblIntervalValue = MakeLabel("2s", 210, y);
            lblIntervalValue.ForeColor = System.Drawing.Color.FromArgb(100, 200, 255);
            y += 25;

            trackInterval = new System.Windows.Forms.TrackBar
            {
                Minimum = 1, Maximum = 10, Value = 2,
                TickFrequency = 1, SmallChange = 1, LargeChange = 1,
                Location = new System.Drawing.Point(10, y),
                Size = new System.Drawing.Size(230, 36),
                BackColor = System.Drawing.Color.FromArgb(40, 40, 40)
            };
            trackInterval.Scroll += TrackInterval_Scroll;
            y += 45;

            // Threshold slider
            lblThresholdLabel = MakeLabel("Confidence Threshold:", 10, y);
            lblThresholdValue = MakeLabel("70", 210, y);
            lblThresholdValue.ForeColor = System.Drawing.Color.FromArgb(100, 200, 255);
            y += 25;

            trackThreshold = new System.Windows.Forms.TrackBar
            {
                Minimum = 50, Maximum = 100, Value = 70,
                TickFrequency = 5, SmallChange = 5, LargeChange = 10,
                Location = new System.Drawing.Point(10, y),
                Size = new System.Drawing.Size(230, 36),
                BackColor = System.Drawing.Color.FromArgb(40, 40, 40)
            };
            trackThreshold.Scroll += TrackThreshold_Scroll;
            y += 45;

            // Checkboxes
            chkNotifications = MakeCheckBox("Enable Notifications", 10, y); y += 26;
            chkSound = MakeCheckBox("Enable Sound", 10, y); y += 26;
            chkLoopSound = MakeCheckBox("Loop Sound", 10, y); y += 26;
            chkSaveScreenshots = MakeCheckBox("Save Screenshots on Detection", 10, y); y += 26;
            chkMinimizeToTray = MakeCheckBox("Minimize to System Tray", 10, y); y += 32;

            // Sound file
            lblSoundFileLabel = MakeLabel("Alarm Sound:", 10, y); y += 20;
            txtSoundPath = new System.Windows.Forms.TextBox
            {
                Location = new System.Drawing.Point(10, y),
                Size = new System.Drawing.Size(195, 22),
                BackColor = System.Drawing.Color.FromArgb(50, 50, 50),
                ForeColor = System.Drawing.Color.WhiteSmoke,
                BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
            };
            btnBrowseSound = MakeSmallButton("...", new System.Drawing.Point(208, y));
            btnBrowseSound.Click += BtnBrowseSound_Click;
            y += 28;
            btnTestSound = MakeButton("🔊 Test Sound", System.Drawing.Color.FromArgb(30, 80, 120));
            btnTestSound.Location = new System.Drawing.Point(10, y);
            btnTestSound.Size = new System.Drawing.Size(120, 26);
            btnTestSound.Click += BtnTestSound_Click;
            y += 36;

            // Save settings
            btnSaveSettings = MakeButton("💾 Save Settings", System.Drawing.Color.FromArgb(40, 80, 40));
            btnSaveSettings.Dock = System.Windows.Forms.DockStyle.Bottom;
            btnSaveSettings.Height = 32;
            btnSaveSettings.Click += BtnSaveSettings_Click;

            grpSettings.Controls.AddRange(new System.Windows.Forms.Control[]
            {
                lblIntervalLabel, lblIntervalValue, trackInterval,
                lblThresholdLabel, lblThresholdValue, trackThreshold,
                chkNotifications, chkSound, chkLoopSound,
                chkSaveScreenshots, chkMinimizeToTray,
                lblSoundFileLabel, txtSoundPath, btnBrowseSound, btnTestSound,
                btnSaveSettings
            });

            splitMain.Panel2.Controls.Add(grpSettings);

            splitMain.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitMain).EndInit();

            // ── Add to form ──────────────────────────────────────────────────
            Controls.Add(splitMain);
            Controls.Add(tblControls);
            Controls.Add(panelTop);

            ResumeLayout(false);
            PerformLayout();
        }

        // ── Helper factory methods ────────────────────────────────────────────

        private static System.Windows.Forms.Button MakeButton(string text, System.Drawing.Color back)
        {
            return new System.Windows.Forms.Button
            {
                Text = text,
                BackColor = back,
                ForeColor = System.Drawing.Color.WhiteSmoke,
                FlatStyle = System.Windows.Forms.FlatStyle.Flat,
                Font = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold),
                Cursor = System.Windows.Forms.Cursors.Hand,
                FlatAppearance = { BorderColor = System.Drawing.Color.FromArgb(80, 80, 80) }
            };
        }

        private static System.Windows.Forms.Button MakeSmallButton(string text, System.Drawing.Point loc)
        {
            return new System.Windows.Forms.Button
            {
                Text = text,
                Location = loc,
                Size = new System.Drawing.Size(32, 22),
                BackColor = System.Drawing.Color.FromArgb(60, 60, 60),
                ForeColor = System.Drawing.Color.WhiteSmoke,
                FlatStyle = System.Windows.Forms.FlatStyle.Flat,
                Font = new System.Drawing.Font("Segoe UI", 8f),
                Cursor = System.Windows.Forms.Cursors.Hand
            };
        }

        private static System.Windows.Forms.GroupBox MakeGroupBox(string text)
        {
            return new System.Windows.Forms.GroupBox
            {
                Text = text,
                ForeColor = System.Drawing.Color.FromArgb(150, 200, 255),
                BackColor = System.Drawing.Color.FromArgb(35, 35, 35),
                Font = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold),
                Padding = new System.Windows.Forms.Padding(4)
            };
        }

        private static System.Windows.Forms.Label MakeLabel(string text, int x, int y)
        {
            return new System.Windows.Forms.Label
            {
                Text = text,
                Location = new System.Drawing.Point(x, y),
                AutoSize = true,
                ForeColor = System.Drawing.Color.WhiteSmoke
            };
        }

        private static System.Windows.Forms.Label MakeStatLabel(string text)
        {
            return new System.Windows.Forms.Label
            {
                Text = text,
                AutoSize = true,
                ForeColor = System.Drawing.Color.FromArgb(180, 220, 180)
            };
        }

        private static System.Windows.Forms.CheckBox MakeCheckBox(string text, int x, int y)
        {
            return new System.Windows.Forms.CheckBox
            {
                Text = text,
                Location = new System.Drawing.Point(x, y),
                AutoSize = true,
                ForeColor = System.Drawing.Color.WhiteSmoke,
                BackColor = System.Drawing.Color.Transparent
            };
        }
    }
}
