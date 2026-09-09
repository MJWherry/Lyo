const caretStyleProps = [
    "direction", "boxSizing", "width", "height",
    "overflowX", "overflowY",
    "borderTopWidth", "borderRightWidth", "borderBottomWidth", "borderLeftWidth", "borderStyle",
    "paddingTop", "paddingRight", "paddingBottom", "paddingLeft",
    "fontStyle", "fontVariant", "fontWeight", "fontStretch", "fontSize", "fontSizeAdjust", "lineHeight", "fontFamily",
    "textAlign", "textTransform", "textIndent", "textDecoration",
    "letterSpacing", "wordSpacing", "tabSize", "MozTabSize",
    "whiteSpace", "wordBreak", "wordWrap", "overflowWrap"
];

export function getCaret(el) {
    if (!el)
        return 0;
    return el.selectionStart ?? 0;
}

export function setCaret(el, pos) {
    if (!el)
        return;
    el.focus();
    const n = Math.max(0, Math.min(pos, el.value.length));
    el.setSelectionRange(n, n);
}

export function restoreCaretIfFocused(el, pos) {
    if (!el || document.activeElement !== el)
        return;
    const n = Math.max(0, Math.min(pos, el.value.length));
    if (el.selectionStart !== n || el.selectionEnd !== n)
        el.setSelectionRange(n, n);
}

function getCaretCoordinates(element, position) {
    const div = document.createElement("div");
    const style = div.style;
    const computed = window.getComputedStyle(element);

    style.position = "absolute";
    style.visibility = "hidden";
    style.overflow = "hidden";
    style.whiteSpace = "pre-wrap";
    style.wordWrap = "break-word";
    style.top = "0";
    style.left = "-9999px";

    for (const prop of caretStyleProps)
        style[prop] = computed[prop];

    div.textContent = element.value.substring(0, position);
    const span = document.createElement("span");
    span.textContent = element.value.substring(position) || ".";
    div.appendChild(span);
    document.body.appendChild(div);

    const lineHeight = Number.parseFloat(computed.lineHeight) || span.offsetHeight;
    const coordinates = {
        top: span.offsetTop + (Number.parseFloat(computed.borderTopWidth) || 0),
        left: span.offsetLeft + (Number.parseFloat(computed.borderLeftWidth) || 0),
        height: lineHeight
    };
    document.body.removeChild(div);
    return coordinates;
}

export function getCaretClientRect(el, position) {
    if (!el)
        return { top: 0, left: 0, height: 0 };

    const pos = Math.max(0, Math.min(position, el.value.length));
    const coords = getCaretCoordinates(el, pos);
    const rect = el.getBoundingClientRect();
    return {
        top: rect.top - el.scrollTop + coords.top,
        left: rect.left - el.scrollLeft + coords.left,
        height: coords.height
    };
}

// The list is position: fixed off the caret, so the viewport is its only natural limit. In a dialog that is too generous: it can hang over the backdrop well past the
// dialog edge. Where a dialog is an ancestor, its box narrows the bounds.
function suggestBounds(textarea) {
    const inset = 8;
    const bounds = {
        left: inset,
        top: inset,
        right: window.innerWidth - inset,
        bottom: window.innerHeight - inset
    };

    const container = textarea.closest(".mud-dialog");
    if (!container)
        return bounds;

    const rect = container.getBoundingClientRect();
    return {
        left: Math.max(bounds.left, rect.left + 4),
        top: Math.max(bounds.top, rect.top + 4),
        right: Math.min(bounds.right, rect.right - 4),
        bottom: Math.min(bounds.bottom, rect.bottom - 4)
    };
}

export function placeSuggest(list, textarea, caretPos) {
    if (!list || !textarea)
        return;

    const caret = getCaretClientRect(textarea, caretPos);
    const bounds = suggestBounds(textarea);
    const gap = 4;

    // Applied before measuring so a long token path wraps inside the bounds instead of widening the list past them. The 28rem cap matches the stylesheet.
    list.style.maxWidth = `min(28rem, ${Math.max(160, Math.round(bounds.right - bounds.left))}px)`;

    const width = list.offsetWidth;
    const height = list.offsetHeight;
    let top = caret.top + caret.height + gap;
    let left = caret.left;

    if (left + width > bounds.right)
        left = bounds.right - width;
    if (left < bounds.left)
        left = bounds.left;
    if (top + height > bounds.bottom && caret.top - height - gap > bounds.top)
        top = caret.top - height - gap;

    list.style.top = `${Math.round(top)}px`;
    list.style.left = `${Math.round(left)}px`;
}

export function scrollItemIntoView(list, index) {
    const item = list?.children?.[index];
    if (item && typeof item.scrollIntoView === "function")
        item.scrollIntoView({ block: "nearest" });
}

const editorListeners = new WeakMap();

export function attachEditor(el, wrap, dotnetRef) {
    detachEditor(el);
    if (!el || !dotnetRef)
        return;

    const onKeyDown = (e) => {
        if (e.isComposing)
            return;
        if (e.key !== "ArrowDown" && e.key !== "ArrowUp" && e.key !== "Enter" && e.key !== "Tab" && e.key !== "Escape")
            return;
        const root = wrap?.closest(".lyo-fmt-editor") ?? el.closest(".lyo-fmt-editor");
        if (!root?.querySelector(".lyo-fmt-suggest"))
            return;
        e.preventDefault();
        e.stopPropagation();
        dotnetRef.invokeMethodAsync("HandleSuggestKey", e.key);
    };

    const onInput = () => {
        dotnetRef.invokeMethodAsync("OnTemplateInput", el.value, el.selectionStart ?? 0);
    };

    const onViewChange = () => {
        dotnetRef.invokeMethodAsync("OnCaretViewChanged");
    };

    el.addEventListener("keydown", onKeyDown, true);
    el.addEventListener("input", onInput);
    wrap?.addEventListener("scroll", onViewChange, { passive: true });
    window.addEventListener("scroll", onViewChange, true);
    window.addEventListener("resize", onViewChange);

    editorListeners.set(el, { onKeyDown, onInput, onViewChange, wrap });
}

export function detachEditor(el) {
    const listeners = editorListeners.get(el);
    if (!listeners)
        return;

    el.removeEventListener("keydown", listeners.onKeyDown, true);
    el.removeEventListener("input", listeners.onInput);
    listeners.wrap?.removeEventListener("scroll", listeners.onViewChange);
    window.removeEventListener("scroll", listeners.onViewChange, true);
    window.removeEventListener("resize", listeners.onViewChange);
    editorListeners.delete(el);
}

export function attachSuggestKeys(el, dotnetRef) {
    attachEditor(el, null, dotnetRef);
}

export function detachSuggestKeys(el) {
    detachEditor(el);
}
