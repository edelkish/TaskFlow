(function () {
    'use strict';
    window.taskflow = window.taskflow || {};

    window.taskflow.ayuda = {
        // Desplaza hasta una seccion de la pagina Ayuda.
        // No se puede usar href="#id" para esto: con <base href="/"> el ancla se resuelve
        // contra la raiz, Blazor navega a "/#id", el router matchea "/" y acaba en Login.
        // replaceState solo cambia el hash del historial, sin passar por el router.
        scrollTo: function (id) {
            if (!id) {
                return;
            }

            var el = document.getElementById(id);
            if (!el) {
                return;
            }

            el.scrollIntoView({ behavior: 'smooth', block: 'start' });

            if (window.history && window.history.replaceState) {
                window.history.replaceState(null, '', '#' + id);
            }
        }
    };
})();
