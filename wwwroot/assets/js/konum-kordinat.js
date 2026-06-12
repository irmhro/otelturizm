(() => {
  const GEO_COOKIE = 'Otelturizm.LastGeo';
  const GEO_SESSION_KEY = 'otelturizm.geo.lastSentAt';
  const GEO_PROMPT_KEY = 'otelturizm.geo.prompt.dismissedAt';
  const SEND_INTERVAL_MS = 12 * 60 * 60 * 1000; // 12 saat
  const PROMPT_SNOOZE_MS = 7 * 24 * 60 * 60 * 1000;

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
          source: payload.source || 'geo-consent-prompt'
        })
      });
      markSent();
    } catch {
      // sessiz
    }
  };

  const hasRecentGeo = () => {
    const existing = safeJsonParse(readCookie(GEO_COOKIE) || '');
    return !!(existing?.ts && (Date.now() - Number(existing.ts)) < SEND_INTERVAL_MS);
  };

  const wasPromptDismissedRecently = () => {
    try {
      const last = Number(localStorage.getItem(GEO_PROMPT_KEY) || '0');
      return !!last && (Date.now() - last) < PROMPT_SNOOZE_MS;
    } catch {
      return false;
    }
  };

  const dismissPrompt = (toast) => {
    try { localStorage.setItem(GEO_PROMPT_KEY, String(Date.now())); } catch {}
    if (toast) toast.remove();
  };

  const requestLocation = (toast) => {
    if (!canUseGeo()) return;
    if (!shouldSendNow()) return;

    if (hasRecentGeo()) return;

    navigator.geolocation.getCurrentPosition(
      (pos) => {
        const coords = pos && pos.coords ? pos.coords : null;
        if (!coords) return;
        const payload = {
          lat: Number(coords.latitude),
          lon: Number(coords.longitude),
          acc: coords.accuracy != null ? Number(coords.accuracy) : null,
          source: 'geo-consent-prompt'
        };
        if (!Number.isFinite(payload.lat) || !Number.isFinite(payload.lon)) return;
        storeLastGeo(payload);
        dismissPrompt(toast);
        void sendToServer(payload);
      },
      () => {
        dismissPrompt(toast);
      },
      {
        enableHighAccuracy: false,
        timeout: 6000,
        maximumAge: 30 * 60 * 1000
      }
    );
  };

  const findPromptHost = () =>
    document.querySelector('main.content-body')
    || document.querySelector('#mainContent')
    || document.querySelector('main.site-main')
    || document.querySelector('.main-layout > main')
    || document.querySelector('main');

  const showLocationPrompt = () => {
    if (!canUseGeo() || !shouldSendNow() || hasRecentGeo() || wasPromptDismissedRecently()) return;
    if (document.querySelector('[data-otelturizm-geo-prompt]')) return;

    const toast = document.createElement('section');
    toast.className = 'ot-geo-prompt';
    toast.setAttribute('data-otelturizm-geo-prompt', 'true');
    toast.setAttribute('role', 'status');
    toast.innerHTML = `
      <button type="button" class="ot-geo-prompt__close" aria-label="Konum bildirimini kapat">×</button>
      <div class="ot-geo-prompt__icon"><i class="fas fa-location-dot" aria-hidden="true"></i></div>
      <div class="ot-geo-prompt__content">
        <strong>Yakın oteller için konum</strong>
        <span>İzin verirseniz size daha yakın sonuçlar gösterebiliriz.</span>
      </div>
      <div class="ot-geo-prompt__actions">
        <button type="button" class="ot-geo-prompt__primary">Konum paylaş</button>
        <button type="button" class="ot-geo-prompt__ghost">Sonra</button>
      </div>`;

    const host = findPromptHost();
    if (host) {
      toast.classList.add('ot-geo-prompt--inline');
      host.insertBefore(toast, host.firstChild);
    } else {
      toast.classList.add('ot-geo-prompt--fallback');
      document.body.insertBefore(toast, document.body.firstChild);
    }

    toast.querySelector('.ot-geo-prompt__primary')?.addEventListener('click', () => requestLocation(toast));
    toast.querySelector('.ot-geo-prompt__ghost')?.addEventListener('click', () => dismissPrompt(toast));
    toast.querySelector('.ot-geo-prompt__close')?.addEventListener('click', () => dismissPrompt(toast));
  };

  // Sayfa yüklenince tarayıcı iznini doğrudan isteme; önce platform içi bildirim göster.
  if (document.readyState === 'complete' || document.readyState === 'interactive') {
    setTimeout(showLocationPrompt, 1600);
  } else {
    document.addEventListener('DOMContentLoaded', () => setTimeout(showLocationPrompt, 1600));
  }
})();

