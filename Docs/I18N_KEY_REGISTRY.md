# I18N Key Registry — SharedResources (Faz 1–2)

**Kaynak:** `Resources/SharedResources.resx` (varsayılan `tr-TR`)  
**Kültür dosyaları:** `SharedResources.en-US.resx`, `SharedResources.de-DE.resx`, `SharedResources.fr.resx`, `SharedResources.es.resx`, `SharedResources.ru.resx`  
**Kayıt:** `AddLocalization()` — `ResourcesPath` yok; embedded ad `otelturizmnew.Resources.SharedResources`  
**Varsayılan:** `tr-TR` LTR; **Arapça kaldırıldı** (#075)

| Key | TR (default) | EN | DE | FR | ES |
|-----|--------------|----|----|----|-----|
| Nav.Hotels | Oteller | Hotels | Hotels | Hôtels | Hoteles |
| Nav.Corporate | Kurumsal | Corporate | Unternehmen | Entreprise | Empresa |
| Nav.Home | Anasayfa | Home | Startseite | Accueil | Inicio |
| Nav.Campaigns | Kampanyalar | Campaigns | Aktionen | Promotions | Campañas |
| Nav.Help | Yardım | Help | Hilfe | Aide | Ayuda |
| Nav.Login | Giriş Yap | Sign in | Anmelden | Connexion | Iniciar sesión |
| Nav.Register | Kayıt Ol | Register | Registrieren | S'inscrire | Registrarse |
| Nav.Account | Hesabım | My account | Mein Konto | Mon compte | Mi cuenta |
| Nav.Map | Harita | Map | Karte | Carte | Mapa |
| Nav.SkipToContent | İçeriğe atla | Skip to content | Zum Inhalt springen | Aller au contenu | Ir al contenido |
| Search.* / Booking.* / Filter.* / Btn.* / Footer.* | (49 keys — see `.resx` files) | | | | |

**Toplam:** 49 anahtar (Faz 1) + **42 Wave-XVIII** = **91 anahtar**

## Wave-XVIII (H15_fe_world_standard)

| Key grubu | Dosyalar | Not |
|-----------|----------|-----|
| `Listing.*` | `OtelListeleme.cshtml` | Hero search summary, concept bar, kart CTA |
| `Campaign.*` | `Kampanyalar/Index.cshtml` | Hero timer, stats, empty state |
| `Detail.ReviewTeaser.*` | `OtelDetay.cshtml` | Yorum teaser bloğu |
| `Nav.QuickLink.*`, `Nav.Panel.*`, `Nav.Featured*` | `_AnasayfaHeader.cshtml` | Nav pills + drawer |
| `Footer.*` (ek) | `_AnasayfaFooter.cshtml` | Açıklama, otelci, destek |
| `Btn.ViewDetails`, `Booking.MobileSummary` | Liste + detay | CTA / sticky bar |

Yeni anahtarlar: `Listing.SearchFound`, `Listing.EditSearch`, `Listing.MapView`, `Listing.ConceptTitle`, `Listing.AllCampaigns`, `Listing.LoyaltyBadge`, `Listing.RatingExcellent`, `Listing.RatingGood`, `Btn.ViewDetails`, `Nav.QuickLink.*` (4), `Nav.FeaturedLinks`, `Nav.FeaturedDeals`, `Nav.LanguageSelection`, `Nav.Panel.*` (5), `Footer.Description`, `Footer.Career`, `Footer.Press`, `Footer.Blog`, `Footer.ForHotels`, `Footer.ExtranetLogin`, `Footer.PartnerProgram`, `Footer.CommissionRates`, `Footer.TrainingSupport`, `Footer.CancelRefund`, `Footer.Faq`, `Footer.TrustCommitments`, `Campaign.Hero.*`, `Campaign.Stat.*`, `Campaign.Timer.*`, `Campaign.Empty.*`, `Campaign.ListHotels`, `Campaign.ViewDetail`, `Detail.ReviewTeaser.*`, `Booking.MobileSummary`.

## Faz 2 kablolama (Wave-XI / H13)

| Dosya | Anahtarlar |
|-------|------------|
| `Views/Shared/_Layout.cshtml` | `Nav.SkipToContent` |
| `Views/Anasayfa/_AnasayfaHeader.cshtml` | Nav, Search, Btn; `InternationalSeoPaths` listing URL |
| `Views/Anasayfa/_AnasayfaFooter.cshtml` | Footer.*, Nav.Hotels, Nav.Help, Nav.Campaigns |
| `Views/Shared/_PublicHeaderUserActions.cshtml` | Nav.Hotels, Nav.Login, Nav.Register, Nav.Corporate, Nav.Account |
| `Views/Shared/yanbar.cshtml` | `Btn.Close` |
| `Infrastructure/RoutePrefixRequestCultureProvider.cs` | `/fr/hotels`, `/es/hoteles` → `fr-FR`, `es-ES` |
| `Controllers/Oteller/OtellerController.cs` | `[Route("fr/hotels")]`, `[Route("es/hoteles")]` |

## Sonraki fazlar

- Rezervasyon akışı ve filtre chip tam kablolama
- Footer keşif / otelci metinleri için ek anahtarlar

## #075 — Arapça kaldırma + i18n düzeltme (2026-05-25)

| Değişiklik | Dosya |
|------------|-------|
| `AddLocalization()` (ResourcesPath kaldırıldı — raw key kök nedeni) | `Program.cs` |
| `/ar/*` → TR canonical 301 | `Program.cs` middleware |
| `ar` route/prefix/hreflang kaldırıldı | `InternationalSeoPaths`, `OtellerController`, `InternationalSeoService`, `RoutePrefixRequestCultureProvider`, `_LanguageSwitcher`, header JS |
| Zorunlu `lang=tr` `dir=ltr` prefix’siz rotalarda | `_Layout.cshtml`, `_AnasayfaHeader.cshtml` |
| Google Translate / localStorage `ar` temizliği | `_AnasayfaHeader.cshtml` |

**Not:** `Detail.ReviewTeaser.*` (doğru anahtar; `ReviewTabber` typo yok). `Campaign.Period.*` kullanılmıyor — timer `Campaign.Timer.*`.

## #077 — Mobil drawer i18n + layout (2026-05-25)

| Key / davranış | TR (default) | Drawer kullanımı |
|----------------|--------------|-------------------|
| `Nav.Login`, `Nav.Register` | Giriş Yap, Kayıt Ol | Auth satırı |
| `Nav.Hotels`, `Nav.Campaigns`, `Nav.Help`, `Nav.Corporate`, `Nav.Firma` | Oteller, Kampanyalar, … | Hızlı kutular + alt satır |
| `Nav.QuickLink.Boutique`, `.EndOfMonth`, `.FlashDiscount`, `.WeekendDeals`, `.SmartPrice` | Butik Seçimler, Ay Sonu Özel, … | Hızlı kutular + promo grid |
| `Nav.FeaturedDeals` | Öne Çıkan Fırsatlar | Bölüm başlığı (uppercase CSS kaldırıldı) |
| `Nav.LanguageSelection` | Dil Seçimi | Dil grid başlığı |
| `Footer.EarlyBooking` | Erken Rezervasyon | Promo grid |
| `Btn.Close`, `Btn.Cancel` | Kapat, İptal | Drawer kapat / çıkış |

| Değişiklik | Dosya |
|------------|-------|
| Drawer linkleri her zaman TR canonical (`drawerHotelsListingPath` → `/oteller`) | `_AnasayfaHeader.cshtml` |
| `SharedLocalizer[...].Value` drawer metinleri | `_AnasayfaHeader.cshtml` |
| Arapça dil butonu yok (tr/en/fr/de/es/ru) | `_AnasayfaHeader.cshtml` |
| Tek sütun tam genişlik, 44px touch, safe-area, 2-col dil grid | `site-layout.mobile.css` |
| 7× `SharedResources*.resx` drawer anahtarları doğrulandı | `Resources/` |
