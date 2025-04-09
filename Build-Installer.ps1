# ExcelMerge NSIS 설치 프로그램 빌드 스크립트

# 설정 변수
$SolutionPath = "E:\github\ExcelMerge"
$ProjectPath = "$SolutionPath\ExcelMerge.GUI"
$PublishPath = "$ProjectPath\bin\Release\net8.0-windows\publish"
$NsisScriptPath = "$SolutionPath\PackageNSIS.nsi"
$NsisCompilerPath = "C:\Program Files (x86)\NSIS\makensis.exe"

# 출력 디렉토리 존재 확인
if (!(Test-Path -Path $ProjectPath)) {
    Write-Error "프로젝트 경로를 찾을 수 없습니다: $ProjectPath"
    exit 1
}

# .NET 프로젝트 발행 (자체 포함 배포)
Write-Host "ExcelMerge 프로젝트 발행 중..." -ForegroundColor Cyan
Push-Location $ProjectPath
try {
    # 기존 발행 폴더 제거
    if (Test-Path -Path $PublishPath) {
        Remove-Item -Path $PublishPath -Recurse -Force
    }
    
    # 먼저 프로젝트 복원
    Write-Host "NuGet 패키지 복원 중..." -ForegroundColor Cyan
    dotnet restore
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "NuGet 패키지 복원 실패"
        exit 1
    }
    
    # 프로젝트 빌드
    Write-Host "프로젝트 빌드 중..." -ForegroundColor Cyan
    dotnet build -c Release --no-restore -noWarn:CS1591 -noWarn:CS1573 -noWarn:CS1572 -noWarn:CS0618 -noWarn:CS0219
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "프로젝트 빌드 실패"
        exit 1
    }
    
    # 프로젝트 발행 (.NET 8.0 자체 포함)
    Write-Host "프로젝트 발행 중..." -ForegroundColor Cyan
    dotnet publish "${ProjectPath}\ExcelMerge.GUI.csproj" -c Release -f net8.0-windows -r win-x64 --self-contained true -p:PublishSingleFile=true -o .\Build\Release -v q --version-suffix 1.0.1 -noWarn:CS1591 -noWarn:CS1573 -noWarn:CS1572 -noWarn:CS0618 -noWarn:CS0219

    if ($LASTEXITCODE -ne 0) {
        Write-Error "프로젝트 발행 실패"
        exit 1
    }
    
    Write-Host "프로젝트가 성공적으로 발행되었습니다." -ForegroundColor Green
}
finally {
    Pop-Location
}

# NSIS 스크립트 확인
if (!(Test-Path -Path $NsisScriptPath)) {
    Write-Error "NSIS 스크립트 파일을 찾을 수 없습니다: $NsisScriptPath"
    exit 1
}

# NSIS 컴파일러 확인
if (!(Test-Path -Path $NsisCompilerPath)) {
    Write-Error "NSIS 컴파일러를 찾을 수 없습니다: $NsisCompilerPath"
    exit 1
}

# NSIS 스크립트 컴파일
Write-Host "NSIS 설치 프로그램 빌드 중..." -ForegroundColor Cyan
& $NsisCompilerPath $NsisScriptPath

if ($LASTEXITCODE -ne 0) {
    Write-Error "NSIS 스크립트 컴파일 실패"
    exit 1
}

Write-Host "설치 프로그램이 성공적으로 빌드되었습니다." -ForegroundColor Green

# 대상 디렉터리 설정 (네트워크 경로)
$TARGET_DIR = "\\ad.npixel.co.kr\1_Setup\34_ExcelMerge"

# 복사할 파일 경로 설정
$SOURCE_FILE = Join-Path $SolutionPath "ExcelMerge-Setup.exe"

# 대상 경로로 파일 복사 (기존 파일이 있어도 덮어쓰기)
#Copy-Item -Path $SOURCE_FILE -Destination $TARGET_DIR -Force