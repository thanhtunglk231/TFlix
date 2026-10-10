// TFlix Global Scripts - Toast & Star Rating System

// Toast Notification
window.showTFlixToast = function (message, iconClass = 'bi-check-circle-fill text-warning') {
    let container = document.getElementById('toastContainer');
    if (!container) {
        container = document.createElement('div');
        container.id = 'toastContainer';
        container.className = 'tflix-toast-container';
        document.body.appendChild(container);
    }
    const toast = document.createElement('div');
    toast.className = 'tflix-toast';
    toast.innerHTML = `<i class="bi ${iconClass}"></i><span>${message}</span>`;
    container.appendChild(toast);
    setTimeout(() => toast.classList.add('show'), 20);
    setTimeout(() => {
        toast.classList.remove('show');
        setTimeout(() => toast.remove(), 350);
    }, 3200);
};

// =========================================================================
// RATING HELPER & DISPLAY SYNCHRONIZER
// =========================================================================
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
                        return;
                    }

                    const res = await resp.json();
                    if (res && res.success) {
                        const newAvg = parseFloat(res.data?.averageRating || res.data?.AverageRating || 0);
                        const newAvgText = newAvg > 0 ? newAvg.toFixed(1) : starVal.toFixed(1);

                        popover.dataset.currentRating = starVal;
                        hintEl.innerHTML = `<span class="text-success fw-bold"><i class="bi bi-check-circle-fill me-1"></i>Đã đánh giá ${starVal}★!</span>`;
                        window.showTFlixToast(`Đã đánh giá ${starVal} sao thành công!`, 'bi-star-fill text-warning');

                        updateRatingDisplays(movieId, newAvgText);

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
    hintEl.textContent = 'Chọn từ 1 đến 5 sao';
    hintEl.classList.remove('hint-highlight');

    // Thử tải trước điểm đã đánh giá của người dùng
    fetch(`/Film/GetRating?movieId=${movieId}`)
        .then(r => r.json())
        .then(res => {
            if (res && res.success && res.data) {
                const uRating = parseInt(res.data.userRating || res.data.UserRating || 0, 10);
                if (uRating > 0 && popover.dataset.movieId == movieId) {
                    popover.dataset.currentRating = uRating;
                    hintEl.textContent = `Bạn đã đánh giá ${uRating} sao`;
                    highlightPopoverStars(uRating);
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

// Lắng nghe sự kiện click trên toàn trang: Bấm vào .card-rating-tag thì mở popup
document.addEventListener('click', function (e) {
    const ratingTag = e.target.closest('.card-rating-tag');
    if (ratingTag) {
        e.preventDefault();
        e.stopPropagation();
        window.openRatingPopover(ratingTag);
        return;
    }

    // Bấm ra ngoài popover thì tự đóng
    const popover = document.getElementById('tflix-rating-popover');
    if (popover && popover.style.display !== 'none' && !popover.contains(e.target)) {
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
            return;
        }

        const data = await response.json();
        if (data && data.success) {
            const newAvg = parseFloat(data.data?.averageRating || data.data?.AverageRating || 0);
            const newAvgText = newAvg > 0 ? newAvg.toFixed(1) : ratingVal.toFixed(1);
            window.showTFlixToast(`Đã đánh giá ${ratingVal} sao thành công!`, 'bi-star-fill text-warning');

            updateRatingDisplays(movieId, newAvgText);
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
