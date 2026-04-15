SET @firma_id := (SELECT id FROM firmalar ORDER BY id ASC LIMIT 1);
SET @firma_user_id := (SELECT id FROM users WHERE firma_id = @firma_id AND rol LIKE 'firma_%' ORDER BY id ASC LIMIT 1);
SET @user_id := (SELECT id FROM users WHERE eposta = 'reservation.draft.test@otelturizm.com' LIMIT 1);

INSERT INTO mesaj_konusmalari
(
  konusma_kodu, rezervasyon_id, otel_id, firma_id, firma_kullanici_id,
  misafir_kullanici_id, otel_yetkilisi_kullanici_id,
  konu_basligi, konusma_turu, konu_kategorisi, durum, oncelik,
  son_mesaj_tarihi, son_mesaj_gonderen, son_mesaj_onizleme,
  misafir_okunmamis_sayisi, firma_okunmamis_sayisi
)
SELECT
  CONCAT('MSG-', LPAD(COALESCE(@user_id, 1), 8, '0')),
  NULL, NULL, @firma_id, @firma_user_id,
  @user_id, NULL,
  'Kurumsal konaklama bilgilendirmesi', 'Firma', 'Firma', 'Açık', 'Normal',
  CURRENT_TIMESTAMP, 'Firma', 'Merhaba, kurumsal konaklama süreciniz için size yardımcı olabiliriz.',
  1, 0
FROM DUAL
WHERE @firma_id IS NOT NULL
  AND @user_id IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM mesaj_konusmalari
      WHERE firma_id = @firma_id
        AND misafir_kullanici_id = @user_id
        AND konusma_turu = 'Firma'
  );

SET @conversation_id := (
    SELECT id FROM mesaj_konusmalari
    WHERE firma_id = @firma_id
      AND misafir_kullanici_id = @user_id
      AND konusma_turu = 'Firma'
    ORDER BY id DESC
    LIMIT 1
);

INSERT INTO mesajlar
(
  konusma_id, gonderen_turu, gonderen_kullanici_id, gonderen_firma_id, gonderen_firma_kullanici_id,
  mesaj_metni, mesaj_tipi, okundu_mu, durum, gonderim_tarihi
)
SELECT
  @conversation_id, 'Firma', @firma_user_id, @firma_id, @firma_user_id,
  'Merhaba, toplu konaklama veya çalışan rezervasyonları için bu güvenli alandan dosya ve belge paylaşabilirsiniz.',
  'Metin', 0, 'Gönderildi', CURRENT_TIMESTAMP
FROM DUAL
WHERE @conversation_id IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM mesajlar
      WHERE konusma_id = @conversation_id
        AND gonderen_turu = 'Firma'
  );
