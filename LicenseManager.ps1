# ============================================================
#  AlarmTool - License Manager
#  Usage:
#    .\LicenseManager.ps1 list
#    .\LicenseManager.ps1 add    -Hwid "285566E8D59B24F8" -Days 30
#    .\LicenseManager.ps1 extend -Hwid "285566E8D59B24F8" -Days 7
#    .\LicenseManager.ps1 remove -Hwid "285566E8D59B24F8"
# ============================================================
param(
    [Parameter(Position=0,Mandatory=$true)][ValidateSet("list","add","extend","remove")][string]$Action,
    [string]$Hwid = "", [int]$Days = 7
)
$ApiKey  = "$" + "2a$" + "10$" + "x788QPCES.yVltsT/8XGte8kcfKt7hFYJvNssAHCaxFYZLguEqAea"
$BinId   = "69dc8e49856a68218929fc75"
$BaseUrl = "https://api.jsonbin.io/v3/b/$BinId"
$GH = @{ "X-Master-Key"=$ApiKey; "X-Bin-Meta"="false" }
$PH = @{ "X-Master-Key"=$ApiKey; "Content-Type"="application/json" }
function BinGet { try { return Invoke-RestMethod -Uri $BaseUrl -Headers $GH -Method Get } catch { Write-Host "ERROR: $_" -ForegroundColor Red; exit 1 } }
function BinSave($d) { try { Invoke-RestMethod -Uri $BaseUrl -Headers $PH -Method Put -Body ($d | ConvertTo-Json -Depth 5) | Out-Null } catch { Write-Host "ERROR: $_" -ForegroundColor Red; exit 1 } }
function ShowLic($h,$e) {
    if (-not $e.activated -or $e.days -le 0) { return }
    try {
        $s=[datetime]::Parse($e.activated).ToUniversalTime(); $x=$s.AddDays([int]$e.days)
        $left=[int]($x-(Get-Date).ToUniversalTime()).TotalDays
        $c=if($left -gt 0){"Green"}else{"Red"}; $st=if($left -gt 0){"ACTIVE ($left d left)"}else{"EXPIRED"}
        Write-Host ("  {0,-18} | Act:{1:yyyy-MM-dd} | {2,3}d | Exp:{3:yyyy-MM-dd} | {4}" -f $h,$s,[int]$e.days,$x,$st) -ForegroundColor $c
    } catch { Write-Host "  $h | (invalid)" -ForegroundColor Gray }
}
if ($Action -eq "list") {
    Write-Host "`nAlarmTool License List`n$("-"*80)" -ForegroundColor Cyan
    $b=BinGet; $n=0
    foreach($p in ($b|Get-Member -MemberType NoteProperty)){ShowLic $p.Name $b.($p.Name);$n++}
    if($n -eq 0){Write-Host "  (no licenses)" -ForegroundColor Gray}; Write-Host ""
} elseif ($Action -eq "add") {
    if(-not $Hwid){Write-Host "ERROR: -Hwid required" -ForegroundColor Red;exit 1}
    $Hwid=$Hwid.ToUpperInvariant(); $b=BinGet
    if($null -ne $b.$Hwid){Write-Host "WARN: $Hwid already exists. Use 'extend' to reset." -ForegroundColor Yellow;exit 0}
    $b|Add-Member -NotePropertyName $Hwid -NotePropertyValue @{activated=(Get-Date).ToUniversalTime().ToString("o");days=$Days} -Force
    BinSave $b; Write-Host "OK: Added $Hwid | $Days days | Expires $((Get-Date).AddDays($Days).ToString('yyyy-MM-dd'))" -ForegroundColor Green
} elseif ($Action -eq "extend") {
    if(-not $Hwid){Write-Host "ERROR: -Hwid required" -ForegroundColor Red;exit 1}
    $Hwid=$Hwid.ToUpperInvariant(); $b=BinGet
    $b|Add-Member -NotePropertyName $Hwid -NotePropertyValue @{activated=(Get-Date).ToUniversalTime().ToString("o");days=$Days} -Force
    BinSave $b; Write-Host "OK: Extended $Hwid | $Days days | Expires $((Get-Date).AddDays($Days).ToString('yyyy-MM-dd'))" -ForegroundColor Green
} elseif ($Action -eq "remove") {
    if(-not $Hwid){Write-Host "ERROR: -Hwid required" -ForegroundColor Red;exit 1}
    $Hwid=$Hwid.ToUpperInvariant(); $b=BinGet
    if($null -eq $b.$Hwid){Write-Host "WARN: $Hwid not found." -ForegroundColor Yellow;exit 0}
    $b.PSObject.Properties.Remove($Hwid); BinSave $b
    Write-Host "OK: Removed $Hwid" -ForegroundColor Green
}
