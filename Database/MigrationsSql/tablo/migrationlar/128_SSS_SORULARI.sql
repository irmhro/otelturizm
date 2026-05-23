-- Tablo: dbo.SSS_SORULARI
IF OBJECT_ID(N'dbo.SSS_SORULARI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[SSS_SORULARI]
    (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [SSS_KATEGORI_ID] bigint NOT NULL,
        [SORU] nvarchar(255) NOT NULL,
        [CEVAP] nvarchar(max) NOT NULL,
        [ONE_CIKAN_MI] bit CONSTRAINT [DF__sss_sorul__one_c__33F4B129] DEFAULT ((0)) NOT NULL,
        [SIRALAMA] int CONSTRAINT [DF__sss_sorul__siral__34E8D562] DEFAULT ((0)) NOT NULL,
        [AKTIF_MI] bit CONSTRAINT [DF__sss_sorul__aktif__35DCF99B] DEFAULT ((1)) NOT NULL,
        [OLUSTURULMA_TARIHI] datetime2(0) CONSTRAINT [DF__sss_sorul__olust__36D11DD4] DEFAULT (sysutcdatetime()) NOT NULL,
        [GUNCELLENME_TARIHI] datetime2(0) CONSTRAINT [DF__sss_sorul__gunce__37C5420D] DEFAULT (sysutcdatetime()) NOT NULL,
        CONSTRAINT [PK_SSS_SORULARI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
