# -*- coding: utf-8 -*-
"""Canli MSSQL'den tablo CREATE migration uretir."""
from __future__ import annotations
import re
import subprocess
from pathlib import Path

ROOT = Path(r"D:\otelturizm")
TABLES_DIR = ROOT / "Database" / "MigrationsSql" / "tablo" / "migrationlar"

# Yerel gelistirme: canli sunucuya baglanma.
SERVER = r"(localdb)\MSSQLLocalDB"
DATABASE = "otelturizm_2026db"
USE_WINDOWS_AUTH = True
USER = ""
PASSWORD = ""

COLUMN_SQL = r"""
SET NOCOUNT ON;
DECLARE @t sysname = N'{table}';
SELECT
    c.column_id,
    c.name AS col_name,
    ty.name AS type_name,
    c.max_length,
    c.precision,
    c.scale,
    c.is_nullable,
    c.is_identity,
    c.is_computed,
    cc.definition AS computed_def,
    dc.definition AS default_def,
    dc.name AS default_name
FROM sys.tables tb
JOIN sys.columns c ON c.object_id = tb.object_id
JOIN sys.types ty ON c.user_type_id = ty.user_type_id
LEFT JOIN sys.computed_columns cc ON cc.object_id = c.object_id AND cc.column_id = c.column_id
LEFT JOIN sys.default_constraints dc ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
WHERE tb.schema_id = SCHEMA_ID('dbo') AND tb.name = @t
ORDER BY c.column_id;
"""

PK_SQL = r"""
SET NOCOUNT ON;
DECLARE @t sysname = N'{table}';
SELECT i.name, STRING_AGG(QUOTENAME(c.name), ', ') WITHIN GROUP (ORDER BY ic.key_ordinal)
FROM sys.indexes i
JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
JOIN sys.tables tb ON tb.object_id = i.object_id
WHERE tb.name = @t AND i.is_primary_key = 1
GROUP BY i.name;
"""


def sqlcmd_query(query: str) -> list[str]:
    cmd = ["sqlcmd", "-S", SERVER, "-d", DATABASE, "-Q", query, "-s", "|", "-h", "-1"]
    if USE_WINDOWS_AUTH:
        cmd.append("-E")
    else:
        cmd.extend(["-U", USER, "-P", PASSWORD])
    proc = subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8", errors="replace")
    lines = []
    for line in (proc.stdout or "").splitlines():
        line = line.strip()
        if not line or line.startswith("(") and "rows affected" in line:
            continue
        lines.append(line)
    return lines


def sql_type(row: dict) -> str:
    t = row["type_name"]
    ml = row["max_length"]
    prec = row["precision"]
    scale = row["scale"]
    if t in ("varchar", "char", "binary", "varbinary"):
        size = "max" if ml == -1 else str(ml)
        return f"{t}({size})"
    if t in ("nvarchar", "nchar"):
        size = "max" if ml == -1 else str(ml // 2)
        return f"{t}({size})"
    if t in ("decimal", "numeric"):
        return f"{t}({prec},{scale})"
    if t in ("datetime2", "datetimeoffset", "time"):
        return f"{t}({scale})"
    return t


def parse_columns(table: str) -> list[dict]:
    rows = sqlcmd_query(COLUMN_SQL.format(table=table))
    out: list[dict] = []
    for line in rows:
        parts = [p.strip() for p in line.split("|")]
        if len(parts) < 8:
            continue
        out.append(
            {
                "column_id": int(parts[0]),
                "col_name": parts[1],
                "type_name": parts[2],
                "max_length": int(parts[3]) if parts[3].isdigit() else -1,
                "precision": int(parts[4]) if parts[4].isdigit() else 0,
                "scale": int(parts[5]) if parts[5].isdigit() else 0,
                "is_nullable": parts[6] == "1",
                "is_identity": parts[7] == "1",
                "is_computed": len(parts) > 8 and parts[8] == "1",
                "computed_def": parts[9] if len(parts) > 9 else "",
                "default_def": parts[10] if len(parts) > 10 else "",
                "default_name": parts[11] if len(parts) > 11 else "",
            }
        )
    return out


def parse_pk(table: str) -> tuple[str, str]:
    rows = sqlcmd_query(PK_SQL.format(table=table))
    if not rows:
        return f"PK_{table}", "[ID]"
    parts = [p.strip() for p in rows[0].split("|")]
    if len(parts) >= 2:
        return parts[0], parts[1]
    return f"PK_{table}", "[ID]"


def build_create(table: str) -> str:
    cols = parse_columns(table)
    if not cols:
        raise RuntimeError(f"Tablo bulunamadi veya bos: {table}")
    pk_name, pk_cols = parse_pk(table)
    lines: list[str] = []
    for c in cols:
        name = c["col_name"]
        if c["is_computed"]:
            line = f"        [{name}] AS {c['computed_def']}"
        else:
            line = f"        [{name}] {sql_type(c)}"
            if c["is_identity"]:
                line += " IDENTITY(1,1)"
            line += " NULL" if c["is_nullable"] else " NOT NULL"
            if c["default_def"] and c["default_def"].upper() != "NULL":
                dname = (c["default_name"] or "").strip()
                if not dname or dname.upper() == "NULL":
                    dname = f"DF_{table}_{name}"
                line += f" CONSTRAINT [{dname}] DEFAULT {c['default_def']}"
        if c != cols[-1] or pk_name:
            line += ","
        lines.append(line)
    lines.append(f"        CONSTRAINT [{pk_name}] PRIMARY KEY CLUSTERED ({pk_cols} ASC)")
    body = "\n".join(lines)
    return f"""SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
IF OBJECT_ID(N'dbo.{table}', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[{table}] (
{body}
    );
END
GO
"""


def find_stub_files() -> list[Path]:
    return sorted(TABLES_DIR.glob("*.sql"))


def main() -> None:
    updated = 0
    for path in find_stub_files():
        text = path.read_text(encoding="utf-8", errors="replace")
        if "UYARI: Stub" not in text and "henuz snapshot" not in text:
            continue
        m = re.search(r"-- Tablo: dbo\.(\w+)", text)
        if not m:
            continue
        table = m.group(1)
        ddl = build_create(table)
        header = f"-- Tablo: dbo.{table}\n-- Kaynak: canli MSSQL sema\n"
        path.write_text(header + ddl + "\n", encoding="utf-8")
        updated += 1
        print(f"OK {path.name}")
    print(f"Guncellenen stub: {updated}")


if __name__ == "__main__":
    main()
