@echo off
setlocal EnableDelayedExpansion

echo Auditing tracked vs. actual files...
for /f "delims=" %%A in ('git rev-parse --show-toplevel') do set REPO=%%A
cd /d "%REPO%"

:: list tracked files in current commit
git ls-tree -r --name-only HEAD > _tracked.txt

:: list all files actually on disk (relative paths)
powershell -NoProfile -Command ^
  "$p=(Get-Location).Path; Get-ChildItem -Recurse -File |" ^
  " %{$_.FullName.Replace($p+'\','')} |" ^
  " Where-Object {$_ -notmatch '^Library\\|^Temp\\|^Logs\\|^Build\\|^Obj\\|^UserSettings\\'} |" ^
  " Set-Content _allfiles.txt"

:: files present on disk but NOT tracked (should be added)
powershell -NoProfile -Command ^
  "Compare-Object (Get-Content _tracked.txt) (Get-Content _allfiles.txt) -PassThru |" ^
  " ?{$_.SideIndicator -eq '=>'} | Set-Content _untracked_candidates.txt"

:: files tracked but NOT present on disk (deleted/never pulled)
powershell -NoProfile -Command ^
  "Compare-Object (Get-Content _tracked.txt) (Get-Content _allfiles.txt) -PassThru |" ^
  " ?{$_.SideIndicator -eq '<='} | Set-Content _missing_on_disk.txt"

echo.
echo ===== Possibly important files NOT tracked (add these?) =====
type _untracked_candidates.txt
echo.
echo ===== Files tracked in HEAD but missing on disk =====
type _missing_on_disk.txt
echo.
echo Done. Check the two lists above.
pause
