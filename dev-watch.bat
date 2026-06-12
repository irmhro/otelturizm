@echo off
chcp 65001 >nul
setlocal EnableExtensions
title Otelturizm - Dev Watch (Hizli) https://localhost:7223
cd /d "%~dp0"

rem --- Maksimum hiz: MSBuild/Roslyn sunucu + tum cekirdek ---
set OTEL_DEV_FAST_BUILD=1
set DOTNET_CLI_USE_MSBUILD_SERVER=1
set MSBUILDUSESERVER=1
set DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
set DOTNET_NOLOGO=1
set DOTNET_WATCH_SUPPRESS_EMOJIS=1
set NUGET_XMLDOC_MODE=skip
set ASPNETCORE_ENVIRONMENT=Development
set DOTNET_WATCH_HOT_RELOAD_ENABLED=0
rem Polling KAPALI - anlik dosya algilama (dev-watch-stable.bat polling kullanir)

if not defined NUMBER_OF_PROCESSORS set NUMBER_OF_PROCESSORS=4
set OTEL_DEV_CPU=%NUMBER_OF_PROCESSORS%

set "PROJECT=%~dp0otelturizm.csproj"
set "DLL=%LOCALAPPDATA%\OtelturizmBuildCache\bin\Debug\net10.0\otelturizm.dll"
set "MSB_FAST=/m /p:MaxCpuCount=0 /p:BuildInParallel=true /p:UseSharedCompilation=true"

echo.
echo  Otelturizm - HIZLI gelistirme modu
echo  -----------------------------------
echo  URL : https://localhost:7223
echo  HTTP: http://localhost:5103
echo.
echo  Derleme: paralel MSBuild + Roslyn (max %OTEL_DEV_CPU% cekirdek)
echo  CSHTML / CSS / JS  - rebuild YOK, tarayicida F5 veya Ctrl+F5 yeterli
echo  C# (.cs)           - degisiklikte incremental derleme (paralel)
echo  Dosya izleme       - anlik (Windows native watcher)
echo.
echo  Sadece tasarim icin: dev-ui.bat  (baslangicta derleme atlanir, ~5 sn)
echo  Agent/toplu patch icin: dev-watch-stable.bat
echo  Durdurmak icin: Ctrl+C
echo.

set "WATCH_FLAGS=--no-restore -v minimal"
if exist "%DLL%" (
    set "WATCH_FLAGS=%WATCH_FLAGS% --no-build"
)

if not exist "%DLL%" (
    echo [%date% %time%] Ilk derleme baslatiliyor ^(%OTEL_DEV_CPU% cekirdek, paralel MSBuild^)...
    dotnet build "%PROJECT%" %MSB_FAST% -v minimal
    if errorlevel 1 (
        echo.
        echo Ilk derleme basarisiz. Hatalari duzeltip tekrar deneyin.
        pause
        exit /b 1
    )
    echo Ilk derleme tamam.
    echo.
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

dotnet watch run --project "%PROJECT%" --launch-profile https --no-hot-reload %WATCH_FLAGS% %MSB_FAST%
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
