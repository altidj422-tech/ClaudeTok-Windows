@echo off
REM Double-clickable wrapper for install.ps1. Unblocks downloaded files and
REM runs the installer with execution policy bypassed for this one invocation.
pushd "%~dp0"
powershell -NoProfile -Command "Get-ChildItem -Recurse | Unblock-File"
powershell -NoProfile -ExecutionPolicy Bypass -File ".\install.ps1"
popd
pause
