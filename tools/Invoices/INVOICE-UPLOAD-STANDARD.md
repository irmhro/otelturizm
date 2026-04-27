## Fatura Upload Standardı (Paket 158)

### Amaç

Fatura/irsaliye gibi finans dokümanlarını güvenli şekilde yüklemek ve saklamak.

### Standart

- **Servis**: `ISecureFileService` üzerinden saklama (App_Data/secure-storage)
- **Kategori**: `invoice` / `invoice-attachment`
- **Uzantılar**: PDF + gerektiğinde görsel (jpg/png/webp)
- **Boyut limiti**: 25–50MB (endpoint bazlı)
- **Magic-byte**: PDF / görsel doğrulaması (SecureFile zaten yapıyor)
- **İndirme**: `SecureFilesController` üzerinden token’lı erişim (no-store)

### Audit

- `UPLOAD_AUDIT` event (kim, boyut, sha256, bağlam)

