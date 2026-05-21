@echo off
REM Double-clickable wrapper for build.ps1.
pushd "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File ".\build.ps1"
popd
pause
