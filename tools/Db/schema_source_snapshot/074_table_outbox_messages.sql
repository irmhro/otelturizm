SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.outbox_messages', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[outbox_messages]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [olusturma_utc] datetime2(7) CONSTRAINT [DF_outbox_messages_created] DEFAULT (sysutcdatetime()) NOT NULL,
        [olay_turu] nvarchar(128) NOT NULL,
        [yuk] nvarchar(max) NOT NULL,
        [islendi_mi] bit CONSTRAINT [DF_outbox_messages_done] DEFAULT ((0)) NOT NULL,
        [islendi_utc] datetime2(7) NULL,
        [deneme_sayisi] int CONSTRAINT [DF_outbox_messages_attempts] DEFAULT ((0)) NOT NULL,
        [son_hata] nvarchar(2000) NULL,
        CONSTRAINT [PK__outbox_m__3213E83FA5B5F99D] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.outbox_messages', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[outbox_messages] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.outbox_messages', N'olusturma_utc') IS NULL
BEGIN
    ALTER TABLE [dbo].[outbox_messages] ADD [olusturma_utc] datetime2(7) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.outbox_messages', N'olay_turu') IS NULL
BEGIN
    ALTER TABLE [dbo].[outbox_messages] ADD [olay_turu] nvarchar(128) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.outbox_messages', N'yuk') IS NULL
BEGIN
    ALTER TABLE [dbo].[outbox_messages] ADD [yuk] nvarchar(max) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.outbox_messages', N'islendi_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[outbox_messages] ADD [islendi_mi] bit DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.outbox_messages', N'islendi_utc') IS NULL
BEGIN
    ALTER TABLE [dbo].[outbox_messages] ADD [islendi_utc] datetime2(7) NULL;
END
GO
IF COL_LENGTH(N'dbo.outbox_messages', N'deneme_sayisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[outbox_messages] ADD [deneme_sayisi] int DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.outbox_messages', N'son_hata') IS NULL
BEGIN
    ALTER TABLE [dbo].[outbox_messages] ADD [son_hata] nvarchar(2000) NULL;
END
GO
