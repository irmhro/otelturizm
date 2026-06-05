import html as htmlmod
import re
import shutil
import textwrap
from pathlib import Path

import pypdf
from reportlab.lib.pagesizes import A4
from reportlab.pdfgen import canvas

ROOT = Path(r"D:\otelturizm")
CONTRACTS = ROOT / "wwwroot" / "uploads" / "contracts"
TMP = ROOT / "tools" / "Db" / "_tmp"
MIGRATION = ROOT / "Database" / "MigrationsSql" / "veri" / "migrationlar" / "20260607_seed_sozlesme_icerik_pdf_partner_firma.sql"


def pdf_to_html_simple(path: Path) -> str:
    reader = pypdf.PdfReader(str(path))
    raw = "\n".join((page.extract_text() or "") for page in reader.pages)
    lines = []
    for ln in raw.replace("\r", "").split("\n"):
        ln = ln.strip()
        if not ln or "otelturizm.com |" in ln or ln in ("•",):
            continue
        if re.match(r"^\d+\s*/\s*\d+$", ln):
            continue
        lines.append(ln)

    merged = []
    buf = ""
    for ln in lines:
        if re.match(r"^\d+\.\s+[A-ZÇĞİÖŞÜ]", ln) or (ln.isupper() and 12 < len(ln) < 100):
            if buf:
                merged.append(("p", buf.strip()))
                buf = ""
            merged.append(("h2", ln))
        else:
            buf += (" " if buf else "") + ln
    if buf:
        merged.append(("p", buf.strip()))

    return "".join(f"<{tag}>{htmlmod.escape(text)}</{tag}>" for tag, text in merged)


def clean_html(content: str) -> str:
    content = re.sub(r"file:///[^\s<]+", "", content)
    content = re.sub(r"\d+\.\d+\.\d+\s+\d+:\d+", "", content)
    return content.strip()


def first_summary(content_html: str) -> str:
    match = re.search(r"<p>(.*?)</p>", content_html, re.S)
    if not match:
        return "<p>Sözleşme metni.</p>"
    text = re.sub(r"<[^>]+>", "", match.group(1))
    text = text[:280] + ("…" if len(text) > 280 else "")
    return f"<p>{htmlmod.escape(text)}</p>"


def sql_escape(value: str) -> str:
    return value.replace("'", "''")


def build_firma_kullanim_html() -> str:
    return """<h2>OTELTURİZM.COM KURUMSAL FİRMA KULLANIM SÖZLEŞMESİ</h2>
<p>Doküman Ref: OT-FRM-26/V2 | Geçerlilik Tarihi: 06 Haziran 2026</p>
<p>İşbu Sözleşme; otelturizm.com platformunu kurumsal firma hesabı ile kullanan, çalışan veya yetkili adına konaklama rezervasyonu yapan tüzel kişiler (Firma) ile Platform arasındaki hak ve yükümlülükleri düzenler.</p>
<h2>1. Taraflar ve kapsam</h2>
<p>Platform, kurumsal firma hesabı üzerinden otel arama, rezervasyon, onay akışı, faturalama ve raporlama hizmetlerini sunar. Konaklama hizmetinin ifası ilgili tesis (Partner) sorumluluğundadır.</p>
<h2>2. Hesap, yetki ve başvuru</h2>
<p>Firma; başvuru formunda verdiği unvan, vergi, yetkili ve evrak bilgilerinin doğruluğundan sorumludur. Admin onayı tamamlanmadan tam panel kullanımı ve kurumsal rezervasyon yetkisi açılmaz.</p>
<h2>3. Rezervasyon ve çalışan verileri</h2>
<p>Firma, çalışan veya misafir adına rezervasyon oluştururken doğru kimlik ve iletişim bilgisi girmekle yükümlüdür. Kişisel veriler yalnızca hizmet ifası için işlenir; KVKK aydınlatma metni geçerlidir.</p>
<h2>4. Faturalama ve ödeme</h2>
<p>Kurumsal faturalar, panelde tanımlı fatura adresi ve vergi bilgilerine göre düzenlenir. Ödeme koşulları, kampanya ve otel politikaları rezervasyon özetinde gösterilir.</p>
<h2>5. İptal, değişiklik ve no-show</h2>
<p>İptal ve iade koşulları ilgili otel politikası ve 6502 sayılı Tüketicinin Korunması Hakkında Kanun hükümlerine tabidir. Firma, çalışanları bu koşullar hakkında bilgilendirmekle yükümlüdür.</p>
<h2>6. Gizlilik ve güvenlik</h2>
<p>Firma panel erişim bilgilerinin gizliliğinden sorumludur. Yetkisiz kullanım şüphesinde Platform destek kanallarına derhal bildirim yapılır.</p>
<h2>7. Uyuşmazlık</h2>
<p>İşbu sözleşmeden doğan uyuşmazlıklarda İstanbul mahkemeleri ve icra daireleri yetkilidir. Zorunlu tüketici hükümleri saklıdır.</p>
<h2>8. Yürürlük</h2>
<p>Sözleşme, başvuru formundaki elektronik onay ile kabul edilmiş sayılır. Güncel sürüm platform üzerinden yayımlanır.</p>"""


def write_firma_pdf(html_content: str, pdf_path: Path) -> None:
    text = htmlmod.unescape(re.sub(r"<[^>]+>", "\n", html_content))
    text = re.sub(r"\n+", "\n", text).strip()
    pdf = canvas.Canvas(str(pdf_path), pagesize=A4)
    width, height = A4
    y = height - 50
    for para in text.split("\n"):
        para = para.strip()
        if not para:
            continue
        for line in textwrap.wrap(para, width=95):
            if y < 50:
                pdf.showPage()
                y = height - 50
            pdf.drawString(40, y, line)
            y -= 14
    pdf.save()


def main() -> None:
    TMP.mkdir(parents=True, exist_ok=True)
    CONTRACTS.mkdir(parents=True, exist_ok=True)

    for pdf in CONTRACTS.glob("*.pdf"):
        if pdf.name.startswith("firma-"):
            continue
        if pdf.name in {
            "kullanici-kullanim-kosullari-v2.pdf",
            "kullanici-kvkk-aydinlatma-v2.pdf",
            "partner-basvuru-sozlesmesi-v2.pdf",
            "partner-kvkk-aydinlatma-v2.pdf",
        }:
            html_path = TMP / f"{pdf.stem}.html"
            html_path.write_text(clean_html(pdf_to_html_simple(pdf)), encoding="utf-8")

    firma_html = build_firma_kullanim_html()
    (TMP / "firma-kurumsal-kullanim-kosullari-v2.html").write_text(firma_html, encoding="utf-8")
    write_firma_pdf(firma_html, CONTRACTS / "firma-kurumsal-kullanim-kosullari-v2.pdf")
    shutil.copy2(CONTRACTS / "kullanici-kvkk-aydinlatma-v2.pdf", CONTRACTS / "firma-kvkk-aydinlatma-v2.pdf")

    entries = [
        ("kullanici-kullanim-kosullari", "kullanici-kullanim-kosullari-v2.html", "kullanici-kullanim-kosullari-v2.pdf", "Kullanici Kullanim Sozlesmesi v2.pdf"),
        ("kullanici-kvkk-aydinlatma", "kullanici-kvkk-aydinlatma-v2.html", "kullanici-kvkk-aydinlatma-v2.pdf", "KVKK Aydinlatma Metni v2.pdf"),
        ("partner-basvuru-sozlesmesi", "partner-basvuru-sozlesmesi-v2.html", "partner-basvuru-sozlesmesi-v2.pdf", "Partner Kullanim Sozlesmesi v2.pdf"),
        ("partner-kvkk-aydinlatma", "partner-kvkk-aydinlatma-v2.html", "partner-kvkk-aydinlatma-v2.pdf", "Partner KVKK Aydinlatma Metni v2.pdf"),
        ("firma-kurumsal-kullanim-kosullari", "firma-kurumsal-kullanim-kosullari-v2.html", "firma-kurumsal-kullanim-kosullari-v2.pdf", "Firma Kurumsal Kullanim Sozlesmesi v2.pdf"),
        ("firma-kvkk-aydinlatma", "kullanici-kvkk-aydinlatma-v2.html", "firma-kvkk-aydinlatma-v2.pdf", "Firma KVKK Aydinlatma Metni v2.pdf"),
    ]

    lines = [
        "-- Idempotent: PDF metin icerigi + partner/firma PDF kayitlari",
        "SET NOCOUNT ON; SET XACT_ABORT ON; SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;",
        "",
    ]

    for slug, html_file, pdf_file, pdf_name in entries:
        content = clean_html((TMP / html_file).read_text(encoding="utf-8"))
        summary = first_summary(content)
        var = slug.replace("-", "_")
        lines.append(f"DECLARE @{var}_html nvarchar(max) = N'{sql_escape(content)}';")
        lines.append(f"DECLARE @{var}_ozet nvarchar(max) = N'{sql_escape(summary)}';")
        lines.append(
            f"UPDATE [dbo].[SOZLESMELER] SET [ICERIK_HTML]=@{var}_html, [OZET_HTML]=@{var}_ozet, "
            f"[VERSIYON_NO]=CASE WHEN [VERSIYON_NO]<2 THEN 2 ELSE [VERSIYON_NO] END, "
            f"[GUNCELLENME_TARIHI]=SYSUTCDATETIME() WHERE [SLUG]=N'{slug}' AND [AKTIF_MI]=1;"
        )
        lines.append("")

    lines.append("IF OBJECT_ID(N'dbo.SOZLESME_DOSYALARI', N'U') IS NOT NULL")
    lines.append("BEGIN")
    for slug, _, pdf_file, pdf_name in entries:
        pdf_url = f"/uploads/contracts/{pdf_file}"
        lines.append(
            f"    IF NOT EXISTS (SELECT 1 FROM [dbo].[SOZLESME_DOSYALARI] d "
            f"INNER JOIN [dbo].[SOZLESMELER] s ON s.[ID]=d.[SOZLESME_ID] "
            f"WHERE s.[SLUG]=N'{slug}' AND d.[DOSYA_TIPI]=N'pdf' AND d.[DOSYA_YOLU]=N'{pdf_url}')"
        )
        lines.append("    BEGIN")
        lines.append(
            f"        INSERT INTO [dbo].[SOZLESME_DOSYALARI] "
            f"([SOZLESME_ID],[DOSYA_TIPI],[DOSYA_ADI],[DOSYA_YOLU],[MIME_TIPI],[OLUSTURULMA_TARIHI]) "
            f"SELECT TOP (1) s.[ID], N'pdf', N'{sql_escape(pdf_name)}', N'{pdf_url}', N'application/pdf', SYSUTCDATETIME() "
            f"FROM [dbo].[SOZLESMELER] s WHERE s.[SLUG]=N'{slug}' AND s.[AKTIF_MI]=1 "
            f"ORDER BY s.[VERSIYON_NO] DESC, s.[ID] DESC;"
        )
        lines.append("    END;")
    lines.append("END;")
    lines.append("")
    lines.append(
        "SELECT s.[SLUG], s.[VERSIYON_NO], LEN(s.[ICERIK_HTML]) AS html_len, d.[DOSYA_YOLU] "
        "FROM [dbo].[SOZLESMELER] s LEFT JOIN [dbo].[SOZLESME_DOSYALARI] d ON d.[SOZLESME_ID]=s.[ID] AND d.[DOSYA_TIPI]=N'pdf' "
        "WHERE s.[SLUG] IN (N'partner-basvuru-sozlesmesi',N'partner-kvkk-aydinlatma',N'firma-kurumsal-kullanim-kosullari',N'firma-kvkk-aydinlatma',N'kullanici-kullanim-kosullari',N'kullanici-kvkk-aydinlatma') "
        "ORDER BY s.[SLUG], d.[ID] DESC;"
    )

    MIGRATION.write_text("\n".join(lines), encoding="utf-8-sig")
    print(f"Wrote {MIGRATION} ({MIGRATION.stat().st_size} bytes)")


if __name__ == "__main__":
    main()
