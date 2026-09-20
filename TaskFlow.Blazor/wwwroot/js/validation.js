(function () {
    'use strict';
    window.taskflow = window.taskflow || {};

    window.taskflow.validation = {
        validateForm: function (form) {
            if (!form) {
                return true;
            }
            if (form.checkValidity()) {
                return true;
            }
            form.classList.add('was-validated');
            return false;
        }
    };
})();