INSERT INTO partner_detaylari (
    kullanici_id, firma_unvani, firma_turu, vergi_dairesi, vergi_numarasi,
    fatura_adresi, fatura_il, fatura_ilce, yetkili_ad_soyad, yetkili_tc_no,
    yetkili_telefon, yetkili_eposta, banka_adi, iban, hesap_sahibi_adi,
    onay_durumu, onay_tarihi
) VALUES (
    1, 'Lüks Otelcilik A.Ş.', 'Anonim Şirketi', 'Turizm Vergi Dairesi', '1234567890',
    'Lara Cad. No:123 Muratpaşa', 'Antalya', 'Muratpaşa', 'Ahmet Yılmaz', '12345678901',
    '+905301234567', 'ahmet@luksotel.com', 'İş Bankası', 'TR320006200123456789000123', 'Lüks Otelcilik A.Ş.',
    'Onaylandi', GETDATE()
);

