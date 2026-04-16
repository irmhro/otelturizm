CREATE TABLE IF NOT EXISTS [kullanici_odeme_yontemleri] (
  [id] bigint  NOT NULL IDENTITY(1,1),
  [kullanici_id] bigint  NOT NULL,
  [kart_etiketi] varchar(100) NOT NULL,
  [kart_sahibi] varchar(100) NOT NULL,
  [marka] varchar(30) NOT NULL DEFAULT 'Visa',
  [son_dort_hane] char(4) NOT NULL,
  [son_kullanim_ay] tinyint  NOT NULL,
  [son_kullanim_yil] smallint  NOT NULL,
  [varsayilan_mi] BIT NOT NULL DEFAULT 0,
  [aktif_mi] BIT NOT NULL DEFAULT 1,
  [olusturulma_tarihi] DATETIME2 NULL DEFAULT GETDATE(),
  [guncellenme_tarihi] DATETIME2 NULL DEFAULT NULL,
  PRIMARY KEY ([id]),
  KEY [idx_kullanici_odeme_yontemleri_user] ([kullanici_id]),
  CONSTRAINT [fk_kullanici_odeme_yontemleri_user] FOREIGN KEY ([kullanici_id]) REFERENCES [users] ([id]) ON DELETE CASCADE
);

INSERT INTO [kullanici_odeme_yontemleri]
([kullanici_id], [kart_etiketi], [kart_sahibi], [marka], [son_dort_hane], [son_kullanim_ay], [son_kullanim_yil], [varsayilan_mi], [aktif_mi])
SELECT u.id, 'Kişisel Kart', u.ad_soyad, 'Visa', '4242', 12, 2028, 1, 1
FROM users u
WHERE u.rol = 'user'
  AND u.eposta = 'sales.test.175455@otelturizm.com'
  AND NOT EXISTS (
      SELECT 1 FROM kullanici_odeme_yontemleri koy WHERE koy.kullanici_id = u.id
  );
