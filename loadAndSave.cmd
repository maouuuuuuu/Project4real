@echo off
cd /d "%~dp0"
title Git Sync Portal - Mau Edition

:menu
cls
echo ============================================
echo         GIT SYNC PORTAL - MAU EDITION
echo ============================================
echo.
echo  [1] Pull (Load latest version from GitHub)
echo  [2] Push (Save current work to GitHub)
echo  [3] Exit
echo.
set /p choice=Enter your choice: 

if "%choice%"=="1" goto pull
if "%choice%"=="2" goto push
if "%choice%"=="3" exit
goto menu

:pull
echo --------------------------------------------
echo Pulling latest version from GitHub...
echo --------------------------------------------
git pull
echo.
echo ✅ Pull complete!
pause
goto menu

:push
echo --------------------------------------------
echo Pushing your work to GitHub...
echo --------------------------------------------
git add .
set /p msg=Enter commit message (e.g. "Updated UI scripts"): 
if "%msg%"=="" set msg=Quick update
git commit -m "%msg%"
git push
echo.
echo ✅ Push complete!
pause
goto menu
