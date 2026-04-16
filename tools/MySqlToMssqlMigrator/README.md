# MySQL to MSSQL Migrator

Schema and data copy tool for moving the project database from MySQL to SQL Server.

## Default Behavior

- Reads MySQL connection from `appsettings.Development.json` (`DefaultConnection`).
- Writes to LocalDB: `Server=(localdb)\MSSQLLocalDB;Database=otelturizm_2026db;Trusted_Connection=True;TrustServerCertificate=True;`
- Creates missing target tables.
- Truncates target table data before copy.
- Copies all rows table-by-table.

## Run

```bash
dotnet run --project tools/MySqlToMssqlMigrator/MySqlToMssqlMigrator.csproj
```

## Useful Options

- `--schema-only` : create tables only, skip data copy
- `--no-truncate` : do not delete target rows before insert
- `--tables "users,oteller,kampanyalar"` : migrate only listed tables
- `--mysql "<connection-string>"` : override source MySQL connection
- `--mssql "<connection-string>"` : override target SQL Server connection
- `--source-db "<db-name>"` : override source schema/database
- `--target-db "<db-name>"` : override target SQL database name

## Example

```bash
dotnet run --project tools/MySqlToMssqlMigrator/MySqlToMssqlMigrator.csproj -- \
  --target-db otelturizm_2026db \
  --tables "users,oteller,oda_tipleri,oda_fiyat_musaitlik"
```
