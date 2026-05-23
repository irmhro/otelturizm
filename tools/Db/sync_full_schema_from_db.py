# -*- coding: utf-8 -*-
"""
Yerel LocalDB semasini Database/MigrationsSql altina yazar (canli sunucu kullanilmaz):
- tablo/migrationlar/*.sql (tum tablolar)
- 900_foreign_keys.sql
- 901_indexes.sql
- 902_triggers.sql
"""
from __future__ import annotations

import json
import re
import subprocess
from pathlib import Path

ROOT = Path(r"D:\otelturizm")
TABLES_DIR = ROOT / "Database" / "MigrationsSql" / "tablo" / "migrationlar"
SQL_DIR = ROOT / "Database" / "MigrationsSql"
TMP = ROOT / "tmp"

SERVER = r"(localdb)\MSSQLLocalDB"
DATABASE = "otelturizm_2026db"
USE_WINDOWS_AUTH = True
USER = ""
PASSWORD = ""

SKIP_PREFIXES = ("CODEXBACKUP_",)
SKIP_CONTAINS = ("_BAK_", "_BACKUP_", "_YEDEK_")
SKIP_TABLES = set()  # SEMA 000'da


def _sqlcmd_base(query: str) -> list[str]:
    cmd = ["sqlcmd", "-S", SERVER, "-d", DATABASE, "-Q", query, "-h", "-1"]
    if USE_WINDOWS_AUTH:
        cmd.append("-E")
    else:
        cmd.extend(["-U", USER, "-P", PASSWORD])
    return cmd


def sqlcmd(query: str, out_file: Path | None = None) -> str:
    cmd = _sqlcmd_base(query)
    if out_file:
        cmd.extend(["-o", str(out_file)])
        subprocess.run(cmd, check=False)
        return out_file.read_text(encoding="utf-8", errors="replace") if out_file.exists() else ""
    proc = subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8", errors="replace")
    return proc.stdout or ""


def sqlcmd_rows(query: str, sep: str = "\t") -> list[list[str]]:
    cmd = _sqlcmd_base(query)
    cmd.extend(["-s", sep])
    proc = subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8", errors="replace")
    rows: list[list[str]] = []
    for line in (proc.stdout or "").splitlines():
        line = line.rstrip("\r")
        if not line.strip():
            continue
        if "rows affected" in line.lower():
            continue
        rows.append([c.strip() for c in line.split(sep)])
    return rows


def should_skip(name: str) -> bool:
    u = name.upper()
    if any(u.startswith(p) for p in SKIP_PREFIXES):
        return True
    if any(p in u for p in SKIP_CONTAINS):
        return True
    return u in SKIP_TABLES


def list_tables() -> list[str]:
    rows = sqlcmd_rows(
        "SET NOCOUNT ON; SELECT name FROM sys.tables WHERE is_ms_shipped=0 ORDER BY name;"
    )
    out = []
    for r in rows:
        if not r:
            continue
        t = r[0]
        if should_skip(t):
            continue
        out.append(t)
    return sorted(set(out))


def sql_type(t: str, ml: int, prec: int, scale: int) -> str:
    if t in ("varchar", "char", "binary", "varbinary"):
        return f"{t}({'max' if ml == -1 else ml})"
    if t in ("nvarchar", "nchar"):
        return f"{t}({'max' if ml == -1 else ml // 2})"
    if t in ("decimal", "numeric"):
        return f"{t}({prec},{scale})"
    if t in ("datetime2", "datetimeoffset", "time"):
        return f"{t}({scale})"
    return t


def build_table_ddl(table: str) -> str:
    rows = sqlcmd_rows(f"""
SET NOCOUNT ON;
DECLARE @t sysname = N'{table}';
SELECT
    c.column_id,
    c.name,
    ty.name,
    c.max_length,
    c.precision,
    c.scale,
    c.is_nullable,
    c.is_identity,
    c.is_computed,
    ISNULL(cc.definition, ''),
    ISNULL(dc.definition, ''),
    ISNULL(dc.name, '')
FROM sys.tables tb
JOIN sys.columns c ON c.object_id = tb.object_id
JOIN sys.types ty ON c.user_type_id = ty.user_type_id
LEFT JOIN sys.computed_columns cc ON cc.object_id = c.object_id AND cc.column_id = c.column_id
LEFT JOIN sys.default_constraints dc ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
WHERE tb.schema_id = SCHEMA_ID('dbo') AND tb.name = @t
ORDER BY c.column_id;
""")
    if not rows:
        raise RuntimeError(f"Tablo yok/bos: {table}")

    pk_rows = sqlcmd_rows(f"""
SET NOCOUNT ON;
DECLARE @t sysname = N'{table}';
SELECT i.name, STRING_AGG(QUOTENAME(c.name), ', ') WITHIN GROUP (ORDER BY ic.key_ordinal)
FROM sys.indexes i
JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
JOIN sys.tables tb ON tb.object_id = i.object_id
WHERE tb.name = @t AND i.is_primary_key = 1
GROUP BY i.name;
""")
    pk_name, pk_cols = f"PK_{table}", "[ID]"
    if pk_rows and len(pk_rows[0]) >= 2:
        pk_name, pk_cols = pk_rows[0][0], pk_rows[0][1]

    lines: list[str] = []
    for i, p in enumerate(rows):
        if len(p) < 8:
            continue
        name = p[1]
        is_computed = p[8] == "1"
        if is_computed:
            line = f"        [{name}] AS {p[9]}"
        else:
            ml = int(p[3]) if p[3].lstrip("-").isdigit() else -1
            prec = int(p[4]) if p[4].isdigit() else 0
            scale = int(p[5]) if p[5].isdigit() else 0
            line = f"        [{name}] {sql_type(p[2], ml, prec, scale)}"
            if p[7] == "1":
                line += " IDENTITY(1,1)"
            line += " NULL" if p[6] == "1" else " NOT NULL"
            ddef = p[10].strip()
            dname = p[11].strip()
            if ddef and ddef.upper() != "NULL":
                if not dname or dname.upper() == "NULL":
                    dname = f"DF_{table}_{name}"
                line += f" CONSTRAINT [{dname}] DEFAULT {ddef}"
        line += ","
        lines.append(line)
    lines.append(f"        CONSTRAINT [{pk_name}] PRIMARY KEY CLUSTERED ({pk_cols} ASC)")
    body = "\n".join(lines)
    return f"""-- Tablo: dbo.{table}
-- Kaynak: canli MSSQL ({DATABASE})
SET ANSI_NULLS ON;
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


def export_foreign_keys() -> str:
    rows = sqlcmd_rows("""
SET NOCOUNT ON;
SELECT
    fk.name,
    OBJECT_NAME(fk.parent_object_id) AS parent_table,
    STRING_AGG(QUOTENAME(pc.name), ', ') WITHIN GROUP (ORDER BY fkc.constraint_column_id) AS parent_cols,
    OBJECT_NAME(fk.referenced_object_id) AS ref_table,
    STRING_AGG(QUOTENAME(rc.name), ', ') WITHIN GROUP (ORDER BY fkc.constraint_column_id) AS ref_cols,
    fk.delete_referential_action_desc,
    fk.update_referential_action_desc
FROM sys.foreign_keys fk
JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
JOIN sys.columns pc ON pc.object_id = fkc.parent_object_id AND pc.column_id = fkc.parent_column_id
JOIN sys.columns rc ON rc.object_id = fkc.referenced_object_id AND rc.column_id = fkc.referenced_column_id
GROUP BY fk.name, fk.parent_object_id, fk.referenced_object_id, fk.delete_referential_action_desc, fk.update_referential_action_desc
ORDER BY parent_table, fk.name;
""")
    chunks = ["-- Foreign keys — canli MSSQL", "SET NOCOUNT ON;", ""]
    for r in rows:
        if len(r) < 5:
            continue
        fk_name, parent, pcols, ref, rcols = r[0], r[1], r[2], r[3], r[4]
        if should_skip(parent) or should_skip(ref):
            continue
        on_del = ""
        if len(r) > 5 and r[5] and r[5] != "NO_ACTION":
            on_del = f" ON DELETE {r[5].replace('_', ' ')}"
        chunks.append(f"IF OBJECT_ID(N'dbo.{fk_name}', N'F') IS NULL")
        chunks.append("BEGIN")
        chunks.append(
            f"    ALTER TABLE [dbo].[{parent}] WITH CHECK ADD CONSTRAINT [{fk_name}] "
            f"FOREIGN KEY ({pcols}) REFERENCES [dbo].[{ref}] ({rcols}){on_del};"
        )
        chunks.append("END")
        chunks.append("GO")
        chunks.append("")
    return "\n".join(chunks)


def export_indexes() -> str:
    rows = sqlcmd_rows("""
SET NOCOUNT ON;
SELECT
    t.name AS table_name,
    i.name AS index_name,
    i.is_unique,
    i.type_desc,
    i.filter_definition,
    STRING_AGG(
        QUOTENAME(c.name) + CASE WHEN ic.is_descending_key = 1 THEN ' DESC' ELSE ' ASC' END,
        ', '
    ) WITHIN GROUP (ORDER BY ic.key_ordinal) AS key_cols
FROM sys.indexes i
JOIN sys.tables t ON t.object_id = i.object_id
JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE t.is_ms_shipped = 0
  AND i.is_primary_key = 0
  AND i.type IN (1, 2)
  AND ic.is_included_column = 0
GROUP BY t.name, i.name, i.is_unique, i.type_desc, i.filter_definition
ORDER BY t.name, i.name;
""")
    chunks = ["-- Indexes — canli MSSQL", "SET NOCOUNT ON;", ""]
    for r in rows:
        if len(r) < 6:
            continue
        table, iname, is_uq, type_desc, filt, cols = r[0], r[1], r[2], r[3], r[4], r[5]
        if should_skip(table):
            continue
        uq = "UNIQUE " if is_uq == "1" else ""
        filt_sql = f" WHERE ({filt})" if filt and filt.strip() else ""
        chunks.append(f"IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'{iname}' AND object_id = OBJECT_ID(N'dbo.{table}'))")
        chunks.append("BEGIN")
        chunks.append(f"    CREATE {uq}NONCLUSTERED INDEX [{iname}] ON [dbo].[{table}] ({cols}){filt_sql};")
        chunks.append("END")
        chunks.append("GO")
        chunks.append("")
    return "\n".join(chunks)


def export_triggers() -> str:
    rows = sqlcmd_rows("""
SET NOCOUNT ON;
SELECT
    OBJECT_NAME(tr.parent_id) AS table_name,
    tr.name AS trigger_name,
    m.definition
FROM sys.triggers tr
JOIN sys.sql_modules m ON m.object_id = tr.object_id
WHERE tr.is_ms_shipped = 0
ORDER BY table_name, trigger_name;
""")
    chunks = ["-- Triggers — canli MSSQL", ""]
    for r in rows:
        if len(r) < 3:
            continue
        table, tname, definition = r[0], r[1], r[2]
        if should_skip(table):
            continue
        defn = definition.strip()
        if not defn.upper().startswith("CREATE"):
            defn = f"CREATE TRIGGER [dbo].[{tname}] ON [dbo].[{table}]\n{defn}"
        chunks.append(f"-- {table}.{tname}")
        chunks.append(f"IF OBJECT_ID(N'[{tname}]', N'TR') IS NOT NULL DROP TRIGGER [dbo].[{tname}];")
        chunks.append("GO")
        chunks.append(defn)
        if not defn.rstrip().endswith("GO"):
            chunks.append("GO")
        chunks.append("")
    return "\n".join(chunks)


def write_tables(tables: list[str]) -> dict[str, str]:
    TABLES_DIR.mkdir(parents=True, exist_ok=True)
    for old in TABLES_DIR.glob("*.sql"):
        old.unlink()

    # 000 migration history
    (TABLES_DIR / "000_SEMA_MIGRASYONLARI.sql").write_text(
        """-- Migration gecmisi
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
IF OBJECT_ID(N'dbo.SEMA_MIGRASYONLARI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[SEMA_MIGRASYONLARI] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [BETIK_ADI] nvarchar(260) NOT NULL,
        [KONTROL_TOPLAMI] char(64) NOT NULL,
        [UYGULANMA_TARIHI] datetime2(0) NOT NULL CONSTRAINT [DF_SEMA_MIGRASYONLARI_UYGULANMA] DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_SEMA_MIGRASYONLARI] PRIMARY KEY CLUSTERED ([ID] ASC),
        CONSTRAINT [UQ_SEMA_MIGRASYONLARI_BETIK_HASH] UNIQUE ([BETIK_ADI], [KONTROL_TOPLAMI])
    );
    CREATE INDEX [IX_SEMA_MIGRASYONLARI_BETIK] ON [dbo].[SEMA_MIGRASYONLARI]([BETIK_ADI]);
END
GO
""",
        encoding="utf-8",
    )

    written: dict[str, str] = {}
    idx = 1
    for table in tables:
        if table == "SEMA_MIGRASYONLARI":
            continue
        ddl = build_table_ddl(table)
        fname = f"{idx:03d}_{table}.sql"
        (TABLES_DIR / fname).write_text(ddl + "\n", encoding="utf-8")
        written[table] = fname
        idx += 1
        print(f"table {fname}")
    return written


def validate(tables: list[str]) -> list[str]:
    issues: list[str] = []
    for table in tables:
        if table == "SEMA_MIGRASYONLARI":
            continue
        db_cols = sqlcmd_rows(f"""
SET NOCOUNT ON;
SELECT c.name FROM sys.columns c
JOIN sys.tables t ON t.object_id = c.object_id
WHERE t.name = N'{table}' ORDER BY c.column_id;
""")
        db_set = {r[0].upper() for r in db_cols if r}
        files = list(TABLES_DIR.glob(f"*_{table}.sql"))
        if not files:
            issues.append(f"MISSING_FILE:{table}")
            continue
        text = files[0].read_text(encoding="utf-8", errors="replace")
        file_cols = set(re.findall(r"\[([A-Za-z0-9_]+)\]", text)) - {
            "dbo", table, "U", "NULL", "N", "PK", "DF", "IX", "FK"
        }
        # crude: only lines inside CREATE with column defs
        create_cols = set()
        in_create = False
        for line in text.splitlines():
            if "CREATE TABLE" in line.upper():
                in_create = True
                continue
            if in_create:
                if line.strip().startswith(");") or line.strip() == ");":
                    break
                m = re.match(r"\s+\[([^\]]+)\]", line)
                if m and "CONSTRAINT" not in line.upper() and " PRIMARY KEY" not in line.upper():
                    create_cols.add(m.group(1).upper())
        missing = db_set - create_cols
        extra = create_cols - db_set
        if missing:
            issues.append(f"{table}:DB_NOT_IN_FILE={sorted(missing)}")
        if extra:
            issues.append(f"{table}:FILE_NOT_IN_DB={sorted(extra)}")
    return issues


def main() -> None:
    TMP.mkdir(parents=True, exist_ok=True)
    tables = list_tables()
    print(f"Remote tables: {len(tables)}")
    (TMP / "remote_tables.txt").write_text("\n".join(tables), encoding="utf-8")

    written = write_tables(tables)
    print("Exporting FK...")
    (SQL_DIR / "900_foreign_keys.sql").write_text(export_foreign_keys(), encoding="utf-8")
    print("Exporting indexes...")
    (SQL_DIR / "901_indexes.sql").write_text(export_indexes(), encoding="utf-8")
    print("Exporting triggers...")
    (SQL_DIR / "902_triggers.sql").write_text(export_triggers(), encoding="utf-8")

    issues = validate(tables)
    manifest = {"tables": tables, "count": len(tables), "files": len(written)}
    (SQL_DIR / "schema_manifest.json").write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8"
    )
    (TMP / "schema_sync_issues.txt").write_text("\n".join(issues) or "OK", encoding="utf-8")
    print(f"Validation issues: {len(issues)}")
    if issues[:10]:
        for i in issues[:10]:
            print(i)


if __name__ == "__main__":
    main()
