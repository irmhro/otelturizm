# Saatlik geliştirme snapshot — commit + GitHub push
# Job: AGENT_LOOP_HOURLY_git_sync (3600s)
param(
    [string]$RepoRoot = (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent),
    [switch]$DryRun
)

Set-Location $RepoRoot

function Write-SyncLog([string]$Message) {
    $ts = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    Write-Output "[$ts] GIT_SYNC $Message"
}

Write-SyncLog 'start'

# Untracked + modified (exclude junk)
git add -A -- . ':!.tmp.driveupload' ':!.coord-build' ':!.coord-build-*' ':!.build-*' ':!23.05.2026' 2>$null
if ($LASTEXITCODE -ne 0) {
    git add -A
}

$status = git status --porcelain
if ([string]::IsNullOrWhiteSpace($status)) {
    Write-SyncLog 'skip — no changes'
    exit 0
}

$hour = Get-Date -Format 'yyyy-MM-dd HH:mm'
$wave = 'orkestra-24h'
if (Test-Path (Join-Path $RepoRoot 'geliştrme-orkestra.md')) {
    $last = Select-String -Path (Join-Path $RepoRoot 'geliştrme-orkestra.md') -Pattern 'Wave-[A-Z0-9-]+' -AllMatches | Select-Object -Last 1
    if ($last) { $wave = $last.Matches[-1].Value }
}

$msg = @"
chore(orkestra): saatlik geliştirme snapshot $hour

24h sürekli geliştirme döngüsü — $wave
Otomatik: tools/Git/Invoke-HourlyGitSync.ps1
"@

if ($DryRun) {
    Write-SyncLog "dry-run commit:`n$msg"
    git status --short | Select-Object -First 20
    exit 0
}

git commit -m $msg
if ($LASTEXITCODE -ne 0) {
    Write-SyncLog 'commit failed'
    exit 1
}

git push origin HEAD
if ($LASTEXITCODE -ne 0) {
    Write-SyncLog 'push failed — commit local only'
    exit 2
}

Write-SyncLog 'push ok'
exit 0
