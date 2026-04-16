/*
SQL Server migration no-op.

This script (139_alter_mesajlar_add_firma_and_soft_delete.sql) previously used MySQL-specific dynamic SQL/metadata constructs (e.g., SET @sql, PREPARE/DEALLOCATE, DELIMITER, or DATABASE()-scoped information_schema checks).
It is intentionally disabled for SQL Server to avoid unsafe or non-portable behavior.

If equivalent behavior is still required, implement an idempotent SQL Server migration using sys catalog views and explicit ALTER statements.
*/
