@echo off
setlocal enabledelayedexpansion

REM === Read version information ===
set /p VERSION=<"%WORKSPACE%\version.txt"
echo [INFO] Version: %VERSION%

set "PUBLISH_PATH=%WORKSPACE%\Build\Release"
set "FILE_NAME=ExcelMerge-Setup-%VERSION%.exe"
set "TARGET_DIR=\\ad.npixel.co.kr\share\1_Setup\34_ExcelMerge"

REM === Transfer installer file ===
echo [INFO] Transferring installer file...

robocopy "%PUBLISH_PATH%" "%TARGET_DIR%" "%FILE_NAME%" /R:2
echo robocopy exit code: %ERRORLEVEL%
IF %errorlevel% NEQ 1 (
   exit %errorlevel%
)

exit 0
