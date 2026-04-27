(() => {
  // p98: flash/toast auto dismiss
  const stack = document.querySelector(".ot-toast-stack");
  if (!stack) return;
  const toasts = Array.from(stack.querySelectorAll(".ot-toast"));
  toasts.forEach((t) => {
    const ms = Number(t.getAttribute("data-timeout-ms") || "5500");
    if (!Number.isFinite(ms) || ms <= 0) return;
    setTimeout(() => {
      t.style.transition = "opacity 250ms ease, transform 250ms ease";
      t.style.opacity = "0";
      t.style.transform = "translateY(-6px)";
      setTimeout(() => t.remove(), 300);
    }, ms);
  });
})();

