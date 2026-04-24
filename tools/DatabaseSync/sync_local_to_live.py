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


def copy_table(local_cursor: pyodbc.Cursor, live_cursor: pyodbc.Cursor, table_name: str) -> int:
    columns = get_columns(local_cursor, table_name)
    if not columns:
        return 0

    col_sql = ", ".join(quote_ident(col) for col in columns)
    local_cursor.execute(f"SELECT {col_sql} FROM dbo.{quote_ident(table_name)}")
    rows = local_cursor.fetchall()
    if not rows:
        return 0

    placeholders = ", ".join("?" for _ in columns)
    insert_sql = f"INSERT INTO dbo.{quote_ident(table_name)} ({col_sql}) VALUES ({placeholders})"

    identity = has_identity(local_cursor, table_name)
    if identity:
        live_cursor.execute(f"SET IDENTITY_INSERT dbo.{quote_ident(table_name)} ON")

    live_cursor.fast_executemany = True
    live_cursor.executemany(insert_sql, rows)

    if identity:
        live_cursor.execute(f"SET IDENTITY_INSERT dbo.{quote_ident(table_name)} OFF")

    return len(rows)


def main() -> None:
    local = pyodbc.connect(LOCAL_CONN_STR)
    live = pyodbc.connect(LIVE_CONN_STR)
    local.autocommit = False
    live.autocommit = False

    try:
        lcur = local.cursor()
        rcur = live.cursor()

        local_tables = get_tables(lcur)
        live_tables = set(get_tables(rcur))
        missing = [table for table in local_tables if table not in live_tables]
        if missing:
            raise RuntimeError(f"Live DB is missing tables: {missing}")

        disable_constraints(rcur, local_tables)
        disable_triggers(rcur, local_tables)
        clear_tables(rcur, local_tables)

        stats: list[tuple[str, int]] = []
        for table in local_tables:
            count = copy_table(lcur, rcur, table)
            stats.append((table, count))

        enable_triggers(rcur, local_tables)
        enable_constraints(rcur, local_tables)
        live.commit()

        print("SYNC_OK")
        for table, count in stats:
            if count:
                print(f"{table}|{count}")
    except Exception:
        live.rollback()
        raise
    finally:
        local.close()
        live.close()


if __name__ == "__main__":
    main()
