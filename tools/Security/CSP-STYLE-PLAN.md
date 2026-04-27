# Inline Style Azaltma Planı (p77)

## Amaç

Web sayfalarında (public + panel) inline `style="..."` ve `<style>...</style>` kullanımını azaltmak.

Not: Email şablonları (Views/Email) inline CSS gerektirebilir; bu plan web UI içindir.

## Envanter

- Script: `tools/Security/Inventory-InlineStyles.ps1`
- Çıktı: `tools/Security/inline-style-inventory.txt`

## Strateji

1. **Web UI**: Inline style’ları `wwwroot/assets/css/*` altındaki ilgili dosyalara taşı.
2. **Dinamik renkler**: `style="background: ... @model"` gibi yerleri CSS değişkeni ile çöz:
   - Elemanda: `style="--accent: @color"` (tek satır)
   - CSS’te: `background: color-mix(in srgb, var(--accent) 14%, white);`
3. **CSP hedefi**: Uzun vadede web tarafında `style-src` için `unsafe-inline` azaltılabilir; kısa vadede script enforce önceliklidir.

