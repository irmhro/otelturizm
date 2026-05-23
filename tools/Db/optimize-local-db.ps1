param(
  [string]$ProjectPath = "D:\otelturizm\otelturizm.csproj"
)

$ErrorActionPreference = "Stop"

Write-Host "Local DB optimize (stats + index reorganize)..." -ForegroundColor Cyan
dotnet run --no-launch-profile --project "$ProjectPath" --no-build -- --optimize-local-db
Write-Host "Tamamlandi." -ForegroundColor Green

