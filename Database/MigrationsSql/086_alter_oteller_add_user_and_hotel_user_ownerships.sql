/*
SQL Server migration no-op.

This script (086_alter_oteller_add_user_and_hotel_user_ownerships.sql) previously used MySQL-specific dynamic SQL/metadata constructs (e.g., SET @sql, PREPARE/DEALLOCATE, DELIMITER, or DATABASE()-scoped information_schema checks).
It is intentionally disabled for SQL Server to avoid unsafe or non-portable behavior.

If equivalent behavior is still required, implement an idempotent SQL Server migration using sys catalog views and explicit ALTER statements.
*/
