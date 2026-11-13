@echo off
setlocal EnableExtensions DisableDelayedExpansion

where magick.exe >nul 2>&1
if errorlevel 1 (
    echo ImageMagick was not found. Add magick.exe to PATH and run this script again. 1>&2
    exit /b 1
)

set "output=%~dp064x64"
if not exist "%output%\" mkdir "%output%"
if not exist "%output%\" exit /b 1

set /a converted=0, failed=0
for %%F in ("%~dp0*.png" "%~dp0*.jpg" "%~dp0*.jpeg" "%~dp0*.webp" "%~dp0*.bmp") do (
    if exist "%%~fF" if not exist "%%~fF\" (
        magick.exe "%%~fF" -auto-orient -filter Lanczos -resize 64x64 -alpha on -background none -gravity center -extent 64x64 "%output%\%%~nxF"
        if errorlevel 1 (
            echo Failed: "%%~nxF" 1>&2
            set /a failed+=1
        ) else (
            set /a converted+=1
        )
    )
)

echo Converted %converted% image(s) to "%output%". Failed: %failed%.
if not "%failed%"=="0" exit /b 1
exit /b 0