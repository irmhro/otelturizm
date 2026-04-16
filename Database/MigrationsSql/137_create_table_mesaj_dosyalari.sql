CREATE TABLE IF NOT EXISTS [mesaj_dosyalari] (
  [id] bigint  NOT NULL IDENTITY(1,1),
  [mesaj_id] bigint  NOT NULL,
  [guvenli_dosya_id] bigint  NOT NULL,
  [gosterim_adi] varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  [siralama] int  NOT NULL DEFAULT '1',
  [aktif_mi] BIT NOT NULL DEFAULT '1',
  [olusturulma_tarihi] DATETIME2 NOT NULL DEFAULT GETDATE(),
  PRIMARY KEY ([id]),
  UNIQUE KEY [uk_mesaj_dosya] ([mesaj_id],[guvenli_dosya_id]),
  KEY [idx_mesaj_dosyalari_mesaj] ([mesaj_id]),
  CONSTRAINT [fk_mesaj_dosyalari_mesaj] FOREIGN KEY ([mesaj_id]) REFERENCES [mesajlar] ([id]) ON DELETE CASCADE,
  CONSTRAINT [fk_mesaj_dosyalari_dosya] FOREIGN KEY ([guvenli_dosya_id]) REFERENCES [guvenli_dosya_varliklari] ([id]) ON DELETE CASCADE
);
