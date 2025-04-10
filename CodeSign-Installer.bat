@echo off

REM === Read version from file ===
set /p VERSION=<"%WORKSPACE%\version.txt"
echo [INFO] Version: %VERSION%

REM === Define paths ===
set "PUBLISH_PATH=%WORKSPACE%\Build\Release"
set "FILE_NAME=ExcelMerge-Setup-%VERSION%.exe"

REM === Run code signing tool ===
"D:\CodeSignClient\CodeSignClient.exe" "%PUBLISH_PATH%\%FILE_NAME%"
if %errorlevel% neq 0 exit %errorlevel%
