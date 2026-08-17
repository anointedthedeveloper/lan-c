@echo off
setlocal

set OUT_SERVER=LanServerExe
set OUT_CLIENT=LanClientExe
set DIST=dist

echo ========================================
echo  Stopping running instances...
echo ========================================
taskkill /f /im LanServer.exe >nul 2>&1
taskkill /f /im LanClient.exe >nul 2>&1
timeout /t 1 /nobreak >nul

echo.
echo ========================================
echo  Clearing output folders...
echo ========================================
if exist %OUT_SERVER% (
    rmdir /s /q %OUT_SERVER%
    if errorlevel 1 ( echo [ERROR] Could not clear %OUT_SERVER% & pause & exit /b 1 )
)
if exist %OUT_CLIENT% (
    rmdir /s /q %OUT_CLIENT%
    if errorlevel 1 ( echo [ERROR] Could not clear %OUT_CLIENT% & pause & exit /b 1 )
)
if exist %DIST% (
    rmdir /s /q %DIST%
    if errorlevel 1 ( echo [ERROR] Could not clear %DIST% - a file may still be in use. & pause & exit /b 1 )
)
mkdir %DIST%

echo.
echo ========================================
echo  Building LanServer...
echo ========================================
dotnet publish LanServer\LanServer.csproj ^
    -c Release ^
    -r win-x64 ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:EnableCompressionInSingleFile=true ^
    -o %OUT_SERVER%
if errorlevel 1 ( echo [ERROR] LanServer build failed. & pause & exit /b 1 )

echo.
echo ========================================
echo  Building LanClient...
echo ========================================
dotnet publish LanClient\LanClient.csproj ^
    -c Release ^
    -r win-x64 ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:EnableCompressionInSingleFile=true ^
    -o %OUT_CLIENT%
if errorlevel 1 ( echo [ERROR] LanClient build failed. & pause & exit /b 1 )

:: Write client version file next to server exe (used by ManageAppPage)
echo 1.0.0> %OUT_SERVER%\client_version.txt

echo.
echo ========================================
echo  Building Inno Setup Installers...
echo ========================================
set ISCC="C:\Program Files\Inno Setup 7\ISCC.exe"
if not exist %ISCC% set ISCC="C:\Program Files (x86)\Inno Setup 7\ISCC.exe"
if not exist %ISCC% set ISCC="C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if not exist %ISCC% set ISCC="C:\Program Files\Inno Setup 6\ISCC.exe"

if exist %ISCC% (
    :: Copy app icons from IconGen output into installers folder so ISCC can find them
    :: (reads from IconGen build output — Assets originals are never touched)
    copy /y IconGen\bin\Debug\net10.0-windows\client.ico installers\client.ico >nul
    copy /y IconGen\bin\Debug\net10.0-windows\server.ico  installers\server.ico >nul

    %ISCC% installers\LanClient_Setup.iss
    if errorlevel 1 ( echo [WARNING] LanClient installer build failed. )
    %ISCC% installers\LanClient_Silent.iss
    if errorlevel 1 ( echo [WARNING] LanClient silent installer build failed. )
    %ISCC% installers\LanServer_Setup.iss
    if errorlevel 1 ( echo [WARNING] LanServer installer build failed. )
    echo Installers output to: %DIST%\
) else (
    echo [WARNING] Inno Setup not found - skipping installer build.
    echo           Install from https://jrsoftware.org/isinfo.php
)

echo.
echo ========================================
echo  Done!
echo  Server EXE      : %OUT_SERVER%\LanServer.exe
echo  Client EXE      : %OUT_CLIENT%\LanClient.exe
echo  Installers      : %DIST%\
echo  Silent Installer: %DIST%\LanClient_AutoInstall.exe
echo ========================================
pause
