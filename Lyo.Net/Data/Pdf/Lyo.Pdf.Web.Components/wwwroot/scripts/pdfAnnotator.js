/**
 * PDF Bounding Box Annotator - Blazor/JS interop
 * Annotator HTML loads PDF.js from CDN (see PdfAnnotatorHtml); this module only manages iframes and postMessage.
 */

let deleteForwardTargetIframe = null;

function globalDeleteForward(e) {
    const iframe = deleteForwardTargetIframe;
    if (!iframe || !iframe.isConnected)
        return;
    const isDel = e.key === 'Delete' || e.key === 'Backspace' || e.code === 'Delete' || e.code === 'Backspace';
    if (!isDel)
        return;
    const pae = document.activeElement;
    if (pae && pae !== iframe && (pae.tagName === 'INPUT' || pae.tagName === 'TEXTAREA' || pae.tagName === 'SELECT' || pae.isContentEditable))
        return;
    const iwin = iframe.contentWindow;
    if (!iwin)
        return;
    const idoc = iframe.contentDocument;
    if (idoc) {
        const ae = idoc.activeElement;
        if (ae && (ae.id === 'idInput' || ae.tagName === 'INPUT' || ae.tagName === 'TEXTAREA'))
            return;
    }
    e.preventDefault();
    e.stopPropagation();
    iwin.postMessage({type: 'lyo-annotator-delete'}, '*');
}

if (typeof window !== 'undefined' && !window.__lyoPdfAnnotatorDeleteForwardInstalled) {
    window.__lyoPdfAnnotatorDeleteForwardInstalled = true;
    window.addEventListener('keydown', globalDeleteForward, true);
}

export function requestDeleteSelectedAnnotation(iframeElement) {
    const iwin = iframeElement?.contentWindow;
    if (!iwin)
        return;
    iwin.postMessage({type: 'lyo-annotator-delete'}, '*');
}

export async function attachInlinePdfAnnotator(iframeElement, htmlContent, dotNetHelper) {
    const id = inlineAnnotatorNextId++;
    const blob = new Blob([htmlContent], {type: 'text/html;charset=utf-8'});
    const url = URL.createObjectURL(blob);
    iframeElement.src = url;

    const handler = (e) => {
        if (e.data?.type === 'pdf-annotations') {
            window.removeEventListener('message', handler);
            inlineAnnotators.delete(id);
            const payload = JSON.stringify(e.data.annotations ?? []);
            dotNetHelper.invokeMethodAsync('OnPdfAnnotationsSaved', payload);
            URL.revokeObjectURL(url);
        } else if (e.data?.type === 'pdf-annotation-progress') {
            dotNetHelper.invokeMethodAsync('OnPdfAnnotationProgress', JSON.stringify(e.data.annotations ?? []));
        } else if (e.data?.type === 'lyo-annotator-warning') {
            dotNetHelper.invokeMethodAsync('OnPdfAnnotatorWarning', String(e.data.message ?? 'Annotator warning.'));
        }
    };
    window.addEventListener('message', handler);

    deleteForwardTargetIframe = iframeElement;

    inlineAnnotators.set(id, {handler, url, iframeElement});
    return id;
}

export async function setIframeHtmlBlob(iframeElement, htmlContent) {
    const prev = iframeElement.dataset.lyoBlobUrl;
    if (prev) {
        try {
            URL.revokeObjectURL(prev);
        } catch {
        }
    }

    const blob = new Blob([htmlContent], {type: 'text/html;charset=utf-8'});
    const url = URL.createObjectURL(blob);
    iframeElement.dataset.lyoBlobUrl = url;
    iframeElement.src = url;
}

let inlineAnnotatorNextId = 0;
const inlineAnnotators = new Map();

export function detachInlinePdfAnnotator(id) {
    const x = inlineAnnotators.get(id);
    if (!x)
        return;
    window.removeEventListener('message', x.handler);
    try {
        URL.revokeObjectURL(x.url);
    } catch {
    }
    if (deleteForwardTargetIframe === x.iframeElement)
        deleteForwardTargetIframe = null;
    inlineAnnotators.delete(id);
}

export async function createPdfAnnotator(htmlContent) {
    return new Promise((resolve) => {
        const iframe = document.createElement('iframe');
        iframe.style.cssText = 'position:fixed;inset:0;width:100%;height:100%;border:none;z-index:9999;';
        document.body.appendChild(iframe);

        const handler = (e) => {
            if (e.data?.type === 'pdf-annotations') {
                window.removeEventListener('message', handler);
                document.body.removeChild(iframe);
                URL.revokeObjectURL(iframe.src);
                resolve(e.data.annotations || []);
            }
        };
        window.addEventListener('message', handler);

        const blob = new Blob([htmlContent], {type: 'text/html;charset=utf-8'});
        iframe.src = URL.createObjectURL(blob);
    });
}
