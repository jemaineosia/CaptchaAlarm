# Creating AlarmTool Installer

## Quick Start

1. **Install Inno Setup** (one-time setup)
   - Download from: https://jrsoftware.org/isdl.php
   - Run the installer and use default options

2. **Build the Installer**
   ```powershell
   .\build-installer.ps1
   ```

3. **Find your installer** in the `Installer` folder:
   - `AlarmTool-Setup-v1.0.0.exe`

## Script Options

### Standard Build (Requires .NET 8 on target PC)
```powershell
.\build-installer.ps1
```
- Smaller installer (~5-10 MB)
- User must have .NET 8 Desktop Runtime installed
- Recommended for tech-savvy users

### Self-Contained Build (No .NET Required)
```powershell
.\build-installer.ps1 -SelfContained
```
- Larger installer (~50-60 MB)
- Includes .NET 8 runtime
- **Recommended for general distribution**
- Works on any Windows 10+ machine

### Skip Rebuild (Just rebuild installer)
```powershell
.\build-installer.ps1 -SkipBuild
```
- Use when you've already built the app
- Only recreates the installer package

## What Gets Installed

The installer will:
- ✅ Install AlarmTool.exe to `C:\Program Files\AlarmTool\`
- ✅ Include all dependencies (Tesseract OCR, NAudio, etc.)
- ✅ Include tessdata (English language data for OCR)
- ✅ Create Start Menu shortcuts
- ✅ Optionally create Desktop shortcut
- ✅ Create proper uninstaller in Windows Settings

## Customizing the Installer

Edit `AlarmTool-Installer.iss`:

### Change App Version
```ini
#define MyAppVersion "1.0.1"
```

### Change Publisher Name
```ini
#define MyAppPublisher "Your Name or Company"
```

### Change Default Install Location
```ini
DefaultDirName={autopf}\MyCustomFolder
```

### Require Administrator Rights
```ini
PrivilegesRequired=admin
```

## Distribution Checklist

Before sharing your installer:

- [ ] Test the installer on a clean Windows machine
- [ ] Verify AlarmTool runs after installation
- [ ] Test OCR functionality (check Detection Log)
- [ ] Verify uninstaller removes everything
- [ ] Update version number in `AlarmTool-Installer.iss`
- [ ] Update CHANGELOG or release notes

## File Structure

```
CaptchaAlarm/
├── AlarmTool-Installer.iss     ← Installer configuration
├── build-installer.ps1          ← Build script
├── LICENSE.txt                  ← License shown during install
├── tessdata/
│   └── eng.traineddata         ← Auto-downloaded OCR data
└── Installer/                   ← Output folder
    └── AlarmTool-Setup-v1.0.0.exe
```

## Troubleshooting

### "Inno Setup not found"
- Install Inno Setup from https://jrsoftware.org/isdl.php
- Restart PowerShell after installation

### "Build failed"
- Make sure you can build normally in Visual Studio first
- Check for compilation errors
- Try running: `dotnet build -c Release`

### "AlarmTool.exe not found"
- Ensure the build succeeded
- Check path: `CaptchaAlarm\bin\Release\net8.0-windows10.0.17763\`

### Installer too large
- Use framework-dependent build (default)
- Self-contained builds include entire .NET runtime (~50MB)

### OCR not working after install
- Verify `tessdata\eng.traineddata` exists in project
- Check the build script downloaded it successfully
- File should be ~4.8 MB

## Advanced: Code Signing (Optional)

To avoid Windows SmartScreen warnings:

1. Get a code signing certificate
2. Add to Inno Setup script:
   ```ini
   SignTool=signtool sign /f "MyCertificate.pfx" /p "password" $f
   ```

## Support

For issues or questions:
- GitHub: https://github.com/jemaineosia/CaptchaAlarm
- Check the Detection Log in the app for errors
