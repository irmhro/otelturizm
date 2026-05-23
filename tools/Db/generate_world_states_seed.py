# -*- coding: utf-8 -*-
"""Dunya eyalet/il verisi -> dbo.ILLER (TR haric, BOLGE_TIPI=EYALET). UTF-8 BOM."""
from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(r"D:\otelturizm")
TMP = ROOT / "tmp"
OUT = ROOT / "Database" / "MigrationsSql" / "veri" / "migrationlar"
STATES_JSON = TMP / "world_states.json"

TR_ASCII = str.maketrans(
    {
        "ç": "c",
        "Ç": "c",
        "ğ": "g",
        "Ğ": "g",
        "ı": "i",
        "İ": "i",
        "ö": "o",
        "Ö": "o",
        "ş": "s",
        "Ş": "s",
        "ü": "u",
        "Ü": "u",
    }
)


def esc(s: str) -> str:
    return (s or "").replace("'", "''")


def slugify(text: str, fallback: str = "") -> str:
    s = (text or fallback or "").strip().translate(TR_ASCII).lower()
    s = re.sub(r"[^a-z0-9]+", "-", s)
    s = re.sub(r"-{2,}", "-", s).strip("-")
    return s or "bolge"


def pick_name(state: dict) -> str:
    tr = (state.get("translations") or {}).get("tr")
    if isinstance(tr, str) and tr.strip():
        return tr.strip()
    native = state.get("native")
    if isinstance(native, str) and native.strip():
        return native.strip()
    return (state.get("name") or "").strip()


def main() -> None:
    if not STATES_JSON.exists():
        raise FileNotFoundError(f"Eksik: {STATES_JSON}")

    states = json.loads(STATES_JSON.read_text(encoding="utf-8"))
    rows: list[str] = []
    skipped_tr = 0

    for s in states:
        iso_country = (s.get("country_code") or "").strip().upper()
        if iso_country == "TR":
            skipped_tr += 1
            continue

        dis = (s.get("iso2") or "").strip().upper()
        if not dis or not iso_country:
            continue

        name = pick_name(s)
        if not name:
            continue

        slug = slugify(name, dis.lower())
        lat = s.get("latitude")
        lon = s.get("longitude")
        lat_sql = "NULL" if lat is None else str(lat)
        lon_sql = "NULL" if lon is None else str(lon)

        rows.append(
            "("
            f"N'{esc(iso_country)}',N'{esc(dis)}',N'{esc(name)}',N'{esc(slug)}',"
            f"{lat_sql},{lon_sql}"
            ")"
        )

    path = OUT / "20260523_seed_eyaletler_dunya.sql"
    lines = [
        "/* dbo.ILLER — dunya eyalet/il (TR haric) - UTF-8, sqlcmd -f 65001 */",
        "SET NOCOUNT ON;",
        "GO",
        "",
        "IF OBJECT_ID(N'dbo.ILLER', N'U') IS NULL OR OBJECT_ID(N'dbo.ULKELER', N'U') IS NULL RETURN;",
        "IF COL_LENGTH(N'dbo.ILLER', N'ULKE_ID') IS NULL RETURN;",
        "GO",
        "",
        "IF OBJECT_ID('tempdb..#eyalet_seed') IS NOT NULL DROP TABLE #eyalet_seed;",
        "CREATE TABLE #eyalet_seed (",
        "    ULKE_ISO2 nchar(2) NOT NULL,",
        "    DIS_KOD nvarchar(16) NOT NULL,",
        "    IL_ADI nvarchar(100) NOT NULL,",
        "    SEO_SLUG nvarchar(120) NOT NULL,",
        "    ENLEM decimal(10,8) NULL,",
        "    BOYLAM decimal(11,8) NULL",
        ");",
        "GO",
        "",
        "INSERT INTO #eyalet_seed (ULKE_ISO2, DIS_KOD, IL_ADI, SEO_SLUG, ENLEM, BOYLAM) VALUES",
    ]

    batch = 350
    for i in range(0, len(rows), batch):
        if i > 0:
            lines.append("INSERT INTO #eyalet_seed (ULKE_ISO2, DIS_KOD, IL_ADI, SEO_SLUG, ENLEM, BOYLAM) VALUES")
        chunk = rows[i : i + batch]
        lines.append(",\n".join(chunk) + ";\n")

    lines.extend(
        [
            "GO",
            "",
            "MERGE [dbo].[ILLER] AS t",
            "USING (",
            "    SELECT",
            "        u.[ID] AS [ULKE_ID],",
            "        s.[DIS_KOD],",
            "        s.[IL_ADI],",
            "        s.[SEO_SLUG],",
            "        s.[ENLEM],",
            "        s.[BOYLAM]",
            "    FROM #eyalet_seed AS s",
            "    INNER JOIN [dbo].[ULKELER] AS u",
            "        ON RTRIM(u.[ISO2_KODU]) = s.[ULKE_ISO2] AND u.[AKTIF_MI] = 1",
            ") AS s",
            "ON t.[ULKE_ID] = s.[ULKE_ID] AND t.[DIS_KOD] = s.[DIS_KOD]",
            "WHEN MATCHED THEN UPDATE SET",
            "    t.[IL_ADI] = s.[IL_ADI],",
            "    t.[SEO_SLUG] = s.[SEO_SLUG],",
            "    t.[BOLGE_TIPI] = N'EYALET',",
            "    t.[ENLEM] = s.[ENLEM],",
            "    t.[BOYLAM] = s.[BOYLAM],",
            "    t.[GUNCELLENME_TARIHI] = sysutcdatetime()",
            "WHEN NOT MATCHED BY TARGET THEN INSERT (",
            "    [ULKE_ID], [BOLGE_TIPI], [DIS_KOD], [PLAKA_KODU], [IL_ADI], [SEO_SLUG], [ENLEM], [BOYLAM], [AKTIF_MI]",
            ") VALUES (",
            "    s.[ULKE_ID], N'EYALET', s.[DIS_KOD], 0, s.[IL_ADI], s.[SEO_SLUG], s.[ENLEM], s.[BOYLAM], 1",
            ");",
            "GO",
            "",
            f"/* toplam kaynak: {len(rows)}, TR atlandi: {skipped_tr} */",
            "",
        ]
    )

    path.write_text("\n".join(lines), encoding="utf-8-sig")
    print(f"Wrote {path} rows={len(rows)} skipped_tr={skipped_tr}")


if __name__ == "__main__":
    main()
