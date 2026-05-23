-- Tablo: dbo.DEVELOPER_BILDIRIMLERI
IF OBJECT_ID(N'dbo.DEVELOPER_BILDIRIMLERI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[DEVELOPER_BILDIRIMLERI] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [BILDIRIM_KODU] uniqueidentifier NOT NULL CONSTRAINT [DF_developer_bildirimleri_kod] DEFAULT (newid()),
        [KAYNAK_PANEL] nvarchar(50) NOT NULL,
        [KAYNAK_ROL] nvarchar(50) NULL,
        [KULLANICI_ID] bigint NULL,
        [KULLANICI_EPOSTA] nvarchar(256) NULL,
        [AD_SOYAD] nvarchar(200) NULL,
        [BILDIRIM_TURU] nvarchar(60) NOT NULL,
        [BASLIK] nvarchar(220) NOT NULL,
        [ICERIK] nvarchar(max) NOT NULL,
        [SAYFA_URL] nvarchar(1000) NULL,
        [IP_ADRESI] nvarchar(80) NULL,
        [KULLANICI_ARACISI] nvarchar(1000) NULL,
        [EKRAN_BILGISI] nvarchar(120) NULL,
        [CIHAZ_BILGISI] nvarchar(500) NULL,
        [GORSEL_URL] nvarchar(1000) NULL,
        [DURUM] nvarchar(60) NOT NULL CONSTRAINT [DF_developer_bildirimleri_durum] DEFAULT (N'Yeni'),
        [ONCELIK] nvarchar(40) NOT NULL CONSTRAINT [DF_developer_bildirimleri_oncelik] DEFAULT (N'Orta'),
        [EPOSTA_KUYRUGA_ALINDI_MI] bit NOT NULL CONSTRAINT [DF_developer_bildirimleri_email] DEFAULT ((0)),
        [ADMIN_NOTU] nvarchar(max) NULL,
        [OLUSTURULMA_TARIHI] datetime2(7) NOT NULL CONSTRAINT [DF_developer_bildirimleri_olusturma] DEFAULT (sysutcdatetime()),
        [GUNCELLENME_TARIHI] datetime2(7) NOT NULL CONSTRAINT [DF_developer_bildirimleri_guncelleme] DEFAULT (sysutcdatetime()),
        [SAYFA_BASLIGI] nvarchar(220) NULL,
        CONSTRAINT [PK_DEVELOPER_BILDIRIMLERI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
