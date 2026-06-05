(function () {
    'use strict';

    var body = document.body;
    if (!body.classList.contains('partnerpanel-v6-shell')) return;

    var sidebar = document.getElementById('appSidebar');
    var toggleBtn = document.getElementById('partnerSidebarToggle');
    var backdrop = document.getElementById('partnerSidebarBackdrop');
    var avatarBtn = document.getElementById('partnerAvatarBtn');
    var profileDropdown = document.getElementById('partnerProfileDropdown');
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

    document.querySelectorAll('.partnerpanel-v6-shell .nav-menu-item[data-nav-group]').forEach(function (item) {
        var trigger = item.querySelector('[data-partner-nav-toggle]');
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

    document.querySelectorAll('.partnerpanel-v6-shell .sub-links-container a').forEach(function (link) {
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

    var searchInput = document.getElementById('partnerGlobalSearch');
    if (searchInput) {
        document.addEventListener('keydown', function (e) {
            if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k') {
                e.preventDefault();
                searchInput.focus();
            }
        });
    }
})();

