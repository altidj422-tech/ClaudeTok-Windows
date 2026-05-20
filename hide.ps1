# hide.ps1 — fires on Stop. Tells the running ClaudeTok app to hide the overlay.

$ErrorActionPreference = "SilentlyContinue"
$logPath = Join-Path $env:TEMP "claudetok-hook.log"
"$(Get-Date -Format 'HH:mm:ss') hide.ps1 fired" | Out-File -FilePath $logPath -Append

try {
    Invoke-WebRequest -Uri "http://127.0.0.1:49823/hide" -UseBasicParsing -TimeoutSec 2 | Out-Null
    "$(Get-Date -Format 'HH:mm:ss')   signaled hide" | Out-File -FilePath $logPath -Append
} catch {
    # App not running — fine, hide is a no-op
}

exit 0
