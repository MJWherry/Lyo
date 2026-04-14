const _editorState = new WeakMap();

function getHosts(root) {
    return root ? [...root.querySelectorAll("[data-lyo-diff-editor]")] : [];
}

function getLineHeight(textarea) {
    const lineHeight = parseFloat(getComputedStyle(textarea).lineHeight || "22");
    return Number.isFinite(lineHeight) && lineHeight > 0 ? lineHeight : 22;
}

function scrollTextareaLineIntoView(textarea, lineIndex) {
    if (!textarea || lineIndex < 0) {
        return;
    }

    const lineHeight = getLineHeight(textarea);
    const targetTop = lineIndex * lineHeight;
    const targetBottom = targetTop + lineHeight;
    const currentTop = textarea.scrollTop;
    const currentBottom = currentTop + textarea.clientHeight;

    if (targetTop < currentTop) {
        textarea.scrollTop = targetTop;
    } else if (targetBottom > currentBottom) {
        textarea.scrollTop = targetBottom - textarea.clientHeight;
    }
}

function scrollTextareaMatch(textarea, startIndex, length) {
    if (!textarea || startIndex < 0 || length <= 0) {
        return;
    }

    textarea.focus();
    textarea.setSelectionRange(startIndex, startIndex + length);
    const before = textarea.value.slice(0, startIndex);
    const lineIndex = before.split("\n").length - 1;
    scrollTextareaLineIntoView(textarea, lineIndex);
}

function applyText(host, text) {
    const state = _editorState.get(host);
    if (!state) return;

    const next = text ?? "";
    if (document.activeElement !== state.textarea && state.textarea.value !== next) {
        state.textarea.value = next;
    }

    scheduleSyncHost(host);
}

function syncHost(host) {
    const state = _editorState.get(host);
    if (!state) return;

    const {textarea, gutter, highlight, content} = state;
    gutter.scrollTop = textarea.scrollTop;

    const x = -textarea.scrollLeft;
    const y = -textarea.scrollTop;
    const w = Math.max(textarea.scrollWidth, textarea.clientWidth);
    const h = Math.max(textarea.scrollHeight, textarea.clientHeight);
    highlight.style.transform = `translate(${x}px, ${y}px)`;
    highlight.style.width = `${w}px`;
    highlight.style.minHeight = `${h}px`;
    if (content) {
        content.style.transform = `translate(${x}px, ${y}px)`;
        content.style.width = `${w}px`;
        content.style.minHeight = `${h}px`;
    }
}

/** scrollWidth/scrollHeight can lag one frame after paste; sync twice. */
function scheduleSyncHost(host) {
    syncHost(host);
    requestAnimationFrame(() => {
        syncHost(host);
        requestAnimationFrame(() => syncHost(host));
    });
}

function wireHost(host, index, dotNetRef) {
    if (_editorState.has(host)) return;

    const textarea = host.querySelector('[data-role="textarea"]');
    const gutter = host.querySelector('[data-role="gutter"]');
    const highlight = host.querySelector('[data-role="highlight"]');
    const content = host.querySelector('[data-role="content"]');
    if (!textarea || !gutter || !highlight) return;

    const onScroll = () => scheduleSyncHost(host);
    const onInput = async () => {
        scheduleSyncHost(host);
        if (dotNetRef) {
            try {
                await dotNetRef.invokeMethodAsync("NotifyEditorInput", index, textarea.value);
            } catch {
                // Ignore callbacks during teardown or when the circuit is gone.
            }
        }
    };

    textarea.addEventListener("scroll", onScroll, {passive: true});
    textarea.addEventListener("input", onInput);

    _editorState.set(host, {textarea, gutter, highlight, content, onScroll, onInput, index});
    scheduleSyncHost(host);
}

let _splitPointerCleanup = null;

function wireSplitPointerDelegation(root, dotNetRef) {
    if (_splitPointerCleanup) {
        _splitPointerCleanup();
        _splitPointerCleanup = null;
    }

    if (!root || !dotNetRef)
        return;

    const onPointerDown = (e) => {
        const splitter = e.target?.closest?.(".lyo-textdiff-splitter");
        if (!splitter || !root.contains(splitter))
            return;

        e.preventDefault();
        e.stopPropagation();
        const h = splitter.closest("[data-lyo-textdiff-hsplit]");
        const v = splitter.closest("[data-lyo-textdiff-vsplit]");
        if (h)
            beginHorizontalSplitDrag(h, dotNetRef);
        else if (v)
            beginVerticalSplitDrag(v, dotNetRef);
    };

    root.addEventListener("pointerdown", onPointerDown, true);
    _splitPointerCleanup = () => root.removeEventListener("pointerdown", onPointerDown, true);
}

export function initializeEditors(root, dotNetRef, texts = []) {
    const hosts = getHosts(root);
    hosts.forEach((host, index) => wireHost(host, index, dotNetRef));
    hosts.forEach((host, index) => applyText(host, texts[index] ?? ""));
    wireSplitPointerDelegation(root, dotNetRef);
}

export function refreshEditors(root, texts = [], searchState = []) {
    const hosts = getHosts(root);
    hosts.forEach((host, index) => applyText(host, texts[index] ?? ""));
    hosts.forEach((host) => scheduleSyncHost(host));

    const activeMatch = Array.isArray(searchState) ? searchState[0] : null;
    if (activeMatch && Number.isInteger(activeMatch.editorIndex)) {
        scrollEditorMatchIntoView(root, activeMatch.editorIndex, activeMatch.startIndex ?? -1, activeMatch.length ?? 0);
    }
}

export function scrollEditorMatchIntoView(root, editorIndex, startIndex, length) {
    const host = getHosts(root)[editorIndex];
    const state = host ? _editorState.get(host) : null;
    if (!state) {
        return;
    }

    const before = state.textarea.value.slice(0, Math.max(0, startIndex));
    const lineIndex = before.split("\n").length - 1;
    scrollTextareaLineIntoView(state.textarea, lineIndex);
    scheduleSyncHost(host);
}

export function scrollEditorLineIntoView(root, editorIndex, lineIndex) {
    const host = getHosts(root)[editorIndex];
    const state = host ? _editorState.get(host) : null;
    if (!state) {
        return;
    }

    scrollTextareaLineIntoView(state.textarea, lineIndex);
    scheduleSyncHost(host);
}

export function scrollUnifiedRowIntoView(root, rowIndex) {
    if (!root || rowIndex < 0) {
        return;
    }

    const container = root.querySelector('[data-role="unified-scroll"]');
    if (!container) {
        return;
    }

    const row = container.querySelector(`[data-row-index="${rowIndex}"]`);
    row?.scrollIntoView({block: "nearest", inline: "nearest"});
}

export function scrollRenderedEditorRowIntoView(root, rowIndex) {
    if (!root || rowIndex < 0) {
        return;
    }

    const rows = root.querySelectorAll(`[data-editor-row-index="${rowIndex}"]`);
    rows.forEach((row) => row.scrollIntoView({block: "nearest", inline: "nearest"}));
}

export function getSplitPercent(element, clientX) {
    if (!element || typeof element.getBoundingClientRect !== "function")
        return 50;

    const rect = element.getBoundingClientRect();
    if (!rect || rect.width <= 0)
        return 50;

    const relativeX = clientX - rect.left;
    return (relativeX / rect.width) * 100;
}

/** Resolves the live horizontal split container under root (avoids stale Blazor ElementReference after view switches). */
export function getSplitPercentFromRoot(root, clientX) {
    return getSplitPercent(resolveHorizontalSplitRoot(root), clientX);
}

export function getVerticalSplitPercent(element, clientY) {
    if (!element || typeof element.getBoundingClientRect !== "function")
        return 50;

    const rect = element.getBoundingClientRect();
    if (!rect || rect.height <= 0)
        return 50;

    const relativeY = clientY - rect.top;
    return (relativeY / rect.height) * 100;
}

/** Resolves the live vertical split container under root (avoids stale Blazor ElementReference after view switches). */
export function getVerticalSplitPercentFromRoot(root, clientY) {
    return getVerticalSplitPercent(resolveVerticalSplitRoot(root), clientY);
}

let _activeSplitDragCleanup = null;

function clampSplitPercent(p) {
    return Math.max(25, Math.min(75, p));
}

/** Blazor may pass the split element directly; otherwise search under a parent or document. */
function resolveHorizontalSplitRoot(passed) {
    if (passed && typeof passed.getAttribute === "function" && passed.getAttribute("data-lyo-textdiff-hsplit") != null)
        return passed;
    if (passed?.querySelector)
        return passed.querySelector("[data-lyo-textdiff-hsplit]") ?? document.querySelector("[data-lyo-textdiff-hsplit]");
    return document.querySelector("[data-lyo-textdiff-hsplit]");
}

function resolveVerticalSplitRoot(passed) {
    if (passed && typeof passed.getAttribute === "function" && passed.getAttribute("data-lyo-textdiff-vsplit") != null)
        return passed;
    if (passed?.querySelector)
        return passed.querySelector("[data-lyo-textdiff-vsplit]") ?? document.querySelector("[data-lyo-textdiff-vsplit]");
    return document.querySelector("[data-lyo-textdiff-vsplit]");
}

/**
 * Document-level move/up so drag works even when the cursor leaves the splitter (Blazor @onmousemove on a parent is unreliable during drag).
 */
export function beginHorizontalSplitDrag(splitRootOrParent, dotNetRef) {
    if (_activeSplitDragCleanup) {
        _activeSplitDragCleanup();
        _activeSplitDragCleanup = null;
    }

    if (!resolveHorizontalSplitRoot(splitRootOrParent) || !dotNetRef)
        return;

    /** Re-query each move: Blazor re-renders after each percent update, detaching the cached node. */
    let rafId = 0;
    let pendingEvent = null;

    const flushMove = () => {
        rafId = 0;
        const e = pendingEvent;
        pendingEvent = null;
        if (!e) return;

        const splitEl = resolveHorizontalSplitRoot(splitRootOrParent);
        if (!splitEl) return;

        const rect = splitEl.getBoundingClientRect();
        if (rect.width <= 1) return;

        const p = clampSplitPercent(((e.clientX - rect.left) / rect.width) * 100);
        /** Update CSS only — Blazor re-renders on every % broke textarea wiring and pointer tracking. */
        splitEl.style.setProperty("--td-hsplit", `${p.toFixed(2)}%`);
    };

    const onMove = (e) => {
        pendingEvent = e;
        if (rafId) return;
        rafId = requestAnimationFrame(flushMove);
    };

    const onUp = (e) => {
        if (rafId) {
            cancelAnimationFrame(rafId);
            rafId = 0;
        }
        const splitEl = resolveHorizontalSplitRoot(splitRootOrParent);
        let finalP = 50;
        if (splitEl && e) {
            const rect = splitEl.getBoundingClientRect();
            if (rect.width > 1)
                finalP = clampSplitPercent(((e.clientX - rect.left) / rect.width) * 100);
        }

        if (splitEl)
            splitEl.style.setProperty("--td-hsplit", `${finalP.toFixed(2)}%`);

        pendingEvent = null;
        document.removeEventListener("mousemove", onMove, true);
        document.removeEventListener("mouseup", onUp, true);
        _activeSplitDragCleanup = null;
        dotNetRef.invokeMethodAsync("NotifyHorizontalSplitPercent", finalP).catch(() => {
        });
        dotNetRef.invokeMethodAsync("NotifySplitDragEnd").catch(() => {
        });
    };

    document.addEventListener("mousemove", onMove, true);
    document.addEventListener("mouseup", onUp, true);
    _activeSplitDragCleanup = onUp;
    dotNetRef.invokeMethodAsync("NotifySplitDragStart").catch(() => {
    });
}

export function beginVerticalSplitDrag(splitRootOrParent, dotNetRef) {
    if (_activeSplitDragCleanup) {
        _activeSplitDragCleanup();
        _activeSplitDragCleanup = null;
    }

    if (!resolveVerticalSplitRoot(splitRootOrParent) || !dotNetRef)
        return;

    let rafId = 0;
    let pendingEvent = null;

    const flushMove = () => {
        rafId = 0;
        const e = pendingEvent;
        pendingEvent = null;
        if (!e) return;

        const splitEl = resolveVerticalSplitRoot(splitRootOrParent);
        if (!splitEl) return;

        const rect = splitEl.getBoundingClientRect();
        if (rect.height <= 1) return;

        const p = clampSplitPercent(((e.clientY - rect.top) / rect.height) * 100);
        splitEl.style.setProperty("--td-vsplit", `${p.toFixed(2)}%`);
    };

    const onMove = (e) => {
        pendingEvent = e;
        if (rafId) return;
        rafId = requestAnimationFrame(flushMove);
    };

    const onUp = (e) => {
        if (rafId) {
            cancelAnimationFrame(rafId);
            rafId = 0;
        }
        const splitEl = resolveVerticalSplitRoot(splitRootOrParent);
        let finalP = 50;
        if (splitEl && e) {
            const rect = splitEl.getBoundingClientRect();
            if (rect.height > 1)
                finalP = clampSplitPercent(((e.clientY - rect.top) / rect.height) * 100);
        }

        if (splitEl)
            splitEl.style.setProperty("--td-vsplit", `${finalP.toFixed(2)}%`);

        pendingEvent = null;
        document.removeEventListener("mousemove", onMove, true);
        document.removeEventListener("mouseup", onUp, true);
        _activeSplitDragCleanup = null;
        dotNetRef.invokeMethodAsync("NotifyVerticalSplitPercent", finalP).catch(() => {
        });
        dotNetRef.invokeMethodAsync("NotifySplitDragEnd").catch(() => {
        });
    };

    document.addEventListener("mousemove", onMove, true);
    document.addEventListener("mouseup", onUp, true);
    _activeSplitDragCleanup = onUp;
    dotNetRef.invokeMethodAsync("NotifySplitDragStart").catch(() => {
    });
}

export function cancelSplitDrag() {
    if (_activeSplitDragCleanup) {
        _activeSplitDragCleanup();
        _activeSplitDragCleanup = null;
    }
}

export function disposeEditors(root) {
    if (_splitPointerCleanup) {
        _splitPointerCleanup();
        _splitPointerCleanup = null;
    }

    if (!root) return;
    root.querySelectorAll("[data-lyo-diff-editor]").forEach((host) => {
        const state = _editorState.get(host);
        if (!state) return;

        state.textarea.removeEventListener("scroll", state.onScroll);
        state.textarea.removeEventListener("input", state.onInput);
        _editorState.delete(host);
    });
}

