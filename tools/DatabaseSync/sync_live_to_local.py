from __future__ import annotations

import pyodbc

LOCAL_CONN_STR = (
    r"DRIVER={ODBC Driver 17 for SQL Server};"
    r"SERVER=(localdb)\MSSQLLocalDB;"
    r"DATABASE=otelturizm_2026db;"
    r"Trusted_Connection=yes;"
)

LIVE_CONN_STR = (
    r"DRIVER={ODBC Driver 17 for SQL Server};"
    r"SERVER=185.111.244.246;"
    r"DATABASE=otelturizm_2026db;"
    r"UID=sa;"
    r"PWD=Nusret.34.34.-;"
    r"Encrypt=no;"
    r"TrustServerCertificate=yes;"
)

SYSTEM_TABLES = {"sysdiagrams"}


def get_tables(cursor: pyodbc.Cursor) -> list[str]:
    cursor.execute(
        """
        SELECT TABLE_NAME
        FROM INFORMATION_SCHEMA.TABLES
        WHERE TABLE_TYPE='BASE TABLE'
        ORDER BY TABLE_NAME
        """
    )
    return [row[0] for row in cursor.fetchall() if row[0] not in SYSTEM_TABLES]


def get_columns(cursor: pyodbc.Cursor, table_name: str) -> list[str]:
    cursor.execute(
        """
        SELECT c.name
        FROM sys.columns c
        INNER JOIN sys.tables t ON t.object_id = c.object_id
        WHERE t.name = ?
          AND c.is_computed = 0
        ORDER BY c.column_id
        """,
        table_name,
    )
    return [row[0] for row in cursor.fetchall()]


def get_shared_columns(source_cursor: pyodbc.Cursor, target_cursor: pyodbc.Cursor, table_name: str) -> list[str]:
    source_columns = get_columns(source_cursor, table_name)
    target_columns = set(get_columns(target_cursor, table_name))
    return [column for column in source_columns if column in target_columns]


def has_identity(cursor: pyodbc.Cursor, table_name: str) -> bool:
    cursor.execute(
        """
        SELECT COUNT(*)
        FROM sys.identity_columns ic
        INNER JOIN sys.tables t ON t.object_id = ic.object_id
        WHERE t.name = ?
        """,
        table_name,
    )
    return cursor.fetchone()[0] > 0


def quote_ident(name: str) -> str:
    return f"[{name}]"


def disable_constraints(cursor: pyodbc.Cursor, tables: list[str]) -> None:
    for table in tables:
        cursor.execute(f"ALTER TABLE dbo.{quote_ident(table)} NOCHECK CONSTRAINT ALL")


def enable_constraints(cursor: pyodbc.Cursor, tables: list[str]) -> None:
    for table in tables:
        cursor.execute(f"ALTER TABLE dbo.{quote_ident(table)} WITH CHECK CHECK CONSTRAINT ALL")


def disable_triggers(cursor: pyodbc.Cursor, tables: list[str]) -> None:
    for table in tables:
        cursor.execute(f"ALTER TABLE dbo.{quote_ident(table)} DISABLE TRIGGER ALL")


def enable_triggers(cursor: pyodbc.Cursor, tables: list[str]) -> None:
    for table in tables:
        cursor.execute(f"ALTER TABLE dbo.{quote_ident(table)} ENABLE TRIGGER ALL")


def clear_tables(cursor: pyodbc.Cursor, tables: list[str]) -> None:
    for table in reversed(tables):
        cursor.execute(f"DELETE FROM dbo.{quote_ident(table)}")


def copy_table(source_cursor: pyodbc.Cursor, target_cursor: pyodbc.Cursor, table_name: str) -> int:
    columns = get_shared_columns(source_cursor, target_cursor, table_name)
    if not columns:
        return 0

    col_sql = ", ".join(quote_ident(col) for col in columns)
    source_cursor.execute(f"SELECT {col_sql} FROM dbo.{quote_ident(table_name)}")
    rows = source_cursor.fetchall()
    if not rows:
        return 0

    placeholders = ", ".join("?" for _ in columns)
    insert_sql = f"INSERT INTO dbo.{quote_ident(table_name)} ({col_sql}) VALUES ({placeholders})"

    identity = has_identity(source_cursor, table_name)
    if identity:
        target_cursor.execute(f"SET IDENTITY_INSERT dbo.{quote_ident(table_name)} ON")

    target_cursor.fast_executemany = True
    target_cursor.executemany(insert_sql, rows)

    if identity:
        target_cursor.execute(f"SET IDENTITY_INSERT dbo.{quote_ident(table_name)} OFF")

    return len(rows)


def main() -> None:
    live = pyodbc.connect(LIVE_CONN_STR)
    local = pyodbc.connect(LOCAL_CONN_STR)
    live.autocommit = False
    local.autocommit = False

    try:
        scur = live.cursor()
        lcur = local.cursor()

        live_tables = get_tables(scur)
        local_tables = set(get_tables(lcur))
        missing = [table for table in live_tables if table not in local_tables]
        if missing:
            raise RuntimeError(f"Local DB is missing tables: {missing}")

        disable_constraints(lcur, live_tables)
        disable_triggers(lcur, live_tables)
        clear_tables(lcur, live_tables)

        stats: list[tuple[str, int]] = []
        for table in live_tables:
            count = copy_table(scur, lcur, table)
            stats.append((table, count))

        enable_triggers(lcur, live_tables)
        enable_constraints(lcur, live_tables)
        local.commit()

        print("SYNC_OK")
        for table, count in stats:
            if count:
                print(f"{table}|{count}")
    except Exception:
        local.rollback()
        raise
    finally:
        live.close()
        local.close()


if __name__ == "__main__":
    main()
