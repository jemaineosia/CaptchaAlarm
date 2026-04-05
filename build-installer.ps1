# Build and Create Installer for AlarmTool
# Run this script to build the app and create the installer

param(
    [switch]$SkipBuild,
    [switch]$SelfContained
)

Write-Host "================================" -ForegroundColor Cyan
Write-Host "  AlarmTool Installer Builder  " -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Clean previous builds
if (-not $SkipBuild) {
    Write-Host "🧹 Cleaning previous builds..." -ForegroundColor Yellow
    Remove-Item -Path "CaptchaAlarm\bin\Release" -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -Path "CaptchaAlarm\obj\Release" -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "✅ Cleaned" -ForegroundColor Green
    Write-Host ""
}

# Step 2: Download tessdata if needed
Write-Host "📥 Checking Tesseract language data..." -ForegroundColor Yellow
if (-not (Test-Path "tessdata\eng.traineddata")) {
    Write-Host "⬇️ Downloading eng.traineddata..." -ForegroundColor Yellow
    New-Item -ItemType Directory -Force -Path "tessdata" | Out-Null
    $url = "https://github.com/tesseract-ocr/tessdata/raw/main/eng.traineddata"
    Invoke-WebRequest -Uri $url -OutFile "tessdata\eng.traineddata"
    Write-Host "✅ Downloaded" -ForegroundColor Green
} else {
    Write-Host "✅ tessdata already exists" -ForegroundColor Green
}
Write-Host ""

# Step 3: Build the application
if (-not $SkipBuild) {
    Write-Host "🔨 Building AlarmTool in Release mode..." -ForegroundColor Yellow
    
    if ($SelfContained) {
        Write-Host "   Building as self-contained (includes .NET runtime)..." -ForegroundColor Gray
        dotnet publish CaptchaAlarm\CaptchaAlarm.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false
        $buildPath = "CaptchaAlarm\bin\Release\net8.0-windows10.0.17763\win-x64\publish"
        
        # Copy to expected location for Inno Setup
        if (Test-Path $buildPath) {
            Copy-Item -Path "$buildPath\*" -Destination "CaptchaAlarm\bin\Release\net8.0-windows10.0.17763\" -Recurse -Force
        }
    } else {
        Write-Host "   Building as framework-dependent (requires .NET 8)..." -ForegroundColor Gray
        dotnet build CaptchaAlarm\CaptchaAlarm.csproj -c Release
    }
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Build failed!" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "✅ Build successful" -ForegroundColor Green
    Write-Host ""
}

# Step 4: Verify build output
$exePath = "CaptchaAlarm\bin\Release\net8.0-windows10.0.17763\AlarmTool.exe"
if (-not (Test-Path $exePath)) {
    Write-Host "❌ AlarmTool.exe not found at expected location!" -ForegroundColor Red
    Write-Host "   Expected: $exePath" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Build output verified" -ForegroundColor Green
Write-Host ""

# Step 5: Generate unique GUID for installer (first time only)
$issFile = "AlarmTool-Installer.iss"
if (Test-Path $issFile) {
    $content = Get-Content $issFile -Raw
    if ($content -match "YOUR-GUID-HERE") {
        $guid = [guid]::NewGuid().ToString().ToUpper()
        $content = $content -replace "YOUR-GUID-HERE", $guid
        Set-Content -Path $issFile -Value $content
        Write-Host "✅ Generated unique installer GUID: $guid" -ForegroundColor Green
        Write-Host ""
    }
}

# Step 6: Check for Inno Setup
Write-Host "🔍 Checking for Inno Setup..." -ForegroundColor Yellow
$innoSetupPaths = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles}\Inno Setup 6\ISCC.exe"
)

$iscc = $null
foreach ($path in $innoSetupPaths) {
    if (Test-Path $path) {
        $iscc = $path
        break
    }
}

if (-not $iscc) {
    Write-Host "❌ Inno Setup not found!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please install Inno Setup from: https://jrsoftware.org/isdl.php" -ForegroundColor Yellow
    Write-Host "After installation, run this script again." -ForegroundColor Yellow
    exit 1
}

Write-Host "✅ Found Inno Setup at: $iscc" -ForegroundColor Green
Write-Host ""

# Step 7: Build the installer
Write-Host "📦 Creating installer..." -ForegroundColor Yellow
& $iscc $issFile

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Installer creation failed!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "================================" -ForegroundColor Green
Write-Host "  ✅ INSTALLER CREATED!         " -ForegroundColor Green
Write-Host "================================" -ForegroundColor Green
Write-Host ""

# Step 8: Show output location
$installerPath = Get-ChildItem -Path "Installer" -Filter "AlarmTool-Setup-*.exe" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($installerPath) {
    Write-Host "📍 Installer location:" -ForegroundColor Cyan
    Write-Host "   $($installerPath.FullName)" -ForegroundColor White
    Write-Host ""
    Write-Host "📊 File size: $([math]::Round($installerPath.Length / 1MB, 2)) MB" -ForegroundColor Cyan
    Write-Host ""
    
    # Offer to open the folder
    $response = Read-Host "Open installer folder? (Y/N)"
    if ($response -eq "Y" -or $response -eq "y") {
        explorer.exe (Resolve-Path "Installer")
    }
} else {
    Write-Host "⚠️ Installer file not found in Installer folder" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Done! 🎉" -ForegroundColor Green
