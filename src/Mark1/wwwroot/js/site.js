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
    if (checkbox.classList.contains('transfer-target-check')) {
        if (checkbox.checked) {
            var otherId = checkbox.getAttribute('data-other-target');
            var other = otherId && document.getElementById(otherId);
            if (other) other.checked = false;
        }
        var form = checkbox.closest('form');
        var purposeGroup = form && form.querySelector('.savings-purpose-group');
        if (purposeGroup) {
            var anyChecked = form.querySelectorAll('.transfer-target-check:checked').length > 0;
            purposeGroup.style.display = anyChecked ? 'block' : 'none';
        }
    }
});

// Inline rename UI (e.g. Settings' category/account lists): swaps a row's display view for an
// editable form in place, no page reload until the form is actually submitted.
document.addEventListener('click', function (event) {
    var toggleBtn = event.target.closest('.toggle-rename');
    if (toggleBtn) {
        var item = toggleBtn.closest('.rename-item');
        if (!item) return;
        item.querySelector('.item-view').classList.add('d-none');
        var form = item.querySelector('.item-rename-form');
        form.classList.remove('d-none');
        var input = form.querySelector('input[type="text"]');
        if (input) { input.focus(); input.select(); }
        return;
    }

    var cancelBtn = event.target.closest('.cancel-rename');
    if (cancelBtn) {
        var item2 = cancelBtn.closest('.rename-item');
        if (!item2) return;
        item2.querySelector('.item-rename-form').classList.add('d-none');
        item2.querySelector('.item-view').classList.remove('d-none');
    }
});
