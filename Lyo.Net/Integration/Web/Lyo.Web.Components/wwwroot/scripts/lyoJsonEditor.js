export function sendToClipboard(text) {
    return navigator.clipboard.writeText(text);
}

export function scrollVirtualRowIntoView(container, rowIndex, itemHeight, activePathId) {
    if (!container || rowIndex < 0 || itemHeight <= 0) {
        return;
    }

    const targetTop = rowIndex * itemHeight;
    const targetBottom = targetTop + itemHeight;
    const currentTop = container.scrollTop;
    const currentBottom = currentTop + container.clientHeight;

    if (targetTop < currentTop) {
        container.scrollTop = targetTop;
    } else if (targetBottom > currentBottom) {
        container.scrollTop = targetBottom - container.clientHeight;
    }

    if (activePathId) {
        const selector = `[data-path-id="${CSS.escape(activePathId)}"]`;
        const activeRow = container.querySelector(selector);
        activeRow?.scrollIntoView({block: "nearest"});
    }
}

export function scrollTextHighlightIntoView(container, matchIndex) {
    if (!container || matchIndex < 0) {
        return;
    }

    const highlights = container.querySelectorAll("mark");
    const target = highlights.item(matchIndex);
    target?.scrollIntoView({block: "nearest", inline: "nearest"});
}

export function scrollTextareaMatchIntoView(textarea, startIndex, length) {
    if (!textarea || startIndex < 0 || length <= 0) {
        return;
    }

    textarea.focus();
    textarea.setSelectionRange(startIndex, startIndex + length);

    const before = textarea.value.slice(0, startIndex);
    const lineIndex = before.split("\n").length - 1;
    const lineHeight = parseFloat(getComputedStyle(textarea).lineHeight || "0");
    if (!Number.isFinite(lineHeight) || lineHeight <= 0) {
        return;
    }

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
