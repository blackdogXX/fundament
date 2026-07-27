window.applyTheme = function(theme, accent) {
    var html = document.documentElement;
    html.setAttribute('data-bs-theme', theme === 'dark' ? 'dark' : 'light');
    var accents = { blue: '#0d6efd', green: '#198754', orange: '#fd7e14', red: '#dc3545', gray: '#6c757d' };
    html.style.setProperty('--accent-color', accents[accent] || accents.blue);
    localStorage.setItem('fundament-theme', theme);
    localStorage.setItem('fundament-accent', accent);
};

window.restoreTheme = function() {
    var t = localStorage.getItem('fundament-theme') || 'light';
    var a = localStorage.getItem('fundament-accent') || 'blue';
    var accents = { blue: '#0d6efd', green: '#198754', orange: '#fd7e14', red: '#dc3545', gray: '#6c757d' };
    document.documentElement.setAttribute('data-bs-theme', t);
    document.documentElement.style.setProperty('--accent-color', accents[a] || accents.blue);
};

restoreTheme();

var observer = new MutationObserver(function(mutations) {
    mutations.forEach(function(mutation) {
        if (mutation.type === 'attributes' && (mutation.attributeName === 'data-bs-theme' || mutation.attributeName === 'style')) {
            var current = document.documentElement.getAttribute('data-bs-theme');
            var saved = localStorage.getItem('fundament-theme');
            var accentVar = document.documentElement.style.getPropertyValue('--accent-color');
            if ((current !== saved && saved) || !accentVar) {
                restoreTheme();
            }
        }
    });
});
observer.observe(document.documentElement, { attributes: true, attributeFilter: ['data-bs-theme', 'style'] });
