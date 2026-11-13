@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\open-game.ps1" -View animation
if errorlevel 1 pause
