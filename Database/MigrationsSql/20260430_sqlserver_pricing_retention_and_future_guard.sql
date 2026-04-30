SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.trg_oda_fiyat_musaitlik_max_future_365', N'TR') IS NOT NULL
    EXEC(N'DROP TRIGGER dbo.trg_oda_fiyat_musaitlik_max_future_365;');
GO

CREATE TRIGGER dbo.trg_oda_fiyat_musaitlik_max_future_365
ON dbo.oda_fiyat_musaitlik
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM inserted
        WHERE CONVERT(date, tarih) > CONVERT(date, DATEADD(DAY, 365, GETDATE()))
    )
    BEGIN
        RAISERROR('365 gunden sonrasi icin oda fiyat musaitlik kaydi olusturulamaz.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;
GO

IF OBJECT_ID(N'dbo.firma_oda_fiyat_musaitlik', N'U') IS NOT NULL
BEGIN
    IF OBJECT_ID(N'dbo.trg_firma_oda_fiyat_musaitlik_max_future_365', N'TR') IS NOT NULL
        EXEC(N'DROP TRIGGER dbo.trg_firma_oda_fiyat_musaitlik_max_future_365;');

    EXEC(N'
    CREATE TRIGGER dbo.trg_firma_oda_fiyat_musaitlik_max_future_365
    ON dbo.firma_oda_fiyat_musaitlik
    AFTER INSERT, UPDATE
    AS
    BEGIN
        SET NOCOUNT ON;

        IF EXISTS (
            SELECT 1
            FROM inserted
            WHERE CONVERT(date, tarih) > CONVERT(date, DATEADD(DAY, 365, GETDATE()))
        )
        BEGIN
            RAISERROR(''365 gunden sonrasi icin firma oda fiyat kaydi olusturulamaz.'', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END
    END;');
END;
GO

IF OBJECT_ID(N'dbo.usp_fiyat_musaitlik_retention_cleanup', N'P') IS NOT NULL
    EXEC(N'DROP PROCEDURE dbo.usp_fiyat_musaitlik_retention_cleanup;');
GO

CREATE PROCEDURE dbo.usp_fiyat_musaitlik_retention_cleanup
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @cutoff date = CONVERT(date, DATEADD(DAY, -60, GETDATE()));
    DECLARE @deletedOda int = 0;
    DECLARE @deletedFirma int = 0;
    DECLARE @rc int = 1;

    WHILE (@rc > 0)
    BEGIN
        DELETE TOP (10000)
        FROM dbo.oda_fiyat_musaitlik
        WHERE CONVERT(date, tarih) < @cutoff;

        SET @rc = @@ROWCOUNT;
        SET @deletedOda += @rc;
    END

    IF OBJECT_ID(N'dbo.firma_oda_fiyat_musaitlik', N'U') IS NOT NULL
    BEGIN
        SET @rc = 1;
        WHILE (@rc > 0)
        BEGIN
            DELETE TOP (10000)
            FROM dbo.firma_oda_fiyat_musaitlik
            WHERE CONVERT(date, tarih) < @cutoff;

            SET @rc = @@ROWCOUNT;
            SET @deletedFirma += @rc;
        END
    END

    SELECT @cutoff AS cutoff_tarihi,
           @deletedOda AS silinen_oda_fiyat_musaitlik,
           @deletedFirma AS silinen_firma_oda_fiyat_musaitlik;
END;
GO

IF DB_NAME() <> N'tempdb'
AND CAST(SERVERPROPERTY('EngineEdition') AS int) <> 4
AND OBJECT_ID(N'msdb.dbo.sp_add_job', N'P') IS NOT NULL
BEGIN
    DECLARE @jobSql nvarchar(max) = N'
    DECLARE @jobId uniqueidentifier;
    SELECT @jobId = job_id
    FROM msdb.dbo.sysjobs
    WHERE name = N''OtelTurizm_FiyatMusaitlik_Temizlik'';

    IF @jobId IS NULL
    BEGIN
        EXEC msdb.dbo.sp_add_job
            @job_name = N''OtelTurizm_FiyatMusaitlik_Temizlik'',
            @enabled = 1,
            @description = N''Her gun gecmis 60 gunden eski oda fiyat musaitlik kayitlarini temizler.'',
            @job_id = @jobId OUTPUT;

        EXEC msdb.dbo.sp_add_jobstep
            @job_id = @jobId,
            @step_name = N''CleanupOldPricingRows'',
            @subsystem = N''TSQL'',
            @database_name = DB_NAME(),
            @command = N''EXEC dbo.usp_fiyat_musaitlik_retention_cleanup;'',
            @on_success_action = 1,
            @on_fail_action = 2;

        IF NOT EXISTS (
            SELECT 1
            FROM msdb.dbo.sysschedules
            WHERE name = N''OtelTurizm_FiyatMusaitlik_Temizlik_Gunluk_0315'')
        BEGIN
            EXEC msdb.dbo.sp_add_schedule
                @schedule_name = N''OtelTurizm_FiyatMusaitlik_Temizlik_Gunluk_0315'',
                @enabled = 1,
                @freq_type = 4,
                @freq_interval = 1,
                @active_start_time = 31500;
        END

        EXEC msdb.dbo.sp_attach_schedule
            @job_id = @jobId,
            @schedule_name = N''OtelTurizm_FiyatMusaitlik_Temizlik_Gunluk_0315'';

        EXEC msdb.dbo.sp_add_jobserver
            @job_id = @jobId;
    END';
    EXEC sp_executesql @jobSql;
END;
GO
