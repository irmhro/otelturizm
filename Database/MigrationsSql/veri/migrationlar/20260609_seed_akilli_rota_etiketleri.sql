SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.AKILLI_ROTA', N'U') IS NULL
BEGIN
    RAISERROR(N'AKILLI_ROTA tablosu bulunamadi. Once 20260609_sqlserver_akilli_rota.sql calistirin.', 16, 1);
    RETURN;
END

CREATE TABLE #akilli_rota_seed
(
    [ETIKET_KODU] nvarchar(80) NOT NULL PRIMARY KEY,
    [ETIKET_ADI] nvarchar(120) NOT NULL,
    [HASHTAG] nvarchar(120) NOT NULL,
    [ARAMA_METNI] nvarchar(500) NULL,
    [RENK_SINIFI] nvarchar(20) NOT NULL,
    [SIRA_NO] int NOT NULL
);

INSERT INTO #akilli_rota_seed ([ETIKET_KODU], [ETIKET_ADI], [HASHTAG], [ARAMA_METNI], [RENK_SINIFI], [SIRA_NO]) VALUES
(N'sakinlik-arayanlar', N'SakinlikArayanlar', N'#SakinlikArayanlar', N'sakin ve huzurlu ortam, sessiz tatil', N'sage', 1),
(N'cocuk-dostu', N'ÇocukDostu', N'#ÇocukDostu', N'çocuk dostu, aile oteli, mini club', N'amber', 2),
(N'gurme-deneyimi', N'GurmeDeneyimi', N'#GurmeDeneyimi', N'gurme deneyim, fine dining, şef restoranı', N'wine', 3),
(N'ozel-havuzlu', N'ÖzelHavuzlu', N'#ÖzelHavuzlu', N'özel havuz, suite, jakuzi', N'sky', 4),
(N'denize-sifir', N'DenizeSıfır', N'#DenizeSıfır', N'denize sıfır, plaj, kumsal', N'ocean', 5),
(N'spa-wellness', N'SpaWellness', N'#SpaWellness', N'spa ve wellness, masaj, hamam', N'violet', 6),
(N'romantik-kacamak', N'RomantikKaçamak', N'#RomantikKaçamak', N'romantik kaçamak, çift tatili', N'wine', 7),
(N'balayi-otelleri', N'BalayıOtelleri', N'#BalayıOtelleri', N'balayı, honeymoon suite', N'wine', 8),
(N'aile-tatili', N'AileTatili', N'#AileTatili', N'aile tatili, geniş oda, çocuk aktivite', N'amber', 9),
(N'luks-konaklama', N'LüksKonaklama', N'#LüksKonaklama', N'lüks konaklama, premium hizmet', N'wine', 10),
(N'butik-otel', N'ButikOtel', N'#ButikOtel', N'butik otel, tasarım otel', N'sage', 11),
(N'tarihi-yarimada', N'TarihiYarımada', N'#TarihiYarımada', N'tarihi yarımada, kültür turu', N'amber', 12),
(N'kapadokya-balon', N'KapadokyaBalon', N'#KapadokyaBalon', N'kapadokya, balon turu, mağara otel', N'sky', 13),
(N'kayak-tatili', N'KayakTatili', N'#KayakTatili', N'kayak tatili, kayak merkezi', N'ocean', 14),
(N'termal-konaklama', N'TermalKonaklama', N'#TermalKonaklama', N'termal konaklama, kaplıca', N'violet', 15),
(N'doga-icinde', N'Doğaİçinde', N'#Doğaİçinde', N'doğa içinde, orman, yeşil alan', N'sage', 16),
(N'sehir-merkezi', N'ŞehirMerkezi', N'#ŞehirMerkezi', N'şehir merkezi, merkezi konum', N'amber', 17),
(N'havaalani-yakin', N'HavaalanıYakın', N'#HavaalanıYakın', N'havaalanı yakın, transfer kolay', N'sky', 18),
(N'pet-dostu', N'PetDostu', N'#PetDostu', N'pet dostu, evcil hayvan kabul', N'sage', 19),
(N'engelsiz-erisim', N'EngelsizErişim', N'#EngelsizErişim', N'engelsiz erişim, tekerlekli sandalye', N'ocean', 20),
(N'all-inclusive', N'AllInclusive', N'#AllInclusive', N'all inclusive, her şey dahil', N'amber', 21),
(N'yari-pansiyon', N'YarımPansiyon', N'#YarımPansiyon', N'yarım pansiyon, akşam yemeği dahil', N'sky', 22),
(N'oda-kahvalti', N'OdaKahvaltı', N'#OdaKahvaltı', N'oda kahvaltı, kahvaltı dahil', N'sky', 23),
(N'vegan-dostu', N'VeganDostu', N'#VeganDostu', N'vegan dostu, bitkisel menü', N'sage', 24),
(N'helal-tatil', N'HelalTatil', N'#HelalTatil', N'helal tatil, helal konsept', N'amber', 25),
(N'yetişkinlere-ozel', N'YetişkinlereÖzel', N'#YetişkinlereÖzel', N'yetişkinlere özel, adults only', N'wine', 26),
(N'genc-enerjisi', N'GençEnerjisi', N'#GençEnerjisi', N'genç enerjisi, sosyal ortam', N'violet', 27),
(N'gece-hayati', N'GeceHayatı', N'#GeceHayatı', N'gece hayatı, canlı müzik', N'violet', 28),
(N'golf-tatili', N'GolfTatili', N'#GolfTatili', N'golf tatili, golf sahası', N'sage', 29),
(N'yat-marina', N'YatMarina', N'#YatMarina', N'yat marina, tekne turu', N'ocean', 30),
(N'dag-manzarasi', N'DağManzarası', N'#DağManzarası', N'dağ manzarası, dağ oteli', N'sage', 31),
(N'gol-kenari', N'GölKenarı', N'#GölKenarı', N'göl kenarı, göl manzarası', N'ocean', 32),
(N'orman-ici', N'Ormanİçi', N'#Ormanİçi', N'orman içi, doğa oteli', N'sage', 33),
(N'organik-yasam', N'OrganikYaşam', N'#OrganikYaşam', N'organik yaşam, sürdürülebilir', N'sage', 34),
(N'yuruyus-rotalari', N'YürüyüşRotaları', N'#YürüyüşRotaları', N'yürüyüş rotaları, trekking', N'sage', 35),
(N'bisiklet-rotasi', N'BisikletRotası', N'#BisikletRotası', N'bisiklet rotası, bike friendly', N'sky', 36),
(N'dalis-merkezi', N'DalışMerkezi', N'#DalışMerkezi', N'dalış merkezi, scuba', N'ocean', 37),
(N'kitesurf', N'Kitesurf', N'#Kitesurf', N'kitesurf, rüzgar sporları', N'ocean', 38),
(N'yoga-retreat', N'YogaRetreat', N'#YogaRetreat', N'yoga retreat, meditasyon', N'violet', 39),
(N'detoks-programi', N'DetoksProgramı', N'#DetoksProgramı', N'detoks programı, sağlıklı yaşam', N'violet', 40),
(N'business-travel', N'BusinessTravel', N'#BusinessTravel', N'business travel, iş seyahati', N'sky', 41),
(N'uzaktan-calisma', N'UzaktanÇalışma', N'#UzaktanÇalışma', N'uzaktan çalışma, workation', N'sky', 42),
(N'toplanti-salonu', N'ToplantıSalonu', N'#ToplantıSalonu', N'toplantı salonu, konferans', N'amber', 43),
(N'mice-hizmeti', N'MICEHizmeti', N'#MICEHizmeti', N'MICE hizmeti, kurumsal etkinlik', N'amber', 44),
(N'dugun-organizasyon', N'DüğünOrganizasyon', N'#DüğünOrganizasyon', N'düğün organizasyon, nikah', N'wine', 45),
(N'ozel-etkinlik', N'ÖzelEtkinlik', N'#ÖzelEtkinlik', N'özel etkinlik, kutlama', N'wine', 46),
(N'genis-aile', N'GenişAile', N'#GenişAile', N'geniş aile, bağlantılı oda', N'amber', 47),
(N'ucuz-tatil', N'UcuzTatil', N'#UcuzTatil', N'ucuz tatil, ekonomik konaklama', N'sky', 48),
(N'son-dakika', N'SonDakika', N'#SonDakika', N'son dakika, erken rezervasyon', N'sky', 49),
(N'hafta-sonu', N'HaftaSonu', N'#HaftaSonu', N'hafta sonu kaçamağı', N'amber', 50),
(N'uzun-konaklama', N'UzunKonaklama', N'#UzunKonaklama', N'uzun konaklama, extended stay', N'sage', 51),
(N'mevsimlik', N'Mevsimlik', N'#Mevsimlik', N'mevsimlik tatil, yaz kış', N'ocean', 52),
(N'antalya-gunesi', N'AntalyaGüneşi', N'#AntalyaGüneşi', N'antalya güneşi, akdeniz', N'ocean', 53),
(N'ege-esintisi', N'EgeEsintisi', N'#EgeEsintisi', N'ege esintisi, ege kıyıları', N'ocean', 54),
(N'karadeniz-sis', N'KaradenizSis', N'#KaradenizSis', N'karadeniz sis, yayla', N'sage', 55),
(N'gun-dogumu', N'GünDoğumu', N'#GünDoğumu', N'gün doğumu, manzara', N'amber', 56),
(N'manzara-keyfi', N'ManzaraKeyfi', N'#ManzaraKeyfi', N'manzara keyfi, panorama', N'sky', 57),
(N'rooftop-bar', N'RooftopBar', N'#RooftopBar', N'rooftop bar, teras', N'violet', 58),
(N'historical-boutique', N'TarihiKonak', N'#TarihiKonak', N'tarihi konak, butik konak', N'amber', 59),
(N'cave-hotel', N'MağaraOtel', N'#MağaraOtel', N'mağara otel, cave hotel', N'sky', 60);

MERGE [dbo].[AKILLI_ROTA] AS target
USING #akilli_rota_seed AS source
    ON target.[ETIKET_KODU] = source.[ETIKET_KODU]
WHEN MATCHED THEN
    UPDATE SET
        [ETIKET_ADI] = source.[ETIKET_ADI],
        [HASHTAG] = source.[HASHTAG],
        [ARAMA_METNI] = source.[ARAMA_METNI],
        [RENK_SINIFI] = source.[RENK_SINIFI],
        [SIRA_NO] = source.[SIRA_NO],
        [AKTIF_MI] = 1,
        [GUNCELLENME_TARIHI] = sysutcdatetime()
WHEN NOT MATCHED THEN
    INSERT ([ETIKET_KODU], [ETIKET_ADI], [HASHTAG], [ARAMA_METNI], [RENK_SINIFI], [SIRA_NO], [AKTIF_MI])
    VALUES (source.[ETIKET_KODU], source.[ETIKET_ADI], source.[HASHTAG], source.[ARAMA_METNI], source.[RENK_SINIFI], source.[SIRA_NO], 1);

DROP TABLE #akilli_rota_seed;
GO
