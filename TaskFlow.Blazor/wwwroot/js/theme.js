window.applyTaskFlowTheme = function (settings) {
    if (!settings) {
        return;
    }

    var root = document.documentElement;

    if (settings.colorMode) {
        root.setAttribute('data-bs-theme', settings.colorMode);
    }

    if (settings.primary) {
        root.style.setProperty('--bs-primary', settings.primary);
        var rgb = hexToRgb(settings.primary);
        if (rgb) {
            root.style.setProperty('--bs-primary-rgb', rgb);
        }
    }

    applyBgClass(document.querySelector('.app-sidebar'), settings.sidebarBg);
    applyBgClass(document.querySelector('.app-header'), settings.headerBg);
    applyBgClass(document.querySelector('.app-footer'), settings.footerBg);
};

function applyBgClass(el, cssClass) {
    if (!el || !cssClass) {
        return;
    }

    var toRemove = [];
    el.classList.forEach(function (c) {
        if (c.indexOf('bg-') === 0) {
            toRemove.push(c);
        }
    });
    toRemove.forEach(function (c) {
        el.classList.remove(c);
    });

    el.classList.add(cssClass);
}

function hexToRgb(hex) {
    var m = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(hex || '');
    if (!m) {
        return null;
    }
    return parseInt(m[1], 16) + ', ' + parseInt(m[2], 16) + ', ' + parseInt(m[3], 16);
}