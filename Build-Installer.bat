@echo off
setlocal enabledelayedexpansion

REM === 경로 설정 ===
set "SOLUTION_PATH=%cd%"
set "PROJECT_PATH=%SOLUTION_PATH%\ExcelMerge.GUI"
set "PUBLISH_PATH=%PROJECT_PATH%\Build\Release"
set "NSIS_SCRIPT_PATH=%SOLUTION_PATH%\PackageNSIS.nsi"
set "NSIS_COMPILER_PATH=D:\NSIS\makensis.exe"

REM === 경로 확인 ===
if not exist "%PROJECT_PATH%" (
    echo [ERROR] 프로젝트 경로가 존재하지 않습니다: %PROJECT_PATH%
    exit /b 1
)

REM === 발행 폴더 초기화 ===
if exist "%PUBLISH_PATH%" (
    echo [INFO] 기존 발행 폴더 제거 중...
    rmdir /s /q "%PUBLISH_PATH%"
)

REM === NuGet 복원 ===
echo [INFO] NuGet 패키지 복원 중...
cd /d "%PROJECT_PATH%"
dotnet restore
if errorlevel 1 (
    echo [ERROR] NuGet 패키지 복원 실패
    exit /b 1
)

REM === 빌드 ===
echo [INFO] 프로젝트 빌드 중...
dotnet build -c Release --no-restore -noWarn:CS1591 -noWarn:CS1573 -noWarn:CS1572 -noWarn:CS0618 -noWarn:CS0219
if errorlevel 1 (
    echo [ERROR] 빌드 실패
    exit /b 1
)

REM === 발행 ===
echo [INFO] 프로젝트 발행 중...
dotnet publish "%PROJECT_PATH%\ExcelMerge.GUI.csproj" -c Release -f net8.0-windows -r win-x64 --self-contained true -p:PublishSingleFile=true -o "%PUBLISH_PATH%" -v q --version-suffix 1.0.1 -noWarn:CS1591 -noWarn:CS1573 -noWarn:CS1572 -noWarn:CS0618 -noWarn:CS0219

if errorlevel 1 (
    echo [ERROR] 프로젝트 발행 실패
    exit /b 1
)

echo [INFO] 발행 완료: %PUBLISH_PATH%

REM === NSIS 스크립트 및 컴파일러 확인 ===
if not exist "%NSIS_SCRIPT_PATH%" (
    echo [ERROR] NSIS 스크립트가 존재하지 않습니다: %NSIS_SCRIPT_PATH%
    exit /b 1
)

if not exist "%NSIS_COMPILER_PATH%" (
    echo [ERROR] NSIS 컴파일러를 찾을 수 없습니다: %NSIS_COMPILER_PATH%
    exit /b 1
)

REM === NSIS 설치파일 빌드 ===
echo [INFO] NSIS 설치 파일 빌드 중...
"%NSIS_COMPILER_PATH%" "%NSIS_SCRIPT_PATH%"
if errorlevel 1 (
    echo [ERROR] NSIS 빌드 실패
    exit /b 1
)

echo [SUCCESS] 설치 프로그램 빌드가 완료되었습니다.

REM === 결과물 복사 ===
REM set "TARGET_DIR=\\ad.npixel.co.kr\1_Setup\34_ExcelMerge"
REM copy /Y "%SOLUTION_PATH%\ExcelMerge-Setup.exe" "%TARGET_DIR%"

endlocal
exit /b 0
