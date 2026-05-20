# install.ps1 — copy ClaudeTok.exe + hook scripts to ~/.claude/extensions/
# and patch ~/.claude/settings.json with the four hooks (SessionStart,
# UserPromptSubmit, Stop, SessionEnd). Idempotent.

$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

$extDir = Join-Path $env:USERPROFILE ".claude\extensions\tiktok-overlay"
$settings = Join-Path $env:USERPROFILE ".claude\settings.json"

# Find the exe — either in bin/publish (after build.ps1) or in the same folder
# (after extracting a release zip).
$exeSrc = $null
if (Test-Path ".\bin\publish\ClaudeTok.exe") {
    $exeSrc = ".\bin\publish\ClaudeTok.exe"
} elseif (Test-Path ".\ClaudeTok.exe") {
    $exeSrc = ".\ClaudeTok.exe"
} else {
    Write-Host "ERROR: ClaudeTok.exe not found." -ForegroundColor Red
    Write-Host "Run .\build.ps1 first, or extract a release zip first."
    exit 1
}

# Kill any running instance so we can overwrite the exe
Get-Process -Name "ClaudeTok" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 300

# Copy files
New-Item -ItemType Directory -Force -Path $extDir | Out-Null
Copy-Item $exeSrc -Destination (Join-Path $extDir "ClaudeTok.exe") -Force
Copy-Item ".\show.ps1", ".\hide.ps1", ".\prelaunch.ps1", ".\stop.ps1" -Destination $extDir -Force
Write-Host "Copied files to $extDir"

# Patch settings.json
if (-not (Test-Path $settings)) {
    "{}" | Out-File -FilePath $settings -Encoding UTF8
}

$cfg = Get-Content $settings -Raw | ConvertFrom-Json -AsHashtable
if (-not $cfg.ContainsKey("hooks")) { $cfg.hooks = @{} }

function Hook($script) {
    return @(@{
        hooks = @(@{
            type = "command"
            # Use PowerShell to execute the .ps1; quote the path for safety
            command = "powershell.exe -NoProfile -ExecutionPolicy Bypass -File `"`$env:USERPROFILE\.claude\extensions\tiktok-overlay\$script`""
        })
    })
}

$cfg.hooks["SessionStart"] = (Hook "prelaunch.ps1")
$cfg.hooks["UserPromptSubmit"] = (Hook "show.ps1")
$cfg.hooks["Stop"] = (Hook "hide.ps1")
$cfg.hooks["SessionEnd"] = (Hook "stop.ps1")

$cfg | ConvertTo-Json -Depth 10 | Out-File -FilePath $settings -Encoding UTF8
Write-Host "Patched $settings"

Write-Host ""
Write-Host "Install complete. Restart Claude Code so the new hooks take effect." -ForegroundColor Green
