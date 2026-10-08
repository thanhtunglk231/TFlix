(() => {
    'use strict';
    const config = window.adminAccountConfig;
    if (!config) return;
    const el = { form: document.getElementById('createAccountForm'), body: document.getElementById('accountTableBody'), table: document.getElementById('accountTableWrap'), loading: document.getElementById('accountLoading'), empty: document.getElementById('accountEmpty'), alert: document.getElementById('accountAlert'), roles: document.getElementById('roleOptions'), roleError: document.getElementById('roleError'), submit: document.getElementById('btnCreateAccount') };
    const val = (item, ...names) => names.map(name => item?.[name]).find(v => v !== undefined && v !== null);
    const html = text => String(text ?? '').replace(/[&<>'"]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', "'": '&#39;', '"': '&quot;' })[c]);
    const tables = response => { const data = response?.data ?? response?.Data ?? {}; return [data.Table ?? data.table ?? [], data.Table1 ?? data.table1 ?? []]; };
    function alert(message, type = 'danger') { el.alert.className = `alert alert-${type}`; el.alert.textContent = message; }
    function renderAccounts(items) {
        el.body.innerHTML = items.map(item => { const status = val(item, 'Status', 'status') ?? ''; const date = val(item, 'CreatedAt', 'createdAt'); return `<tr><td class="fw-semibold">${html(val(item,'FullName','fullName'))}</td><td>${html(val(item,'Email','email'))}</td><td><span class="badge text-bg-info">${html(val(item,'Roles','roles') || 'Chưa gán')}</span></td><td><span class="badge ${status === 'ACTIVE' ? 'text-bg-success' : 'text-bg-secondary'}">${html(status)}</span></td><td>${html(date ? new Date(date).toLocaleString('vi-VN') : '')}</td></tr>`; }).join('');
        el.empty.classList.toggle('d-none', items.length > 0); el.table.classList.toggle('d-none', items.length === 0);
    }
    function renderRoles(items) { el.roles.innerHTML = items.map(item => { const id=val(item,'RoleId','roleId'), code=val(item,'RoleCode','roleCode'), name=val(item,'RoleName','roleName'); return `<label class="form-check border rounded p-2 ps-5"><input class="form-check-input role-checkbox" type="checkbox" value="${html(id)}"><span class="fw-semibold">${html(code)}</span><span class="text-muted ms-1">${html(name)}</span></label>`; }).join(''); }
    async function load() {
        el.loading.classList.remove('d-none'); el.table.classList.add('d-none'); el.empty.classList.add('d-none');
        try { const response=await fetch(config.dataUrl,{headers:{Accept:'application/json','X-Requested-With':'XMLHttpRequest'}}); const payload=await response.json(); if(!response.ok || !(payload.success ?? payload.Success)) throw new Error(payload.message ?? payload.Message ?? 'Không tải được dữ liệu.'); const [accounts,roles]=tables(payload); renderAccounts(accounts); renderRoles(roles); }
        catch(error) { alert(error.message || 'Không tải được dữ liệu tài khoản.'); }
        finally { el.loading.classList.add('d-none'); }
    }
    el.form?.addEventListener('submit', async event => {
        event.preventDefault(); const roleIds=[...document.querySelectorAll('.role-checkbox:checked')].map(input=>Number(input.value)); el.roleError.classList.toggle('d-none',roleIds.length>0);
        if(!el.form.checkValidity() || roleIds.length===0){el.form.classList.add('was-validated');return;}
        const token=document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? ''; const payload={fullName:document.getElementById('fullName').value.trim(),email:document.getElementById('email').value.trim(),password:document.getElementById('password').value,roleIds};
        el.submit.disabled=true; el.submit.querySelector('.submit-label').classList.add('d-none'); el.submit.querySelector('.spinner-border').classList.remove('d-none');
        try { const response=await fetch(config.createUrl,{method:'POST',headers:{'Content-Type':'application/json',Accept:'application/json',RequestVerificationToken:token,'X-Requested-With':'XMLHttpRequest'},body:JSON.stringify(payload)}); const result=await response.json(); if(!response.ok || !(result.success ?? result.Success)) throw new Error(result.message ?? result.Message ?? 'Không tạo được tài khoản.'); bootstrap.Modal.getInstance(document.getElementById('createAccountModal'))?.hide(); el.form.reset(); el.form.classList.remove('was-validated'); alert(result.message ?? result.Message ?? 'Tạo tài khoản thành công.','success'); await load(); }
        catch(error){alert(error.message || 'Không tạo được tài khoản.');}
        finally{el.submit.disabled=false;el.submit.querySelector('.submit-label').classList.remove('d-none');el.submit.querySelector('.spinner-border').classList.add('d-none');}
    });
    load();
})();
