Kodlama Sözleşmesi — UI & Yorum Kılavuzu

Amaç
- Kullanıcı-odaklı, yalın ve sürdürülebilir kod. Arayüzler gerçek kullanıcıların (mobil ve masaüstü) beklisini karşılayacak şekilde tasarlanır.

Genel İlkeler
- "Kullanıcı gibi düşün": Her arayüz değişikliğinde önce basit bir kullanıcı senaryosu düşünün (ne görmek isterim?, en önemli bilgi nedir?).
- Yorumlar kısa ve hedefe odaklı olsun: maksimum 1–2 satır; neden (neden bu yapıldı), altta yatan karmaşıklık önemliyse dış belgede detaylandır.
- Kodda büyük açıklama bloklarından kaçının; bunlar sürüm kontrolündeki commit mesajlarına veya proje dokümanına taşınsın.

UI / Tasarım Kuralları
- Bilgi hiyerarşisi: başlık, kısa açıklama/konum, etiketler/öncelikler, görsel (thumbnail), metrikler (puan/yorum), fiyat, CTA.
- Kartlar kompakt olmalı: gereksiz büyük boşluk, aşırı gölge veya büyük tipografik sapmalar olmasın.
- Mobil öncelikli: mobil görünümü düşünerek grid/stack tasarımları kullanın.
- Erişilebilirlik: semantic HTML, alt metin, label, kontrast ve klavye erişilebilirliği zorunlu.

Bileşenler ve CSS
- Tek sorumluluk: bileşenler küçük ve yeniden kullanılabilir olsun.
- Tasarım token'ları: renk, radius, spacing gibi sabitler `:root` değişkenlerinde tutulsun.
- CSS yorumları kısa ve bölüm başlıkları şeklinde olsun (// veya /* Section */). Büyük açıklamalar kodun içine yazılmasın.

İnceleme & Rollere Göre Onay
- Değişiklikler küçük PR'larda sunulsun.
- PR incelemesi için en az 2 onay: frontend (UI/uygulama), UX veya erişilebilirlik reviewer.
- Kritik/özgün tasarım değişimlerinde Product Owner, UX uzmanı ve 1 backend reviewer dahil edilmelidir.

Commit Mesajları
- Kısa özet (max 50 karakter) + boş satır + detay (opsiyonel) + issue id.

Test & Ölçüm
- Görsel değişiklikler için gözlem (A/B veya user feedback) önerilir.
- UI hatalarında responsive test (320–1440px) yapılmalı.

Uygulama Örneği — Otel Kartı (özet)
- thumbnail | badge (küçük) | favori ikonu
- başlık (1 satır), konum (metin)
- etiketler (küçük pill'ler)
- özellikler (ikon+etiket satırı)
- rating | fiyat | CTA

Bu sözleşmeye uymayan büyük değişiklikler yapılmasın; değişiklik teklif edilecekse önce PR açıklamasında kullanıcı senaryosu ve hangi rollerin inceleyeceği belirtilsin.
