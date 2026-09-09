/**
 * Client-side virtualized table preview.
 * Fetches row windows from .NET via DotNetObjectReference.invokeMethodAsync('GetWindowAsync', start, count).
 */
const ROW_HEIGHT = 32;
const OVERSCAN = 12;

/**
 * @param {HTMLElement} host
 * @param {*} dotNetRef
 * @param {{ maxHeightCss?: string, showRowNumbers?: boolean, rowHeight?: number }} options
 */
export async function mount(host, dotNetRef, options) {
    if (!host || !dotNetRef)
        return null;

    disposeInternal(host);

    const rowHeight = options?.rowHeight > 0 ? options.rowHeight : ROW_HEIGHT;
    const showRowNumbers = options?.showRowNumbers !== false;
    const maxHeightCss = options?.maxHeightCss || '480px';

    const state = {
        host,
        dotNetRef,
        rowHeight,
        showRowNumbers,
        totalRows: 0,
        columnCount: 0,
        headers: [],
        disposed: false,
        renderScheduled: false,
        lastStart: -1,
        lastCount: -1,
    };

    host._lyoDtPreview = state;
    host.classList.add('lyo-dt-js-host');
    host.style.maxHeight = maxHeightCss;
    host.style.overflow = 'auto';
    host.style.position = 'relative';
    host.innerHTML = '';

    ensureStyles();

    const headerTable = document.createElement('table');
    headerTable.className = 'lyo-dt-js-table lyo-dt-js-header';
    const thead = document.createElement('thead');
    const headerRow = document.createElement('tr');
    thead.appendChild(headerRow);
    headerTable.appendChild(thead);

    const bodySpace = document.createElement('div');
    bodySpace.className = 'lyo-dt-js-body-space';

    const bodyTable = document.createElement('table');
    bodyTable.className = 'lyo-dt-js-table lyo-dt-js-body';
    const tbody = document.createElement('tbody');
    bodyTable.appendChild(tbody);
    bodySpace.appendChild(bodyTable);

    host.appendChild(headerTable);
    host.appendChild(bodySpace);

    state.headerRow = headerRow;
    state.bodySpace = bodySpace;
    state.bodyTable = bodyTable;
    state.tbody = tbody;

    const onScroll = () => scheduleRender(state);
    host.addEventListener('scroll', onScroll, { passive: true });
    state.onScroll = onScroll;

    const meta = await dotNetRef.invokeMethodAsync('GetMetaAsync');
    if (state.disposed)
        return null;

    applyMeta(state, meta);
    await renderWindow(state);

    return {
        refresh: async () => {
            if (state.disposed) return;
            const next = await dotNetRef.invokeMethodAsync('GetMetaAsync');
            if (state.disposed) return;
            applyMeta(state, next);
            state.lastStart = -1;
            await renderWindow(state);
        },
    };
}

export function dispose(host) {
    disposeInternal(host);
}

function applyMeta(state, meta) {
    state.totalRows = meta?.totalRows ?? 0;
    state.columnCount = meta?.columnCount ?? 0;
    state.headers = Array.isArray(meta?.headers) ? meta.headers : [];
    renderHeader(state);
    state.bodySpace.style.height = `${Math.max(0, state.totalRows) * state.rowHeight}px`;
}

function disposeInternal(host) {
    const state = host?._lyoDtPreview;
    if (!state)
        return;
    state.disposed = true;
    if (state.onScroll)
        host.removeEventListener('scroll', state.onScroll);
    host._lyoDtPreview = null;
    host.innerHTML = '';
}

function scheduleRender(state) {
    if (state.renderScheduled || state.disposed)
        return;
    state.renderScheduled = true;
    requestAnimationFrame(async () => {
        state.renderScheduled = false;
        try {
            await renderWindow(state);
        } catch {
            // ignored — circuit may be gone
        }
    });
}

async function renderWindow(state) {
    if (state.disposed || !state.host)
        return;

    const { host, rowHeight, totalRows } = state;
    // Header is sticky and takes space at the top of the scrollport; body rows use bodySpace.
    const headerH = host.querySelector('.lyo-dt-js-header')?.offsetHeight ?? rowHeight;
    const scrollTop = Math.max(0, host.scrollTop);
    const viewH = Math.max(0, (host.clientHeight || 480) - headerH);
    const start = Math.max(0, Math.floor(scrollTop / rowHeight) - OVERSCAN);
    const visible = Math.ceil(viewH / rowHeight) + OVERSCAN * 2 + 1;
    const count = Math.max(0, Math.min(visible, totalRows - start));

    if (start === state.lastStart && count === state.lastCount && state.tbody.childElementCount > 0)
        return;

    state.lastStart = start;
    state.lastCount = count;

    state.bodyTable.style.top = `${start * rowHeight}px`;

    if (count === 0) {
        state.tbody.replaceChildren();
        return;
    }

    const window = await state.dotNetRef.invokeMethodAsync('GetWindowAsync', start, count);
    if (state.disposed)
        return;

    const cells = Array.isArray(window?.cells) ? window.cells : [];
    const frag = document.createDocumentFragment();
    for (let i = 0; i < cells.length; i++) {
        const rowIndex = start + i;
        const tr = document.createElement('tr');
        if (rowIndex % 2 === 1)
            tr.className = 'lyo-dt-js-stripe';
        tr.style.height = `${rowHeight}px`;

        if (state.showRowNumbers) {
            const rn = document.createElement('td');
            rn.className = 'lyo-dt-js-rownum';
            rn.textContent = String(rowIndex + 1);
            tr.appendChild(rn);
        }

        const rowCells = cells[i] || [];
        for (let c = 0; c < state.columnCount; c++) {
            const td = document.createElement('td');
            td.textContent = rowCells[c] ?? '';
            tr.appendChild(td);
        }
        frag.appendChild(tr);
    }

    state.tbody.replaceChildren(frag);
}

function renderHeader(state) {
    const { headerRow, headers, columnCount, showRowNumbers } = state;
    headerRow.replaceChildren();
    if (showRowNumbers) {
        const th = document.createElement('th');
        th.className = 'lyo-dt-js-rownum';
        th.textContent = '#';
        headerRow.appendChild(th);
    }
    for (let c = 0; c < columnCount; c++) {
        const th = document.createElement('th');
        th.textContent = headers[c] ?? '';
        headerRow.appendChild(th);
    }
}

function ensureStyles() {
    if (document.getElementById('lyo-dt-js-styles'))
        return;
    const style = document.createElement('style');
    style.id = 'lyo-dt-js-styles';
    style.textContent = `
.lyo-dt-js-host {
  background: var(--mud-palette-surface);
  border: 1px solid var(--mud-palette-lines-default);
  border-radius: 4px;
}
.lyo-dt-js-body-space {
  position: relative;
  width: 100%;
}
.lyo-dt-js-table {
  width: 100%;
  border-collapse: collapse;
  table-layout: fixed;
}
.lyo-dt-js-header {
  position: sticky;
  top: 0;
  z-index: 4;
  background: var(--mud-palette-surface);
}
.lyo-dt-js-body {
  position: absolute;
  left: 0;
  right: 0;
  width: 100%;
}
.lyo-dt-js-table thead th {
  background: var(--mud-palette-surface);
  border-bottom: 1px solid var(--mud-palette-lines-default);
  border-right: 1px solid var(--mud-palette-lines-default);
  padding: 6px 10px;
  text-align: left;
  font-weight: 500;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.lyo-dt-js-table td {
  border-bottom: 1px solid var(--mud-palette-lines-default);
  border-right: 1px solid var(--mud-palette-lines-default);
  padding: 6px 10px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  box-sizing: border-box;
}
.lyo-dt-js-table tr.lyo-dt-js-stripe td { background: var(--mud-palette-background-grey); }
.lyo-dt-js-table th.lyo-dt-js-rownum,
.lyo-dt-js-table td.lyo-dt-js-rownum {
  position: sticky;
  left: 0;
  z-index: 1;
  width: 4rem;
  min-width: 4rem;
  max-width: 4rem;
  text-align: right;
  color: var(--mud-palette-text-secondary);
  background: var(--mud-palette-surface);
  box-shadow: 1px 0 0 var(--mud-palette-lines-default);
}
.lyo-dt-js-header th.lyo-dt-js-rownum { z-index: 5; }
.lyo-dt-js-table tr.lyo-dt-js-stripe td.lyo-dt-js-rownum { background: var(--mud-palette-background-grey); }
`;
    document.head.appendChild(style);
}
