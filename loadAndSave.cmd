@echo off
setlocal enabledelayedexpansion
cd /d "%~dp0"
title Git Sync Portal - Mau Edition

rem Jump to the menu first so we don't fall into helper labels
goto menu

rem -------------------------
rem Helper: check last error
rem -------------------------
:check_error
if errorlevel 1 (
  echo.
  echo ERROR: Command failed with exit code %errorlevel%.
  echo If this was a push, try:  git pull --rebase origin main
  echo Then push again. For LFS weirdness:  git lfs push --all origin main
  echo.
  pause
  goto menu
)
goto :eof

:menu
cls
echo ============================================
echo         GIT SYNC PORTAL - MAU EDITION
echo ============================================
echo.
echo  [1] Pull (Load latest version from GitHub)
echo  [2] Push (Save current work to GitHub)
echo  [3] Status
echo  [4] Exit
echo.
set /p choice=Enter your choice: 

if "%choice%"=="1" goto pull
if "%choice%"=="2" goto push
if "%choice%"=="3" goto status
if "%choice%"=="4" exit /b 0
goto menu

:pull
echo --------------------------------------------
echo Pulling latest version from GitHub...
echo --------------------------------------------
git pull --rebase
call :check_error
echo.
echo Pull complete.
pause
goto menu

:push
echo --------------------------------------------
echo Preparing to push your work...
echo --------------------------------------------
git add .
set /p msg=Enter commit message (default: "Quick update"): 
if "%msg%"=="" set msg=Quick update
git commit -m "%msg%"
if errorlevel 1 (
  echo (Nothing to commit. Attempting push anyway.)
)
git push origin main
if errorlevel 1 (
  echo.
  echo Push failed. Trying full LFS push, then retrying...
  git lfs push --all origin main
  call :check_error
  git push origin main
  call :check_error
)
echo.
echo Push complete.
pause
goto menu

:status
echo --------------------------------------------
echo Repo status
echo --------------------------------------------
git remote -v
git branch
echo.
git status
echo.
echo LFS tracked files:
git lfs ls-files
echo.
pause
goto menu
