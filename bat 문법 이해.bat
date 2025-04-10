@echo off
setlocal enabledelayedexpansion

set "TEST_STRING=<Version>1.2.3</Version>"
echo !TEST_STRING!

for /f "usebackq tokens=1-3 delims=<>" %%a in ('!TEST_STRING!') do (
    echo %%a
    echo %%b
    echo %%c

    set "VERSION=%%b"

    for /f "usebackq tokens=1-3 delims=." %%a in ('!VERSION!') do (
        set major=%%a
        echo %%a
        set minor=%%b
        echo %%b
        set patch=%%c
        echo %%c
    )

    echo Version1: !major!.!minor!.!patch!
)

echo Version2: %major%.%minor%.%patch%

endlocal