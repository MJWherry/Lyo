namespace Lyo.Web.Automation.Playwright.Record;

/// <summary>Installed on the current page only while Record is on. It is not an init script and it is not part of a plan run.</summary>
internal static class StudioRecorderPage
{
    internal const string Source = """
                                   (() => {
                                     if (window.__lyoRecorderInstalled) return;
                                     window.__lyoRecorderInstalled = true;

                                     function cssEscape(value) {
                                       if (window.CSS && CSS.escape) return CSS.escape(value);
                                       return String(value).replace(/(["\\])/g, "\\$1");
                                     }

                                     function unique(selector) {
                                       try { return document.querySelectorAll(selector).length === 1; }
                                       catch { return false; }
                                     }

                                     function stableId(id) {
                                       if (!id) return false;
                                       if (/^[0-9a-f]{8}-[0-9a-f]{4}-[1-8][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(id)) return false;
                                       if (/^\d{6,}$/.test(id)) return false;
                                       return true;
                                     }

                                     function attrSelector(el) {
                                       const tag = el.tagName.toLowerCase();
                                       let sel = tag;
                                       const name = el.getAttribute("name");
                                       if (name) sel += `[name="${cssEscape(name)}"]`;
                                       return sel;
                                     }

                                     function cssPath(el) {
                                       const parts = [];
                                       let node = el;
                                       while (node && node.nodeType === 1 && node !== document.documentElement) {
                                         let bit = node === el ? attrSelector(node) : (stableId(node.id) ? `[id="${cssEscape(node.id)}"]` : node.tagName.toLowerCase());
                                         const parent = node.parentElement;
                                         if (parent) {
                                           const same = [...parent.children].filter((child) => child.tagName === node.tagName);
                                           if (same.length > 1) bit += `:nth-of-type(${same.indexOf(node) + 1})`;
                                         }
                                         parts.unshift(bit);
                                         const candidate = parts.join(" > ");
                                         if (unique(candidate) && !/^[a-z][a-z0-9-]*$/i.test(candidate)) return candidate;
                                         if (node === document.body) break;
                                         node = node.parentElement;
                                       }
                                       const full = parts.join(" > ");
                                       return unique(full) ? full : null;
                                     }

                                     function locatorFor(el) {
                                       if (!(el instanceof Element)) return null;
                                       const testId = el.getAttribute("data-testid");
                                       if (testId) {
                                         const sel = `[data-testid="${cssEscape(testId)}"]`;
                                         if (unique(sel)) return { kind: "CssSelector", value: sel };
                                       }
                                       if (stableId(el.id)) return { kind: "Id", value: el.id };
                                       if (el.tagName === "A") {
                                         const raw = el.textContent || "";
                                         const text = raw.trim();
                                         if (text && text.length <= 80 && !/[\r\n]/.test(raw)) {
                                           const matches = [...document.querySelectorAll("a")].filter((a) => (a.textContent || "").trim() === text);
                                           if (matches.length === 1) return { kind: "LinkText", value: text };
                                         }
                                       }
                                       const path = cssPath(el);
                                       if (!path) return null;
                                       return { kind: "CssSelector", value: path };
                                     }

                                     function report(kind, el, extra) {
                                       if (typeof window.__lyoRecord !== "function") return;
                                       const locator = el ? locatorFor(el) : null;
                                       if (!locator && kind !== "Navigate") return;
                                       const key = el ? (el.getAttribute("data-testid") || el.id || (locator && locator.value) || "") : "";
                                       window.__lyoRecord(JSON.stringify(Object.assign({ kind, key, locator }, extra || {})));
                                     }

                                     document.addEventListener("click", (event) => {
                                       const el = event.target instanceof Element ? event.target.closest("a,button,input,textarea,select,[role='button'],[onclick]") || event.target : null;
                                       if (!(el instanceof Element) || el === document.body || el === document.documentElement) return;
                                       report("Click", el);
                                     }, true);

                                     document.addEventListener("input", (event) => {
                                       const el = event.target;
                                       if (!(el instanceof HTMLInputElement || el instanceof HTMLTextAreaElement)) return;
                                       report("Input", el, { text: el.value });
                                     }, true);

                                     document.addEventListener("change", (event) => {
                                       const el = event.target;
                                       if (!(el instanceof HTMLSelectElement)) return;
                                       report("Select", el, { value: el.value });
                                     }, true);

                                     document.addEventListener("focusout", (event) => {
                                       const el = event.target instanceof Element ? event.target : null;
                                       if (!el) return;
                                       report("Blur", el);
                                     }, true);
                                   })()
                                   """;
}