@echo off
setlocal
chcp 65001 >nul
set "PYTHONUTF8=1"
cd /d "%~dp0"
python -c "import tkinter; import PIL" >nul 2>nul
if errorlevel 1 (
    echo Python 3.10+ with Tkinter and Pillow is required.
    echo Install Pillow using: python -m pip install -r requirements.txt
    pause
    exit /b 1
)
python -u -B card_art.py
if errorlevel 1 pause
