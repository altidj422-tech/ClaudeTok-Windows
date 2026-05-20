# prelaunch.ps1 — fires on SessionStart. Starts the ClaudeTok overlay hidden
# off-screen so the first show is instant.

$ErrorActionPreference = "SilentlyContinue"
$extDir = Join-Path $env:USERPROFILE ".claude\extensions\tiktok-overlay"
$exe = Join-Path $extDir "ClaudeTok.exe"
$logPath = Join-Path $env:TEMP "claudetok-hook.log"

"$(Get-Date -Format 'HH:mm:ss') prelaunch.ps1 fired" | Out-File -FilePath $logPath -Append

if (Get-Process -Name "ClaudeTok" -ErrorAction SilentlyContinue) {
    "$(Get-Date -Format 'HH:mm:ss')   already running, skip" | Out-File -FilePath $logPath -Append
    exit 0
}

if (-not (Test-Path $exe)) {
    "$(Get-Date -Format 'HH:mm:ss')   exe missing at $exe" | Out-File -FilePath $logPath -Append
    exit 0
}

Start-Process $exe -ArgumentList "--hidden" -WindowStyle Hidden
"$(Get-Date -Format 'HH:mm:ss')   launched hidden" | Out-File -FilePath $logPath -Append

exit 0
