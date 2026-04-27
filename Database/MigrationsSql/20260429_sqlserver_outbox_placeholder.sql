/*
  Paket 242: Transactional outbox hedef tablosu (minimal şema — genişletilebilir).
*/

IF OBJECT_ID(N'dbo.outbox_messages', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.outbox_messages (
        id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        olusturma_utc DATETIME2 NOT NULL CONSTRAINT DF_outbox_messages_created DEFAULT SYSUTCDATETIME(),
        olay_turu NVARCHAR(128) NOT NULL,
        yuk NVARCHAR(MAX) NOT NULL,
        islendi_mi BIT NOT NULL CONSTRAINT DF_outbox_messages_done DEFAULT 0,
        islendi_utc DATETIME2 NULL,
        deneme_sayisi INT NOT NULL CONSTRAINT DF_outbox_messages_attempts DEFAULT 0,
        son_hata NVARCHAR(2000) NULL
    );

    CREATE INDEX IX_outbox_messages_pending ON dbo.outbox_messages (islendi_mi, olusturma_utc)
        WHERE islendi_mi = 0;
END
