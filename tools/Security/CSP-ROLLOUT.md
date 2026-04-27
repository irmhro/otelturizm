# CSP Enforcement Rollout (p75/p76)

Bu proje CSP için iki mod destekler:

- `Security:CspEnforce=false`: **Enforce değil**, `Content-Security-Policy` daha gevşek (script için `unsafe-inline` var). Ek olarak strict politika `Content-Security-Policy-Report-Only` ile raporlanır.
- `Security:CspEnforce=true`: **Enforce**, script için `unsafe-inline` yoktur; `script-src` sadece `self` + `nonce-...` + gerekli `https:` kaynakları ile çalışır.

## Raporlama (p71)

Varsayılan olarak CSP raporu aktiftir:

- `Security:CspReportEnabled=true`
- Endpoint: `POST /csp/report`
- Header: `Report-To: csp-endpoint` + `report-uri /csp/report; report-to csp-endpoint;`

## Önerilen kademeli geçiş

1. **Report-Only gözlem**: `Security:CspEnforce=false` iken prod loglarında `CSP_REPORT` kayıtlarını izle.
2. **Nonce coverage doğrula**: `tools/Security/Inventory-InlineScripts.ps1` ile `Nonce=False` kalmadığından emin ol.
3. **Enforce aç**: `Security:CspEnforce=true` yap, ilk etapta kısa süre izle.
4. **Kaynak kısıtlarını sıkılaştır**: İhtiyaç oldukça `script-src` / `connect-src` allowlist daralt (örn. sadece kullanılan CDN).

