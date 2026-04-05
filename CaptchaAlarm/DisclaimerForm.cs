using System.Drawing;
using System.Windows.Forms;

namespace CaptchaAlarm
{
    /// <summary>
    /// Disclaimer and terms of use dialog shown on first run.
    /// </summary>
    public class DisclaimerForm : Form
    {
        private Button btnAccept;
        private Button btnDecline;
        private RichTextBox txtDisclaimer;
        private CheckBox chkUnderstand;

        public DisclaimerForm()
        {
            InitializeComponent();
            LoadDisclaimerText();
        }

        private void InitializeComponent()
        {
            // Form setup
            Text = "AlarmTool - Disclaimer and Terms of Use";
            Size = new Size(700, 550);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(30, 30, 30);
            ForeColor = Color.WhiteSmoke;
            Font = new Font("Segoe UI", 9f);

            // Icon
            try
            {
                var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "icon.ico");
                if (File.Exists(iconPath))
                    Icon = new Icon(iconPath);
            }
            catch { /* ignore */ }

            // Title label
            var lblTitle = new Label
            {
                Text = "⚠️ Important Disclaimer",
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 180, 80),
                AutoSize = false,
                Size = new Size(660, 40),
                Location = new Point(20, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };
            Controls.Add(lblTitle);

            // Disclaimer text box
            txtDisclaimer = new RichTextBox
            {
                Location = new Point(20, 70),
                Size = new Size(660, 320),
                ReadOnly = true,
                BackColor = Color.FromArgb(25, 25, 25),
                ForeColor = Color.WhiteSmoke,
                Font = new Font("Segoe UI", 9.5f),
                BorderStyle = BorderStyle.FixedSingle,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };
            Controls.Add(txtDisclaimer);

            // Checkbox
            chkUnderstand = new CheckBox
            {
                Text = "I have read and understand the disclaimer above. I accept all risks and agree to use this tool at my own responsibility.",
                Location = new Point(20, 405),
                Size = new Size(660, 40),
                ForeColor = Color.WhiteSmoke,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Checked = false
            };
            chkUnderstand.CheckedChanged += ChkUnderstand_CheckedChanged;
            Controls.Add(chkUnderstand);

            // Buttons
            btnAccept = new Button
            {
                Text = "I Agree - Continue",
                Size = new Size(200, 40),
                Location = new Point(480, 460),
                BackColor = Color.FromArgb(40, 130, 60),
                ForeColor = Color.WhiteSmoke,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Enabled = false,
                Cursor = Cursors.Hand
            };
            btnAccept.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);
            btnAccept.Click += BtnAccept_Click;
            Controls.Add(btnAccept);

            btnDecline = new Button
            {
                Text = "Decline - Exit",
                Size = new Size(150, 40),
                Location = new Point(310, 460),
                BackColor = Color.FromArgb(120, 40, 40),
                ForeColor = Color.WhiteSmoke,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnDecline.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);
            btnDecline.Click += BtnDecline_Click;
            Controls.Add(btnDecline);
        }

        private void LoadDisclaimerText()
        {
            txtDisclaimer.Text = @"DISCLAIMER AND TERMS OF USE

READ CAREFULLY BEFORE USING THIS SOFTWARE

1. NO WARRANTY
This software (""AlarmTool"") is provided ""AS IS"" without warranty of any kind, either expressed or implied, including but not limited to the implied warranties of merchantability and fitness for a particular purpose.

2. USE AT YOUR OWN RISK
By using AlarmTool, you acknowledge and accept that:
• You use this software entirely at your own risk
• The software monitors your screen and may trigger alarms based on visual detection
• This tool is designed to detect the Aion Daeva Verification CAPTCHA window

3. GAME USAGE & BAN RISK
WARNING: Using any third-party tools while playing online games may violate the game's Terms of Service and could result in:
• Account suspension
• Permanent account ban
• Loss of in-game progress and purchases
• Other penalties imposed by the game publisher

4. NO LIABILITY
The author(s) and contributor(s) of this software shall NOT be held liable for:
• Account bans or suspensions
• Loss of game accounts, characters, or virtual items
• Any direct or indirect damages arising from the use of this software
• Any violations of game terms of service
• Any consequences resulting from the use of this tool

5. YOUR RESPONSIBILITY
By using this software, you agree that:
• You are solely responsible for any consequences of using this tool
• You understand the risks associated with using third-party software with online games
• You will not hold the developers liable for any negative outcomes
• You have read and understood your game's Terms of Service
• You accept full responsibility for any account actions taken against you

6. DETECTION & MONITORING
This software works by:
• Taking periodic screenshots of your screen
• Analyzing images using OCR and color detection
• Triggering alarms when specific patterns are detected
• This is a monitoring tool and does not modify game files or inject code

7. LEGITIMATE USE
This tool is intended for:
• Alerting players when a CAPTCHA window appears
• Preventing AFK penalties in games that use anti-bot verification
• Personal use and convenience

8. NO GUARANTEE
• The developers make no guarantees about detection accuracy
• The software may produce false positives or miss detections
• The software may not work with future game updates

By clicking ""I Agree"" below, you acknowledge that you have read, understood, and agreed to all terms in this disclaimer. You accept all risks and agree that you will not hold the developers liable for any consequences.

If you do not agree with these terms, click ""Decline"" to exit the application.";

            // Make certain sections bold
            int warningIndex = txtDisclaimer.Text.IndexOf("WARNING:");
            if (warningIndex >= 0)
            {
                txtDisclaimer.Select(warningIndex, 8);
                txtDisclaimer.SelectionFont = new Font(txtDisclaimer.Font, FontStyle.Bold);
                txtDisclaimer.SelectionColor = Color.FromArgb(255, 100, 100);
            }

            // Reset selection
            txtDisclaimer.Select(0, 0);
        }

        private void ChkUnderstand_CheckedChanged(object? sender, EventArgs e)
        {
            btnAccept.Enabled = chkUnderstand.Checked;
            btnAccept.BackColor = chkUnderstand.Checked 
                ? Color.FromArgb(40, 130, 60) 
                : Color.FromArgb(60, 60, 60);
        }

        private void BtnAccept_Click(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void BtnDecline_Click(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
