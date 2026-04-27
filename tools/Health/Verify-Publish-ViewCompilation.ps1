param(
    [string]$Configuration = "Release",
    [string]$OutDir = "d:\otelturizm\artifacts\publish-verify",
    [switch]$Clean
)

$ErrorActionPreference = "Stop"

if ($Clean -and (Test-Path -LiteralPath $OutDir)) {
    Remove-Item -LiteralPath $OutDir -Recurse -Force
}

Write-Host "Publishing (Razor compile verification)..."
dotnet publish "d:\otelturizm\otelturizmnew.csproj" -c $Configuration -o $OutDir
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host ""
Write-Host "Publish OK. Output: $OutDir"

