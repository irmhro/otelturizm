# -*- coding: utf-8 -*-
"""Fast SQL-literal-only schema mapping (avoids catastrophic regex on large files)."""
from __future__ import annotations

import sys
from pathlib import Path

ROOT = Path(r"D:\otelturizm")
sys.path.insert(0, str(ROOT / "tools" / "Db"))
from apply_schema_mapping_to_csharp import (  # noqa: E402
    METADATA_REPLACEMENTS,
    load_maps,
    transform_sql_block,
)

TARGET_DIRS = [
    ROOT / "Services",
    ROOT / "Data",
    ROOT / "Middleware",
    ROOT / "Filters",
]


def transform_verbatim_strings(text: str, tables: dict, columns: dict) -> str:
    out: list[str] = []
    i = 0
    n = len(text)
    while i < n:
        if i + 1 < n and text[i] == "@" and text[i + 1] == '"':
            j = i + 2
            body: list[str] = []
            while j < n:
                c = text[j]
                if c == '"':
                    if j + 1 < n and text[j + 1] == '"':
                        body.append('"')
                        j += 2
                        continue
                    break
                body.append(c)
                j += 1
            raw = "".join(body)
            if "SELECT" in raw.upper() or "FROM" in raw.upper() or "INSERT" in raw.upper():
                raw = transform_sql_block(raw, tables, columns)
            out.append('@"' + raw + '"')
            i = j + 1
            continue
        out.append(text[i])
        i += 1
    return "".join(out)


def transform_quoted_lines(text: str, tables: dict, columns: dict) -> str:
    lines = text.splitlines(keepends=True)
    out: list[str] = []
    for line in lines:
        upper = line.upper()
        if any(k in upper for k in ("SELECT", "INSERT", "UPDATE", "DELETE", "FROM", "JOIN")) and '"' in line:
            # Transform only double-quoted segments on SQL-ish lines.
            parts = line.split('"')
            for i in range(1, len(parts), 2):
                if any(k in parts[i].upper() for k in ("SELECT", "FROM", "INSERT", "UPDATE", "JOIN")):
                    parts[i] = transform_sql_block(parts[i], tables, columns)
            line = '"'.join(parts)
        out.append(line)
    return "".join(out)


def transform_file_content(text: str, tables: dict, columns: dict) -> str:
    for old, new in METADATA_REPLACEMENTS.items():
        text = text.replace(old, new)

    parts = text.split('"""')
    for idx in range(1, len(parts), 2):
        parts[idx] = transform_sql_block(parts[idx], tables, columns)
    text = '"""'.join(parts)

    text = transform_verbatim_strings(text, tables, columns)
    return transform_quoted_lines(text, tables, columns)


def main() -> None:
    tables, columns = load_maps()
    changed = 0
    for base in TARGET_DIRS:
        if not base.exists():
            continue
        for path in sorted(base.rglob("*.cs")):
            original = path.read_text(encoding="utf-8", errors="replace")
            updated = transform_file_content(original, tables, columns)
            if updated != original:
                path.write_text(updated, encoding="utf-8")
                changed += 1
                print(path.relative_to(ROOT))
    print(f"changed={changed}")


if __name__ == "__main__":
    main()
