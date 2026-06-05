(function () {
    'use strict';

    var body = document.body;
    if (!body.classList.contains('admin-panel-shell')) return;

    var sidebar = document.getElementById('appSidebar');
    var toggleBtn = document.getElementById('adminSidebarToggle');
    var backdrop = document.getElementById('adminSidebarBackdrop');
    var avatarBtn = document.getElementById('adminAvatarBtn');
    var profileDropdown = document.getElementById('adminProfileDropdown');
    var themeOpen = document.getElementById('adminThemeOpen');
    var themeClose = document.getElementById('adminThemeClose');
    var themeBackdrop = document.getElementById('adminThemeBackdrop');
    var mqMobile = window.matchMedia('(max-width: 991px)');

    function isMobile() {
        return mqMobile.matches;
    }

    function resetSidebarScroll() {
        var scroll = sidebar && sidebar.querySelector('.sidebar-scroll');
        if (scroll) scroll.scrollTop = 0;
    }

    function resetShellLayout() {
        body.classList.remove('mobile-open');
        if (!isMobile()) {
            body.classList.remove('sidebar-collapsed');
        }
        resetSidebarScroll();
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

    function openThemeDrawer() {
        body.classList.add('theme-drawer-open');
        var drawer = document.getElementById('adminThemeSettings');
        if (drawer) drawer.setAttribute('aria-hidden', 'false');
    }

    function closeThemeDrawer() {
        body.classList.remove('theme-drawer-open');
        var drawer = document.getElementById('adminThemeSettings');
        if (drawer) drawer.setAttribute('aria-hidden', 'true');
    }

    resetShellLayout();

    if (toggleBtn) {
        toggleBtn.addEventListener('click', function (e) {
            e.stopPropagation();
            toggleSidebar();
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

    document.querySelectorAll('.admin-panel-shell .nav-menu-item[data-nav-group]').forEach(function (item) {
        var trigger = item.querySelector('[data-admin-nav-toggle]');
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

    document.querySelectorAll('.admin-panel-shell .sub-links-container a').forEach(function (link) {
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

    var searchInput = document.getElementById('adminGlobalSearch');
    if (searchInput) {
        document.addEventListener('keydown', function (e) {
            if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k') {
                e.preventDefault();
                searchInput.focus();
            }
        });
    }

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') {
            closeThemeDrawer();
            closeMobileSidebar();
        }
    });
})();
