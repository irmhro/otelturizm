// Mobil menü drawer davranışı (site header)
(function () {
    try {
        const header = document.getElementById('mainHeader');
        const overlay = document.getElementById('mobileMenuOverlay');
        const drawer = document.getElementById('mobileMenuDrawer');
        const toggle = document.getElementById('mobileMenuToggle');
        const closeBtn = document.getElementById('drawerClose');

        if (!header || !overlay || !drawer || !toggle || !closeBtn) return;

        window.addEventListener('scroll', function () {
            if (window.scrollY > 40) { header.classList.add('scrolled'); }
            else { header.classList.remove('scrolled'); }
        });

        function openMenu() {
            overlay.classList.add('active');
            drawer.classList.add('active');
            document.body.style.overflow = 'hidden';
        }

        function closeMenu() {
            overlay.classList.remove('active');
            drawer.classList.remove('active');
            document.body.style.overflow = '';
        }

        toggle.addEventListener('click', openMenu);
        closeBtn.addEventListener('click', closeMenu);
        overlay.addEventListener('click', closeMenu);

        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape' && drawer.classList.contains('active')) closeMenu();
        });
    } catch (e) { }
})();

