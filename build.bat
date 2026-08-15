@echo off
setlocal

set OUT_SERVER=LanServerExe
set OUT_CLIENT=LanClientExe

echo ========================================
echo  Building LanServer...
echo ========================================
dotnet publish LanServer\LanServer.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o %OUT_SERVER%
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
dotnet publish LanClient\LanClient.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o %OUT_CLIENT%
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
