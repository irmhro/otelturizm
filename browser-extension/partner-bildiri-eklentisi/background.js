const SUMMARY_URL = "https://localhost:7223/panel/header-bildiri/ozet?panelKey=partner";
const POLL_ALARM_NAME = "partner-bildiri-poll";
const POLL_MINUTES = 1;

const setBadge = async (count) => {
  const safeCount = Number.isFinite(count) ? Math.max(0, count) : 0;
  if (safeCount > 0) {
    await chrome.action.setBadgeBackgroundColor({ color: "#0f5bd7" });
    await chrome.action.setBadgeText({ text: safeCount > 99 ? "99+" : String(safeCount) });
  } else {
    await chrome.action.setBadgeText({ text: "" });
  }
};

const saveState = async (state) => {
  await chrome.storage.local.set({
    unreadCount: state.unreadCount || 0,
    items: Array.isArray(state.items) ? state.items : [],
    panelLabel: state.panelLabel || "Partner",
    lastSyncIso: new Date().toISOString()
  });
};

const showNotification = async (summary, previousCount) => {
  const unreadCount = Number(summary.unreadCount || 0);
  if (unreadCount <= previousCount) {
    return;
  }

  const firstItem = Array.isArray(summary.items)
    ? summary.items.find((item) => item && !item.isPlaceholder)
    : null;
  const message = firstItem
    ? `${firstItem.title}\n${firstItem.absoluteTimeLabel || firstItem.timeLabel || ""}`
    : `${unreadCount} okunmamis bildirim var.`;

  await chrome.notifications.create({
    type: "basic",
    iconUrl: "https://localhost:7223/favicon.ico",
    title: `${summary.panelLabel || "Partner"} bildirimleri`,
    message: message,
    priority: 2
  });
};

const fetchSummary = async () => {
  const response = await fetch(SUMMARY_URL, {
    method: "GET",
    credentials: "include",
    cache: "no-store"
  });
  if (!response.ok) {
    throw new Error(`Bildiri ozeti alinamadi: ${response.status}`);
  }
  return response.json();
};

const poll = async () => {
  const stored = await chrome.storage.local.get(["unreadCount"]);
  const previousCount = Number(stored.unreadCount || 0);
  try {
    const summary = await fetchSummary();
    const unreadCount = Number(summary.unreadCount || 0);
    await setBadge(unreadCount);
    await saveState(summary);
    await showNotification(summary, previousCount);
  } catch (_error) {
    // Partner giris yapilmadiysa veya local site acik degilse sessizce gec.
  }
};

chrome.runtime.onInstalled.addListener(async () => {
  await chrome.alarms.create(POLL_ALARM_NAME, { periodInMinutes: POLL_MINUTES });
  await poll();
});

chrome.runtime.onStartup.addListener(async () => {
  await chrome.alarms.create(POLL_ALARM_NAME, { periodInMinutes: POLL_MINUTES });
  await poll();
});

chrome.alarms.onAlarm.addListener(async (alarm) => {
  if (alarm.name === POLL_ALARM_NAME) {
    await poll();
  }
});

chrome.notifications.onClicked.addListener(() => {
  chrome.tabs.create({ url: "https://localhost:7223/panel/partner/rezervasyonlar" });
});
