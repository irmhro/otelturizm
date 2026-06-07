#!/usr/bin/env python3
"""Generate oteldetay_masaustu.css from oteldetay_mobil.css + desktop layout."""

from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
MOBIL = ROOT / "wwwroot/assets/css/oteldetay_mobil.css"
OUT = ROOT / "wwwroot/assets/css/oteldetay_masaustu.css"

DESKTOP_HEADER = """/* Otel detay — masaüstü (/oteller/{slug}) · mobil V3/V4 orchestra tasarım dili */

.oteldetay-page-shell {
    background: var(--bg-warm, #FCFBF9);
}

.oteldetay-page-shell.anasayfa-page .oteldetay-page {
    --od-crimson: var(--flag-crimson, #E30A17);
    --od-sun: var(--anatolian-sun, #FF9E00);
    --od-dark: var(--luxury-dark, #1A1919);
    --od-muted: var(--text-muted, #6E6E77);
    --od-border: var(--border-subtle, #EBE9E4);
    --od-bg: var(--bg-warm, #FCFBF9);
    --od-radius: var(--radius-premium, 16px);
    --od-radius-lg: 24px;
    --od-shadow: 0 20px 35px -12px rgba(0, 0, 0, 0.08);
    --od-type-section: var(--home-type-section, 22px);
    --od-type-card-title: var(--home-type-card-title, 14.5px);
    --od-type-card-location: var(--home-type-card-location, 12.5px);
    --od-type-card-meta: var(--home-type-card-meta, 12px);
    --od-type-btn: var(--home-type-btn, 13px);
    --od-page-max: 1240px;
    --home-type-section: 22px;
    --home-type-card-title: 14.5px;
    --home-type-card-location: 12.5px;
    --home-type-card-meta: 12px;
    --home-type-btn: 13px;
}

.oteldetay-page {
    font-family: 'Plus Jakarta Sans', sans-serif;
    letter-spacing: -0.01em;
    -webkit-font-smoothing: antialiased;
    color: var(--od-dark, #1A1919);
    background: var(--od-bg, #FCFBF9);
    padding-bottom: 48px;
}

.oteldetay-page-shell.anasayfa-page .otel-detail-template-v41.od-detail {
    --od-brand: var(--flag-crimson, #E30A17);
    --od-accent: var(--anatolian-sun, #FF9E00);
    --page-max-width: 1240px;
    --border-light: var(--border-subtle, #EBE9E4);
    --card-bg: #fff;
}

"""

DESKTOP_LAYOUT = """
@media (min-width: 901px) {
    /* —— Masaüstü yapı: genişlik, galeri grid, 2 sütun layout —— */
    .oteldetay-page .detail-breadcrumb,
    .oteldetay-page .gallery-grid,
    .oteldetay-page .gallery-filmstrip-wrap,
    .oteldetay-page .detail-header,
    .oteldetay-page .od-quick-facts,
    .oteldetay-page .detail-grid {
        width: min(var(--od-page-max, 1240px), calc(100% - 48px));
        margin-left: auto;
        margin-right: auto;
    }

    .oteldetay-page .mobile-booking-bar,
    .oteldetay-page .od-gallery--mobile,
    .oteldetay-page .gallery-mobile-rating,
    .oteldetay-page .weather-trigger-mobile,
    .oteldetay-page .od-amenities__icon-row,
    .oteldetay-page .od-amenities__see-all-mobile,
    .oteldetay-page .booking-sheet-handle,
    .oteldetay-page .booking-modal-close {
        display: none !important;
    }

    .oteldetay-page .gallery-grid.od-gallery--desktop {
        display: grid !important;
        grid-template-columns: 1.6fr 1fr;
        gap: 8px;
        border-radius: var(--od-radius-lg);
        overflow: hidden;
        min-height: 420px;
        max-height: 520px;
        margin-bottom: -20px;
        position: relative;
        z-index: 2;
        box-shadow: var(--od-shadow);
    }

    .oteldetay-page .gallery-filmstrip-wrap {
        display: block !important;
    }

    .oteldetay-page .gallery-side-grid {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 8px;
    }

    .oteldetay-page .gallery-main-image img,
    .oteldetay-page .gallery-main-trigger img {
        width: 100%;
        height: 100%;
        object-fit: cover;
        min-height: 420px;
    }

    .oteldetay-page .gallery-thumb img {
        width: 100%;
        height: 100%;
        object-fit: cover;
        min-height: 100px;
        transition: transform 0.3s ease;
    }

    .oteldetay-page .gallery-thumb:hover img {
        transform: scale(1.04);
    }

    .oteldetay-page .gallery-filmstrip-wrap {
        margin-top: 12px;
        margin-bottom: 8px;
    }

    .oteldetay-page .gallery-filmstrip {
        display: flex;
        gap: 8px;
        overflow-x: auto;
        scrollbar-width: none;
        padding: 4px 0;
    }

    .oteldetay-page .gallery-filmstrip-thumb {
        flex-shrink: 0;
        width: 72px;
        height: 52px;
        border-radius: 10px;
        overflow: hidden;
        border: 2px solid transparent;
        padding: 0;
        cursor: pointer;
    }

    .oteldetay-page .gallery-filmstrip-thumb.active {
        border-color: var(--od-crimson);
    }

    .oteldetay-page .detail-rating-chip--desktop {
        display: flex !important;
    }

    .oteldetay-page .detail-hero-head {
        display: flex !important;
        align-items: flex-start;
        justify-content: space-between;
        gap: 20px;
        flex-wrap: wrap;
    }

    .oteldetay-page .weather-trigger-desktop {
        display: inline-flex !important;
    }

    .oteldetay-page .detail-header.hotel-title-section {
        background: #fff;
        border-radius: var(--od-radius-lg);
        border: 1px solid var(--od-border);
        box-shadow: var(--od-shadow);
        padding: 24px 28px;
        margin-top: 0;
        position: relative;
        z-index: 5;
    }

    .oteldetay-page .hotel-name h1 {
        font-size: clamp(1.5rem, 2.4vw, 2rem);
        font-weight: 800;
        letter-spacing: -0.025em;
        line-height: 1.15;
        margin: 0 0 8px;
    }

    .oteldetay-page .detail-grid.od-detail__layout {
        display: grid !important;
        grid-template-columns: minmax(0, 1fr) 380px !important;
        gap: 32px !important;
        align-items: start;
        margin-top: 8px;
        margin-bottom: 32px;
        padding: 0 !important;
    }

    .oteldetay-page .booking-sidebar.od-detail__booking {
        position: sticky !important;
        top: calc(var(--home-header-sticky-h, 99px) + 12px) !important;
        inset: auto !important;
        transform: none !important;
        max-height: none !important;
        width: auto !important;
        pointer-events: auto !important;
        visibility: visible !important;
        z-index: 10 !important;
        padding: 0 !important;
        margin: 0 !important;
        background: transparent !important;
        box-shadow: none !important;
    }

    .oteldetay-page .detail-grid .booking-modal-backdrop {
        display: none !important;
    }

    .oteldetay-page .od-similar-grid {
        grid-template-columns: repeat(3, minmax(0, 1fr)) !important;
        gap: 16px !important;
    }

    @media (min-width: 901px) and (max-width: 1100px) {
        .oteldetay-page .detail-grid.od-detail__layout {
            grid-template-columns: minmax(0, 1fr) 340px !important;
            gap: 24px !important;
        }
    }

    @media (min-width: 901px) and (max-width: 1000px) {
        .oteldetay-page .detail-grid.od-detail__layout {
            grid-template-columns: 1fr !important;
        }

        .oteldetay-page .booking-sidebar.od-detail__booking {
            position: relative !important;
            top: 0 !important;
        }
    }

"""

DESKTOP_FOOTER = """
    /* —— Masaüstü orchestra tokenları —— */
    .oteldetay-page.otel-detail-template-v41.otel-detail-tabler {
        --od-d-step: 38px;
        --od-d-radius: 14px;
        --od-d-border: 1px solid var(--od-border, #EBE9E4);
        padding-bottom: 48px;
    }

    .oteldetay-page.otel-detail-template-v41.otel-detail-tabler .content-card {
        border: 1px solid var(--od-border, #EBE9E4) !important;
        border-radius: var(--od-radius-lg) !important;
        box-shadow: 0 4px 14px rgba(26, 25, 25, 0.03) !important;
        background: #fff !important;
        margin-bottom: 20px !important;
        padding: 22px 24px !important;
    }

    .oteldetay-page.otel-detail-template-v41.otel-detail-tabler .detail-header.hotel-title-section {
        border: 1px solid var(--od-border, #EBE9E4) !important;
        border-radius: var(--od-radius-lg) !important;
        box-shadow: var(--od-shadow) !important;
        padding: 24px 28px !important;
        margin-bottom: 0 !important;
    }

    .oteldetay-page.otel-detail-template-v41.otel-detail-tabler .weather-trigger-desktop {
        display: inline-flex !important;
    }

    .oteldetay-page.otel-detail-template-v41.otel-detail-tabler .weather-trigger-mobile {
        display: none !important;
    }

    .oteldetay-page.otel-detail-template-v41.otel-detail-tabler .weather-inline-trigger {
        width: auto;
        border-radius: var(--od-d-radius) !important;
        border: var(--od-d-border) !important;
        box-shadow: none !important;
        background: var(--od-bg, #FCFBF9) !important;
        padding: 10px 14px !important;
        min-height: 0 !important;
        transform: none !important;
    }

    .oteldetay-page.otel-detail-template-v41.otel-detail-tabler .booking-card {
        border: 1px solid var(--od-border, #EBE9E4) !important;
        border-radius: 28px !important;
        box-shadow: 0 12px 28px rgba(0, 0, 0, 0.05) !important;
        background: #fff !important;
        padding: 24px !important;
        max-height: none !important;
        overflow: visible !important;
    }

    .oteldetay-page.otel-detail-template-v41.otel-detail-tabler .guest-btn,
    .oteldetay-page.otel-detail-template-v41.otel-detail-tabler .room-quantity-control button {
        width: var(--od-d-step) !important;
        height: var(--od-d-step) !important;
        min-width: var(--od-d-step) !important;
        min-height: var(--od-d-step) !important;
        border-radius: var(--od-d-radius) !important;
        border: var(--od-d-border) !important;
        background: #fff !important;
        box-shadow: none !important;
        color: var(--od-dark, #1A1919) !important;
        font-size: 16px !important;
        font-weight: 700 !important;
        display: inline-flex !important;
        align-items: center !important;
        justify-content: center !important;
    }

    .oteldetay-page.otel-detail-template-v41 #reviewsSection .review-summary.review-summary--ota {
        grid-template-columns: minmax(160px, 220px) minmax(0, 1fr) !important;
        gap: 24px !important;
        padding: 18px !important;
    }

    .oteldetay-page.otel-detail-template-v41 #reviewsSection .review-summary-score-col {
        flex-direction: column !important;
        text-align: center !important;
        align-items: center !important;
    }

    .oteldetay-page.otel-detail-template-v41 #reviewsSection .review-topic-strip {
        flex-wrap: wrap !important;
        overflow-x: visible !important;
    }

    .oteldetay-page #roomsCard .room-card.od-room-card {
        border-radius: 22px !important;
        transition: transform 0.22s ease, box-shadow 0.22s ease;
    }

    .oteldetay-page #roomsCard .room-card.od-room-card:hover {
        transform: translateY(-3px);
        box-shadow: 0 12px 28px rgba(26, 25, 25, 0.08) !important;
    }

    .oteldetay-page .detail-breadcrumb {
        display: flex;
        align-items: center;
        justify-content: space-between;
        flex-wrap: wrap;
        gap: 12px;
        padding: 14px 0 10px;
    }

    .oteldetay-page .detail-back-link {
        display: inline-flex;
        align-items: center;
        gap: 8px;
        padding: 8px 16px;
        border-radius: 999px;
        background: #fff;
        border: 1px solid var(--od-border);
        color: var(--od-dark);
        font-weight: 700;
        font-size: var(--od-type-btn);
        text-decoration: none;
        transition: background 0.18s ease, color 0.18s ease, border-color 0.18s ease;
    }

    .oteldetay-page .detail-back-link:hover {
        background: var(--od-crimson);
        border-color: var(--od-crimson);
        color: #fff;
    }

    .oteldetay-page .tabler-section-head h2,
    .oteldetay-page .detail-collapsible__summary h2 {
        font-size: var(--od-type-section);
        font-weight: 800;
        letter-spacing: -0.02em;
        margin: 0;
        padding-left: 14px;
        border-left: 5px solid var(--od-sun);
        line-height: 1.2;
    }

    .oteldetay-page .btn-reserve,
    .oteldetay-page .booking-form button[type="submit"],
    .oteldetay-page .reservation-create-btn {
        width: 100%;
        min-height: 48px;
        margin-top: 12px;
        border: 0;
        border-radius: 999px;
        background: linear-gradient(105deg, var(--od-crimson), #B00010);
        color: #fff;
        font-weight: 800;
        font-size: 15px;
        cursor: pointer;
    }

    .oteldetay-page .booking-price-total {
        font-size: clamp(1.6rem, 2.2vw, 2.1rem);
        font-weight: 900;
        color: var(--od-crimson);
        letter-spacing: -0.02em;
    }

    .oteldetay-page .od-amenities__see-all-desktop {
        display: inline-flex;
    }

    .oteldetay-page .amenities-list-clean {
        display: grid;
        grid-template-columns: repeat(2, minmax(0, 1fr));
        gap: 10px;
        margin-top: 14px;
    }

    .oteldetay-page .od-policy-list {
        list-style: none;
        margin: 0;
        padding: 0;
        display: grid;
        gap: 14px;
    }

    .oteldetay-page .od-policy-item {
        display: grid;
        grid-template-columns: 40px minmax(0, 1fr);
        gap: 12px;
        padding: 14px 16px;
        border: 1px solid var(--od-border);
        border-radius: var(--od-radius);
        background: #fff;
    }

    .oteldetay-page .od-similar-card {
        display: flex;
        flex-direction: column;
        border: 1px solid var(--od-border);
        border-radius: var(--od-radius);
        overflow: hidden;
        background: #fff;
        text-decoration: none;
        color: inherit;
        transition: box-shadow 0.18s ease, transform 0.18s ease;
    }

    .oteldetay-page .od-similar-card:hover {
        box-shadow: var(--od-shadow);
        transform: translateY(-2px);
    }

    .oteldetay-page-shell.anasayfa-page .otel-detail-template-v41.od-detail .tabler-section-action-btn.is-primary {
        background: var(--flag-crimson, #E30A17);
        border-color: var(--flag-crimson, #E30A17);
    }

    .oteldetay-page .detail-location-map,
    .oteldetay-page .map-card .detail-location-map {
        height: 240px;
        border-radius: var(--od-radius);
        overflow: hidden;
        margin-top: 12px;
    }

    /* Konum: başlık + adres + Haritada aç aynı satır (masaüstü) */
    .oteldetay-page .od-section--map .tabler-section-headrow,
    .oteldetay-page .detail-map-card .tabler-section-headrow {
        flex-direction: row !important;
        align-items: center !important;
        justify-content: space-between !important;
        flex-wrap: nowrap !important;
        gap: 12px !important;
    }

    .oteldetay-page .od-section--map .tabler-section-titles,
    .oteldetay-page .detail-map-card .tabler-section-titles {
        display: flex !important;
        flex-direction: row !important;
        align-items: center !important;
        gap: 10px !important;
        flex: 1 1 auto !important;
        min-width: 0 !important;
    }

    .oteldetay-page .od-section--map .tabler-section-titles p,
    .oteldetay-page .detail-map-card .tabler-section-titles p {
        margin: 0 !important;
        flex: 1 1 auto !important;
        min-width: 0 !important;
        white-space: nowrap !important;
        overflow: hidden !important;
        text-overflow: ellipsis !important;
    }

    .oteldetay-page .od-section--map .tabler-section-actions,
    .oteldetay-page .detail-map-card .tabler-section-actions {
        width: auto !important;
        flex: 0 0 auto !important;
    }

    .oteldetay-page .od-section--map .tabler-section-action-btn,
    .oteldetay-page .detail-map-card .tabler-section-action-btn {
        width: auto !important;
        min-height: 36px !important;
        white-space: nowrap !important;
    }
}

@media (prefers-reduced-motion: reduce) {
    .oteldetay-page .room-card.od-room-card,
    .oteldetay-page .gallery-thumb img,
    .oteldetay-page .od-similar-card {
        transition: none;
    }
}
"""

SKIP_SELECTOR_FRAGMENTS = (
    ".mobile-booking-bar",
    ".gallery-mobile-",
    ".od-gallery--mobile",
    ".weather-trigger-mobile",
    ".booking-sheet-handle",
    ".booking-modal-close",
    ".gallery-mobile-rating",
    ".od-amenities__icon-row",
    ".od-amenities__see-all-mobile",
    ".hotel-title-heading--header",
    "> #detailInfoModal",
    "> #profileCompletionModal",
    "> #reservationConfirmModal",
    "> #detailActionBackdrop",
    "> .detail-modal-backdrop",
    "> .review-modal",
    "> .review-insight-modal",
    "> .detail-modal",
    "> .room-pick-popover",
    "> .discount-modal",
    "> .booking-modal-backdrop",
    "> #weatherPopupBackdrop",
    "> #weatherPopup",
    "> #amenitiesModal",
    "> #roomDetailModal",
    ".public-premium-main",
)

SKIP_BODY_FRAGMENTS = (
    "order:",
    "padding-bottom: calc(124px",
    "padding-bottom: calc(var(--od-m-dock)",
    "display: none !important;\n        flex-direction: column",
    "inset: auto 0 0 0",
    "transform: translateY(calc(100%",
    "overflow-x: clip",
    "display: flex;\n        flex-direction: column;\n        gap: 0",
    ".gallery-grid.od-gallery--desktop",
    "display: none !important;\n    }\n\n    .oteldetay-page .gallery-filmstrip-wrap",
    ".detail-hero-head {\n        display: none",
    ".weather-trigger-desktop {\n        display: none",
    ".weather-trigger-mobile {\n        display: flex",
    "grid-template-columns: 1fr !important;\n        gap: 0",
    "gap: 0 !important;\n        padding: 0 !important",
    "border-radius: 0 !important",
    "border-bottom: 1px solid var(--od-border",
    "--od-m-dock",
    "safe-area-inset-bottom",
    "-webkit-overflow-scrolling: touch",
    "scroll-snap-type",
    "overscroll-behavior-x",
)


def extract_mobil_block(text: str) -> str:
    m = re.search(
        r"@media\s*\(\s*max-width:\s*900px\s*\)\s*\{(.*)\}\s*@media\s+print",
        text,
        re.DOTALL,
    )
    if not m:
        raise SystemExit("Could not find mobil @media block")
    return m.group(1)


def split_rules(block: str) -> list[str]:
    """Split CSS block into top-level rule strings (comments + rules)."""
    rules: list[str] = []
    buf: list[str] = []
    depth = 0
    i = 0
    while i < len(block):
        c = block[i]
        if c == "{":
            depth += 1
        elif c == "}":
            depth -= 1
            if depth == 0:
                buf.append(c)
                chunk = "".join(buf).strip()
                if chunk:
                    rules.append(chunk)
                buf = []
                i += 1
                continue
        buf.append(c)
        i += 1
    tail = "".join(buf).strip()
    if tail:
        rules.append(tail)
    return rules


def should_skip(rule: str) -> bool:
    if "{" not in rule:
        return rule.strip().startswith("/*")
    selector = rule.split("{", 1)[0]
    for frag in SKIP_SELECTOR_FRAGMENTS:
        if frag in selector:
            return True
    body = rule[rule.find("{") + 1 :] if "{" in rule else ""
    for frag in SKIP_BODY_FRAGMENTS:
        if frag in body or frag in rule:
            return True
    if re.search(r"\.oteldetay-page\s*\{[^}]*display:\s*flex[^}]*flex-direction:\s*column", rule, re.DOTALL):
        return True
    if re.search(r"\.booking-sidebar\.od-detail__booking[^}]*position:\s*fixed", rule, re.DOTALL):
        return True
    if re.search(r"\.detail-grid\s+\.booking-modal-backdrop", rule):
        return True
    if re.search(r"\.detail-grid\s+\.booking-sidebar", rule) and "position: fixed" in rule:
        return True
    if re.search(r"\.mobile-booking-bar", selector):
        return True
    if re.search(r"--home-btn-min-height", rule):
        return True
    return False


def adapt_rule(rule: str) -> str:
    rule = rule.replace("--od-m-step", "--od-d-step")
    rule = rule.replace("--od-m-radius", "--od-d-radius")
    rule = rule.replace("--od-m-border", "--od-d-border")
    rule = rule.replace("grid-template-columns: 1fr !important", "grid-template-columns: minmax(160px, 220px) minmax(0, 1fr) !important")
    return rule


def main() -> None:
    mobil_text = MOBIL.read_text(encoding="utf-8")
    inner = extract_mobil_block(mobil_text)
    rules = split_rules(inner)

    kept: list[str] = []
    for rule in rules:
        if should_skip(rule):
            continue
        kept.append(adapt_rule(rule))

    body = "\n\n    ".join(kept)
    output = DESKTOP_HEADER + DESKTOP_LAYOUT + body + DESKTOP_FOOTER
    OUT.write_text(output, encoding="utf-8")
    print(f"Wrote {OUT} ({len(output.splitlines())} lines, {len(kept)} rules from mobil)")


if __name__ == "__main__":
    main()
