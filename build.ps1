# build.ps1 — compile ClaudeTok.exe as a single-file self-contained Windows binary.
# Requires .NET 8 SDK installed (https://dotnet.microsoft.com/download).

$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

Write-Host "Restoring NuGet packages..."
dotnet restore

Write-Host "Publishing single-file self-contained Windows binary..."
dotnet publish -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o "bin/publish"

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Built: $PSScriptRoot\bin\publish\ClaudeTok.exe" -ForegroundColor Green
