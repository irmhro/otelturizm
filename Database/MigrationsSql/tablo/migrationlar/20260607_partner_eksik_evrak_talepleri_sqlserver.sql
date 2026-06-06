-- Partner admin "eksik evrak" talepleri (Askida durumunda partner panelinde gösterilir)
IF OBJECT_ID(N'dbo.PARTNER_EKSIK_EVRAK_TALEPLERI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PARTNER_EKSIK_EVRAK_TALEPLERI] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [PARTNER_ID] bigint NOT NULL,
        [EVRAK_TIPI] nvarchar(80) NOT NULL,
        [AKTIF_MI] bit NOT NULL CONSTRAINT [DF_partner_eksik_evrak_aktif] DEFAULT ((1)),
        [ADMIN_NOTU] nvarchar(500) NULL,
        [OLUSTURAN_ADMIN_ID] bigint NULL,
        [OLUSTURULMA_TARIHI] datetime2(0) NOT NULL CONSTRAINT [DF_partner_eksik_evrak_olustur] DEFAULT (sysutcdatetime()),
        [TAMAMLANDI_TARIHI] datetime2(0) NULL,
        CONSTRAINT [PK_PARTNER_EKSIK_EVRAK_TALEPLERI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.PARTNER_EKSIK_EVRAK_TALEPLERI') AND name = N'IX_partner_eksik_evrak_partner_aktif')
BEGIN
    CREATE INDEX [IX_partner_eksik_evrak_partner_aktif]
        ON [dbo].[PARTNER_EKSIK_EVRAK_TALEPLERI] ([PARTNER_ID] ASC, [AKTIF_MI] ASC, [EVRAK_TIPI] ASC);
END;
