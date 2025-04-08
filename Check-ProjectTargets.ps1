# 프로젝트 파일들의 TargetFramework 확인 및 수정
$projectFiles = Get-ChildItem -Path "E:\github\ExcelMerge" -Recurse -Filter "*.csproj" | Select-Object -ExpandProperty FullName

foreach ($projectFile in $projectFiles) {
    Write-Host "프로젝트 파일 확인: $projectFile" -ForegroundColor Yellow
    $content = Get-Content -Path $projectFile -Raw
    
    if ($content -match "<TargetFramework>([^<]+)</TargetFramework>") {
        $currentTarget = $matches[1]
        Write-Host "  현재 대상 프레임워크: $currentTarget" -ForegroundColor Cyan
        
        if ($currentTarget -ne "net5.0-windows") {
            Write-Host "  대상 프레임워크를 net5.0-windows로 변경합니다." -ForegroundColor Green
            $content = $content -replace "<TargetFramework>[^<]+</TargetFramework>", "<TargetFramework>net5.0-windows</TargetFramework>"
            Set-Content -Path $projectFile -Value $content
        }
    } else {
        Write-Host "  TargetFramework 태그를 찾을 수 없습니다." -ForegroundColor Red
    }
}

Write-Host "모든 프로젝트 파일 확인 완료" -ForegroundColor Green
