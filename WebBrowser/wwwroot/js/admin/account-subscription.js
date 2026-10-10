(() => {
    'use strict';
    const page = document.getElementById('accountSubscriptionPage');
    if (!page) return;

    const byId = id => document.getElementById(id);
    const modal = bootstrap.Modal.getOrCreateInstance(byId('subscriptionModal'));
    const deleteModal = bootstrap.Modal.getOrCreateInstance(byId('deleteSubscriptionModal'));
    const toastEl = byId('subscriptionToast');
    const toast = bootstrap.Toast.getOrCreateInstance(toastEl, { delay: 3500 });
    const token = page.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
    let subscriptions = [], users = [], plans = [], deleteId = 0;

    const value = (obj, ...keys) => { for (const key of keys) if (obj?.[key] !== undefined && obj[key] !== null) return obj[key]; return null; };
    const escapeHtml = text => String(text ?? '').replace(/[&<>'"]/g, char => ({ '&':'&amp;', '<':'&lt;', '>':'&gt;', "'":'&#39;', '"':'&quot;' })[char]);
    const responseMessage = json => json?.message || json?.Message || 'Không thể xử lý yêu cầu.';
    const parseDate = date => date ? new Date(date) : null;
    const formatDate = date => parseDate(date)?.toLocaleString('vi-VN', { dateStyle: 'short', timeStyle: 'short' }) || '—';
    const localInput = date => {
        const d = date ? new Date(date) : new Date();
        const offset = d.getTimezoneOffset();
        return new Date(d.getTime() - offset * 60000).toISOString().slice(0, 16);
    };
    const showToast = (message, ok = true) => {
        toastEl.classList.toggle('text-bg-success', ok);
        toastEl.classList.toggle('text-bg-danger', !ok);
        toastEl.querySelector('.toast-body').textContent = message;
        toast.show();
    };
    async function readResponse(response) {
        const json = await response.json().catch(() => null);
        if (!response.ok || !(json?.Success ?? json?.success)) throw new Error(responseMessage(json));
        return json;
    }

    function setOptions() {
        byId('subscriptionUser').innerHTML = '<option value="">-- Chọn tài khoản --</option>' + users.map(user => {
            const id = value(user, 'USER_ID', 'UserId', 'userId');
            const name = value(user, 'FULL_NAME', 'FullName', 'fullName') || 'Chưa có tên';
            const email = value(user, 'EMAIL', 'Email', 'email') || '';
            return `<option value="${id}">${escapeHtml(name)} — ${escapeHtml(email)}</option>`;
        }).join('');
        byId('subscriptionPlan').innerHTML = '<option value="">-- Chọn gói --</option>' + plans.map(plan => {
            const id = value(plan, 'PLAN_ID', 'PlanId', 'planId');
            const name = value(plan, 'NAME', 'Name', 'name');
            const days = value(plan, 'DURATION_DAYS', 'DurationDays', 'durationDays');
            return `<option value="${id}" data-days="${days || 30}">${escapeHtml(name)} (${days} ngày)</option>`;
        }).join('');
    }

    function render() {
        const query = byId('subscriptionSearch').value.trim().toLowerCase();
        const status = byId('subscriptionStatusFilter').value;
        const filtered = subscriptions.filter(item => {
            const haystack = [value(item,'EMAIL','Email'), value(item,'FULL_NAME','FullName'), value(item,'PLAN_NAME','PlanName'), value(item,'PLAN_CODE','PlanCode')].join(' ').toLowerCase();
            return (!query || haystack.includes(query)) && (!status || String(value(item,'STATUS','Status')).toUpperCase() === status);
        });
        byId('subscriptionEmpty').classList.toggle('d-none', filtered.length > 0);
        byId('subscriptionTableWrap').classList.toggle('d-none', filtered.length === 0);
        byId('subscriptionRows').innerHTML = filtered.map(item => {
            const id = value(item,'SUBSCRIPTION_ID','SubscriptionId','subscriptionId');
            const currentStatus = String(value(item,'STATUS','Status') || 'EXPIRED').toUpperCase();
            const statusClass = currentStatus === 'ACTIVE' ? 'success' : currentStatus === 'CANCELLED' ? 'secondary' : 'warning';
            const statusText = currentStatus === 'ACTIVE' ? 'Đang hoạt động' : currentStatus === 'CANCELLED' ? 'Đã hủy' : 'Hết hạn';
            return `<tr>
                <td class="subscription-user"><strong>${escapeHtml(value(item,'FULL_NAME','FullName') || 'Chưa có tên')}</strong><span>${escapeHtml(value(item,'EMAIL','Email'))}</span></td>
                <td><strong>${escapeHtml(value(item,'PLAN_NAME','PlanName'))}</strong><div class="small text-info">${escapeHtml(value(item,'PLAN_CODE','PlanCode'))}</div></td>
                <td class="subscription-period">${formatDate(value(item,'START_AT','StartAt'))}<small>đến ${formatDate(value(item,'END_AT','EndAt'))}</small></td>
                <td><span class="badge text-bg-${statusClass}">${statusText}</span></td>
                <td class="text-end subscription-actions"><button class="btn btn-sm btn-outline-info me-1" data-action="edit" data-id="${id}" aria-label="Sửa đăng ký"><i class="bi bi-pencil"></i></button><button class="btn btn-sm btn-outline-danger" data-action="delete" data-id="${id}" aria-label="Xóa đăng ký"><i class="bi bi-trash"></i></button></td>
            </tr>`;
        }).join('');
    }

    async function load() {
        byId('subscriptionLoading').classList.remove('d-none');
        byId('subscriptionError').classList.add('d-none');
        byId('subscriptionTableWrap').classList.add('d-none');
        byId('subscriptionEmpty').classList.add('d-none');
        try {
            const json = await readResponse(await fetch(page.dataset.listUrl, { headers: { Accept: 'application/json' } }));
            subscriptions = json.subscriptions || json.Subscriptions || [];
            users = json.users || json.Users || [];
            plans = json.plans || json.Plans || [];
            setOptions(); render();
        } catch (error) {
            byId('subscriptionError').textContent = error.message;
            byId('subscriptionError').classList.remove('d-none');
        } finally { byId('subscriptionLoading').classList.add('d-none'); }
    }

    function openCreate() {
        byId('subscriptionForm').reset(); byId('subscriptionId').value = '';
        byId('subscriptionModalTitle').textContent = 'Gắn gói Premium cho tài khoản';
        const start = new Date(), end = new Date(); end.setDate(end.getDate() + 30);
        byId('subscriptionStartAt').value = localInput(start); byId('subscriptionEndAt').value = localInput(end);
        byId('subscriptionStatus').value = 'ACTIVE'; byId('subscriptionFormError').classList.add('d-none'); modal.show();
    }
    function openEdit(id) {
        const item = subscriptions.find(row => String(value(row,'SUBSCRIPTION_ID','SubscriptionId','subscriptionId')) === String(id));
        if (!item) return showToast('Không tìm thấy đăng ký cần sửa.', false);
        byId('subscriptionId').value = id;
        byId('subscriptionUser').value = value(item,'USER_ID','UserId');
        byId('subscriptionPlan').value = value(item,'PLAN_ID','PlanId');
        byId('subscriptionStartAt').value = localInput(value(item,'START_AT','StartAt'));
        byId('subscriptionEndAt').value = localInput(value(item,'END_AT','EndAt'));
        byId('subscriptionStatus').value = String(value(item,'STATUS','Status')).toUpperCase();
        byId('subscriptionModalTitle').textContent = 'Cập nhật đăng ký Premium';
        byId('subscriptionFormError').classList.add('d-none'); modal.show();
    }

    byId('subscriptionPlan').addEventListener('change', event => {
        if (byId('subscriptionId').value) return;
        const start = parseDate(byId('subscriptionStartAt').value); if (!start) return;
        const days = Number(event.target.selectedOptions[0]?.dataset.days || 30);
        const end = new Date(start); end.setDate(end.getDate() + days); byId('subscriptionEndAt').value = localInput(end);
    });
    byId('subscriptionForm').addEventListener('submit', async event => {
        event.preventDefault();
        if (!event.currentTarget.reportValidity()) return;
        const id = Number(byId('subscriptionId').value || 0);
        const payload = { UserId: Number(byId('subscriptionUser').value), PlanId: Number(byId('subscriptionPlan').value), StartAt: new Date(byId('subscriptionStartAt').value).toISOString(), EndAt: new Date(byId('subscriptionEndAt').value).toISOString(), Status: byId('subscriptionStatus').value };
        if (payload.EndAt <= payload.StartAt) { byId('subscriptionFormError').textContent = 'Thời gian kết thúc phải sau thời gian bắt đầu.'; byId('subscriptionFormError').classList.remove('d-none'); return; }
        if (id) payload.SubscriptionId = id;
        const button = byId('btnSaveSubscription'); button.disabled = true;
        try {
            const response = await fetch(id ? page.dataset.updateUrl : page.dataset.createUrl, { method: 'POST', headers: { 'Content-Type':'application/json', 'RequestVerificationToken':token }, body: JSON.stringify(payload) });
            const json = await readResponse(response); modal.hide(); showToast(responseMessage(json)); await load();
        } catch (error) { byId('subscriptionFormError').textContent = error.message; byId('subscriptionFormError').classList.remove('d-none'); }
        finally { button.disabled = false; }
    });
    byId('subscriptionRows').addEventListener('click', event => {
        const button = event.target.closest('button[data-action]'); if (!button) return;
        if (button.dataset.action === 'edit') openEdit(button.dataset.id);
        else { deleteId = Number(button.dataset.id); deleteModal.show(); }
    });
    byId('btnConfirmDeleteSubscription').addEventListener('click', async () => {
        const button = byId('btnConfirmDeleteSubscription'); button.disabled = true;
        try {
            const json = await readResponse(await fetch(page.dataset.deleteUrl, { method:'POST', headers:{ 'Content-Type':'application/json', 'RequestVerificationToken':token }, body:JSON.stringify({ SubscriptionId:deleteId }) }));
            deleteModal.hide(); showToast(responseMessage(json)); deleteId = 0; await load();
        } catch (error) { showToast(error.message, false); } finally { button.disabled = false; }
    });
    byId('btnCreateSubscription').addEventListener('click', openCreate);
    byId('btnReloadSubscriptions').addEventListener('click', load);
    byId('subscriptionSearch').addEventListener('input', render);
    byId('subscriptionStatusFilter').addEventListener('change', render);
    load();
})();
