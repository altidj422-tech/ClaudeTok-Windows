# release.ps1 — build the exe, then bundle it with the install scripts into
# a zip ready to attach to a GitHub Release.

param([string]$Version = "v0.1.0")

$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

# Build
.\build.ps1

$staging = "release-staging\claudetok-win"
$zip = "claudetok-win-$Version.zip"

if (Test-Path "release-staging") { Remove-Item -Recurse -Force "release-staging" }
New-Item -ItemType Directory -Path $staging | Out-Null

Copy-Item ".\bin\publish\ClaudeTok.exe" -Destination $staging
Copy-Item ".\show.ps1", ".\hide.ps1", ".\prelaunch.ps1", ".\stop.ps1" -Destination $staging
Copy-Item ".\install.ps1", ".\README.md", ".\LICENSE" -Destination $staging

if (Test-Path $zip) { Remove-Item $zip }
Compress-Archive -Path "$staging\*" -DestinationPath $zip

Remove-Item -Recurse -Force "release-staging"

Write-Host ""
Write-Host "Built: $PSScriptRoot\$zip" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:"
Write-Host "  1. Go to https://github.com/altidj422-tech/ClaudeTok-Windows/releases/new"
Write-Host "  2. Tag: $Version   Title: $Version"
Write-Host "  3. Upload $zip"
Write-Host "  4. Publish release"
