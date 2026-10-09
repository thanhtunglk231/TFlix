(() => {
    'use strict';

    const config = window.adminAccountConfig;
    if (!config) return;

    // DOM elements
    const el = {
        form: document.getElementById('accountForm'),
        modal: document.getElementById('accountModal'),
        modalTitle: document.getElementById('accountModalTitle'),
        userId: document.getElementById('userId'),
        fullName: document.getElementById('fullName'),
        email: document.getElementById('email'),
        emailHelpText: document.getElementById('emailHelpText'),
        statusGroup: document.getElementById('statusGroup'),
        status: document.getElementById('accountStatus'),
        password: document.getElementById('password'),
        passwordHelp: document.getElementById('passwordHelpText'),
        passwordStar: document.getElementById('passwordRequiredStar'),
        roleOptions: document.getElementById('roleOptions'),
        roleError: document.getElementById('roleError'),
        submitBtn: document.getElementById('btnSubmitAccount'),
        submitLabel: document.querySelector('#btnSubmitAccount .submit-label'),
        spinner: document.querySelector('#btnSubmitAccount .spinner-border'),
        body: document.getElementById('accountTableBody'),
        table: document.getElementById('accountTableWrap'),
        loading: document.getElementById('accountLoading'),
        empty: document.getElementById('accountEmpty'),
        alert: document.getElementById('accountAlert'),
        btnOpenCreate: document.getElementById('btnOpenCreate'),
        btnReload: document.getElementById('btnReloadAccounts'),
        // Delete Modal Elements
        deleteModal: document.getElementById('deleteAccountModal'),
        deleteAccountName: document.getElementById('deleteAccountName'),
        btnConfirmDelete: document.getElementById('btnConfirmDelete'),
        deleteLabel: document.querySelector('#btnConfirmDelete .delete-label'),
        deleteSpinner: document.querySelector('#btnConfirmDelete .spinner-border')
    };

    let bsAccountModal = null;
    let bsDeleteModal = null;
    let cachedAccounts = [];
    let cachedRoles = [];
    let pendingDeleteUserId = null;

    // Helper functions
    const val = (item, ...names) => {
        for (const name of names) {
            if (item && item[name] !== undefined && item[name] !== null) {
                return item[name];
            }
        }
        return undefined;
    };

    const html = text => String(text ?? '').replace(/[&<>'"]/g, c => ({
        '&': '&amp;', '<': '&lt;', '>': '&gt;', "'": '&#39;', '"': '&quot;'
    })[c]);

    function showAlert(message, type = 'danger') {
        if (!el.alert) return;
        el.alert.className = `alert alert-${type} alert-dismissible fade show`;
        el.alert.innerHTML = `
            <span>${html(message)}</span>
            <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Đóng"></button>
        `;
        el.alert.classList.remove('d-none');
        el.alert.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
    }

    function hideAlert() {
        if (el.alert) {
            el.alert.classList.add('d-none');
            el.alert.innerHTML = '';
        }
    }

    function parsePayload(payload) {
        let data = payload?.data ?? payload?.Data ?? payload;
        if (typeof data === 'string') {
            try { data = JSON.parse(data); } catch (e) { console.error('Parse error:', e); }
        }

        let accounts = val(data, 'Table', 'table', 'accounts', 'Accounts');
        let roles = val(data, 'Table1', 'table1', 'roles', 'Roles');

        if (!Array.isArray(accounts)) {
            if (Array.isArray(data)) {
                accounts = data;
            } else if (Array.isArray(data?.tables?.[0])) {
                accounts = data.tables[0];
                roles = data.tables[1] || [];
            } else {
                accounts = [];
            }
        }

        if (!Array.isArray(roles)) {
            roles = [];
        }

        return { accounts, roles };
    }

    function renderStatusBadge(status) {
        const s = String(status || '').toUpperCase();
        if (s === 'ACTIVE') {
            return '<span class="badge bg-success-subtle text-success border border-success-subtle px-2 py-1"><i class="bi bi-check-circle me-1"></i>Hoạt động</span>';
        }
        if (s === 'INACTIVE') {
            return '<span class="badge bg-warning-subtle text-warning-emphasis border border-warning-subtle px-2 py-1"><i class="bi bi-pause-circle me-1"></i>Tạm dừng</span>';
        }
        if (s === 'LOCKED') {
            return '<span class="badge bg-danger-subtle text-danger border border-danger-subtle px-2 py-1"><i class="bi bi-lock me-1"></i>Đã khóa</span>';
        }
        return `<span class="badge bg-secondary-subtle text-secondary px-2 py-1">${html(status || 'N/A')}</span>`;
    }

    function renderRolesBadge(rolesStr) {
        if (!rolesStr || !rolesStr.trim()) {
            return '<span class="text-muted small fst-italic">Chưa gán vai trò</span>';
        }
        const roles = rolesStr.split(',').map(r => r.trim()).filter(Boolean);
        return roles.map(role => {
            const isAdm = role.toUpperCase().includes('ADMIN');
            const bgClass = isAdm ? 'bg-primary-subtle text-primary border border-primary-subtle' : 'bg-info-subtle text-info-emphasis border border-info-subtle';
            return `<span class="badge ${bgClass} me-1 mb-1">${html(role)}</span>`;
        }).join('');
    }

    function renderAccounts(items) {
        if (!el.body) return;
        cachedAccounts = items || [];

        if (cachedAccounts.length === 0) {
            el.body.innerHTML = '';
            el.empty.classList.remove('d-none');
            el.table.classList.add('d-none');
            return;
        }

        el.empty.classList.add('d-none');
        el.table.classList.remove('d-none');

        el.body.innerHTML = cachedAccounts.map((item, index) => {
            const userId = val(item, 'UserId', 'userId', 'USER_ID', 'id');
            const fullName = val(item, 'FullName', 'fullName', 'FULL_NAME') || '—';
            const email = val(item, 'Email', 'email', 'EMAIL') || '—';
            const status = val(item, 'Status', 'status', 'STATUS') || 'ACTIVE';
            const roles = val(item, 'Roles', 'roles', 'ROLES') || '';
            const createdAt = val(item, 'CreatedAt', 'createdAt', 'CREATED_AT');

            const formattedDate = createdAt
                ? new Date(createdAt).toLocaleString('vi-VN', {
                    day: '2-digit', month: '2-digit', year: 'numeric',
                    hour: '2-digit', minute: '2-digit'
                })
                : '—';

            return `
                <tr>
                    <td>
                        <div class="fw-semibold text-white">${html(fullName)}</div>
                        <div class="text-muted small">ID: #${html(userId)}</div>
                    </td>
                    <td>
                        <span class="text-slate-300">${html(email)}</span>
                    </td>
                    <td>
                        ${renderRolesBadge(roles)}
                    </td>
                    <td>
                        ${renderStatusBadge(status)}
                    </td>
                    <td>
                        <span class="text-muted small">${html(formattedDate)}</span>
                    </td>
                    <td class="text-end pe-3">
                        <div class="btn-group btn-group-sm" role="group">
                            <button type="button" class="btn btn-outline-primary btn-edit-account" data-index="${index}" title="Chỉnh sửa tài khoản">
                                <i class="bi bi-pencil-square me-1"></i>Sửa
                            </button>
                            <button type="button" class="btn btn-outline-danger btn-delete-account" data-index="${index}" title="Xóa tài khoản">
                                <i class="bi bi-trash3 me-1"></i>Xóa
                            </button>
                        </div>
                    </td>
                </tr>
            `;
        }).join('');

        // Attach event listeners to Edit buttons
        el.body.querySelectorAll('.btn-edit-account').forEach(btn => {
            btn.addEventListener('click', () => {
                const idx = parseInt(btn.getAttribute('data-index'), 10);
                const account = cachedAccounts[idx];
                if (account) openEditModal(account);
            });
        });

        // Attach event listeners to Delete buttons
        el.body.querySelectorAll('.btn-delete-account').forEach(btn => {
            btn.addEventListener('click', () => {
                const idx = parseInt(btn.getAttribute('data-index'), 10);
                const account = cachedAccounts[idx];
                if (account) openDeleteModal(account);
            });
        });
    }

    function renderRoles(items) {
        if (!el.roleOptions) return;
        cachedRoles = items || [];

        if (cachedRoles.length === 0) {
            el.roleOptions.innerHTML = '<div class="text-warning small fst-italic">Không tìm thấy danh sách vai trò hệ thống.</div>';
            return;
        }

        el.roleOptions.innerHTML = cachedRoles.map(item => {
            const id = val(item, 'RoleId', 'roleId', 'ROLE_ID', 'id');
            const code = val(item, 'RoleCode', 'roleCode', 'ROLE_CODE', 'code') || '';
            const name = val(item, 'RoleName', 'roleName', 'ROLE_NAME', 'name') || code;

            return `
                <label class="form-check border rounded p-2 ps-5 d-flex align-items-center mb-1 role-checkbox-label" style="cursor: pointer;">
                    <input class="form-check-input role-checkbox me-2" type="checkbox" value="${html(id)}" data-code="${html(code)}" style="margin-left: -2.5em;">
                    <div>
                        <span class="fw-semibold text-white">${html(code)}</span>
                        ${name !== code ? `<span class="text-muted small ms-1">(${html(name)})</span>` : ''}
                    </div>
                </label>
            `;
        }).join('');
    }

    function openCreateModal() {
        if (!el.form) return;
        el.form.reset();
        el.form.classList.remove('was-validated');

        el.userId.value = '';
        el.modalTitle.innerHTML = '<i class="bi bi-person-plus me-2"></i>Tạo tài khoản quản trị';
        el.submitLabel.textContent = 'Tạo tài khoản';

        // Email & Name editable - không có màu nền trắng chói
        el.email.readOnly = false;
        el.email.classList.remove('bg-light');
        if (el.emailHelpText) el.emailHelpText.textContent = '';

        // Status group hidden (default ACTIVE)
        el.statusGroup.classList.add('d-none');
        el.status.value = 'ACTIVE';

        // Password is required for create
        el.password.value = '';
        el.password.required = true;
        el.password.minLength = 8;
        el.passwordStar.classList.remove('d-none');
        el.passwordHelp.textContent = 'Tối thiểu 8 ký tự.';

        // Uncheck all roles
        el.roleOptions.querySelectorAll('.role-checkbox').forEach(cb => cb.checked = false);
        el.roleError.classList.add('d-none');

        getAccountModalInstance().show();
    }

    function openEditModal(account) {
        if (!el.form || !account) return;
        el.form.reset();
        el.form.classList.remove('was-validated');

        const userId = val(account, 'UserId', 'userId', 'USER_ID', 'id');
        const fullName = val(account, 'FullName', 'fullName', 'FULL_NAME') || '';
        const email = val(account, 'Email', 'email', 'EMAIL') || '';
        const status = val(account, 'Status', 'status', 'STATUS') || 'ACTIVE';
        const roleIdsStr = String(val(account, 'RoleIds', 'roleIds', 'ROLE_IDS') || '');
        const rolesStr = String(val(account, 'Roles', 'roles', 'ROLES') || '');

        const accountRoleIds = roleIdsStr.split(',').map(s => s.trim()).filter(Boolean);
        const accountRoleCodes = rolesStr.split(',').map(s => s.trim().toUpperCase()).filter(Boolean);

        // Điền dữ liệu
        el.userId.value = userId;
        el.fullName.value = fullName;
        el.email.value = email;

        // Email ở chế độ readonly - TUYỆT ĐỐI KHÔNG DÙNG bg-light để tránh màu trắng chói mắt
        el.email.readOnly = true;
        el.email.classList.remove('bg-light');
        if (el.emailHelpText) el.emailHelpText.textContent = 'Email dùng để đăng nhập và không thể sửa đổi.';

        // Status group visible
        el.statusGroup.classList.remove('d-none');
        el.status.value = status;

        // Password optional for edit
        el.password.value = '';
        el.password.required = false;
        el.passwordStar.classList.add('d-none');
        el.passwordHelp.textContent = 'Để trống nếu giữ nguyên mật khẩu (nếu đổi, nhập ít nhất 8 ký tự).';

        // Tự động tích chọn checkbox các vai trò của tài khoản này
        el.roleOptions.querySelectorAll('.role-checkbox').forEach(cb => {
            const roleId = String(cb.value);
            const roleCode = String(cb.getAttribute('data-code') || '').toUpperCase();
            cb.checked = accountRoleIds.includes(roleId) || accountRoleCodes.includes(roleCode);
        });

        el.roleError.classList.add('d-none');
        el.modalTitle.innerHTML = `<i class="bi bi-pencil-square me-2"></i>Chỉnh sửa tài khoản <span class="text-primary fs-6">#${html(userId)}</span>`;
        el.submitLabel.textContent = 'Lưu thay đổi';

        getAccountModalInstance().show();
    }

    function openDeleteModal(account) {
        if (!account) return;
        const userId = val(account, 'UserId', 'userId', 'USER_ID', 'id');
        const fullName = val(account, 'FullName', 'fullName', 'FULL_NAME') || '';
        const email = val(account, 'Email', 'email', 'EMAIL') || '';

        pendingDeleteUserId = userId;
        if (el.deleteAccountName) {
            el.deleteAccountName.textContent = `${fullName} (${email})`;
        }
        getDeleteModalInstance().show();
    }

    function getAccountModalInstance() {
        if (!bsAccountModal && el.modal) {
            bsAccountModal = bootstrap.Modal.getOrCreateInstance(el.modal);
        }
        return bsAccountModal;
    }

    function getDeleteModalInstance() {
        if (!bsDeleteModal && el.deleteModal) {
            bsDeleteModal = bootstrap.Modal.getOrCreateInstance(el.deleteModal);
        }
        return bsDeleteModal;
    }

    async function load() {
        if (!el.loading) return;
        el.loading.classList.remove('d-none');
        el.table.classList.add('d-none');
        el.empty.classList.add('d-none');

        try {
            const response = await fetch(config.dataUrl, {
                headers: {
                    'Accept': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest'
                }
            });

            if (!response.ok) {
                if (response.status === 401) throw new Error('Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.');
                if (response.status === 403) throw new Error('Bạn không có quyền xem danh sách tài khoản.');
                throw new Error(`Lỗi máy chủ (${response.status}) khi tải dữ liệu.`);
            }

            const payload = await response.json();
            const isSuccess = payload.success ?? payload.Success;
            if (!isSuccess && payload.code !== '200') {
                throw new Error(payload.message ?? payload.Message ?? 'Không tải được dữ liệu tài khoản.');
            }

            const { accounts, roles } = parsePayload(payload);
            renderRoles(roles);
            renderAccounts(accounts);
        } catch (error) {
            console.error('Error loading account management data:', error);
            showAlert(error.message || 'Không tải được dữ liệu tài khoản quản trị.');
        } finally {
            el.loading.classList.add('d-none');
        }
    }

    // Submit handler (Create & Update)
    if (el.form) {
        el.form.addEventListener('submit', async event => {
            event.preventDefault();

            const isEdit = Boolean(el.userId.value);
            const selectedRoleIds = [...document.querySelectorAll('.role-checkbox:checked')]
                .map(input => Number(input.value))
                .filter(Boolean);

            const hasRoles = selectedRoleIds.length > 0;
            el.roleError.classList.toggle('d-none', hasRoles);

            const pwdVal = el.password.value;
            if (isEdit && pwdVal.length > 0 && pwdVal.length < 8) {
                el.password.setCustomValidity('Mật khẩu mới phải có ít nhất 8 ký tự.');
            } else {
                el.password.setCustomValidity('');
            }

            if (!el.form.checkValidity() || !hasRoles) {
                el.form.classList.add('was-validated');
                return;
            }

            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';
            const payload = isEdit ? {
                userId: Number(el.userId.value),
                fullName: el.fullName.value.trim(),
                email: el.email.value.trim(),
                password: pwdVal ? pwdVal : null,
                status: el.status.value,
                roleIds: selectedRoleIds
            } : {
                fullName: el.fullName.value.trim(),
                email: el.email.value.trim(),
                password: pwdVal,
                roleIds: selectedRoleIds
            };

            const targetUrl = isEdit ? (config.updateUrl || config.createUrl) : config.createUrl;

            el.submitBtn.disabled = true;
            el.submitLabel.classList.add('d-none');
            el.spinner.classList.remove('d-none');
            hideAlert();

            try {
                const response = await fetch(targetUrl, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'Accept': 'application/json',
                        'RequestVerificationToken': token,
                        'X-Requested-With': 'XMLHttpRequest'
                    },
                    body: JSON.stringify(payload)
                });

                const result = await response.json();
                const isSuccess = result.success ?? result.Success;

                if (!response.ok || (!isSuccess && result.code !== '200')) {
                    throw new Error(result.message ?? result.Message ?? (isEdit ? 'Không thể cập nhật tài khoản.' : 'Không thể tạo tài khoản.'));
                }

                getAccountModalInstance().hide();
                el.form.reset();
                el.form.classList.remove('was-validated');

                showAlert(result.message ?? result.Message ?? (isEdit ? 'Cập nhật tài khoản thành công.' : 'Tạo tài khoản thành công.'), 'success');
                await load();
            } catch (error) {
                console.error('Error saving account:', error);
                showAlert(error.message || 'Thao tác không thành công.');
            } finally {
                el.submitBtn.disabled = false;
                el.submitLabel.classList.remove('d-none');
                el.spinner.classList.add('d-none');
            }
        });
    }

    // Confirm Delete button handler
    if (el.btnConfirmDelete) {
        el.btnConfirmDelete.addEventListener('click', async () => {
            if (!pendingDeleteUserId) return;

            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';
            const targetUrl = `${config.deleteUrl}?userId=${encodeURIComponent(pendingDeleteUserId)}`;

            el.btnConfirmDelete.disabled = true;
            el.deleteLabel.classList.add('d-none');
            el.deleteSpinner.classList.remove('d-none');

            try {
                const response = await fetch(targetUrl, {
                    method: 'POST',
                    headers: {
                        'Accept': 'application/json',
                        'RequestVerificationToken': token,
                        'X-Requested-With': 'XMLHttpRequest'
                    }
                });

                const result = await response.json();
                const isSuccess = result.success ?? result.Success;

                if (!response.ok || (!isSuccess && result.code !== '200')) {
                    throw new Error(result.message ?? result.Message ?? 'Không thể xóa tài khoản này.');
                }

                getDeleteModalInstance().hide();
                showAlert(result.message ?? result.Message ?? 'Xóa tài khoản thành công.', 'success');
                pendingDeleteUserId = null;
                await load();
            } catch (error) {
                console.error('Error deleting account:', error);
                showAlert(error.message || 'Xóa tài khoản thất bại.');
            } finally {
                el.btnConfirmDelete.disabled = false;
                el.deleteLabel.classList.remove('d-none');
                el.deleteSpinner.classList.add('d-none');
            }
        });
    }

    if (el.btnOpenCreate) el.btnOpenCreate.addEventListener('click', openCreateModal);
    if (el.btnReload) el.btnReload.addEventListener('click', () => { hideAlert(); load(); });

    load();
})();
