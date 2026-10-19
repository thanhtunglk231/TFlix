document.addEventListener('DOMContentLoaded', () => {
    const page = document.getElementById('videoUploadRequestPage');
    if (!page) return;

    const listUrl = page.dataset.listUrl;
    const reviewUrl = page.dataset.reviewUrl;
    const token = page.querySelector('input[name="__RequestVerificationToken"]')?.value;

    const searchInput = document.getElementById('requestSearch');
    const statusFilter = document.getElementById('requestStatusFilter');
    const btnReload = document.getElementById('btnReloadRequests');

    const loadingEl = document.getElementById('requestLoading');
    const errorEl = document.getElementById('requestError');
    const emptyEl = document.getElementById('requestEmpty');
    const tableWrap = document.getElementById('requestTableWrap');
    const rowsBody = document.getElementById('requestRows');

    const reviewModalEl = document.getElementById('reviewModal');
    if (reviewModalEl && reviewModalEl.parentNode !== document.body) {
        document.body.appendChild(reviewModalEl);
    }
    const reviewModal = reviewModalEl ? new bootstrap.Modal(reviewModalEl) : null;
    if (reviewModalEl) {
        reviewModalEl.addEventListener('hidden.bs.modal', () => {
            document.querySelectorAll('.modal-backdrop').forEach(el => el.remove());
            document.body.classList.remove('modal-open');
            document.body.style.removeProperty('overflow');
            document.body.style.removeProperty('padding-right');
        });
    }
    const reviewForm = document.getElementById('reviewForm');
    const reviewRequestId = document.getElementById('reviewRequestId');
    const reviewUserName = document.getElementById('reviewUserName');
    const reviewUserEmail = document.getElementById('reviewUserEmail');
    const reviewPlanName = document.getElementById('reviewPlanName');
    const reviewUserReason = document.getElementById('reviewUserReason');
    const reviewAdminNotes = document.getElementById('reviewAdminNotes');
    const reviewFormError = document.getElementById('reviewFormError');
    const btnSubmitReview = document.getElementById('btnSubmitReview');

    const toastEl = document.getElementById('requestToast');
    const toastMessage = document.getElementById('toastMessage');
    const toast = toastEl ? new bootstrap.Toast(toastEl, { delay: 3500 }) : null;

    let allRequests = [];

    function showToast(message, isSuccess = true) {
        if (!toast || !toastMessage) return;
        toastMessage.textContent = message;
        toastEl.className = `toast align-items-center text-light ${isSuccess ? 'bg-success' : 'bg-danger'} border-0`;
        toast.show();
    }

    async function loadRequests() {
        loadingEl.classList.remove('d-none');
        errorEl.classList.add('d-none');
        emptyEl.classList.add('d-none');
        tableWrap.classList.add('d-none');
        rowsBody.innerHTML = '';

        const status = statusFilter ? statusFilter.value : '';
        const url = status ? `${listUrl}?status=${encodeURIComponent(status)}` : listUrl;

        try {
            const res = await fetch(url, {
                headers: { 'Accept': 'application/json' }
            });

            if (!res.ok) {
                throw new Error(`HTTP error! status: ${res.status}`);
            }

            const result = await res.json();
            allRequests = result.data || [];
            applyFilterAndRender();

            // Cập nhật lại dấu đỏ trên sidebar
            const pendingCount = allRequests.filter(r => r.status === 'PENDING').length;
            const dot = document.getElementById('sidebarUploadRedDot');
            const badge = document.getElementById('sidebarUploadPendingBadge');
            const countText = document.getElementById('sidebarUploadPendingCount');
            if (pendingCount > 0) {
                if (dot) dot.classList.remove('d-none');
                if (badge) badge.classList.remove('d-none');
                if (countText) countText.textContent = pendingCount > 99 ? '99+' : pendingCount;
            } else {
                if (dot) dot.classList.add('d-none');
                if (badge) badge.classList.add('d-none');
            }
        } catch (err) {
            console.error('Lỗi tải danh sách yêu cầu upload:', err);
            errorEl.textContent = 'Không thể tải danh sách yêu cầu. Vui lòng thử lại sau.';
            errorEl.classList.remove('d-none');
        } finally {
            loadingEl.classList.add('d-none');
        }
    }

    function applyFilterAndRender() {
        const query = (searchInput?.value || '').trim().toLowerCase();
        const filtered = allRequests.filter(item => {
            if (!query) return true;
            const name = (item.fullName || '').toLowerCase();
            const email = (item.email || '').toLowerCase();
            const reason = (item.reason || '').toLowerCase();
            return name.includes(query) || email.includes(query) || reason.includes(query);
        });

        renderRows(filtered);
    }

    function formatDate(dateStr) {
        if (!dateStr) return '--';
        try {
            const d = new Date(dateStr);
            return d.toLocaleDateString('vi-VN', {
                day: '2-digit',
                month: '2-digit',
                year: 'numeric',
                hour: '2-digit',
                minute: '2-digit'
            });
        } catch {
            return dateStr;
        }
    }

    function renderRows(items) {
        if (!items || items.length === 0) {
            emptyEl.classList.remove('d-none');
            tableWrap.classList.add('d-none');
            rowsBody.innerHTML = '';
            return;
        }

        emptyEl.classList.add('d-none');
        tableWrap.classList.remove('d-none');

        rowsBody.innerHTML = items.map(item => {
            let statusBadge = '';
            if (item.status === 'APPROVED') {
                statusBadge = '<span class="badge bg-success-subtle text-success border border-success-subtle px-2 py-1"><i class="bi bi-check-circle me-1"></i>Đã duyệt</span>';
            } else if (item.status === 'REJECTED') {
                statusBadge = '<span class="badge bg-danger-subtle text-danger border border-danger-subtle px-2 py-1"><i class="bi bi-x-circle me-1"></i>Từ chối</span>';
            } else {
                statusBadge = '<span class="badge bg-warning-subtle text-warning border border-warning-subtle px-2 py-1"><i class="bi bi-hourglass-split me-1"></i>Chờ duyệt</span>';
            }

            const reasonDisplay = item.reason ? (item.reason.length > 50 ? `${item.reason.substring(0, 50)}...` : item.reason) : '<span class="text-secondary fst-italic">Không ghi</span>';
            const adminNotesDisplay = item.adminNotes ? `<div class="small text-secondary">${item.adminNotes}</div>` : '<span class="text-secondary">--</span>';
            const planDisplay = item.planName ? `<span class="badge bg-warning text-dark"><i class="bi bi-star-fill me-1"></i>${item.planName}</span>` : '<span class="badge bg-secondary">Chưa có gói</span>';

            return `
                <tr>
                    <td class="ps-3 fw-bold text-secondary">#${item.requestId}</td>
                    <td>
                        <div class="fw-medium text-light">${item.fullName || 'Người dùng #' + item.userId}</div>
                        <div class="small text-secondary">${item.email || ''}</div>
                    </td>
                    <td>${planDisplay}</td>
                    <td>
                        <div title="${item.reason || ''}" class="text-light" style="cursor:help;">
                            ${reasonDisplay}
                        </div>
                    </td>
                    <td class="small text-secondary">${formatDate(item.createdAt)}</td>
                    <td>${statusBadge}</td>
                    <td>${adminNotesDisplay}</td>
                    <td class="text-end pe-3">
                        <button type="button" class="btn btn-sm btn-outline-info rounded-pill px-3 js-open-review" data-id="${item.requestId}">
                            <i class="bi bi-pencil-square me-1"></i>Xét duyệt
                        </button>
                    </td>
                </tr>
            `;
        }).join('');

        // Gắn sự kiện mở modal review
        rowsBody.querySelectorAll('.js-open-review').forEach(btn => {
            btn.addEventListener('click', () => {
                const id = parseInt(btn.dataset.id, 10);
                const item = allRequests.find(r => r.requestId === id);
                if (!item) return;

                reviewRequestId.value = item.requestId;
                reviewUserName.textContent = item.fullName || 'Người dùng #' + item.userId;
                reviewUserEmail.textContent = item.email || '--';
                reviewPlanName.textContent = item.planName || 'Chưa có gói';
                reviewUserReason.textContent = item.reason || 'Không có lý do';
                reviewAdminNotes.value = item.adminNotes || '';
                reviewFormError.classList.add('d-none');

                if (item.status === 'REJECTED') {
                    document.getElementById('statusReject').checked = true;
                } else {
                    document.getElementById('statusApprove').checked = true;
                }

                reviewModal?.show();
            });
        });
    }

    // Xử lý submit xét duyệt
    if (reviewForm) {
        reviewForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            const id = parseInt(reviewRequestId.value, 10);
            const status = reviewForm.querySelector('input[name="reviewStatusRadio"]:checked')?.value || 'APPROVED';
            const adminNotes = reviewAdminNotes.value.trim();

            btnSubmitReview.disabled = true;
            btnSubmitReview.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Đang lưu...';
            reviewFormError.classList.add('d-none');

            try {
                const res = await fetch(reviewUrl, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': token || ''
                    },
                    body: JSON.stringify({
                        requestId: id,
                        status: status,
                        adminNotes: adminNotes
                    })
                });

                const data = await res.json();
                if (res.ok && data.success) {
                    reviewModal?.hide();
                    showToast(data.message || 'Cập nhật trạng thái thành công!', true);
                    loadRequests();
                } else {
                    reviewFormError.textContent = (data && data.message) ? data.message : 'Có lỗi xảy ra khi xét duyệt.';
                    reviewFormError.classList.remove('d-none');
                }
            } catch (err) {
                console.error('Lỗi khi gửi yêu cầu review:', err);
                reviewFormError.textContent = 'Không thể kết nối máy chủ.';
                reviewFormError.classList.remove('d-none');
            } finally {
                btnSubmitReview.disabled = false;
                btnSubmitReview.innerHTML = '<i class="bi bi-check-lg me-1"></i>Xác nhận xét duyệt';
            }
        });
    }

    // Filter events
    if (searchInput) {
        let debounceTimer;
        searchInput.addEventListener('input', () => {
            clearTimeout(debounceTimer);
            debounceTimer = setTimeout(applyFilterAndRender, 250);
        });
    }

    if (statusFilter) {
        statusFilter.addEventListener('change', loadRequests);
    }

    if (btnReload) {
        btnReload.addEventListener('click', loadRequests);
    }

    // Load ban đầu
    loadRequests();
});
