(function () {
    'use strict';

    var body = document.body;
    if (!body.classList.contains('firmapanel-v6-shell')) return;

    var sidebar = document.getElementById('appSidebar');
    var toggleBtn = document.getElementById('firmaSidebarToggle');
    var backdrop = document.getElementById('firmaSidebarBackdrop');
    var avatarBtn = document.getElementById('firmaAvatarBtn');
    var profileDropdown = document.getElementById('firmaProfileDropdown');
    var mqMobile = window.matchMedia('(max-width: 991px)');

    function isMobile() {
        return mqMobile.matches;
    }

    function closeMobileSidebar() {
        body.classList.remove('mobile-open');
    }

    function toggleSidebar() {
        if (isMobile()) {
            body.classList.toggle('mobile-open');
            return;
        }
        body.classList.toggle('sidebar-collapsed');
    }

    if (toggleBtn) {
        toggleBtn.addEventListener('click', function (e) {
            e.stopPropagation();
            toggleSidebar();
        });
    }

    if (backdrop) {
        backdrop.addEventListener('click', closeMobileSidebar);
    }

    document.querySelectorAll('.firmapanel-v6-shell .nav-menu-item[data-nav-group]').forEach(function (item) {
        var trigger = item.querySelector('[data-firma-nav-toggle]');
        if (!trigger) return;

        trigger.addEventListener('click', function (e) {
            e.preventDefault();
            var willOpen = !item.classList.contains('open');
            item.classList.toggle('open', willOpen);
            item.classList.toggle('active', willOpen);
            trigger.setAttribute('aria-expanded', willOpen ? 'true' : 'false');
        });
    });

    if (avatarBtn && profileDropdown) {
        avatarBtn.addEventListener('click', function (e) {
            e.stopPropagation();
            profileDropdown.classList.toggle('active');
        });

        document.addEventListener('click', function (e) {
            if (!profileDropdown.contains(e.target) && !avatarBtn.contains(e.target)) {
                profileDropdown.classList.remove('active');
            }
        });
    }

    document.querySelectorAll('.firmapanel-v6-shell .sub-links-container a').forEach(function (link) {
        link.addEventListener('click', function () {
            if (isMobile()) closeMobileSidebar();
        });
    });

    mqMobile.addEventListener('change', function () {
        if (!isMobile()) {
            closeMobileSidebar();
        } else {
            body.classList.remove('sidebar-collapsed');
        }
    });

    var searchInput = document.getElementById('firmaGlobalSearch');
    if (searchInput) {
        document.addEventListener('keydown', function (e) {
            if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k') {
                e.preventDefault();
                searchInput.focus();
            }
        });

        searchInput.addEventListener('keydown', function (e) {
            if (e.key !== 'Enter') {
                return;
            }

            var query = (searchInput.value || '').trim();
            if (!query) {
                return;
            }

            var base = searchInput.getAttribute('data-firma-search-base') || '/panel/firma/rezervasyonlar';
            window.location.href = base + '?q=' + encodeURIComponent(query);
        });
    }

    document.querySelectorAll('.firmapanel-v6-shell .currency-switcher [data-currency]').forEach(function (btn) {
        btn.addEventListener('click', function () {
            var group = btn.closest('.currency-switcher');
            if (!group) return;
            group.querySelectorAll('[data-currency]').forEach(function (item) {
                item.classList.toggle('active', item === btn);
            });
            try {
                localStorage.setItem('firmaPanelDisplayCurrency', btn.getAttribute('data-currency') || 'TRY');
            } catch (err) { /* ignore */ }
        });
    });

    try {
        var savedCurrency = localStorage.getItem('firmaPanelDisplayCurrency');
        if (savedCurrency) {
            document.querySelectorAll('.firmapanel-v6-shell .currency-switcher [data-currency="' + savedCurrency + '"]').forEach(function (btn) {
                btn.click();
            });
        }
    } catch (err) { /* ignore */ }
})();
