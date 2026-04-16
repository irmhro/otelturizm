CREATE TABLE IF NOT EXISTS [otel_rakip_analizi] (
  [id] bigint  NOT NULL IDENTITY(1,1),
  [otel_id] bigint  NOT NULL,
  [rakip_otel_adi] varchar(200) COLLATE utf8mb4_unicode_ci NOT NULL,
  [rakip_sehir] varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  [rakip_ilce] varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  [analiz_tarihi] date NOT NULL,
  [ortalama_gecelik_fiyat] decimal(10,2) DEFAULT NULL,
  [tahmini_doluluk_orani] decimal(5,2) DEFAULT NULL,
  [kaynak_url] varchar(500) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  [notlar] NVARCHAR(MAX) COLLATE utf8mb4_unicode_ci,
  PRIMARY KEY ([id]),
  KEY [idx_otel_rakip_analizi_otel_tarih] ([otel_id],[analiz_tarihi]),
  CONSTRAINT [fk_otel_rakip_analizi_otel] FOREIGN KEY ([otel_id]) REFERENCES [oteller] ([id]) ON DELETE CASCADE
);
