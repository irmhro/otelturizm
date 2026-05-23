## Paket 191 — `dotnet publish` üretim standardı

### Önerilen akış

1. Yerelde doğrula:
   - `dotnet build -c Release`
   - `tools/Health/Verify-Publish-ViewCompilation.ps1`
2. Klasör publish (secret içermez):
   - `dotnet publish -c Release /p:PublishProfile=FolderProfile-Release-Prod`
   - Çıktı: `bin\Release\net10.0\publish\`
3. IIS’e kopyala veya pipeline artifact olarak kullan.

### Profiller

| Profil | Amaç |
|--------|------|
| `FolderProfile-Release-Prod.pubxml` | Repo içi standart; sunucu/WMSVC bilgisi yok |
| `IISProfile.pubxml` | Ortamınıza özel Web Deploy — kimlik bilgisi **publish sırasında** verilir; repoda şifre tutulmaz |

### Komut örnekleri

```powershell
dotnet publish .\otelturizm.csproj -c Release /p:PublishProfile=FolderProfile-Release-Prod
```

Web Deploy kullanıyorsanız şifreyi CLI’dan verin (repoya yazmayın):

```powershell
dotnet publish -c Release /p:PublishProfile=IISProfile /p:Password="***"
```
