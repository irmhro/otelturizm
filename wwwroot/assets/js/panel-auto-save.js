/**
 * Ortak panel otomatik kayıt — PANEL_OTOMATIK_KAYIT_SOZLESMESI.md
 * Formlar: [data-panel-auto-save], aksiyon formları: [data-panel-auto-action], yükleme: [data-panel-auto-upload]
 */
(() => {
  const DEBOUNCE_MS = 480;
  const toastApi = () => window.OtPanelToast;

  const postForm = async (targetForm) => {
    const fallbackSuccess =
      targetForm.dataset.panelAutoUploadSuccess ||
      targetForm.dataset.panelAutoSaveSuccess ||
      "Seçiminiz kaydedildi.";

    const response = await fetch(targetForm.action, {
      method: "POST",
      body: new FormData(targetForm),
      headers: {
        "X-Requested-With": "XMLHttpRequest",
        Accept: "application/json",
      },
      credentials: "same-origin",
      redirect: "manual",
    });

    const payload = await readPayload(response, fallbackSuccess);
    if (!payload.success) {
      throw new Error(payload.message || "Kayıt tamamlanamadı.");
    }
    return payload;
  };

  const readPayload = async (response, fallbackSuccess) => {
    const contentType = (response.headers.get("content-type") || "").toLowerCase();

    if (contentType.includes("application/json")) {
      const payload = await response.json().catch(() => null);
      if (!payload) {
        return { success: false, message: "İşlem sonucu okunamadı." };
      }
      return payload;
    }

    // MVC redirect = işlem genelde başarılı (eski endpoint uyumluluğu)
    if (response.status >= 300 && response.status < 400) {
      return { success: true, message: fallbackSuccess, reload: false };
    }

    if (response.ok) {
      const text = (await response.text()).trim();
      if (!text) {
        return { success: true, message: fallbackSuccess, reload: false };
      }
      try {
        return JSON.parse(text);
      } catch {
        return { success: true, message: fallbackSuccess, reload: false };
      }
    }

    return {
      success: false,
      message:
        response.status === 400
          ? "İstek doğrulanamadı. Sayfayı yenileyip tekrar deneyin."
          : "İşlem tamamlanamadı.",
    };
  };

  const shouldReload = (targetForm, payload) => {
    if (targetForm.hasAttribute("data-panel-auto-save")) return false;

    const reloadPref =
      targetForm.dataset.panelAutoActionReload ??
      targetForm.dataset.panelAutoUploadReload;
    if (reloadPref === "true") return payload.reload !== false;
    if (reloadPref === "false") return false;

    return payload.reload === true;
  };

  const bindAutoSaveForms = () => {
    document.querySelectorAll("[data-panel-auto-save]").forEach((saveForm) => {
      if (saveForm.dataset.panelAutoSaveBound === "1") return;
      saveForm.dataset.panelAutoSaveBound = "1";

      let timer = 0;
      let saving = false;

      const save = async () => {
        if (saving) return;
        if (typeof saveForm.checkValidity === "function" && !saveForm.checkValidity()) {
          return;
        }

        saving = true;
        const toast = toastApi();
        toast?.show("Kaydediliyor…", "info");

        try {
          const payload = await postForm(saveForm);
          const fallback =
            saveForm.dataset.panelAutoSaveSuccess || "Seçiminiz kaydedildi.";
          toast?.show(payload.message || fallback, "success");
          saveForm.dispatchEvent(
            new CustomEvent("panel-auto-save:success", {
              bubbles: true,
              detail: { payload, form: saveForm },
            })
          );
        } catch (error) {
          toast?.show(error.message || "Kayıt tamamlanamadı.", "danger");
        } finally {
          saving = false;
        }
      };

      const scheduleSave = () => {
        window.clearTimeout(timer);
        timer = window.setTimeout(save, DEBOUNCE_MS);
      };

      saveForm.addEventListener("submit", (event) => {
        event.preventDefault();
        window.clearTimeout(timer);
        save();
      });

      saveForm.querySelectorAll('[data-panel-autosave="change"]').forEach((field) => {
        field.addEventListener("change", scheduleSave);
      });

      saveForm.querySelectorAll('[data-panel-autosave="blur"]').forEach((field) => {
        field.addEventListener("blur", scheduleSave);
        field.addEventListener("keydown", (event) => {
          if (event.key !== "Enter" || field.tagName === "TEXTAREA") return;
          event.preventDefault();
          field.blur();
        });
      });
    });
  };

  const bindAutoActionForms = () => {
    document.querySelectorAll("[data-panel-auto-action]").forEach((actionForm) => {
      if (actionForm.dataset.panelAutoActionBound === "1") return;
      actionForm.dataset.panelAutoActionBound = "1";

      actionForm.addEventListener("submit", async (event) => {
        event.preventDefault();
        const action = actionForm.dataset.panelAutoAction || "";
        if (action === "delete" && !window.confirm("Bu kayıt silinsin mi?")) return;

        const toast = toastApi();
        toast?.show(
          action === "delete" ? "Siliniyor…" : "Kaydediliyor…",
          "info"
        );

        try {
          const payload = await postForm(actionForm);
          const successMessage =
            actionForm.dataset.panelAutoActionSuccess ||
            payload.message ||
            "Seçiminiz kaydedildi.";
          toast?.show(successMessage, "success");

          actionForm.dispatchEvent(
            new CustomEvent("panel-auto-action:success", {
              bubbles: true,
              detail: { action, payload, form: actionForm },
            })
          );

          if (shouldReload(actionForm, payload)) {
            window.setTimeout(() => window.location.reload(), 650);
          }
        } catch (error) {
          toast?.show(error.message || "İşlem tamamlanamadı.", "danger");
        }
      });
    });
  };

  const bindAutoUploadForms = () => {
    document.querySelectorAll("[data-panel-auto-upload]").forEach((uploadForm) => {
      if (uploadForm.dataset.panelAutoUploadBound === "1") return;
      uploadForm.dataset.panelAutoUploadBound = "1";

      const input = uploadForm.querySelector('input[type="file"]');
      if (!input) return;

      input.addEventListener("change", async () => {
        const file = input.files && input.files[0];
        if (!file) return;

        const toast = toastApi();
        toast?.show("Yükleniyor…", "info");

        try {
          const payload = await postForm(uploadForm);
          toast?.show(payload.message || "Yükleme tamamlandı.", "success");
          uploadForm.dispatchEvent(
            new CustomEvent("panel-auto-upload:success", {
              bubbles: true,
              detail: { payload, form: uploadForm },
            })
          );
          if (shouldReload(uploadForm, payload)) {
            window.setTimeout(() => window.location.reload(), 650);
          }
        } catch (error) {
          toast?.show(error.message || "Yükleme tamamlanamadı.", "danger");
          input.value = "";
        }
      });
    });
  };

  const init = () => {
    bindAutoSaveForms();
    bindAutoActionForms();
    bindAutoUploadForms();
  };

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", init);
  } else {
    init();
  }
})();
