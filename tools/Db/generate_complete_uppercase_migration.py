# -*- coding: utf-8 -*-
"""Tek dosyada: tum tablo/sutun -> BUYUK HARF (CI icin iki adimli rename)."""
from __future__ import annotations
import json
import re
from pathlib import Path

ROOT = Path(r"D:\otelturizm")
MAP_FILE = ROOT / "Database" / "MigrationsSql" / "20260522_schema_name_mapping.json"
OUT_SQL = ROOT / "Database" / "MigrationsSql" / "20260526_sqlserver_complete_uppercase_all.sql"

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
    "__sql_migrations": "SEMA_MIGRASYONLARI",
}

TOKEN_RULES: list[tuple[str, str]] = [
    ("created_by_user", "olusturan_kullanici"),
    ("updated_by_user", "guncelleyen_kullanici"),
    ("favorite_price_alert_jobs", "favori_fiyat_alarm_isleri"),
    ("favorite_price_alerts", "favori_fiyat_alarmlari"),
    ("onaylayan_admin_user", "onaylayan_admin_kullanici"),
    ("olusturan_sales_user", "olusturan_satis_kullanici"),
    ("sales_user", "satis_kullanici"),
    ("admin_user", "admin_kullanici"),
    ("talep_eden_user", "talep_eden_kullanici"),
    ("user_agent", "kullanici_aracisi"),
    ("user_id", "kullanici_id"),
    ("user_favori", "kullanici_favori"),
    ("user_favorite", "kullanici_favori"),
    ("users_partner", "kullanici_partnerleri"),
    ("users", "kullanicilar"),
    ("user", "kullanici"),
    ("is_active", "aktif_mi"),
    ("email", "eposta"),
    ("favorite", "favori"),
    ("message", "mesaj"),
    ("messages", "mesajlar"),
    ("services", "servisler"),
    ("service", "servis"),
    ("delivery", "teslimat"),
    ("template", "sablon"),
    ("archive", "arsiv"),
    ("alert", "alarm"),
    ("normalized", "normalize"),
    ("search_text", "arama_metni"),
    ("fts_search_text", "fts_arama_metni"),
    ("permissions", "yetkiler"),
    ("permission", "yetki"),
    ("roles", "roller"),
    ("role", "rol"),
]


def transform(name: str, *, is_table: bool) -> str:
    if name in TABLE_EXACT:
        return TABLE_EXACT[name]
    n = name.lower()
    if is_table and n == "kullanicilar":
        return "KULLANICILAR"
    for old, new in TOKEN_RULES:
        n = n.replace(old, new)
    while "__" in n:
        n = n.replace("__", "_")
    return n.strip("_").upper()


def esc(s: str) -> str:
    return s.replace("'", "''")


def load_schema(path: Path) -> dict[str, list[str]]:
    tables: dict[str, list[str]] = {}
    for line in path.read_text(encoding="utf-8", errors="replace").splitlines():
        p = line.strip().split("|")
        if len(p) >= 2:
            tables.setdefault(p[0], []).append(p[1])
    return tables


def col_rename_lines(table: str, old_c: str, new_c: str) -> list[str]:
    if old_c == new_c:
        return []
    t = esc(table)
    o, n = esc(old_c), esc(new_c)
    if old_c.lower() == new_c.lower():
        tmp = esc(old_c + "__cs_tmp")
        return [
            f"IF COL_LENGTH(N'dbo.{t}', N'{o}') IS NOT NULL",
            f"    EXEC sp_rename N'dbo.{t}.{o}', N'{tmp}', N'COLUMN';",
            f"IF COL_LENGTH(N'dbo.{t}', N'{tmp}') IS NOT NULL",
            f"    EXEC sp_rename N'dbo.{t}.{tmp}', N'{n}', N'COLUMN';",
        ]
    return [
        f"IF COL_LENGTH(N'dbo.{t}', N'{o}') IS NOT NULL AND COL_LENGTH(N'dbo.{t}', N'{n}') IS NULL",
        f"    EXEC sp_rename N'dbo.{t}.{o}', N'{n}', N'COLUMN';",
    ]


def main() -> None:
    schema_path = ROOT / "tmp" / "schema_columns.txt"
    if not schema_path.exists():
        raise SystemExit("tmp/schema_columns.txt yok; once sqlcmd ile export edin.")
    tables = load_schema(schema_path)
    lines = [
        "-- EKSIKSIZ tablo + sutun BUYUK HARF (idempotent)",
        "SET NOCOUNT ON;",
        "GO",
        "",
        "/* 1) Trigger */",
        "DECLARE @s nvarchar(max);",
        "SELECT @s = (SELECT N'DROP TRIGGER ' + QUOTENAME(OBJECT_SCHEMA_NAME(parent_id)) + N'.' + QUOTENAME(name) + N';'",
        "FROM sys.triggers WHERE parent_id > 0 FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)');",
        "IF @s IS NOT NULL EXEC sp_executesql @s;",
        "GO",
        "",
        "/* 2) FK kaldir */",
        "DECLARE @s nvarchar(max);",
        "SELECT @s = (SELECT N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(parent_object_id)) + N'.' + QUOTENAME(OBJECT_NAME(parent_object_id))",
        " + N' DROP CONSTRAINT ' + QUOTENAME(name) + N';' FROM sys.foreign_keys FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)');",
        "IF @s IS NOT NULL EXEC sp_executesql @s;",
        "GO",
        "",
        "/* 3) Nonclustered index + UQ kaldir (computed'dan once) */",
        "DECLARE @s nvarchar(max);",
        "SELECT @s = (SELECT N'DROP INDEX ' + QUOTENAME(i.name) + N' ON ' + QUOTENAME(OBJECT_SCHEMA_NAME(i.object_id)) + N'.' + QUOTENAME(OBJECT_NAME(i.object_id)) + N';'",
        "FROM sys.indexes i WHERE i.type IN (1,2) AND i.is_primary_key=0 AND OBJECTPROPERTY(i.object_id,'IsMsShipped')=0 FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)');",
        "IF @s IS NOT NULL BEGIN TRY EXEC sp_executesql @s; END TRY BEGIN CATCH END CATCH END;",
        "SELECT @s = (SELECT N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(parent_object_id)) + N'.' + QUOTENAME(OBJECT_NAME(parent_object_id))",
        " + N' DROP CONSTRAINT ' + QUOTENAME(name) + N';' FROM sys.key_constraints WHERE type='UQ' FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)');",
        "IF @s IS NOT NULL EXEC sp_executesql @s;",
        "GO",
        "",
        "/* 4) Computed sutun kaldir */",
        "DECLARE @d nvarchar(max);",
        "SELECT @d = (SELECT N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(cc.object_id)) + N'.' + QUOTENAME(OBJECT_NAME(cc.object_id))",
        " + N' DROP COLUMN ' + QUOTENAME(c.name) + N';' FROM sys.computed_columns cc",
        "JOIN sys.columns c ON cc.object_id=c.object_id AND cc.column_id=c.column_id FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)');",
        "IF @d IS NOT NULL EXEC sp_executesql @d;",
        "GO",
        "",
        "/* 5) Tablolar */",
    ]
    for old_t in sorted(tables.keys()):
        new_t = transform(old_t, is_table=True)
        if old_t == new_t:
            continue
        o, n = esc(old_t), esc(new_t)
        if old_t.lower() == new_t.lower():
            lines += [
                f"IF OBJECT_ID(N'dbo.{o}', N'U') IS NOT NULL",
                f"    EXEC sp_rename N'dbo.{o}', N'{o}__cs_tmp', N'OBJECT';",
                f"IF OBJECT_ID(N'dbo.{o}__cs_tmp', N'U') IS NOT NULL",
                f"    EXEC sp_rename N'dbo.{o}__cs_tmp', N'{n}', N'OBJECT';",
            ]
        else:
            lines += [
                f"IF OBJECT_ID(N'dbo.{o}', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.{n}', N'U') IS NULL",
                f"    EXEC sp_rename N'dbo.{o}', N'{n}', N'OBJECT';",
            ]
    lines.append("GO\n")
    lines.append("/* 6) Sutunlar */")
    for old_t, cols in sorted(tables.items()):
        new_t = transform(old_t, is_table=True)
        pending = []
        for c in cols:
            nc = transform(c, is_table=False)
            if c != nc:
                pending.append((c, nc))
        if not pending:
            continue
        lines.append(f"/* {new_t} */")
        for oc, nc in pending:
            lines.extend(col_rename_lines(new_t, oc, nc))
        lines.append("GO")
    lines += [
        "",
        "PRINT N'BUYUK HARF donusum tamam. Sonra computed + trigger dosyalarini calistirin.';",
        "GO",
    ]
    OUT_SQL.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print("Wrote", OUT_SQL, "lines", len(lines))


if __name__ == "__main__":
    main()
