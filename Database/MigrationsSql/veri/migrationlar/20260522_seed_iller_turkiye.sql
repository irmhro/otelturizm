/* dbo.ILLER — 81 il - UTF-8, sqlcmd -f 65001 */
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.ILLER', N'U') IS NULL RETURN;
-- Idempotent seed: mevcut il/ilce/mahalle verisini silmez, MERGE ile gunceller/ekler.
GO

IF OBJECT_ID('tempdb..#il_seed') IS NOT NULL DROP TABLE #il_seed;
CREATE TABLE #il_seed (
    PLAKA smallint NOT NULL PRIMARY KEY,
    IL_ADI nvarchar(100) NOT NULL,
    SEO_SLUG nvarchar(120) NOT NULL,
    BOLGE nvarchar(50) NULL,
    ENLEM decimal(10,8) NULL,
    BOYLAM decimal(11,8) NULL,
    NUFUS int NULL
);
GO

INSERT INTO #il_seed (PLAKA, IL_ADI, SEO_SLUG, BOLGE, ENLEM, BOYLAM, NUFUS) VALUES
(1,N'Adana',N'adana',N'Akdeniz',36.9863599,35.3252861,2283609),
(2,N'Adıyaman',N'adiyaman',N'Güneydoğu Anadolu',37.7602985,38.2772986,617821),
(3,N'Afyonkarahisar',N'afyonkarahisar',N'Ege',38.7568597,30.5387044,751808),
(4,N'Ağrı',N'agri',N'Doğu Anadolu',39.719125,43.0504894,491489),
(5,N'Amasya',N'amasya',N'Karadeniz',40.6503248,35.8329148,342242),
(6,N'Ankara',N'ankara',N'İç Anadolu',39.9207759,32.8540497,5910320),
(7,N'Antalya',N'antalya',N'Akdeniz',36.8865728,30.7030242,2777677),
(8,N'Artvin',N'artvin',N'Karadeniz',41.1830811,41.8287448,167531),
(9,N'Aydın',N'aydin',N'Ege',37.8483767,27.8435878,1172107),
(10,N'Balıkesir',N'balikesir',N'Marmara',39.6473917,27.8879787,1284517),
(11,N'Bilecik',N'bilecik',N'Marmara',40.1435101,29.9752911,228995),
(12,N'Bingöl',N'bingol',N'Doğu Anadolu',38.8861265,40.4972333,282299),
(13,N'Bitlis',N'bitlis',N'Doğu Anadolu',38.4020539,42.1084568,360423),
(14,N'Bolu',N'bolu',N'Karadeniz',40.7332953,31.6110479,327173),
(15,N'Burdur',N'burdur',N'Akdeniz',37.7248394,30.2887286,277226),
(16,N'Bursa',N'bursa',N'Marmara',40.1825734,29.0675039,3263011),
(17,N'Çanakkale',N'canakkale',N'Marmara',40.146271,26.4028892,573976),
(18,N'Çankırı',N'cankiri',N'Marmara',40.5971947,33.6212704,200549),
(19,N'Çorum',N'corum',N'Karadeniz',40.5499106,34.9537344,519590),
(20,N'Denizli',N'denizli',N'Ege',37.7827875,29.0966476,1060975),
(21,N'Diyarbakır',N'diyarbakir',N'Güneydoğu Anadolu',37.9162222,40.2363542,1852356),
(22,N'Edirne',N'edirne',N'Marmara',41.6759327,26.5587225,422438),
(23,N'Elazığ',N'elazig',N'Doğu Anadolu',38.6747164,39.2227135,605678),
(24,N'Erzincan',N'erzincan',N'Doğu Anadolu',39.7467552,39.49103,239625),
(25,N'Erzurum',N'erzurum',N'Doğu Anadolu',39.90632,41.2727715,736877),
(26,N'Eskişehir',N'eskisehir',N'İç Anadolu',39.7743941,30.519116,927956),
(27,N'Gaziantep',N'gaziantep',N'Güneydoğu Anadolu',37.0628317,37.3792617,2222415),
(28,N'Giresun',N'giresun',N'Karadeniz',40.9174453,38.3847864,455074),
(29,N'Gümüşhane',N'gumushane',N'Karadeniz',40.4617844,39.4757339,138807),
(30,N'Hakkari',N'hakkari',N'Güneydoğu Anadolu',37.5766959,43.7377862,279681),
(31,N'Hatay',N'hatay',N'Akdeniz',36.2025471,36.1602908,1577531),
(32,N'Isparta',N'isparta',N'Akdeniz',37.7636722,30.5550569,445303),
(33,N'Mersin',N'mersin',N'Akdeniz',36.7978381,34.6298391,1956428),
(34,N'İstanbul',N'istanbul',N'Marmara',41.006381,28.9758715,15754053),
(35,N'İzmir',N'izmir',N'Ege',38.4192537,27.128469,4504185),
(36,N'Kars',N'kars',N'Doğu Anadolu',40.6070761,43.0947521,268991),
(37,N'Kastamonu',N'kastamonu',N'Karadeniz',41.3765359,33.7770087,379934),
(38,N'Kayseri',N'kayseri',N'İç Anadolu',38.7219011,35.4873214,1458991),
(39,N'Kırklareli',N'kirklareli',N'Marmara',41.7370223,27.2235523,379595),
(40,N'Kırşehir',N'kirsehir',N'İç Anadolu',39.1461142,34.1605587,242777);

INSERT INTO #il_seed (PLAKA, IL_ADI, SEO_SLUG, BOLGE, ENLEM, BOYLAM, NUFUS) VALUES
(41,N'Kocaeli',N'kocaeli',N'Marmara',40.7653892,29.9407361,2161171),
(42,N'Konya',N'konya',N'İç Anadolu',37.872734,32.4924376,2343409),
(43,N'Kütahya',N'kutahya',N'Ege',39.4199106,29.9857886,570478),
(44,N'Malatya',N'malatya',N'Doğu Anadolu',38.3487153,38.3190674,755854),
(45,N'Manisa',N'manisa',N'Ege',38.6125793,27.4333969,1477756),
(46,N'Kahramanmaraş',N'kahramanmaras',N'Akdeniz',37.5812744,36.927509,1146278),
(47,N'Mardin',N'mardin',N'Güneydoğu Anadolu',37.3132581,40.7354383,903576),
(48,N'Muğla',N'mugla',N'Ege',37.2151784,28.363733,1099547),
(49,N'Muş',N'mus',N'Doğu Anadolu',38.7322221,41.4898925,389127),
(50,N'Nevşehir',N'nevsehir',N'İç Anadolu',38.6250389,34.7150685,320150),
(51,N'Niğde',N'nigde',N'İç Anadolu',37.969849,34.6764495,374492),
(52,N'Ordu',N'ordu',N'Karadeniz',40.9852301,37.8797732,768087),
(53,N'Rize',N'rize',N'Karadeniz',41.0248249,40.5199142,346947),
(54,N'Sakarya',N'sakarya',N'Marmara',40.7726291,30.4038575,1123693),
(55,N'Samsun',N'samsun',N'Karadeniz',41.2946149,36.3320596,1392403),
(56,N'Siirt',N'siirt',N'Güneydoğu Anadolu',37.9273623,41.94218,332369),
(57,N'Sinop',N'sinop',N'Karadeniz',42.0265798,35.1511512,225848),
(58,N'Sivas',N'sivas',N'İç Anadolu',39.7503574,37.0145173,631401),
(59,N'Tekirdağ',N'tekirdag',N'Marmara',40.9781214,27.5107799,1208441),
(60,N'Tokat',N'tokat',N'Karadeniz',40.3233534,36.552162,614141),
(61,N'Trabzon',N'trabzon',N'Karadeniz',41.0054605,39.7301463,823323),
(62,N'Tunceli',N'tunceli',N'Doğu Anadolu',39.1060641,39.5482693,85083),
(63,N'Şanlıurfa',N'sanliurfa',N'Güneydoğu Anadolu',37.1596239,38.791929,2265800),
(64,N'Uşak',N'usak',N'Ege',38.6740401,29.4058419,374405),
(65,N'Van',N'van',N'Doğu Anadolu',38.500875,43.3946051,1112013),
(66,N'Yozgat',N'yozgat',N'İç Anadolu',39.8221974,34.8080972,413208),
(67,N'Zonguldak',N'zonguldak',N'Karadeniz',41.4526765,31.787598,585203),
(68,N'Aksaray',N'aksaray',N'İç Anadolu',38.3705416,34.026907,441136),
(69,N'Bayburt',N'bayburt',N'Karadeniz',40.2551608,40.2205036,82836),
(70,N'Karaman',N'karaman',N'İç Anadolu',37.1808502,33.2194554,262355),
(71,N'Kırıkkale',N'kirikkale',N'İç Anadolu',39.84104835,33.5058536,282830),
(72,N'Batman',N'batman',N'Güneydoğu Anadolu',37.8835738,41.1277565,662626),
(73,N'Şırnak',N'sirnak',N'Güneydoğu Anadolu',37.5219577,42.4570311,573666),
(74,N'Bartın',N'bartin',N'Karadeniz',41.6350461,32.3366205,206663),
(75,N'Ardahan',N'ardahan',N'Doğu Anadolu',41.1102966,42.7035585,90392),
(76,N'Iğdır',N'igdir',N'Doğu Anadolu',39.9218784,44.0467957,205071),
(77,N'Yalova',N'yalova',N'Marmara',40.6582529,29.2699916,311635),
(78,N'Karabük',N'karabuk',N'Karadeniz',41.1955402,32.6231154,249614),
(79,N'Kilis',N'kilis',N'Güneydoğu Anadolu',36.7165552,37.1146069,157363),
(80,N'Osmaniye',N'osmaniye',N'Akdeniz',37.0733588,36.2507673,564123);

INSERT INTO #il_seed (PLAKA, IL_ADI, SEO_SLUG, BOLGE, ENLEM, BOYLAM, NUFUS) VALUES
(81,N'Düzce',N'duzce',N'Karadeniz',40.8391531,31.1595361,415622);

GO

MERGE [dbo].[ILLER] AS t
USING #il_seed AS s ON t.[PLAKA_KODU] = s.[PLAKA]
WHEN MATCHED THEN UPDATE SET
    [IL_ADI]=s.[IL_ADI],[SEO_SLUG]=s.[SEO_SLUG],[BOLGE]=s.[BOLGE],
    [ENLEM]=s.[ENLEM],[BOYLAM]=s.[BOYLAM],[NUFUS]=s.[NUFUS],
    [AKTIF_MI]=1,[GUNCELLENME_TARIHI]=sysutcdatetime()
WHEN NOT MATCHED THEN INSERT ([PLAKA_KODU],[IL_ADI],[SEO_SLUG],[BOLGE],[ENLEM],[BOYLAM],[NUFUS],[AKTIF_MI])
    VALUES (s.[PLAKA],s.[IL_ADI],s.[SEO_SLUG],s.[BOLGE],s.[ENLEM],s.[BOYLAM],s.[NUFUS],1);
GO
-- 81 il
