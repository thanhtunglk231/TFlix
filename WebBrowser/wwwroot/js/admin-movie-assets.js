(() => {
    const app = document.getElementById('mediaAssetApp');
    if (!app) return;

    const modal = new bootstrap.Modal(document.getElementById('assetModal'));
    const form = document.getElementById('assetForm');
    const ownerType = document.getElementById('ownerType');
    const ownerId = document.getElementById('ownerId');
    const rows = document.getElementById('assetRows');
    const loadMoreButton = document.getElementById('btnLoadMore');
    const owners = { MOVIE: [], SERIES: [], EPISODE: [] };
    let assets = [];
    let cursor = null;

    const val = (object, ...keys) => keys.map(key => object?.[key]).find(item => item !== undefined && item !== null);
    const escapeHtml = text => String(text ?? '').replace(/[&<>'"]/g, character => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', "'": '&#39;', '"': '&quot;' })[character]);
    const showAlert = (message, success = false) => {
        const element = document.getElementById('assetAlert');
        element.className = `alert alert-${success ? 'success' : 'danger'}`;
        element.textContent = message;
    };

    function fillOwners(selected) {
        ownerId.innerHTML = '<option value="">Chọn nội dung</option>' + owners[ownerType.value].map(item => {
            const id = val(item, 'ownerId', 'OwnerId');
            return `<option value="${id}" ${String(id) === String(selected) ? 'selected' : ''}>${escapeHtml(val(item, 'ownerTitle', 'OwnerTitle'))}</option>`;
        }).join('');
    }

    function render() {
        document.getElementById('assetLoading').classList.add('d-none');
        document.getElementById('assetEmpty').classList.toggle('d-none', assets.length > 0);
        document.getElementById('assetTableWrap').classList.toggle('d-none', assets.length === 0);
        loadMoreButton.classList.toggle('d-none', !cursor?.hasMore);
        rows.innerHTML = assets.map((item, index) => {
            const url = val(item, 'url', 'Url');
            const type = val(item, 'assetType', 'AssetType');
            const isVideo = type === 'TRAILER' || /\.(mp4|webm|m3u8)(\?|$)/i.test(url || '');
            const preview = isVideo
                ? `<video class="asset-preview asset-preview-video" muted preload="metadata"><source src="${escapeHtml(url)}"></video>`
                : `<img class="asset-preview" src="${escapeHtml(url)}" alt="${escapeHtml(type)}" loading="lazy">`;
            return `<tr><td>${preview}</td><td class="asset-owner-title"><span class="badge text-bg-secondary mb-1">${escapeHtml(val(item, 'ownerType', 'OwnerType'))}</span><div>${escapeHtml(val(item, 'ownerTitle', 'OwnerTitle'))}</div></td><td>${escapeHtml(type)}</td><td>${escapeHtml(val(item, 'sortOrder', 'SortOrder') ?? 0)}</td><td class="text-end"><button class="btn btn-sm btn-outline-info me-2" data-edit="${index}">Sửa</button><button class="btn btn-sm btn-outline-danger" data-delete="${index}">Xóa</button></td></tr>`;
        }).join('');
    }

    async function loadOwners() {
        if (owners.MOVIE.length || owners.SERIES.length || owners.EPISODE.length) return;
        const response = await fetch(app.dataset.ownersUrl);
        const json = await response.json();
        if (!response.ok || !json.success) throw new Error(json.message || 'Không tải được danh sách nội dung.');
        owners.MOVIE = val(json.data, 'table', 'Table') || [];
        owners.SERIES = val(json.data, 'table1', 'Table1') || [];
        owners.EPISODE = val(json.data, 'table2', 'Table2') || [];
        fillOwners();
    }

    async function loadAssets(append = false) {
        loadMoreButton.disabled = true;
        let url = `${app.dataset.listUrl}?pageSize=10`;
        if (append && cursor) url += `&cursorOwnerType=${encodeURIComponent(cursor.ownerType)}&cursorAssetId=${cursor.assetId}`;
        const response = await fetch(url);
        const json = await response.json();
        if (!response.ok || !json.success) throw new Error(json.message || 'Không tải được media asset.');
        const page = val(json.data, 'table', 'Table') || [];
        const metadata = (val(json.data, 'table1', 'Table1') || [])[0];
        assets = append ? assets.concat(page) : page;
        cursor = metadata ? {
            ownerType: val(metadata, 'nextOwnerType', 'NextOwnerType'),
            assetId: val(metadata, 'nextAssetId', 'NextAssetId'),
            hasMore: Boolean(val(metadata, 'hasMore', 'HasMore'))
        } : null;
        loadMoreButton.disabled = false;
        render();
    }

    async function reload() {
        try { cursor = null; assets = []; await loadAssets(false); }
        catch (error) { document.getElementById('assetLoading').classList.add('d-none'); showAlert(error.message); }
    }

    document.getElementById('btnAddAsset').addEventListener('click', async () => {
        try {
            await loadOwners(); form.reset(); document.getElementById('assetId').value = ''; document.getElementById('oldUrl').value = '';
            fillOwners(); document.getElementById('assetModalTitle').textContent = 'Thêm media'; modal.show();
        } catch (error) { showAlert(error.message); }
    });
    ownerType.addEventListener('change', () => fillOwners());
    loadMoreButton.addEventListener('click', async () => { try { await loadAssets(true); } catch (error) { showAlert(error.message); loadMoreButton.disabled = false; } });

    rows.addEventListener('click', async event => {
        const edit = event.target.closest('[data-edit]');
        const remove = event.target.closest('[data-delete]');
        if (edit) {
            try { await loadOwners(); } catch (error) { showAlert(error.message); return; }
            const item = assets[edit.dataset.edit]; ownerType.value = val(item, 'ownerType', 'OwnerType'); fillOwners(val(item, 'ownerId', 'OwnerId'));
            document.getElementById('assetId').value = val(item, 'assetId', 'AssetId'); document.getElementById('oldUrl').value = val(item, 'url', 'Url');
            document.getElementById('assetType').value = val(item, 'assetType', 'AssetType'); document.getElementById('sortOrder').value = val(item, 'sortOrder', 'SortOrder') ?? 0;
            document.getElementById('assetModalTitle').textContent = 'Thay file media'; modal.show();
        }
        if (remove) {
            const item = assets[remove.dataset.delete]; if (!confirm('Xóa media asset này?')) return;
            const id = val(item, 'assetId', 'AssetId'), type = val(item, 'ownerType', 'OwnerType'), url = val(item, 'url', 'Url');
            const response = await fetch(`/Admin/MovieAsset/${id}?ownerType=${encodeURIComponent(type)}&url=${encodeURIComponent(url || '')}`, { method: 'DELETE' });
            const json = await response.json(); if (!response.ok || !json.success) return showAlert(json.message || 'Xóa thất bại.');
            showAlert(json.message || 'Đã xóa.', true); await reload();
        }
    });

    form.addEventListener('submit', async event => {
        event.preventDefault(); const button = document.getElementById('btnSaveAsset'); button.disabled = true;
        const data = new FormData(); data.append('file', document.getElementById('assetFile').files[0]); data.append('ownerType', ownerType.value); data.append('ownerId', ownerId.value);
        data.append('assetType', document.getElementById('assetType').value); data.append('sortOrder', document.getElementById('sortOrder').value || '0');
        const id = document.getElementById('assetId').value; if (id) data.append('oldUrl', document.getElementById('oldUrl').value);
        try {
            const response = await fetch(id ? `/Admin/MovieAsset/${id}/replace-file` : app.dataset.addUrl, { method: 'POST', body: data });
            const json = await response.json(); if (!response.ok || !json.success) throw new Error(json.message || 'Không lưu được media.');
            modal.hide(); showAlert(json.message || 'Lưu thành công.', true); await reload();
        } catch (error) { showAlert(error.message); } finally { button.disabled = false; }
    });

    reload();
})();
