from __future__ import annotations

import hashlib
import html
import json
import sys
import re
import shutil
from dataclasses import dataclass
from datetime import date, datetime, timedelta
from decimal import Decimal, InvalidOperation
from io import BytesIO
from pathlib import Path
from typing import Any
from urllib.parse import urljoin

import pyodbc
import requests
from bs4 import BeautifulSoup
from PIL import Image, UnidentifiedImageError


ROOT = Path(__file__).resolve().parents[2]
IMPORT_ROOT = ROOT / "App_Data" / "imports" / "216suites"
HOTEL_UPLOAD_ROOT = ROOT / "wwwroot" / "uploads" / "216suites" / "hotels"
ROOM_UPLOAD_ROOT = ROOT / "wwwroot" / "uploads" / "216suites" / "rooms"
BASE_URL = "https://216suites.com"
LOCAL_CONN_STR = r"DRIVER={ODBC Driver 17 for SQL Server};SERVER=(localdb)\MSSQLLocalDB;DATABASE=otelturizm_2026db;Trusted_Connection=yes;"
DEFAULT_PASSWORD = "216Suites2026!"
DEFAULT_PHONE = "4440216"
TODAY = date(2026, 4, 24)
DEFAULT_CITY = "İstanbul"
DEFAULT_COUNTRY = "Türkiye"


EMAIL_MAP: dict[str, str] = {
    "216-eagle-palace": "eaglepalaceonline@gmail.com",
    "216-comfort-inn": "comforttsuite@gmail.com",
    "216-palace": "216palace216hotel@gmail.com",
    "216-hill": "pendikhillsuites@gmail.com",
    "216-pasha-palace": "pashapalacehotel@gmail.com",
    "216-f&b": "info@216suites.com",
    "216-macity": "rhisshotelmaltepe21@gmail.com",
    "216-trend": "216umraniyesuite@gmail.com",
    "216-bosphorus": "216bosphorus@gmail.com",
    "216-station": "216station@gmail.com",
    "216-ruby": "216rubysuite@gmail.com",
    "216-style": "216stylesuite@gmail.com",
    "216-star": "216starsuit@gmail.com",
    "216-north": "onboarding+216-north@otelturizm.local",
    "216-silver": "216silvertuzla@gmail.com",
    "216-castle": "216castlehotel@gmail.com",
    "216-prestige": "onboarding+216-prestige@otelturizm.local",
}


@dataclass
class RoomData:
    source_room_id: int
    slug: str
    name: str
    category: str
    capacity_total: int
    capacity_adult: int
    capacity_child: int
    nightly_price: Decimal
    summary: str
    size_sqm: int | None
    bed_type: str | None
    bathroom: str | None
    general: str | None
    card_conditions: list[str]
    cover_source_url: str | None
    image_urls: list[str]
    imported_cover_url: str | None = None
    imported_gallery_urls: list[str] | None = None
    feature_names: list[str] | None = None


@dataclass
class HotelData:
    slug: str
    name: str
    short_description: str
    description: str
    location_description: str
    address: str
    city: str
    district: str
    neighborhood: str | None
    latitude: Decimal | None
    longitude: Decimal | None
    star_count: int | None
    check_in_time: str
    check_out_time: str
    children_policy: str
    pet_policy: str
    features: list[str]
    rules: dict[str, str]
    page_url: str
    owner_email: str
    owner_name: str
    images: list[str]
    cover_import_url: str | None = None
    imported_image_urls: list[str] | None = None
    rooms: list[RoomData] | None = None


def slugify(text: str) -> str:
    value = html.unescape(text or "").strip().lower()
    repl = {
        "ı": "i",
        "ğ": "g",
        "ü": "u",
        "ş": "s",
        "ö": "o",
        "ç": "c",
        "İ": "i",
        "Ğ": "g",
        "Ü": "u",
        "Ş": "s",
        "Ö": "o",
        "Ç": "c",
        "&": "and",
    }
    for src, dst in repl.items():
        value = value.replace(src, dst)
    value = re.sub(r"[^a-z0-9]+", "-", value)
    return value.strip("-")


def decode_text(value: str | None) -> str:
    if not value:
        return ""
    return re.sub(r"\s+", " ", html.unescape(value)).strip()


def sha256_hex(value: str) -> str:
    return hashlib.sha256(value.encode("utf-8")).hexdigest()


def parse_decimal_from_price(value: str | None) -> Decimal:
    raw = decode_text(value).replace("₺", "").replace("TRY", "").replace(".", "").replace(",", ".")
    try:
        return Decimal(raw)
    except (InvalidOperation, TypeError):
        return Decimal("0")


def parse_capacity(value: str) -> tuple[int, int, int]:
    match = re.search(r"(\d+)", decode_text(value))
    total = int(match.group(1)) if match else 2
    adult = min(total, 2 if total <= 2 else total)
    child = max(0, total - adult)
    return total, adult, child


def parse_star_count(description: str) -> int | None:
    match = re.search(r"(\d)\s*yıldız", description, re.IGNORECASE)
    if match:
        return int(match.group(1))
    return None


def parse_map_coords(iframe_src: str | None) -> tuple[Decimal | None, Decimal | None]:
    if not iframe_src:
        return None, None
    match = re.search(r"!2d([0-9.\-]+)!3d([0-9.\-]+)", iframe_src)
    if not match:
        return None, None
    return Decimal(match.group(2)), Decimal(match.group(1))


def parse_address(meta_keywords: str, hotel_name: str) -> str:
    content = decode_text(meta_keywords)
    if not content:
        return ""
    content = re.sub(rf"^{re.escape(hotel_name)}\s*,\s*", "", content, flags=re.IGNORECASE)
    content = re.split(r",\s*otel\b", content, flags=re.IGNORECASE)[0]
    return content.strip(" ,")


def extract_city_district(address: str) -> tuple[str, str]:
    cleaned = decode_text(address)
    if "/" in cleaned:
        left, right = cleaned.rsplit("/", 1)
        city = right.strip().title()
        district = left.split()[-1].strip(", ").title()
        return city or DEFAULT_CITY, district or "Merkez"
    return DEFAULT_CITY, "Merkez"


def guess_neighborhood(address: str) -> str | None:
    cleaned = decode_text(address)
    parts = re.split(r",", cleaned)
    if parts:
        first = parts[0].strip()
        if first:
            return first
    return None


def room_category_from_name(name: str) -> str:
    upper = name.upper()
    if "FAMILY" in upper:
        return "Aile Odası"
    if "SUITE" in upper or "VIP" in upper:
        return "Suite"
    if "DELUXE" in upper or "PREMIUM" in upper:
        return "Deluxe"
    if "KING" in upper:
        return "Deluxe"
    if "TWIN" in upper or "DOUBLE" in upper or "CLASSIC" in upper:
        return "Standart"
    return "Standart"


def sanitize_feature_name(name: str) -> str:
    value = decode_text(name)
    replacements = {
        "Internet": "Ücretsiz WiFi",
        "WiFi": "Ücretsiz WiFi",
        "Telefon": "Oda Telefonu",
        "İkramlar": "İkramlar",
        "Mini Buz Dolabı": "Minibar",
    }
    value = replacements.get(value, value)
    return truncate(value, 100)


def split_features(raw: str | None) -> list[str]:
    if not raw:
        return []
    tokens = re.split(r",|/|;|\||\n|\r|•|\.", decode_text(raw))
    parts: list[str] = []
    for token in tokens:
        cleaned = sanitize_feature_name(token)
        if not cleaned:
            continue
        if len(cleaned) > 80 and " " in cleaned:
            continue
        if re.search(r"\d+\s*m²", cleaned, re.IGNORECASE):
            continue
        parts.append(cleaned)
    return [x for x in parts if x]


def truncate(value: str | None, limit: int) -> str:
    text = decode_text(value)
    return text[:limit].strip()


def summarize_children_policy(value: str | None) -> str:
    text = decode_text(value)
    if not text:
        return "0-6 yaş"

    ages = [int(match) for match in re.findall(r"(\d+)\s*yaş", text, re.IGNORECASE)]
    if ages:
        upper = max(ages)
        label = f"0-{upper} yaş"
        return truncate(label, 20) or "0-6 yaş"

    if "bebek" in text.lower():
        return "Bebek kabul edilir"

    return truncate(text, 20) or "0-6 yaş"


def ensure_clean_dir(path: Path) -> None:
    if path.exists():
        shutil.rmtree(path, ignore_errors=True)
    path.mkdir(parents=True, exist_ok=True)


def download_and_convert_to_webp(session: requests.Session, source_url: str, dest_path: Path) -> tuple[str, int, int, int]:
    response = session.get(source_url, timeout=30)
    response.raise_for_status()
    data = response.content
    dest_path.parent.mkdir(parents=True, exist_ok=True)
    with Image.open(BytesIO(data)) as img:
        image = img.convert("RGBA") if img.mode in ("P", "RGBA", "LA") else img.convert("RGB")
        image.save(dest_path, format="WEBP", quality=82, method=6)
        width, height = image.size
    size_kb = max(1, round(dest_path.stat().st_size / 1024))
    return str(dest_path), size_kb, width, height


def scrape_homepage(session: requests.Session) -> list[str]:
    response = session.get(BASE_URL, timeout=30)
    response.raise_for_status()
    soup = BeautifulSoup(response.text, "html.parser")
    links: list[str] = []
    for anchor in soup.select('a[href^="/oteller/"]'):
        href = anchor.get("href", "").strip()
        if not href:
            continue
        full = urljoin(BASE_URL, href)
        if full not in links:
            links.append(full)
    return links


def scrape_hotel(session: requests.Session, url: str) -> HotelData:
    response = session.get(url, timeout=30)
    response.raise_for_status()
    soup = BeautifulSoup(response.text, "html.parser")

    slug = url.rstrip("/").split("/")[-1]
    ld_hotel_name = ""
    for script in soup.select('script[type="application/ld+json"]'):
        script_text = script.get_text(strip=True)
        if '"@type": "Hotel"' in script_text:
            name_match = re.search(r'"name"\s*:\s*"([^"]+)"', script_text)
            if name_match:
                ld_hotel_name = decode_text(name_match.group(1))
                break
    hotel_name = ld_hotel_name or decode_text(soup.title.text.replace(" - 216 Suites", "") if soup.title else slug.replace("-", " ").title())

    meta_keywords = soup.select_one('meta[name="keywords"]')
    meta_description = soup.select_one('meta[name="description"]')
    address = parse_address(meta_keywords.get("content", "") if meta_keywords else "", hotel_name)
    city, district = extract_city_district(address)
    neighborhood = guess_neighborhood(address)
    description = decode_text(meta_description.get("content", "") if meta_description else "")
    short_description = description[:490].strip()

    location_heading = next((h for h in soup.find_all(["h4", "h3"]) if "lokasyon" in decode_text(h.get_text()).lower()), None)
    location_description = decode_text(location_heading.find_next("p").get_text(" ", strip=True) if location_heading and location_heading.find_next("p") else "")

    iframe = soup.select_one(".map-preview-image iframe")
    latitude, longitude = parse_map_coords(iframe.get("src") if iframe else None)
    star_count = parse_star_count(description)

    rules: dict[str, str] = {}
    rule_titles = soup.select(".kosullar-box .rule-title")
    for title in rule_titles:
        key = decode_text(title.get_text(" ", strip=True))
        value = decode_text(title.find_next_sibling(class_="rule-text").get_text(" ", strip=True) if title.find_next_sibling(class_="rule-text") else "")
        if key:
            rules[key] = value

    features = [sanitize_feature_name(li.get_text(" ", strip=True)) for li in soup.select(".hp-amini li")]
    features = [x for x in features if x]

    gallery_urls = []
    for anchor in soup.select(".booking-gallery a[href]"):
        href = anchor.get("href", "").strip()
        if not href:
            continue
        full = urljoin(BASE_URL, href)
        if full not in gallery_urls:
            gallery_urls.append(full)

    owner_email = EMAIL_MAP.get(slug, f"onboarding+{slug}@otelturizm.local")
    owner_name = f"{hotel_name} Yetkilisi"

    rooms: list[RoomData] = []
    for room_box in soup.select(".room-box"):
        onclick = room_box.get("onclick", "")
        match = re.search(r"odaDetayGoster\('(\d+)','(.*?)','(.*?)','(.*?)','(.*?)'\)", onclick)
        if not match:
            continue

        room_id = int(match.group(1))
        room_name = decode_text(match.group(2)) or decode_text(room_box.select_one(".room-title").get_text(" ", strip=True))
        capacity_text = decode_text(match.group(4))
        total_cap, adult_cap, child_cap = parse_capacity(capacity_text)
        nightly_price = parse_decimal_from_price(match.group(5))
        cover_img = room_box.select_one(".room-left img")
        cover_source_url = urljoin(BASE_URL, cover_img.get("src", "").strip()) if cover_img else None
        room_conditions = [decode_text(x.get_text(" ", strip=True)) for x in room_box.select(".room-conditions p")]

        detail_response = session.get(urljoin(BASE_URL, f"/Hotels/OdaBilgileri/{room_id}"), timeout=30)
        detail_response.raise_for_status()
        detail_json = detail_response.json()

        images_response = session.get(urljoin(BASE_URL, f"/Hotels/OdaResimleri/{room_id}"), timeout=30)
        images_response.raise_for_status()
        room_image_names = images_response.json() or []

        base_folder = ""
        if cover_img and cover_img.get("src"):
            parts = cover_img.get("src").strip("/").split("/")
            if len(parts) >= 4:
                base_folder = "/".join(parts[:4])

        room_image_urls: list[str] = []
        if base_folder:
            for name in room_image_names:
                full = urljoin(BASE_URL, "/" + base_folder + "/" + str(name).strip())
                if full not in room_image_urls:
                    room_image_urls.append(full)
        elif cover_source_url:
            room_image_urls.append(cover_source_url)

        room = RoomData(
            source_room_id=room_id,
            slug=slugify(room_name or f"oda-{room_id}"),
            name=room_name,
            category=room_category_from_name(room_name),
            capacity_total=total_cap,
            capacity_adult=adult_cap,
            capacity_child=child_cap,
            nightly_price=nightly_price,
            summary=decode_text(detail_json.get("genel")) or room_name,
            size_sqm=int(re.search(r"\d+", str(detail_json.get("boyut", "") or "")).group()) if re.search(r"\d+", str(detail_json.get("boyut", "") or "")) else None,
            bed_type=decode_text(detail_json.get("yatak")),
            bathroom=decode_text(detail_json.get("banyo")),
            general=decode_text(detail_json.get("genel")),
            card_conditions=room_conditions,
            cover_source_url=cover_source_url,
            image_urls=room_image_urls,
        )
        room.feature_names = [x for x in dict.fromkeys(
            split_features(room.bed_type) +
            split_features(room.bathroom) +
            split_features(room.general) +
            split_features(" ".join(room.card_conditions))
        )]
        rooms.append(room)

    return HotelData(
        slug=slug,
        name=hotel_name,
        short_description=short_description or hotel_name,
        description=description or hotel_name,
        location_description=location_description,
        address=address,
        city=city,
        district=district,
        neighborhood=neighborhood,
        latitude=latitude,
        longitude=longitude,
        star_count=star_count,
        check_in_time=rules.get("Check-in", "En erken saat 14:00 ve sonrası"),
        check_out_time=rules.get("Check-out", "En geç saat 12:00 ve öncesi"),
        children_policy=rules.get("Çocuklar", ""),
        pet_policy=rules.get("Evcil Hayvan", ""),
        features=features,
        rules=rules,
        page_url=url,
        owner_email=owner_email,
        owner_name=owner_name,
        images=gallery_urls,
        rooms=rooms,
    )


def stage_hotel_assets(session: requests.Session, hotel: HotelData) -> None:
    hotel_import_dir = IMPORT_ROOT / hotel.slug
    hotel_image_dir = HOTEL_UPLOAD_ROOT / hotel.slug
    room_root = ROOM_UPLOAD_ROOT / hotel.slug
    ensure_clean_dir(hotel_import_dir)
    hotel_image_dir.mkdir(parents=True, exist_ok=True)
    room_root.mkdir(parents=True, exist_ok=True)

    imported_hotel_urls: list[str] = []
    for index, source_url in enumerate(hotel.images or [], start=1):
        file_name = f"hotel-{index:02d}.webp"
        target = hotel_image_dir / file_name
        try:
            download_and_convert_to_webp(session, source_url, target)
        except (requests.RequestException, UnidentifiedImageError, OSError):
            continue
        imported_hotel_urls.append(f"/uploads/216suites/hotels/{hotel.slug}/{file_name}")
    hotel.imported_image_urls = imported_hotel_urls
    hotel.cover_import_url = imported_hotel_urls[0] if imported_hotel_urls else None

    for room in hotel.rooms or []:
        room_dir = room_root / room.slug
        room_dir.mkdir(parents=True, exist_ok=True)
        imported_room_urls: list[str] = []
        for index, source_url in enumerate(room.image_urls or [], start=1):
            target = room_dir / f"room-{index:02d}.webp"
            try:
                download_and_convert_to_webp(session, source_url, target)
            except (requests.RequestException, UnidentifiedImageError, OSError):
                continue
            imported_room_urls.append(f"/uploads/216suites/rooms/{hotel.slug}/{room.slug}/room-{index:02d}.webp")
        room.imported_gallery_urls = imported_room_urls
        room.imported_cover_url = imported_room_urls[0] if imported_room_urls else None

        room_json = {
            "source_room_id": room.source_room_id,
            "slug": room.slug,
            "name": room.name,
            "category": room.category,
            "capacity_total": room.capacity_total,
            "capacity_adult": room.capacity_adult,
            "capacity_child": room.capacity_child,
            "nightly_price": str(room.nightly_price),
            "summary": room.summary,
            "size_sqm": room.size_sqm,
            "bed_type": room.bed_type,
            "bathroom": room.bathroom,
            "general": room.general,
            "conditions": room.card_conditions,
            "features": room.feature_names,
            "cover": room.imported_cover_url,
            "gallery": room.imported_gallery_urls,
        }
        (hotel_import_dir / f"room-{room.slug}.json").write_text(json.dumps(room_json, ensure_ascii=False, indent=2), encoding="utf-8")

    hotel_json = {
        "slug": hotel.slug,
        "name": hotel.name,
        "owner_email": hotel.owner_email,
        "owner_name": hotel.owner_name,
        "short_description": hotel.short_description,
        "description": hotel.description,
        "location_description": hotel.location_description,
        "address": hotel.address,
        "city": hotel.city,
        "district": hotel.district,
        "neighborhood": hotel.neighborhood,
        "latitude": str(hotel.latitude) if hotel.latitude is not None else None,
        "longitude": str(hotel.longitude) if hotel.longitude is not None else None,
        "star_count": hotel.star_count,
        "check_in_time": hotel.check_in_time,
        "check_out_time": hotel.check_out_time,
        "children_policy": hotel.children_policy,
        "pet_policy": hotel.pet_policy,
        "rules": hotel.rules,
        "features": hotel.features,
        "page_url": hotel.page_url,
        "cover": hotel.cover_import_url,
        "gallery": hotel.imported_image_urls,
        "rooms": [room.slug for room in hotel.rooms or []],
    }
    (hotel_import_dir / "hotel.json").write_text(json.dumps(hotel_json, ensure_ascii=False, indent=2), encoding="utf-8")


def fetch_lookup_dict(cursor: pyodbc.Cursor, table: str, key_col: str, value_col: str) -> dict[str, Any]:
    cursor.execute(f"SELECT {key_col}, {value_col} FROM {table}")
    result: dict[str, Any] = {}
    for row in cursor.fetchall():
        result[decode_text(str(row[0])).lower()] = row[1]
    return result


def ensure_hotel_feature(cursor: pyodbc.Cursor, feature_name: str) -> int:
    normalized = decode_text(feature_name)
    lookup = fetch_lookup_dict(cursor, "otel_ozellikleri", "ozellik_adi", "id")
    if normalized.lower() in lookup:
        return int(lookup[normalized.lower()])
    cursor.execute(
        """
        INSERT INTO otel_ozellikleri (kategori_id, ozellik_adi, ozellik_ikon, ucretli_mi, one_cikan_ozellik, siralama, aktif_mi)
        OUTPUT INSERTED.id
        VALUES (1, ?, N'fa-star', 0, 0, 999, 1);
        """,
        normalized,
    )
    return int(cursor.fetchone()[0])


def ensure_room_feature(cursor: pyodbc.Cursor, feature_name: str) -> int:
    normalized = truncate(feature_name, 100)
    if not normalized:
        return 0
    lookup = fetch_lookup_dict(cursor, "oda_ozellikleri", "ozellik_adi", "id")
    if normalized.lower() in lookup:
        return int(lookup[normalized.lower()])
    cursor.execute(
        """
        INSERT INTO oda_ozellikleri (kategori, ozellik_adi, ozellik_ikon, siralama, aktif_mi)
        OUTPUT INSERTED.id
        VALUES (N'Genel', ?, N'fa-circle-check', 999, 1);
        """,
        normalized,
    )
    return int(cursor.fetchone()[0])


def parse_time_text(text: str, fallback: str) -> str:
    match = re.search(r"(\d{1,2}:\d{2})", decode_text(text))
    return match.group(1) if match else fallback


def cleanup_existing_import(cursor: pyodbc.Cursor) -> None:
    delete_statements = [
        "DELETE FROM rezervasyon_odeme_kalemleri",
        "DELETE FROM odeme_islemleri",
        "DELETE FROM basarisiz_odeme_denemeleri",
        "DELETE FROM faturalar",
        "DELETE FROM komisyon_muhasebe_kayitlari",
        "DELETE FROM sepet_blokajlari",
        "DELETE FROM rezervasyon_taslaklari",
        "DELETE FROM rezervasyonlar",
        "DELETE FROM yorumlar",
        "DELETE FROM kampanya_oteller",
        "DELETE FROM user_favori_oteller",
        "DELETE FROM oda_gorselleri",
        "DELETE FROM oda_fiyat_musaitlik",
        "DELETE FROM oda_tipi_ozellikleri",
        "DELETE FROM oda_tipleri",
        "DELETE FROM otel_gorselleri",
        "DELETE FROM otel_ozellik_iliskileri",
        "DELETE FROM otel_kosullari",
        "DELETE FROM otel_istatistikleri",
        "DELETE FROM otel_rakip_analizi",
        "DELETE FROM otel_kullanici_sahiplikleri",
        "DELETE FROM oteller",
        "DELETE FROM partner_detaylari WHERE aciklama LIKE N'%[216SUITES-IMPORT]%'",
        "DELETE FROM users WHERE kayit_kaynagi = N'216suites-import'",
    ]
    for stmt in delete_statements:
        try:
            cursor.execute(stmt)
        except pyodbc.Error:
            continue


def insert_partner_user(cursor: pyodbc.Cursor, hotel: HotelData) -> tuple[int, int]:
    now = datetime.utcnow()
    cursor.execute(
        """
        INSERT INTO users (
            ad_soyad, eposta, telefon, sifre, rol, sahiplik_partner_id, hesap_durumu,
            kayit_kaynagi, ulke, dil_tercihi, para_birimi, olusturulma_tarihi, guncellenme_tarihi,
            email_dogrulama_tarihi, iki_asamali_dogrulama_aktif_mi
        )
        OUTPUT INSERTED.id
        VALUES (?, ?, ?, ?, N'partner_owner', NULL, 1, N'216suites-import', N'Türkiye', N'tr-TR', N'TRY', ?, ?, NULL, 0);
        """,
        hotel.owner_name,
        hotel.owner_email,
        DEFAULT_PHONE,
        sha256_hex(DEFAULT_PASSWORD),
        now,
        now,
    )
    user_id = int(cursor.fetchone()[0])

    cursor.execute(
        """
        INSERT INTO partner_detaylari (
            kullanici_id, firma_unvani, firma_turu, vergi_dairesi, vergi_numarasi, fatura_adresi, fatura_il, fatura_ilce,
            yetkili_ad_soyad, yetkili_tc_no, yetkili_telefon, yetkili_eposta, banka_adi, iban, hesap_sahibi_adi,
            onay_durumu, web_sitesi, aciklama, olusturulma_tarihi, guncellenme_tarihi
        )
        OUTPUT INSERTED.id
        VALUES (?, ?, N'Otel İşletmesi', N'Belirlenecek', N'0000000000', ?, ?, ?, ?, N'00000000000', ?, ?, N'Belirlenecek', N'TR000000000000000000000000', ?, N'Beklemede', ?, ?, ?, ?);
        """,
        user_id,
        hotel.name,
        hotel.address,
        hotel.city,
        hotel.district,
        hotel.owner_name,
        DEFAULT_PHONE,
        hotel.owner_email,
        hotel.name,
        hotel.page_url,
        f"[216SUITES-IMPORT] {hotel.name} kaydı 216suites.com üzerinden içe aktarıldı.",
        now,
        now,
    )
    partner_id = int(cursor.fetchone()[0])
    cursor.execute("UPDATE users SET sahiplik_partner_id = ? WHERE id = ?", partner_id, user_id)
    return user_id, partner_id


def insert_hotel(cursor: pyodbc.Cursor, hotel: HotelData, user_id: int, partner_id: int) -> int:
    now = datetime.utcnow()
    total_room_count = max(1, len(hotel.rooms or []))
    cursor.execute(
        """
        INSERT INTO oteller (
            otel_kodu, partner_id, user_id, otel_adi, otel_turu, yildiz_sayisi, ulke, sehir, ilce, mahalle,
            tam_adres, enlem, boylam, telefon_1, eposta, web_sitesi, check_in_saati, check_out_saati,
            toplam_oda_sayisi, toplam_yatak_kapasitesi, kisa_aciklama, uzun_aciklama, konum_aciklamasi,
            komisyon_turu, varsayilan_komisyon_orani, komisyon_hesaplama_tipi, odeme_vadesi, odeme_yontemi,
            fatura_kesim_turu, minimum_konaklama_gecesi, maksimum_konaklama_gecesi, ortalama_puan, toplam_yorum_sayisi,
            kapak_fotografi, galeri, yayin_durumu, onay_durumu, onay_tarihi, rezervasyon_telefonu,
            satis_kontak_adi, satis_kontak_telefonu, satis_kontak_eposta, olusturulma_tarihi, guncellenme_tarihi
        )
        OUTPUT INSERTED.id
        VALUES (?, ?, ?, ?, N'Otel', ?, N'Türkiye', ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?,
                N'sabit_oran', 15, N'toplam_tutar_uzerinden', N'Çıkış Günü', N'Havale/EFT', N'Otel Keser',
                1, 30, 0, 0, ?, ?, N'Yayında', N'Onaylandı', ?, ?, ?, ?, ?, ?, ?);
        """,
        hotel.slug.upper().replace("-", "_")[:20],
        partner_id,
        user_id,
        hotel.name,
        hotel.star_count,
        hotel.city,
        hotel.district,
        hotel.neighborhood,
        hotel.address,
        hotel.latitude,
        hotel.longitude,
        DEFAULT_PHONE,
        hotel.owner_email,
        hotel.page_url,
        parse_time_text(hotel.check_in_time, "14:00"),
        parse_time_text(hotel.check_out_time, "12:00"),
        total_room_count,
        sum(x.capacity_total for x in hotel.rooms or []) or total_room_count * 2,
        hotel.short_description[:500],
        hotel.description,
        hotel.location_description,
        hotel.cover_import_url,
        json.dumps(hotel.imported_image_urls or [], ensure_ascii=False),
        now,
        DEFAULT_PHONE,
        hotel.owner_name,
        DEFAULT_PHONE,
        hotel.owner_email,
        now,
        now,
    )
    return int(cursor.fetchone()[0])


def insert_hotel_rules(cursor: pyodbc.Cursor, hotel_id: int, hotel: HotelData) -> None:
    now = datetime.utcnow()
    children_range = summarize_children_policy(hotel.children_policy)
    rules_summary = " | ".join(
        [part for part in [decode_text(hotel.rules.get("Check-in", "")), decode_text(hotel.rules.get("Check-out", ""))] if part]
    )
    if not rules_summary:
        rules_summary = "Check-in 14:00 | Check-out 12:00"
    cursor.execute(
        """
        INSERT INTO otel_kosullari (
            otel_id, sigara_politikasi, evcil_hayvan_politikasi, parti_etkinlik_izin,
            minimum_yas_siniri, cocuk_kabul_yas_araligi, bebek_karyolasi_var_mi, ekstra_yatak_var_mi,
            on_odeme_gerekli_mi, kredi_karti_ile_odeme_kabul, nakit_odeme_kabul, iptal_politikasi_ozet,
            detayli_iptal_kosullari, ucretsiz_iptal_suresi, disaridan_yiyecek_icecek_serbest_mi,
            ziyaretci_kabul_edilir_mi, ozel_kosullar, guncellenme_tarihi
        )
        VALUES (?, N'Sigara içilmeyen alanlar mevcuttur.', ?, 0, 18, ?, 1, 0, 0, 1, 1, N'Ücretsiz iptal',
                ?, 1, 1, 1, ?, ?)
        """,
        hotel_id,
        truncate(hotel.pet_policy or "Evcil hayvan kabul edilmemektedir.", 255),
        children_range,
        truncate(rules_summary, 500),
        hotel.children_policy + " " + hotel.location_description if hotel.children_policy else (hotel.location_description or hotel.description),
        now,
    )


def insert_hotel_features(cursor: pyodbc.Cursor, hotel_id: int, feature_names: list[str]) -> None:
    seen: set[int] = set()
    for feature_name in feature_names:
        feature_id = ensure_hotel_feature(cursor, feature_name)
        if feature_id in seen:
            continue
        seen.add(feature_id)
        cursor.execute(
            "INSERT INTO otel_ozellik_iliskileri (otel_id, ozellik_id, ek_ucret, aciklama) VALUES (?, ?, 0, NULL)",
            hotel_id,
            feature_id,
        )


def insert_hotel_images(cursor: pyodbc.Cursor, hotel_id: int, hotel: HotelData) -> None:
    now = datetime.utcnow()
    for index, image_url in enumerate(hotel.imported_image_urls or [], start=1):
        file_path = ROOT / image_url.lstrip("/").replace("/", "\\")
        width = height = 0
        size_kb = max(1, round(file_path.stat().st_size / 1024)) if file_path.exists() else 1
        if file_path.exists():
            with Image.open(file_path) as img:
                width, height = img.size
        cursor.execute(
            """
            INSERT INTO otel_gorselleri (
                otel_id, gorsel_url, thumbnail_url, gorsel_turu, baslik, aciklama, kapak_fotografi_mi,
                one_cikan, siralama, boyut_kb, genislik, yukseklik, onay_durumu, onay_tarihi, olusturulma_tarihi
            )
            VALUES (?, ?, NULL, N'genel_alan', ?, ?, ?, 0, ?, ?, ?, ?, N'Onaylandı', ?, ?)
            """,
            hotel_id,
            image_url,
            f"{hotel.name} görsel {index}",
            f"{hotel.name} galeri görseli {index}",
            1 if index == 1 else 0,
            index,
            size_kb,
            width,
            height,
            now,
            now,
        )


def insert_room(cursor: pyodbc.Cursor, hotel_id: int, hotel_slug: str, room: RoomData, order: int) -> int:
    now = datetime.utcnow()
    cursor.execute(
        """
        INSERT INTO oda_tipleri (
            otel_id, oda_tip_kodu, oda_adi, oda_kategorisi, maksimum_kisi_sayisi, maksimum_yetiskin_sayisi,
            maksimum_cocuk_sayisi, yatak_tipi, yatak_sayisi, ek_yatak_eklenebilir_mi, oda_metrekare,
            balkon_var_mi, manzara_tipi, ozel_banyo_var_mi, banyo_tipi, standart_gecelik_fiyat,
            toplam_oda_sayisi, kapak_fotografi, galeri, ozellikler, aktif_mi, siralama, olusturulma_tarihi, guncellenme_tarihi
        )
        OUTPUT INSERTED.id
        VALUES (?, ?, ?, ?, ?, ?, ?, ?, 1, 0, ?, 0, N'Yok', 1, ?, ?, 5, ?, ?, ?, 1, ?, ?, ?);
        """,
        hotel_id,
        f"{hotel_slug[:12].upper()}_{room.source_room_id}",
        room.name,
        room.category,
        room.capacity_total,
        room.capacity_adult,
        room.capacity_child,
        room.bed_type,
        room.size_sqm,
        room.bathroom[:50] if room.bathroom else "Duş",
        room.nightly_price,
        room.imported_cover_url,
        json.dumps(room.imported_gallery_urls or [], ensure_ascii=False),
        json.dumps(room.feature_names or [], ensure_ascii=False),
        order,
        now,
        now,
    )
    return int(cursor.fetchone()[0])


def insert_room_features(cursor: pyodbc.Cursor, room_type_id: int, feature_names: list[str]) -> None:
    seen: set[int] = set()
    for feature_name in feature_names:
        cleaned = decode_text(feature_name)
        if not cleaned:
            continue
        if len(cleaned) > 80 and " " in cleaned:
            continue
        if any(token in cleaned.lower() for token in ["konaklama sunar", "uygun olarak tasar", "misafire kadar", "kapasiteside"]):
            continue
        feature_id = ensure_room_feature(cursor, feature_name)
        if not feature_id:
            continue
        if feature_id in seen:
            continue
        seen.add(feature_id)
        cursor.execute(
            "INSERT INTO oda_tipi_ozellikleri (oda_tip_id, ozellik_id, miktar) VALUES (?, ?, 1)",
            room_type_id,
            feature_id,
        )


def insert_room_images(cursor: pyodbc.Cursor, room_type_id: int, room: RoomData) -> None:
    now = datetime.utcnow()
    for index, image_url in enumerate(room.imported_gallery_urls or [], start=1):
        file_path = ROOT / image_url.lstrip("/").replace("/", "\\")
        size_kb = max(1, round(file_path.stat().st_size / 1024)) if file_path.exists() else 1
        cursor.execute(
            """
            INSERT INTO oda_gorselleri (
                oda_tip_id, gorsel_url, thumbnail_url, baslik, aciklama, kapak_fotografi_mi,
                siralama, boyut_kb, onay_durumu, onay_tarihi, olusturulma_tarihi
            )
            VALUES (?, ?, NULL, ?, ?, ?, ?, ?, N'Onaylandı', ?, ?)
            """,
            room_type_id,
            image_url,
            f"{room.name} görsel {index}",
            f"{room.name} galeri görseli {index}",
            1 if index == 1 else 0,
            index,
            size_kb,
            now,
            now,
        )


def insert_room_prices(cursor: pyodbc.Cursor, hotel_id: int, room_type_id: int, room: RoomData) -> None:
    now = datetime.utcnow()
    for offset in range(0, 365):
        current_day = TODAY + timedelta(days=offset)
        cursor.execute(
            """
            INSERT INTO oda_fiyat_musaitlik (
                oda_tip_id, otel_id, tarih, gecelik_fiyat, indirimli_fiyat, kampanya_id, toplam_oda_sayisi,
                satilan_oda_sayisi, bloke_oda_sayisi, minimum_geceleme, maksimum_geceleme, kapali_satis,
                kampanya_etiketi, fiyat_notu, guncelleyen_kullanici_id, sadece_gunubirlik, iptal_politikasi_override, guncellenme_tarihi
            )
            VALUES (?, ?, ?, ?, NULL, NULL, 5, 0, 0, 1, 30, 0, NULL, N'Tüm vergiler dahil', NULL, 0, NULL, ?)
            """,
            room_type_id,
            hotel_id,
            current_day,
            room.nightly_price,
            now,
        )


def scrape_all() -> list[HotelData]:
    session = requests.Session()
    session.headers.update({"User-Agent": "Mozilla/5.0 (compatible; OtelTurizmImportBot/1.0)"})
    links = scrape_homepage(session)
    hotels: list[HotelData] = []

    ensure_clean_dir(IMPORT_ROOT)
    HOTEL_UPLOAD_ROOT.mkdir(parents=True, exist_ok=True)
    ROOM_UPLOAD_ROOT.mkdir(parents=True, exist_ok=True)

    for url in links:
        hotel = scrape_hotel(session, url)
        stage_hotel_assets(session, hotel)
        hotels.append(hotel)

    index_payload = [
        {
            "slug": hotel.slug,
            "name": hotel.name,
            "owner_email": hotel.owner_email,
            "city": hotel.city,
            "district": hotel.district,
            "rooms": len(hotel.rooms or []),
            "gallery": len(hotel.imported_image_urls or []),
        }
        for hotel in hotels
    ]
    (IMPORT_ROOT / "index.json").write_text(json.dumps(index_payload, ensure_ascii=False, indent=2), encoding="utf-8")
    return hotels


def load_staged_hotels() -> list[HotelData]:
    hotels: list[HotelData] = []
    index_path = IMPORT_ROOT / "index.json"
    if not index_path.exists():
        return hotels
    index_items = json.loads(index_path.read_text(encoding="utf-8"))
    for item in index_items:
        hotel_dir = IMPORT_ROOT / item["slug"]
        hotel_json_path = hotel_dir / "hotel.json"
        if not hotel_json_path.exists():
            continue
        hotel_json = json.loads(hotel_json_path.read_text(encoding="utf-8"))
        rooms: list[RoomData] = []
        for room_slug in hotel_json.get("rooms", []):
            room_json_path = hotel_dir / f"room-{room_slug}.json"
            if not room_json_path.exists():
                continue
            room_json = json.loads(room_json_path.read_text(encoding="utf-8"))
            rooms.append(
                RoomData(
                    source_room_id=int(room_json["source_room_id"]),
                    slug=room_json["slug"],
                    name=room_json["name"],
                    category=room_json["category"],
                    capacity_total=int(room_json["capacity_total"]),
                    capacity_adult=int(room_json["capacity_adult"]),
                    capacity_child=int(room_json["capacity_child"]),
                    nightly_price=Decimal(str(room_json["nightly_price"])),
                    summary=room_json.get("summary", ""),
                    size_sqm=room_json.get("size_sqm"),
                    bed_type=room_json.get("bed_type"),
                    bathroom=room_json.get("bathroom"),
                    general=room_json.get("general"),
                    card_conditions=room_json.get("conditions") or [],
                    cover_source_url=None,
                    image_urls=[],
                    imported_cover_url=room_json.get("cover"),
                    imported_gallery_urls=room_json.get("gallery") or [],
                    feature_names=room_json.get("features") or [],
                )
            )

        hotels.append(
            HotelData(
                slug=hotel_json["slug"],
                name=hotel_json["name"],
                short_description=hotel_json.get("short_description", ""),
                description=hotel_json.get("description", ""),
                location_description=hotel_json.get("location_description", ""),
                address=hotel_json.get("address", ""),
                city=hotel_json.get("city", DEFAULT_CITY),
                district=hotel_json.get("district", "Merkez"),
                neighborhood=hotel_json.get("neighborhood"),
                latitude=Decimal(hotel_json["latitude"]) if hotel_json.get("latitude") else None,
                longitude=Decimal(hotel_json["longitude"]) if hotel_json.get("longitude") else None,
                star_count=hotel_json.get("star_count"),
                check_in_time=hotel_json.get("check_in_time", "14:00"),
                check_out_time=hotel_json.get("check_out_time", "12:00"),
                children_policy=hotel_json.get("children_policy", ""),
                pet_policy=hotel_json.get("pet_policy", ""),
                features=hotel_json.get("features") or [],
                rules=hotel_json.get("rules") or {},
                page_url=hotel_json.get("page_url", ""),
                owner_email=hotel_json.get("owner_email", ""),
                owner_name=hotel_json.get("owner_name", ""),
                images=[],
                cover_import_url=hotel_json.get("cover"),
                imported_image_urls=hotel_json.get("gallery") or [],
                rooms=rooms,
            )
        )
    return hotels


def import_to_local_db(hotels: list[HotelData]) -> None:
    conn = pyodbc.connect(LOCAL_CONN_STR)
    conn.autocommit = False
    cursor = conn.cursor()
    try:
        cleanup_existing_import(cursor)
        for hotel in hotels:
            user_id, partner_id = insert_partner_user(cursor, hotel)
            hotel_id = insert_hotel(cursor, hotel, user_id, partner_id)
            insert_hotel_rules(cursor, hotel_id, hotel)
            insert_hotel_features(cursor, hotel_id, hotel.features)
            insert_hotel_images(cursor, hotel_id, hotel)

            for order, room in enumerate(hotel.rooms or [], start=1):
                room_type_id = insert_room(cursor, hotel_id, hotel.slug, room, order)
                insert_room_features(cursor, room_type_id, room.feature_names or [])
                insert_room_images(cursor, room_type_id, room)
                insert_room_prices(cursor, hotel_id, room_type_id, room)

        conn.commit()
    except Exception:
        conn.rollback()
        raise
    finally:
        cursor.close()
        conn.close()


def main() -> None:
    import_only = "--import-only" in sys.argv
    hotels = load_staged_hotels() if import_only else scrape_all()
    if not hotels:
        hotels = scrape_all()
    import_to_local_db(hotels)
    summary = {
        "hotel_count": len(hotels),
        "room_count": sum(len(x.rooms or []) for x in hotels),
        "emails_missing": [x.slug for x in hotels if x.owner_email.endswith("@otelturizm.local")],
        "import_root": str(IMPORT_ROOT),
    }
    print(json.dumps(summary, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
