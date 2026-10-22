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

export function caret(element) {
    if (!element) return { start: 0, end: 0 };
    return { start: element.selectionStart ?? 0, end: element.selectionEnd ?? 0 };
}

export function place(element, position) {
    if (!element) return;
    element.focus();
    const pos = position ?? element.value.length;
    element.setSelectionRange(pos, pos);
}

function caretCoordinates(element, position) {
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

export function getCaretClientRect(element, position) {
    if (!element) return { top: 0, left: 0, height: 0 };
    const pos = Math.max(0, Math.min(position, element.value.length));
    const coords = caretCoordinates(element, pos);
    const rect = element.getBoundingClientRect();
    return {
        top: rect.top - element.scrollTop + coords.top,
        left: rect.left - element.scrollLeft + coords.left,
        height: coords.height
    };
}

function suggestBounds(textarea) {
    const inset = 8;
    const bounds = { left: inset, top: inset, right: window.innerWidth - inset, bottom: window.innerHeight - inset };
    const container = textarea.closest(".mud-dialog");
    if (!container) return bounds;
    const rect = container.getBoundingClientRect();
    return {
        left: Math.max(bounds.left, rect.left + 4),
        top: Math.max(bounds.top, rect.top + 4),
        right: Math.min(bounds.right, rect.right - 4),
        bottom: Math.min(bounds.bottom, rect.bottom - 4)
    };
}

export function placeSuggest(list, textarea, caretPos) {
    if (!list || !textarea) return;
    const caret = getCaretClientRect(textarea, caretPos);
    const bounds = suggestBounds(textarea);
    const gap = 4;
    list.style.maxWidth = `min(28rem, ${Math.max(160, Math.round(bounds.right - bounds.left))}px)`;
    const width = list.offsetWidth;
    const height = list.offsetHeight;
    let top = caret.top + caret.height + gap;
    let left = caret.left;
    if (left + width > bounds.right) left = bounds.right - width;
    if (left < bounds.left) left = bounds.left;
    if (top + height > bounds.bottom && caret.top - height - gap > bounds.top) top = caret.top - height - gap;
    list.style.top = `${Math.round(top)}px`;
    list.style.left = `${Math.round(left)}px`;
}

export function scrollItemIntoView(list, index) {
    const item = list?.children?.[index];
    if (!item) return;
    const top = item.offsetTop;
    const bottom = top + item.offsetHeight;
    if (top < list.scrollTop)
        list.scrollTop = top;
    else if (bottom > list.scrollTop + list.clientHeight)
        list.scrollTop = bottom - list.clientHeight;
}

export function bindKeys(element, dotnet) {
    if (!element || element._algoKeys) return;
    const onKeyDown = (event) => {
        if (event.isComposing) return;
        if (event.key !== "ArrowDown" && event.key !== "ArrowUp" && event.key !== "Enter" && event.key !== "Tab" && event.key !== "Escape")
            return;
        const root = element.closest(".algorithm-expression");
        if (!root?.querySelector(".algorithm-suggest")) return;
        event.preventDefault();
        event.stopPropagation();
        dotnet.invokeMethodAsync("HandleSuggestKey", event.key);
    };
    element.addEventListener("keydown", onKeyDown, true);
    element._algoKeys = onKeyDown;
}

export function unbindKeys(element) {
    if (!element?._algoKeys) return;
    element.removeEventListener("keydown", element._algoKeys, true);
    delete element._algoKeys;
}

export function bindView(wrap, dotnet) {
    if (!wrap || wrap._algoView) return;
    const onView = () => dotnet.invokeMethodAsync("PlaceSuggest");
    wrap.addEventListener("scroll", onView, { passive: true });
    window.addEventListener("scroll", onView, true);
    window.addEventListener("resize", onView);
    wrap._algoView = onView;
}

export function unbindView(wrap) {
    const onView = wrap?._algoView;
    if (!onView) return;
    wrap.removeEventListener("scroll", onView);
    window.removeEventListener("scroll", onView, true);
    window.removeEventListener("resize", onView);
    delete wrap._algoView;
}
