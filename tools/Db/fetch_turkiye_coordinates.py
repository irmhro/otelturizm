# -*- coding: utf-8 -*-
"""
Turkiye il / ilce / mahalle ENLEM-BOYLAM guncelleme (OpenStreetMap Nominatim).

Kullanim:
  python tools/Db/fetch_turkiye_coordinates.py --scope all
  python tools/Db/fetch_turkiye_coordinates.py --scope districts --resume
  python tools/Db/fetch_turkiye_coordinates.py --scope all --write-sql

Not: Nominatim kullanim politikasi geregi istekler arasi bekleme uygulanir (~1.1 sn).
"""
from __future__ import annotations

import argparse
import json
import re
import time
import urllib.parse
import urllib.request
from pathlib import Path

ROOT = Path(r"D:\otelturizm")
TMP = ROOT / "tmp"
OUT = ROOT / "Database" / "MigrationsSql"
CACHE_PATH = TMP / "turkiye_coords_cache.json"
USER_AGENT = "otelturizm-geo-seed/1.0 (local-dev; contact: dev@otelturizm.local)"

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


def normalize_name(text: str) -> str:
    s = (text or "").strip()
    s = re.sub(r"\s+MAH\.?$", "", s, flags=re.IGNORECASE)
    s = re.sub(r"\s+KOYU\.?$", "", s, flags=re.IGNORECASE)
    s = re.sub(r"\s+MAHALLESI$", "", s, flags=re.IGNORECASE)
    return s.strip()


def load_json(name: str) -> list[dict]:
    path = TMP / f"turkiye_{name}.json"
    if not path.exists():
        raise FileNotFoundError(f"Eksik: {path}")
    data = json.loads(path.read_text(encoding="utf-8"))
    return data if isinstance(data, list) else data.get("data", data)


def load_cache() -> dict:
    if not CACHE_PATH.exists():
        return {"provinces": {}, "districts": {}, "neighborhoods": {}, "postal": {}}
    return json.loads(CACHE_PATH.read_text(encoding="utf-8"))


def save_cache(cache: dict) -> None:
    CACHE_PATH.parent.mkdir(parents=True, exist_ok=True)
    CACHE_PATH.write_text(json.dumps(cache, ensure_ascii=False, indent=2), encoding="utf-8")


def nominatim_search(delay: float, **params) -> tuple[float, float, str] | None:
    time.sleep(delay)
    q = {k: v for k, v in params.items() if v}
    q["format"] = "json"
    q["limit"] = 1
    q["countrycodes"] = "tr"
    url = "https://nominatim.openstreetmap.org/search?" + urllib.parse.urlencode(q)
    req = urllib.request.Request(url, headers={"User-Agent": USER_AGENT})
    try:
        with urllib.request.urlopen(req, timeout=15) as resp:
            rows = json.loads(resp.read())
    except Exception:
        return None
    if not rows:
        return None
    row = rows[0]
    try:
        lat = float(row["lat"])
        lon = float(row["lon"])
    except (KeyError, TypeError, ValueError):
        return None
    src = str(row.get("type") or row.get("class") or "nominatim")
    return lat, lon, src


def fetch_provinces(cache: dict, delay: float) -> int:
    provinces = load_json("provinces")
    updated = 0
    for p in provinces:
        key = str(int(p["id"]))
        if key in cache["provinces"]:
            continue
        coords = p.get("coordinates") or {}
        lat = coords.get("latitude")
        lon = coords.get("longitude")
        if lat is None or lon is None:
            hit = nominatim_search(
                delay,
                city=normalize_name(p.get("name", "")),
                country="Turkey",
            )
            if hit:
                lat, lon, src = hit
            else:
                continue
        else:
            src = "turkiyeapi"
        cache["provinces"][key] = {
            "plaka": int(p["id"]),
            "lat": float(lat),
            "lon": float(lon),
            "source": src,
        }
        updated += 1
        save_cache(cache)
    return updated


def fetch_districts(cache: dict, delay: float, prov_names: dict[int, str]) -> int:
    districts = load_json("districts")
    updated = 0
    for d in districts:
        api_id = str(int(d["id"]))
        if api_id in cache["districts"]:
            continue
        plaka = int(d["provinceId"])
        il = prov_names.get(plaka, "")
        ilce = normalize_name(d.get("name", ""))
        hit = nominatim_search(
            delay,
            county=ilce,
            state=il,
            country="Turkey",
        )
        if not hit:
            hit = nominatim_search(delay, q=f"{ilce}, {il}, Turkiye")
        if not hit:
            continue
        lat, lon, src = hit
        cache["districts"][api_id] = {
            "api_kodu": int(d["id"]),
            "plaka": plaka,
            "lat": lat,
            "lon": lon,
            "source": src,
        }
        updated += 1
        if updated % 25 == 0:
            save_cache(cache)
            print(f"  ilce: {updated} yeni (toplam cache {len(cache['districts'])})")
    save_cache(cache)
    return updated


def fetch_neighborhoods(
    cache: dict,
    delay: float,
    districts: list[dict],
    prov_names: dict[int, str],
    phase: str = "full",
) -> int:
    neighborhoods = load_json("neighborhoods")
    dist_by_api = {int(d["id"]): d for d in districts}
    updated = 0

    if phase == "assign-only":
        return _assign_neighborhoods_from_cache(cache, neighborhoods, dist_by_api)

    # 1) Posta kodu gruplari
    postal_groups: dict[str, dict] = {}
    for n in neighborhoods:
        pc = (n.get("postalCode") or "").strip()
        if not pc:
            continue
        d = dist_by_api.get(int(n["districtId"]))
        if not d:
            continue
        plaka = int(d["provinceId"])
        key = f"{pc}|{n['districtId']}|{plaka}"
        if key not in postal_groups:
            postal_groups[key] = {
                "postal": pc,
                "district_id": int(n["districtId"]),
                "plaka": plaka,
                "il": prov_names.get(plaka, ""),
                "ilce": normalize_name(d.get("name", "")),
            }

    total_postal = len(postal_groups)
    done_postal = len(cache["postal"])
    print(f"Posta kodu gruplari: {total_postal}, cache: {done_postal}", flush=True)

    for key, meta in postal_groups.items():
        if key in cache["postal"]:
            continue
        hit = nominatim_search(
            delay,
            postalcode=meta["postal"],
            county=meta["ilce"],
            state=meta["il"],
            country="Turkey",
        )
        if hit:
            lat, lon, src = hit
            cache["postal"][key] = {"lat": lat, "lon": lon, "source": f"postal:{src}"}
        else:
            dkey = str(meta["district_id"])
            dc = cache["districts"].get(dkey)
            if dc:
                cache["postal"][key] = {
                    "lat": dc["lat"],
                    "lon": dc["lon"],
                    "source": "postal:ilce-fallback",
                }
        if key in cache["postal"]:
            updated += 1
            if updated % 25 == 0:
                save_cache(cache)
                print(f"  posta: {len(cache['postal'])} / {len(postal_groups)}", flush=True)

    save_cache(cache)
    print(f"Posta tamam: {len(cache['postal'])} / {total_postal}", flush=True)

    if phase == "postal-only":
        return updated

    return _assign_neighborhoods_from_cache(cache, neighborhoods, dist_by_api) + updated


def _assign_neighborhoods_from_cache(
    cache: dict,
    neighborhoods: list[dict],
    dist_by_api: dict[int, dict],
) -> int:
    updated = 0
    print("Mahalle atamasi basliyor...", flush=True)
    for n in neighborhoods:
        api_id = str(int(n["id"]))
        if api_id in cache["neighborhoods"]:
            continue
        d = dist_by_api.get(int(n["districtId"]))
        if not d:
            continue
        plaka = int(d["provinceId"])
        pc = (n.get("postalCode") or "").strip()
        pkey = f"{pc}|{n['districtId']}|{plaka}"
        if pc and pkey in cache["postal"]:
            p = cache["postal"][pkey]
            cache["neighborhoods"][api_id] = {
                "api_kodu": int(n["id"]),
                "lat": p["lat"],
                "lon": p["lon"],
                "source": p["source"],
            }
            updated += 1
            if updated % 5000 == 0:
                save_cache(cache)
                print(f"  mahalle(posta): {len(cache['neighborhoods'])}")
            continue

        dkey = str(int(d["id"]))
        if dkey in cache["districts"]:
            dc = cache["districts"][dkey]
            cache["neighborhoods"][api_id] = {
                "api_kodu": int(n["id"]),
                "lat": dc["lat"],
                "lon": dc["lon"],
                "source": "ilce-fallback",
            }
            updated += 1
            if updated % 5000 == 0:
                save_cache(cache)
                print(f"  mahalle(ilce-fallback): {len(cache['neighborhoods'])}")

    save_cache(cache)
    print(f"Mahalle atandi: {len(cache['neighborhoods'])}", flush=True)
    return updated


def write_sql(cache: dict) -> None:
    path = OUT / "20260524_seed_koordinat_turkiye.sql"
    lines = [
        "/* Turkiye il / ilce / mahalle koordinat guncelleme - UTF-8, sqlcmd -f 65001 */",
        "SET NOCOUNT ON;",
        "GO",
        "",
    ]

    if cache["provinces"]:
        lines.extend(
            [
                "IF OBJECT_ID('tempdb..#il_koord') IS NOT NULL DROP TABLE #il_koord;",
                "CREATE TABLE #il_koord (PLAKA smallint NOT NULL PRIMARY KEY, ENLEM decimal(10,8) NOT NULL, BOYLAM decimal(11,8) NOT NULL);",
                "GO",
                "INSERT INTO #il_koord (PLAKA, ENLEM, BOYLAM) VALUES",
            ]
        )
        rows = [
            f"({v['plaka']},{v['lat']},{v['lon']})" for v in cache["provinces"].values()
        ]
        batch = 80
        for i in range(0, len(rows), batch):
            if i > 0:
                lines.append("INSERT INTO #il_koord (PLAKA, ENLEM, BOYLAM) VALUES")
            lines.append(",\n".join(rows[i : i + batch]) + ";\n")
        lines.extend(
            [
                "GO",
                "UPDATE i SET i.[ENLEM]=s.[ENLEM], i.[BOYLAM]=s.[BOYLAM], i.[GUNCELLENME_TARIHI]=sysutcdatetime()",
                "FROM [dbo].[ILLER] AS i",
                "INNER JOIN #il_koord AS s ON i.[PLAKA_KODU]=s.[PLAKA]",
                "INNER JOIN [dbo].[ULKELER] AS u ON u.[ID]=i.[ULKE_ID] AND RTRIM(u.[ISO2_KODU])=N'TR';",
                "GO",
                "",
            ]
        )

    if cache["districts"]:
        lines.extend(
            [
                "IF OBJECT_ID('tempdb..#ilce_koord') IS NOT NULL DROP TABLE #ilce_koord;",
                "CREATE TABLE #ilce_koord (API_KODU int NOT NULL PRIMARY KEY, ENLEM decimal(10,8) NOT NULL, BOYLAM decimal(11,8) NOT NULL);",
                "GO",
                "INSERT INTO #ilce_koord (API_KODU, ENLEM, BOYLAM) VALUES",
            ]
        )
        rows = [
            f"({v['api_kodu']},{v['lat']},{v['lon']})"
            for v in cache["districts"].values()
        ]
        batch = 80
        for i in range(0, len(rows), batch):
            if i > 0:
                lines.append("INSERT INTO #ilce_koord (API_KODU, ENLEM, BOYLAM) VALUES")
            lines.append(",\n".join(rows[i : i + batch]) + ";\n")
        lines.extend(
            [
                "GO",
                "UPDATE c SET c.[ENLEM]=s.[ENLEM], c.[BOYLAM]=s.[BOYLAM], c.[GUNCELLENME_TARIHI]=sysutcdatetime()",
                "FROM [dbo].[ILCELER] AS c",
                "INNER JOIN #ilce_koord AS s ON c.[API_KODU]=s.[API_KODU];",
                "GO",
                "",
            ]
        )

    if cache["neighborhoods"]:
        lines.extend(
            [
                "IF OBJECT_ID('tempdb..#mahalle_koord') IS NOT NULL DROP TABLE #mahalle_koord;",
                "CREATE TABLE #mahalle_koord (API_KODU int NOT NULL PRIMARY KEY, ENLEM decimal(10,8) NOT NULL, BOYLAM decimal(11,8) NOT NULL);",
                "GO",
                "INSERT INTO #mahalle_koord (API_KODU, ENLEM, BOYLAM) VALUES",
            ]
        )
        rows = [
            f"({v['api_kodu']},{v['lat']},{v['lon']})"
            for v in cache["neighborhoods"].values()
        ]
        batch = 200
        for i in range(0, len(rows), batch):
            if i > 0:
                lines.append("INSERT INTO #mahalle_koord (API_KODU, ENLEM, BOYLAM) VALUES")
            lines.append(",\n".join(rows[i : i + batch]) + ";\n")
        lines.extend(
            [
                "GO",
                "UPDATE m SET m.[ENLEM]=s.[ENLEM], m.[BOYLAM]=s.[BOYLAM], m.[GUNCELLENME_TARIHI]=sysutcdatetime()",
                "FROM [dbo].[MAHALLELER] AS m",
                "INNER JOIN #mahalle_koord AS s ON m.[API_KODU]=s.[API_KODU];",
                "GO",
                "",
            ]
        )

    lines.append(f"/* iller={len(cache['provinces'])}, ilce={len(cache['districts'])}, mahalle={len(cache['neighborhoods'])} */")
    path.write_text("\n".join(lines), encoding="utf-8-sig")
    print(f"SQL -> {path}")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--scope",
        choices=["provinces", "districts", "neighborhoods", "all"],
        default="all",
    )
    parser.add_argument(
        "--phase",
        choices=["full", "postal-only", "assign-only"],
        default="full",
        help="neighborhoods: posta kodu cek veya sadece cache'den mahalle ata",
    )
    parser.add_argument("--delay", type=float, default=1.15)
    parser.add_argument("--write-sql", action="store_true")
    args = parser.parse_args()

    provinces = load_json("provinces")
    districts = load_json("districts")
    prov_names = {int(p["id"]): p["name"] for p in provinces}
    cache = load_cache()

    if args.scope in ("provinces", "all"):
        print("Iller (TurkiyeAPI + eksikler)...")
        n = fetch_provinces(cache, args.delay)
        print(f"  iller guncellendi: {n}")

    if args.scope in ("districts", "all"):
        print("Ilceler (Nominatim)...")
        n = fetch_districts(cache, args.delay, prov_names)
        print(f"  ilce guncellendi: {n}")

    if args.scope in ("neighborhoods", "all"):
        print("Mahalleler (posta kodu + Nominatim + ilce fallback)...")
        n = fetch_neighborhoods(cache, args.delay, districts, prov_names, args.phase)
        print(f"  mahalle guncellendi: {n}")

    if args.write_sql:
        write_sql(cache)

    print(
        f"Cache: iller={len(cache['provinces'])}, ilce={len(cache['districts'])}, "
        f"mahalle={len(cache['neighborhoods'])}, posta={len(cache['postal'])}"
    )


if __name__ == "__main__":
    main()
