# -*- coding: utf-8 -*-
"""Transform 000_current_schema_by_table SQL files to uppercase Turkish identifiers."""
from __future__ import annotations
import json
import re
from pathlib import Path

ROOT = Path(r"D:\otelturizm")
MAP_FILE = ROOT / "Database" / "MigrationsSql" / "20260522_schema_name_mapping.json"
SCHEMA_DIR = ROOT / "Database" / "MigrationsSql" / "000_current_schema_by_table"

# sort replacements longest-first to avoid partial matches
def build_replacements() -> list[tuple[str, str]]:
    data = json.loads(MAP_FILE.read_text(encoding="utf-8"))
    pairs: list[tuple[str, str]] = []
    for t_old, info in data["tables"].items():
        t_new = info["new"]
        if t_old != t_new:
            pairs.append((t_old, t_new))
        for c_old, c_new in info["columns"].items():
            if c_old != c_new:
                pairs.append((c_old, c_new))
    pairs.sort(key=lambda x: len(x[0]), reverse=True)
    return pairs


def transform_content(text: str, pairs: list[tuple[str, str]]) -> str:
    out = text
    for old, new in pairs:
        # bracketed identifiers
        out = re.sub(rf"\[{re.escape(old)}\]", f"[{new}]", out, flags=re.IGNORECASE)
        # dbo.name patterns
        out = re.sub(
            rf"\bdbo\.{re.escape(old)}\b",
            f"dbo.{new}",
            out,
            flags=re.IGNORECASE,
        )
        out = re.sub(
            rf"N'dbo\.{re.escape(old)}'",
            f"N'dbo.{new}'",
            out,
            flags=re.IGNORECASE,
        )
        out = re.sub(
            rf"'dbo\.{re.escape(old)}'",
            f"'dbo.{new}'",
            out,
            flags=re.IGNORECASE,
        )
    return out


def main() -> None:
    pairs = build_replacements()
    for path in sorted(SCHEMA_DIR.glob("*.sql")):
        original = path.read_text(encoding="utf-8", errors="replace")
        updated = transform_content(original, pairs)
        if updated != original:
            path.write_text(updated, encoding="utf-8")
    readme = SCHEMA_DIR / "README.md"
    if readme.exists():
        text = readme.read_text(encoding="utf-8")
        readme.write_text(transform_content(text, pairs), encoding="utf-8")
    print("Transformed", len(list(SCHEMA_DIR.glob("*.sql"))), "files")


if __name__ == "__main__":
    main()
