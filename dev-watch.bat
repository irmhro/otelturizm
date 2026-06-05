@echo off
chcp 65001 >nul
title Otelturizm - Dev Watch (Hizli) https://localhost:7223
cd /d "%~dp0"

echo.
echo  Otelturizm - HIZLI gelistirme modu
echo  -----------------------------------
echo  URL : https://localhost:7223
echo  HTTP: http://localhost:5103
echo.
echo  CSHTML / CSS / JS  - rebuild YOK, tarayicida F5 veya Ctrl+F5 yeterli
echo  C# (.cs)           - degisiklikte otomatik incremental derleme (~20-40 sn)
echo  Dosya izleme       - anlik (Windows native watcher)
echo.
echo  Sadece tasarim icin: dev-ui.bat  (baslangicta derleme atlanir, ~5 sn)
echo  Agent/toplu patch icin: dev-watch-stable.bat
echo  Durdurmak icin: Ctrl+C
echo.

set ASPNETCORE_ENVIRONMENT=Development
set DOTNET_WATCH_HOT_RELOAD_ENABLED=0
rem Polling KAPALI - anlik dosya algilama

set "DLL=%LOCALAPPDATA%\OtelturizmBuildCache\bin\Debug\net10.0\otelturizm.dll"
set "WATCH_EXTRA=--no-restore"
if exist "%DLL%" (
    set "WATCH_EXTRA=%WATCH_EXTRA% --no-build"
)

:watch_loop
echo.
echo [%date% %time%] dotnet watch baslatiliyor...
if exist "%DLL%" (
    echo  Mevcut derleme kullaniliyor - baslangic "Derleniyor..." atlandi.
) else (
    echo  Ilk calistirma - tam derleme ~1-2 dk surebilir.
)
echo.

dotnet watch run --project "%~dp0otelturizm.csproj" --launch-profile https --no-hot-reload %WATCH_EXTRA%
set EXITCODE=%ERRORLEVEL%

if %EXITCODE%==0 goto watch_done
if %EXITCODE%==-1073741510 goto watch_done
if %EXITCODE%==3221225786 goto watch_done

echo.
echo [%date% %time%] Watch durdu (kod: %EXITCODE%). 5 sn sonra yeniden denenecek...
timeout /t 5 /nobreak >nul
goto watch_loop

:watch_done
echo.
echo Dev watch durduruldu.
pause
