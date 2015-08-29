@echo off
chcp 65001 > nul
setlocal enabledelayedexpansion
FOR /F "tokens=* USEBACKQ" %%F IN (`mergepathstrings.exe`) DO (
SET newpath=%%F
)
endlocal & SET PATH=%newpath%