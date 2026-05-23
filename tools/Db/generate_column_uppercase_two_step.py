# -*- coding: utf-8 -*-
"""CI collation icin sutun/tablo BUYUK HARF: iki adimli sp_rename."""
from __future__ import annotations
import json
import re
from pathlib import Path

ROOT = Path(r"D:\otelturizm")
SCHEMA_FILE = ROOT / "tmp" / "schema_columns.txt"
MAP_FILE = ROOT / "Database" / "MigrationsSql" / "20260522_schema_name_mapping.json"
OUT_SQL = ROOT / "Database" / "MigrationsSql" / "20260523_sqlserver_force_uppercase_columns_two_step.sql"


def cs_different(a: str, b: str) -> bool:
    return a != b


def case_only_change(old: str, new: str) -> bool:
    return old.lower() == new.lower() and old != new


def sql_escape(name: str) -> str:
    return name.replace("'", "''")


def rename_object_lines(kind: str, schema_table: str, old: str, new: str) -> list[str]:
    """kind: OBJECT or COLUMN; schema_table: dbo.TABLO"""
    lines: list[str] = []
    if not cs_different(old, new):
        return lines
    if kind == "COLUMN":
        check_old = (
            f"COL_LENGTH(N'{schema_table}', N'{sql_escape(old)}') IS NOT NULL"
        )
    else:
        check_old = f"OBJECT_ID(N'{schema_table}', N'U') IS NOT NULL"

    if case_only_change(old, new):
        temp = f"{old}__cs_tmp"
        if kind == "COLUMN":
            lines.append(f"IF {check_old}")
            lines.append(
                f"    EXEC sp_rename N'{schema_table}.{sql_escape(old)}', N'{sql_escape(temp)}', N'COLUMN';"
            )
            lines.append(
                f"IF COL_LENGTH(N'{schema_table}', N'{sql_escape(temp)}') IS NOT NULL"
            )
            lines.append(
                f"    EXEC sp_rename N'{schema_table}.{sql_escape(temp)}', N'{sql_escape(new)}', N'COLUMN';"
            )
        else:
            new_full = schema_table.rsplit(".", 1)[0] + "." + new
            lines.append(
                f"IF OBJECT_ID(N'{schema_table}', N'U') IS NOT NULL AND OBJECT_ID(N'{new_full}', N'U') IS NULL"
            )
            lines.append(
                f"    EXEC sp_rename N'{schema_table}', N'{sql_escape(temp)}', N'OBJECT';"
            )
            lines.append(
                f"IF OBJECT_ID(N'{schema_table.rsplit('.',1)[0]}.{temp}', N'U') IS NOT NULL"
            )
            lines.append(
                f"    EXEC sp_rename N'{schema_table.rsplit('.',1)[0]}.{sql_escape(temp)}', N'{sql_escape(new)}', N'OBJECT';"
            )
    else:
        if kind == "COLUMN":
            lines.append(
                f"IF {check_old} AND COL_LENGTH(N'{schema_table}', N'{sql_escape(new)}') IS NULL"
            )
            lines.append(
                f"    EXEC sp_rename N'{schema_table}.{sql_escape(old)}', N'{sql_escape(new)}', N'COLUMN';"
            )
        else:
            new_full = schema_table.rsplit(".", 1)[0] + "." + new
            lines.append(
                f"IF OBJECT_ID(N'{schema_table}', N'U') IS NOT NULL AND OBJECT_ID(N'{new_full}', N'U') IS NULL"
            )
            lines.append(
                f"    EXEC sp_rename N'{schema_table}', N'{sql_escape(new)}', N'OBJECT';"
            )
    return lines


def load_current_schema() -> dict[str, list[str]]:
    tables: dict[str, list[str]] = {}
    for line in SCHEMA_FILE.read_text(encoding="utf-8", errors="replace").splitlines():
        parts = line.strip().split("|")
        if len(parts) >= 2:
            tables.setdefault(parts[0], []).append(parts[1])
    return tables


def main() -> None:
    mapping = json.loads(MAP_FILE.read_text(encoding="utf-8"))
    current = load_current_schema()

    lines: list[str] = [
        "-- CI collation: sutun/tablo adlarini gercek BUYUK harfe cevir (iki adimli rename)",
        "-- Once: 20260522_* migration'lari uygulanmis olmali.",
        "SET NOCOUNT ON;",
        "SET XACT_ABORT ON;",
        "GO",
        "",
        "/* Triggers gecici kaldir */",
    ]
    for tr in [
        "TR_REZERVASYONLAR_PREVENT_DELETE_SQLSERVER",
        "TRG_KULLANICI_FAVORI_OTELLER_SYNC_OTELLER_FAVORI_SAYISI",
        "TRG_ODA_FIYAT_MUSAITLIK_MAX_FUTURE_365",
        "TRG_FIRMA_ODA_FIYAT_MUSAITLIK_MAX_FUTURE_365",
        "TR_REZERVASYONLAR_REZERVASYON_DURUMU_SYNC",
        "TR_REZERVASYONLAR_ODEME_DURUMU_SYNC",
        "tr_rezervasyonlar_prevent_delete_sqlserver",
        "trg_user_favori_oteller_sync_oteller_favori_sayisi",
        "trg_oda_fiyat_musaitlik_max_future_365",
        "trg_firma_oda_fiyat_musaitlik_max_future_365",
        "tr_rezervasyonlar_rezervasyon_durumu_sync",
        "tr_rezervasyonlar_odeme_durumu_sync",
    ]:
        lines.append(f"IF OBJECT_ID(N'dbo.{tr}', N'TR') IS NOT NULL DROP TRIGGER dbo.{tr};")
    lines.append("GO\n")

    lines.append("/* ---- Tablolar (yalnizca buyuk harf farki) ---- */")
    for old_t, cols in sorted(current.items()):
        info = mapping["tables"].get(old_t)
        if not info:
            continue
        new_t = info["new"]
        lines.extend(rename_object_lines("OBJECT", f"dbo.{old_t}", old_t, new_t))
    lines.append("GO\n")

    lines.append("/* ---- Sutunlar (tablo bazli batch) ---- */")
    for old_t, col_list in sorted(current.items()):
        info = mapping["tables"].get(old_t)
        if not info:
            continue
        new_t = info["new"]
        col_targets = info["columns"]
        pending: list[tuple[str, str]] = []
        for old_c in col_list:
            new_c = col_targets.get(old_c, old_c.upper())
            if cs_different(old_c, new_c):
                pending.append((old_c, new_c))
        if not pending:
            continue
        lines.append(f"/* {old_t} -> {new_t} ({len(pending)} sutun) */")
        for old_c, new_c in pending:
            lines.extend(
                rename_object_lines("COLUMN", f"dbo.{new_t}", old_c, new_c)
            )
        lines.append("GO")

    lines.append(
        "PRINT N'Sutun BUYUK HARF tamam. 20260522_sqlserver_triggers_after_uppercase_rename.sql calistirin.';"
    )
    lines.append("GO")

    OUT_SQL.write_text("\n".join(lines) + "\n", encoding="utf-8")
    col_count = sum(
        1
        for t, cols in current.items()
        for c in cols
        if mapping["tables"].get(t)
        and cs_different(c, mapping["tables"][t]["columns"].get(c, c.upper()))
    )
    print(f"Column renames (CS different): {col_count}")
    print(f"Wrote {OUT_SQL}")


if __name__ == "__main__":
    main()
