@echo off
chcp 65001 >nul
title Otelturizm - Dev Watch (Kararli/Agent) https://localhost:7223
cd /d "%~dp0"

echo.
echo  Otelturizm - KARARLI mod (agent / toplu patch)
echo  -----------------------------------------------
echo  20 sn polling - dosya kilidi/race durumlarinda daha guvenli
echo  Hot Reload kapali - tam restart
echo  CSS/CSHTML icin yine F5 yeterli (RuntimeCompilation acik)
echo.

set DOTNET_USE_POLLING_FILE_WATCHER=1
set DOTNET_WATCH_HOT_RELOAD_ENABLED=0
set ASPNETCORE_ENVIRONMENT=Development

:watch_loop
echo [%date% %time%] dotnet watch (polling 20sn)...
dotnet watch run --project "%~dp0otelturizm.csproj" --launch-profile https --no-hot-reload /p:WatchPollingIntervalMs=20000
set EXITCODE=%ERRORLEVEL%

if %EXITCODE%==0 goto watch_done
if %EXITCODE%==-1073741510 goto watch_done
if %EXITCODE%==3221225786 goto watch_done

echo 8 sn sonra yeniden denenecek...
timeout /t 8 /nobreak >nul
goto watch_loop

:watch_done
pause
