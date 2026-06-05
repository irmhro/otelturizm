(() => {
    const checkIn = document.getElementById('home-checkin');
    const checkOut = document.getElementById('home-checkout');
    if (!checkIn || !checkOut) return;

    const today = checkIn.min || new Date().toISOString().slice(0, 10);

    const addDays = (isoDate, days) => {
        const date = new Date(`${isoDate}T12:00:00`);
        date.setDate(date.getDate() + days);
        return date.toISOString().slice(0, 10);
    };

    const syncDates = () => {
        if (!checkIn.value || checkIn.value < today) {
            checkIn.value = today;
        }

        const minCheckOut = addDays(checkIn.value, 1);
        checkOut.min = minCheckOut;

        if (!checkOut.value || checkOut.value <= checkIn.value) {
            checkOut.value = addDays(checkIn.value, 7);
        }
    };

    checkIn.addEventListener('change', syncDates);
    checkOut.addEventListener('change', () => {
        if (checkOut.value <= checkIn.value) {
            checkOut.value = addDays(checkIn.value, 1);
        }
    });

    syncDates();
})();
