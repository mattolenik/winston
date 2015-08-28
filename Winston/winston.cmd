@echo off
winstonapp %*
IF %ERRORLEVEL% EQU 2 updatepath.cmd
IF %ERRORLEVEL% EQU 3 updatepath.cmd