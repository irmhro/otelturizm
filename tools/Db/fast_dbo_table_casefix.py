# -*- coding: utf-8 -*-
"""Fast dbo.<table> case fix using schema_name_mapping.json (whole-file replace)."""
from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(r"D:\otelturizm")
MAP_FILE = ROOT / "tools" / "Db" / "schema_name_mapping.json"
TARGET_DIRS = [
    ROOT / "Services",
    ROOT / "Controllers",
    ROOT / "Data",
    ROOT / "Middleware",
    ROOT / "Filters",
    ROOT / "BackgroundServices",
]

EXTRA = {
    "users": "KULLANICILAR",
    "email_services": "EPOSTA_SERVISLERI",
}


def load_tables() -> dict[str, str]:
    data = json.loads(MAP_FILE.read_text(encoding="utf-8"))
    tables: dict[str, str] = {k.lower(): v["new"] for k, v in data.get("tables", {}).items()}
    for k, v in EXTRA.items():
        tables[k.lower()] = v
    return tables


def fix_content(text: str, tables: dict[str, str]) -> str:
    for old, new in sorted(tables.items(), key=lambda x: -len(x[0])):
        if old == new:
            continue
        # dbo.table / [dbo].[table] / OBJECT_ID(N'dbo.table'
        text = re.sub(
            rf"\[dbo\]\.\[{re.escape(old)}\]",
            f"[dbo].[{new}]",
            text,
            flags=re.I,
        )
        text = re.sub(
            rf"\bdbo\.{re.escape(old)}\b",
            f"dbo.{new}",
            text,
            flags=re.I,
        )
    return text


def main() -> None:
    tables = load_tables()
    changed = 0
    for base in TARGET_DIRS:
        if not base.exists():
            continue
        for path in base.rglob("*.cs"):
            original = path.read_text(encoding="utf-8", errors="replace")
            updated = fix_content(original, tables)
            if updated != original:
                path.write_text(updated, encoding="utf-8")
                changed += 1
                print(path.relative_to(ROOT))
    print(f"changed={changed}")


if __name__ == "__main__":
    main()
