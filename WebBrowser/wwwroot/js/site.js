// =========================================================================
// TFLIX CINEMATIC PAGE TRANSITION & TOP PROGRESS BAR
// =========================================================================
(function () {
    let progressBar = document.getElementById('tflixPageProgressBar');
    if (!progressBar) {
        progressBar = document.createElement('div');
        progressBar.id = 'tflixPageProgressBar';
        document.body.prepend(progressBar);
    }

    let progressInterval = null;
    let currentProgress = 0;

    window.tflixProgressBar = {
        start: function () {
            clearInterval(progressInterval);
            currentProgress = 15;
            progressBar.classList.add('active');
            progressBar.style.width = currentProgress + '%';
            progressBar.style.opacity = '1';

            progressInterval = setInterval(() => {
                if (currentProgress < 85) {
                    currentProgress += Math.random() * 12 + 5;
                    progressBar.style.width = Math.min(currentProgress, 88) + '%';
                }
            }, 180);
        },
        set: function (percent) {
            currentProgress = Math.max(0, Math.min(100, percent));
            progressBar.style.width = currentProgress + '%';
        },
        done: function () {
            clearInterval(progressInterval);
            progressBar.style.width = '100%';
            setTimeout(() => {
                progressBar.style.opacity = '0';
                setTimeout(() => {
                    progressBar.classList.remove('active');
                    progressBar.style.width = '0%';
                }, 300);
            }, 200);
        }
    };

    // Tự động kích hoạt thanh tiến trình và hiệu ứng mượt mà khi click chuyển trang nội bộ
    document.addEventListener('click', function (e) {
        const anchor = e.target.closest('a');
        if (!anchor) return;

        const href = anchor.getAttribute('href');
        const target = anchor.getAttribute('target');

        if (!href || href.startsWith('#') || href.startsWith('javascript:') || href.startsWith('mailto:') || href.startsWith('tel:')) return;
        if (target === '_blank' || anchor.hasAttribute('download')) return;
        if (anchor.getAttribute('data-bs-toggle') || anchor.getAttribute('data-bs-target') || anchor.classList.contains('no-transition')) return;
        if (e.ctrlKey || e.metaKey || e.shiftKey || e.altKey) return;

        const currentUrl = window.location.pathname + window.location.search;
        if (href === currentUrl || href === window.location.href) return;

        const isInternal = href.startsWith('/') || href.startsWith(window.location.origin);
        if (isInternal) {
            document.body.classList.add('page-transitioning');
            window.tflixProgressBar.start();
        }
    });

    window.addEventListener('pageshow', function () {
        document.body.classList.remove('page-transitioning');
        window.tflixProgressBar.done();
    });
})();

// =========================================================================
// UNICODE DECODER HELPER
// =========================================================================
function decodeUnicodeString(str) {
    if (!str || typeof str !== 'string') return str || '';
    try {
        const txt = document.createElement('textarea');
        txt.innerHTML = str;
        return txt.value;
    } catch (e) {
        return str;
    }
}
window.decodeUnicodeString = decodeUnicodeString;

// =========================================================================
// PREMIUM OTT TOAST NOTIFICATION SYSTEM
// =========================================================================
window.showTFlixToast = function (message, type = 'info', title = null) {
    let container = document.getElementById('toastContainer');
    if (!container) {
        container = document.createElement('div');
        container.id = 'toastContainer';
        container.className = 'tflix-toast-container';
        document.body.appendChild(container);
    }

    // Chuẩn hóa loại toast (type)
    let toastType = 'info';
    let iconClass = 'bi-info-circle-fill';
    let defaultTitle = 'Thông báo';

    const t = String(type).toLowerCase();
    if (t === 'success' || t.includes('success')) {
        toastType = 'success';
        iconClass = 'bi-check-circle-fill';
        defaultTitle = 'Thành công';
    } else if (t === 'error' || t === 'danger' || t.includes('danger') || t.includes('error')) {
        toastType = 'error';
        iconClass = 'bi-exclamation-triangle-fill';
        defaultTitle = 'Có lỗi xảy ra';
    } else if (t === 'warning' || t.includes('warning')) {
        toastType = 'warning';
        iconClass = 'bi-exclamation-circle-fill';
        defaultTitle = 'Cảnh báo';
    } else if (t.includes('bi-')) {
        // Cho phép truyền thẳng class icon tùy chọn
        iconClass = type;
        if (t.includes('check')) { toastType = 'success'; defaultTitle = 'Thành công'; }
        else if (t.includes('star')) { toastType = 'warning'; defaultTitle = 'Đánh giá'; }
        else if (t.includes('heart')) { toastType = 'success'; defaultTitle = 'Yêu thích'; }
        else { toastType = 'info'; defaultTitle = 'Thông báo'; }
    }

    const toastTitle = title || defaultTitle;
    const cleanMessage = decodeUnicodeString(message);

    const toast = document.createElement('div');
    toast.className = `tflix-toast toast-${toastType}`;
    toast.innerHTML = `
        <div class="tflix-toast-icon-wrap">
            <i class="bi ${iconClass}"></i>
        </div>
        <div class="tflix-toast-content">
            <div class="tflix-toast-title">${toastTitle}</div>
            <div class="tflix-toast-message">${cleanMessage}</div>
        </div>
        <button type="button" class="tflix-toast-close" aria-label="Đóng">&times;</button>
        <div class="tflix-toast-progress"></div>
    `;

    container.appendChild(toast);

    // Kích hoạt animation trượt vào
    requestAnimationFrame(() => {
        toast.classList.add('show');
    });

    let dismissTimer = null;
    const closeToast = () => {
        clearTimeout(dismissTimer);
        toast.classList.remove('show');
        toast.classList.add('hide');
        setTimeout(() => toast.remove(), 350);
    };

    const closeBtn = toast.querySelector('.tflix-toast-close');
    if (closeBtn) closeBtn.addEventListener('click', closeToast);

    // Tự động đóng sau 3.5s
    dismissTimer = setTimeout(closeToast, 3500);

    // Tạm dừng khi rê chuột vào
    toast.addEventListener('mouseenter', () => {
        clearTimeout(dismissTimer);
        const bar = toast.querySelector('.tflix-toast-progress');
        if (bar) bar.style.animationPlayState = 'paused';
    });
    toast.addEventListener('mouseleave', () => {
        dismissTimer = setTimeout(closeToast, 2000);
        const bar = toast.querySelector('.tflix-toast-progress');
        if (bar) bar.style.animationPlayState = 'running';
    });
};


function triggerLoginModalIfAvailable() {
    if (typeof showLoginModal === 'function') {
        showLoginModal();
    } else {
        const modalEl = document.getElementById('loginRequiredModal');
        if (modalEl && typeof bootstrap !== 'undefined') {
            bootstrap.Modal.getOrCreateInstance(modalEl).show();
        }
    }
}

// =========================================================================
// RATING HELPER & DISPLAY SYNCHRONIZER
// =========================================================================
function saveUserRating(movieId, ratingVal) {
    if (!movieId || !ratingVal) return;
    try {
        const key = 'tflix_user_ratings';
        const ratings = JSON.parse(localStorage.getItem(key) || '{}');
        ratings[String(movieId)] = parseInt(ratingVal, 10);
        localStorage.setItem(key, JSON.stringify(ratings));
    } catch (e) {}
}

function getUserRating(movieId) {
    if (!movieId) return 0;
    try {
        const key = 'tflix_user_ratings';
        const ratings = JSON.parse(localStorage.getItem(key) || '{}');
        return parseInt(ratings[String(movieId)], 10) || 0;
    } catch (e) {
        return 0;
    }
}

function applyStarsToWidget(widget, ratingVal) {
    if (!widget) return;
    const allStars = widget.querySelectorAll('.btn-card-star, .star-btn, .star-btn-sm');
    allStars.forEach(btn => {
        const s = parseInt(btn.dataset.star, 10);
        const icon = btn.querySelector('i');
        if (s <= ratingVal) {
            btn.classList.add('active');
            if (icon) {
                icon.classList.remove('bi-star');
                icon.classList.add('bi-star-fill');
            }
        } else {
            btn.classList.remove('active');
            if (icon) {
                icon.classList.remove('bi-star-fill');
                icon.classList.add('bi-star');
            }
        }
    });
}

window.saveUserRating = saveUserRating;
window.getUserRating = getUserRating;
window.applyStarsToWidget = applyStarsToWidget;

window.syncAllCardRatingWidgets = function () {
    // 1. Đồng bộ các widget sao trên card phim ở trang chủ và danh sách
    document.querySelectorAll('.card-stars-widget[data-movie-id], [data-movie-id] .card-stars-widget').forEach(widget => {
        const movieId = widget.dataset.movieId || getMovieIdFromElement(widget);
        const uRating = getUserRating(movieId);
        if (uRating > 0) {
            applyStarsToWidget(widget, uRating);
        }
    });

    // 2. Đồng bộ widget sao trên trang xem phim Film/Watch nếu chưa có server data
    const watchWidget = document.getElementById('starRatingContainer');
    if (watchWidget) {
        const movieId = watchWidget.dataset.movieId || getMovieIdFromElement(watchWidget);
        const currentServerRating = parseInt(watchWidget.dataset.userRating, 10) || 0;
        const uRating = currentServerRating > 0 ? currentServerRating : getUserRating(movieId);
        if (uRating > 0) {
            watchWidget.dataset.userRating = uRating;
            applyStarsToWidget(watchWidget, uRating);
            const feedbackText = document.getElementById('ratingFeedbackText');
            if (feedbackText) {
                feedbackText.innerHTML = `<span class="text-warning"><i class="bi bi-check-circle-fill me-1"></i>Bạn đã đánh giá ${uRating} sao</span>`;
            }
        }
    }

    // 3. Đồng bộ widget sao trên trang chi tiết Preview
    const previewWidget = document.getElementById('previewStarRating');
    if (previewWidget) {
        const movieId = previewWidget.dataset.movieId || getMovieIdFromElement(previewWidget);
        const currentServerRating = parseInt(previewWidget.dataset.userRating, 10) || 0;
        const uRating = currentServerRating > 0 ? currentServerRating : getUserRating(movieId);
        if (uRating > 0) {
            previewWidget.dataset.userRating = uRating;
            applyStarsToWidget(previewWidget, uRating);
            const pFeedback = document.getElementById('previewRatingFeedback');
            if (pFeedback) {
                pFeedback.innerHTML = `<span class="text-warning"><i class="bi bi-check-circle-fill me-1"></i>Bạn đã đánh giá ${uRating} sao</span>`;
            }
        }
    }
};

function getMovieIdFromElement(el) {
    if (!el) return null;
    if (el.dataset && el.dataset.movieId) return el.dataset.movieId;
    if (el.dataset && el.dataset.id) return el.dataset.id;

    // Tìm trong phần tử cha gần nhất có data-movie-id hoặc data-id
    const parentWithId = el.closest('[data-movie-id], [data-id]');
    if (parentWithId) {
        return parentWithId.dataset.movieId || parentWithId.dataset.id;
    }

    // Tìm trong thẻ phim chứa link watch url
    const card = el.closest('.movie-card-item, .movie-card, .single-card, .card, .film-card') || el.parentElement;
    if (card) {
        const link = card.querySelector('a[href*="id="], a[data-url*="id="]');
        if (link) {
            const url = link.getAttribute('href') || link.getAttribute('data-url') || '';
            const match = url.match(/[?&]id=(\d+)/i);
            if (match) return match[1];
        }
    }
    return null;
}

function updateRatingDisplays(movieId, newAvgText) {
    if (!movieId) return;
    const mIdStr = String(movieId);

    // 1. Cập nhật thẻ rating đang active
    if (currentActiveRatingTag) {
        const span = currentActiveRatingTag.querySelector('.rating-val, .rating-num');
        if (span) span.textContent = newAvgText;
        currentActiveRatingTag.setAttribute('title', `Đánh giá: ${newAvgText} / 5`);
    }

    // 2. Cập nhật mọi thẻ phim trên trang có movieId này
    const selectors = [
        `[data-movie-id="${mIdStr}"] .card-rating-tag`,
        `[data-id="${mIdStr}"] .card-rating-tag`,
        `.card-rating-tag[data-movie-id="${mIdStr}"]`,
        `[data-id="${mIdStr}"] .movie-meta span:first-child`,
        `[data-movie-id="${mIdStr}"] .rating-val`
    ];

    document.querySelectorAll(selectors.join(', ')).forEach(el => {
        const span = el.querySelector ? el.querySelector('.rating-val, .rating-num') : null;
        if (span) {
            span.textContent = newAvgText;
        } else if (el.classList && el.classList.contains('rating-val')) {
            el.textContent = newAvgText;
        } else if (el.innerHTML && el.classList && el.classList.contains('card-rating-tag')) {
            el.innerHTML = `<i class="bi bi-star-fill text-warning"></i> <span class="rating-val">${newAvgText}</span>`;
        }
    });

    // 3. Cập nhật modal quick view nếu đang mở
    const modalRating = document.getElementById('modalRating');
    const modalWidget = document.getElementById('modalStarWidget');
    if (modalRating && modalWidget && String(modalWidget.dataset.movieId) === mIdStr) {
        modalRating.textContent = newAvgText;
    }

    // 4. Cập nhật trên trang xem phim Film/Watch nếu đang mở
    const badgeAvgRating = document.getElementById('badgeAvgRating');
    const ratingAvgDisplay = document.getElementById('ratingAvgDisplay');
    if (badgeAvgRating) badgeAvgRating.textContent = newAvgText;
    if (ratingAvgDisplay) ratingAvgDisplay.textContent = newAvgText;
}

// =========================================================================
// FLOATING 5-STAR RATING POPOVER (NHẤN VÀO CARD-RATING-TAG)
// =========================================================================
const ratingDescriptions = {
    1: '1★ Dở tệ',
    2: '2★ Dưới trung bình',
    3: '3★ Bình thường',
    4: '4★ Phim hay',
    5: '5★ Tuyệt phẩm xuất sắc!'
};

let currentActiveRatingTag = null;

function highlightPopoverStars(activeCount) {
    const popover = document.getElementById('tflix-rating-popover');
    if (!popover) return;
    const starBtns = popover.querySelectorAll('.popover-star-btn');
    starBtns.forEach(btn => {
        const val = parseInt(btn.dataset.star, 10);
        const icon = btn.querySelector('i');
        if (val <= activeCount) {
            btn.classList.add('hovered');
            if (icon) {
                icon.classList.remove('bi-star');
                icon.classList.add('bi-star-fill');
            }
        } else {
            btn.classList.remove('hovered');
            if (icon) {
                icon.classList.remove('bi-star-fill');
                icon.classList.add('bi-star');
            }
        }
    });
}

function getRatingPopover() {
    let popover = document.getElementById('tflix-rating-popover');
    if (!popover) {
        popover = document.createElement('div');
        popover.id = 'tflix-rating-popover';
        popover.className = 'tflix-rating-popover';
        popover.innerHTML = `
            <div class="rating-popover-arrow"></div>
            <div class="rating-popover-header">
                <span class="rating-popover-title"><i class="bi bi-star-fill text-warning me-1"></i>Đánh giá phim</span>
                <button type="button" class="rating-popover-close" aria-label="Đóng">&times;</button>
            </div>
            <div class="rating-popover-stars">
                <button type="button" class="popover-star-btn" data-star="1" title="1 sao - Dở tệ"><i class="bi bi-star"></i></button>
                <button type="button" class="popover-star-btn" data-star="2" title="2 sao - Dưới trung bình"><i class="bi bi-star"></i></button>
                <button type="button" class="popover-star-btn" data-star="3" title="3 sao - Bình thường"><i class="bi bi-star"></i></button>
                <button type="button" class="popover-star-btn" data-star="4" title="4 sao - Phim hay"><i class="bi bi-star"></i></button>
                <button type="button" class="popover-star-btn" data-star="5" title="5 sao - Tuyệt phẩm"><i class="bi bi-star"></i></button>
            </div>
            <div class="rating-popover-hint">Chọn từ 1 đến 5 sao</div>
        `;
        document.body.appendChild(popover);

        // Đóng popover khi nhấn nút X
        popover.querySelector('.rating-popover-close').addEventListener('click', function (e) {
            e.stopPropagation();
            closeRatingPopover();
        });

        // Tương tác hover và click trên 5 ngôi sao trong popover
        const starsContainer = popover.querySelector('.rating-popover-stars');
        const hintEl = popover.querySelector('.rating-popover-hint');
        const starBtns = popover.querySelectorAll('.popover-star-btn');

        starBtns.forEach(btn => {
            btn.addEventListener('mouseenter', function () {
                const s = parseInt(this.dataset.star, 10);
                hintEl.textContent = ratingDescriptions[s] || `${s} sao`;
                hintEl.classList.add('hint-highlight');
                highlightPopoverStars(s);
            });

            btn.addEventListener('click', async function (e) {
                e.stopPropagation();
                e.preventDefault();
                const starVal = parseInt(this.dataset.star, 10);
                const movieId = popover.dataset.movieId;
                if (!movieId) return;

                hintEl.innerHTML = `<span class="text-warning fw-bold"><i class="bi bi-hourglass-split me-1"></i>Đang lưu ${starVal} sao...</span>`;
                starBtns.forEach(b => b.disabled = true);

                try {
                    const resp = await fetch('/Film/RateMovie', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({
                            movieId: parseInt(movieId, 10),
                            rating: starVal
                        })
                    });

                    if (resp.status === 401) {
                        hintEl.innerHTML = `<span class="text-danger small"><i class="bi bi-lock-fill me-1"></i>Cần <a href="/Auth" class="text-warning text-decoration-underline">đăng nhập</a></span>`;
                        window.showTFlixToast('Vui lòng đăng nhập để đánh giá phim!', 'bi-person-lock text-warning');
                        starBtns.forEach(b => b.disabled = false);
                        triggerLoginModalIfAvailable();
                        return;
                    }

                    const res = await resp.json();
                    if (res && res.success) {
                        const newAvg = parseFloat(res.data?.averageRating || res.data?.AverageRating || 0);
                        const newAvgText = newAvg > 0 ? newAvg.toFixed(1) : starVal.toFixed(1);

                        saveUserRating(movieId, starVal);
                        popover.dataset.currentRating = starVal;
                        hintEl.innerHTML = `<span class="text-success fw-bold"><i class="bi bi-check-circle-fill me-1"></i>Đã đánh giá ${starVal}★!</span>`;
                        window.showTFlixToast(`Đã đánh giá ${starVal} sao thành công!`, 'bi-star-fill text-warning');

                        updateRatingDisplays(movieId, newAvgText);
                        window.syncAllCardRatingWidgets();

                        setTimeout(() => {
                            closeRatingPopover();
                        }, 750);
                    } else {
                        hintEl.textContent = res.message || 'Không thể lưu đánh giá.';
                        starBtns.forEach(b => b.disabled = false);
                    }
                } catch (err) {
                    console.error('Lỗi khi đánh giá phim:', err);
                    hintEl.textContent = 'Lỗi kết nối máy chủ.';
                    starBtns.forEach(b => b.disabled = false);
                }
            });
        });

        starsContainer.addEventListener('mouseleave', function () {
            const currentRating = parseInt(popover.dataset.currentRating, 10) || 0;
            if (currentRating > 0) {
                hintEl.textContent = `Bạn đã đánh giá ${currentRating} sao`;
                hintEl.classList.remove('hint-highlight');
                highlightPopoverStars(currentRating);
            } else {
                hintEl.textContent = 'Chọn từ 1 đến 5 sao';
                hintEl.classList.remove('hint-highlight');
                highlightPopoverStars(0);
            }
        });
    }
    return popover;
}

window.openRatingPopover = function (ratingTag) {
    const movieId = getMovieIdFromElement(ratingTag);
    if (!movieId) {
        console.warn('Không tìm thấy movieId cho thẻ đánh giá:', ratingTag);
        return;
    }

    // Toggle: Nhấn lại đúng thẻ đang mở thì đóng lại
    if (currentActiveRatingTag === ratingTag) {
        closeRatingPopover();
        return;
    }

    closeRatingPopover();
    currentActiveRatingTag = ratingTag;
    ratingTag.classList.add('popover-active');

    const popover = getRatingPopover();
    popover.dataset.movieId = movieId;
    popover.dataset.currentRating = '0';

    // Reset trạng thái các nút sao trong popover
    const starBtns = popover.querySelectorAll('.popover-star-btn');
    starBtns.forEach(b => {
        b.disabled = false;
        b.classList.remove('active', 'hovered');
        const icon = b.querySelector('i');
        if (icon) {
            icon.classList.remove('bi-star-fill');
            icon.classList.add('bi-star');
        }
    });

    const hintEl = popover.querySelector('.rating-popover-hint');
    const cachedRating = getUserRating(movieId);
    if (cachedRating > 0) {
        popover.dataset.currentRating = cachedRating;
        hintEl.textContent = `Bạn đã đánh giá ${cachedRating} sao`;
        highlightPopoverStars(cachedRating);
    } else {
        hintEl.textContent = 'Chọn từ 1 đến 5 sao';
        hintEl.classList.remove('hint-highlight');
    }

    // Thử tải trước điểm đã đánh giá của người dùng từ server
    fetch(`/Film/GetRating?movieId=${movieId}`)
        .then(r => r.json())
        .then(res => {
            if (res && res.success && res.data) {
                const uRating = parseInt(res.data.userRating || res.data.UserRating || 0, 10);
                if (uRating > 0) {
                    saveUserRating(movieId, uRating);
                    if (popover.dataset.movieId == movieId) {
                        popover.dataset.currentRating = uRating;
                        hintEl.textContent = `Bạn đã đánh giá ${uRating} sao`;
                        highlightPopoverStars(uRating);
                    }
                    window.syncAllCardRatingWidgets();
                }
            }
        })
        .catch(() => {});

    // Định vị hiển thị Popover
    popover.style.display = 'block';
    popover.style.visibility = 'hidden';

    const rect = ratingTag.getBoundingClientRect();
    const popoverRect = popover.getBoundingClientRect();
    const popoverWidth = popoverRect.width || 240;
    const popoverHeight = popoverRect.height || 116;

    let top = rect.top - popoverHeight - 10;
    let arrowType = 'arrow-bottom';

    if (top < 10) {
        // Nếu không đủ khoảng trống phía trên màn hình, mở xuống dưới
        top = rect.bottom + 10;
        arrowType = 'arrow-top';
    }

    let left = rect.left + (rect.width / 2) - (popoverWidth / 2);
    if (left < 10) left = 10;
    if (left + popoverWidth > window.innerWidth - 10) {
        left = window.innerWidth - popoverWidth - 10;
    }

    // Định vị mũi tên trỏ vào thẻ
    const arrowEl = popover.querySelector('.rating-popover-arrow');
    if (arrowEl) {
        const arrowX = (rect.left + rect.width / 2) - left - 5;
        const boundedArrowX = Math.max(12, Math.min(popoverWidth - 22, arrowX));
        arrowEl.style.left = `${boundedArrowX}px`;
    }

    popover.className = `tflix-rating-popover ${arrowType}`;
    popover.style.top = `${top}px`;
    popover.style.left = `${left}px`;
    popover.style.visibility = 'visible';
};

window.closeRatingPopover = function () {
    const popover = document.getElementById('tflix-rating-popover');
    if (popover) {
        popover.style.display = 'none';
    }
    if (currentActiveRatingTag) {
        currentActiveRatingTag.classList.remove('popover-active');
        currentActiveRatingTag = null;
    }
};

// Chặn chuyển trang khi tương tác với các widget đánh giá sao, nhưng không chặn các nút sao hoạt động
document.addEventListener('click', function (e) {
    // 1. Nếu click bên trong floating popover đánh giá, để popover và các nút sao của nó xử lý bình thường!
    if (e.target.closest('#tflix-rating-popover, .tflix-rating-popover')) {
        return;
    }

    // Không chặn các nút sao tương tác chuyên biệt trên trang Xem phim (Film/Watch) hoặc trang Preview Details
    if (e.target.closest('#starRatingContainer, #previewStarRating, .star-btn, .star-btn-sm')) {
        return;
    }

    // 2. Nhấp vào thẻ badge đánh giá sao (.card-rating-tag) trên card phim -> mở Popover 5 sao
    const ratingTag = e.target.closest('.card-rating-tag');
    if (ratingTag) {
        e.preventDefault();
        e.stopPropagation();
        e.stopImmediatePropagation();
        window.openRatingPopover(ratingTag);
        return;
    }

    // 3. Nhấp vào nút sao nhanh trên card phim (.btn-card-star)
    const starBtn = e.target.closest('.btn-card-star');
    if (starBtn) {
        e.preventDefault();
        e.stopPropagation();
        e.stopImmediatePropagation();
        const starVal = parseInt(starBtn.dataset.star, 10);
        const movieId = getMovieIdFromElement(starBtn);
        if (movieId && starVal) {
            window.rateMovieFromCard(movieId, starVal, e, starBtn);
        }
        return;
    }

    // 4. Nhấp vào container của rating widget trên card phim -> chặn click lan truyền làm chuyển trang
    const isRatingWidget = e.target.closest('.card-quick-rating, .card-stars-widget');
    if (isRatingWidget && e.target.closest('.movie-card-item, .film-card, .preview-catalog-card, .slist-item, .item, a')) {
        e.stopPropagation();
        return;
    }
}, true); // useCapture = true: Bắt trước thẻ card cha để ngăn chuyển trang ngoài ý muốn

// Đóng popover khi nhấp ra ngoài (Bubbling phase)
document.addEventListener('click', function (e) {
    const popover = document.getElementById('tflix-rating-popover');
    if (popover && popover.style.display !== 'none' && !popover.contains(e.target) && !e.target.closest('.card-rating-tag')) {
        window.closeRatingPopover();
    }
});

// Nhấn Escape đóng popover
document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape') {
        window.closeRatingPopover();
    }
});

// Tự đóng khi cuộn trang hoặc đổi kích thước màn hình
window.addEventListener('resize', () => window.closeRatingPopover());
window.addEventListener('scroll', function () {
    const popover = document.getElementById('tflix-rating-popover');
    if (popover && popover.style.display !== 'none') {
        window.closeRatingPopover();
    }
}, { passive: true });

// =========================================================================
// GLOBAL STAR RATING HANDLER (HOVER CARD OVERLAY & WIDGETS)
// =========================================================================
window.rateMovieFromCard = async function (movieId, ratingVal, event, starBtn) {
    if (event) {
        event.stopPropagation();
        event.preventDefault();
    }
    if (!movieId || ratingVal < 1 || ratingVal > 5) return;

    const widget = starBtn ? starBtn.closest('.card-stars-widget, .quick-stars, .stars-picker') : null;
    if (widget) {
        const allStars = widget.querySelectorAll('.btn-card-star, .star-btn');
        allStars.forEach(btn => {
            const s = parseInt(btn.dataset.star, 10);
            if (s <= ratingVal) {
                btn.classList.add('active');
                const icon = btn.querySelector('i');
                if (icon) {
                    icon.classList.remove('bi-star');
                    icon.classList.add('bi-star-fill');
                }
            } else {
                btn.classList.remove('active');
                const icon = btn.querySelector('i');
                if (icon) {
                    icon.classList.remove('bi-star-fill');
                    icon.classList.add('bi-star');
                }
            }
        });
    }

    try {
        const response = await fetch('/Film/RateMovie', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                movieId: parseInt(movieId, 10),
                rating: parseInt(ratingVal, 10)
            })
        });

        if (response.status === 401) {
            window.showTFlixToast('Vui lòng đăng nhập để đánh giá phim!', 'bi-person-lock text-warning');
            triggerLoginModalIfAvailable();
            return;
        }

        const data = await response.json();
        if (data && data.success) {
            const newAvg = parseFloat(data.data?.averageRating || data.data?.AverageRating || 0);
            const newAvgText = newAvg > 0 ? newAvg.toFixed(1) : ratingVal.toFixed(1);
            saveUserRating(movieId, ratingVal);
            window.showTFlixToast(`Đã đánh giá ${ratingVal} sao thành công!`, 'bi-star-fill text-warning');

            updateRatingDisplays(movieId, newAvgText);
            window.syncAllCardRatingWidgets();
        } else {
            window.showTFlixToast(data.message || 'Không thể lưu đánh giá.', 'bi-exclamation-triangle text-danger');
        }
    } catch (err) {
        console.error('Lỗi khi đánh giá phim:', err);
        window.showTFlixToast('Lỗi kết nối khi đánh giá phim.', 'bi-wifi-off text-danger');
    }
};

// Mouseover & Mouseout Preview for Card Stars
document.addEventListener('mouseover', function (e) {
    const starBtn = e.target.closest('.btn-card-star, .star-btn');
    if (!starBtn) return;
    const widget = starBtn.closest('.card-stars-widget, .quick-stars, .stars-picker');
    if (!widget) return;
    const hoverVal = parseInt(starBtn.dataset.star, 10);
    const allStars = widget.querySelectorAll('.btn-card-star, .star-btn');
    allStars.forEach(btn => {
        const val = parseInt(btn.dataset.star, 10);
        const icon = btn.querySelector('i');
        if (val <= hoverVal) {
            btn.classList.add('hovered');
            if (icon) {
                icon.classList.remove('bi-star');
                icon.classList.add('bi-star-fill');
            }
        } else {
            btn.classList.remove('hovered');
            if (!btn.classList.contains('active')) {
                if (icon) {
                    icon.classList.remove('bi-star-fill');
                    icon.classList.add('bi-star');
                }
            }
        }
    });
});

document.addEventListener('mouseout', function (e) {
    const starBtn = e.target.closest('.btn-card-star, .star-btn');
    if (!starBtn) return;
    const widget = starBtn.closest('.card-stars-widget, .quick-stars, .stars-picker');
    if (!widget) return;
    const allStars = widget.querySelectorAll('.btn-card-star, .star-btn');
    allStars.forEach(btn => {
        btn.classList.remove('hovered');
        const icon = btn.querySelector('i');
        if (btn.classList.contains('active')) {
            if (icon) {
                icon.classList.remove('bi-star');
                icon.classList.add('bi-star-fill');
            }
        } else {
            if (icon) {
                icon.classList.remove('bi-star-fill');
                icon.classList.add('bi-star');
            }
        }
    });
});

// Tự động khôi phục số sao đã đánh giá của người dùng khi tải trang xong
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', function () {
        if (window.syncAllCardRatingWidgets) {
            window.syncAllCardRatingWidgets();
        }
    });
} else {
    if (window.syncAllCardRatingWidgets) {
        window.syncAllCardRatingWidgets();
    }
}

