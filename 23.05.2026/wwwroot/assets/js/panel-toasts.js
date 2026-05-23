/**
 * Ortak panel toast (#ot-panel-toast-stack) — dismiss .is-hiding ile panel-toasts.css uyumlu.
 */
(() => {
  const stack = document.getElementById("ot-panel-toast-stack");
  if (!stack) return;

  const toasts = Array.from(stack.querySelectorAll(".ot-toast"));
  if (!toasts.length) return;

  const prefersReduced = window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches;

  toasts.forEach((toast) => {
    const msAttr = toast.getAttribute("data-timeout-ms");
    const ms = Math.max(0, parseInt(msAttr || "0", 10) || 0);
    if (!ms) return;

    window.setTimeout(() => {
      if (!toast || !toast.isConnected) return;
      toast.classList.add("is-hiding");
      if (prefersReduced) {
        toast.remove();
        if (stack && !stack.querySelector(".ot-toast")) {
          stack.remove();
        }
        return;
      }
      const removeAfter = 240;
      window.setTimeout(() => {
        if (toast && toast.isConnected) toast.remove();
        if (stack && stack.isConnected && !stack.querySelector(".ot-toast")) {
          stack.remove();
        }
      }, removeAfter);
    }, ms);
  });
})();
