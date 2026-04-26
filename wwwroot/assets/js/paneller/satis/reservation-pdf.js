(function () {
    const reservationId = Number(window.__salesReservationPdfId || 0);
    const statusEl = document.querySelector("[data-pdf-status]");
    const btnDownload = document.querySelector("[data-pdf-download]");
    const btnOpen = document.querySelector("[data-pdf-open]");

    const setStatus = function (text) {
        if (statusEl) statusEl.textContent = text;
    };

    if (!reservationId || !window.pdfMake) {
        setStatus("PDF üretimi başlatılamadı (eksik rezervasyon id veya pdf kütüphanesi).");
        return;
    }

    const formatMoney = function (value) {
        try {
            return new Intl.NumberFormat("tr-TR", { style: "currency", currency: "TRY", maximumFractionDigits: 2 }).format(Number(value || 0));
        } catch {
            return String(value || "0");
        }
    };

    const formatDate = function (isoOrValue) {
        if (!isoOrValue) return "-";
        // backend DateOnly JSON -> "2026-04-25"
        return String(isoOrValue).slice(0, 10).split("-").reverse().join(".");
    };

    const buildDoc = function (data) {
        const lines = [
            { label: "Rezervasyon No", value: data.reservationNo },
            { label: "Otel", value: data.hotelName },
            { label: "Otel Telefon", value: data.hotelPhone || "-" },
            { label: "Oda Tipi", value: data.roomName },
            { label: "Giriş - Çıkış", value: formatDate(data.checkInDate) + " - " + formatDate(data.checkOutDate) + " (" + (data.nightCount || 0) + " gece)" },
            { label: "Misafir", value: data.guestFullName },
            { label: "Misafir Telefon", value: data.guestPhone || "-" },
            { label: "Misafir E-posta", value: data.guestEmail ? data.guestEmail : "Yok (PDF ile paylaşıldı)" },
            { label: "Kişi / Oda", value: (data.adultCount || 0) + " yetişkin, " + (data.childCount || 0) + " çocuk · " + (data.roomCount || 0) + " oda" },
            { label: "Oluşturulma", value: data.createdAtText || "-" }
        ];

        const doc = {
            info: {
                title: "Rezervasyon Onayı - " + (data.reservationNo || "SAT")
            },
            pageMargins: [32, 32, 32, 32],
            content: [
                {
                    text: "OTELTURIZM · Rezervasyon Onayı",
                    style: "brand"
                },
                {
                    text: data.hotelName || "",
                    style: "title"
                },
                {
                    canvas: [{ type: "line", x1: 0, y1: 0, x2: 515, y2: 0, lineWidth: 1, lineColor: "#E5E7EB" }],
                    margin: [0, 10, 0, 14]
                },
                {
                    table: {
                        widths: [140, "*"],
                        body: lines.map(function (row) {
                            return [
                                { text: row.label, style: "label" },
                                { text: row.value || "-", style: "value" }
                            ];
                        })
                    },
                    layout: "noBorders"
                },
                {
                    text: "Fiyat Özeti",
                    style: "section",
                    margin: [0, 18, 0, 10]
                },
                {
                    table: {
                        widths: ["*", "auto"],
                        body: [
                            ["Gecelik fiyat", formatMoney(data.nightlyPrice)],
                            ["Oda toplamı", formatMoney(data.roomTotal)],
                            ["Vergiler", formatMoney(data.taxAmount)],
                            [{ text: "Genel Toplam", bold: true }, { text: formatMoney(data.totalAmount), bold: true }]
                        ].map(function (row) {
                            return [{ text: row[0], style: "value" }, { text: row[1], style: "value", alignment: "right" }];
                        })
                    },
                    layout: {
                        hLineWidth: function (i) { return i === 0 || i === 4 ? 1 : 0.5; },
                        vLineWidth: function () { return 0; },
                        hLineColor: function () { return "#E5E7EB"; },
                        paddingLeft: function () { return 0; },
                        paddingRight: function () { return 0; },
                        paddingTop: function () { return 6; },
                        paddingBottom: function () { return 6; }
                    }
                },
                {
                    text: "Not: Bu çıktı satış panelinden üretilmiştir. Partner/otel tarafına rezervasyon kaydı iletilmiştir.",
                    style: "footnote",
                    margin: [0, 18, 0, 0]
                }
            ],
            styles: {
                brand: { fontSize: 11, color: "#0F172A", bold: true, letterSpacing: 0.5 },
                title: { fontSize: 18, bold: true, color: "#0F172A", margin: [0, 6, 0, 0] },
                section: { fontSize: 13, bold: true, color: "#0F172A" },
                label: { fontSize: 10, color: "#64748B", bold: true, margin: [0, 2, 0, 2] },
                value: { fontSize: 11, color: "#0F172A", margin: [0, 2, 0, 2] },
                footnote: { fontSize: 9, color: "#64748B" }
            },
            defaultStyle: {
                fontSize: 11
            }
        };
        return doc;
    };

    const loadData = function () {
        setStatus("Rezervasyon bilgileri alınıyor...");
        return fetch("/panel/satis/api/rezervasyon-pdf/" + reservationId, { headers: { "Accept": "application/json" } })
            .then(function (r) { return r.ok ? r.json() : null; })
            .then(function (payload) {
                if (!payload || payload.success !== true || !payload.data) {
                    throw new Error((payload && payload.message) || "Rezervasyon verisi alınamadı.");
                }
                return payload.data;
            });
    };

    const createPdf = function () {
        return loadData().then(function (data) {
            setStatus("PDF hazırlanıyor...");
            const doc = buildDoc(data);
            return window.pdfMake.createPdf(doc);
        });
    };

    btnDownload && btnDownload.addEventListener("click", function () {
        createPdf()
            .then(function (pdf) {
                setStatus("PDF indiriliyor...");
                pdf.download("rezervasyon-" + reservationId + ".pdf");
                setStatus("Hazır.");
            })
            .catch(function (err) {
                setStatus("PDF üretilemedi: " + (err && err.message ? err.message : String(err)));
            });
    });

    btnOpen && btnOpen.addEventListener("click", function () {
        createPdf()
            .then(function (pdf) {
                setStatus("PDF açılıyor...");
                pdf.open();
                setStatus("Hazır.");
            })
            .catch(function (err) {
                setStatus("PDF üretilemedi: " + (err && err.message ? err.message : String(err)));
            });
    });

    // otomatik hazırlık
    setStatus("Hazır. PDF indirmek için butona basın.");
})();

