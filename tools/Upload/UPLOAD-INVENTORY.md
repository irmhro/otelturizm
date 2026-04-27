## Upload Envanteri (Paket 131–140)

Bu doküman projedeki upload giriş noktalarını ve kullanılan servisleri özetler.

### Public / Oteller

- `POST /oteller/{slug}/rezervasyon`
  - **Dosya**: `BankTransferReceipt` (pdf/jpg/png/webp)
  - **Kaydetme**: `PublicReservationService` -> `ISecureFileService` (secure storage)

### User Panel

- `POST /panel/user/profil-bilgilerim/profil-resmi-yukle`
  - **Dosya**: `profileImage` (image/*)
  - **Kaydetme**: `IImageStorageService.SaveAsWebpAsync` -> `wwwroot/uploads/user/avatars/{userId}/`

- `POST /panel/user/mesajlarim/gonder`
  - **Dosyalar**: `attachments[]` (pdf/jpg/png/webp/gif)
  - **Kaydetme**: `MessageCenterService` -> `ISecureFileService` (secure storage) + mesaj eki tablosu

### Firma Panel

- `POST /panel/firma/mesajlar/gonder`
  - **Dosyalar**: `attachments[]`
  - **Kaydetme**: `MessageCenterService` -> `ISecureFileService`

### Admin Panel

- `POST /admin/sozlesmeler/pdf-yukle`
  - **Dosya**: `pdfFile` (PDF)
  - **Kaydetme**: `ISecureFileService` (secure storage) + `sozlesme_dosyalari` (varsa)

- Otel / Oda fotoğraf upload’ları
  - **Kaydetme**: `AdminHotelManagementService` -> `IImageStorageService` -> `wwwroot/uploads/hotels/admin/...`

### Developer Panel

- (Talep görseli) `DeveloperPanelController.SaveVisualAsync(IFormFile?)`
  - **Kaydetme**: `IImageStorageService` -> `wwwroot/uploads/developer/requests/{userId}/`

