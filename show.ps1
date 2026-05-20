# show.ps1 — fires on UserPromptSubmit. Pings the running ClaudeTok app to
# show the overlay. Launches the app if it isn't running.

$ErrorActionPreference = "SilentlyContinue"
$extDir = Join-Path $env:USERPROFILE ".claude\extensions\tiktok-overlay"
$exe = Join-Path $extDir "ClaudeTok.exe"
$logPath = Join-Path $env:TEMP "claudetok-hook.log"

"$(Get-Date -Format 'HH:mm:ss') show.ps1 fired" | Out-File -FilePath $logPath -Append

$running = Get-Process -Name "ClaudeTok" -ErrorAction SilentlyContinue
if (-not $running) {
    if (Test-Path $exe) {
        Start-Process $exe -WindowStyle Hidden
        Start-Sleep -Milliseconds 800
        "$(Get-Date -Format 'HH:mm:ss')   launched fresh" | Out-File -FilePath $logPath -Append
    } else {
        "$(Get-Date -Format 'HH:mm:ss')   exe missing at $exe" | Out-File -FilePath $logPath -Append
        exit 0
    }
}

try {
    Invoke-WebRequest -Uri "http://127.0.0.1:49823/show" -UseBasicParsing -TimeoutSec 2 | Out-Null
    "$(Get-Date -Format 'HH:mm:ss')   signaled show" | Out-File -FilePath $logPath -Append
} catch {
    "$(Get-Date -Format 'HH:mm:ss')   ipc failed: $($_.Exception.Message)" | Out-File -FilePath $logPath -Append
}

exit 0
