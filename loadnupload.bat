@echo off
echo ======================================
echo        Mau's Git Sync Launcher
echo ======================================
echo.
echo What do you want to do?
echo [1] Load latest code (git pull)
echo [2] Upload latest code (git add/commit/push)
echo.
set /p choice=Choose 1 or 2: 

if "%choice%"=="1" goto pull
if "%choice%"=="2" goto push

echo Invalid choice, bro. Try again.
goto end

:pull
echo --------------------------------------
echo Pulling latest changes from remote...
echo --------------------------------------
git pull origin main
echo.
echo Pull complete!
goto end

:push
echo --------------------------------------
echo Adding all changes...
echo --------------------------------------
git add .
echo.
set /p msg=Enter commit message: 

if "%msg%"=="" set msg=Updated assets
git commit -m "%msg%"
echo.
echo Pushing to remote...
git push origin main
echo.
echo Push complete!
goto end

:end
echo.
pause
