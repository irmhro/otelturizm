const SUMMARY_URL = "https://localhost:7223/panel/header-bildiri/ozet?panelKey=partner";

const unreadPill = document.getElementById("unreadPill");
const panelLabel = document.getElementById("panelLabel");
const lastSync = document.getElementById("lastSync");
const list = document.getElementById("list");
const refreshBtn = document.getElementById("refreshBtn");

const formatSyncTime = (iso) => {
  if (!iso) return "-";
  try {
    return new Date(iso).toLocaleString("tr-TR");
  } catch {
    return "-";
  }
};

const renderItems = (items) => {
  list.innerHTML = "";
  if (!Array.isArray(items) || items.length === 0) {
    const empty = document.createElement("div");
    empty.className = "item";
    empty.innerHTML = "<strong>Bildirim yok</strong><p>Yeni bildirim geldiginde burada gorunecek.</p>";
    list.appendChild(empty);
    return;
  }

  items.forEach((item) => {
    if (!item || item.isPlaceholder) {
      return;
    }
    const node = document.createElement("a");
    node.className = "item";
    node.href = item.url || "https://localhost:7223/panel/partner/rezervasyonlar";
    node.target = "_blank";
    node.rel = "noreferrer";
    node.innerHTML = `
      <strong>${item.title || "-"}</strong>
      <p>${item.description || ""}</p>
      <small>${item.absoluteTimeLabel || item.timeLabel || "-"}</small>
    `;
    list.appendChild(node);
  });

  if (!list.children.length) {
    const fallback = document.createElement("div");
    fallback.className = "item";
    fallback.innerHTML = "<strong>Bildirim yok</strong><p>Yeni bildirim geldiginde burada gorunecek.</p>";
    list.appendChild(fallback);
  }
};

const refreshFromStorage = async () => {
  const data = await chrome.storage.local.get(["unreadCount", "panelLabel", "items", "lastSyncIso"]);
  panelLabel.textContent = data.panelLabel || "Partner";
  unreadPill.textContent = `${Number(data.unreadCount || 0)} okunmamis`;
  lastSync.textContent = `Son kontrol: ${formatSyncTime(data.lastSyncIso)}`;
  renderItems(data.items || []);
};

const refreshFromServer = async () => {
  refreshBtn.disabled = true;
  try {
    const response = await fetch(SUMMARY_URL, { method: "GET", credentials: "include", cache: "no-store" });
    if (response.ok) {
      const summary = await response.json();
      await chrome.storage.local.set({
        unreadCount: Number(summary.unreadCount || 0),
        panelLabel: summary.panelLabel || "Partner",
        items: Array.isArray(summary.items) ? summary.items : [],
        lastSyncIso: new Date().toISOString()
      });
    }
  } catch (_error) {
    // Sessizce gec.
  } finally {
    refreshBtn.disabled = false;
    await refreshFromStorage();
  }
};

refreshBtn.addEventListener("click", refreshFromServer);
refreshFromStorage();
