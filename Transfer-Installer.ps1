# 설정 변수
$SolutionPath = "E:\github\ExcelMerge"

# 대상 디렉터리 설정 (네트워크 경로)
$TARGET_DIR = "\\ad.npixel.co.kr\1_Setup\34_ExcelMerge"

# 복사할 파일 경로 설정
$SOURCE_FILE = Join-Path $SolutionPath "ExcelMerge-Setup.exe"

# 대상 경로로 파일 복사 (기존 파일이 있어도 덮어쓰기)
Copy-Item -Path $SOURCE_FILE -Destination $TARGET_DIR -Force