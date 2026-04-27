# Kişiselleştirme ve anonim olaylar (paket 229)

Bu projede growth ve RUM uçları:

- `POST /growth/events` — `GROWTH_EVENT` structured log + bellek içi özet (`CommerceMetricsAccumulator`).
- `POST /rum/vitals` — `RUM_VITALS` structured log + metrik özet.

**Öneriler (KVKK/GDPR uyumu):**

- Kişisel veri (ad, e-posta, telefon) göndermeyin; `meta` alanını kısa anonim kodlarla sınırlayın.
- IP adresini logda tutmak gerekiyorsa saklama süresi ve erişim rolleri için şirket içi politika tanımlayın.
- İstemci parmak izi çerezi (`Otelturizm.ClientFp`) yalnızca yüzdelik rollout için kullanılır; profiling veya üçüncü taraf paylaşımı yapılmaz.
