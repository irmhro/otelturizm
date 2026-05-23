# -*- coding: utf-8 -*-
"""Yalnizca C# icindeki SQL string literal bloklarinda tablo/sutun adlarini gunceller."""
from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(r"D:\otelturizm")
MAP_FILE = ROOT / "tools" / "Db" / "schema_name_mapping.json"

EXTRA_TABLES = {
    "users": "KULLANICILAR",
    "user_favori_oteller": "KULLANICI_FAVORI_OTELLER",
    "email_services": "EPOSTA_SERVISLERI",
}

EXTRA_COLUMNS = {
    "onaylayan_admin_user_id": "ONAYLAYAN_ADMIN_KULLANICI_ID",
    "talep_eden_user_id": "TALEP_EDEN_KULLANICI_ID",
    "user_id": "KULLANICI_ID",
    "profil_resim_url": "PROFIL_RESIM_URL",
    "email_dogrulama_tarihi": "EPOSTA_DOGRULAMA_TARIHI",
}

SKIP_COLUMN_KEYS = {"ad", "id", "name", "sum", "type", "html", "role", "key", "value", "date", "text"}

TARGET_DIRS = [
    ROOT / "Services",
    ROOT / "Controllers",
    ROOT / "Data",
    ROOT / "Middleware",
    ROOT / "Filters",
]

SQL_HINT = re.compile(r"\b(SELECT|INSERT\s+INTO|UPDATE|DELETE\s+FROM|FROM|JOIN|MERGE|WHERE)\b", re.I)

METADATA_REPLACEMENTS = {
    'RelatedTable = "users"': 'RelatedTable = "KULLANICILAR"',
    "RelatedTable = 'users'": "RelatedTable = 'KULLANICILAR'",
    'ContextTable = "users"': 'ContextTable = "KULLANICILAR"',
    "ContextTable = 'users'": "ContextTable = 'KULLANICILAR'",
}


def load_maps() -> tuple[dict[str, str], dict[str, str]]:
    data = json.loads(MAP_FILE.read_text(encoding="utf-8"))
    tables = {k.lower(): v for k, v in EXTRA_TABLES.items()}
    columns = {k.lower(): v for k, v in EXTRA_COLUMNS.items()}
    for old_t, info in data.get("tables", {}).items():
        new_t = info["new"]
        # Case-only renames (oteller -> OTELLER) must still be applied in SQL literals.
        if old_t != new_t:
            tables[old_t.lower()] = new_t
        for old_c, new_c in info.get("columns", {}).items():
            ol = old_c.lower()
            if old_c != new_c and ol not in SKIP_COLUMN_KEYS:
                columns[ol] = new_c
    return tables, columns


def transform_sql_block(text: str, tables: dict[str, str], columns: dict[str, str]) -> str:
    if not SQL_HINT.search(text):
        return text
    for old, new in sorted(tables.items(), key=lambda x: -len(x[0])):
        text = re.sub(rf"\[dbo\]\.\[{re.escape(old)}\]", f"[dbo].[{new}]", text, flags=re.I)
        text = re.sub(rf"\bdbo\.{re.escape(old)}\b", f"[dbo].[{new}]", text, flags=re.I)
        for kw in ("FROM", "JOIN", "INTO", "UPDATE", "DELETE FROM"):
            text = re.sub(
                rf"(\b{kw}\s+){re.escape(old)}\b",
                rf"\1[dbo].[{new}]",
                text,
                flags=re.I,
            )
    for old, new in sorted(columns.items(), key=lambda x: -len(x[0])):
        text = re.sub(rf"\[{re.escape(old)}\]", f"[{new}]", text, flags=re.I)
        text = re.sub(rf"\.{re.escape(old)}\b", f".[{new}]", text, flags=re.I)
        if len(old) >= 5:
            text = re.sub(
                rf"(?<=[\s,(\n\r]){re.escape(old)}(?=[\s,)\n\r;=])",
                f"[{new}]",
                text,
                flags=re.I,
            )
    return text


def transform_file_content(text: str, tables: dict[str, str], columns: dict[str, str]) -> str:
    for old, new in METADATA_REPLACEMENTS.items():
        text = text.replace(old, new)

    # """ ... """ bloklari (ardisik olmayan)
    parts = re.split(r'(""")', text)
    out: list[str] = []
    in_triple = False
    for part in parts:
        if part == '"""':
            in_triple = not in_triple
            out.append(part)
        elif in_triple:
            out.append(transform_sql_block(part, tables, columns))
        else:
            # @"..." ve "..." icinde SQL
            def repl_dq(m: re.Match[str]) -> str:
                body = m.group(1)
                if SQL_HINT.search(body):
                    return '"' + transform_sql_block(body, tables, columns) + '"'
                return m.group(0)

            def repl_verbatim(m: re.Match[str]) -> str:
                body = m.group(1)
                if SQL_HINT.search(body):
                    return '@"' + transform_sql_block(body, tables, columns) + '"'
                return m.group(0)

            chunk = part
            chunk = re.sub(r'@"(.*?)"', repl_verbatim, chunk, flags=re.DOTALL)
            chunk = re.sub(r'"(.*?)"', repl_dq, chunk, flags=re.DOTALL)
            out.append(chunk)
    return "".join(out)


def transform_file(path: Path, tables: dict[str, str], columns: dict[str, str]) -> bool:
    original = path.read_text(encoding="utf-8", errors="replace")
    updated = transform_file_content(original, tables, columns)
    if updated != original:
        path.write_text(updated, encoding="utf-8")
        return True
    return False


def main() -> None:
    tables, columns = load_maps()
    changed = 0
    for base in TARGET_DIRS:
        for path in base.rglob("*.cs"):
            if transform_file(path, tables, columns):
                changed += 1
                print(path.relative_to(ROOT))
    print(f"changed={changed}")


if __name__ == "__main__":
    main()
