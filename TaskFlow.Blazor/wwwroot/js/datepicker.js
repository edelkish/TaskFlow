(function () {
    'use strict';
    window.taskflow = window.taskflow || {};

    window.taskflow.datepicker = {
        init: function (element, dotNetHelper, initialValue) {
            if (!element || element._flatpickr) {
                return;
            }
            flatpickr(element, {
                dateFormat: 'Y-m-d',
                allowInput: true,
                locale: 'es',
                defaultDate: initialValue || null,
                onChange: function (selectedDates, dateStr) {
                    dotNetHelper.invokeMethodAsync('OnFlatpickrChange', dateStr);
                }
            });
        },
        destroy: function (element) {
            if (element && element._flatpickr) {
                element._flatpickr.destroy();
            }
        }
    };
})();
