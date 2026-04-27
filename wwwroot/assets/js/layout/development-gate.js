(function () {
    var accessUrl = '/gelisim/kilit-ac';
    var lockCodeLength = 6;
    var inputBuffer = '';
    var isSending = false;
    var unlockPaths = ['/gelisim'];
    var gateInput = document.getElementById('developmentGateCodeInput');
    var gateButton = document.getElementById('developmentGateSubmit');
    var gateStatus = document.getElementById('developmentGateStatus');

    function isUnlockPage() {
        var currentPath = (window.location.pathname || '').toLowerCase();
        return unlockPaths.some(function (path) { return currentPath.indexOf(path) === 0; });
    }

    function resetBuffer() {
        inputBuffer = '';
        if (gateInput) {
            gateInput.value = '';
        }
    }

    function setStatus(message, isError) {
        if (!gateStatus) {
            return;
        }

        gateStatus.textContent = message;
        gateStatus.classList.toggle('is-error', !!isError);
    }

    async function tryUnlock(code) {
        if (isSending) {
            return;
        }

        isSending = true;
        try {
            var response = await fetch(accessUrl, {
                method: 'POST',
                credentials: 'same-origin',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest'
                },
                body: JSON.stringify({ code: code })
            });

            if (response.ok) {
                setStatus('Kod doğrulandı. Platform açılıyor...', false);
                window.location.reload();
                return;
            }

            setStatus('Geçersiz erişim kodu. Tekrar deneyin.', true);
            gateInput?.classList.add('is-error');
        } catch (_) {
            setStatus('Erişim doğrulanamadı. Tekrar deneyin.', true);
        } finally {
            isSending = false;
            resetBuffer();
        }
    }

    document.addEventListener('keydown', function (event) {
        if (event.ctrlKey || event.altKey || event.metaKey) {
            return;
        }

        if (event.key === 'Backspace') {
            inputBuffer = inputBuffer.slice(0, -1);
            if (gateInput && document.activeElement !== gateInput) {
                gateInput.value = '•'.repeat(inputBuffer.length);
            }
            return;
        }

        if (!/^\d$/.test(event.key)) {
            return;
        }

        inputBuffer = (inputBuffer + event.key).slice(-lockCodeLength);
        if (gateInput && document.activeElement !== gateInput) {
            gateInput.classList.remove('is-error');
            gateInput.value = '•'.repeat(inputBuffer.length);
        }
        if (inputBuffer.length === lockCodeLength) {
            tryUnlock(inputBuffer);
        }
    }, true);

    gateButton?.addEventListener('click', function () {
        var typedCode = gateInput ? gateInput.value.trim() : inputBuffer.trim();
        if (!typedCode) {
            setStatus('Lütfen erişim kodunu girin.', true);
            gateInput?.classList.add('is-error');
            return;
        }

        gateInput?.classList.remove('is-error');
        tryUnlock(typedCode);
    });

    gateInput?.addEventListener('input', function () {
        inputBuffer = gateInput.value.trim().slice(0, 32);
        gateInput.classList.remove('is-error');
        setStatus('Yetkiliyseniz ekranda erişim kodunu yazın.', false);
    });

    gateInput?.addEventListener('keydown', function (event) {
        if (event.key === 'Enter') {
            event.preventDefault();
            gateButton?.click();
        }
    });

    if (isUnlockPage()) {
        document.body.classList.add('development-unlock-page');
    }
})();
