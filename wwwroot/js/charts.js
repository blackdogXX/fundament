
window.togglePassword = function(inputId, iconId, show) {
    var input = document.getElementById(inputId);
    var icon = document.getElementById(iconId);
    if (input) {
        input.type = show ? "text" : "password";
    }
    if (icon) {
        icon.className = show ? "bi bi-eye-slash" : "bi bi-eye";
    }
};

window.setTheme = function(theme, accent) {
    document.documentElement.setAttribute('data-theme', theme);
    document.documentElement.setAttribute('data-accent', accent);
    localStorage.setItem('theme', theme);
    localStorage.setItem('accent', accent);
};

// При загрузке страницы — применить сохранённую тему
(function() {
    var theme = localStorage.getItem('theme') || 'light';
    var accent = localStorage.getItem('accent') || 'brand';
    document.documentElement.setAttribute('data-theme', theme);
    document.documentElement.setAttribute('data-accent', accent);
})();

window.downloadCsv = function(filename, text) {
    var blob = new Blob(['\ufeff' + text], { type: 'text/csv;charset=utf-8;' });
    var link = document.createElement('a');
    link.href = URL.createObjectURL(blob);
    link.download = filename;
    link.click();
};
