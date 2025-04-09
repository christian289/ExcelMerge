@echo off

set "SOURCE_DIR=%cd%\Build\Release"
set "FILE_NAME=ExcelMerge-Setup.exe"
set "TARGET_DIR=\\ad.npixel.co.kr\share\1_Setup\34_ExcelMerge"

robocopy "%SOURCE_DIR%" "%TARGET_DIR%" "%FILE_NAME%" /R:2

IF %ERRORLEVEL% GEQ 8 (
    echo Copy Fail! (Code: %ERRORLEVEL%)
    exit /b 1
) else (
    echo Copy Success! (Code: %ERRORLEVEL%)
    exit /b 0
)