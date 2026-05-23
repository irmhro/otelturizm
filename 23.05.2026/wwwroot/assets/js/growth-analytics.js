(() => {
  const endpoint = "/growth/events";
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
        keepalive: true
      }).catch(() => {});
    } catch {
      // ignore
    }
  };

  const conn = navigator.connection || navigator.mozConnection || navigator.webkitConnection;
  const base = () => ({
    route: location.pathname,
    page: location.href,
    vw: window.innerWidth || 0,
    vh: window.innerHeight || 0,
    connection: conn ? `${conn.effectiveType || ""}/${conn.downlink || ""}` : "",
    navType:
      (performance.getEntriesByType && performance.getEntriesByType("navigation")[0]?.type) ||
      (performance.navigation?.type === 1 ? "reload" : "navigate")
  });

  if (Math.random() < 0.35) {
    send({ ...base(), kind: "funnel", step: "page_view", meta: document.title || "" });
  }

  const clicks = new Map();
  document.addEventListener(
    "click",
    (ev) => {
      const t = ev.target && ev.target.closest ? ev.target.closest("button,a,[role=button]") : null;
      if (!t) return;
      const key = (t.getAttribute("id") || t.tagName || "el").toString();
      const now = Date.now();
      const prev = clicks.get(key) || [];
      prev.push(now);
      const recent = prev.filter((x) => now - x < 650);
      clicks.set(key, recent);
      if (recent.length >= 3) {
        send({ ...base(), kind: "rage_click", step: "triple", meta: location.pathname });
        clicks.delete(key);
      }
    },
    true
  );

  document.addEventListener(
    "click",
    (ev) => {
      if (Math.random() > 0.07) return;
      const el = ev.target;
      if (!el || !el.closest) return;
      if (el.closest("a,button,input,textarea,select,[role=button],label")) return;
      send({ ...base(), kind: "dead_click", step: "non_interactive", meta: (el.tagName || "").toLowerCase() });
    },
    true
  );

  try {
    const slow =
      conn && (conn.saveData || conn.effectiveType === "slow-2g" || conn.effectiveType === "2g");
    if (slow) document.documentElement.classList.add("ot-adaptive-lite");
    const nav = performance.getEntriesByType && performance.getEntriesByType("navigation")[0];
    if (nav && nav.duration > 3200) {
      document.documentElement.classList.add("ot-adaptive-lite");
    }
  } catch {
    // ignore
  }

  const abandonSeen = new Set();
  document.addEventListener(
    "blur",
    (ev) => {
      const el = ev.target;
      if (!el || el.tagName !== "INPUT" && el.tagName !== "SELECT" && el.tagName !== "TEXTAREA") return;
      const id = (el.name || el.id || "") + el.tagName;
      if (abandonSeen.has(id)) return;
      abandonSeen.add(id);
      send({
        ...base(),
        kind: "form_abandon",
        step: el.name || el.id || "field",
        meta: el.value ? "touched" : "empty"
      });
    },
    true
  );
})();
