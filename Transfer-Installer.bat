@echo off
REM === 결과물 복사 ===
set "SOLUTION_PATH=%cd%"
set "TARGET_DIR=\\ad.npixel.co.kr\1_Setup\34_ExcelMerge"
copy /Y "%SOLUTION_PATH%\ExcelMerge-Setup.exe" "%TARGET_DIR%"
endlocal
exit /b 0
