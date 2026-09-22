// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

document.addEventListener('change', function (event) {
    var checkbox = event.target;

    // Inline list checkboxes (e.g. Paid) that save instantly via fetch instead of a full page
    // reload - reverts itself and warns if the save actually fails, so what's shown always
    // matches what's saved.
    var ajaxForm = checkbox.closest('.ajax-toggle-form');
    if (ajaxForm) {
        var previousChecked = !checkbox.checked;
        checkbox.disabled = true;
        fetch(ajaxForm.action, { method: 'POST', body: new FormData(ajaxForm) })
            .then(function (response) {
                if (!response.ok) {
                    checkbox.checked = previousChecked;
                    alert('Could not save that change. Please try again.');
                }
            })
            .catch(function () {
                checkbox.checked = previousChecked;
                alert('Could not save that change. Please check your connection and try again.');
            })
            .finally(function () {
                checkbox.disabled = false;
            });
        return;
    }

    // Mutually exclusive "Transfer to Savings" / "Transfer to Retained" checkboxes on the Expense form.
    if (checkbox.classList.contains('transfer-target-check') && checkbox.checked) {
        var otherId = checkbox.getAttribute('data-other-target');
        var other = otherId && document.getElementById(otherId);
        if (other) other.checked = false;
    }
});
