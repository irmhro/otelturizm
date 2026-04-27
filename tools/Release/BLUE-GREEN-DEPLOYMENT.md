# Blue-green deployment (paket 223)

**Amaç:** Yeni sürümü yeşil slot’ta doğrula; trafiği tam kesmeden mavi/yedek sürümü hazır tut.

Özet adımlar:

1. Yeni deployment’ı ikinci fiziksel veya mantıksal site/slot’a yayınlayın (`dotnet publish` profili aynı kalır).
2. Dahili smoke: `/admin/sistem-sagligi`, `/health` (varsa), kritik müşteri URL’leri.
3. Reverse proxy’de `Host`/upstream’i yeşile çevirin veya cookie tabanlı pilot yönlendirme kullanın.
4. Sorun halinde tek adımda eski (mavi) slot’a geri dönün; DB migration’ları geri almayı gerektirmeyecek şekilde tasarlayın veya önce şema uyumluluğunu doğrulayın.
