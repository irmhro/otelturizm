# -*- coding: utf-8 -*-
"""Generate idempotent MSSQL rename migration: snake_case -> TURKCE BUYUK HARF."""
from __future__ import annotations
import re
from pathlib import Path

ROOT = Path(r"D:\otelturizm")
SCHEMA_FILE = ROOT / "tmp" / "schema_columns.txt"
COMPUTED_FILE = ROOT / "tmp" / "computed_columns.txt"
OUT_SQL = ROOT / "Database" / "MigrationsSql" / "20260522_sqlserver_turkish_uppercase_schema_rename.sql"
OUT_MAP = ROOT / "Database" / "MigrationsSql" / "20260522_schema_name_mapping.json"

TABLE_EXACT: dict[str, str] = {
    "users": "KULLANICILAR",
    "users_partner": "KULLANICI_PARTNERLERI",
    "user_favori_oteller": "KULLANICI_FAVORI_OTELLER",
    "user_favorite_price_alerts": "KULLANICI_FAVORI_FIYAT_ALARMLARI",
    "user_favorite_price_alert_jobs": "KULLANICI_FAVORI_FIYAT_ALARM_ISLERI",
    "email_services": "EPOSTA_SERVISLERI",
    "email_dogrulama_tokenlari": "EPOSTA_DOGRULAMA_TOKENLARI",
    "api_loglari": "API_LOGLARI",
    "outbox_messages": "DIS_KUTU_MESAJLARI",
    "schema_migrations": "SEMA_MIGRASYONLARI",
    "rezervasyonlar_archive": "REZERVASYONLAR_ARSIV",
    "sysdiagrams": "SISTEM_DIYAGRAMLARI",
}

# Longest-first token replacements (snake_case fragments)
TOKEN_RULES: list[tuple[str, str]] = [
    ("created_by_user", "olusturan_kullanici"),
    ("updated_by_user", "guncelleyen_kullanici"),
    ("favorite_price_alert_jobs", "favori_fiyat_alarm_isleri"),
    ("favorite_price_alerts", "favori_fiyat_alarmlari"),
    ("favorite_price", "favori_fiyat"),
    ("price_alert_jobs", "fiyat_alarm_isleri"),
    ("price_alerts", "fiyat_alarmlari"),
    ("price_alert", "fiyat_alarm"),
    ("onaylayan_admin_user", "onaylayan_admin_kullanici"),
    ("olusturan_sales_user", "olusturan_satis_kullanici"),
    ("sales_user", "satis_kullanici"),
    ("admin_user", "admin_kullanici"),
    ("talep_eden_user", "talep_eden_kullanici"),
    ("created_by", "olusturan"),
    ("updated_by", "guncelleyen"),
    ("user_agent", "kullanici_aracisi"),
    ("user_id", "kullanici_id"),
    ("user_favori", "kullanici_favori"),
    ("user_favorite", "kullanici_favori"),
    ("users_partner", "kullanici_partnerleri"),
    ("users", "kullanicilar"),
    ("user", "kullanici"),
    ("is_active", "aktif_mi"),
    ("last_test_message_at", "son_test_mesaj_tarihi"),
    ("delivery_status", "teslimat_durumu"),
    ("error_message", "hata_mesaji"),
    ("error_code", "hata_kodu"),
    ("response_status", "yanit_durumu"),
    ("default_language_code", "varsayilan_dil_kodu"),
    ("verification_template_name", "dogrulama_sablon_adi"),
    ("template_name", "sablon_adi"),
    ("script_name", "betik_adi"),
    ("phone_number_id", "telefon_numarasi_id"),
    ("internet_message_id", "internet_mesaj_kimligi"),
    ("text_icerik", "metin_icerik"),
    ("fts_search_text", "fts_arama_metni"),
    ("search_text", "arama_metni"),
    ("outbox", "dis_kutu"),
    ("schema", "sema"),
    ("favorite", "favori"),
    ("message", "mesaj"),
    ("messages", "mesajlar"),
    ("services", "servisler"),
    ("service", "servis"),
    ("delivery", "teslimat"),
    ("template", "sablon"),
    ("language", "dil"),
    ("archive", "arsiv"),
    ("email", "eposta"),
    ("alert", "alarm"),
    ("alerts", "alarmlar"),
    ("phone", "telefon"),
    ("status", "durum"),
    ("active", "aktif"),
    ("error", "hata"),
    ("response", "yanit"),
    ("script", "betik"),
    ("search", "arama"),
    ("normalized", "normalize"),
    ("partner", "partner"),
    ("admin", "admin"),
    ("sales", "satis"),
    ("token", "token"),
    ("code", "kod"),
    ("name", "ad"),
    ("text", "metin"),
]


def transform_identifier(name: str, *, is_table: bool) -> str:
    if name in TABLE_EXACT:
        return TABLE_EXACT[name]
    n = name.lower()
    if is_table and n == "kullanicilar":
        return "KULLANICILAR"
    for old, new in TOKEN_RULES:
        n = n.replace(old, new)
    # collapse duplicate underscores
    while "__" in n:
        n = n.replace("__", "_")
    return n.strip("_").upper()


def load_schema() -> dict[str, list[str]]:
    tables: dict[str, list[str]] = {}
    for line in SCHEMA_FILE.read_text(encoding="utf-8", errors="replace").splitlines():
        parts = line.strip().split("|")
        if len(parts) < 2:
            continue
        t, c = parts[0], parts[1]
        tables.setdefault(t, []).append(c)
    return tables


def load_computed_skip() -> set[tuple[str, str]]:
    if not COMPUTED_FILE.exists():
        return set()
    skip: set[tuple[str, str]] = set()
    for line in COMPUTED_FILE.read_text(encoding="utf-8", errors="replace").splitlines():
        parts = line.strip().split("|")
        if len(parts) >= 2:
            skip.add((parts[0].lower(), parts[1].lower()))
    return skip


def main() -> None:
    tables = load_schema()
    computed_skip = load_computed_skip()
    table_map = {t: transform_identifier(t, is_table=True) for t in tables}
    column_map: dict[str, dict[str, str]] = {}
    for t, cols in tables.items():
        column_map[t] = {}
        for c in cols:
            if (t.lower(), c.lower()) in computed_skip:
                continue
            column_map[t][c] = transform_identifier(c, is_table=False)

    lines: list[str] = [
        "-- Idempotent schema rename: Turkce BUYUK HARF + Ingilizce token Turkcelestirme",
        "-- Hedef DB: otelturizm_2026db | Once yedek alin.",
        "SET NOCOUNT ON;",
        "SET XACT_ABORT ON;",
        "GO",
        "",
        "/* ---- 1) Trigger kaldir ---- */",
    ]

    triggers = [
        "tr_rezervasyonlar_prevent_delete_sqlserver",
        "trg_user_favori_oteller_sync_oteller_favori_sayisi",
        "trg_oda_fiyat_musaitlik_max_future_365",
        "trg_firma_oda_fiyat_musaitlik_max_future_365",
        "tr_rezervasyonlar_rezervasyon_durumu_sync",
        "tr_rezervasyonlar_odeme_durumu_sync",
    ]
    for tr in triggers:
        lines.append(
            f"IF OBJECT_ID(N'dbo.{tr}', N'TR') IS NOT NULL DROP TRIGGER dbo.{tr};"
        )
    lines.append("GO\n")

    lines.append("/* ---- 2) Tablo yeniden adlandir ---- */")
    for old_t in sorted(tables.keys()):
        new_t = table_map[old_t]
        if old_t == new_t:
            continue
        lines.append(
            f"IF OBJECT_ID(N'dbo.{new_t}', N'U') IS NULL AND OBJECT_ID(N'dbo.{old_t}', N'U') IS NOT NULL"
        )
        lines.append(f"    EXEC sp_rename N'dbo.{old_t}', N'{new_t}', N'OBJECT';")
    lines.append("GO\n")

    lines.append("/* ---- 3) Sutun yeniden adlandir (tablo bazli batch) ---- */")
    for old_t in sorted(tables.keys()):
        new_t = table_map[old_t]
        pending = [
            (o, n)
            for o, n in column_map[old_t].items()
            if o != n
        ]
        if not pending:
            continue
        lines.append(f"/* {old_t} -> {new_t} */")
        for old_c, new_c in pending:
            lines.append(
                f"IF OBJECT_ID(N'dbo.{new_t}', N'U') IS NOT NULL "
                f"AND COL_LENGTH(N'dbo.{new_t}', N'{old_c}') IS NOT NULL "
                f"AND COL_LENGTH(N'dbo.{new_t}', N'{new_c}') IS NULL"
            )
            lines.append(
                f"    EXEC sp_rename N'dbo.{new_t}.{old_c}', N'{new_c}', N'COLUMN';"
            )
        lines.append("GO")

    lines.append("/* ---- 4) Hesaplanan sutunlar: 20260522_sqlserver_computed_columns_uppercase.sql ---- */")
    lines.append("PRINT N'Tablo/sutun rename tamam. Computed + trigger dosyalarini calistirin.';")
    lines.append("GO")

    OUT_SQL.write_text("\n".join(lines) + "\n", encoding="utf-8")

    import json

    payload = {
        "tables": {o: {"new": table_map[o], "columns": column_map[o]} for o in tables},
    }
    OUT_MAP.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"Tables: {len(tables)}")
    print(f"Table renames: {sum(1 for t in tables if table_map[t] != t)}")
    print(
        f"Column renames: {sum(1 for t in tables for c in column_map[t] if column_map[t][c] != c)}"
    )
    print(f"Wrote {OUT_SQL}")
    print(f"Wrote {OUT_MAP}")


if __name__ == "__main__":
    main()
