# -*- coding: utf-8 -*-
"""MigrationsSql klasorunu yalnizca guncel tablo + constraints yapisina getirir."""
from __future__ import annotations

import json
import re
import shutil
from pathlib import Path

ROOT = Path(r"D:\otelturizm")
SQL = ROOT / "Database" / "MigrationsSql"
TABLES = SQL / "tablo" / "migrationlar"
CONSTRAINTS = SQL / "constraints"
SNAPSHOT = ROOT / "tools" / "Db" / "schema_source_snapshot"
MAP = ROOT / "tools" / "Db" / "schema_name_mapping.json"

# Eski Ingilizce tablo adlari — tek gecerli migration kalir
DROP_TABLE_FILES = {
    "PLATFORM_EMAIL_HESAPLARI",
    "PLATFORM_EMAIL_MESAJLARI",
}


def apply_mapping(text: str) -> str:
    if not MAP.exists():
        return text
    data = json.loads(MAP.read_text(encoding="utf-8"))
    extra_t = {
        "users": "KULLANICILAR",
        "platform_email_mesajlari": "PLATFORM_EPOSTA_MESAJLARI",
        "platform_email_hesaplari": "PLATFORM_EPOSTA_HESAPLARI",
    }
    extra_c = {
        "onaylayan_admin_user_id": "ONAYLAYAN_ADMIN_KULLANICI_ID",
        "talep_eden_user_id": "TALEP_EDEN_KULLANICI_ID",
        "kapsam_degeri_normalized": "KAPSAM_DEGERI_NORMALIZE",
        "sehir_normalized": "SEHIR_NORMALIZE",
        "ilce_normalized": "ILCE_NORMALIZE",
        "mahalle_normalized": "MAHALLE_NORMALIZE",
        "otel_adi_normalized": "OTEL_ADI_NORMALIZE",
        "konum_normalized": "KONUM_NORMALIZE",
        "fts_search_text": "FTS_ARAMA_METNI",
    }
    tmap = dict(extra_t)
    cmap = dict(extra_c)
    for old_t, info in data.get("tables", {}).items():
        tmap[old_t] = info["new"]
        for oc, nc in info.get("columns", {}).items():
            if oc != nc:
                cmap[oc] = nc
    for ot, nt in sorted(tmap.items(), key=lambda x: -len(x[0])):
        text = re.sub(rf"\[dbo\]\.\[{re.escape(ot)}\]", f"[dbo].[{nt}]", text, flags=re.I)
        text = re.sub(rf"\bdbo\.{re.escape(ot)}\b", f"dbo.{nt}", text, flags=re.I)
    for oc, nc in sorted(cmap.items(), key=lambda x: -len(x[0])):
        if not re.fullmatch(r"[A-Za-z_][A-Za-z0-9_]*", oc):
            continue
        text = re.sub(
            rf"(?<![A-Za-z0-9_])\[{re.escape(oc)}\](?!\s*\()",
            f"[{nc}]",
            text,
            flags=re.I,
        )
    return text.lstrip("\ufeff")


def strip_legacy_headers(text: str) -> str:
    lines = []
    for line in text.splitlines():
        if "generated from current" in line.lower():
            continue
        if "000_current_schema" in line:
            continue
        if "canli MSSQL" in line or "canli DB" in line:
            continue
        lines.append(line)
    return "\n".join(lines).strip() + "\n"


def main() -> None:
    # Eski kok dosyalari kaldir
    for pattern in (
        "202605*.sql",
        "202604*.sql",
        "202603*.sql",
        "9*.sql",
        "*_archive*",
    ):
        for f in SQL.glob(pattern):
            if f.is_file():
                f.unlink()

    if (SQL / "000_current_schema_by_table").exists():
        if SNAPSHOT.exists():
            shutil.rmtree(SNAPSHOT)
        shutil.move(str(SQL / "000_current_schema_by_table"), str(SNAPSHOT))

    CONSTRAINTS.mkdir(parents=True, exist_ok=True)
    for name, src in (
        ("900_foreign_keys.sql", SNAPSHOT / "900_foreign_keys.sql"),
        ("901_indexes.sql", SNAPSHOT / "901_indexes.sql"),
    ):
        if src.exists():
            body = apply_mapping(strip_legacy_headers(src.read_text(encoding="utf-8", errors="replace")))
            (CONSTRAINTS / name).write_text(f"-- {name}\n{body}\n", encoding="utf-8")

    trig_src = ROOT / "Database" / "MigrationsSql" / "902_triggers.sql"
    if not trig_src.exists():
        trig_src = SQL / "902_triggers.sql"
    (CONSTRAINTS / "902_triggers.sql").write_text(
        strip_legacy_headers(trig_src.read_text(encoding="utf-8", errors="replace"))
        if trig_src.exists()
        else "-- Triggers (yerel; tanim yoksa bos)\n",
        encoding="utf-8",
    )
    for old in (SQL / "900_foreign_keys.sql", SQL / "901_indexes.sql", SQL / "902_triggers.sql"):
        old.unlink(missing_ok=True)

    # Tablolar: regenerate
    import regenerate_database_migrations as regen

    regen.OLD_SCHEMA = SNAPSHOT
    regen.MAP_FILE = MAP
    regen.OUT_SQL = SQL
    regen.OUT_TABLES = TABLES
    regen.main()

    for path in list(TABLES.glob("*.sql")):
        m = re.match(r"\d{3}_(.+)\.sql$", path.name, re.I)
        if m and m.group(1).upper() in DROP_TABLE_FILES:
            path.unlink()

    # Tablo dosyalarini sadelestir
    for path in TABLES.glob("*.sql"):
        text = path.read_text(encoding="utf-8", errors="replace")
        text = strip_legacy_headers(text)
        m = re.search(r"(\d{3})_(\w+)\.sql", path.name, re.I)
        table = m.group(2) if m else path.stem
        header = f"-- Tablo: dbo.{table}\n"
        if not text.startswith("-- Tablo:"):
            text = header + text
        path.write_text(text, encoding="utf-8")

    tables = []
    for path in sorted(TABLES.glob("*.sql")):
        m = re.match(r"\d{3}_(.+)\.sql$", path.name, re.I)
        if m:
            tables.append(m.group(1).upper())
    (SQL / "schema_manifest.json").write_text(
        json.dumps({"tables": tables, "count": len(tables)}, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )

    readme = """# MigrationsSql

Yalnizca guncel sema migration dosyalari. Eski tarihli / snapshot klasoru burada tutulmaz.

## Yapi

| Klasor / dosya | Aciklama |
|----------------|----------|
| `tablo/migrationlar/000_SEMA_MIGRASYONLARI.sql` | Migration gecmisi tablosu |
| `tablo/migrationlar/001_TABLO.sql` … | Tablo `CREATE` (BUYUK HARF, idempotent) |
| `veri/migrationlar/` | Seed (`YYYYMMDD_seed_*.sql`) |
| `constraints/900_foreign_keys.sql` | Foreign key |
| `constraints/901_indexes.sql` | Index |
| `constraints/902_triggers.sql` | Trigger |
| `schema_manifest.json` | Tablo listesi |

## Siralama

1. `Database/Bootstrap/001_create_otelturizm_database.sql`
2. `tablo/migrationlar/*.sql` (alfanumerik)
3. `constraints/900` → `901` → `902`
4. `veri/migrationlar/*.sql` (alfanumerik)

## Uretim (yerel)

```powershell
python tools/Db/clean_migrations_sql.py
```

Kaynak snapshot: `tools/Db/schema_source_snapshot` (repo disi, salt okunur uretim kaynagi).
"""
    (SQL / "README.md").write_text(readme, encoding="utf-8")
    print(f"Temiz yapi: {len(tables)} tablo, constraints/900-902")


if __name__ == "__main__":
    main()
