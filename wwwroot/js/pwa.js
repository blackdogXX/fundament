(function () {
    var deferred = null;
    var BTN_ID = 'pwa-install-btn';

    function installed() {
        return window.matchMedia('(display-mode: standalone)').matches
            || window.navigator.standalone === true;
    }

    function browser() {
        var u = navigator.userAgent;
        if (/YaBrowser/i.test(u)) return 'yandex';
        if (/SamsungBrowser/i.test(u)) return 'samsung';
        if (/Firefox|FxiOS/i.test(u)) return 'firefox';
        if (/OPR|Opera/i.test(u)) return 'opera';
        if (/CriOS/i.test(u)) return 'chrome-ios';
        if (/Edg/i.test(u)) return 'edge';
        if (/Chrome/i.test(u)) return 'chrome';
        if (/Safari/i.test(u)) return 'safari';
        return 'other';
    }

    var HINTS = {
        yandex: 'Меню (три полоски внизу) → «Добавить на главный экран».',
        samsung: 'Меню (三 внизу справа) → «Добавить страницу» → «Главный экран».',
        firefox: 'Меню (⋮) → «Установить» или «Добавить на главный экран».',
        opera: 'Меню → «Добавить на» → «Главный экран».',
        'chrome-ios': 'Кнопка «Поделиться» → «На экран «Домой»».',
        safari: 'Кнопка «Поделиться» (квадрат со стрелкой) → «На экран «Домой»».',
        chrome: 'Меню (⋮) → «Установить приложение». Если пункта нет — обновите страницу и попробуйте снова.',
        edge: 'Меню (⋮) → «Приложения» → «Установить этот сайт как приложение».',
        other: 'Откройте меню браузера и выберите «Добавить на главный экран».'
    };

    function modal() {
        var old = document.getElementById('pwa-modal');
        if (old) old.remove();
        var d = document.createElement('div');
        d.id = 'pwa-modal';
        d.style.cssText = 'position:fixed;inset:0;background:rgba(0,0,0,.5);z-index:99999;' +
            'display:flex;align-items:center;justify-content:center;padding:1rem';
        d.innerHTML =
            '<div style="background:#fff;color:#212529;border-radius:14px;max-width:360px;width:100%;padding:1.5rem;' +
            'font-family:system-ui,-apple-system,sans-serif;box-shadow:0 10px 40px rgba(0,0,0,.3)">' +
            '<div style="width:56px;height:56px;border-radius:12px;margin:0 auto 1rem;' +
            'background:linear-gradient(135deg,#c1440e,#d4693a);color:#fff;font:900 30px Georgia,serif;' +
            'display:flex;align-items:center;justify-content:center">&#8381;</div>' +
            '<h5 style="text-align:center;margin:0 0 .75rem;font-weight:700">Установка приложения</h5>' +
            '<p style="color:#495057;line-height:1.5;margin:0 0 1.25rem;font-size:.95rem">' +
            HINTS[browser()] + '</p>' +
            '<button id="pwa-modal-close" style="width:100%;background:#c1440e;color:#fff;border:0;' +
            'border-radius:8px;padding:.7rem;font-size:1rem;cursor:pointer">Понятно</button></div>';
        document.body.appendChild(d);
        d.addEventListener('click', function (e) {
            if (e.target === d || e.target.id === 'pwa-modal-close') d.remove();
        });
    }

    function click() {
        if (deferred) {
            deferred.prompt();
            deferred.userChoice.then(function (c) {
                if (c.outcome === 'accepted') hide();
                deferred = null;
            });
        } else {
            modal();
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
            '<span>Установить приложение</span>';
        b.style.cssText = 'position:fixed;right:16px;bottom:16px;z-index:9998;' +
            'background:linear-gradient(135deg,#c1440e,#d4693a);color:#fff;border:0;border-radius:26px;' +
            'padding:11px 18px;font-size:14px;font-weight:600;cursor:pointer;display:flex;align-items:center;' +
            'gap:8px;box-shadow:0 4px 16px rgba(0,0,0,.25);font-family:system-ui,-apple-system,sans-serif';
        b.addEventListener('click', click);
        document.body.appendChild(b);
    }

    window.addEventListener('beforeinstallprompt', function (e) {
        e.preventDefault();
        deferred = e;
        show();
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
