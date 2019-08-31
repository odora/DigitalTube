@echo off

set CURDIR=%~dp0
set Prog1="%CURDIR%bin\Release\DigitalTube.exe"
set IMG1=%CURDIR%TestImages\624.jpg
set IMG2=%CURDIR%TestImages\1873.jpg
set IMG3=%CURDIR%TestImages\2496.jpg
set IMG4=%CURDIR%TestImages\11606.jpg

%Prog1% %IMG1% 1
echo.
%Prog1% %IMG2% 1
echo.
%Prog1% %IMG3% 1
echo.
%Prog1% %IMG4% 1
echo.

@PAUSE