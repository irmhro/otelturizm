(function () {
  const storageKey = (form) => {
    const panel = form.querySelector("input[name='PanelKey']")?.value || "panel";
    const page = form.querySelector("input[name='PageUrl']")?.value || location.pathname;
    return `otelturizm.devFeedback.${panel}.${page}`;
  };

  const readForm = (form) => ({
    FeedbackId: form.querySelector("[name='FeedbackId']")?.value || "",
    FeedbackType: form.querySelector("[name='FeedbackType']")?.value || "",
    Title: form.querySelector("[name='Title']")?.value || "",
    Content: form.querySelector("[name='Content']")?.value || ""
  });

  const writeForm = (form, data) => {
    if (!data) return;
    const type = form.querySelector("[name='FeedbackType']");
    const feedbackId = form.querySelector("[name='FeedbackId']");
    const title = form.querySelector("[name='Title']");
    const content = form.querySelector("[name='Content']");
    if (feedbackId) feedbackId.value = data.FeedbackId || "";
    if (type && data.FeedbackType) type.value = data.FeedbackType;
    if (title && data.Title) title.value = data.Title;
    if (content && data.Content) content.value = data.Content;
  };

  const escapeHtml = (text) =>
    (text || "").replace(/[&<>"']/g, (char) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[char]));

  const saveDraft = (form) => {
    try {
      localStorage.setItem(storageKey(form), JSON.stringify(readForm(form)));
    } catch (_) {}
  };

  const loadDraft = (form) => {
    try {
      const raw = localStorage.getItem(storageKey(form));
      if (raw) writeForm(form, JSON.parse(raw));
    } catch (_) {}
  };

  document.querySelectorAll("[data-dev-feedback-clear='true']").forEach((node) => {
    try {
      const panelKey = JSON.parse(node.textContent || "{}").panelKey || "";
      Object.keys(localStorage)
        .filter((key) => key.startsWith(`otelturizm.devFeedback.${panelKey}.`))
        .forEach((key) => localStorage.removeItem(key));
    } catch (_) {}
  });

  const fillMeta = (form) => {
    const viewport = form.querySelector("[data-dev-feedback-viewport]");
    const device = form.querySelector("[data-dev-feedback-device]");
    if (viewport) viewport.value = `${window.innerWidth}x${window.innerHeight} DPR:${window.devicePixelRatio || 1}`;
    if (device) device.value = navigator.userAgent || "";
  };

  const resetForm = (form) => {
    if (!form) return;
    const id = form.querySelector("[name='FeedbackId']");
    const type = form.querySelector("[name='FeedbackType']");
    const title = form.querySelector("[name='Title']");
    const content = form.querySelector("[name='Content']");
    const file = form.querySelector("[name='Screenshot']");
    if (id) id.value = "";
    if (type) type.value = "Geliştirme";
    if (title) title.value = "";
    if (content) content.value = "";
    if (file) file.value = "";
  };

  const showResult = (modal, success, message) => {
    const box = modal.querySelector("[data-dev-feedback-result]");
    if (!box) return;
    box.hidden = false;
    box.className = `developer-feedback-result ${success ? "is-success" : "is-error"}`;
    box.textContent = message || (success ? "İşlem tamamlandı." : "İşlem başarısız.");
  };

  const renderHistory = async (modal) => {
    const state = modal.querySelector("[data-dev-feedback-history-state]");
    const list = modal.querySelector("[data-dev-feedback-history-list]");
    if (!state || !list || list.dataset.loaded === "true") return;
    state.textContent = "Geçmiş bildirimler yükleniyor...";
    try {
      const response = await fetch("/dev-bildirim/gecmis", { credentials: "same-origin", headers: { "X-Requested-With": "XMLHttpRequest" } });
      const payload = await response.json();
      if (!response.ok || !payload || payload.success !== true) throw new Error("history");
      const items = Array.isArray(payload.items) ? payload.items : [];
      list.innerHTML = items.length
        ? items.map((item) => `
          <article class="developer-feedback-history-item">
            <div class="developer-feedback-history-head">
              <div>
                <strong>${escapeHtml(item.title)}</strong>
                <p>${escapeHtml(item.pageLabel)}</p>
              </div>
              <span class="developer-feedback-status tone-${escapeHtml(item.statusTone)}">${escapeHtml(item.status)}</span>
            </div>
            <div class="developer-feedback-history-meta">
              <span>#${escapeHtml(String(item.id || ""))}</span>
              <span>${escapeHtml(item.feedbackType)}</span>
              <span>${escapeHtml(item.createdAtText)}</span>
            </div>
            <p class="developer-feedback-history-content">${escapeHtml(item.content)}</p>
            ${item.adminNote ? `<div class="developer-feedback-history-note"><strong>Geliştirme notu</strong><p>${escapeHtml(item.adminNote)}</p></div>` : ""}
            <div class="developer-feedback-history-actions">
              ${item.canEdit ? `<button type="button" class="developer-feedback-mini-btn" data-feedback-edit='${escapeHtml(JSON.stringify(item))}'>Düzenle</button>` : ""}
              ${item.canDelete ? `<button type="button" class="developer-feedback-mini-btn is-danger" data-feedback-delete='${escapeHtml(String(item.id || ""))}'>Sil</button>` : ""}
            </div>
          </article>`).join("")
        : "";
      state.textContent = items.length ? "" : "Henüz kayıtlı geri bildiriminiz yok.";
      list.dataset.loaded = "true";
    } catch (_) {
      state.textContent = "Geçmiş bildirimler yüklenemedi.";
    }
  };

  const switchTab = (modal, key) => {
    const form = modal.querySelector("[data-dev-feedback-form]");
    const history = modal.querySelector("[data-dev-feedback-history]");
    modal.querySelectorAll("[data-dev-feedback-tab]").forEach((btn) => btn.classList.toggle("is-active", btn.getAttribute("data-dev-feedback-tab") === key));
    if (form) form.hidden = key !== "form";
    if (history) history.hidden = key !== "history";
    if (key === "history") renderHistory(modal);
  };

  document.querySelectorAll("[data-dev-feedback-open]").forEach((button) => {
    button.addEventListener("click", () => {
      const modal = document.getElementById(button.getAttribute("data-dev-feedback-open") || "");
      if (!modal) return;
      modal.classList.add("is-open");
      modal.setAttribute("aria-hidden", "false");
      const form = modal.querySelector("[data-dev-feedback-form]");
      if (form) {
        loadDraft(form);
        fillMeta(form);
      }
      showResult(modal, true, "");
      const box = modal.querySelector("[data-dev-feedback-result]");
      if (box) box.hidden = true;
      switchTab(modal, "form");
      const first = modal.querySelector("input[name='Title']");
      if (first) first.focus();
    });
  });

  document.querySelectorAll("[data-dev-feedback-tab]").forEach((button) => {
    button.addEventListener("click", () => {
      const modal = button.closest(".developer-feedback-modal");
      if (!modal) return;
      switchTab(modal, button.getAttribute("data-dev-feedback-tab") || "form");
    });
  });

  document.querySelectorAll("[data-dev-feedback-close]").forEach((button) => {
    button.addEventListener("click", () => {
      const modal = button.closest(".developer-feedback-modal");
      if (!modal) return;
      modal.classList.remove("is-open");
      modal.setAttribute("aria-hidden", "true");
    });
  });

  document.querySelectorAll("[data-dev-feedback-form]").forEach((form) => {
    loadDraft(form);
    form.querySelectorAll("input, textarea, select").forEach((field) => {
      if (field.type === "hidden" || field.type === "file") return;
      field.addEventListener("input", () => {
        field.setAttribute("data-dev-feedback-dirty", "true");
        saveDraft(form);
      });
      field.addEventListener("change", () => {
        field.setAttribute("data-dev-feedback-dirty", "true");
        saveDraft(form);
      });
    });
    form.addEventListener("submit", async (event) => {
      event.preventDefault();
      fillMeta(form);
      saveDraft(form);
      const modal = form.closest(".developer-feedback-modal");
      const submit = form.querySelector(".developer-feedback-submit");
      if (submit) submit.disabled = true;
      try {
        const response = await fetch(form.getAttribute("action") || "/dev-bildirim/gonder", {
          method: "POST",
          body: new FormData(form),
          credentials: "same-origin",
          headers: { "X-Requested-With": "XMLHttpRequest" }
        });
        const payload = await response.json();
        if (!response.ok || !payload || payload.success !== true) {
          showResult(modal, false, payload?.message || "Bildirim gönderilemedi.");
          return;
        }
        localStorage.removeItem(storageKey(form));
        resetForm(form);
        if (modal) {
          const list = modal.querySelector("[data-dev-feedback-history-list]");
          if (list) list.dataset.loaded = "false";
          showResult(modal, true, payload.message || "Gönderildi.");
          switchTab(modal, "history");
        }
      } catch (_) {
        showResult(modal, false, "Bildirim gönderilemedi.");
      } finally {
        if (submit) submit.disabled = false;
      }
    });
  });

  document.addEventListener("click", async (event) => {
    const editButton = event.target.closest("[data-feedback-edit]");
    if (editButton) {
      const modal = editButton.closest(".developer-feedback-modal");
      const form = modal?.querySelector("[data-dev-feedback-form]");
      if (!modal || !form) return;
      try {
        const item = JSON.parse(editButton.getAttribute("data-feedback-edit") || "{}");
        writeForm(form, { FeedbackId: String(item.id || ""), FeedbackType: item.feedbackType || "Geliştirme", Title: item.title || "", Content: item.content || "" });
        showResult(modal, true, "");
        const box = modal.querySelector("[data-dev-feedback-result]");
        if (box) box.hidden = true;
        switchTab(modal, "form");
      } catch (_) {}
      return;
    }

    const deleteButton = event.target.closest("[data-feedback-delete]");
    if (!deleteButton) return;
    const modal = deleteButton.closest(".developer-feedback-modal");
    if (!modal) return;
    const token = modal.querySelector("input[name='__RequestVerificationToken']")?.value || "";
    const id = deleteButton.getAttribute("data-feedback-delete") || "";
    if (!id) return;
    try {
      const formData = new FormData();
      formData.append("__RequestVerificationToken", token);
      formData.append("id", id);
      const response = await fetch("/dev-bildirim/sil", {
        method: "POST",
        body: formData,
        credentials: "same-origin",
        headers: { "X-Requested-With": "XMLHttpRequest" }
      });
      const payload = await response.json();
      if (!response.ok || !payload || payload.success !== true) {
        showResult(modal, false, payload?.message || "Bildirim silinemedi.");
        return;
      }
      const list = modal.querySelector("[data-dev-feedback-history-list]");
      if (list) list.dataset.loaded = "false";
      showResult(modal, true, payload.message || "Bildirim silindi.");
      await renderHistory(modal);
    } catch (_) {
      showResult(modal, false, "Bildirim silinemedi.");
    }
  });

  document.addEventListener("keydown", (event) => {
    if (event.key !== "Escape") return;
    document.querySelectorAll(".developer-feedback-modal.is-open").forEach((modal) => {
      modal.classList.remove("is-open");
      modal.setAttribute("aria-hidden", "true");
    });
  });
})();
