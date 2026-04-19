using System.Drawing;
using System.Windows.Forms;

namespace CaptchaAlarm
{
    /// <summary>
    /// Shown when the machine's HWID has not been activated by the developer.
    /// The user copies their HWID and sends it to get a license.
    /// </summary>
    public class ActivationRequiredForm : Form
    {
        public ActivationRequiredForm(string hwid)
        {
            InitializeComponent(hwid);
        }

        private void InitializeComponent(string hwid)
        {
            Text = "AlarmTool – Activation Required";
            Size = new Size(500, 360);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ControlBox = false;
            BackColor = Color.FromArgb(30, 30, 30);
            ForeColor = Color.WhiteSmoke;
            Font = new Font("Segoe UI", 9f);

            try
            {
                var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "icon.ico");
                if (File.Exists(iconPath)) Icon = new Icon(iconPath);
            }
            catch { /* ignore */ }

            var lblIcon = new Label
            {
                Text = "🔒",
                Font = new Font("Segoe UI", 36f),
                AutoSize = true,
                Location = new Point(28, 22),
                BackColor = Color.Transparent
            };

            var lblTitle = new Label
            {
                Text = "Activation Required",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 180, 255),
                AutoSize = true,
                Location = new Point(100, 32)
            };

            var lblMessage = new Label
            {
                Text = "This copy of AlarmTool has not been activated.\r\n" +
                       "Send your Hardware ID below to the developer to get a license.",
                Font = new Font("Segoe UI", 10f),
                ForeColor = Color.FromArgb(200, 200, 200),
                Location = new Point(30, 100),
                Size = new Size(440, 46),
                AutoSize = false
            };

            var lblHwidLabel = new Label
            {
                Text = "Your Hardware ID (HWID):",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(150, 200, 255),
                AutoSize = true,
                Location = new Point(30, 158)
            };

            var txtHwid = new TextBox
            {
                Text = hwid,
                ReadOnly = true,
                Location = new Point(30, 176),
                Size = new Size(330, 28),
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.FromArgb(80, 240, 160),
                Font = new Font("Consolas", 12f, FontStyle.Bold),
                BorderStyle = BorderStyle.FixedSingle,
                TabStop = false
            };
            txtHwid.Click += (_, _) => txtHwid.SelectAll();

            var btnCopy = new Button
            {
                Text = "📋 Copy",
                Location = new Point(368, 175),
                Size = new Size(92, 30),
                BackColor = Color.FromArgb(40, 80, 120),
                ForeColor = Color.WhiteSmoke,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCopy.FlatAppearance.BorderColor = Color.FromArgb(80, 120, 160);
            btnCopy.Click += (_, _) =>
            {
                Clipboard.SetText(hwid);
                btnCopy.Text = "✔ Copied!";
                var t = new System.Windows.Forms.Timer { Interval = 1500 };
                t.Tick += (_, _) => { btnCopy.Text = "📋 Copy"; t.Stop(); t.Dispose(); };
                t.Start();
            };

            var lblContact = new Label
            {
                Text = "📧  jemaine.osia@gmail.com",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(160, 200, 255),
                AutoSize = true,
                Location = new Point(30, 216)
            };

            var lblNote = new Label
            {
                Text = "The developer will activate your HWID and reply with confirmation.\r\nRestart the app after activation.",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(140, 140, 140),
                Location = new Point(30, 238),
                Size = new Size(440, 36),
                AutoSize = false
            };

            var btnExit = new Button
            {
                Text = "Exit Application",
                Location = new Point(170, 284),
                Size = new Size(155, 38),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.WhiteSmoke,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnExit.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100);
            btnExit.Click += (_, _) => Application.Exit();

            Controls.AddRange(new Control[]
            {
                lblIcon, lblTitle, lblMessage,
                lblHwidLabel, txtHwid, btnCopy,
                lblContact, lblNote,
                btnExit
            });
        }
    }
}
