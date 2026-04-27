-- p69: SQL index/plan kontrolü için yardımcı rapor (SQL Server)
-- Not: Bu script sysadmin yetkisi gerektirmez; dm_exec_query_stats için VIEW SERVER STATE gerekir.
-- Eğer yetki yoksa DBA ile birlikte çalıştırın.

SELECT TOP (20)
       qs.execution_count,
       CAST(qs.total_elapsed_time / 1000.0 AS decimal(18,2)) AS total_elapsed_ms,
       CAST(qs.max_elapsed_time / 1000.0 AS decimal(18,2)) AS max_elapsed_ms,
       CAST((qs.total_elapsed_time / NULLIF(qs.execution_count,0)) / 1000.0 AS decimal(18,2)) AS avg_elapsed_ms,
       DB_NAME(st.dbid) AS db_name,
       SUBSTRING(st.text, (qs.statement_start_offset/2)+1,
                 ((CASE qs.statement_end_offset WHEN -1 THEN DATALENGTH(st.text) ELSE qs.statement_end_offset END
                   - qs.statement_start_offset)/2)+1) AS sql_text
FROM sys.dm_exec_query_stats qs
CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) st
ORDER BY qs.max_elapsed_time DESC;

