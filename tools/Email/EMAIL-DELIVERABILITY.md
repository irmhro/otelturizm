# Email Deliverability Notları (p90)

Bu doküman, transactional e-postaların (2FA, doğrulama, rezervasyon bildirimleri) spam’e düşmemesi ve güvenli şekilde teslim edilmesi için temel DNS/kimlik doğrulama adımlarını özetler.

## 1) SPF

- Alan adının DNS’ine SPF TXT kaydı eklenir.
- Örnek (gönderim sağlayıcına göre değişir):
  - `v=spf1 include:spf.protection.outlook.com -all`
  - veya `v=spf1 include:sendgrid.net -all`

Kontrol:
- SPF “pass” olmalı, “softfail/neutral” olmamalı.

## 2) DKIM

- Gönderim sağlayıcının verdiği DKIM public key DNS’e eklenir (genellikle `selector._domainkey`).
- DKIM “pass” olmalı.

## 3) DMARC

- Başlangıç için raporlama modunda önerilir:
  - `v=DMARC1; p=none; rua=mailto:dmarc@otelturizm.com; ruf=mailto:dmarc@otelturizm.com; fo=1; adkim=s; aspf=s`
- Stabil olduktan sonra kademeli:
  - `p=quarantine` → `p=reject`

## 4) Reverse DNS / HELO

- Kendi SMTP’niz varsa: PTR (reverse) kaydı gönderen IP’den domain’e dönmeli.
- HELO/EHLO ismi de domain ile tutarlı olmalı.

## 5) Envelope / From tutarlılığı

- `From:` domain ve envelope sender mümkünse aynı domain’den olmalı (alignment).
- `Reply-To` farklıysa bile DMARC alignment bozulmamalı.

## 6) İçerik & Link hijyeni

- Transactional e-postalarda linkler **HTTPS** olmalı.
- p88 kapsamında linklere UTM eklenir:
  - `utm_source=transactional`
  - `utm_medium=email`
  - `utm_campaign=<template_code>`

