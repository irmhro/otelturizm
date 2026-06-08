# Sync third-party assets into wwwroot/assets/vendor for offline/local use.
$ErrorActionPreference = 'Stop'
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$vendor = Join-Path $root 'wwwroot\assets\vendor'

function Save-Url($url, $dest) {
    $dir = Split-Path $dest -Parent
    if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
    Write-Host "GET $url"
    Invoke-WebRequest -Uri $url -OutFile $dest -UseBasicParsing
}

# Plus Jakarta Sans (latin, weights used on site)
$pjsDir = Join-Path $vendor 'fonts\plus-jakarta-sans'
$weights = @(400, 500, 600, 700, 800)
$cssLines = @('/* Plus Jakarta Sans — local vendor */')
foreach ($w in $weights) {
    $file = "plus-jakarta-sans-latin-$w-normal.woff2"
    $url = "https://cdn.jsdelivr.net/npm/@fontsource/plus-jakarta-sans@5.2.5/files/$file"
    Save-Url $url (Join-Path $pjsDir $file)
    $cssLines += @(
        "@font-face {",
        "  font-family: 'Plus Jakarta Sans';",
        "  font-style: normal;",
        "  font-weight: $w;",
        "  font-display: swap;",
        "  src: url('./$file') format('woff2');",
        "  unicode-range: U+0000-00FF, U+0131, U+0152-0153, U+02BB-02BC, U+02C6, U+02DA, U+02DC, U+0304, U+0308, U+0329, U+2000-206F, U+20AC, U+2122, U+2191, U+2193, U+2212, U+2215, U+FEFF, U+FFFD;",
        "}",
        ""
    )
}
Set-Content -Path (Join-Path $pjsDir 'plus-jakarta-local.css') -Value ($cssLines -join "`n") -Encoding UTF8

# Leaflet 1.9.4
$leafletDir = Join-Path $vendor 'leaflet'
Save-Url 'https://unpkg.com/leaflet@1.9.4/dist/leaflet.css' (Join-Path $leafletDir 'leaflet.css')
Save-Url 'https://unpkg.com/leaflet@1.9.4/dist/leaflet.js' (Join-Path $leafletDir 'leaflet.js')
Save-Url 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png' (Join-Path $leafletDir 'images\marker-icon.png')
Save-Url 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png' (Join-Path $leafletDir 'images\marker-icon-2x.png')
Save-Url 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png' (Join-Path $leafletDir 'images\marker-shadow.png')
Save-Url 'https://unpkg.com/leaflet@1.9.4/dist/images/layers.png' (Join-Path $leafletDir 'images\layers.png')
Save-Url 'https://unpkg.com/leaflet@1.9.4/dist/images/layers-2x.png' (Join-Path $leafletDir 'images\layers-2x.png')
$leafletCss = Get-Content (Join-Path $leafletDir 'leaflet.css') -Raw
$leafletCss = $leafletCss -replace 'url\(([^)]*?images/)', "url('$1"
Set-Content -Path (Join-Path $leafletDir 'leaflet.css') -Value $leafletCss -Encoding UTF8 -NoNewline

# Leaflet markercluster 1.5.3
$clusterDir = Join-Path $vendor 'leaflet-markercluster'
Save-Url 'https://unpkg.com/leaflet.markercluster@1.5.3/dist/MarkerCluster.css' (Join-Path $clusterDir 'MarkerCluster.css')
Save-Url 'https://unpkg.com/leaflet.markercluster@1.5.3/dist/MarkerCluster.Default.css' (Join-Path $clusterDir 'MarkerCluster.Default.css')
Save-Url 'https://unpkg.com/leaflet.markercluster@1.5.3/dist/leaflet.markercluster.js' (Join-Path $clusterDir 'leaflet.markercluster.js')

# TinyMCE OSS 7.6 (community; self-hosted)
$tinyDir = Join-Path $vendor 'tinymce'
Save-Url 'https://cdn.jsdelivr.net/npm/tinymce@7.6.1/tinymce.min.js' (Join-Path $tinyDir 'tinymce.min.js')
$tinyFolders = @('icons', 'models', 'plugins', 'skins', 'themes')
foreach ($folder in $tinyFolders) {
    $indexUrl = "https://cdn.jsdelivr.net/npm/tinymce@7.6.1/$folder/"
    try {
        $html = (Invoke-WebRequest -Uri $indexUrl -UseBasicParsing).Content
        $matches = [regex]::Matches($html, 'href="([^"?]+\/)?"')
        $subdirs = $matches | ForEach-Object { $_.Groups[1].Value } | Where-Object { $_ -and $_ -ne '../' } | Select-Object -Unique
        if ($subdirs.Count -eq 0) {
            $fileMatches = [regex]::Matches($html, 'href="([^"?]+\.(js|css))"')
            foreach ($m in $fileMatches) {
                $rel = $m.Groups[1].Value
                Save-Url "$indexUrl$rel" (Join-Path $tinyDir "$folder\$rel")
            }
        }
        else {
            foreach ($sub in $subdirs) {
                $subUrl = "$indexUrl$sub"
                $subHtml = (Invoke-WebRequest -Uri $subUrl -UseBasicParsing).Content
                $fileMatches = [regex]::Matches($subHtml, 'href="([^"?]+\.(js|css|woff|woff2|svg|gif|png))"')
                foreach ($m in $fileMatches) {
                    $rel = ($sub.TrimEnd('/') + '/' + $m.Groups[1].Value).Replace('//', '/')
                    Save-Url "$indexUrl$rel" (Join-Path $tinyDir "$folder\$($rel.Replace('/', '\'))")
                }
            }
        }
    }
    catch {
        Write-Warning "TinyMCE folder sync skipped for $folder : $_"
    }
}

# Known TinyMCE paths (jsdelivr listing is limited — fetch required bundles explicitly)
$tinyFiles = @(
    'icons/default/icons.min.js',
    'models/dom/model.min.js',
    'themes/silver/theme.min.js',
    'skins/ui/oxide/skin.min.css',
    'skins/ui/oxide/content.min.css',
    'skins/ui/oxide/content.inline.min.css',
    'skins/content/default/content.min.css',
    'plugins/anchor/plugin.min.js',
    'plugins/autolink/plugin.min.js',
    'plugins/charmap/plugin.min.js',
    'plugins/codesample/plugin.min.js',
    'plugins/emoticons/plugin.min.js',
    'plugins/emoticons/js/emojis.min.js',
    'plugins/link/plugin.min.js',
    'plugins/lists/plugin.min.js',
    'plugins/media/plugin.min.js',
    'plugins/searchreplace/plugin.min.js',
    'plugins/table/plugin.min.js',
    'plugins/visualblocks/plugin.min.js',
    'plugins/wordcount/plugin.min.js',
    'plugins/code/plugin.min.js',
    'plugins/fullscreen/plugin.min.js',
    'plugins/help/plugin.min.js',
    'plugins/image/plugin.min.js',
    'plugins/preview/plugin.min.js'
)
foreach ($rel in $tinyFiles) {
    $dest = Join-Path $tinyDir ($rel -replace '/', '\')
    if (-not (Test-Path $dest)) {
        Save-Url "https://cdn.jsdelivr.net/npm/tinymce@7.6.1/$rel" $dest
    }
}

Write-Host "Done. Vendor root: $vendor"
