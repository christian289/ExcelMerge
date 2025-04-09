@echo off
setlocal enabledelayedexpansion

REM === 경로 설정 ===
set "SOLUTION_PATH=%WORKSPACE%"
set "PROJECT_PATH=%SOLUTION_PATH%\ExcelMerge.GUI"
set "PROJECT_FILE=%PROJECT_PATH%\ExcelMerge.GUI.csproj"
set "PUBLISH_PATH=%PROJECT_PATH%\Build\Release"
set "NSIS_SCRIPT_PATH=%SOLUTION_PATH%\PackageNSIS.nsi"
set "NSIS_COMPILER_PATH=D:\NSIS\makensis.exe"

REM === 기본 버전 설정 (프로젝트 파일에서 추출 실패 시 사용) ===
set "VERSION_MAJOR=1"
set "VERSION_MINOR=0"
set "VERSION=%VERSION_MAJOR%.%VERSION_MINOR%.%BUILD_NUMBER%"

REM === 프로젝트 파일에서 버전 정보 추출 시도 ===
echo [INFO] 프로젝트 파일에서 버전 정보 추출 중...

REM 프로젝트 파일에서 Version 태그를 찾아서 추출 (간단한 방법)
findstr "<Version>" "%PROJECT_FILE%" > version_temp.txt
if %errorlevel% equ 0 (
    for /f "tokens=2 delims=<>" %%a in ('findstr "<Version>" "%PROJECT_FILE%"') do (
        set "PROJECT_VERSION=%%a"
        echo [INFO] 프로젝트 버전: !PROJECT_VERSION!
        
        REM 버전 구성요소 분리(메이저.마이너)
        for /f "tokens=1,2 delims=." %%b in ("!PROJECT_VERSION!") do (
            set "VERSION_MAJOR=%%b"
            set "VERSION_MINOR=%%c"
        )
        
        set "VERSION=%VERSION_MAJOR%.%VERSION_MINOR%.%BUILD_NUMBER%"
    )
)
del version_temp.txt 2>nul

echo [INFO] 최종 버전: %VERSION%

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
dotnet publish "%PROJECT_FILE%" -c Release -f net8.0-windows -r win-x64 --self-contained true -p:PublishSingleFile=true -o "%PUBLISH_PATH%" -v q -p:Version=%VERSION% -noWarn:CS1591 -noWarn:CS1573 -noWarn:CS1572 -noWarn:CS0618 -noWarn:CS0219

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
"%NSIS_COMPILER_PATH%" /DPRODUCT_VERSION="%VERSION%" "%NSIS_SCRIPT_PATH%"
if errorlevel 1 (
    echo [ERROR] NSIS 빌드 실패
    exit /b 1
)

REM === 버전 정보 파일 생성 (다음 단계로 정보 전달) ===
echo %VERSION%> "%WORKSPACE%\version.txt"

echo [SUCCESS] 설치 프로그램 빌드가 완료되었습니다. 버전: %VERSION%
endlocal
exit /b 0