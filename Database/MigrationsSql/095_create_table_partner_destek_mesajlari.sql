CREATE TABLE IF NOT EXISTS [partner_destek_mesajlari] (
  [id] bigint  NOT NULL IDENTITY(1,1),
  [talep_id] bigint  NOT NULL,
  [gonderen_kullanici_id] bigint  DEFAULT NULL,
  [gonderen_tipi] enum('Partner','Admin','Sistem') COLLATE utf8mb4_unicode_ci NOT NULL,
  [mesaj] NVARCHAR(MAX) COLLATE utf8mb4_unicode_ci NOT NULL,
  [ek_dosya_yolu] varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  [okundu_mu] BIT DEFAULT '0',
  [olusturulma_tarihi] DATETIME2 NULL DEFAULT GETDATE(),
  PRIMARY KEY ([id]),
  KEY [idx_partner_destek_mesaj_talep] ([talep_id],[olusturulma_tarihi]),
  KEY [idx_partner_destek_mesaj_gonderen] ([gonderen_kullanici_id]),
  CONSTRAINT [fk_partner_destek_mesaj_talep] FOREIGN KEY ([talep_id]) REFERENCES [partner_destek_talepleri] ([id]) ON DELETE CASCADE,
  CONSTRAINT [fk_partner_destek_mesaj_user] FOREIGN KEY ([gonderen_kullanici_id]) REFERENCES [users] ([id]) ON DELETE SET NULL
);
