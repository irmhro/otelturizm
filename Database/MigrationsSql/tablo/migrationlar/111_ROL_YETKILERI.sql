-- Tablo: dbo.ROL_YETKILERI
IF OBJECT_ID(N'dbo.ROL_YETKILERI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ROL_YETKILERI]
    (
        [ROL_ID] smallint NOT NULL,
        [YETKI_ID] int NOT NULL,
        [IZIN_VAR] bit CONSTRAINT [DF__rol_yetki__izin___77DFC722] DEFAULT ((1)) NULL,
        [ATAYAN_KULLANICI_ID] bigint NULL,
        [ATAMA_TARIHI] datetime2(0) CONSTRAINT [DF__rol_yetki__atama__78D3EB5B] DEFAULT (sysutcdatetime()) NULL,
        CONSTRAINT [PK_ROL_YETKILERI] PRIMARY KEY CLUSTERED ([ROL_ID] ASC, [YETKI_ID] ASC)
    );
END
