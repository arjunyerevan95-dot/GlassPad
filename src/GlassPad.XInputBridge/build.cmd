@echo off
setlocal
call "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\VC\Auxiliary\Build\vcvars64.bat" >nul
if errorlevel 1 exit /b 1
if not exist "%~dp0bin" mkdir "%~dp0bin"
cl /nologo /LD /MT /O2 /W4 /EHsc /D_WIN32_WINNT=0x0A00 "%~dp0bridge.cpp" /Fo"%~dp0bin\bridge.obj" /link /DEF:"%~dp0bridge.def" /OUT:"%~dp0bin\xinput1_3.dll" kernel32.lib
exit /b %errorlevel%
