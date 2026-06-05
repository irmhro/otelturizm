# Kullanıcı Paneli — Mobil Master Plan (MOBIL_TEK_EKRAN)



**Orkestra:** `H4_fe_user` · **Wave:** `Wave-VIII-user-mobile`  

**Hedef:** Tüm user panel sayfalarında Booking/Airbnb seviyesi mobil UX — 44px touch, safe-area, tek kolon, kart tabloları.



---



## Envanter (17 sayfa)



| Sayfa | Route | View | mobile.css | Durum |

|-------|-------|------|------------|-------|

| Dashboard | `/panel/user` | Dashboard.cshtml | dashboard.mobile | ✅ |

| Rezervasyonlar | `/panel/user/rezervasyonlar` | Reservations.cshtml | reservations.mobile | ✅ |

| Favoriler | `/panel/user/favorilerim` | Favorites.cshtml | favorites.mobile | ✅ |

| Profil | `/panel/user/profil` | Profile.cshtml | profile.mobile | ✅ |

| Faturalarım | `/panel/user/faturalarim` | Invoices.cshtml | invoices.mobile | ✅ |

| Ödeme | `/panel/user/odeme-yontemleri` | PaymentMethods.cshtml | payment-methods.mobile | ✅ |

| Bildirimler | `/panel/user/bildirimler` | Notifications.cshtml | notifications.mobile | ✅ |

| Güvenlik | `/panel/user/guvenlik` | Security.cshtml | security.mobile | ✅ |

| Sadakat | `/panel/user/sadakat` | Loyalty.cshtml | loyalty.mobile | ✅ |

| Mesajlar | `/panel/user/mesajlar` | Messages.cshtml | messages.mobile | ✅ |

| Yorumlar | `/panel/user/yorumlar` | Reviews.cshtml | reviews.mobile | ✅ |

| Rez. yorum | `/panel/user/rezervasyon-yorum` | ReservationReview.cshtml | reviews.mobile | ✅ paylaşımlı |



**Shell:** `shell.mobile.css` + `user-mobile-bundle.css` + `wwwroot/assets/css/shared/mobile-viewport-shell.css` import.



---



## MOBIL_TEK_EKRAN kuralları (user)



1. `@import` shell + `mobile-viewport-shell.css` (hub: `user-mobile-bundle.css`)

2. Tablolar → `.user-table--cards` + `data-label` (rezervasyon, fatura, dashboard, sadakat geçmişi)

3. KPI grid → 2 kolon @768px, 1 kolon @390px

4. Sticky alt CTA rezervasyon detay linkleri

5. `env(safe-area-inset-*)` topbar + bottom nav (`_UserMobileNav`)

6. Form: `width:100%`, min-height 44px inputs

7. Boş durum: ikon + başlık + 2 CTA



---



## Teknik görevler



| ID | Görev | Durum |

|----|--------|-------|

| T420 | `user-mobile-bundle.css` — ortak import hub | ✅ |

| T421 | `UserPanelController` — tüm action'larda `PageCssPath` + explicit `PageCssMobile` (rezervasyon-yorum) | ✅ |

| T422 | dashboard.mobile | ✅ |

| T423 | reservations.mobile | ✅ |

| T424 | favorites.mobile | ✅ |

| T425 | profile.mobile | ✅ |

| T426 | invoices.mobile | ✅ |

| T427 | payment-methods.mobile | ✅ |

| T428 | notifications.mobile | ✅ |

| T429 | security.mobile | ✅ |

| T430 | loyalty.mobile | ✅ |

| T431 | messages.mobile | ✅ |

| T432 | reviews.mobile + pages.mobile | ✅ |

| T433 | `_UserPanelLayout` — viewport-fit, bundle link | ✅ |

| T434 | FE-CTO user batch SS checklist | ⬜ manuel QA |



---



*Build doğrulama: `dotnet build -o .build-h4-user-mobile`*

