# Panel Kontrast Audit (p100)

Hedef: Panel UI’da AA kontrast seviyesine yaklaşmak.

## Hızlı checklist

- Birincil metin rengi: `--ot-text` (#0f172a) açık zeminde yeterli.
- Muted metin rengi: `--ot-muted` bazı zeminlerde soluk olabilir:
  - Kritik bilgi/CTA’da muted kullanma.
  - Muted sadece yardımcı metin (hint) için.
- Primary buton: `--ot-primary` (#2563eb) + beyaz yazı yeterli.
- Warning butonlarda koyu metin kullan (mevcut).
- Error state’de metin: `#991b1b` gibi koyu kırmızı kullan (mevcut toast/alert).

## Uygulama notları

- Odak halkası: `panel-standards.css` içinde `--ot-focus` ile eklendi.
- Tablo başlıkları: uppercase/letter spacing ile okunabilirlik arttırıldı.

## Takip

Gerçek sayfalarda (Admin/Firma/Partner/Satış/User) kritik CTA ve uyarı mesajları gözle kontrol edilmeli.

