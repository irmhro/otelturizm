(() => {
    const checkIn = document.getElementById('home-checkin');
    const checkOut = document.getElementById('home-checkout');
    if (!checkIn || !checkOut) return;

    const localToday = () => {
        const now = new Date();
        const pad = (value) => String(value).padStart(2, '0');
        return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`;
    };

    const today = checkIn.min && checkIn.min >= localToday() ? checkIn.min : localToday();

    const addDays = (isoDate, days) => {
        const date = new Date(`${isoDate}T12:00:00`);
        date.setDate(date.getDate() + days);
        return date.toISOString().slice(0, 10);
    };

    const syncDates = () => {
        checkIn.min = today;

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
    checkIn.addEventListener('input', syncDates);
    checkOut.addEventListener('change', () => {
        if (checkOut.value <= checkIn.value) {
            checkOut.value = addDays(checkIn.value, 1);
        }
    });
    checkOut.addEventListener('input', () => {
        if (checkOut.value <= checkIn.value) {
            checkOut.value = addDays(checkIn.value, 1);
        }
    });

    syncDates();
})();
