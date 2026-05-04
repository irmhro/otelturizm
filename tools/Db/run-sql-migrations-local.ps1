param(
  [string]$ProjectPath = "D:\otelturizm\otelturizmnew.csproj"
)

$ErrorActionPreference = "Stop"

Write-Host "SQL migrations (local) baslatiliyor..." -ForegroundColor Cyan

dotnet run --project "$ProjectPath" --no-build -- --run-sql-migrations

Write-Host "SQL migrations (local) tamamlandi." -ForegroundColor Green

