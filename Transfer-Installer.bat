@echo off
setlocal enabledelayedexpansion

REM === 버전 정보 읽기 ===
set /p VERSION=<"%WORKSPACE%\version.txt"
echo [INFO] 버전: %VERSION%

set "PUBLISH_PATH=%WORKSPACE%\Build\Release"
set "FILE_NAME=ExcelMerge-Setup-%VERSION%.exe"
set "TARGET_DIR=\\ad.npixel.co.kr\share\1_Setup\34_ExcelMerge"

REM === 설치 파일 전송 ===
echo [INFO] 설치 파일 전송 중...

robocopy "%PUBLISH_PATH%" "%TARGET_DIR%" "%FILE_NAME%" /R:2
echo robocopy 종료 코드: %ERRORLEVEL%
IF %errorlevel% NEQ 1 (
   exit %errorlevel%
 )

exit 0