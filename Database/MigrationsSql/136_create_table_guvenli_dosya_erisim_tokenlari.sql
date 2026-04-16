CREATE TABLE IF NOT EXISTS [guvenli_dosya_erisim_tokenlari] (
  [id] bigint  NOT NULL IDENTITY(1,1),
  [guvenli_dosya_id] bigint  NOT NULL,
  [erisim_tokeni] varchar(64) COLLATE utf8mb4_unicode_ci NOT NULL,
  [kullanici_id] bigint  NOT NULL,
  [hesap_tipi] varchar(30) COLLATE utf8mb4_unicode_ci NOT NULL,
  [kullanim_sayisi] int  NOT NULL DEFAULT '0',
  [maksimum_kullanim_sayisi] int  DEFAULT NULL,
  [gecerlilik_tarihi] DATETIME2 NOT NULL,
  [son_erisim_tarihi] DATETIME2 NULL DEFAULT NULL,
  [iptal_tarihi] DATETIME2 NULL DEFAULT NULL,
  [olusturulma_tarihi] DATETIME2 NOT NULL DEFAULT GETDATE(),
  PRIMARY KEY ([id]),
  UNIQUE KEY [uk_guvenli_token] ([erisim_tokeni]),
  KEY [idx_guvenli_token_kullanici] ([kullanici_id],[hesap_tipi]),
  KEY [idx_guvenli_token_gecerlilik] ([gecerlilik_tarihi]),
  CONSTRAINT [fk_guvenli_token_dosya] FOREIGN KEY ([guvenli_dosya_id]) REFERENCES [guvenli_dosya_varliklari] ([id]) ON DELETE CASCADE,
  CONSTRAINT [fk_guvenli_token_kullanici] FOREIGN KEY ([kullanici_id]) REFERENCES [users] ([id]) ON DELETE CASCADE
);
