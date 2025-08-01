$(document).ready(function() {
    // Ambil data dari container
    const ideaFormContainer = $('#idea-form-data');
    if (ideaFormContainer.length === 0) {
        console.error('Idea form data container not found.');
        return;
    }

    const urls = {
        getDepartments: ideaFormContainer.data('url-get-departments'),
        getEmployee: ideaFormContainer.data('url-get-employee'),
        redirect: ideaFormContainer.data('url-redirect')
    };

    const editContext = {
        isEdit: ideaFormContainer.data('is-edit'),
        toDivision: ideaFormContainer.data('to-division'),
        toDepartment: ideaFormContainer.data('to-department'),
        badgeNumber: ideaFormContainer.data('badge-number')
    };

    // --- LOGIKA UNTUK LOOKUP BADGE & DROPDOWN DEPARTEMEN ---
    let badgeTimeout;
    $('#BadgeNumber').on('input', function() {
        clearTimeout(badgeTimeout);
        const badgeNumber = $(this).val().trim();
        if (badgeNumber.length >= 3) {
            badgeTimeout = setTimeout(() => fetchEmployeeData(badgeNumber), 500);
        } else {
            $('#employee-profile').slideUp();
        }
    });

    $('#divisionSelect').change(function() {
        const selectedDivision = $(this).val();
        const departmentSelect = $('#departmentSelect');
        departmentSelect.val('');
        if (selectedDivision) {
            $('#department-loading').show();
            departmentSelect.prop('disabled', true);
            $.ajax({
                url: urls.getDepartments,
                data: { divisionId: selectedDivision },
                success: function(response) {
                    departmentSelect.prop('disabled', false);
                    $('#department-loading').hide();
                    if (response.success) {
                        departmentSelect.html('<option value="">-- Select To Department --</option>');
                        $.each(response.data, (index, dept) => {
                            departmentSelect.append($('<option></option>').val(dept.id).text(dept.name));
                        });
                        if (editContext.isEdit && editContext.toDepartment) {
                            departmentSelect.val(editContext.toDepartment);
                        }
                    } else {
                        departmentSelect.html('<option value="">-- No departments found --</option>');
                    }
                },
                error: () => {
                    $('#department-loading').hide();
                    departmentSelect.prop('disabled', false);
                    departmentSelect.html('<option value="">-- Error loading --</option>');
                }
            });
        } else {
            departmentSelect.html('<option value="">-- Select To Division First --</option>');
            departmentSelect.prop('disabled', false);
        }
    });

    function fetchEmployeeData(badgeNumber) {
        $('#badge-loading').show();
        $.ajax({
            url: urls.getEmployee,
            data: { badgeNumber: badgeNumber },
            success: function(response) {
                $('#badge-loading').hide();
                if (response.success) {
                    $('#employee-name').val(response.data.name);
                    $('#employee-email').val(response.data.email);
                    $('#employee-position').val(response.data.position);
                    $('#employee-division').val(response.data.division);
                    $('#employee-department').val(response.data.department);
                    $('#employee-profile').slideDown();
                    $('#badge-error').hide();
                } else {
                    $('#badge-error').text(response.message || 'Employee not found').show();
                    $('#employee-profile').slideUp();
                }
            },
            error: () => {
                $('#badge-loading').hide();
                $('#badge-error').text('Error fetching employee data.').show();
                $('#employee-profile').slideUp();
            }
        });
    }

    // Inisialisasi untuk mode edit
    if (editContext.isEdit) {
        if (editContext.toDivision) {
            setTimeout(() => $('#divisionSelect').val(editContext.toDivision).trigger('change'), 100);
        }
        if (editContext.badgeNumber) {
            setTimeout(() => fetchEmployeeData(editContext.badgeNumber), 100);
        }
    }

    // --- LOGIKA SUBMIT FORM DENGAN AJAX ---
    $('form').on('submit', function(e) {
        e.preventDefault();

        const form = $(this);
        const submitBtn = $('#submit-btn');
        const originalBtnText = submitBtn.html();

        if (!form.valid()) {
            showAlert('warning', 'Please fill in all required fields correctly.');
            return;
        }

        submitBtn.html('<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Submitting...').prop('disabled', true);

        const formData = new FormData(this);

        fetch(form.attr('action'), {
            method: 'POST',
            body: formData,
            headers: {
                // Token sudah termasuk dalam FormData
            }
        })
        .then(response => {
            if (!response.ok) {
                return response.json().catch(() => response.text()).then(errorBody => {
                    throw new Error(`Server responded with ${response.status}. Body: ${JSON.stringify(errorBody)}`);
                });
            }
            return response.json();
        })
        .then(data => {
            if (data.success) {
                showAlert('success', data.message, data.redirectUrl || urls.redirect);
            } else {
                const errorMessage = data.errors ? data.message + '<br><br>' + data.errors.join('<br>') : data.message;
                showAlert('error', errorMessage);
                submitBtn.html(originalBtnText).prop('disabled', false);
            }
        })
        .catch(error => {
            console.error("Fetch Error:", error);
            showAlert('error', 'A network or script error occurred. Please check the console and try again.');
            submitBtn.html(originalBtnText).prop('disabled', false);
        });
    });

    function showAlert(type, message, redirectUrl = null) {
        const options = {
            icon: type,
            title: type === 'success' ? 'Success!' : 'Submission Failed',
            html: message
        };

        if (type === 'success') {
            options.timer = 2500;
            options.showConfirmButton = false;
            options.allowOutsideClick = false;
        }

        Swal.fire(options).then(() => {
            if (type === 'success' && redirectUrl) {
                window.location.href = redirectUrl;
            }
        });
    }
});
