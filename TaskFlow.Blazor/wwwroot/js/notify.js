(function () {
    'use strict';
    window.taskflow = window.taskflow || {};

    var CONTAINER_ID = 'tf-toast-container';

    var LABELS = {
        success: 'Éxito',
        error: 'Error',
        info: 'Información',
        warning: 'Advertencia'
    };

    var BADGES = {
        success: 'text-bg-success',
        error: 'text-bg-danger',
        info: 'text-bg-info',
        warning: 'text-bg-warning'
    };

    function ensureContainer() {
        var existing = document.getElementById(CONTAINER_ID);
        if (existing) {
            return existing;
        }
        var container = document.createElement('div');
        container.id = CONTAINER_ID;
        container.className = 'toast-container position-fixed top-0 end-0 p-3';
        container.style.zIndex = '1090';
        document.body.appendChild(container);
        return container;
    }

    function bootstrapToast(type, message) {
        if (typeof bootstrap === 'undefined' || !bootstrap.Toast) {
            window.alert(message);
            return;
        }

        var container = ensureContainer();
        var toast = document.createElement('div');
        toast.className = 'toast';
        toast.setAttribute('role', 'alert');

        var header = document.createElement('div');
        header.className = 'toast-header text-white ' + (BADGES[type] || 'text-bg-secondary');

        var title = document.createElement('strong');
        title.className = 'me-auto';
        title.textContent = LABELS[type] || 'Notificación';

        var closeBtn = document.createElement('button');
        closeBtn.type = 'button';
        closeBtn.className = 'btn-close btn-close-white';
        closeBtn.setAttribute('data-bs-dismiss', 'toast');
        closeBtn.setAttribute('aria-label', 'Cerrar');

        var body = document.createElement('div');
        body.className = 'toast-body';
        body.textContent = message;

        header.appendChild(title);
        header.appendChild(closeBtn);
        toast.appendChild(header);
        toast.appendChild(body);
        container.appendChild(toast);

        var instance = new bootstrap.Toast(toast, { autohide: true, delay: 4000 });
        toast.addEventListener('hidden.bs.toast', function () {
            toast.remove();
        });
        instance.show();
    }

    function show(type, message) {
        if (window.toastr && typeof window.toastr[type] === 'function') {
            window.toastr.options = Object.assign({}, window.toastr.options, {
                positionClass: 'toast-top-right',
                timeOut: 4000,
                progressBar: true,
                closeButton: true
            });
            window.toastr[type](message);
            return;
        }
        bootstrapToast(type, message);
    }

    window.taskflow.notify = {
        success: function (message) { show('success', message); },
        error: function (message) { show('error', message); },
        info: function (message) { show('info', message); },
        warning: function (message) { show('warning', message); }
    };
})();