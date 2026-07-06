@echo off
REM ── Udaan headless model build (BACKUP path; primary is the live Blender MCP on 4.x) ──
REM Runs Blender in the background to build models, EXPORT them into Assets\Art\Models,
REM and RENDER preview PNGs into Udaan-Brain\blender-previews (which Claude can read).
REM
REM USE: just double-click. No Blender window opens; wait for "press any key".
REM Then tell Claude "models built" and it reads the log + previews.
REM
REM IMPORTANT: point BLENDER at a Blender 4.x LTS exe (NOT 5.x — the pipeline uses the
REM 4.x FBX exporter). Edit the line below if your install path differs.
REM ──────────────────────────────────────────────────────────────────────────────────

set "BLENDER="
for %%P in (
  "C:\Program Files\Blender Foundation\Blender 4.2\blender.exe"
  "C:\Program Files\Blender Foundation\Blender 4.3\blender.exe"
  "C:\Program Files\Blender Foundation\Blender 4.4\blender.exe"
  "C:\Program Files\Blender Foundation\Blender 4.5\blender.exe"
) do if exist %%P set "BLENDER=%%~P"

if "%BLENDER%"=="" (
  echo Could not find a Blender 4.x install in the usual place.
  echo Edit this .bat and set BLENDER to your Blender 4.x blender.exe path.
  pause
  exit /b 1
)

echo Using: %BLENDER%
"%BLENDER%" --background --python "%~dp0build_models.py" --log-level 0 > "%~dp0build_models.log" 2>&1

echo.
echo Done. Log: %~dp0build_models.log
echo Models -> udaan-client\Assets\Art\Models   Previews -> Udaan-Brain\blender-previews
echo Tell Claude "models built".
pause
