@echo off
chcp 65001 > nul
winstonapp %*
IF %ERRORLEVEL% EQU 2 updatepath.cmd
IF %ERRORLEVEL% EQU 3 updatepath.cmd