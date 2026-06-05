(() => {
    const root = document.querySelector('[data-ai-route]');
    if (!root) return;

    const queryInput = root.querySelector('[data-ai-query]');
    const summaryEl = root.querySelector('[data-ai-summary]');
    const submitBtn = root.querySelector('[data-ai-submit]');
    const tagButtons = root.querySelectorAll('[data-ai-tag]');

    const segmentDropdown = root.querySelector('[data-ai-segment-dropdown]');
    const segmentTrigger = root.querySelector('[data-ai-segment-trigger]');
    const segmentPanel = root.querySelector('[data-ai-segment-panel]');
    const segmentLabelEl = root.querySelector('[data-ai-segment-label]');
    const segmentCountEl = root.querySelector('[data-ai-segment-count]');
    const segmentChipsEl = root.querySelector('[data-ai-segment-chips]');
    const segmentInputs = root.querySelectorAll('[data-ai-segment]');

    const selectedTags = new Map();
    let manualQuery = '';

    const segmentLabels = {
        'premium-luks': 'Premium Lüks',
        butik: 'Butik',
        aile: 'Aile Dostu',
        'is-seyahati': 'İş Seyahati',
        ekonomik: 'Ekonomik'
    };

    const escapeRegExp = (value) => value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');

    const stripSelectedTagsFromValue = (value) => {
        let val = value || '';
        selectedTags.forEach((text) => {
            const esc = escapeRegExp(text);
            val = val
                .replace(new RegExp(`(,\\s*)${esc}(?=\\s*,|\\s*$)`, 'gi'), '')
                .replace(new RegExp(`^${esc}\\s*,\\s*`, 'gi'), '')
                .replace(new RegExp(`^${esc}$`, 'gi'), '')
                .trim();
        });
        return val.replace(/,\s*,+/g, ',').replace(/^,\s*|,\s*$/g, '').trim();
    };

    const rebuildQueryInput = () => {
        if (!queryInput) return;
        const parts = [];
        const manual = manualQuery.trim();
        if (manual) parts.push(manual);
        selectedTags.forEach((text) => parts.push(text));
        queryInput.value = parts.join(', ');
    };

    const getActiveSegmentInputs = () =>
        Array.from(segmentInputs).filter(input => input.checked);

    const updateSegmentLabel = () => {
        if (!segmentLabelEl) return;
        const active = getActiveSegmentInputs();
        const names = active.map(input => {
            const key = input.getAttribute('data-ai-segment') || '';
            return segmentLabels[key] || '';
        }).filter(Boolean);

        if (names.length === 0) {
            segmentLabelEl.textContent = 'Segment seçin';
            return;
        }

        if (names.length <= 2) {
            segmentLabelEl.textContent = names.join(', ');
            return;
        }

        segmentLabelEl.textContent = `${names.slice(0, 2).join(', ')} +${names.length - 2}`;
    };

    const renderSegmentChips = () => {
        if (!segmentChipsEl) return;
        segmentChipsEl.innerHTML = '';
        const active = getActiveSegmentInputs();

        if (active.length > 1) {
            active.forEach(input => {
                const key = input.getAttribute('data-ai-segment') || '';
                const label = segmentLabels[key];
                if (!label) return;

                const chip = document.createElement('span');
                chip.className = 'ai-segment-chip';
                chip.textContent = label;
                segmentChipsEl.appendChild(chip);
            });
        }

        segmentChipsEl.classList.toggle('has-chips', active.length > 1);
    };

    const updateSegmentCount = () => {
        if (!segmentCountEl) return;
        const count = getActiveSegmentInputs().length;
        if (count <= 1) {
            segmentCountEl.hidden = true;
            segmentCountEl.textContent = '';
            return;
        }
        segmentCountEl.hidden = false;
        segmentCountEl.textContent = `${count} segment seçili`;
    };

    const updateSummary = () => {
        updateSegmentLabel();
        updateSegmentCount();
        renderSegmentChips();

        if (!summaryEl) return;

        const items = [];
        const query = (queryInput?.value || '').trim();
        if (query) {
            const label = query.length > 72 ? `${query.slice(0, 69)}…` : query;
            items.push({ type: 'query', label });
        }

        getActiveSegmentInputs().forEach(input => {
            const key = input.getAttribute('data-ai-segment') || '';
            const label = segmentLabels[key];
            if (label) items.push({ type: 'segment', label });
        });

        summaryEl.innerHTML = '';
        if (items.length === 0) {
            summaryEl.innerHTML = '<span class="ai-summary-empty">Aramada kullanılacak kriterler burada görünür</span>';
            return;
        }

        items.forEach(item => {
            const chip = document.createElement('span');
            chip.className = `ai-summary-chip ai-summary-chip--${item.type}`;
            chip.textContent = item.label;
            chip.title = item.label;
            summaryEl.appendChild(chip);
        });
    };

    const buildQuery = () => {
        const parts = [];
        const query = (queryInput?.value || '').trim();
        if (query) parts.push(query);
        getActiveSegmentInputs().forEach(input => {
            const key = input.getAttribute('data-ai-segment') || '';
            const label = segmentLabels[key] || '';
            if (label) parts.push(`${label} segment`);
        });
        return parts.join(', ');
    };

    const closeSegmentPanel = () => {
        segmentDropdown?.classList.remove('is-open');
        segmentPanel?.setAttribute('hidden', '');
        segmentTrigger?.setAttribute('aria-expanded', 'false');
    };

    const openSegmentPanel = () => {
        segmentDropdown?.classList.add('is-open');
        segmentPanel?.removeAttribute('hidden');
        segmentTrigger?.setAttribute('aria-expanded', 'true');
    };

    const toggleSegmentPanel = () => {
        if (segmentDropdown?.classList.contains('is-open')) {
            closeSegmentPanel();
        } else {
            openSegmentPanel();
        }
    };

    tagButtons.forEach(btn => {
        btn.addEventListener('click', () => {
            const tagText = btn.getAttribute('data-ai-tag') || '';
            const label = btn.textContent.trim();
            if (selectedTags.has(label)) {
                selectedTags.delete(label);
                btn.classList.remove('active');
            } else {
                selectedTags.set(label, tagText);
                btn.classList.add('active');
            }
            rebuildQueryInput();
            updateSummary();
        });
    });

    segmentInputs.forEach(input => {
        input.addEventListener('change', () => {
            const checked = getActiveSegmentInputs();
            if (checked.length === 0) {
                input.checked = true;
            }
            updateSummary();
        });
    });

    segmentTrigger?.addEventListener('click', (e) => {
        e.stopPropagation();
        toggleSegmentPanel();
    });

    segmentPanel?.addEventListener('click', (e) => e.stopPropagation());

    document.addEventListener('click', () => closeSegmentPanel());

    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape') closeSegmentPanel();
    });

    queryInput?.addEventListener('input', () => {
        manualQuery = stripSelectedTagsFromValue(queryInput.value);
        updateSummary();
    });

    submitBtn?.addEventListener('click', () => {
        const q = buildQuery();
        const url = q ? `/oteller?q=${encodeURIComponent(q)}` : '/oteller';
        window.location.href = url;
    });

    manualQuery = stripSelectedTagsFromValue(queryInput?.value || '');
    updateSummary();
})();
