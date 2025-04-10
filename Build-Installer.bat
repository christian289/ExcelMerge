@echo off
setlocal enabledelayedexpansion

REM === Set default values ===
if not defined BUILD_NUMBER (
    set "BUILD_NUMBER=0"
)

REM Set WORKSPACE to the script execution directory if not already defined
if not defined WORKSPACE (
    for %%A in ("%~dp0\.") do set "WORKSPACE=%%~fA"
)

REM === Path configuration ===
set "SOLUTION_PATH=%WORKSPACE%"
set "PROJECT_PATH=%SOLUTION_PATH%\ExcelMerge.GUI"
set "PROJECT_FILE=%PROJECT_PATH%\ExcelMerge.GUI.csproj"
set "PUBLISH_PATH=%PROJECT_PATH%\Build\Release"
set "NSIS_SCRIPT_PATH=%SOLUTION_PATH%\PackageNSIS.nsi"

REM Set NSIS compiler path (modify as needed) 8.3 format
set "NSIS_COMPILER_PATH=C:\Progra~2\NSIS\makensis.exe"

REM === Default version (used if extracting from project file fails) ===
set "VERSION_MAJOR=1"
set "VERSION_MINOR=0"
set "VERSION=%VERSION_MAJOR%.%VERSION_MINOR%.%BUILD_NUMBER%"

REM === Try extracting version info from project file ===
echo [INFO] Extracting version information from project file...

REM Find and extract the <Version> tag from the project file
(for /f "usebackq delims=" %%L in (`findstr "<Version>" "%PROJECT_FILE%"`) do (
    set "line=%%L"
    call set "line=%%line: =%%"
    echo !line!
)) > version_temp.txt
type version_temp.txt
if %errorlevel% equ 0 (
    for /f "usebackq tokens=2 delims=<>" %%a in ("version_temp.txt") do (
        set "PROJECT_VERSION=%%a"
        echo [INFO] Project version: !PROJECT_VERSION!
        
        REM Split version into major and minor components
        for /f "tokens=1-2 delims=." %%b in ("!PROJECT_VERSION!") do (
            set "VERSION_MAJOR=%%b"
            set "VERSION_MINOR=%%c"
        )
        
        set "VERSION=!VERSION_MAJOR!.!VERSION_MINOR!.%BUILD_NUMBER%"
    )
)
del version_temp.txt 2>nul

echo [INFO] Final version: %VERSION%

REM === Clear existing publish folder ===
if exist "%PUBLISH_PATH%" (
    echo [INFO] Removing existing publish folder...
    rmdir /s /q "%PUBLISH_PATH%"
)

REM === Restore NuGet packages ===
echo [INFO] Restoring NuGet packages...
cd /d "%PROJECT_PATH%"
dotnet restore
if errorlevel 1 (
    echo [ERROR] Failed to restore NuGet packages
    exit /b 1
)

REM === Build the project ===
echo [INFO] Building project...
dotnet build -c Release --no-restore -p:Version=%VERSION% -noWarn:CS1591 -noWarn:CS1573 -noWarn:CS1572 -noWarn:CS0618 -noWarn:CS0219
if errorlevel 1 (
    echo [ERROR] Build failed
    exit /b 1
)

REM === Publish the project ===
echo [INFO] Publishing project...
dotnet publish "%PROJECT_FILE%" -c Release -f net8.0-windows -r win-x64 --self-contained true -p:PublishSingleFile=true -o "%PUBLISH_PATH%" -v q -p:Version=%VERSION% -noWarn:CS1591 -noWarn:CS1573 -noWarn:CS1572 -noWarn:CS0618 -noWarn:CS0219

if errorlevel 1 (
    echo [ERROR] Publish failed
    exit /b 1
)

echo [INFO] Publish completed: %PUBLISH_PATH%

REM === Check NSIS script and compiler ===
if not exist "%NSIS_SCRIPT_PATH%" (
    echo [ERROR] NSIS script not found: %NSIS_SCRIPT_PATH%
    exit /b 1
)

if not exist "%NSIS_COMPILER_PATH%" (
    echo [ERROR] NSIS compiler not found: %NSIS_COMPILER_PATH%
    exit /b 1
)

REM === Build NSIS installer ===
echo [INFO] Building NSIS installer...
"%NSIS_COMPILER_PATH%" /DPRODUCT_VERSION="%VERSION%" "%NSIS_SCRIPT_PATH%"
if errorlevel 1 (
    echo [ERROR] NSIS build failed
    exit /b 1
)

REM === Create version info file for next pipeline step ===
echo %VERSION%> "%WORKSPACE%\version.txt"

echo [SUCCESS] Installer build completed. Version: %VERSION%
endlocal
exit /b 0