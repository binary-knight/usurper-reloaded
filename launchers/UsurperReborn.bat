@echo off
title Usurper Reborn

:: Set larger console window (120 columns x 50 rows)
mode con: cols=120 lines=50

cd /d "%~dp0"
UsurperRemake.exe
pause
