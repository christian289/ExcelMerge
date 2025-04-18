!include "MUI2.nsh"
!include "FileFunc.nsh"
!include "LogicLib.nsh"
!include "StrReplace.nsh"
!include "Sections.nsh"

!ifndef PRODUCT_VERSION
  !define PRODUCT_VERSION "1.0.0"
!endif

!define PRODUCT_NAME "ExcelMerge"
!define PRODUCT_PUBLISHER "NPIXEL"

Name "${PRODUCT_NAME} ${PRODUCT_VERSION}"
OutFile ".\Build\Release\ExcelMerge-Setup-${PRODUCT_VERSION}.exe"
Unicode True

InstallDir "$PROGRAMFILES64\ExcelMerge"
InstallDirRegKey HKLM "Software\ExcelMerge" "Install_Dir"

RequestExecutionLevel admin

!define MUI_ABORTWARNING
!define MUI_ICON ".\ExcelMerge.GUI\app64.ico"
!define MUI_UNICON ".\ExcelMerge.GUI\app64.ico"

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_LANGUAGE "Korean"
!insertmacro MUI_LANGUAGE "English"
!insertmacro MUI_LANGUAGE "Japanese"
!insertmacro MUI_LANGUAGE "SimpChinese"
!insertmacro MUI_LANGUAGE "TradChinese"

!define MUI_LANGDLL_ALLLANGUAGES

; ===================================================================== [Section] SecMain
Section "!ExcelMerge" SecMain
  SectionIn RO
  
  Call CheckNetFramework
  
  SetOutPath "$INSTDIR"
  
  File /r ".\ExcelMerge.GUI\bin\Release\net8.0-windows\*.*"
  File "ExcelMerge.GUI\app64.ico"

  WriteRegStr HKLM "Software\ExcelMerge" "IconPath" "$INSTDIR\app64.ico"
  WriteRegStr HKCR "Applications\ExcelMerge.exe\DefaultIcon" "" "$INSTDIR\app64.ico"

  CreateDirectory "$SMPROGRAMS\ExcelMerge"
  CreateShortcut "$SMPROGRAMS\ExcelMerge\ExcelMerge.lnk" "$INSTDIR\ExcelMerge.exe" "" "$INSTDIR\app64.ico"
  CreateShortcut "$SMPROGRAMS\ExcelMerge\Uninstall.lnk" "$INSTDIR\Uninstall.exe" "" "$INSTDIR\app64.ico"
  
  WriteRegStr HKLM "Software\ExcelMerge" "Install_Dir" "$INSTDIR"
  
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\ExcelMerge" "DisplayName" "ExcelMerge"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\ExcelMerge" "UninstallString" '"$INSTDIR\Uninstall.exe"'
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\ExcelMerge" "DisplayIcon" "$INSTDIR\app64.ico"
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\ExcelMerge" "NoModify" 1
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\ExcelMerge" "NoRepair" 1
  
  File "jq-windows-amd64.exe"

  WriteUninstaller "$INSTDIR\Uninstall.exe"
SectionEnd
; =====================================================================

; ===================================================================== [Section] SecDesktop
Section "Desktop ShortCut" SecDesktop
  CreateShortcut "$DESKTOP\ExcelMerge.lnk" "$INSTDIR\ExcelMerge.exe" "" "$INSTDIR\app64.ico"
SectionEnd
; =====================================================================

; ===================================================================== [Section] SecGitConfig
Section ".gitconfig Update" SecGitConfig
  SectionGetText ${SecGitConfig} $9
  ${If} $9 == ""
    Goto GitConfigDone
  ${EndIf}

  Call UpdateGitConfig
GitConfigDone:
SectionEnd
; =====================================================================

; ===================================================================== [Section] SecFork
Section "Set Fork External Diff Tool" SecFork
  SectionGetText ${SecFork} $9
  StrCmp $9 "" ForkDone

  IfFileExists "$LOCALAPPDATA\Fork\current\Fork.exe" ForkExists
  SetOutPath "$INSTDIR"
  Goto ForkPathFound

ForkExists:
  StrCpy $0 "$LOCALAPPDATA\Fork\current"

ForkPathFound:
  StrCpy $1 "$LOCALAPPDATA\Fork\settings.json"
  IfFileExists $1 0 ForkDone
  CopyFiles "$1" "$1.backup"
  ${StrReplaceV4} $2 "\" "\\\\" $INSTDIR
  
  nsExec::ExecToStack 'cmd /c ""$INSTDIR\jq-windows-amd64.exe" --arg ExcelMergeInstallPath "$INSTDIR\ExcelMerge.exe" ".ExternalDiffTool.ApplicationPath |= $$ExcelMergeInstallPath" "$1" > "$LOCALAPPDATA\Fork\temp.json""'
  Pop $3 ; 리턴 값
  Pop $4 ; 출력
  IntCmp $3 0 0 RestoreBackup RestoreBackup
  nsExec::ExecToStack 'cmd /c "move /y "$LOCALAPPDATA\Fork\temp.json" "$LOCALAPPDATA\Fork\settings.json""'
  Pop $5
  
  IntCmp $5 0 DeleteFiles RestoreBackup
  
RestoreBackup:
  CopyFiles "$1.backup" "$1"
  
DeleteFiles:
  Delete "$LOCALAPPDATA\Fork\temp.json"
  Delete "$1.backup"
  
ForkDone:
SectionEnd
; =====================================================================

; ===================================================================== [Section] SecTortoiseSVN
Section "TortoiseSVN Integration" SecTortoiseSVN
  SectionGetText ${SecTortoiseSVN} $9
  ${If} $9 == ""
    Goto SVNDone
  ${EndIf}

  SectionGetFlags ${SecTortoiseSVN} $0
  IntOp $0 $0 & ${SF_SELECTED}
  ${If} $0 = 0
    Goto SVNDone
  ${EndIf}
  
  WriteRegStr HKCU 'SOFTWARE\TortoiseSVN' 'Diff' '$INSTDIR\ExcelMerge.exe'
  WriteRegStr HKCU 'SOFTWARE\TortoiseSVN' 'DiffProps' '$INSTDIR\ExcelMerge.exe'
  WriteRegStr HKCU 'SOFTWARE\TortoiseSVN' 'DiffViewer' '$INSTDIR\ExcelMerge.exe'

  StrCpy $1 '"$INSTDIR\ExcelMerge.exe" -s %base -d %mine'
  WriteRegStr HKCU 'SOFTWARE\TortoiseSVN\DiffTools' '.xls' $1
  WriteRegStr HKCU 'SOFTWARE\TortoiseSVN\DiffTools' '.xlsx' $1
  WriteRegStr HKCU 'SOFTWARE\TortoiseSVN\DiffTools' '.xlsm' $1
  WriteRegStr HKCU 'SOFTWARE\TortoiseSVN\DiffTools' '.csv' $1
  WriteRegStr HKCU 'SOFTWARE\TortoiseSVN\DiffTools' '.tsv' $1
  
SVNDone:
SectionEnd
; =====================================================================

; ===================================================================== [Section] Uninstall
Section "Uninstall"
  ReadEnvStr $0 "USERPROFILE"
  StrCpy $0 "$0\.gitconfig"
  DeleteINISec $0 diff
  DeleteINISec $0 'difftool "ExcelMerge"'
  DeleteINISec $0 alias

  Delete "$INSTDIR\*.*"
  RMDir /r "$INSTDIR"
  
  Delete "$SMPROGRAMS\ExcelMerge\*.*"
  RMDir "$SMPROGRAMS\ExcelMerge"
  
  Delete "$DESKTOP\ExcelMerge.lnk"
  
  DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\ExcelMerge"
  DeleteRegKey HKLM "Software\ExcelMerge"
  DeleteRegKey HKCR "Applications\ExcelMerge.exe"

  WriteRegStr HKCU "SOFTWARE\TortoiseSVN" "Diff" ""
  WriteRegStr HKCU "SOFTWARE\TortoiseSVN" "DiffProps" ""
  WriteRegStr HKCU "SOFTWARE\TortoiseSVN" "DiffViewer" ""

  StrCpy $1 'wscript.exe "C:\Program Files\TortoiseSVN\Diff-Scripts\diff-xls.js" %base %mine //E:javascript'
  WriteRegStr HKCU "SOFTWARE\TortoiseSVN\DiffTools" ".xls" $1
  WriteRegStr HKCU "SOFTWARE\TortoiseSVN\DiffTools" ".xlsx" $1
  WriteRegStr HKCU "SOFTWARE\TortoiseSVN\DiffTools" ".xlsm" $1
  DeleteRegValue HKCU "SOFTWARE\TortoiseSVN\DiffTools" ".csv"
  DeleteRegValue HKCU "SOFTWARE\TortoiseSVN\DiffTools" ".tsv"

  System::Call 'shell32.dll::SHChangeNotify(i, i, i, i) v (0x08000000, 0, 0, 0)'
SectionEnd
; =====================================================================

; ===================================================================== LangString
LangString DESC_SecMain ${LANG_KOREAN} "ExcelMerge 프로그램을 설치합니다."
LangString DESC_SecMain ${LANG_ENGLISH} "Installs the ExcelMerge program."
LangString DESC_SecMain ${LANG_JAPANESE} "ExcelMerge プログラムをインストールします。"
LangString DESC_SecMain ${LANG_SIMPCHINESE} "安装 ExcelMerge 程序。"
LangString DESC_SecMain ${LANG_TRADCHINESE} "安裝 ExcelMerge 程序。"

LangString DESC_SecDesktop ${LANG_KOREAN} "바탕화면에 ExcelMerge 바로가기를 생성합니다."
LangString DESC_SecDesktop ${LANG_ENGLISH} "Creates a desktop shortcut for ExcelMerge."
LangString DESC_SecDesktop ${LANG_JAPANESE} "デスクトップに ExcelMerge のショートカットを作成します。"
LangString DESC_SecDesktop ${LANG_SIMPCHINESE} "在桌面上创建 ExcelMerge 快捷方式。"
LangString DESC_SecDesktop ${LANG_TRADCHINESE} "在桌面上創建 ExcelMerge 快捷方式。"

LangString DESC_SecGitConfig ${LANG_KOREAN} "Git에서 ExcelMerge를 diff 도구로 사용할 수 있도록 .gitconfig 파일을 업데이트합니다."
LangString DESC_SecGitConfig ${LANG_ENGLISH} "Updates .gitconfig file to use ExcelMerge as a diff tool for Git."
LangString DESC_SecGitConfig ${LANG_JAPANESE} "Git で ExcelMerge を diff ツールとして使用するために .gitconfig ファイルを更新します。"
LangString DESC_SecGitConfig ${LANG_SIMPCHINESE} "更新 .gitconfig 文件以将 ExcelMerge 用作 Git 的 diff 工具。"
LangString DESC_SecGitConfig ${LANG_TRADCHINESE} "更新 .gitconfig 文件以將 ExcelMerge 用作 Git 的 diff 工具。"

LangString DESC_SecFork ${LANG_KOREAN} "Fork의 diff 도구로 ExcelMerge를 선택합니다."
LangString DESC_SecFork ${LANG_ENGLISH} "Installs Fork Git Client. An intuitive Git management tool."
LangString DESC_SecFork ${LANG_JAPANESE} "Fork Git クライアントをインストールします。直感的な Git 管理ツールです。"
LangString DESC_SecFork ${LANG_SIMPCHINESE} "安装 Fork Git 客户端。一个直观的 Git 管理工具。"
LangString DESC_SecFork ${LANG_TRADCHINESE} "安裝 Fork Git 客戶端。一個直觀的 Git 管理工具。"

LangString DESC_SecTortoiseSVN ${LANG_KOREAN} "TortoiseSVN의 Diff Viewer로 ExcelMerge를 선택합니다. (.tsv, .csv, .xls, .xlsx, .xlsm 적용)"
LangString DESC_SecTortoiseSVN ${LANG_ENGLISH} "Integrates ExcelMerge with TortoiseSVN."
LangString DESC_SecTortoiseSVN ${LANG_JAPANESE} "ExcelMerge を TortoiseSVN と統合します。"
LangString DESC_SecTortoiseSVN ${LANG_SIMPCHINESE} "将 ExcelMerge 与 TortoiseSVN 集成。"
LangString DESC_SecTortoiseSVN ${LANG_TRADCHINESE} "將 ExcelMerge 與 TortoiseSVN 集成。"
; =====================================================================

; ===================================================================== 섹션 설명에 추가
!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
  !insertmacro MUI_DESCRIPTION_TEXT ${SecMain} $(DESC_SecMain)
  !insertmacro MUI_DESCRIPTION_TEXT ${SecDesktop} $(DESC_SecDesktop)
  !insertmacro MUI_DESCRIPTION_TEXT ${SecGitConfig} $(DESC_SecGitConfig)
  !insertmacro MUI_DESCRIPTION_TEXT ${SecFork} $(DESC_SecFork)
  !insertmacro MUI_DESCRIPTION_TEXT ${SecTortoiseSVN} $(DESC_SecTortoiseSVN)
!insertmacro MUI_FUNCTION_DESCRIPTION_END
; =====================================================================

; ===================================================================== [Function] .onInit
Function .onInit
  !insertmacro MUI_LANGDLL_DISPLAY
  Call CheckGitInstallation
  Call CheckForkInstallation
  Call CheckTortoiseSVNInstallation
FunctionEnd
; =====================================================================

; ===================================================================== [Function] CheckGitInstallation
Function CheckGitInstallation
  nsExec::ExecToStack 'where git'
  Pop $0
  ${If} $0 == 0
    Goto GitFound
  ${EndIf}

  nsExec::ExecToStack 'git --version'
  Pop $0
  ${If} $0 == 0
    Goto GitFound
  ${EndIf}
  
  SectionSetText ${SecGitConfig} ""
  Goto GitCheckDone
  
GitFound:
  SectionSetFlags ${SecGitConfig} ${SF_SELECTED}

GitCheckDone:
FunctionEnd
; =====================================================================

; ===================================================================== [Function] CheckForkInstallation
Function CheckForkInstallation
  IfFileExists "$LOCALAPPDATA\Fork\current\Fork.exe" ForkFound ForkNotFound
  
ForkFound:
  SectionSetFlags ${SecFork} ${SF_SELECTED}
  Goto ForkCheckDone
    
ForkNotFound:
  SectionSetText ${SecFork} ""
  
ForkCheckDone:
FunctionEnd
; =====================================================================

; ===================================================================== [Function] CheckTortoiseSVNInstallation
Function CheckTortoiseSVNInstallation
  ReadRegStr $0 HKLM "SOFTWARE\TortoiseSVN" "Directory"
  ${If} $0 != ""
    Goto SVNFound
  ${EndIf}
  
  ${If} ${FileExists} "$PROGRAMFILES64\TortoiseSVN\bin\TortoiseProc.exe"
    Goto SVNFound
  ${EndIf}

  ${If} ${FileExists} "$PROGRAMFILES\TortoiseSVN\bin\TortoiseProc.exe"
    Goto SVNFound
  ${EndIf}

  Goto SVNNotFound
  
SVNFound:
  SectionSetFlags ${SecTortoiseSVN} ${SF_SELECTED}
  Goto SVNCheckDone

SVNNotFound:
  SectionSetText ${SecTortoiseSVN} ""

SVNCheckDone:
FunctionEnd
; =====================================================================

; ===================================================================== [Function] CheckNetFramework
Function CheckNetFramework
  nsExec::ExecToStack 'cmd /c "dotnet --list-runtimes | findstr "Microsoft.NETCore.App 8.0""'
  Pop $0
  Pop $1
  
  ${If} $0 != 0
  ${OrIf} $1 == ""
    MessageBox MB_YESNO|MB_ICONQUESTION ".NET 8.0 Runtime is not detected on your system. Would you like to open the download page to install it?" IDNO DotNetEnd
    ExecShell "open" "https://dotnet.microsoft.com/download/dotnet/8.0"
  ${EndIf}
  
DotNetEnd:
FunctionEnd
; =====================================================================

; ===================================================================== [Function] UpdateGitConfig
Function UpdateGitConfig
  ReadEnvStr $0 "USERPROFILE"
  StrCpy $0 "$0\.gitconfig"
  IfFileExists $0 GitConfigExists GitConfigNotExists
  
GitConfigExists:
  FileOpen $1 $0 a
  FileSeek $1 0 END
  FileWrite $1 "[diff]$\r$\n"
  FileWrite $1 "$\ttool = ExcelMerge$\r$\n"
  StrCpy $2 '[difftool "ExcelMerge"]'
  FileWrite $1 "$2$\r$\n"

  ${StrReplaceV4} $3 "\" "\\" $INSTDIR
  StrCpy $2 '$\tcmd = "$3\\ExcelMerge.exe" diff -s "$$LOCAL" -d "$$REMOTE" -c WinMerge -i -w -v -k$\r$\n'
  FileWrite $1 $2

  FileWrite $1 "[alias]$\r$\n"
  FileWrite $1 "$\twindiff = difftool -g -y -t ExcelMerge$\r$\n"
  FileClose $1
  Goto GitConfigDone

GitConfigNotExists:
  FileOpen $1 $0 w
  FileWrite $1 "[diff]$\r$\n"
  FileWrite $1 "tool = ExcelMerge$\r$\n"
  StrCpy $2 '[difftool "ExcelMerge"]'
  FileWrite $1 "$2$\r$\n"
  
  ${StrReplaceV4} $3 "\" "\\" $INSTDIR
  StrCpy $2 '$\tcmd = "$3\\ExcelMerge.exe" diff -s "$$LOCAL" -d "$$REMOTE" -c WinMerge -i -w -v -k$\r$\n'
  FileWrite $1 $2

  FileWrite $1 "[alias]$\r$\n"
  FileWrite $1 "windiff = difftool -g -y -t ExcelMerge$\r$\n"
  FileClose $1

GitConfigDone:
FunctionEnd
; =====================================================================
