/* scope: dbo.OTEL_OZELLIK_ILISKILERI only
   remove temporary pricing columns added by 20260522_enhance_otel_ozellikleri_and_iliskileri.sql
*/

IF OBJECT_ID(N'dbo.OTEL_OZELLIK_ILISKILERI', N'U') IS NULL RETURN;

BEGIN TRAN;

DECLARE @tbl sysname = N'dbo.OTEL_OZELLIK_ILISKILERI';
DECLARE @col sysname;
DECLARE @sql nvarchar(max);

DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
SELECT c.name
FROM sys.columns c
WHERE c.object_id = OBJECT_ID(@tbl)
  AND c.name IN
  (
    N'FIYATLANDIRMA_TIPI',
    N'FIYAT_PARA_BIRIMI',
    N'FIYAT_BIRIMI',
    N'FIYAT_1_KISI',
    N'FIYAT_2_KISI',
    N'FIYAT_3_KISI',
    N'FIYAT_4_KISI'
  );

OPEN cur;
FETCH NEXT FROM cur INTO @col;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @sql = NULL;
    SELECT @sql = N'ALTER TABLE ' + @tbl + N' DROP CONSTRAINT ' + QUOTENAME(dc.name) + N';'
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c
        ON c.object_id = dc.parent_object_id
       AND c.column_id = dc.parent_column_id
    WHERE dc.parent_object_id = OBJECT_ID(@tbl)
      AND c.name = @col;

    IF @sql IS NOT NULL
    BEGIN
        EXEC sp_executesql @sql;
        SET @sql = NULL;
    END

    SET @sql = N'ALTER TABLE ' + @tbl + N' DROP COLUMN ' + QUOTENAME(@col) + N';';
    EXEC sp_executesql @sql;

    FETCH NEXT FROM cur INTO @col;
END
CLOSE cur;
DEALLOCATE cur;

COMMIT TRAN;
