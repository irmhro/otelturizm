/* Örneklemli tarayıcı hata raporu → POST /diagnostics/client-error (rate limited). */
(() => {
  const SAMPLE_RATE = 12;
  if (Math.random() * 100 >= SAMPLE_RATE) {
    return;
  }

  const endpoint = "/diagnostics/client-error";
  let lastKey = "";
  let lastAt = 0;

  const dedupe = (msg) => {
    const k = (msg || "").slice(0, 120);
    const now = Date.now();
    if (k === lastKey && now - lastAt < 8000) {
      return false;
    }
    lastKey = k;
    lastAt = now;
    return true;
  };

  const send = (payload) => {
    try {
      const body = JSON.stringify(payload);
      if (navigator.sendBeacon) {
        navigator.sendBeacon(endpoint, new Blob([body], { type: "application/json" }));
        return;
      }
      fetch(endpoint, {
        method: "POST",
        headers: { "content-type": "application/json" },
        body,
        keepalive: true,
        credentials: "same-origin",
      }).catch(() => {});
    } catch {
      /* sessiz */
    }
  };

  window.addEventListener(
    "error",
    (ev) => {
      const msg = ev.message || "script_error";
      if (!dedupe(msg)) {
        return;
      }
      send({
        message: msg,
        stack: ev.error && ev.error.stack ? String(ev.error.stack) : "",
        source: ev.filename || "",
        line: typeof ev.lineno === "number" ? ev.lineno : null,
        column: typeof ev.colno === "number" ? ev.colno : null,
        route: location.pathname,
        page: location.href,
      });
    },
    true
  );

  window.addEventListener("unhandledrejection", (ev) => {
    const msg =
      ev.reason && ev.reason.message
        ? String(ev.reason.message)
        : ev.reason
          ? String(ev.reason)
          : "unhandled_rejection";
    if (!dedupe(msg)) {
      return;
    }
    send({
      message: msg,
      stack:
        ev.reason && ev.reason.stack
          ? String(ev.reason.stack).slice(0, 1500)
          : "",
      route: location.pathname,
      page: location.href,
    });
  });
})();
