@echo off
chcp 65001 >nul
title Otelturizm - UI Dev (Derlemesiz) https://localhost:7223
cd /d "%~dp0"

set "DLL=%LOCALAPPDATA%\OtelturizmBuildCache\bin\Debug\net10.0\otelturizm.dll"

echo.
echo  Otelturizm - TASARIM modu (CSS / CSHTML / JS)
echo  ----------------------------------------------
echo  URL : https://localhost:7223
echo  HTTP: http://localhost:5103
echo.
echo  CSS / JS / CSHTML  - rebuild YOK, tarayicida F5 veya Ctrl+F5
echo  C# (.cs) degisirse - bu pencereyi kapat, dev-watch.bat kullan
echo.
echo  Ilk kez calistiriyorsaniz veya .cs degistiyseniz otomatik build yapilir.
echo  Durdurmak icin: Ctrl+C
echo.

set ASPNETCORE_ENVIRONMENT=Development

if not exist "%DLL%" (
    echo [%date% %time%] Ilk derleme yapiliyor...
    dotnet build "%~dp0otelturizm.csproj" -v q
    if errorlevel 1 (
        echo.
        echo Build basarisiz. Hatalari duzeltip tekrar deneyin.
        pause
        exit /b 1
    )
    echo Derleme tamam.
    echo.
)

echo [%date% %time%] Uygulama baslatiliyor (--no-build)...
echo.

dotnet run --project "%~dp0otelturizm.csproj" --launch-profile https --no-build --no-restore
set EXITCODE=%ERRORLEVEL%

if %EXITCODE% NEQ 0 (
    echo.
    echo [%date% %time%] --no-build basarisiz; tam derleme deneniyor...
    dotnet build "%~dp0otelturizm.csproj" -v q
    if errorlevel 1 (
        echo Build basarisiz.
        pause
        exit /b 1
    )
    dotnet run --project "%~dp0otelturizm.csproj" --launch-profile https --no-build --no-restore
    set EXITCODE=%ERRORLEVEL%
)

echo.
echo UI dev durduruldu.
pause
exit /b %EXITCODE%
