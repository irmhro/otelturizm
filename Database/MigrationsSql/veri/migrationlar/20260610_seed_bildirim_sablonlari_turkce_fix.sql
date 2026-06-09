-- Idempotent: BILDIRIM_SABLONLARI Turkce karakter duzeltmesi (yanlis kodlama / cift encode)
-- Uygulama: sqlcmd -I -f 65001 -b -i "...\20260610_seed_bildirim_sablonlari_turkce_fix.sql"
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

IF OBJECT_ID(N'dbo.BILDIRIM_SABLONLARI', N'U') IS NULL
BEGIN
    PRINT N'BILDIRIM_SABLONLARI tablosu bulunamadi, atlandi.';
    RETURN;
END;

BEGIN TRY
    BEGIN TRAN;

    ;WITH src AS
    (
        SELECT * FROM (VALUES
            (N'login_2fa_email', N'Giriş Güvenlik Kodu', N'Giriş güvenlik kodunuz', N'Giriş Güvenlik Kodu', N'Views/Email/Giris_Guvenlik_Kodu.cshtml', N'verification_code,user_first_name,login_time'),
            (N'email_verify', N'E-posta Doğrulama', N'E-posta adresinizi onaylayın', N'E-posta Doğrulama', N'Views/Email/E-posta_Adresini_Onayla.cshtml', N'user_first_name,user_email,registration_date,verification_link,verification_code'),
            (N'password_reset', N'Şifre Sıfırlama', N'Şifre sıfırlama talebi', N'Şifre Sıfırlama', N'Views/Email/Sifre_Sifirlama_Talebi.cshtml', N'user_first_name,user_email,reset_link,request_ip'),
            (N'reservation_received_customer', N'Rezervasyon Talebi Alındı', N'Rezervasyon talebiniz alındı', N'Rezervasyon Talebi Alındı', N'Views/Email/Rezervasyon_Talebi_Alindi.cshtml', N'reservation_no,hotel_name,guest_full_name,check_in_date,check_out_date,room_type_name,total_price'),
            (N'reservation_confirmed_customer', N'Rezervasyon Onaylandı', N'Rezervasyonunuz onaylandı', N'Rezervasyon Onaylandı', N'Views/Email/RezervasyonOnaylandi.cshtml', N'reservation_no,hotel_name,guest_full_name,check_in_date,check_out_date,room_type_name,total_price'),
            (N'reservation_new_partner', N'Partner Yeni Rezervasyon', N'Yeni rezervasyon onayı', N'Partner Yeni Rezervasyon', N'Views/Email/Partner_Yeni_Rezervasyon.cshtml', N'reservation_no,hotel_name,guest_full_name,check_in_date,check_out_date,room_type_name,total_price'),
            (N'reservation_rejected_customer', N'Rezervasyon Reddedildi', N'Rezervasyon talebiniz reddedildi', N'Rezervasyon Reddedildi', N'Views/Email/Rezervasyon_Reddedildi.cshtml', N'reservation_no,hotel_name,guest_full_name,cancel_reason'),
            (N'reservation_guest_message', N'Rezervasyon Mesajı', N'Rezervasyon mesajınız var', N'Rezervasyon Mesajı', N'Views/Email/Rezervasyon_Mesaji.cshtml', N'reservation_no,hotel_name,message_body,guest_full_name'),
            (N'reservation_cancelled_partner', N'Partner Rezervasyon İptal', N'Rezervasyon iptal edildi', N'Partner Rezervasyon İptal', N'Views/Email/Partner_Rezervasyon_Iptal.cshtml', N'hotel_manager_name,hotel_name,booking_reference,guest_full_name,check_in_date,check_out_date,room_type_name,total_price,cancel_reason'),
            (N'favorite_price_alert_match', N'Favori Fiyat Alarmı', N'Fiyat alarmınız tetiklendi', N'Favori Fiyat Alarmı', N'Views/Email/Favori_Fiyat_Alarmi.cshtml', N'hotel_name,room_name,old_price,new_price,check_in_date,check_out_date'),
            (N'contract_delivery', N'Sözleşme Bildirimi', N'Sözleşme ve KVKK paketi', N'Sözleşme Bildirimi', N'Views/Email/Sozlesme_Bildirimi.cshtml', N'recipient_name,module_label,contract_bundle_title,contract_sections_html,primary_contract_url'),
            (N'firma_reservation_created_company', N'Kurumsal Rezervasyon Firma Bildirimi', N'Kurumsal rezervasyon talebiniz alındı', N'Kurumsal Rezervasyon', N'Views/Email/Firma_Rezervasyon_Bildirimi.cshtml', N'reservation_no,hotel_name,company_name,check_in_date,check_out_date,room_count,total_price'),
            (N'firma_reservation_created_partner', N'Kurumsal Rezervasyon Partner Bildirimi', N'Yeni kurumsal rezervasyon talebi', N'Kurumsal Rezervasyon', N'Views/Email/Firma_Rezervasyon_Bildirimi.cshtml', N'reservation_no,hotel_name,company_name,check_in_date,check_out_date,room_count,total_price'),
            (N'system_health_link_report', N'Sistem Sağlığı Link Raporu', N'Sistem link kontrol raporu', N'Link Kontrol Raporu', N'Views/Email/Link_Kontrol_Raporu.cshtml', N'report_title,report_summary,report_items,generated_at'),
            (N'admin_routing_notice', N'Admin Yönlendirme Bildirimi', N'Admin bildirim yönlendirmesi', N'Admin Routing', N'Views/Email/tr/Admin_Routing_Bildirimi.cshtml', N'email_subject,badge,title,intro,detail_html,primary_url,primary_label,event_code,occurred_at'),
            (N'partner_facility_user_invite', N'Tesis Kullanıcı Daveti', N'Tesis kullanıcı daveti', N'Tesis Kullanıcı Daveti', N'Views/Email/Partner_Tesis_Kullanıcı_Daveti.cshtml', N'partner_name,hotel_name,invite_link,recipient_name'),
            (N'developer_feedback', N'Beta Geri Bildirim', N'[BETA BİLDİRİM] {{title}}', N'Beta Geri Bildirim', N'Views/Email/tr/Developer_Bildirim.cshtml', N'feedback_id,panel_key,feedback_type,title,content,page_url,page_title,user_full_name,user_email,account_type,ip_address,user_agent,viewport,device_info,image_url,created_at')
        ) x(SABLON_KODU, SABLON_ADI, KONU, BASLIK, ICERIK, DEGISKENLER)
    )
    UPDATE t
    SET
        t.[SABLON_ADI] = s.[SABLON_ADI],
        t.[KONU] = s.[KONU],
        t.[BASLIK] = s.[BASLIK],
        t.[ICERIK] = s.[ICERIK],
        t.[DEGISKENLER] = s.[DEGISKENLER]
    FROM [dbo].[BILDIRIM_SABLONLARI] AS t
    INNER JOIN src AS s
        ON t.[SABLON_KODU] = s.[SABLON_KODU]
       AND t.[TUR] = N'E-posta'
       AND t.[DIL] = N'tr';

    PRINT CONCAT(N'BILDIRIM_SABLONLARI Turkce duzeltme tamamlandi. Guncellenen kayit: ', @@ROWCOUNT);

    COMMIT;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH;
