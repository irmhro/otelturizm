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
dotnet publish "d:\otelturizm\otelturizm.csproj" -c $Configuration -o $OutDir
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$requiredFiles = @(
    "otelturizm.dll",
    "Views\Email\tr\RezervasyonOnaylandi.cshtml",
    "Views\Email\tr\Rezervasyon Talebi Alindi.cshtml",
    "Views\Email\tr\Partner Yeni Rezervasyon.cshtml",
    "Views\Email\tr\Rezervasyon Reddedildi.cshtml",
    "Views\Paneller\Partner\Dashboard.cshtml",
    "Views\Paneller\Partner\_PartnerSidebar.cshtml",
    "wwwroot\assets\css\paneller\partner\dashboard.css",
    "wwwroot\assets\css\paneller\partner\shell.css"
)

$missing = @()
foreach ($file in $requiredFiles) {
    $fullPath = Join-Path $OutDir $file
    if (-not (Test-Path -LiteralPath $fullPath)) {
        $missing += $file
    }
}

if ($missing.Count -gt 0) {
    Write-Host ""
    Write-Error ("Publish eksik dosya uretti:`n - " + ($missing -join "`n - "))
    exit 2
}

Write-Host ""
Write-Host "Publish OK. Output: $OutDir"
Write-Host "Required runtime cshtml/css files OK."

