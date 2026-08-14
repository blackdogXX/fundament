window.bnSheet = function (open) {
    var s = document.getElementById('bnSheet');
    var b = document.getElementById('bnBackdrop');
    if (!s || !b) return;
    if (open) {
        s.classList.add('bn-open');
        b.classList.add('bn-open');
        document.body.style.overflow = 'hidden';
    } else {
        s.classList.remove('bn-open');
        b.classList.remove('bn-open');
        document.body.style.overflow = '';
    }
};
document.addEventListener('click', function (e) {
    var a = e.target.closest('.bn-sheet-item');
    if (a) window.bnSheet(false);
});
