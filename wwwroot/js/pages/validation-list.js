document.addEventListener('DOMContentLoaded', function () {
    const filterInputs = document.querySelectorAll('#searchString, #selectedDivision, #selectedDepartment, #selectedStatus, #selectedStage');
    const tableBody = document.getElementById('idea-table-body');
    const spinner = document.getElementById('table-loading-spinner');
    const tableContainer = document.getElementById('idea-table-container');
    const paginationContainer = document.getElementById('pagination-container');
    let debounceTimeout;

    // --- REAL-TIME FILTERING LOGIC ---
    document.getElementById('searchString').addEventListener('input', function () {
        clearTimeout(debounceTimeout);
        debounceTimeout = setTimeout(() => applyFilters(1), 300);
    });

    // Generic handler for dropdowns that DON'T affect others
    document.querySelectorAll('#selectedDepartment, #selectedStatus, #selectedStage').forEach(select => {
        select.addEventListener('change', function () {
            applyFilters(1);
        });
    });

    function applyFilters(pageNumber = 1) {
        const searchString = document.getElementById('searchString').value;
        const selectedDivision = document.getElementById('selectedDivision').value;
        const selectedDepartment = document.getElementById('selectedDepartment').value;
        const selectedStatus = document.getElementById('selectedStatus').value;
        const selectedStage = document.getElementById('selectedStage').value;

        const query = new URLSearchParams({
            searchString,
            selectedDivision,
            selectedDepartment,
            selectedStatus,
            selectedStage,
            pageNumber
        }).toString();

        spinner.style.display = 'block';
        tableContainer.style.display = 'none';
        paginationContainer.style.display = 'none';

        fetch(`/Validation/FilterIdeas?${query}`)
            .then(response => response.json())
            .then(data => {
                tableBody.innerHTML = data.tableHtml;
                paginationContainer.innerHTML = data.paginationHtml;
                
                spinner.style.display = 'none';
                tableContainer.style.display = 'block';
                paginationContainer.style.display = 'flex';
                
                // No need to re-attach listeners with event delegation
            })
            .catch(error => {
                console.error('Error fetching filtered ideas:', error);
                spinner.style.display = 'none';
                tableContainer.style.display = 'block';
                paginationContainer.style.display = 'flex';
                tableBody.innerHTML = '<tr><td colspan="12" class="text-center text-danger">Error loading data.</td></tr>';
            });
    }

    // --- PAGINATION CLICK HANDLING ---
    if (paginationContainer) {
        paginationContainer.addEventListener('click', function (e) {
            const pageLink = e.target.closest('a.page-link');
            if (pageLink) {
                e.preventDefault();
                const page = pageLink.getAttribute('data-page');
                if (page) {
                    applyFilters(page);
                }
            }
        });
    }

    // --- CASCADING DROPDOWNS LOGIC ---
    const divisionSelect = document.getElementById('selectedDivision');
    const departmentSelect = document.getElementById('selectedDepartment');

    if (divisionSelect && departmentSelect) {
        divisionSelect.addEventListener('change', function () {
            const divisionId = this.value;

            // Always reset the department dropdown when the division changes.
            departmentSelect.innerHTML = '<option value="">All</option>';
            departmentSelect.value = ''; // Ensure value is cleared for the filter function

            if (!divisionId) {
                // If "All Divisions" is selected, just trigger the filter.
                applyFilters(1);
                return;
            }

            // If a specific division is selected, fetch its departments.
            departmentSelect.innerHTML = '<option value="">Loading...</option>';
            departmentSelect.disabled = true;

            fetch(`/Idea/GetDepartmentsByDivision?divisionId=${divisionId}`)
                .then(response => response.json())
                .then(data => {
                    departmentSelect.innerHTML = '<option value="">All</option>'; // Reset again after loading
                    if (data.success) {
                        data.data.forEach(dept => {
                            const option = document.createElement('option');
                            option.value = dept.id;
                            option.textContent = dept.name;
                            departmentSelect.appendChild(option);
                        });
                    }
                })
                .catch(error => {
                    console.error('Error fetching departments:', error);
                    departmentSelect.innerHTML = '<option value="">Error</option>';
                })
                .finally(() => {
                    departmentSelect.disabled = false;
                    // Trigger a filter update now that the departments are loaded.
                    applyFilters(1);
                });
        });
    }

    // --- DELETE BUTTON LOGIC (using Event Delegation) ---
    tableBody.addEventListener('click', function(e) {
        const deleteButton = e.target.closest('.delete-idea-btn');
        if (!deleteButton) {
            return; // Click was not on a delete button
        }

        e.preventDefault();
        e.stopPropagation();

        const ideaId = deleteButton.getAttribute('data-id');
        const row = deleteButton.closest('tr');
        const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');

        if (!tokenInput) {
            console.error('Request Verification Token not found in the form.');
            Swal.fire('Error!', 'Security token not found. Please refresh the page.', 'error');
            return;
        }
        const token = tokenInput.value;

        Swal.fire({
            title: 'Are you sure?',
            text: "You won't be able to revert this!",
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#d33',
            cancelButtonColor: '#3085d6',
            confirmButtonText: 'Yes, delete it!'
        }).then((result) => {
            if (result.isConfirmed) {
                fetch(`/Validation/Delete/${ideaId}`, {
                    method: 'POST',
                    headers: {
                        'RequestVerificationToken': token,
                        'Content-Type': 'application/json'
                    }
                })
                .then(response => response.json())
                .then(data => {
                    if (data.success) {
                        Swal.fire('Deleted!', data.message, 'success');
                        row.remove();
                    } else {
                        Swal.fire('Failed!', data.message, 'error');
                    }
                })
                .catch(error => {
                    console.error('Error:', error);
                    Swal.fire('Error!', 'An error occurred while deleting the idea.', 'error');
                });
            }
        });
    });
});
