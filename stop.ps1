# stop.ps1 — fires on SessionEnd. Fully quits the ClaudeTok overlay.

$ErrorActionPreference = "SilentlyContinue"
$logPath = Join-Path $env:TEMP "claudetok-hook.log"
"$(Get-Date -Format 'HH:mm:ss') stop.ps1 fired" | Out-File -FilePath $logPath -Append

# Ask politely via IPC first (graceful shutdown)
try {
    Invoke-WebRequest -Uri "http://127.0.0.1:49823/quit" -UseBasicParsing -TimeoutSec 1 | Out-Null
} catch {}

Start-Sleep -Milliseconds 400

# Force-kill any survivors
Get-Process -Name "ClaudeTok" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

exit 0
