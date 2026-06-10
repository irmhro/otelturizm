(function () {
    'use strict';

    var body = document.body;
    if (!body.classList.contains('kullanici-panel-shell')) return;

    var sidebar = document.getElementById('appSidebar');
    var toggleBtn = document.getElementById('userSidebarToggle');
    var backdrop = document.getElementById('userSidebarBackdrop');
    var avatarBtn = document.getElementById('userAvatarBtn');
    var profileDropdown = document.getElementById('userProfileDropdown');
    var themeOpen = document.getElementById('userThemeOpen');
    var themeClose = document.getElementById('userThemeClose');
    var themeBackdrop = document.getElementById('userThemeBackdrop');
    var mqMobile = window.matchMedia('(max-width: 991px)');

    function isMobile() {
        return mqMobile.matches;
    }

    function closeMobileSidebar() {
        body.classList.remove('mobile-open');
        syncMobileMenuState();
    }

    function toggleSidebar() {
        if (isMobile()) {
            body.classList.toggle('mobile-open');
            return;
        }
        body.classList.toggle('sidebar-collapsed');
    }

    function openThemeDrawer() {
        body.classList.add('theme-drawer-open');
        var drawer = document.getElementById('userThemeSettings');
        if (drawer) drawer.setAttribute('aria-hidden', 'false');
    }

    function closeThemeDrawer() {
        body.classList.remove('theme-drawer-open');
        var drawer = document.getElementById('userThemeSettings');
        if (drawer) drawer.setAttribute('aria-hidden', 'true');
    }

    function syncMobileMenuState() {
        var mobileMenuBtn = document.getElementById('userMobileMenuBtn');
        if (!mobileMenuBtn) return;
        var open = body.classList.contains('mobile-open');
        mobileMenuBtn.classList.toggle('active', open);
        mobileMenuBtn.setAttribute('aria-expanded', open ? 'true' : 'false');
    }

    if (toggleBtn) {
        toggleBtn.addEventListener('click', function (e) {
            e.stopPropagation();
            toggleSidebar();
            syncMobileMenuState();
        });
    }

    var mobileMenuBtn = document.getElementById('userMobileMenuBtn');
    if (mobileMenuBtn) {
        mobileMenuBtn.addEventListener('click', function (e) {
            e.stopPropagation();
            toggleSidebar();
            syncMobileMenuState();
        });
    }

    if (backdrop) {
        backdrop.addEventListener('click', closeMobileSidebar);
    }

    if (themeOpen) {
        themeOpen.addEventListener('click', function (e) {
            e.preventDefault();
            openThemeDrawer();
        });
    }

    if (themeClose) {
        themeClose.addEventListener('click', closeThemeDrawer);
    }

    if (themeBackdrop) {
        themeBackdrop.addEventListener('click', closeThemeDrawer);
    }

    document.querySelectorAll('.kullanici-panel-shell .nav-menu-item[data-nav-group]').forEach(function (item) {
        var trigger = item.querySelector('[data-user-nav-toggle]');
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
            avatarBtn.setAttribute('aria-expanded', profileDropdown.classList.contains('active') ? 'true' : 'false');
        });

        document.addEventListener('click', function (e) {
            if (!profileDropdown.contains(e.target) && !avatarBtn.contains(e.target)) {
                profileDropdown.classList.remove('active');
                avatarBtn.setAttribute('aria-expanded', 'false');
            }
        });
    }

    document.querySelectorAll('.kullanici-panel-shell .sub-links-container a').forEach(function (link) {
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

    var searchInput = document.getElementById('userGlobalSearch');
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

            var base = searchInput.getAttribute('data-user-search-base') || '/panel/user/rezervasyonlarim';
            window.location.href = base + '?searchTerm=' + encodeURIComponent(query);
        });
    }

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') {
            closeThemeDrawer();
            closeMobileSidebar();
        }
    });
})();
