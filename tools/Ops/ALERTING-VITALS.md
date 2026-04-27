# Alerting: vitals ve 5xx (paket 239)

Önerilen sinyaller:

| Sinyal | Kaynak | Örnek eşik |
|---------|--------|------------|
| HTTP 5xx oranı | IIS/Kestrel log veya reverse proxy | 5 dk pencerede > %1 |
| Uygulama hataları | `App_Data/logs/app-*.json` (Serilog) | Dakikada > N `Error` |
| Web Vitals | İstemci `POST /rum/vitals` + Admin **Ticari içgörü** | LCP/INP ağırlıklı ortalama sapması (manuel veya harici dashboard) |

Altyapı bağımsız implementasyon: Application Insights / Grafana / Elastic — önemli olan **aynı correlation id** ile log birleştirmesi ve **sayfa/route kırılımı**.
