!include "MUI2.nsh"
!include "FileFunc.nsh"

!define PRODUCT_NAME "ExcelMerge"
!define PRODUCT_VERSION "1.0.0"
!define PRODUCT_PUBLISHER "NPIXEL"

Name "${PRODUCT_NAME} ${PRODUCT_VERSION}"
OutFile "ExcelMerge-Setup.exe"
Unicode True

InstallDir "$PROGRAMFILES\ExcelMerge"
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
Function .onInit
  !insertmacro MUI_LANGDLL_DISPLAY
FunctionEnd

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

Section "ExcelMerge" SecMain
  SectionIn RO
  
  Call CheckNetFramework
  
  SetOutPath "$INSTDIR"
  
  File /r "E:\github\ExcelMerge\ExcelMerge.GUI\bin\Release\net8.0-windows\*.*"
  File "ExcelMerge.GUI\app64.ico"
  
  CreateDirectory "$SMPROGRAMS\ExcelMerge"
  CreateShortcut "$SMPROGRAMS\ExcelMerge\ExcelMerge.lnk" "$INSTDIR\ExcelMerge.exe" "" "$INSTDIR\app64.ico"
  CreateShortcut "$SMPROGRAMS\ExcelMerge\Uninstall.lnk" "$INSTDIR\Uninstall.exe" "" "$INSTDIR\app64.ico"
  
  WriteRegStr HKLM "Software\ExcelMerge" "Install_Dir" "$INSTDIR"
  
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\ExcelMerge" "DisplayName" "ExcelMerge"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\ExcelMerge" "UninstallString" '"$INSTDIR\Uninstall.exe"'
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\ExcelMerge" "NoModify" 1
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\ExcelMerge" "NoRepair" 1
  
  WriteUninstaller "$INSTDIR\Uninstall.exe"
SectionEnd

Section "Desktop ShortCut" SecDesktop
  CreateShortcut "$DESKTOP\ExcelMerge.lnk" "$INSTDIR\ExcelMerge.exe" "" "$INSTDIR\app64.ico"
SectionEnd

Section "Uninstall"
  Delete "$INSTDIR\*.*"
  RMDir /r "$INSTDIR"
  
  Delete "$SMPROGRAMS\ExcelMerge\*.*"
  RMDir "$SMPROGRAMS\ExcelMerge"
  
  Delete "$DESKTOP\ExcelMerge.lnk"
  
  DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\ExcelMerge"
  DeleteRegKey HKLM "Software\ExcelMerge"
SectionEnd
