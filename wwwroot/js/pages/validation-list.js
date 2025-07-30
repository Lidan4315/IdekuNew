document.addEventListener('DOMContentLoaded', function () {
    const deleteButtons = document.querySelectorAll('.delete-idea-btn');
    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');

    if (!tokenInput) {
        console.error('Request Verification Token not found in the form.');
        // Mungkin tampilkan notifikasi ke pengguna di sini jika diperlukan
        return;
    }
    const token = tokenInput.value;

    deleteButtons.forEach(button => {
        button.addEventListener('click', function (e) {
            // Mencegah event lain (seperti navigasi baris) dari terpicu
            e.preventDefault();
            e.stopPropagation();

            const ideaId = this.getAttribute('data-id');
            const row = this.closest('tr');

            Swal.fire({
                title: 'Apakah Anda yakin?',
                text: "Anda tidak akan dapat mengembalikan tindakan ini!",
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#d33',
                cancelButtonColor: '#3085d6',
                confirmButtonText: 'Ya, hapus!',
                cancelButtonText: 'Batal'
            }).then((result) => {
                if (result.isConfirmed) {
                    // Kirim permintaan hapus ke server
                    fetch(`/Validation/Delete/${ideaId}`, {
                        method: 'POST',
                        headers: {
                            'RequestVerificationToken': token,
                            'Content-Type': 'application/json'
                        }
                    })
                    .then(response => {
                        if (!response.ok) {
                            // Jika respons tidak OK, coba baca sebagai JSON untuk pesan error
                            return response.json().then(err => { throw err; });
                        }
                        return response.json();
                    })
                    .then(data => {
                        if (data.success) {
                            Swal.fire(
                                'Dihapus!',
                                data.message,
                                'success'
                            );
                            // Hapus baris dari tabel setelah berhasil
                            row.remove();
                        } else {
                            // Tampilkan pesan error dari server
                            Swal.fire(
                                'Gagal!',
                                data.message,
                                'error'
                            );
                        }
                    })
                    .catch(error => {
                        console.error('Error:', error);
                        // Tampilkan pesan error umum jika fetch gagal atau ada masalah lain
                        const errorMessage = error.message || 'Terjadi kesalahan saat mencoba menghapus ide.';
                        Swal.fire(
                            'Error!',
                            errorMessage,
                            'error'
                        );
                    });
                }
            });
        });
    });
});
