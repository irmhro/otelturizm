# Kullanıcı paneli (`/panel/user`) — eksiklik planı ve durum

## Kapsam

- Controller: `Controllers/Paneller/User/UserPanelController.cs`
- Layout / parçalar: `Views/Paneller/User/_UserPanelLayout.cshtml`, `_UserSidebar.cshtml`, `_UserMobileNav.cshtml`
- Servis: `Services/UserPanelService.cs`, `IUserPanelService`

## Plan (öncelik sırası)

| # | Konu | Durum |
|---|------|--------|
| 1 | Mobil alt menü: `Index` yerine `Dashboard`, doğru controller ve aktif sınıflar | Tamamlandı |
| 2 | Kenar çubuğu rozetleri: tüm sayfalarda `FavoriteCount` / `ReservationCount` / `MessageCount` tutarlılığı | Tamamlandı (`GetNavBadgeCountsAsync` + layout doldurma) |
| 3 | Türkçe başlık ve alt başlık metinleri (ViewData) | Tamamlandı (controller düzeltmeleri) |
| 4 | Mobil UX: alt bar dahil edildi, yatay kaydırmalı ek bağlantılar (Mesaj, Yorum) | Tamamlandı |
| 5 | Mobil sayfa alt dolgu (`safe-area`) | Tamamlandı (`user-panel-page-body-mobile-pad`) |

## Teknik notlar

- **Rozetler:** Sayfa action’ı ViewData set etmese bile layout, `userId > 0` iken eksik alanları `GetNavBadgeCountsAsync` ile doldurur. Dashboard özeti ile aynı SQL mantığına hizalıdır (mesaj: `durum <> 'Arşivlendi'`).
- **Mobil menü:** Sadece oturum açık kullanıcıda (`userId > 0`) gösterilir; stiller `wwwroot/assets/css/paneller/user/shell.mobile.css` içindedir.

## Doğrulama

- Yerel: `dotnet build "D:\otelturizm\otelturizmnew.csproj" --no-restore`
