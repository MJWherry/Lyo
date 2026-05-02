/**
 * comicViewer.js
 *
 * Handles keyboard, touch-swipe, and image navigation for the ComicViewer
 * Blazor component. Navigation within a chapter is driven entirely by JS so
 * the image swap is instant (no SignalR round-trip). Blazor is notified
 * asynchronously to keep state (page counter, slider) in sync.
 *
 * Public API (called via IJSObjectReference):
 *   initViewer(viewerId, dotNetRef)                 – attach listeners
 *   disposeViewer(viewerId)                         – remove listeners and clean up
 *   setPageUrl(viewerId, page, url)                 – register a prefetched page URL
 *   setPageState(viewerId, currentPage, totalPages) – sync navigation state
 *   clearUrlMap(viewerId)                           – clear URL map on chapter change
 *   prefetchImages(urls)                            – warm the browser image cache
 *
 * .NET callbacks (invoked from navigate()):
 *   OnJsCounterChanged(page)              – update counter/slider, no image load
 *   OnJsPageChanged(page, imageShown)     – full navigation (image load or sync)
 *   OnJsNextChapter() / OnJsPreviousChapter() – chapter boundary (debounced)
 */

const _instances = new Map();

// Strong references to prefetched Image objects keyed by URL.
// Without this, GC can discard the decoded image data and the browser
// would have to re-decode (or re-fetch) when the <img> element needs it.
const _prefetchedImages = new Map();

const SWIPE_THRESHOLD_PX  = 50;   // minimum horizontal distance to count as swipe
const SWIPE_MAX_VERTICAL  = 150;  // reject swipe if vertical drift exceeds this
const TAP_MAX_MOVE_PX     = 10;   // finger movement under this is a tap, not a swipe
const TAP_MAX_DURATION_MS = 300;  // touch shorter than this is a tap

const NAV_DEBOUNCE_MS = 300; // delay before notifying Blazor after rapid page changes


export function initViewer(viewerId, dotNetRef) {
    const el = document.getElementById(viewerId);
    if (!el) return;

    const state = {
        el,
        dotNetRef,
        // JS-side navigation state (mirrors Blazor's CurrentPage / EffectiveTotalPages)
        urlMap: {},       // pageNumber (int) → url (string)
        currentPage: 1,
        totalPages: 1,
        // touch tracking
        touchStartX: 0,
        touchStartY: 0,
        touchStartTime: 0,
        touchOnOverlay: false,
        // stored handlers for removeEventListener
        onKeyDown: null,
        onTouchStart: null,
        onTouchEnd: null,
        // debounce timer for Blazor notifications
        navDebounceTimer: null,
    };

    // @onclick was removed from the Blazor tap zones; JS handles all navigation.
    const leftZone  = el.querySelector('.comic-viewer__tap-zone--left');
    const rightZone = el.querySelector('.comic-viewer__tap-zone--right');
    if (leftZone)  leftZone.addEventListener('click', () => navigate(state, -1));
    if (rightZone) rightZone.addEventListener('click', () => navigate(state,  1));

    state.onKeyDown = (e) => {
        if (!el.contains(document.activeElement) && document.activeElement !== el) return;
        if (e.key === 'ArrowRight') navigate(state, 1);
        else if (e.key === 'ArrowLeft') navigate(state, -1);
    };
    document.addEventListener('keydown', state.onKeyDown);
    el.focus({ preventScroll: true });

    state.onTouchStart = (e) => {
        const touch = e.touches[0];
        state.touchStartX    = touch.clientX;
        state.touchStartY    = touch.clientY;
        state.touchStartTime = Date.now();
        state.touchOnOverlay = !!e.target.closest(
            '.comic-viewer__top-bar, .comic-viewer__bottom-bar'
        );
    };

    state.onTouchEnd = (e) => {
        if (state.touchOnOverlay) return;

        const touch  = e.changedTouches[0];
        const dx     = touch.clientX - state.touchStartX;
        const dy     = touch.clientY - state.touchStartY;
        const dt     = Date.now() - state.touchStartTime;
        const absDx  = Math.abs(dx);
        const absDy  = Math.abs(dy);

        if (absDx >= SWIPE_THRESHOLD_PX && absDy <= SWIPE_MAX_VERTICAL) {
            e.preventDefault();
            navigate(state, dx > 0 ? -1 : 1); // swipe right = previous, left = next
            return;
        }

        if (absDx <= TAP_MAX_MOVE_PX && absDy <= TAP_MAX_MOVE_PX && dt <= TAP_MAX_DURATION_MS) {
            const relX = touch.clientX / el.offsetWidth;
            if (relX < 0.35) {
                e.preventDefault();
                navigate(state, -1);
            } else if (relX > 0.65) {
                e.preventDefault();
                navigate(state, 1);
            }
            // centre tap: let the native click reach the Blazor @onclick handler
        }
    };

    el.addEventListener('touchstart', state.onTouchStart, { passive: true });
    el.addEventListener('touchend',   state.onTouchEnd,   { passive: false });

    _instances.set(viewerId, state);
}

export function disposeViewer(viewerId) {
    const state = _instances.get(viewerId);
    if (!state) return;

    document.removeEventListener('keydown', state.onKeyDown);
    state.el.removeEventListener('touchstart', state.onTouchStart);
    state.el.removeEventListener('touchend',   state.onTouchEnd);
    clearTimeout(state.navDebounceTimer);

    for (const url of Object.values(state.urlMap))
        _prefetchedImages.delete(url);

    _instances.delete(viewerId);
}

/**
 * Register the resolved URL for an upcoming page so JS can swap img.src instantly
 * on navigation. Also warms the browser image cache and keeps a strong Image reference
 * so the browser retains the decoded frame in memory (preventing re-decode on display).
 *
 * Only called for upcoming pages — never for the currently-displayed page — to avoid
 * triggering a browser cache revalidation on the visible image.
 */
export function setPageUrl(viewerId, page, url) {
    const state = _instances.get(viewerId);
    if (!state || !url) return;
    state.urlMap[page] = url;

    if (!url.startsWith('data:') && !_prefetchedImages.has(url)) {
        const img = new Image();
        img.src = url;
        _prefetchedImages.set(url, img);
    }
}

/** Keep JS in sync with Blazor's CurrentPage and EffectiveTotalPages. */
export function setPageState(viewerId, currentPage, totalPages) {
    const state = _instances.get(viewerId);
    if (!state) return;
    state.currentPage = currentPage;
    state.totalPages  = totalPages;
}

/** Clear the URL map and release prefetch image refs when the chapter changes. */
export function clearUrlMap(viewerId) {
    const state = _instances.get(viewerId);
    if (!state) return;
    for (const url of Object.values(state.urlMap))
        _prefetchedImages.delete(url);
    state.urlMap = {};
}

/**
 * Warm the browser image cache for upcoming pages. new Image() goes through
 * the same image pipeline the <img> element uses, so the next page renders
 * from cache with zero network wait. Data URIs are skipped (already in memory).
 */
export function prefetchImages(urls) {
    for (const url of urls) {
        if (!url || url.startsWith('data:')) continue;
        new Image().src = url;
    }
}

/**
 * Navigate by `delta` pages (+1 = next, -1 = previous).
 *
 * Fast path (URL in urlMap): JS immediately swaps the <img> src so the image
 * appears before the SignalR round-trip completes. Blazor is notified async.
 *
 * Slow path (URL not yet resolved or chapter boundary): falls back to Blazor.
 */
function navigate(state, delta) {
    const newPage = state.currentPage + delta;

    if (delta > 0 && state.currentPage >= state.totalPages) {
        debounceBlazorCall(state, () => state.dotNetRef.invokeMethodAsync('OnJsNextChapter'));
        return;
    }
    if (delta < 0 && state.currentPage <= 1) {
        debounceBlazorCall(state, () => state.dotNetRef.invokeMethodAsync('OnJsPreviousChapter'));
        return;
    }

    // Advance JS page counter immediately so rapid presses chain to the right page.
    state.currentPage = newPage;

    // Update the counter and slider immediately on every keypress via a lightweight
    // call that does NOT trigger an image load.
    state.dotNetRef.invokeMethodAsync('OnJsCounterChanged', newPage);

    // Debounce the actual image swap — fires only once the user pauses.
    const capturedPage = newPage;
    debounceBlazorCall(state, () => {
        const url = state.urlMap[capturedPage];
        if (url) {
            // Fast path: URL already in JS cache — swap img.src directly.
            const img = state.el.querySelector('.comic-viewer__page-img');
            if (img) img.src = url;
            // Tell Blazor the image was already shown so it syncs its own URL
            // tracking and kicks off prefetching for the next pages.
            state.dotNetRef.invokeMethodAsync('OnJsPageChanged', capturedPage, true);
        } else {
            // Slow path: ask Blazor to fetch and display the image.
            state.dotNetRef.invokeMethodAsync('OnJsPageChanged', capturedPage, false);
        }
    });
}

function debounceBlazorCall(state, fn) {
    clearTimeout(state.navDebounceTimer);
    state.navDebounceTimer = setTimeout(fn, NAV_DEBOUNCE_MS);
}
