CREATE TABLE IF NOT EXISTS [kullanici_bildirim_tercihleri] (
  [id] bigint  NOT NULL IDENTITY(1,1),
  [kullanici_id] bigint  NOT NULL,
  [rezervasyon_eposta] BIT NOT NULL DEFAULT 1,
  [rezervasyon_push] BIT NOT NULL DEFAULT 1,
  [checkin_hatirlatma] BIT NOT NULL DEFAULT 1,
  [iptal_degisim] BIT NOT NULL DEFAULT 1,
  [kampanya_eposta] BIT NOT NULL DEFAULT 0,
  [kampanya_sms] BIT NOT NULL DEFAULT 0,
  [sistem_bildirimi] BIT NOT NULL DEFAULT 1,
  [olusturulma_tarihi] DATETIME2 NULL DEFAULT GETDATE(),
  [guncellenme_tarihi] DATETIME2 NULL DEFAULT NULL,
  PRIMARY KEY ([id]),
  UNIQUE KEY [uk_kullanici_bildirim_tercihleri_user] ([kullanici_id]),
  CONSTRAINT [fk_kullanici_bildirim_tercihleri_user] FOREIGN KEY ([kullanici_id]) REFERENCES [users] ([id]) ON DELETE CASCADE
);

INSERT INTO [kullanici_bildirim_tercihleri] ([kullanici_id])
SELECT [id]
FROM [users]
WHERE [rol] = 'user'
  AND NOT EXISTS (
      SELECT 1
      FROM [kullanici_bildirim_tercihleri] kb
      WHERE kb.[kullanici_id] = [users].[id]
  );
