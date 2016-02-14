@echo off
setlocal enabledelayedexpansion
FOR /F "tokens=* USEBACKQ" %%F IN (`MergePaths.exe`) DO (
SET newpath=%%F
)
endlocal & SET PATH=%newpath%