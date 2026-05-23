# -*- coding: utf-8 -*-
"""900/901 dosyalarinda tablo ve sutun adlarini mapping ile gunceller."""
from __future__ import annotations
import json
import re
from pathlib import Path

ROOT = Path(r"D:\otelturizm")
MAP_FILE = ROOT / "tmp" / "schema_name_mapping.json"
FILES = [
    ROOT / "Database" / "MigrationsSql" / "constraints" / "900_foreign_keys.sql",
    ROOT / "Database" / "MigrationsSql" / "constraints" / "901_indexes.sql",
]

EXTRA_TABLES = {
    "users": "KULLANICILAR",
    "platform_email_mesajlari": "PLATFORM_EPOSTA_MESAJLARI",
    "platform_email_hesaplari": "PLATFORM_EPOSTA_HESAPLARI",
    "platform_ekip_uyeleri": "PLATFORM_EKIP_UYELERI",
}

EXTRA_COLUMNS = {
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


def apply_mapping(text: str, tables: dict, columns: dict) -> str:
    for old_t, new_t in sorted(tables.items(), key=lambda x: -len(x[0])):
        text = re.sub(rf"\[dbo\]\.\[{re.escape(old_t)}\]", f"[dbo].[{new_t}]", text, flags=re.I)
        text = re.sub(rf"\bdbo\.{re.escape(old_t)}\b", f"dbo.{new_t}", text, flags=re.I)
    for old_c, new_c in sorted(columns.items(), key=lambda x: -len(x[0])):
        text = re.sub(rf"\[{re.escape(old_c)}\]", f"[{new_c}]", text, flags=re.I)
    return text


def main() -> None:
    data = json.loads(MAP_FILE.read_text(encoding="utf-8"))
    for path in FILES:
        if not path.exists():
            continue
        text = path.read_text(encoding="utf-8", errors="replace")
        table_map = dict(EXTRA_TABLES)
        column_map = dict(EXTRA_COLUMNS)
        for old_t, info in data.get("tables", {}).items():
            table_map[old_t] = info["new"]
            for old_c, new_c in info.get("columns", {}).items():
                if old_c != new_c:
                    column_map[old_c] = new_c
        text = apply_mapping(text, table_map, column_map)
        path.write_text(text, encoding="utf-8")
        print(f"Updated {path.name}")

    tables_dir = ROOT / "Database" / "MigrationsSql" / "tablo" / "migrationlar"
    for path in tables_dir.glob("*.sql"):
        text = path.read_text(encoding="utf-8", errors="replace")
        table_map = dict(EXTRA_TABLES)
        column_map = dict(EXTRA_COLUMNS)
        for old_t, info in data.get("tables", {}).items():
            table_map[old_t] = info["new"]
            for old_c, new_c in info.get("columns", {}).items():
                if old_c != new_c:
                    column_map[old_c] = new_c
        new_text = apply_mapping(text, table_map, column_map)
        if new_text != text:
            path.write_text(new_text, encoding="utf-8")
    print("Updated tablo/migrationlar/*.sql where needed")


if __name__ == "__main__":
    main()
