export function scrollIntoView(paneSelector, key) {
    if (!paneSelector || !key)
        return;

    const pane = document.querySelector(paneSelector);
    if (!pane)
        return;

    const find = (k) => pane.querySelector(`[data-lyo-design="${CSS.escape(k)}"]`);
    let el = find(key);
    if (!el) {
        const col = key.lastIndexOf(":col:");
        const row = key.lastIndexOf(":row:");
        const cut = Math.max(col, row);
        if (cut > 0)
            el = find(key.slice(0, cut));
    }

    if (!el)
        return;

    const er = el.getBoundingClientRect();
    const pr = pane.getBoundingClientRect();
    const visible = er.top >= pr.top && er.bottom <= pr.bottom && er.left >= pr.left && er.right <= pr.right;
    if (visible)
        return;

    el.scrollIntoView({ block: "nearest", inline: "nearest" });
}
