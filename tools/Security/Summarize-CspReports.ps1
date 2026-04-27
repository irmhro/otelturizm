param(
    [string]$LogRoot = "d:\otelturizm\App_Data",
    [string]$OutFile = (Join-Path $PSScriptRoot "csp-report-summary.txt"),
    [int]$Top = 20
)

$ErrorActionPreference = "Stop"

$files = Get-ChildItem -LiteralPath $LogRoot -Recurse -File -Include *.log,*.txt -ErrorAction SilentlyContinue
if (-not $files -or $files.Count -eq 0) {
    Write-Host "No log files found under: $LogRoot"
    exit 0
}

$counts = @{}

foreach ($f in $files) {
    try {
        $lines = Get-Content -LiteralPath $f.FullName -ErrorAction Stop
        foreach ($line in $lines) {
            if ($line -notmatch "CSP_REPORT") { continue }

            # Basit normalizasyon: raporun ana kısmını yakala (body=...)
            $body = ""
            if ($line -match "body=(.+)$") {
                $body = $Matches[1]
            }

            $key = $body
            if ([string]::IsNullOrWhiteSpace($key)) { $key = "(no-body)" }
            if ($key.Length -gt 600) { $key = $key.Substring(0, 600) }

            if (-not $counts.ContainsKey($key)) { $counts[$key] = 0 }
            $counts[$key] = $counts[$key] + 1
        }
    } catch { }
}

$items = $counts.GetEnumerator() | Sort-Object -Property Value -Descending | Select-Object -First $Top
$out = New-Object System.Collections.Generic.List[string]
$out.Add("# CSP report summary")
$out.Add("# Generated at: $(Get-Date -Format o)")
$out.Add("")

foreach ($it in $items) {
    $out.Add(("count={0} body={1}" -f $it.Value, $it.Key))
    $out.Add("")
}

$out | Set-Content -LiteralPath $OutFile -Encoding UTF8
Write-Host "Wrote: $OutFile"
Write-Host "Unique bodies: $($counts.Count)"

