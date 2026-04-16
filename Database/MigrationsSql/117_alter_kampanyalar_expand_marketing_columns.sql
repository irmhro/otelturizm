/*
SQL Server migration no-op.

This script (117_alter_kampanyalar_expand_marketing_columns.sql) previously used MySQL-specific dynamic SQL/metadata constructs (e.g., SET @sql, PREPARE/DEALLOCATE, DELIMITER, or DATABASE()-scoped information_schema checks).
It is intentionally disabled for SQL Server to avoid unsafe or non-portable behavior.

If equivalent behavior is still required, implement an idempotent SQL Server migration using sys catalog views and explicit ALTER statements.
*/
