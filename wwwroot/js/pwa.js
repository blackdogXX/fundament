(function () {
    var deferred = null;
    var BTN_ID = 'pwa-install-btn';
    var APK_URL = '/fundament.apk';

    function installed() {
        return window.matchMedia('(display-mode: standalone)').matches
            || window.navigator.standalone === true
            || document.referrer.startsWith('android-app://');
    }

    function isAndroid() {
        return /Android/i.test(navigator.userAgent);
    }

    function isIOS() {
        return /iPhone|iPad|iPod/i.test(navigator.userAgent);
    }

    function modal(html) {
        var old = document.getElementById('pwa-modal');
        if (old) old.remove();
        var d = document.createElement('div');
        d.id = 'pwa-modal';
        d.style.cssText = 'position:fixed;inset:0;background:rgba(0,0,0,.5);z-index:99999;' +
            'display:flex;align-items:center;justify-content:center;padding:1rem';
        d.innerHTML =
            '<div style="background:#fff;color:#212529;border-radius:14px;max-width:360px;width:100%;padding:1.5rem;' +
            'font-family:Inter,system-ui,-apple-system,sans-serif;box-shadow:0 10px 40px rgba(0,0,0,.3)">' + html + '</div>';
        document.body.appendChild(d);
        d.addEventListener('click', function (e) {
            if (e.target === d || e.target.id === 'pwa-modal-close') d.remove();
        });
    }

    function androidModal() {
        modal(
            '<div style="width:56px;height:56px;border-radius:12px;margin:0 auto 1rem;' +
            'background:linear-gradient(135deg,#c1440e,#d4693a);display:flex;align-items:center;justify-content:center">' +
            '<span style="color:#fff;font-size:28px">&#8681;</span></div>' +
            '<h5 style="text-align:center;margin:0 0 .75rem;font-weight:700">Установить Фундамент</h5>' +
            '<p style="color:#495057;line-height:1.5;margin:0 0 1rem;font-size:.93rem">' +
            'Скачайте установочный файл. При установке Android спросит разрешение — ' +
            'нажмите «Настройки» и разрешите установку из этого источника.</p>' +
            '<a href="' + APK_URL + '" download style="display:block;text-align:center;text-decoration:none;' +
            'background:#c1440e;color:#fff;border-radius:8px;padding:.8rem;font-size:1rem;font-weight:600;margin-bottom:.5rem">' +
            'Скачать приложение</a>' +
            '<button id="pwa-modal-close" style="width:100%;background:transparent;color:#6c757d;border:0;' +
            'padding:.6rem;font-size:.92rem;cursor:pointer">Позже</button>'
        );
    }

    function iosModal() {
        modal(
            '<h5 style="text-align:center;margin:0 0 .75rem;font-weight:700">Добавить на экран «Домой»</h5>' +
            '<p style="color:#495057;line-height:1.5;margin:0 0 1.25rem;font-size:.93rem">' +
            'Нажмите кнопку «Поделиться» внизу браузера, затем выберите «На экран «Домой»».</p>' +
            '<button id="pwa-modal-close" style="width:100%;background:#c1440e;color:#fff;border:0;' +
            'border-radius:8px;padding:.7rem;font-size:1rem;cursor:pointer">Понятно</button>'
        );
    }

    function click() {
        if (isAndroid()) { androidModal(); return; }
        if (isIOS()) { iosModal(); return; }
        if (deferred) {
            deferred.prompt();
            deferred.userChoice.then(function (c) {
                if (c.outcome === 'accepted') hide();
                deferred = null;
            });
        } else {
            modal(
                '<h5 style="text-align:center;margin:0 0 .75rem;font-weight:700">Установка приложения</h5>' +
                '<p style="color:#495057;line-height:1.5;margin:0 0 1.25rem;font-size:.93rem">' +
                'Откройте меню браузера и выберите «Установить приложение». ' +
                'Для телефона на Android откройте этот сайт с телефона — там доступна установка отдельным файлом.</p>' +
                '<button id="pwa-modal-close" style="width:100%;background:#c1440e;color:#fff;border:0;' +
                'border-radius:8px;padding:.7rem;font-size:1rem;cursor:pointer">Понятно</button>'
            );
        }
    }

    function hide() {
        var b = document.getElementById(BTN_ID);
        if (b) b.remove();
    }

    function show() {
        if (installed() || document.getElementById(BTN_ID)) return;
        var b = document.createElement('button');
        b.id = BTN_ID;
        b.type = 'button';
        b.innerHTML = '<span style="font-size:17px;line-height:1">&#11015;</span>' +
            '<span>' + (isAndroid() ? 'Скачать приложение' : 'Установить приложение') + '</span>';
        b.style.cssText = 'position:fixed;right:16px;bottom:16px;z-index:9998;' +
            'background:linear-gradient(135deg,#c1440e,#d4693a);color:#fff;border:0;border-radius:26px;' +
            'padding:11px 18px;font-size:14px;font-weight:600;cursor:pointer;display:flex;align-items:center;' +
            'gap:8px;box-shadow:0 4px 16px rgba(0,0,0,.25);font-family:Inter,system-ui,-apple-system,sans-serif';
        b.addEventListener('click', click);
        document.body.appendChild(b);
    }

    window.addEventListener('beforeinstallprompt', function (e) {
        e.preventDefault();
        deferred = e;
    });

    window.addEventListener('appinstalled', hide);

    function init() {
        if (installed()) return;
        show();
        setInterval(function () {
            if (installed()) hide(); else show();
        }, 2000);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
