@echo off
REM ── Udaan compile check ────────────────────────────────────────────────
REM Runs Unity headless to compile all scripts and writes the result to a log
REM INSIDE the project folder, so Claude (Cowork) can read the errors directly.
REM
REM USE: close the Unity Editor first (batchmode needs the project lock), then
REM double-click this file. When it finishes, tell Claude "compile done".
REM
REM If your Unity is installed elsewhere, edit UNITY below to match.
REM ───────────────────────────────────────────────────────────────────────

set "UNITY=C:\Program Files\Unity\Hub\Editor\6000.4.3f1\Editor\Unity.exe"
set "PROJECT=%~dp0udaan-client"
set "LOG=%~dp0compile.log"

echo Compiling %PROJECT% ...
echo (this opens Unity headless; it can take a minute on first run)

"%UNITY%" -batchmode -quit -projectPath "%PROJECT%" -logFile "%LOG%"

echo.
echo Done. Log written to: %LOG%
echo Tell Claude "compile done" and it will read the errors.
pause
