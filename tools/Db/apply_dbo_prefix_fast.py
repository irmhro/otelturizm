# -*- coding: utf-8 -*-
"""Normalize dbo.lowercase_table -> [dbo].[UPPERCASE_TABLE] in .cs files."""
from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(r"D:\otelturizm")
MAP_FILE = ROOT / "tools" / "Db" / "schema_name_mapping.json"
EXTRA = {
    "users": "KULLANICILAR",
    "email_services": "EPOSTA_SERVISLERI",
    "destek_kategorileri": "DESTEK_KATEGORILERI",
    "yorum_kaldirma_talepleri": "YORUM_KALDIRMA_TALEPLERI",
    "rezervasyon_faturalari": "REZERVASYON_FATURALARI",
    "partner_tesis_kullanicilari": "PARTNER_TESIS_KULLANICILARI",
    "blockyorumkelime": "BLOCKYORUMKELIME",
    "rezervasyonlar_archive": "REZERVASYONLAR_ARSIV",
}
DIRS = [ROOT / "Services", ROOT / "Data", ROOT / "Middleware", ROOT / "Filters"]


def load_tables() -> dict[str, str]:
    data = json.loads(MAP_FILE.read_text(encoding="utf-8"))
    tables = dict(EXTRA)
    for old, info in data.get("tables", {}).items():
        new = info["new"]
        if old != new:
            tables[old.lower()] = new
    return tables


def patch(text: str, tables: dict[str, str]) -> str:
    for old in sorted(tables, key=len, reverse=True):
        new = tables[old]
        text = re.sub(
            rf"(?<![A-Za-z0-9_])dbo\.{re.escape(old)}\b",
            f"[dbo].[{new}]",
            text,
            flags=re.I,
        )
    return text


def main() -> None:
    tables = load_tables()
    n = 0
    for base in DIRS:
        if not base.exists():
            continue
        for path in sorted(base.rglob("*.cs")):
            original = path.read_text(encoding="utf-8", errors="replace")
            if "dbo." not in original.lower():
                continue
            updated = patch(original, tables)
            if updated != original:
                path.write_text(updated, encoding="utf-8")
                n += 1
                print(path.relative_to(ROOT))
    print(f"changed={n}")


if __name__ == "__main__":
    main()
