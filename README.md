# CaptchaAlarm

A .NET 6 Windows Forms application that monitors your screen for the **Aion Daeva Verification** CAPTCHA popup and triggers an alarm (custom sound + Windows notification) when detected.

---

## Features

- **Multi-layered CAPTCHA Detection**
  - Tesseract OCR scans for key text strings
  - Colour-signature analysis (dark background, yellow timer, cyan text)
  - Visual layout pattern detection
  - Confidence-score system to minimise false positives
- **Alarm System**
  - Custom WAV/MP3 sound playback (NAudio)
  - Configurable loop count
  - Default alarm sound included
  - Fallback system beep if no file found
- **Windows 10/11 Toast Notifications**
  - Clickable notification brings app to foreground
  - Timestamp and confidence score shown
- **Professional UI**
  - Dark theme with status indicators (green / red / yellow flashing)
  - Real-time detection log with timestamps and confidence scores
  - Statistics panel (total detections, session start, last detection)
  - Settings panel with sliders and toggles
  - Start / Stop / Pause controls
- **System Tray Integration**
  - Minimize to tray
  - Context menu (Show/Hide, Start/Stop, Exit)
  - Balloon notification from tray
- **Persistent Settings** – stored in `appsettings.json`
- **Optional Screenshot Capture** on each detection
- **Cooldown Period** – 30-second cooldown prevents alarm spam

---

## How It Works

### Multi-Layered Detection

```
Screenshot taken every N seconds
        │
        ├─► OCR Analysis (Tesseract)
        │     • Scans for: "Daeva Verification", "Enter the text you see in the image",
        │       "chance(s) remaining", "Failing to complete verification", "debuffs"
        │     • +50 points per keyword matched
        │
        ├─► Colour Signature Analysis
        │     • Checks for dark-grey window background (#2d2d2d ±15)  → +20 pts
        │     • Checks for yellow/orange timer pixels                  → +15 pts
        │     • Checks for cyan/blue "chances remaining" pixels        → +15 pts
        │
        └─► Layout Pattern Analysis
              • Dark rectangle detected near screen centre             → +20 pts

Total score ≥ Threshold (default 70) → Alarm triggered!
```

### Why This Works Well

The Aion Daeva Verification CAPTCHA has very distinctive characteristics:
1. **Unique title text** – "Daeva Verification" is unlikely to appear elsewhere
2. **Distinctive dark background** at a specific location on screen
3. **Specific colour palette** – yellow countdown timer + cyan text
4. The combination of all three layers pushes the confidence score above 70 reliably

### Non-Invasive

- Only captures screen pixels via `Graphics.CopyFromScreen`
- No process hooking, memory injection, or game file access
- Works with any application on Windows

---

## Installation

### Prerequisites

| Requirement | Version |
|---|---|
| Windows | 10 or 11 (64-bit, build 17763+) |
| .NET Runtime | 8.0 or later |
| Tesseract OCR | Optional (for text detection) |

### Tesseract OCR Setup (Optional but Recommended)

Tesseract OCR greatly improves detection accuracy. Without it, the application falls back to colour + layout detection only.

1. Download and install [Tesseract for Windows](https://github.com/UB-Mannheim/tesseract/wiki)
2. Note the installation path (default: `C:\Program Files\Tesseract-OCR`)
3. The app searches for `tessdata` in the following locations automatically:
   - `<AppDirectory>\tessdata\`
   - `C:\Program Files\Tesseract-OCR\tessdata\`
   - `C:\tessdata\`

Alternatively, copy `eng.traineddata` into a `tessdata` folder next to the `.exe`.

### Build from Source

```bash
# Clone the repository
git clone https://github.com/jemaineosia/CaptchaAlarm.git
cd CaptchaAlarm

# Restore dependencies
dotnet restore CaptchaAlarm.sln

# Build (Release)
dotnet build CaptchaAlarm.sln -c Release

# Run
dotnet run --project CaptchaAlarm/CaptchaAlarm.csproj
```

### Run the Application

Double-click `CaptchaAlarm.exe` in the build output directory.

---

## Usage

### Starting Monitoring

1. Launch **CaptchaAlarm.exe**
2. (Optional) Adjust settings in the **Settings** panel on the right
3. Click **▶ Start Monitoring**
4. The status indicator turns green — monitoring is active!

### When a CAPTCHA is Detected

- Status indicator flashes **yellow**
- Alarm sound plays
- Windows toast notification appears
- Entry added to the **Detection Log** with timestamp and confidence score
- System tray balloon shown (if minimised)

### Configuring Settings

| Setting | Description | Default |
|---|---|---|
| Monitoring Interval | Seconds between screenshots | 2s |
| Confidence Threshold | Minimum score to trigger alarm | 70 |
| Enable Notifications | Show Windows toast notifications | ✓ |
| Enable Sound | Play alarm sound | ✓ |
| Loop Sound | Repeat alarm sound | ✓ |
| Save Screenshots | Save PNG on detection | ✗ |
| Minimize to Tray | Hide to system tray on minimize | ✓ |

Click **💾 Save Settings** to persist your changes.

### Customising the Alarm Sound

1. Click **...** next to the sound path
2. Select a WAV or MP3 file
3. Click **🔊 Test Sound** to preview
4. Click **💾 Save Settings**

### System Tray

- The app minimises to the system tray when you close or minimise the window
- Right-click the tray icon for options: **Show/Hide**, **Start/Stop**, **Exit**
- Double-click to restore

---

## Screenshots

> *Screenshots will be added after first UI build*

---

## System Requirements

- **OS**: Windows 10 / 11 (64-bit, build 17763 / 1809 or later)
- **Runtime**: [.NET 8.0](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- **Disk**: ~50 MB
- **CPU**: < 5% average during monitoring (configurable interval)
- **RAM**: ~50 MB

---

## Troubleshooting

| Problem | Solution |
|---|---|
| OCR not working | Install Tesseract and ensure `tessdata\eng.traineddata` is available |
| No sound | Check that the sound file path is valid; click "Test Sound" |
| High CPU usage | Increase monitoring interval in Settings |
| App not detecting CAPTCHA | Lower the Confidence Threshold (try 50); ensure Tesseract is installed |
| Notifications not appearing | Check Windows notification settings; run app as administrator |
| Window not restored from tray | Double-click the tray icon |

---

## Important Disclaimer

> **This application performs passive screen monitoring only.**
> - No game files are modified or accessed
> - No process hooking or memory injection
> - No network traffic inspection
> - Pure screen pixel monitoring via standard Windows APIs
> - Safe to use alongside Aion and other games
> - Use at your own discretion and in accordance with the game's Terms of Service

---

## Project Structure

```
CaptchaAlarm/
├── CaptchaAlarm.sln
├── CaptchaAlarm/
│   ├── CaptchaAlarm.csproj
│   ├── Program.cs
│   ├── AppLogger.cs
│   ├── SettingsManager.cs
│   ├── MainForm.cs
│   ├── MainForm.Designer.cs
│   ├── MainForm.resx
│   ├── Services/
│   │   ├── ScreenCaptureService.cs
│   │   ├── CaptchaDetectionService.cs
│   │   ├── AlarmService.cs
│   │   └── NotificationService.cs
│   ├── Models/
│   │   ├── DetectionResult.cs
│   │   ├── AppSettings.cs
│   │   └── DetectionLog.cs
│   ├── Resources/
│   │   ├── default-alarm.wav
│   │   └── icon.ico
│   └── appsettings.json
├── Screenshots/         ← created at runtime
├── .gitignore
├── README.md
└── LICENSE.md
```

---

## License

MIT License – see [LICENSE.md](LICENSE.md)