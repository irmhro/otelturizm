[System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }
$base = 'https://localhost:7223'
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$common = @{ UseBasicParsing = $true; WebSession = $session }
$loginPage = Invoke-WebRequest -Uri "$base/partner-giris" @common
$token = [regex]::Match($loginPage.Content, 'name="__RequestVerificationToken" type="hidden" value="([^"]+)"').Groups[1].Value
if (-not $token) { throw 'Antiforgery token bulunamadi.' }
$body = @{ __RequestVerificationToken = $token; partnerIdentity = '216silvertuzla@gmail.com'; partnerPassword = '1585'; rememberMe = 'true' }
try {
  $loginResponse = Invoke-WebRequest -Uri "$base/partner-giris" -Method Post -Body $body @common -MaximumRedirection 0 -ErrorAction Stop
  "LOGIN_STATUS|$($loginResponse.StatusCode)"
} catch {
  if ($_.Exception.Response -and $_.Exception.Response.StatusCode.Value__ -in 302,303,307) {
    "LOGIN_REDIRECT|$($_.Exception.Response.StatusCode.Value__)|$($_.Exception.Response.Headers['Location'])"
  } else {
    throw
  }
}
$dashboard = Invoke-WebRequest -Uri "$base/panel/partner/dashboard" @common
"DASHBOARD|$($dashboard.StatusCode)|$([regex]::Match($dashboard.Content,'<h1>(.*?)</h1>').Groups[1].Value)"
$containsSilver = $dashboard.Content -like '*216 SILVER SUITE*'
"HAS_SILVER|$containsSilver"
$routes = @('/panel/partner/rezervasyonlar','/panel/partner/takvim-fiyatlar','/panel/partner/oda-yonetimi','/panel/partner/otel-bilgileri','/panel/partner/fotograflar','/panel/partner/performans','/panel/partner/degerlendirmeler','/panel/partner/finans','/panel/partner/tercihler','/panel/partner/724-destek')
foreach ($route in $routes) {
  try {
    $resp = Invoke-WebRequest -Uri ($base + $route) @common
    $title = [regex]::Match($resp.Content,'<h1>(.*?)</h1>').Groups[1].Value
    $hasHotel = $resp.Content -like '*216 SILVER SUITE*'
    "PAGE|$route|$($resp.StatusCode)|$title|$hasHotel"
  } catch {
    "PAGEERR|$route|$($_.Exception.Message)"
  }
}
