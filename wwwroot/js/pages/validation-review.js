// File: wwwroot/js/pages/validation-review.js

// Fungsi untuk menampilkan notifikasi menggunakan SweetAlert
function showAlert(type, message, redirectUrl = null) {
    const options = {
        icon: type,
        title: type === 'success' ? 'Success!' : 'Oops...',
        text: message,
    };

    if (type === 'success') {
        options.timer = 2000;
        options.showConfirmButton = false;
    }

    Swal.fire(options).then(() => {
        if (type === 'success' && redirectUrl) {
            window.location.href = redirectUrl;
        }
    });
}

// Fungsi untuk menangani logika persetujuan (approve)
function approveIdea(approveUrl, ideaId, token, listUrl) {
    const comments = document.getElementById('approvalComments').value.trim();
    const validatedCostInput = document.getElementById('validatedSavingCost');
    const validatedCost = validatedCostInput.value;

    if (!comments) {
        showAlert('warning', 'Approval comments are required.');
        return;
    }

    if (!validatedCost) {
        showAlert('warning', 'Validated Saving Cost is required.');
        return;
    }

    if (parseFloat(validatedCost) <= 0) {
        showAlert('warning', 'Validated Saving Cost must be a positive number.');
        return;
    }

    Swal.fire({
        title: 'Are you sure?',
        text: "Do you really want to approve this idea?",
        icon: 'question',
        showCancelButton: true,
        confirmButtonColor: '#28a745',
        cancelButtonColor: '#6c757d',
        confirmButtonText: 'Yes, approve it!'
    }).then((result) => {
        if (result.isConfirmed) {
            fetch(`${approveUrl}/${ideaId}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                    'RequestVerificationToken': token
                },
                body: new URLSearchParams({
                    'comments': comments,
                    'validatedSavingCost': validatedCost
                })
            })
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    showAlert('success', data.message, listUrl);
                } else {
                    showAlert('danger', data.message);
                }
            })
            .catch(error => {
                console.error('Error approving idea:', error);
                showAlert('danger', 'An error occurred while processing your request.');
            });
        }
    });
}

// Fungsi untuk menangani logika penolakan (reject)
function rejectIdea(rejectUrl, ideaId, token, listUrl) {
    const reason = document.getElementById('rejectionReason').value.trim();
    
    if (!reason) {
        showAlert('warning', 'Please provide a rejection reason.');
        return;
    }
    
    Swal.fire({
        title: 'Are you sure?',
        text: "Do you really want to reject this idea? This action cannot be undone.",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#d33',
        cancelButtonColor: '#6c757d',
        confirmButtonText: 'Yes, reject it!'
    }).then((result) => {
        if (result.isConfirmed) {
            fetch(`${rejectUrl}/${ideaId}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                    'RequestVerificationToken': token
                },
                body: new URLSearchParams({
                    'rejectReason': reason
                })
            })
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    showAlert('success', data.message, listUrl);
                } else {
                    showAlert('danger', data.message);
                }
            })
            .catch(error => {
                console.error('Error rejecting idea:', error);
                showAlert('danger', 'An error occurred while processing your request.');
            });
        }
    });
}

// Event listener utama yang dijalankan setelah DOM dimuat
document.addEventListener('DOMContentLoaded', function () {
    // Ambil data dari elemen di view
    const validationDataElem = document.getElementById('validation-data-container');
    if (!validationDataElem) {
        console.error('Validation data container not found.');
        return;
    }

    const ideaId = validationDataElem.dataset.ideaId;
    const approveUrl = validationDataElem.dataset.approveUrl;
    const rejectUrl = validationDataElem.dataset.rejectUrl;
    const listUrl = validationDataElem.dataset.listUrl;
    const viewAttachmentUrlBase = validationDataElem.dataset.viewAttachmentUrl;
    const downloadAttachmentUrlBase = validationDataElem.dataset.downloadAttachmentUrl;
    const downloadAllUrl = validationDataElem.dataset.downloadAllUrl;

    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

    // Setup event listeners untuk tombol
    const approveBtn = document.getElementById('approve-idea-btn');
    const rejectBtn = document.getElementById('reject-idea-btn');

    if (approveBtn) {
        approveBtn.addEventListener('click', () => approveIdea(approveUrl, ideaId, token, listUrl));
    }
    if (rejectBtn) {
        rejectBtn.addEventListener('click', () => rejectIdea(rejectUrl, ideaId, token, listUrl));
    }

    // Logika untuk modal attachment viewer
    const attachmentModal = new bootstrap.Modal(document.getElementById('attachmentViewerModal'));
    const modalBody = document.getElementById('attachmentViewerBody');
    const modalTitle = document.getElementById('attachmentViewerModalLabel');

    document.querySelectorAll('.view-attachment-btn').forEach(button => {
        button.addEventListener('click', function () {
            const filename = this.dataset.filename;
            const extension = filename.split('.').pop().toLowerCase();
            
            // Buat URL dengan benar
            const viewUrl = `${viewAttachmentUrlBase}&filename=${encodeURIComponent(filename)}`;

            modalTitle.textContent = `Preview: ${filename}`;
            modalBody.innerHTML = ''; // Clear previous content

            if (['png', 'jpg', 'jpeg', 'gif', 'pdf'].includes(extension)) {
                const iframe = document.createElement('iframe');
                iframe.src = viewUrl;
                iframe.style.width = '100%';
                iframe.style.height = '100%';
                iframe.style.border = 'none';
                modalBody.appendChild(iframe);
            } else {
                const downloadUrl = `${downloadAttachmentUrlBase}&filename=${encodeURIComponent(filename)}`;
                modalBody.innerHTML = `
                    <div class="text-center p-5">
                        <p>Preview for this file type is not available.</p>
                        <a href="${downloadUrl}" class="btn btn-primary">
                            <i class="bi bi-download me-2"></i>Download "${filename}"
                        </a>
                    </div>
                `;
            }
            
            attachmentModal.show();
        });
    });
});
