@echo off
setlocal enabledelayedexpansion
FOR /F "tokens=* USEBACKQ" %%F IN (`mergepathstrings.exe`) DO (
SET newpath=%%F
)
endlocal & SET PATH=%newpath%