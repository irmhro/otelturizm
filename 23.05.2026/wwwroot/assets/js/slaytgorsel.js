(function () {
  if (window.SlaytGorsel) return;

  function q(id) { return document.getElementById(id); }

  const root = q('slaytGorsel');
  const img = q('slaytGorselImage');
  const title = q('slaytGorselTitle');
  const count = q('slaytGorselCount');
  const thumbs = q('slaytGorselThumbs');
  const figure = q('slaytGorselFigure');

  let images = [];
  let activeIndex = 0;
  let prevScrollY = 0;
  let lastFocus = null;
  let lastTintSrc = '';
  let swipeStartX = 0;
  let swipeStartY = 0;
  let swipeActive = false;
  let swipeLocked = false;
  let swipePointerId = null;

  function clamp(n, min, max) { return Math.min(Math.max(n, min), max); }

  function computeTintFromImage(src) {
    if (!(figure instanceof HTMLElement) || !src || src === lastTintSrc) return;
    lastTintSrc = src;

    try {
      const probe = new Image();
      probe.crossOrigin = 'anonymous';
      probe.decoding = 'async';
      probe.loading = 'eager';
      probe.src = src;
      probe.onload = function () {
        try {
          const canvas = document.createElement('canvas');
          const ctx = canvas.getContext('2d', { willReadFrequently: true });
          if (!ctx) return;
          const w = Math.min(48, probe.naturalWidth || 48);
          const h = Math.min(48, probe.naturalHeight || 48);
          canvas.width = w;
          canvas.height = h;
          ctx.drawImage(probe, 0, 0, w, h);
          const data = ctx.getImageData(0, 0, w, h).data;
          let r = 0, g = 0, b = 0, count = 0;
          for (let i = 0; i < data.length; i += 16) {
            const a = data[i + 3];
            if (a < 50) continue;
            r += data[i];
            g += data[i + 1];
            b += data[i + 2];
            count++;
          }
          if (!count) return;
          r = Math.round(r / count);
          g = Math.round(g / count);
          b = Math.round(b / count);
          // Sinematik ama "maviye kaçmasın": mavi baskınsa biraz kırp ve sıcaklığı artır.
          const boost = 1.06;
          r = Math.min(255, Math.round(r * boost));
          g = Math.min(255, Math.round(g * boost));
          b = Math.min(255, Math.round(b * boost));

          // Blue dominance correction (özellikle indoor görsellerde mavi/soğuk bias olabiliyor)
          const blueDominance = b - Math.max(r, g);
          if (blueDominance > 18) {
            const cut = Math.min(42, Math.round(blueDominance * 0.55));
            b = Math.max(0, b - cut);
            r = Math.min(255, r + Math.round(cut * 0.35));
            g = Math.min(255, g + Math.round(cut * 0.18));
          }

          // Çok koyuysa biraz yukarı çek (gradientlerde daha okunaklı)
          const lum = (0.2126 * r + 0.7152 * g + 0.0722 * b);
          if (lum < 44) {
            const lift = Math.min(40, Math.round((44 - lum) * 0.6));
            r = Math.min(255, r + lift);
            g = Math.min(255, g + lift);
            b = Math.min(255, b + lift);
          }
          const tint = r + ', ' + g + ', ' + b;
          // Figure icinde ve genel arkaplanda (backdrop) ayni tonu kullan.
          figure.style.setProperty('--slayt-tint-rgb', tint);
          if (root instanceof HTMLElement) {
            root.style.setProperty('--slayt-tint-rgb', tint);
          }
        } catch (_) { /* no-op */ }
      };
    } catch (_) { /* no-op */ }
  }

  function setAmbient() {
    if (!images.length) return;
    const prev = (activeIndex - 1 + images.length) % images.length;
    const next = (activeIndex + 1) % images.length;
    const prevUrl = 'url(\"' + (images[prev] || '') + '\")';
    const nextUrl = 'url(\"' + (images[next] || '') + '\")';
    const activeUrl = 'url(\"' + (images[activeIndex] || '') + '\")';

    if (figure instanceof HTMLElement) {
      figure.style.setProperty('--slayt-prev', prevUrl);
      figure.style.setProperty('--slayt-next', nextUrl);
      figure.style.setProperty('--slayt-active', activeUrl);
    }
    // Backdrop root degiskeni: genel slayt arkaplanina da yansisin.
    if (root instanceof HTMLElement) {
      root.style.setProperty('--slayt-prev', prevUrl);
      root.style.setProperty('--slayt-next', nextUrl);
      root.style.setProperty('--slayt-active', activeUrl);
    }
  }

  function renderThumbs() {
    if (!thumbs) return;
    thumbs.innerHTML = images.map(function (src, idx) {
      const active = idx === activeIndex ? ' is-active' : '';
      return '<button type="button" class="slayt-gorsel__thumb' + active + '" data-slayt-thumb="' + idx + '" aria-label="Görsel ' + (idx + 1) + '">' +
        '<img src="' + src + '" alt="Küçük görsel ' + (idx + 1) + '" loading="lazy" decoding="async" />' +
        '</button>';
    }).join('');

    thumbs.querySelectorAll('[data-slayt-thumb]').forEach(function (btn) {
      btn.addEventListener('click', function () {
        const idx = parseInt(btn.getAttribute('data-slayt-thumb') || '0', 10);
        goTo(idx);
      });
    });

    const activeBtn = thumbs.querySelector('.slayt-gorsel__thumb.is-active');
    activeBtn?.scrollIntoView({ behavior: 'smooth', inline: 'center', block: 'nearest' });
  }

  function render() {
    if (!img || !root) return;
    if (!images.length) return;
    activeIndex = clamp(activeIndex, 0, images.length - 1);
    img.src = images[activeIndex];
    img.alt = (title?.textContent || 'Görsel') + ' ' + (activeIndex + 1);
    if (count) count.textContent = (activeIndex + 1) + ' / ' + images.length;
    renderThumbs();
    setAmbient();
    computeTintFromImage(images[activeIndex]);
  }

  function goTo(index) {
    if (!images.length) return;
    activeIndex = (index + images.length) % images.length;
    render();
  }

  function open(payload) {
    if (!root || !img) return;
    const nextImages = Array.isArray(payload?.images) ? payload.images.filter(Boolean) : [];
    if (!nextImages.length) return;

    images = nextImages;
    activeIndex = clamp(parseInt(payload?.startIndex || '0', 10) || 0, 0, images.length - 1);
    if (title) title.textContent = (payload?.title || '').toString().trim();

    lastFocus = document.activeElement;
    prevScrollY = window.scrollY || 0;

    root.hidden = false;
    root.setAttribute('aria-hidden', 'false');
    document.body.classList.add('slayt-gorsel-open');

    render();

    // focus
    const closeBtn = root.querySelector('[data-slayt-close]');
    closeBtn?.focus?.();
  }

  function close() {
    if (!root) return;
    root.hidden = true;
    root.setAttribute('aria-hidden', 'true');
    document.body.classList.remove('slayt-gorsel-open');
    images = [];
    activeIndex = 0;
    if (thumbs) thumbs.innerHTML = '';
    if (figure instanceof HTMLElement) {
      figure.style.removeProperty('--slayt-prev');
      figure.style.removeProperty('--slayt-next');
      figure.style.removeProperty('--slayt-active');
      figure.style.removeProperty('--slayt-tint-rgb');
    }
    if (root instanceof HTMLElement) {
      root.style.removeProperty('--slayt-prev');
      root.style.removeProperty('--slayt-next');
      root.style.removeProperty('--slayt-active');
      root.style.removeProperty('--slayt-tint-rgb');
    }
    window.scrollTo(0, prevScrollY || 0);
    if (lastFocus && typeof lastFocus.focus === 'function') lastFocus.focus();
  }

  function isOpen() {
    return !!(root && !root.hidden);
  }

  function resetSwipe() {
    swipeActive = false;
    swipeLocked = false;
    swipePointerId = null;
    swipeStartX = 0;
    swipeStartY = 0;
  }

  function shouldHandleSwipe(target) {
    if (!isOpen()) return false;
    if (!(figure instanceof HTMLElement)) return false;
    if (!(target instanceof HTMLElement)) return true;
    // thumbs icinde swipe yakalama (yatay scroll) istemiyoruz.
    if (target.closest('#slaytGorselThumbs')) return false;
    return true;
  }

  function handleSwipeDelta(dx, dy) {
    const absX = Math.abs(dx);
    const absY = Math.abs(dy);
    if (!swipeLocked) {
      // ilk karar: yatay mi dikey mi
      if (absX < 6 && absY < 6) return { decided: false, horizontal: false };
      swipeLocked = true;
      return { decided: true, horizontal: absX >= absY };
    }
    return { decided: true, horizontal: Math.abs(dx) >= Math.abs(dy) };
  }

  function tryCommitSwipe(dx) {
    const threshold = Math.max(42, Math.min(86, (window.innerWidth || 360) * 0.14));
    if (Math.abs(dx) < threshold) return false;
    goTo(activeIndex + (dx < 0 ? 1 : -1));
    return true;
  }

  // Pointer events (modern browsers)
  figure?.addEventListener('pointerdown', function (event) {
    if (!shouldHandleSwipe(event.target)) return;
    if (event.pointerType !== 'touch' && event.pointerType !== 'pen') return;
    swipePointerId = event.pointerId;
    swipeActive = true;
    swipeLocked = false;
    swipeStartX = event.clientX;
    swipeStartY = event.clientY;
    figure.setPointerCapture?.(event.pointerId);
  });

  figure?.addEventListener('pointermove', function (event) {
    if (!swipeActive || swipePointerId !== event.pointerId) return;
    const dx = event.clientX - swipeStartX;
    const dy = event.clientY - swipeStartY;
    const dec = handleSwipeDelta(dx, dy);
    if (dec.decided && dec.horizontal) {
      event.preventDefault?.();
    }
  });

  figure?.addEventListener('pointerup', function (event) {
    if (!swipeActive || swipePointerId !== event.pointerId) return;
    const dx = event.clientX - swipeStartX;
    const dy = event.clientY - swipeStartY;
    const dec = handleSwipeDelta(dx, dy);
    if (dec.decided && dec.horizontal) {
      tryCommitSwipe(dx);
    }
    resetSwipe();
  });

  figure?.addEventListener('pointercancel', function () {
    resetSwipe();
  });

  // Touch events (iOS Safari fallback)
  figure?.addEventListener('touchstart', function (event) {
    if (!shouldHandleSwipe(event.target)) return;
    if (!event.touches || event.touches.length !== 1) return;
    const t = event.touches[0];
    swipeActive = true;
    swipeLocked = false;
    swipeStartX = t.clientX;
    swipeStartY = t.clientY;
  }, { passive: true });

  figure?.addEventListener('touchmove', function (event) {
    if (!swipeActive) return;
    if (!event.touches || event.touches.length !== 1) return;
    const t = event.touches[0];
    const dx = t.clientX - swipeStartX;
    const dy = t.clientY - swipeStartY;
    const dec = handleSwipeDelta(dx, dy);
    if (dec.decided && dec.horizontal) {
      event.preventDefault();
    }
  }, { passive: false });

  figure?.addEventListener('touchend', function (event) {
    if (!swipeActive) return;
    const t = event.changedTouches && event.changedTouches[0];
    if (t) {
      const dx = t.clientX - swipeStartX;
      const dy = t.clientY - swipeStartY;
      const dec = handleSwipeDelta(dx, dy);
      if (dec.decided && dec.horizontal) {
        tryCommitSwipe(dx);
      }
    }
    resetSwipe();
  }, { passive: true });

  document.addEventListener('click', function (event) {
    if (!root || root.hidden) return;
    const target = event.target instanceof HTMLElement ? event.target : null;
    if (!target) return;
    if (target.closest('[data-slayt-close]')) {
      event.preventDefault();
      close();
      return;
    }
    if (target.closest('[data-slayt-prev]')) {
      event.preventDefault();
      goTo(activeIndex - 1);
      return;
    }
    if (target.closest('[data-slayt-next]')) {
      event.preventDefault();
      goTo(activeIndex + 1);
      return;
    }
  });

  document.addEventListener('keydown', function (event) {
    if (!isOpen()) return;
    if (event.key === 'Escape') {
      event.preventDefault();
      close();
      return;
    }
    if (event.key === 'ArrowLeft') {
      event.preventDefault();
      goTo(activeIndex - 1);
      return;
    }
    if (event.key === 'ArrowRight') {
      event.preventDefault();
      goTo(activeIndex + 1);
      return;
    }
  });

  window.SlaytGorsel = { open: open, close: close, isOpen: isOpen, goTo: goTo };
})();

