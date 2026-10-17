(() => {
    'use strict';
    const app = document.getElementById('mediaAssetApp');
    if (!app) return;

    const modalElement = document.getElementById('assetModal');
    const form = document.getElementById('assetForm');
    const ownerType = document.getElementById('ownerType');
    const ownerId = document.getElementById('ownerId');
    const assetFile = document.getElementById('assetFile');
    const assetFileHelp = document.getElementById('assetFileHelp');
    const rows = document.getElementById('assetRows');
    const loading = document.getElementById('assetLoading');
    const empty = document.getElementById('assetEmpty');
    const tableWrap = document.getElementById('assetTableWrap');
    const loadMoreButton = document.getElementById('btnLoadMore');
    const saveButton = document.getElementById('btnSaveAsset');
    const modal = bootstrap.Modal.getOrCreateInstance(modalElement);
    const owners = { MOVIE: [], SERIES: [], EPISODE: [] };
    let assets = [];
    let cursor = null;

    const val = (object, ...keys) => keys.map(key => object?.[key]).find(item => item !== undefined && item !== null);
    const escapeHtml = text => String(text ?? '').replace(/[&<>'"]/g, character => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', "'": '&#39;', '"': '&quot;' })[character]);
    const isSuccess = json => json?.success === true || json?.Success === true || String(json?.code) === '200';

    function showAlert(message, success = false) {
        const element = document.getElementById('assetAlert');
        element.className = `alert alert-${success ? 'success' : 'danger'}`;
        element.textContent = message;
        element.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
    }

    async function fetchJson(url, options = {}) {
        const response = await fetch(url, {
            ...options,
            headers: { Accept: 'application/json', 'X-Requested-With': 'XMLHttpRequest', ...(options.headers || {}) }
        });
        const contentType = response.headers.get('content-type') || '';
        const json = contentType.includes('application/json') ? await response.json() : null;
        if (response.status === 401) {
            window.location.href = `/Admin/Auth?returnUrl=${encodeURIComponent(window.location.pathname + window.location.search)}`;
            throw new Error(json?.message || 'Phiên đăng nhập đã hết hạn.');
        }
        if (!response.ok || !isSuccess(json)) throw new Error(json?.message || `Yêu cầu thất bại (${response.status}).`);
        return json;
    }

    function fillOwners(selected) {
        const items = owners[ownerType.value] || [];
        ownerId.innerHTML = '<option value="">Chọn nội dung</option>' + items.map(item => {
            const id = val(item, 'ownerId', 'OwnerId');
            return `<option value="${id}" ${String(id) === String(selected) ? 'selected' : ''}>${escapeHtml(val(item, 'ownerTitle', 'OwnerTitle'))}</option>`;
        }).join('');
    }

    function render() {
        loading.classList.add('d-none');
        empty.classList.toggle('d-none', assets.length > 0);
        tableWrap.classList.toggle('d-none', assets.length === 0);
        loadMoreButton.classList.toggle('d-none', !cursor?.hasMore);
        rows.innerHTML = assets.map((item, index) => {
            const url = val(item, 'url', 'Url');
            const type = String(val(item, 'assetType', 'AssetType') || '').toUpperCase();
            const isVideo = type === 'TRAILER' || /\.(mp4|webm|m3u8)(\?|$)/i.test(url || '');
            const preview = isVideo
                ? `<video class="asset-preview asset-preview-video" muted preload="metadata"><source src="${escapeHtml(url)}"></video>`
                : `<img class="asset-preview" src="${escapeHtml(url)}" alt="${escapeHtml(type || 'Media')}" loading="lazy">`;
            return `<tr><td>${preview}</td><td class="asset-owner-title"><span class="badge text-bg-secondary mb-1">${escapeHtml(val(item, 'ownerType', 'OwnerType'))}</span><div>${escapeHtml(val(item, 'ownerTitle', 'OwnerTitle'))}</div></td><td>${escapeHtml(type)}</td><td>${escapeHtml(val(item, 'sortOrder', 'SortOrder') ?? 0)}</td><td class="text-end"><button type="button" class="btn btn-sm btn-outline-info me-2" data-edit="${index}">Sửa</button><button type="button" class="btn btn-sm btn-outline-danger" data-delete="${index}">Xóa</button></td></tr>`;
        }).join('');
    }

    async function loadOwners() {
        if (owners.MOVIE.length || owners.SERIES.length || owners.EPISODE.length) return;
        const json = await fetchJson(app.dataset.ownersUrl);
        owners.MOVIE = val(json.data, 'table', 'Table') || [];
        owners.SERIES = val(json.data, 'table1', 'Table1') || [];
        owners.EPISODE = val(json.data, 'table2', 'Table2') || [];
        fillOwners();
    }

    async function loadAssets(append = false) {
        loadMoreButton.disabled = true;
        if (!append) loading.classList.remove('d-none');
        let url = `${app.dataset.listUrl}?pageSize=10`;
        if (append && cursor) url += `&cursorOwnerType=${encodeURIComponent(cursor.ownerType)}&cursorAssetId=${encodeURIComponent(cursor.assetId)}`;
        try {
            const json = await fetchJson(url);
            const page = val(json.data, 'table', 'Table') || [];
            const metadata = (val(json.data, 'table1', 'Table1') || [])[0];
            assets = append ? assets.concat(page) : page;
            cursor = metadata ? {
                ownerType: val(metadata, 'nextOwnerType', 'NextOwnerType'),
                assetId: val(metadata, 'nextAssetId', 'NextAssetId'),
                hasMore: val(metadata, 'hasMore', 'HasMore') === true
            } : null;
            render();
        } finally {
            loading.classList.add('d-none');
            loadMoreButton.disabled = false;
        }
    }

    async function reload() {
        cursor = null;
        assets = [];
        try { await loadAssets(false); }
        catch (error) {
            empty.classList.add('d-none');
            tableWrap.classList.add('d-none');
            showAlert(error.message || 'Không tải được danh sách media.');
        }
    }

    document.getElementById('btnAddAsset').addEventListener('click', async () => {
        try {
            await loadOwners();
            form.reset();
            document.getElementById('assetId').value = '';
            document.getElementById('oldUrl').value = '';
            assetFile.required = true;
            assetFileHelp.textContent = 'Chọn ảnh hoặc video để thêm media.';
            fillOwners();
            document.getElementById('assetModalTitle').textContent = 'Thêm media';
            modal.show();
        } catch (error) { showAlert(error.message); }
    });

    ownerType.addEventListener('change', () => fillOwners());
    loadMoreButton.addEventListener('click', async () => {
        try { await loadAssets(true); } catch (error) { showAlert(error.message); }
    });

    rows.addEventListener('click', async event => {
        const edit = event.target.closest('[data-edit]');
        const remove = event.target.closest('[data-delete]');
        if (edit) {
            try {
                await loadOwners();
                const item = assets[Number(edit.dataset.edit)];
                ownerType.value = val(item, 'ownerType', 'OwnerType');
                fillOwners(val(item, 'ownerId', 'OwnerId'));
                document.getElementById('assetId').value = val(item, 'assetId', 'AssetId');
                document.getElementById('oldUrl').value = val(item, 'url', 'Url');
                document.getElementById('assetType').value = val(item, 'assetType', 'AssetType');
                document.getElementById('sortOrder').value = val(item, 'sortOrder', 'SortOrder') ?? 0;
                assetFile.value = '';
                assetFile.required = false;
                assetFileHelp.textContent = 'Không bắt buộc. Chỉ chọn file khi muốn thay thế media hiện tại.';
                document.getElementById('assetModalTitle').textContent = 'Sửa media';
                modal.show();
            } catch (error) { showAlert(error.message); }
        }
        if (remove) {
            const item = assets[Number(remove.dataset.delete)];
            if (!window.confirm('Xóa media asset này? Thao tác không thể hoàn tác.')) return;
            const id = val(item, 'assetId', 'AssetId');
            const type = val(item, 'ownerType', 'OwnerType');
            const url = val(item, 'url', 'Url');
            remove.disabled = true;
            try {
                const json = await fetchJson(`/Admin/MovieAsset/${id}?ownerType=${encodeURIComponent(type)}&url=${encodeURIComponent(url || '')}`, { method: 'DELETE' });
                showAlert(json.message || 'Đã xóa media.', true);
                await reload();
            } catch (error) { showAlert(error.message || 'Xóa thất bại.'); }
            finally { remove.disabled = false; }
        }
    });

    form.addEventListener('submit', async event => {
        event.preventDefault();
        if (!form.reportValidity()) return;
        const id = document.getElementById('assetId').value;
        const selectedFile = assetFile.files[0];
        if (!id && !selectedFile) {
            assetFile.setCustomValidity('Vui lòng chọn file media.');
            assetFile.reportValidity();
            assetFile.setCustomValidity('');
            return;
        }

        saveButton.disabled = true;
        const originalButtonText = saveButton.innerHTML;
        saveButton.innerHTML = '<span class="spinner-border spinner-border-sm me-2" aria-hidden="true"></span>Đang lưu...';
        const data = new FormData();
        if (selectedFile) data.append('file', selectedFile);
        data.append('ownerType', ownerType.value);
        data.append('ownerId', ownerId.value);
        data.append('assetType', document.getElementById('assetType').value);
        data.append('sortOrder', document.getElementById('sortOrder').value || '0');
        if (id) data.append('oldUrl', document.getElementById('oldUrl').value);
        try {
            const json = await fetchJson(id ? `/Admin/MovieAsset/${id}/replace-file` : app.dataset.addUrl, { method: 'POST', body: data });
            modal.hide();
            showAlert(json.message || 'Lưu media thành công.', true);
            await reload();
        } catch (error) { showAlert(error.message || 'Không lưu được media.'); }
        finally {
            saveButton.disabled = false;
            saveButton.innerHTML = originalButtonText;
        }
    });

    reload();
})();
