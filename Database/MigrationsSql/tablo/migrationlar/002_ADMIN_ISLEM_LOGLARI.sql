-- Tablo: dbo.ADMIN_ISLEM_LOGLARI
IF OBJECT_ID(N'dbo.ADMIN_ISLEM_LOGLARI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ADMIN_ISLEM_LOGLARI] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [ADMIN_KULLANICI_ID] bigint NOT NULL,
        [ISLEM_TURU] nvarchar(255) NOT NULL,
        [HEDEF_TABLO] nvarchar(50) NOT NULL,
        [HEDEF_KAYIT_ID] bigint NULL,
        [ONCEKI_DEGER] nvarchar(max) NULL,
        [YENI_DEGER] nvarchar(max) NULL,
        [DEGISIKLIK_OZETI] nvarchar(max) NULL,
        [ISLEM_NEDENI] nvarchar(500) NULL,
        [ISLEM_NOTU] nvarchar(max) NULL,
        [IP_ADRESI] nvarchar(45) NOT NULL,
        [ISLEM_TARIHI] datetime2(0) NULL CONSTRAINT [DF__admin_isl__islem__6FE99F9F] DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_ADMIN_ISLEM_LOGLARI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
