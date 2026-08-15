@echo off
setlocal

set OUT_SERVER=LanServerExe
set OUT_CLIENT=LanClientExe

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
    if errorlevel 1 (
        echo [ERROR] Could not clear %OUT_SERVER% - a file may still be in use.
        pause
        exit /b 1
    )
)
if exist %OUT_CLIENT% (
    rmdir /s /q %OUT_CLIENT%
    if errorlevel 1 (
        echo [ERROR] Could not clear %OUT_CLIENT% - a file may still be in use.
        pause
        exit /b 1
    )
)

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
if errorlevel 1 (
    echo.
    echo [ERROR] LanServer build failed.
    pause
    exit /b 1
)

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
if errorlevel 1 (
    echo.
    echo [ERROR] LanClient build failed.
    pause
    exit /b 1
)

echo.
echo ========================================
echo  Done!
echo  Server: %OUT_SERVER%\LanServer.exe
echo  Client: %OUT_CLIENT%\LanClient.exe
echo ========================================
pause
