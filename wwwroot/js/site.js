// Auto-hide alerts
document.addEventListener('DOMContentLoaded', function() {
    // Auto-hide alerts after 5 seconds
    const alerts = document.querySelectorAll('.alert');
    alerts.forEach(function(alert) {
        setTimeout(function() {
            const bsAlert = new bootstrap.Alert(alert);
            if (bsAlert) {
                bsAlert.close();
            }
        }, 5000);
    });

    // Form enhancements
    enhanceForms();
    
    // Loading states for buttons
    enhanceButtons();
});

// Form enhancements
function enhanceForms() {
    const forms = document.querySelectorAll('form');
    forms.forEach(function(form) {
        form.addEventListener('submit', function(event) {
            const submitBtn = form.querySelector('button[type="submit"]');
            if (submitBtn && form.checkValidity()) {
                submitBtn.innerHTML = '<i class="bi bi-spinner-border spinner-border-sm me-2"></i>Processing...';
                submitBtn.disabled = true;
            }
        });
    });
}

// Button enhancements
function enhanceButtons() {
    const deleteButtons = document.querySelectorAll('[data-confirm-delete]');
    deleteButtons.forEach(function(btn) {
        btn.addEventListener('click', function(e) {
            if (!confirm('Are you sure you want to delete this item?')) {
                e.preventDefault();
            }
        });
    });
}

// File upload preview
function previewFile(input) {
    const file = input.files[0];
    const preview = document.getElementById('filePreview');
    
    if (file && preview) {
        const reader = new FileReader();
        reader.onload = function(e) {
            preview.innerHTML = `
                <div class="file-preview">
                    <i class="bi bi-file-earmark"></i>
                    <span>${file.name}</span>
                    <small>(${(file.size / 1024).toFixed(2)} KB)</small>
                </div>
            `;
        };
        reader.readAsDataURL(file);
    }
}

// Sidebar toggle for mobile
function toggleSidebar() {
    const sidebar = document.querySelector('.sidebar');
    sidebar.classList.toggle('show');
}

// Loading overlay
function showLoading() {
    const overlay = document.createElement('div');
    overlay.className = 'loading-overlay';
    overlay.innerHTML = `
        <div class="loading-spinner">
            <div class="spinner-border text-primary" role="status">
                <span class="visually-hidden">Loading...</span>
            </div>
        </div>
    `;
    document.body.appendChild(overlay);
}

function hideLoading() {
    const overlay = document.querySelector('.loading-overlay');
    if (overlay) {
        overlay.remove();
    }
}