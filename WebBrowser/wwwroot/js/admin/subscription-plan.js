(() => {
    const page = document.getElementById('subscriptionPlanPage');
    if (!page) return;
    const field = id => document.getElementById(id);
    const modal = bootstrap.Modal.getOrCreateInstance(field('planModal'));
    const deleteModal = bootstrap.Modal.getOrCreateInstance(field('deletePlanModal'));
    const form = field('planForm');
    const token = page.querySelector('input[name="__RequestVerificationToken"]').value;
    let plans = [], deletePlanId = 0;
    const normalized = key => key.replaceAll('_', '').toLowerCase();
    const val = (item, ...keys) => {
        const direct = keys.map(key => item?.[key]).find(value => value != null);
        if (direct !== undefined) return direct;
        const expected = new Set(keys.map(normalized));
        const actual = Object.keys(item ?? {}).find(key => expected.has(normalized(key)));
        return actual === undefined ? undefined : item[actual];
    };
    const yes = value => value === true || String(value).toUpperCase() === 'Y';
    const esc = text => { const el = document.createElement('div'); el.textContent = text ?? ''; return el.innerHTML; };
    const rowsOf = result => { const data = result?.data ?? result?.Data; return data?.table ?? data?.Table ?? (Array.isArray(data) ? data : []); };
    const isSuccess = result => result?.success === true || result?.Success === true || String(result?.code ?? result?.Code) === '200';
    function responseMessage(result) {
        const direct = result?.message ?? result?.Message ?? result?.detail ?? result?.Detail ?? result?.title ?? result?.Title;
        if (direct) return direct;
        const errors = result?.errors ?? result?.Errors;
        if (errors && typeof errors === 'object') {
            return Object.values(errors).flat().join(' ');
        }
        return '';
    }
    async function readResponse(response, fallback) {
        const text = await response.text();
        let result;
        try {
            result = text ? JSON.parse(text) : null;
        } catch {
            throw new Error(`${fallback} (HTTP ${response.status}).`);
        }
        if (!response.ok || !isSuccess(result)) {
            throw new Error(responseMessage(result) || `${fallback} (HTTP ${response.status}).`);
        }
        return result;
    }
    function state(name) {
        ['Loading', 'Error', 'Empty'].forEach(value => field(`plan${value}`).classList.toggle('d-none', name !== value.toLowerCase()));
        field('planTableWrap').classList.toggle('d-none', name !== 'ready');
    }
    function toast(message, ok = true) {
        const el = field('planToast'); el.classList.toggle('text-bg-success', ok); el.classList.toggle('text-bg-danger', !ok);
        el.querySelector('.toast-body').textContent = message; bootstrap.Toast.getOrCreateInstance(el).show();
    }
    function render() {
        if (!plans.length) return state('empty');
        field('planRows').innerHTML = plans.map(plan => {
            const id = val(plan, 'PLAN_ID', 'planId', 'PlanId'), code = val(plan, 'PLAN_CODE', 'planCode', 'PlanCode');
            const name = val(plan, 'NAME', 'name', 'Name'), price = Number(val(plan, 'PRICE', 'price', 'Price') || 0);
            const days = val(plan, 'DURATION_DAYS', 'durationDays', 'DurationDays'), devices = val(plan, 'MAX_DEVICES', 'maxDevices', 'MaxDevices');
            const quality = val(plan, 'QUALITY_CAP', 'qualityCap', 'QualityCap') || 'Không giới hạn';
            const perks = `<span class="benefit-badge">${esc(quality)}</span>${yes(val(plan, 'ADS_FREE', 'adsFree', 'AdsFree')) ? '<span class="benefit-badge">Không quảng cáo</span>' : ''}${yes(val(plan, 'DOWNLOADABLE', 'downloadable', 'Downloadable')) ? '<span class="benefit-badge">Tải xuống</span>' : ''}`;
            return `<tr><td><span class="badge text-bg-dark">${esc(code)}</span></td><td class="fw-semibold">${esc(name)}</td><td class="plan-price">${price.toLocaleString('vi-VN')} ₫</td><td>${days} ngày</td><td>${devices}</td><td>${perks}</td><td class="text-end"><div class="btn-group btn-group-sm" role="group" aria-label="Thao tác với gói ${esc(name)}"><button class="btn btn-outline-info edit-plan" data-id="${id}" aria-label="Sửa gói ${esc(name)}"><i class="bi bi-pencil-square"></i> Sửa</button><button class="btn btn-outline-danger delete-plan" data-id="${id}" data-name="${esc(name)}" aria-label="Xóa gói ${esc(name)}"><i class="bi bi-trash"></i> Xóa</button></div></td></tr>`;
        }).join(''); state('ready');
    }
    async function load() {
        state('loading');
        try {
            const response = await fetch(page.dataset.listUrl, { headers: { Accept: 'application/json', 'X-Requested-With': 'XMLHttpRequest' } });
            const json = await readResponse(response, 'Không tải được danh sách gói.');
            plans = rowsOf(json); render();
        } catch (error) { field('planError').textContent = error.message; state('error'); }
    }
    function create() {
        form.reset(); field('planId').value = ''; field('maxDevices').value = 2; field('adsFree').checked = true;
        field('planModalTitle').textContent = 'Thêm gói Premium'; field('formError').classList.add('d-none'); modal.show();
    }
    function edit(id) {
        const plan = plans.find(item => String(val(item, 'PLAN_ID', 'planId', 'PlanId')) === String(id));
        if (!plan) return toast('Không tìm thấy gói Premium cần sửa.', false);
        field('planId').value = id; field('planCode').value = val(plan, 'PLAN_CODE', 'planCode', 'PlanCode') || '';
        field('planName').value = val(plan, 'NAME', 'name', 'Name') || ''; field('planPrice').value = val(plan, 'PRICE', 'price', 'Price') || 0;
        field('durationDays').value = val(plan, 'DURATION_DAYS', 'durationDays', 'DurationDays') || 1;
        field('maxDevices').value = val(plan, 'MAX_DEVICES', 'maxDevices', 'MaxDevices') || 1;
        field('qualityCap').value = val(plan, 'QUALITY_CAP', 'qualityCap', 'QualityCap') || '';
        field('adsFree').checked = yes(val(plan, 'ADS_FREE', 'adsFree', 'AdsFree'));
        field('downloadable').checked = yes(val(plan, 'DOWNLOADABLE', 'downloadable', 'Downloadable'));
        field('planModalTitle').textContent = 'Chỉnh sửa gói Premium'; field('formError').classList.add('d-none'); modal.show();
    }
    form.addEventListener('submit', async event => {
        event.preventDefault(); if (!form.reportValidity()) return;
        const id = Number(field('planId').value || 0);
        const body = { planId: id, planCode: field('planCode').value.trim().toUpperCase(), name: field('planName').value.trim(), price: Number(field('planPrice').value), durationDays: Number(field('durationDays').value), maxDevices: Number(field('maxDevices').value), qualityCap: field('qualityCap').value || null, adsFree: field('adsFree').checked, downloadable: field('downloadable').checked };
        field('btnSavePlan').disabled = true;
        try {
            const response = await fetch(id ? page.dataset.updateUrl : page.dataset.createUrl, { method: 'POST', headers: { 'Content-Type': 'application/json; charset=utf-8', RequestVerificationToken: token, 'X-Requested-With': 'XMLHttpRequest' }, body: JSON.stringify(body) });
            const json = await readResponse(response, 'Không lưu được gói Premium.');
            modal.hide(); toast(responseMessage(json) || 'Đã lưu gói Premium.'); await load();
        } catch (error) { field('formError').textContent = error.message; field('formError').classList.remove('d-none'); }
        finally { field('btnSavePlan').disabled = false; }
    });
    field('btnCreatePlan').addEventListener('click', create);
    field('planRows').addEventListener('click', event => {
        const editButton = event.target.closest('.edit-plan'); if (editButton) return edit(editButton.dataset.id);
        const deleteButton = event.target.closest('.delete-plan');
        if (deleteButton) { deletePlanId = Number(deleteButton.dataset.id || 0); if (!deletePlanId) return toast('Không xác định được gói cần xóa.', false); field('deletePlanName').textContent = deleteButton.dataset.name || `#${deletePlanId}`; deleteModal.show(); }
    });
    field('btnConfirmDelete').addEventListener('click', async () => {
        if (!deletePlanId) return; const button = field('btnConfirmDelete'); button.disabled = true;
        try {
            const response = await fetch(page.dataset.deleteUrl, { method: 'POST', headers: { 'Content-Type': 'application/json; charset=utf-8', RequestVerificationToken: token, 'X-Requested-With': 'XMLHttpRequest' }, body: JSON.stringify({ planId: deletePlanId }) });
            const json = await readResponse(response, 'Không xóa được gói Premium.');
            deleteModal.hide(); toast(responseMessage(json) || 'Đã xóa gói Premium.'); deletePlanId = 0; await load();
        } catch (error) { deleteModal.hide(); toast(error.message, false); }
        finally { button.disabled = false; }
    });
    load();
})();
