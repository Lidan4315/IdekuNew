// My Ideas JavaScript functionality
document.addEventListener('DOMContentLoaded', function() {
    const filterForm = document.getElementById('filterForm');
    const tableContainer = document.getElementById('ideas-table-container');
    const loadingSpinner = document.getElementById('table-loading-spinner');
    
    // Auto-submit form on filter changes
    const filterInputs = filterForm.querySelectorAll('input, select');
    filterInputs.forEach(input => {
        input.addEventListener('change', function() {
            submitFilter();
        });
        
        // For search input, add debounced keyup event
        if (input.type === 'text') {
            let debounceTimer;
            input.addEventListener('keyup', function() {
                clearTimeout(debounceTimer);
                debounceTimer = setTimeout(function() {
                    submitFilter();
                }, 300);
            });
        }
    });

    // Handle pagination clicks
    document.addEventListener('click', function(e) {
        if (e.target.matches('.pagination .page-link') || e.target.closest('.pagination .page-link')) {
            e.preventDefault();
            const pageLink = e.target.closest('.page-link');
            const page = pageLink.getAttribute('data-page');
            
            if (page && !pageLink.closest('.page-item').classList.contains('disabled')) {
                submitFilter(page);
            }
        }
    });

    function submitFilter(page = 1) {
        const formData = new FormData(filterForm);
        const params = new URLSearchParams();
        
        // Add form data to params
        for (let [key, value] of formData.entries()) {
            if (value) {
                params.append(key, value);
            }
        }
        
        // Add page parameter
        params.append('page', page);
        
        // Show loading spinner
        showLoading();
        
        // Make AJAX request
        fetch(`${filterForm.action}?${params.toString()}`, {
            method: 'GET',
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            }
        })
        .then(response => response.text())
        .then(html => {
            tableContainer.innerHTML = html;
            hideLoading();
            
            // Update URL without page reload
            const newUrl = `${window.location.pathname}?${params.toString()}`;
            window.history.replaceState(null, '', newUrl);
        })
        .catch(error => {
            console.error('Error:', error);
            hideLoading();
            showError('An error occurred while filtering ideas. Please try again.');
        });
    }

    function showLoading() {
        tableContainer.style.opacity = '0.5';
        loadingSpinner.style.display = 'block';
    }

    function hideLoading() {
        tableContainer.style.opacity = '1';
        loadingSpinner.style.display = 'none';
    }

    function showError(message) {
        const alertDiv = document.createElement('div');
        alertDiv.className = 'alert alert-danger alert-dismissible fade show';
        alertDiv.innerHTML = `
            <i class="bi bi-exclamation-triangle-fill me-2"></i>
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        `;
        
        const pageHeader = document.querySelector('.page-header');
        pageHeader.insertAdjacentElement('afterend', alertDiv);
        
        // Auto-remove after 5 seconds
        setTimeout(() => {
            if (alertDiv.parentNode) {
                alertDiv.remove();
            }
        }, 5000);
    }
});

// Global function for delete confirmation (used by the table)
function confirmDelete(ideaId, ideaTitle) {
    document.getElementById('ideaName').textContent = ideaTitle;
    document.getElementById('deleteForm').action = '/Idea/Delete/' + ideaId;
    new bootstrap.Modal(document.getElementById('deleteModal')).show();
}