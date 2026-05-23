# -*- coding: utf-8 -*-
"""Database/MigrationsSql temiz tablo migration uretici (BUYUK HARF)."""
from __future__ import annotations
import json
import re
import shutil
import subprocess
from pathlib import Path

ROOT = Path(r"D:\otelturizm")
OLD_SCHEMA = ROOT / "tools" / "Db" / "schema_source_snapshot"
OUT_BASE = ROOT / "Database"
OUT_SQL = OUT_BASE / "MigrationsSql"
OUT_TABLES = OUT_SQL / "tablo" / "migrationlar"
OUT_CONSTRAINTS = OUT_SQL / "constraints"
MAP_FILE = ROOT / "tools" / "Db" / "schema_name_mapping.json"

DROP_TABLE_NAMES = frozenset({"PLATFORM_EMAIL_HESAPLARI", "PLATFORM_EMAIL_MESAJLARI"})

TABLE_EXACT = {
    "users": "KULLANICILAR",
    "users_partner": "KULLANICI_PARTNERLERI",
    "user_favori_oteller": "KULLANICI_FAVORI_OTELLER",
    "user_favorite_price_alerts": "KULLANICI_FAVORI_FIYAT_ALARMLARI",
    "user_favorite_price_alert_jobs": "KULLANICI_FAVORI_FIYAT_ALARM_ISLERI",
    "email_services": "EPOSTA_SERVISLERI",
    "email_dogrulama_tokenlari": "EPOSTA_DOGRULAMA_TOKENLARI",
    "api_loglari": "API_LOGLARI",
    "outbox_messages": "DIS_KUTU_MESAJLARI",
    "schema_migrations": "SCHEMA_MIGRATIONS",
    "rezervasyonlar_archive": "REZERVASYONLAR_ARSIV",
    "sysdiagrams": "SISTEM_DIYAGRAMLARI",
}

SKIP_TABLE_PATTERNS = (
    "CODEXBACKUP_",
    "_BAK_",
    "_BACKUP_",
    "_YEDEK_",
)


def table_name_from_file(path: Path) -> str:
    m = re.search(r"_table_(.+)\.sql$", path.name, re.I)
    if not m:
        return path.stem.upper()
    old = m.group(1).lower()
    return TABLE_EXACT.get(old, old.upper())


def extract_create_block(text: str) -> str | None:
    start = re.search(
        r"IF OBJECT_ID\(N'dbo\.[^']+',\s*N'U'\)\s*IS NULL\s*BEGIN",
        text,
        re.I,
    )
    if not start:
        return None
    sub = text[start.start() :]
    end = re.search(r"\);\s*\r?\nEND\b", sub, re.I)
    if not end:
        return None
    block = sub[: end.end()]
    if "CREATE TABLE" not in block.upper():
        return None
    return block.strip()


def normalize_create(block: str, table: str) -> str:
    block = re.sub(
        r"IF OBJECT_ID\(N'dbo\.[^']+',\s*N'U'\)",
        f"IF OBJECT_ID(N'dbo.{table}', N'U')",
        block,
        count=1,
        flags=re.I,
    )
    block = re.sub(
        r"CREATE TABLE\s*\[dbo\]\.\[[^\]]+\]",
        f"CREATE TABLE [dbo].[{table}]",
        block,
        count=1,
        flags=re.I,
    )
    block = re.sub(
        r"CONSTRAINT\s+\[PK_[^\]]+\]",
        f"CONSTRAINT [PK_{table}]",
        block,
        count=1,
        flags=re.I,
    )
    return block


def load_remote_tables() -> list[str]:
    txt = ROOT / "tmp" / "remote_tables.txt"
    if not txt.exists():
        return []
    lines = txt.read_text(encoding="utf-8", errors="replace").splitlines()
    out: list[str] = []
    for line in lines:
        line = line.strip()
        if not line or line.isdigit():
            continue
        if any(p in line.upper() for p in SKIP_TABLE_PATTERNS):
            continue
        out.append(line.upper())
    return sorted(set(out))


def export_remote_tables() -> None:
    """Canli DB kullanilmaz; tablo listesi snapshot + mevcut tables dosyalarindan uretilir."""
    return


def collect_table_names(produced: dict[str, str]) -> list[str]:
    names = {t for t in produced.keys() if t not in DROP_TABLE_NAMES}
    return sorted(names)


def apply_name_mapping(text: str) -> str:
    map_path = ROOT / "tmp" / "schema_name_mapping.json"
    if not map_path.exists():
        map_path = MAP_FILE
    if not map_path.exists():
        return text
    data = json.loads(map_path.read_text(encoding="utf-8"))
    extra_tables = {
        "users": "KULLANICILAR",
        "platform_email_mesajlari": "PLATFORM_EPOSTA_MESAJLARI",
        "platform_email_hesaplari": "PLATFORM_EPOSTA_HESAPLARI",
        "platform_ekip_uyeleri": "PLATFORM_EKIP_UYELERI",
    }
    table_map = dict(extra_tables)
    column_map = {
        "onaylayan_admin_user_id": "ONAYLAYAN_ADMIN_KULLANICI_ID",
        "talep_eden_user_id": "TALEP_EDEN_KULLANICI_ID",
    }
    for old_t, info in data.get("tables", {}).items():
        table_map[old_t] = info["new"]
        for old_c, new_c in info.get("columns", {}).items():
            if old_c != new_c:
                column_map[old_c] = new_c
    for old_t, new_t in sorted(table_map.items(), key=lambda x: -len(x[0])):
        text = re.sub(rf"\[dbo\]\.\[{re.escape(old_t)}\]", f"[dbo].[{new_t}]", text, flags=re.I)
        text = re.sub(rf"\bdbo\.{re.escape(old_t)}\b", f"dbo.{new_t}", text, flags=re.I)
    for old_c, new_c in sorted(column_map.items(), key=lambda x: -len(x[0])):
        if old_c == new_c:
            continue
        if not re.fullmatch(r"[A-Za-z_][A-Za-z0-9_]*", old_c):
            continue
        text = re.sub(
            rf"(?<![A-Za-z0-9_])\[{re.escape(old_c)}\](?!\s*\()",
            f"[{new_c}]",
            text,
            flags=re.I,
        )
    return text


def load_overlay_from_tables_dir() -> dict[str, str]:
    overlay: dict[str, str] = {}
    if not OUT_TABLES.exists():
        return overlay
    for path in OUT_TABLES.glob("*.sql"):
        if path.name.startswith("000_"):
            continue
        m = re.search(r"\d{3}_(.+)\.sql$", path.name, re.I)
        if not m:
            continue
        table = m.group(1).upper()
        text = path.read_text(encoding="utf-8", errors="replace")
        if "Kaynak: canli" in text or "CREATE TABLE" in text.upper():
            block = extract_create_block(text) or text
            overlay[table] = apply_name_mapping(block if block.startswith("SET") else normalize_create(block, table))
    return overlay


def write_bootstrap() -> None:
    boot = OUT_BASE / "Bootstrap"
    boot.mkdir(parents=True, exist_ok=True)
    (boot / "001_create_otelturizm_database.sql").write_text(
        """-- Veritabani olustur (yoksa)
IF DB_ID(N'otelturizm_2026db') IS NULL
BEGIN
    CREATE DATABASE [otelturizm_2026db];
END
GO
""",
        encoding="utf-8",
    )
    (boot / "002_verify_connection.sql").write_text(
        """SELECT DB_NAME() AS veritabani, @@VERSION AS surum;
GO
""",
        encoding="utf-8",
    )


def write_sema_migration() -> None:
    (OUT_TABLES / "000_SEMA_MIGRASYONLARI.sql").write_text(
        """-- Migration gecmisi tablosu
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


def generate_from_old_files() -> dict[str, str]:
    produced: dict[str, str] = {}
    if not OLD_SCHEMA.exists():
        return produced
    for path in sorted(OLD_SCHEMA.glob("*_table_*.sql")):
        table = table_name_from_file(path)
        text = path.read_text(encoding="utf-8", errors="replace")
        block = extract_create_block(text)
        if not block:
            continue
        produced[table] = normalize_create(block, table)
    return produced


def generate_stub_table(table: str) -> str:
    return f"""SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF OBJECT_ID(N'dbo.{table}', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[{table}] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [OLUSTURULMA_TARIHI] datetime2(0) NOT NULL CONSTRAINT [DF_{table}_OLUSTURULMA] DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_{table}] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
GO
"""


def copy_constraints_from_old() -> tuple[str, str]:
    fk = OLD_SCHEMA / "900_foreign_keys.sql"
    ix = OLD_SCHEMA / "901_indexes.sql"
    fk_text = fk.read_text(encoding="utf-8", errors="replace") if fk.exists() else "-- FK yok\n"
    ix_text = ix.read_text(encoding="utf-8", errors="replace") if ix.exists() else "-- Index yok\n"
    # users -> KULLANICILAR vb.
    if MAP_FILE.exists():
        data = json.loads(MAP_FILE.read_text(encoding="utf-8"))
        for old_t, info in data.get("tables", {}).items():
            new_t = info["new"]
            fk_text = re.sub(rf"\bdbo\.{re.escape(old_t)}\b", f"dbo.{new_t}", fk_text, flags=re.I)
            fk_text = re.sub(rf"\[dbo\]\.\[{re.escape(old_t)}\]", f"[dbo].[{new_t}]", fk_text, flags=re.I)
            ix_text = re.sub(rf"\bdbo\.{re.escape(old_t)}\b", f"dbo.{new_t}", ix_text, flags=re.I)
            ix_text = re.sub(rf"\[dbo\]\.\[{re.escape(old_t)}\]", f"[dbo].[{new_t}]", ix_text, flags=re.I)
    return fk_text, ix_text


def write_triggers() -> str:
    trig = ROOT / "Database" / "MigrationsSql" / "20260522_sqlserver_triggers_after_uppercase_rename.sql"
    if trig.exists():
        return trig.read_text(encoding="utf-8")
    return "-- trigger dosyasi\n"


def main() -> None:
    overlay = load_overlay_from_tables_dir()
    produced = generate_from_old_files()
    for table, ddl in overlay.items():
        produced[table] = ddl
    all_tables = collect_table_names(produced)
    fk_raw, ix_raw = copy_constraints_from_old()
    fk_text = apply_name_mapping(fk_raw).lstrip("\ufeff")
    ix_text = apply_name_mapping(ix_raw).lstrip("\ufeff")
    triggers_text = write_triggers()

    if OUT_TABLES.exists():
        for f in OUT_TABLES.glob("*.sql"):
            f.unlink()
    OUT_TABLES.mkdir(parents=True, exist_ok=True)
    write_bootstrap()
    write_sema_migration()

    idx = 1
    written: list[str] = []
    for table in all_tables:
        if table == "SEMA_MIGRASYONLARI":
            continue
        content = produced.get(table)
        if not content:
            content = generate_stub_table(table)
            stub = True
        else:
            content = apply_name_mapping(content)
            stub = False
        num = f"{idx:03d}"
        fname = f"{num}_{table}.sql"
        header = f"-- Tablo: dbo.{table}\n"
        if stub:
            header += "-- UYARI: Stub migration; tablo DDL henuz snapshot'ta yok.\n"
        (OUT_TABLES / fname).write_text(header + content + "\n", encoding="utf-8")
        written.append(table)
        idx += 1

    OUT_CONSTRAINTS.mkdir(parents=True, exist_ok=True)
    (OUT_CONSTRAINTS / "900_foreign_keys.sql").write_text(
        "-- Foreign keys\n" + fk_text.lstrip(),
        encoding="utf-8",
    )
    (OUT_CONSTRAINTS / "901_indexes.sql").write_text(
        "-- Indexes\n" + ix_text.lstrip(),
        encoding="utf-8",
    )
    (OUT_CONSTRAINTS / "902_triggers.sql").write_text(
        triggers_text if triggers_text.strip() else "-- Triggers\n",
        encoding="utf-8",
    )
    for legacy in (
        OUT_SQL / "900_foreign_keys.sql",
        OUT_SQL / "901_indexes.sql",
        OUT_SQL / "902_triggers.sql",
    ):
        legacy.unlink(missing_ok=True)

    readme = f"""# MigrationsSql

Guncel tablo migrationlari: `tablo/migrationlar/` ({len(written)} tablo) + `constraints/900-902`.

Uretim: `python tools/Db/clean_migrations_sql.py`
"""
    (OUT_SQL / "README.md").write_text(readme, encoding="utf-8")

    manifest = {"tables": written, "count": len(written)}
    (OUT_SQL / "schema_manifest.json").write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8"
    )
    print(f"Wrote {len(written)} table migrations to {OUT_TABLES}")


if __name__ == "__main__":
    main()
