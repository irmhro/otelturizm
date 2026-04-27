(() => {
  // p60: Web-Vitals (RUM) - lightweight, no external dependency.
  // Sends metrics via sendBeacon/fetch to /rum/vitals with sampling.

  const SAMPLE_RATE = 10; // percent (1-100)
  const sampleHit = Math.random() * 100 < SAMPLE_RATE;
  if (!sampleHit) return;

  const endpoint = "/rum/vitals";
  const navType =
    (performance.getEntriesByType && performance.getEntriesByType("navigation")[0]?.type) ||
    (performance.navigation?.type === 1 ? "reload" : "navigate");

  const basePayload = () => ({
    route: location.pathname,
    page: location.href,
    navType,
    referrer: document.referrer || "",
    sampleRate: SAMPLE_RATE,
    dpr: String(window.devicePixelRatio || 1),
    vw: window.innerWidth || 0,
    vh: window.innerHeight || 0,
  });

  const send = (metric, value, unit) => {
    try {
      const payload = { ...basePayload(), metric, value, unit };
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
      }).catch(() => {});
    } catch {
      // ignore
    }
  };

  // CLS
  try {
    let cls = 0;
    const clsObserver = new PerformanceObserver((list) => {
      for (const entry of list.getEntries()) {
        if (!entry.hadRecentInput) cls += entry.value;
      }
    });
    clsObserver.observe({ type: "layout-shift", buffered: true });
    addEventListener(
      "visibilitychange",
      () => {
        if (document.visibilityState === "hidden") send("CLS", cls, "score");
      },
      { once: true }
    );
  } catch {}

  // LCP
  try {
    let lcp = 0;
    const lcpObserver = new PerformanceObserver((list) => {
      const entries = list.getEntries();
      const last = entries[entries.length - 1];
      if (last) lcp = last.startTime;
    });
    lcpObserver.observe({ type: "largest-contentful-paint", buffered: true });
    addEventListener(
      "visibilitychange",
      () => {
        if (document.visibilityState === "hidden" && lcp > 0) send("LCP", Math.round(lcp), "ms");
      },
      { once: true }
    );
  } catch {}

  // INP (fallback to FID-ish behavior if unsupported)
  try {
    let inp = 0;
    const inpObserver = new PerformanceObserver((list) => {
      const entries = list.getEntries();
      for (const entry of entries) {
        const d = entry.duration || 0;
        if (d > inp) inp = d;
      }
    });
    inpObserver.observe({ type: "event", buffered: true, durationThreshold: 40 });
    addEventListener(
      "visibilitychange",
      () => {
        if (document.visibilityState === "hidden" && inp > 0) send("INP", Math.round(inp), "ms");
      },
      { once: true }
    );
  } catch {}
})();

