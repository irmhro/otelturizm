$base = 'http://localhost:5103'
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$loginPage = Invoke-WebRequest -Uri "$base/partner-giris" -WebSession $session -UseBasicParsing
$token = [regex]::Match($loginPage.Content, 'name="__RequestVerificationToken" type="hidden" value="([^"]+)"').Groups[1].Value
if (-not $token) { throw 'Antiforgery token bulunamadi.' }
$body = @{ __RequestVerificationToken = $token; partnerIdentity = '216silvertuzla@gmail.com'; partnerPassword = '1585'; rememberMe = 'true' }
$loginResponse = Invoke-WebRequest -Uri "$base/partner-giris" -Method Post -Body $body -WebSession $session -MaximumRedirection 0 -ErrorAction SilentlyContinue
if ($loginResponse.StatusCode -eq 302) {
  "LOGIN_REDIRECT|$($loginResponse.Headers.Location)"
} elseif ($loginResponse.Exception.Response.StatusCode.Value__ -eq 302) {
  "LOGIN_REDIRECT|$($loginResponse.Exception.Response.Headers['Location'])"
} else {
  "LOGIN_STATUS|$($loginResponse.StatusCode)"
}
$dashboard = Invoke-WebRequest -Uri "$base/panel/partner/dashboard" -WebSession $session -UseBasicParsing
"DASHBOARD|$($dashboard.StatusCode)|$([regex]::Match($dashboard.Content,'<h1>(.*?)</h1>').Groups[1].Value)"
$containsSilver = $dashboard.Content -like '*216 SILVER SUITE*'
"HAS_SILVER|$containsSilver"
$routes = @('/panel/partner/rezervasyonlar','/panel/partner/takvim-fiyatlar','/panel/partner/oda-yonetimi','/panel/partner/otel-bilgileri','/panel/partner/fotograflar','/panel/partner/performans','/panel/partner/degerlendirmeler','/panel/partner/finans','/panel/partner/tercihler','/panel/partner/724-destek')
foreach ($route in $routes) {
  try {
    $resp = Invoke-WebRequest -Uri ($base + $route) -WebSession $session -UseBasicParsing
    $title = [regex]::Match($resp.Content,'<h1>(.*?)</h1>').Groups[1].Value
    "PAGE|$route|$($resp.StatusCode)|$title"
  } catch {
    "PAGEERR|$route|$($_.Exception.Message)"
  }
}
