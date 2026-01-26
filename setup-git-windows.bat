@echo off
REM Unity 프로젝트 Git & LFS 자동 설정 스크립트 (Windows)

echo ========================================
echo Unity 프로젝트 Git & LFS 설정 시작
echo ========================================
echo.

REM Git 확인
git --version >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] Git이 설치되지 않았습니다!
    echo https://git-scm.com/download/win 에서 설치하세요.
    pause
    exit /b 1
)
echo [OK] Git이 설치되어 있습니다.

REM Git LFS 확인
git lfs version >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] Git LFS가 설치되지 않았습니다!
    echo https://git-lfs.github.com/ 에서 설치하세요.
    pause
    exit /b 1
)
echo [OK] Git LFS가 설치되어 있습니다.
echo.

REM 현재 디렉토리 확인
if not exist "Assets" (
    echo [ERROR] Assets 폴더를 찾을 수 없습니다!
    echo Unity 프로젝트 루트 디렉토리에서 실행하세요.
    pause
    exit /b 1
)
echo [OK] Unity 프로젝트 디렉토리 확인됨.
echo.

REM Git 초기화
if exist ".git" (
    echo [INFO] Git 저장소가 이미 존재합니다.
) else (
    echo [STEP 1] Git 초기화 중...
    git init
    echo [OK] Git 초기화 완료
)
echo.

REM Git LFS 설치
echo [STEP 2] Git LFS 설정 중...
git lfs install
echo [OK] Git LFS 설정 완료
echo.

REM .gitignore 확인
if not exist ".gitignore" (
    echo [WARNING] .gitignore 파일이 없습니다!
    echo 제공된 .gitignore 파일을 프로젝트 루트에 복사하세요.
    pause
) else (
    echo [OK] .gitignore 파일이 있습니다.
)
echo.

REM .gitattributes 확인
if not exist ".gitattributes" (
    echo [WARNING] .gitattributes 파일이 없습니다!
    echo 제공된 .gitattributes 파일을 프로젝트 루트에 복사하세요.
    pause
) else (
    echo [OK] .gitattributes 파일이 있습니다.
)
echo.

REM LFS 추적 확인
echo [STEP 3] LFS 추적 파일 확인 중...
git lfs track
echo.

REM 사용자에게 커밋 여부 확인
echo ========================================
echo 첫 커밋을 진행하시겠습니까?
echo ========================================
echo.
set /p commit="첫 커밋 진행? (y/n): "
if /i "%commit%"=="y" (
    echo.
    echo [STEP 4] 모든 파일 추가 중...
    git add .
    echo [OK] 파일 추가 완료
    echo.
    
    echo [STEP 5] 커밋 생성 중...
    git commit -m "Initial commit: Unity project setup with LFS"
    echo [OK] 커밋 완료
    echo.
    
    echo ========================================
    echo 설정 완료!
    echo ========================================
    echo.
    echo 다음 명령어로 원격 저장소에 푸시하세요:
    echo   git remote add origin [저장소 URL]
    echo   git branch -M main
    echo   git push -u origin main
    echo.
) else (
    echo.
    echo 커밋을 건너뜁니다.
    echo 나중에 다음 명령어로 커밋하세요:
    echo   git add .
    echo   git commit -m "Initial commit"
    echo.
)

echo ========================================
echo Git 상태 확인
echo ========================================
git status

echo.
pause
