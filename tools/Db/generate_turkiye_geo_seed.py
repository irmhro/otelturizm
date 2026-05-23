# -*- coding: utf-8 -*-
"""Türkiye il / ilçe / mahalle seed SQL (UTF-8 BOM, TurkiyeAPI 2025)."""
from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(r"D:\otelturizm")
TMP = ROOT / "tmp"
OUT = ROOT / "Database" / "MigrationsSql"
CACHE_PATH = TMP / "turkiye_coords_cache.json"


def load_coords_cache() -> dict:
    if not CACHE_PATH.exists():
        return {"districts": {}, "neighborhoods": {}}
    data = json.loads(CACHE_PATH.read_text(encoding="utf-8"))
    return {
        "districts": data.get("districts", {}),
        "neighborhoods": data.get("neighborhoods", {}),
    }

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
    return s or "adres"


def load_json(name: str) -> list[dict]:
    path = TMP / f"turkiye_{name}.json"
    if not path.exists():
        raise FileNotFoundError(f"Eksik: {path} — once curl ile indirin.")
    data = json.loads(path.read_text(encoding="utf-8"))
    return data if isinstance(data, list) else data.get("data", data)


def sql_header(title: str) -> list[str]:
    return [
        f"/* {title} - UTF-8, sqlcmd -f 65001 */",
        "SET NOCOUNT ON;",
        "GO",
        "",
    ]


def write_batches(path: Path, insert_header: str, rows: list[str], batch_size: int = 400) -> None:
    lines = sql_header(path.stem)
    lines.append(insert_header)
    for i in range(0, len(rows), batch_size):
        chunk = rows[i : i + batch_size]
        sep = ",\n" if i + batch_size < len(rows) else ";\n"
        lines.append(",\n".join(chunk) + sep)
        if i + batch_size < len(rows):
            lines.append("INSERT INTO #seed VALUES")
    lines.append("GO\n")
    path.write_text("\n".join(lines), encoding="utf-8-sig")


def generate_iller(provinces: list[dict]) -> None:
    path = OUT / "20260522_seed_iller_turkiye.sql"
    lines = sql_header("dbo.ILLER — 81 il")
    lines.extend(
        [
            "IF OBJECT_ID(N'dbo.ILLER', N'U') IS NULL RETURN;",
            "DECLARE @trUlkeId bigint = (",
            "    SELECT TOP (1) [ID] FROM [dbo].[ULKELER]",
            "    WHERE [AKTIF_MI] = 1 AND RTRIM([ISO2_KODU]) = N'TR'",
            "    ORDER BY [VARSAYILAN_ULKE] DESC, [ID] ASC",
            ");",
            "IF @trUlkeId IS NULL RETURN;",
            "DELETE m FROM [dbo].[MAHALLELER] AS m",
            "INNER JOIN [dbo].[ILCELER] AS c ON c.[ID] = m.[ILCE_ID]",
            "INNER JOIN [dbo].[ILLER] AS i ON i.[ID] = c.[IL_ID]",
            "WHERE i.[ULKE_ID] = @trUlkeId;",
            "DELETE c FROM [dbo].[ILCELER] AS c",
            "INNER JOIN [dbo].[ILLER] AS i ON i.[ID] = c.[IL_ID]",
            "WHERE i.[ULKE_ID] = @trUlkeId;",
            "DELETE FROM [dbo].[ILLER] WHERE [ULKE_ID] = @trUlkeId;",
            "GO",
            "",
            "IF OBJECT_ID('tempdb..#il_seed') IS NOT NULL DROP TABLE #il_seed;",
            "CREATE TABLE #il_seed (",
            "    PLAKA smallint NOT NULL PRIMARY KEY,",
            "    IL_ADI nvarchar(100) NOT NULL,",
            "    SEO_SLUG nvarchar(120) NOT NULL,",
            "    BOLGE nvarchar(50) NULL,",
            "    ENLEM decimal(10,8) NULL,",
            "    BOYLAM decimal(11,8) NULL,",
            "    NUFUS int NULL",
            ");",
            "GO",
            "",
            "INSERT INTO #il_seed (PLAKA, IL_ADI, SEO_SLUG, BOLGE, ENLEM, BOYLAM, NUFUS) VALUES",
        ]
    )
    rows = []
    for p in sorted(provinces, key=lambda x: x["id"]):
        pid = int(p["id"])
        name = p["name"]
        slug = p.get("slug") or slugify(name, str(pid))
        region = p.get("region")
        if isinstance(region, dict):
            bolge = region.get("tr") or region.get("en")
        else:
            bolge = region
        coords = p.get("coordinates") or {}
        lat = coords.get("latitude")
        lon = coords.get("longitude")
        pop = p.get("population")
        lat_sql = "NULL" if lat is None else str(lat)
        lon_sql = "NULL" if lon is None else str(lon)
        pop_sql = "NULL" if pop is None else str(int(pop))
        bolge_sql = "NULL" if not bolge else f"N'{esc(str(bolge))}'"
        rows.append(
            f"({pid},N'{esc(name)}',N'{esc(slug)}',{bolge_sql},{lat_sql},{lon_sql},{pop_sql})"
        )
    batch = 40
    for i in range(0, len(rows), batch):
        if i > 0:
            lines.append("INSERT INTO #il_seed (PLAKA, IL_ADI, SEO_SLUG, BOLGE, ENLEM, BOYLAM, NUFUS) VALUES")
        chunk = rows[i : i + batch]
        lines.append(",\n".join(chunk) + ";\n")
    lines.extend(
        [
            "GO",
            "",
            "DECLARE @trUlkeId bigint = (",
            "    SELECT TOP (1) [ID] FROM [dbo].[ULKELER]",
            "    WHERE [AKTIF_MI] = 1 AND RTRIM([ISO2_KODU]) = N'TR'",
            "    ORDER BY [VARSAYILAN_ULKE] DESC, [ID] ASC",
            ");",
            "IF @trUlkeId IS NULL RETURN;",
            "",
            "MERGE [dbo].[ILLER] AS t",
            "USING (",
            "    SELECT @trUlkeId AS [ULKE_ID], s.*,",
            "           RIGHT(N'00' + CAST(s.[PLAKA] AS nvarchar(3)), 2) AS [DIS_KOD]",
            "    FROM #il_seed AS s",
            ") AS s ON t.[ULKE_ID] = s.[ULKE_ID] AND t.[PLAKA_KODU] = s.[PLAKA]",
            "WHEN MATCHED THEN UPDATE SET",
            "    [IL_ADI]=s.[IL_ADI],[SEO_SLUG]=s.[SEO_SLUG],[BOLGE]=s.[BOLGE],",
            "    [BOLGE_TIPI]=N'IL',[DIS_KOD]=s.[DIS_KOD],",
            "    [ENLEM]=s.[ENLEM],[BOYLAM]=s.[BOYLAM],[NUFUS]=s.[NUFUS],",
            "    [AKTIF_MI]=1,[GUNCELLENME_TARIHI]=sysutcdatetime()",
            "WHEN NOT MATCHED THEN INSERT ([ULKE_ID],[BOLGE_TIPI],[DIS_KOD],[PLAKA_KODU],[IL_ADI],[SEO_SLUG],[BOLGE],[ENLEM],[BOYLAM],[NUFUS],[AKTIF_MI])",
            "    VALUES (s.[ULKE_ID],N'IL',s.[DIS_KOD],s.[PLAKA],s.[IL_ADI],s.[SEO_SLUG],s.[BOLGE],s.[ENLEM],s.[BOYLAM],s.[NUFUS],1);",
            "GO",
            f"-- {len(rows)} il",
            "",
        ]
    )
    path.write_text("\n".join(lines), encoding="utf-8-sig")
    print(f"iller: {len(rows)} -> {path.name}")


def generate_ilceler(provinces: list[dict], districts: list[dict]) -> None:
    path = OUT / "20260522_seed_ilceler_turkiye.sql"
    prov_names = {int(p["id"]): p["name"] for p in provinces}
    coord_cache = load_coords_cache()
    lines = sql_header("dbo.ILCELER")
    lines.extend(
        [
            "IF OBJECT_ID(N'dbo.ILCELER', N'U') IS NULL RETURN;",
            "IF NOT EXISTS (SELECT 1 FROM [dbo].[ILLER]) RETURN;",
            "GO",
            "",
            "IF OBJECT_ID('tempdb..#ilce_seed') IS NOT NULL DROP TABLE #ilce_seed;",
            "CREATE TABLE #ilce_seed (",
            "    API_KODU int NOT NULL PRIMARY KEY,",
            "    PLAKA smallint NOT NULL,",
            "    ILCE_ADI nvarchar(100) NOT NULL,",
            "    SEO_SLUG nvarchar(140) NOT NULL,",
            "    MERKEZ_MI bit NOT NULL,",
            "    NUFUS int NULL,",
            "    ENLEM decimal(10,8) NULL,",
            "    BOYLAM decimal(11,8) NULL",
            ");",
            "GO",
            "",
            "INSERT INTO #ilce_seed (API_KODU, PLAKA, ILCE_ADI, SEO_SLUG, MERKEZ_MI, NUFUS, ENLEM, BOYLAM) VALUES",
        ]
    )
    rows = []
    for d in districts:
        api_id = int(d["id"])
        plaka = int(d["provinceId"])
        name = d["name"]
        slug = d.get("slug") or slugify(name, str(api_id))
        pname = prov_names.get(plaka, "")
        merkez = 1 if name.strip().casefold() == pname.strip().casefold() else 0
        pop = d.get("population")
        pop_sql = "NULL" if pop is None else str(int(pop))
        c = coord_cache["districts"].get(str(api_id))
        lat_sql = "NULL" if not c else str(c["lat"])
        lon_sql = "NULL" if not c else str(c["lon"])
        rows.append(
            f"({api_id},{plaka},N'{esc(name)}',N'{esc(slug)}',{merkez},{pop_sql},{lat_sql},{lon_sql})"
        )
    batch = 35
    for i in range(0, len(rows), batch):
        if i > 0:
            lines.append("INSERT INTO #ilce_seed (API_KODU, PLAKA, ILCE_ADI, SEO_SLUG, MERKEZ_MI, NUFUS, ENLEM, BOYLAM) VALUES")
        chunk = rows[i : i + batch]
        lines.append(",\n".join(chunk) + ";\n")
    lines.extend(
        [
            "GO",
            "",
            "DECLARE @trUlkeId2 bigint = (",
            "    SELECT TOP (1) [ID] FROM [dbo].[ULKELER]",
            "    WHERE [AKTIF_MI] = 1 AND RTRIM([ISO2_KODU]) = N'TR'",
            "    ORDER BY [VARSAYILAN_ULKE] DESC, [ID] ASC",
            ");",
            "GO",
            "",
            "MERGE [dbo].[ILCELER] AS t",
            "USING (",
            "    SELECT @trUlkeId2 AS [ULKE_ID], i.[ID] AS IL_ID, s.[API_KODU], s.[ILCE_ADI], s.[SEO_SLUG],",
            "           s.[MERKEZ_MI], s.[NUFUS], s.[ENLEM], s.[BOYLAM]",
            "    FROM #ilce_seed s",
            "    INNER JOIN [dbo].[ILLER] i ON i.[PLAKA_KODU] = s.[PLAKA] AND i.[ULKE_ID] = @trUlkeId2",
            ") AS s ON t.[API_KODU] = s.[API_KODU]",
            "WHEN MATCHED THEN UPDATE SET",
            "    [ULKE_ID]=s.[ULKE_ID],[IL_ID]=s.[IL_ID],[DIS_KOD]=s.[API_KODU],[ILCE_ADI]=s.[ILCE_ADI],",
            "    [SEO_SLUG]=s.[SEO_SLUG],[MERKEZ_MI]=s.[MERKEZ_MI],[NUFUS]=s.[NUFUS],",
            "    [ENLEM]=COALESCE(s.[ENLEM],t.[ENLEM]),[BOYLAM]=COALESCE(s.[BOYLAM],t.[BOYLAM]),",
            "    [AKTIF_MI]=1,[GUNCELLENME_TARIHI]=sysutcdatetime()",
            "WHEN NOT MATCHED THEN INSERT ([ULKE_ID],[IL_ID],[DIS_KOD],[API_KODU],[ILCE_ADI],[SEO_SLUG],[MERKEZ_MI],[NUFUS],[ENLEM],[BOYLAM],[AKTIF_MI])",
            "    VALUES (s.[ULKE_ID],s.[IL_ID],s.[API_KODU],s.[API_KODU],s.[ILCE_ADI],s.[SEO_SLUG],s.[MERKEZ_MI],s.[NUFUS],s.[ENLEM],s.[BOYLAM],1);",
            "GO",
            f"-- {len(rows)} ilce",
            "",
        ]
    )
    path.write_text("\n".join(lines), encoding="utf-8-sig")
    print(f"ilceler: {len(rows)} -> {path.name}")


def generate_mahalleler(neighborhoods: list[dict]) -> None:
    path = OUT / "20260522_seed_mahalleler_turkiye.sql"
    coord_cache = load_coords_cache()
    lines = sql_header("dbo.MAHALLELER")
    lines.extend(
        [
            "IF OBJECT_ID(N'dbo.MAHALLELER', N'U') IS NULL RETURN;",
            "IF NOT EXISTS (SELECT 1 FROM [dbo].[ILCELER]) RETURN;",
            "GO",
            "",
            "IF OBJECT_ID('tempdb..#mahalle_seed') IS NOT NULL DROP TABLE #mahalle_seed;",
            "CREATE TABLE #mahalle_seed (",
            "    API_KODU int NOT NULL PRIMARY KEY,",
            "    PLAKA smallint NOT NULL,",
            "    ILCE_API int NOT NULL,",
            "    MAHALLE_ADI nvarchar(120) NOT NULL,",
            "    SEO_SLUG nvarchar(180) NOT NULL,",
            "    POSTA_KODU nvarchar(10) NULL,",
            "    ENLEM decimal(10,8) NULL,",
            "    BOYLAM decimal(11,8) NULL",
            ");",
            "GO",
            "",
        ]
    )
    rows = []
    for n in neighborhoods:
        api_id = int(n["id"])
        plaka = int(n["provinceId"])
        ilce_api = int(n["districtId"])
        name = n["name"]
        slug = n.get("slug") or slugify(name, str(api_id))
        pc = n.get("postalCode")
        pc_sql = "NULL" if not pc else f"N'{esc(str(pc).strip())}'"
        c = coord_cache["neighborhoods"].get(str(api_id))
        lat_sql = "NULL" if not c else str(c["lat"])
        lon_sql = "NULL" if not c else str(c["lon"])
        rows.append(
            f"({api_id},{plaka},{ilce_api},N'{esc(name)}',N'{esc(slug)}',{pc_sql},{lat_sql},{lon_sql})"
        )

    lines.append("INSERT INTO #mahalle_seed (API_KODU, PLAKA, ILCE_API, MAHALLE_ADI, SEO_SLUG, POSTA_KODU, ENLEM, BOYLAM) VALUES")
    batch = 300
    for i in range(0, len(rows), batch):
        if i > 0:
            lines.append("INSERT INTO #mahalle_seed (API_KODU, PLAKA, ILCE_API, MAHALLE_ADI, SEO_SLUG, POSTA_KODU, ENLEM, BOYLAM) VALUES")
        chunk = rows[i : i + batch]
        lines.append(",\n".join(chunk) + ";\n")
    lines.extend(
        [
            "GO",
            "",
            "DECLARE @trUlkeId3 bigint = (",
            "    SELECT TOP (1) [ID] FROM [dbo].[ULKELER]",
            "    WHERE [AKTIF_MI] = 1 AND RTRIM([ISO2_KODU]) = N'TR'",
            "    ORDER BY [VARSAYILAN_ULKE] DESC, [ID] ASC",
            ");",
            "GO",
            "",
            "MERGE [dbo].[MAHALLELER] AS t",
            "USING (",
            "    SELECT",
            "        @trUlkeId3 AS [ULKE_ID],",
            "        i.[ID] AS IL_ID,",
            "        c.[ID] AS ILCE_ID,",
            "        s.[API_KODU], s.[MAHALLE_ADI], s.[SEO_SLUG], s.[POSTA_KODU], s.[ENLEM], s.[BOYLAM]",
            "    FROM #mahalle_seed s",
            "    INNER JOIN [dbo].[ILLER] i ON i.[PLAKA_KODU] = s.[PLAKA] AND i.[ULKE_ID] = @trUlkeId3",
            "    INNER JOIN [dbo].[ILCELER] c ON c.[API_KODU] = s.[ILCE_API] AND c.[ULKE_ID] = @trUlkeId3",
            ") AS s ON t.[API_KODU] = s.[API_KODU]",
            "WHEN MATCHED THEN UPDATE SET",
            "    [ULKE_ID]=s.[ULKE_ID],[IL_ID]=s.[IL_ID],[ILCE_ID]=s.[ILCE_ID],[MAHALLE_ADI]=s.[MAHALLE_ADI],",
            "    [SEO_SLUG]=s.[SEO_SLUG],[POSTA_KODU]=s.[POSTA_KODU],",
            "    [ENLEM]=COALESCE(s.[ENLEM],t.[ENLEM]),[BOYLAM]=COALESCE(s.[BOYLAM],t.[BOYLAM]),",
            "    [AKTIF_MI]=1,[GUNCELLENME_TARIHI]=sysutcdatetime()",
            "WHEN NOT MATCHED THEN INSERT ([ULKE_ID],[IL_ID],[ILCE_ID],[API_KODU],[MAHALLE_ADI],[SEO_SLUG],[POSTA_KODU],[ENLEM],[BOYLAM],[AKTIF_MI])",
            "    VALUES (s.[ULKE_ID],s.[IL_ID],s.[ILCE_ID],s.[API_KODU],s.[MAHALLE_ADI],s.[SEO_SLUG],s.[POSTA_KODU],s.[ENLEM],s.[BOYLAM],1);",
            "GO",
            f"-- {len(rows)} mahalle",
            "",
        ]
    )
    path.write_text("\n".join(lines), encoding="utf-8-sig")
    print(f"mahalleler: {len(rows)} -> {path.name} ({path.stat().st_size // 1024} KB)")


def main() -> None:
    provinces = load_json("provinces")
    districts = load_json("districts")
    neighborhoods = load_json("neighborhoods")
    if len(provinces) != 81:
        raise RuntimeError(f"Beklenen 81 il, gelen {len(provinces)}")
    if len(districts) < 970:
        raise RuntimeError(f"Ilce sayisi dusuk: {len(districts)}")
    if len(neighborhoods) < 30000:
        raise RuntimeError(f"Mahalle sayisi dusuk: {len(neighborhoods)}")
    generate_iller(provinces)
    generate_ilceler(provinces, districts)
    generate_mahalleler(neighborhoods)
    print("Tamam.")


if __name__ == "__main__":
    main()
