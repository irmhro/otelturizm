# YZ Çalışma Yönergeleri

**Agent grupları:** [AGENT_GRUPLARI_MASTER.md](AGENT_GRUPLARI_MASTER.md) — bağımsız gruplar, açık bağımlılıklar, charter: `docs/agent-gruplari/`.

Bu proje üzerinde çalışan YZ ajanları token ve zaman tüketimini azaltmak için aşağıdaki kurallara uymalıdır.

## Bağlam Kullanımı

- Sadece talep edilen geliştirme alanıyla ilgili dosyaları oku.
- Tüm projeyi, tüm `Views`, tüm `Services` veya tüm `wwwroot` klasörünü gerekmedikçe tarama.
- Önce dosya adı, route, controller, service veya view ipucuna göre dar arama yap.
- Aynı bilgiyi tekrar tekrar okumamak için bulduğun bağlamı kısa not olarak kullan.
- İlgisiz modül, eski tasarım, vendor, tema, backup, publish ve upload klasörlerini tarama.

## Log Okuma

- Log dosyalarını yalnızca hata, build kırığı, runtime exception veya kullanıcı açıkça istediğinde oku.
- Normal geliştirme sırasında `.log`, `.binlog`, `stdout`, `stderr`, publish logları ve geçici çalışma çıktıları okunmaz.
- Log okunacaksa önce son satırlar veya hata içeren satırlar okunur; tüm dosya baştan sona okunmaz.
- Büyük loglarda önce `Select-String`, `rg`, `tail` benzeri filtreleme yapılır.

## Arama Kuralları

- Metin aramada `rg`, dosya aramada `rg --files` tercih edilir.
- Aramalar klasörle sınırlandırılır. Örnek: sadece `Views/Paneller/Partner`, `Services/PartnerService.cs`, `wwwroot/assets/css/paneller/partner`.
- `bin`, `obj`, `.git`, `.vs`, `node_modules`, `wwwroot/vendor`, `wwwroot/paneltematabler`, `Database/Backups`, `publish`, `tmp`, `uploads` klasörleri açık gerekçe yoksa taranmaz.
- Görsel, tema veya upload görevi yoksa `wwwroot/uploads` okunmaz.
- Tabler/AdminLTE gibi vendor tema klasörleri sadece tema dosyası istenirse okunur.

## MCP Kullanımı

- Codex oturumunda `otelturizm-context` MCP server varsa önce `project_map`, `find_files`, `search_code`, `read_range` veya `read_method` araçlarıyla dar bağlam al.
- Büyük dosya gerekiyorsa tüm dosya yerine ilgili satır aralığı veya metot okunur.
- MCP dışlanan klasörleri okumuyorsa bunu token tasarruf kuralı kabul et; sadece kullanıcı açıkça isterse doğrudan dosya sisteminden bak.

## Değişiklik Yapma

- Mevcut dosyaları güncellerken sadece ilgili metot, blok veya satırları değiştir.
- **Panel otomatik kayıt:** Yeni panel form/seçim geliştirmelerinde [PANEL_OTOMATIK_KAYIT_SOZLESMESI.md](PANEL_OTOMATIK_KAYIT_SOZLESMESI.md) kurallarına uy.
- Sayfa geliştirmelerinde route, action/metot, `.cshtml`, `.css` ve varsa `.mobile.css` adları sayfa adı standardıyla eşleşmeli; örnek `favorilerim` için `Favorites.cshtml`/`favorites.css`, `rezervasyonlarim` için `Reservations.cshtml`/`reservations.css`.
- Alakasız düzenleme, formatlama ve toplu refactor yapma.
- Kullanıcının veya başka aracın yaptığı değişiklikleri geri alma.
- Manuel dosya düzenlemelerinde `apply_patch` kullan.
- Canlıya yükleme, GitHub push veya deploy işlemi sadece kullanıcı açıkça isterse yapılır.

## Veritabanı

- Canlı veritabanında işlem yapılacaksa önce yedek alınır.
- Tablo yapısı güncellenecekse mevcut canlı veriye zarar vermeyen `ALTER`/onarım yaklaşımı kullanılır.
- Veri silme, reset veya truncate işlemi kullanıcı açıkça istemedikçe yapılmaz.
- Migration üretirken güncel MSSQL şemasına uygun, tablo bazlı ve okunabilir SQL tercih edilir.

## Canlı DB ile Çalışma Protokolü (Zorunlu)

- Veritabanı şema/değer değişikliği **asla “elle canlıya girip düzeltme” şeklinde bırakılmaz**; mutlaka `Database/MigrationsSql` altında **idempotent** bir `.sql` dosyası oluşturulur.
- Her yeni DB değişikliği için:
  - **Şema**: `Database/MigrationsSql/tablo/migrationlar/` — `YYYYMMDD_*_sqlserver_*.sql` veya tablo snapshot (CREATE/ALTER).
  - **Seed / içerik**: `Database/MigrationsSql/veri/migrationlar/` — `YYYYMMDD_seed_*.sql` (INSERT/UPDATE idempotent, UTF-8 BOM).
  - **FK/index/trigger**: `Database/MigrationsSql/constraints/` (900–902).
- Canlıya uygulanmadan önce:
  - **Yedek zorunlu** (full backup veya eşdeğer güvenli yedek).
  - Uygulama sırası dokümante edilir (hangi scriptler, hangi sırayla).
- Bu repoda “kaynak gerçek” yaklaşımı geçerlidir:
  - Localde oluşturulan sözleşme/şablon içerikleri canlıya taşınacaksa **veri kopyası** yerine, içerik **migration seed** dosyasıyla taşınır.
  - Böylece canlı/stage/local aynı scriptlerle birebir eşitlenebilir.

## Build ve Test

- Kod değişikliğinden sonra mümkünse build al.
- Varsayılan build:
 `dotnet build "D:\otelturizm\otelturizm.csproj" --no-restore`
- Çalışan uygulama build çıktısını kilitliyorsa uygulamayı gereksiz yere kapatma; gerekirse ayrı geçici output klasörüyle doğrula.
- Build çıktısı kısa özetlenir; tüm log kullanıcıya dökülmez.

## İletişim

- Kullanıcıya kısa ve net ilerleme bilgisi ver.
- Büyük plan gerekiyorsa sadece ilgili adımları yaz.
- Yapılmayan işlemleri açık söyle: örneğin canlıya yükleme yapılmadı, GitHub'a push yapılmadı.
- Gereksiz teknik ayrıntı ve uzun dosya listesi verme; sadece karar ve sonuç için gerekli olanları aktar.
