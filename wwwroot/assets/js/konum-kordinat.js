(() => {
  const GEO_COOKIE = 'Otelturizm.LastGeo';
  const GEO_SESSION_KEY = 'otelturizm.geo.lastSentAt';
  const SEND_INTERVAL_MS = 12 * 60 * 60 * 1000; // 12 saat

  const canUseGeo = () => typeof navigator !== 'undefined' && !!navigator.geolocation;

  const safeJsonParse = (value) => {
    try { return JSON.parse(value); } catch { return null; }
  };

  const readCookie = (name) => {
    try {
      const parts = document.cookie.split(';').map((c) => c.trim());
      const hit = parts.find((p) => p.startsWith(name + '='));
      if (!hit) return null;
      return decodeURIComponent(hit.substring(name.length + 1));
    } catch {
      return null;
    }
  };

  const writeCookie = (name, value, days) => {
    try {
      const maxAge = Math.max(1, Math.floor(days * 24 * 60 * 60));
      document.cookie = `${name}=${encodeURIComponent(value)}; Path=/; Max-Age=${maxAge}; SameSite=Lax`;
    } catch {
      // sessiz
    }
  };

  const shouldSendNow = () => {
    try {
      const last = Number(localStorage.getItem(GEO_SESSION_KEY) || '0');
      return !last || (Date.now() - last) >= SEND_INTERVAL_MS;
    } catch {
      return true;
    }
  };

  const markSent = () => {
    try { localStorage.setItem(GEO_SESSION_KEY, String(Date.now())); } catch {}
  };

  const storeLastGeo = (payload) => {
    const compact = {
      lat: payload.lat,
      lon: payload.lon,
      acc: payload.acc,
      ts: Date.now()
    };
    writeCookie(GEO_COOKIE, JSON.stringify(compact), 7);
    try { sessionStorage.setItem(GEO_COOKIE, JSON.stringify(compact)); } catch {}
  };

  const sendToServer = async (payload) => {
    try {
      await fetch('/oteller/konum-kaydet', {
        method: 'POST',
        credentials: 'same-origin',
        headers: {
          'Content-Type': 'application/json',
          'X-Requested-With': 'XMLHttpRequest'
        },
        body: JSON.stringify({
          latitude: payload.lat,
          longitude: payload.lon,
          radiusKm: null,
          visibleHotelCount: null,
          listedHotelIds: null,
          searchTerm: null,
          searchRegion: null,
          source: payload.source || 'footer-auto'
        })
      });
      markSent();
    } catch {
      // sessiz
    }
  };

  const requestLocation = () => {
    if (!canUseGeo()) return;
    if (!shouldSendNow()) return;

    // Kullanıcı daha önce cookie'de konum verdiyse ve yeni isteğe gerek yoksa yine loglamayalım.
    const existing = safeJsonParse(readCookie(GEO_COOKIE) || '');
    if (existing?.ts && (Date.now() - Number(existing.ts)) < SEND_INTERVAL_MS) {
      return;
    }

    navigator.geolocation.getCurrentPosition(
      (pos) => {
        const coords = pos && pos.coords ? pos.coords : null;
        if (!coords) return;
        const payload = {
          lat: Number(coords.latitude),
          lon: Number(coords.longitude),
          acc: coords.accuracy != null ? Number(coords.accuracy) : null,
          source: 'footer-auto'
        };
        if (!Number.isFinite(payload.lat) || !Number.isFinite(payload.lon)) return;
        storeLastGeo(payload);
        void sendToServer(payload);
      },
      () => {
        // izin yok / hata: sessiz
      },
      {
        enableHighAccuracy: false,
        timeout: 6000,
        maximumAge: 30 * 60 * 1000
      }
    );
  };

  // sayfa yüklenince küçük gecikmeyle iste (banner/CLS etkisini azaltır)
  if (document.readyState === 'complete' || document.readyState === 'interactive') {
    setTimeout(requestLocation, 1200);
  } else {
    document.addEventListener('DOMContentLoaded', () => setTimeout(requestLocation, 1200));
  }
})();

