# -*- coding: utf-8 -*-
"""Table-name-only SQL literal updates (fast, no per-column regex)."""
from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(r"D:\otelturizm")
MAP_FILE = ROOT / "tools" / "Db" / "schema_name_mapping.json"
EXTRA = {
    "users": "KULLANICILAR",
    "user_favori_oteller": "KULLANICI_FAVORI_OTELLER",
    "email_services": "EPOSTA_SERVISLERI",
    "destek_kategorileri": "DESTEK_KATEGORILERI",
}
DIRS = [ROOT / "Services", ROOT / "Data", ROOT / "Middleware", ROOT / "Filters"]
SQL_KW = re.compile(r"\b(SELECT|INSERT|UPDATE|DELETE|FROM|JOIN|INTO|MERGE|WHERE|OBJECT_ID|COL_LENGTH|INFORMATION_SCHEMA)\b", re.I)


def load_tables() -> dict[str, str]:
    data = json.loads(MAP_FILE.read_text(encoding="utf-8"))
    tables = {k.lower(): v for k, v in EXTRA.items()}
    for old, info in data.get("tables", {}).items():
        new = info["new"]
        if old != new:
            tables[old.lower()] = new
    return tables


def patch_sql(text: str, tables: dict[str, str]) -> str:
    if not SQL_KW.search(text):
        return text
    for old in sorted(tables, key=len, reverse=True):
        new = tables[old]
        text = re.sub(rf"\[dbo\]\.\[{re.escape(old)}\]", f"[dbo].[{new}]", text, flags=re.I)
        text = re.sub(rf"\bdbo\.{re.escape(old)}\b", f"[dbo].[{new}]", text, flags=re.I)
        for kw in ("FROM", "JOIN", "INTO", "UPDATE", "DELETE FROM"):
            text = re.sub(rf"(\b{kw}\s+){re.escape(old)}\b", rf"\1[dbo].[{new}]", text, flags=re.I)
    return text


def patch_verbatim(text: str, tables: dict[str, str]) -> str:
    out: list[str] = []
    i, n = 0, len(text)
    while i < n:
        if i + 1 < n and text[i] == "@" and text[i + 1] == '"':
            j, body = i + 2, []
            while j < n:
                if text[j] == '"' and not (j + 1 < n and text[j + 1] == '"'):
                    break
                if text[j] == '"' and j + 1 < n and text[j + 1] == '"':
                    body.append('"')
                    j += 2
                    continue
                body.append(text[j])
                j += 1
            raw = "".join(body)
            if SQL_KW.search(raw):
                raw = patch_sql(raw, tables)
            out.append('@"' + raw + '"')
            i = j + 1
            continue
        out.append(text[i])
        i += 1
    return "".join(out)


def patch_file(path: Path, tables: dict[str, str]) -> bool:
    original = path.read_text(encoding="utf-8", errors="replace")
    parts = original.split('"""')
    for i in range(1, len(parts), 2):
        parts[i] = patch_sql(parts[i], tables)
    text = patch_verbatim('"""'.join(parts), tables)
    lines = []
    for line in text.splitlines(keepends=True):
        if '"' in line and SQL_KW.search(line):
            segs = line.split('"')
            for j in range(1, len(segs), 2):
                if SQL_KW.search(segs[j]):
                    segs[j] = patch_sql(segs[j], tables)
            line = '"'.join(segs)
        lines.append(line)
    updated = "".join(lines)
    if updated != original:
        path.write_text(updated, encoding="utf-8")
        return True
    return False


def main() -> None:
    tables = load_tables()
    n = 0
    for base in DIRS:
        if not base.exists():
            continue
        for path in sorted(base.rglob("*.cs")):
            if patch_file(path, tables):
                n += 1
                print(path.relative_to(ROOT))
    print(f"changed={n}")


if __name__ == "__main__":
    main()
