-- Tablo: dbo.OTEL_OZELLIKLERI
IF OBJECT_ID(N'dbo.OTEL_OZELLIKLERI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[OTEL_OZELLIKLERI] (
        [ID] int IDENTITY(1,1) NOT NULL,
        [KATEGORI_ID] smallint NOT NULL,
        [OZELLIK_ADI] nvarchar(100) NOT NULL,
        [OZELLIK_IKON] nvarchar(50) NULL,
        [UCRETLI_MI] bit NULL CONSTRAINT [DF__otel_ozel__ucret__220B0B18] DEFAULT ((0)),
        [ONE_CIKAN_OZELLIK] bit NULL CONSTRAINT [DF__otel_ozel__one_c__22FF2F51] DEFAULT ((0)),
        [SIRALAMA] smallint NULL CONSTRAINT [DF__otel_ozel__siral__23F3538A] DEFAULT ((0)),
        [AKTIF_MI] bit NULL CONSTRAINT [DF__otel_ozel__aktif__24E777C3] DEFAULT ((1)),
        CONSTRAINT [PK_OTEL_OZELLIKLERI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
