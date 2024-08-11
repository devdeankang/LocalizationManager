Unicode true
 
; headers
!include "MUI2.nsh" # Modern UI를 적용합니다.
!include LogicLib.nsh
!include nsDialogs.nsh


; define const
# 자주 사용하는 문자열을 상수로 지정해두면 유지보수가 수월해집니다.
!define APP_NAME "Localization Manager"
!define APP_DIR "..\LocalizationManager\bin\Release\"
!define FILE_VERSION "0.9.5.1"
!define PRODUCT_VERSION "0.9.5.1"
!define MANUFACTURER "LocalizationTool"
!define REG_APP_NAME "LocalizationManager"
!define REG_HKLM_UNINST "Software\Microsoft\Windows\CurrentVersion\Uninstall"
 
; metadata
# 인스톨러 이름
Name "${APP_NAME}"
# 빌드 후 생성할 파일명
OutFile "${APP_NAME}_${PRODUCT_VERSION}.exe"
# 설치 디렉터리 기본값
InstallDir "$PROGRAMFILES32\${MANUFACTURER}\${APP_NAME}"
# Windows 커널 5.1부터 적용된 비주얼 스타일을 적용합니다.
XPStyle on
# 인스톨러의 압축 방식을 지정합니다.
SetCompressor zlib
# 설치 도중 자세한 정보를 기본적으로 보여줍니다.
ShowInstDetails show
 
 
; file descriptions
# 파일 버전 정보를 기입합니다. 파일 속성의 자세히 탭에 나타나는 정보입니다.
VIProductVersion "${PRODUCT_VERSION}"
VIAddVersionKey "FileVersion" "${PRODUCT_VERSION}"
VIAddVersionKey "ProductVersion" "${PRODUCT_VERSION}"
VIAddVersionKey "ProductName" "${APP_NAME}"
VIAddVersionKey "CompanyName" "${MANUFACTURER}"
VIAddVersionKey "FileDescription" "${APP_NAME} Installer"
VIAddVersionKey "LegalCopyright" "${MANUFACTURER} Inc."
 
; MUI config
!define MUI_ICON "icon.ico"
!define MUI_UNICON "icon.ico"
 
; inst pages
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
Page custom nsDialogEnter nsDialogLeave
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH
 
; uninst pages
!insertmacro MUI_UNPAGE_WELCOME
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_UNPAGE_FINISH
 
!insertmacro MUI_LANGUAGE "Korean"

Var hMainDlg
;Var hCheckBox1
;Var CheckState

Function nsDialogEnter
	nsDialogs::Create 1018
	Pop $hMainDlg
	
	${If} $hMainDlg == error
		Abort
	${EndIf}
	
;	${NSD_CreateCheckBox} 0 0 100% 12u '설치폴더 환경변수 등록'
;	Pop $hCheckBox1	
	
;	${NSD_SetState} $hCheckBox1 ${BST_CHECKED}
	nsDialogs::Show
	
FunctionEnd

Function nsDialogLeave
;	${NSD_GetState} $hCheckBox1 $CheckState
;	${If} $CheckState == ${BST_CHECKED}
#		MessageBox MB_OK '설치 폴더를 환경변수로 등록합니다.'
;	${EndIf}
FunctionEnd


#========================================================================================================================================================
Section "main"
# 파일이 설치될 디렉터리를 지정합니다.
SetOutPath "$INSTDIR"
# 설치할 파일들을 지정합니다. /r 옵션을 통해 이 스크립트가 존재하는 디렉터리의 ${APP_DIR} 폴더 전체를 압축한 후 SetOutPath로 지정한 위치에 설치(압축 해제)합니다.
File /r "${APP_DIR}"
File "icon.ico"

;${If} $CheckState == ${BST_CHECKED}
;    Push "$INSTDIR"
;    Call AddToPath
;${EndIf}
;nsExec::Exec "$INSTDIR\LocalizationManager.exe -install"
SectionEnd
#========================================================================================================================================================
 
#========================================================================================================================================================
Section "-post"
# 설치 로그에 문자열을 한 줄 출력합니다.
DetailPrint "언인스톨 정보 등록"
# 언인스톨러를 생성합니다.
WriteUninstaller "$PROGRAMFILES32\${MANUFACTURER}\${APP_NAME}\uninstall.exe"
# 시작메뉴에 바로가기를 생성합니다.
SetShellVarContext all # 모든 사용자 계정의 시작메뉴에 생성합니다.
CreateDirectory $SMPROGRAMS\${MANUFACTURER}
SetOutPath "$INSTDIR\"

CreateShortCut "$SMPROGRAMS\${MANUFACTURER}\${APP_NAME}.lnk" "$INSTDIR\LocalizationManager.exe" "" "$INSTDIR\icon.ico"

# 언인스톨 레지스트리를 추가합니다.
WriteRegStr HKLM "${REG_HKLM_UNINST}\${REG_APP_NAME}" "DisplayName" "${APP_NAME}"
WriteRegStr HKLM "${REG_HKLM_UNINST}\${REG_APP_NAME}" "DisplayIcon" "$INSTDIR\uninstall.exe"
WriteRegStr HKLM "${REG_HKLM_UNINST}\${REG_APP_NAME}" "DisplayVersion" "${PRODUCT_VERSION}"
WriteRegStr HKLM "${REG_HKLM_UNINST}\${REG_APP_NAME}" "InstallLocation" "$INSTDIR"
WriteRegStr HKLM "${REG_HKLM_UNINST}\${REG_APP_NAME}" "Publisher" "${MANUFACTURER}"
WriteRegStr HKLM "${REG_HKLM_UNINST}\${REG_APP_NAME}" "UninstallString" "$INSTDIR\uninstall.exe"
SectionEnd
#========================================================================================================================================================
 
#======================================================================================================================================================== 
# 언인스톨러에서 사용하는 섹션입니다.
Section Uninstall
SectionIn RO

;Push $INSTDIR
;Call un.RemoveFromPath

;nsExec::Exec "$INSTDIR\LocalizationManager.exe -uninstall"

RMDir /r "$INSTDIR\*"
RMDir "$INSTDIR"

SetShellVarContext all
RMDir /r "$STARTMENU\${APP_NAME}"
DeleteRegKey HKLM "${REG_HKLM_UNINST}\${REG_APP_NAME}"
SectionEnd
#========================================================================================================================================================






;--------------------------------------------------------------------
; Path functions
;
; Based on example from:
; http://nsis.sourceforge.net/Path_Manipulation
;


!include "WinMessages.nsh"

; Registry Entry for environment (NT4,2000,XP)
; All users:
;!define Environ 'HKLM "SYSTEM\CurrentControlSet\Control\Session Manager\Environment"'
; Current user only:
!define Environ 'HKCU "Environment"'

