/**
 * Ortak panel toast (#ot-panel-toast-stack) — dismiss .is-hiding ile panel-toasts.css uyumlu.
 * Programatik API: window.OtPanelToast.show(text, tone, title?)
 */
(() => {
  const TITLES = {
    success: "Kaydedildi",
    danger: "Hata",
    warning: "Uyarı",
    info: "Kaydediliyor",
  };

  const GLYPHS = {
    success: "fas fa-circle-check",
    danger: "fas fa-circle-exclamation",
    warning: "fas fa-triangle-exclamation",
    info: "fas fa-circle-notch fa-spin",
  };

  const TIMEOUTS = {
    success: 4200,
    danger: 8500,
    warning: 6500,
    info: 2200,
  };

  const ensureStack = () => {
    let stack = document.getElementById("ot-panel-toast-stack");
    if (stack) return stack;

    stack = document.createElement("div");
    stack.id = "ot-panel-toast-stack";
    stack.className = "ot-toast-stack";
    stack.setAttribute("role", "region");
    stack.setAttribute("aria-label", "Sistem bildirimleri");
    stack.setAttribute("aria-live", "polite");
    stack.setAttribute("aria-atomic", "true");

    const host =
      document.querySelector(".content-body") ||
      document.querySelector("main") ||
      document.body;
    host.prepend(stack);
    return stack;
  };

  const dismissToast = (toast, stack) => {
    if (!toast || !toast.isConnected) return;
    const prefersReduced =
      window.matchMedia &&
      window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    toast.classList.add("is-hiding");
    const removeAfter = prefersReduced ? 0 : 240;
    window.setTimeout(() => {
      if (toast.isConnected) toast.remove();
      if (stack.isConnected && !stack.querySelector(".ot-toast")) {
        stack.remove();
      }
    }, removeAfter);
  };

  const scheduleDismiss = (toast, stack, ms) => {
    if (!ms) return;
    window.setTimeout(() => dismissToast(toast, stack), ms);
  };

  const createToast = (text, tone, title) => {
    const safeTone = TITLES[tone] ? tone : "success";
    const stack = ensureStack();
    stack.querySelectorAll('.ot-toast[data-ot-dynamic="1"]').forEach((node) => {
      dismissToast(node, stack);
    });

    const toast = document.createElement("div");
    toast.className = `ot-toast ot-toast--${safeTone === "danger" ? "danger" : safeTone}`;
    toast.setAttribute("role", safeTone === "danger" ? "alert" : "status");
    toast.dataset.otDynamic = "1";
    toast.dataset.otToast = safeTone;

    const glyph = document.createElement("span");
    glyph.className = "ot-toast__glyph";
    glyph.setAttribute("aria-hidden", "true");
    glyph.innerHTML = `<i class="${GLYPHS[safeTone]}"></i>`;

    const body = document.createElement("div");
    body.className = "ot-toast__body";

    const titleEl = document.createElement("p");
    titleEl.className = "ot-toast__title";
    titleEl.textContent = title || TITLES[safeTone];

    const textEl = document.createElement("p");
    textEl.className = "ot-toast__text";
    textEl.textContent = text || "";

    body.appendChild(titleEl);
    body.appendChild(textEl);
    toast.appendChild(glyph);
    toast.appendChild(body);
    stack.appendChild(toast);

    scheduleDismiss(toast, stack, TIMEOUTS[safeTone]);
    return toast;
  };

  window.OtPanelToast = {
    show(text, tone = "success", title) {
      return createToast(text, tone, title);
    },
  };

  const stack = document.getElementById("ot-panel-toast-stack");
  if (!stack) return;

  const toasts = Array.from(stack.querySelectorAll(".ot-toast"));
  if (!toasts.length) return;

  toasts.forEach((toast) => {
    const msAttr = toast.getAttribute("data-timeout-ms");
    const ms = Math.max(0, parseInt(msAttr || "0", 10) || 0);
    scheduleDismiss(toast, stack, ms);
  });
})();
