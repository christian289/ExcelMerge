set /p VERSION=<"%WORKSPACE%\version.txt"
echo [INFO] 버전: %VERSION%

set "PUBLISH_PATH=%WORKSPACE%\Build\Release"
set "FILE_NAME=ExcelMerge-Setup-%VERSION%.exe"

"D:\CodeSignClient\CodeSignClient.exe" "%PUBLISH_PATH%\%FILE_NAME%"
if %errorlevel% neq 0 exit %errorlevel%