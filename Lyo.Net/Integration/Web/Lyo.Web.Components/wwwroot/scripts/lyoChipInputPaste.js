/**
 * Paste-only bulk chips: read clipboard synchronously in capture phase (before the input updates).
 * - Multiline: single-line inputs drop \n before Blazor sees them.
 * - a,c style lists: ValueChanged can run before Blazor @onpaste on a parent, so the flag path races.
 * Typing never fires paste, so intercepting commas/tabs/; here does not affect composite keys typed in the field.
 */
export function attachBulkPasteListener(element, dotNetRef) {
    if (!element || !dotNetRef) return;

    const hasListSeparators = t =>
        /[\r\n]/.test(t) || /[,;\uFF0C\t]/.test(t);

    const handler = e => {
        const t = e.clipboardData?.getData('text/plain');
        if (!t || !hasListSeparators(t)) return;
        e.preventDefault();
        e.stopImmediatePropagation?.();
        dotNetRef.invokeMethodAsync('OnBulkPasteText', t);
    };

    element.addEventListener('paste', handler, true);
}
