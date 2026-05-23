# -*- coding: utf-8 -*-
"""ULKELER seed SQL (UTF-8 BOM, Turkce ulke adlari)."""
from __future__ import annotations

import json
import urllib.request
from pathlib import Path

ROOT = Path(r"D:\otelturizm")
OUT = ROOT / "Database" / "MigrationsSql" / "veri" / "migrationlar" / "20260522_seed_ulkeler_dunya_listesi.sql"

FLAG_ICON = {"GB": "gb", "US": "us", "GR": "gr", "AX": "ax"}


def turkish_name(iso2: str, fallback: str) -> str:
    try:
        from babel import Locale

        tr = Locale.parse("tr")
        name = tr.territories.get(iso2)
        if name:
            return name
    except Exception:
        pass
    return fallback


def load_countries() -> list[dict]:
    import pycountry

    rows = []
    for c in pycountry.countries:
        iso2 = c.alpha_2
        iso3 = c.alpha_3
        name = turkish_name(iso2, c.name)
        if iso2 == "TR":
            name = "Türkiye"
        rows.append(
            {
                "iso2": iso2,
                "iso3": iso3,
                "name": name,
                "flag": FLAG_ICON.get(iso2, iso2.lower()),
            }
        )
    return sorted(rows, key=lambda x: (0 if x["iso2"] == "TR" else 1, x["name"]))


def esc(s: str) -> str:
    return s.replace("'", "''")


def main() -> None:
    countries = load_countries()
    if len(countries) < 240:
        raise RuntimeError(f"Ulke sayisi beklenenden az: {len(countries)}")

    lines = [
        "/* dbo.ULKELER: dunya ulke listesi (UTF-8) — sqlcmd -f 65001 ile calistirin */",
        "IF OBJECT_ID(N'dbo.ULKELER', N'U') IS NULL RETURN;",
        "SET NOCOUNT ON;",
        "GO",
        "",
        "IF COL_LENGTH(N'dbo.ULKELER', N'BAYRAK_IKON_KODU') IS NULL",
        "BEGIN",
        "    ALTER TABLE [dbo].[ULKELER] ADD [BAYRAK_IKON_KODU] nvarchar(16) NULL;",
        "END",
        "GO",
        "",
        "IF OBJECT_ID('tempdb..#ulke_seed') IS NOT NULL DROP TABLE #ulke_seed;",
        "CREATE TABLE #ulke_seed (",
        "    ISO2 nchar(2) NOT NULL PRIMARY KEY,",
        "    ULKE_ADI nvarchar(150) NOT NULL,",
        "    ISO3 nchar(3) NOT NULL,",
        "    BAYRAK_IKON_KODU nvarchar(16) NOT NULL,",
        "    VARSAYILAN_ULKE bit NOT NULL,",
        "    AKTIF_MI bit NOT NULL",
        ");",
        "GO",
        "",
        "INSERT INTO #ulke_seed (ISO2, ULKE_ADI, ISO3, BAYRAK_IKON_KODU, VARSAYILAN_ULKE, AKTIF_MI) VALUES",
    ]
    value_rows = []
    for c in countries:
        iso2 = c["iso2"]
        var = 1 if iso2 == "TR" else 0
        value_rows.append(
            f"(N'{esc(iso2)}',N'{esc(c['name'])}',N'{esc(c['iso3'])}',N'{esc(c['flag'])}',{var},1)"
        )
    chunk = 35
    for i in range(0, len(value_rows), chunk):
        part = value_rows[i : i + chunk]
        sep = ",\n" if i + chunk < len(value_rows) else ";\n"
        lines.append(",\n".join(part) + sep)
    lines.extend(
        [
            "GO",
            "",
            "MERGE [dbo].[ULKELER] AS t",
            "USING #ulke_seed AS s ON t.[ISO2_KODU] = s.[ISO2]",
            "WHEN MATCHED THEN UPDATE SET",
            "    [ULKE_ADI] = s.[ULKE_ADI],",
            "    [ISO3_KODU] = s.[ISO3],",
            "    [BAYRAK_IKON_KODU] = s.[BAYRAK_IKON_KODU],",
            "    [VARSAYILAN_ULKE] = s.[VARSAYILAN_ULKE],",
            "    [AKTIF_MI] = s.[AKTIF_MI],",
            "    [GUNCELLENME_TARIHI] = sysutcdatetime()",
            "WHEN NOT MATCHED THEN INSERT ([ULKE_ADI],[ISO2_KODU],[ISO3_KODU],[BAYRAK_IKON_KODU],[VARSAYILAN_ULKE],[AKTIF_MI])",
            "    VALUES (s.[ULKE_ADI], s.[ISO2], s.[ISO3], s.[BAYRAK_IKON_KODU], s.[VARSAYILAN_ULKE], s.[AKTIF_MI]);",
            "GO",
            "",
            f"-- Toplam: {len(countries)} ulke",
            "",
        ]
    )
    OUT.write_text("\n".join(lines), encoding="utf-8-sig")

    # Dogrulama
    sample = OUT.read_text(encoding="utf-8-sig")
    for need in ("Türkiye", "İran", "İspanya", "Almanya", "Fransa"):
        if need not in sample:
            raise RuntimeError(f"Seed dosyasinda eksik: {need}")
    print(f"Wrote {len(countries)} countries (UTF-8 BOM) -> {OUT}")


if __name__ == "__main__":
    main()
