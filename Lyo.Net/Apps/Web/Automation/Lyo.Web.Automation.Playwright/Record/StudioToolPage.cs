namespace Lyo.Web.Automation.Playwright.Record;

/// <summary>
/// Installed on the current page only while the info grabber is on. It is not an init script and it is not part of a plan run.
/// </summary>
internal static class StudioToolPage
{
    internal const string Source = """
        (() => {
          if (window.__lyoTool) {
            window.__lyoTool.set(true);
            return;
          }

          let grabbing = false;
          let hoverEl = null;
          let lastEl = null;
          let inside = true;
          const outlined = new Set();

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

          function describe(el) {
            if (!(el instanceof Element)) return null;
            const attrs = {};
            for (const attr of el.attributes) attrs[attr.name] = attr.value;
            return {
              tag: el.tagName.toLowerCase(),
              text: (el.innerText || el.textContent || "").trim().slice(0, 500),
              attributes: attrs,
              links: [...el.querySelectorAll("a[href]")].map((a) => a.href),
              images: [...el.querySelectorAll("img")].map((img) => img.currentSrc || img.src).filter(Boolean),
              url: location.href,
              title: document.title,
              locator: locatorFor(el)
            };
          }

          function remember(node) {
            if (!("lyoPrevOutline" in node.dataset)) {
              node.dataset.lyoPrevOutline = node.style.outline || "";
              node.dataset.lyoPrevOffset = node.style.outlineOffset || "";
            }
            outlined.add(node);
          }

          function restoreNode(node) {
            if ("lyoPrevOutline" in node.dataset) {
              node.style.outline = node.dataset.lyoPrevOutline;
              node.style.outlineOffset = node.dataset.lyoPrevOffset || "";
            }
            delete node.dataset.lyoTarget;
            delete node.dataset.lyoTargetOutline;
            delete node.dataset.lyoTargetOffset;
            delete node.dataset.lyoPrevOutline;
            delete node.dataset.lyoPrevOffset;
            outlined.delete(node);
          }

          function clearMarks() {
            for (const node of [...outlined]) {
              if (node === hoverEl) continue;
              restoreNode(node);
            }
          }

          function clearHover() {
            if (!hoverEl) return;
            const node = hoverEl;
            hoverEl = null;
            restoreNode(node);
          }

          function leave() {
            inside = false;
            const hovered = hoverEl;
            hoverEl = null;
            if (hovered) restoreNode(hovered);
            clearMarks();
            lastEl = null;
          }

          function paint(el) {
            if (!(el instanceof Element)) return;
            inside = true;
            lastEl = el;
            if (!grabbing || el === hoverEl) return;
            clearHover();
            clearMarks();
            remember(el);
            el.style.outline = "2px solid #4c8dff";
            el.style.outlineOffset = "2px";
            hoverEl = el;
          }

          function mark(el) {
            if (!inside || el !== hoverEl) {
              clearMarks();
              return;
            }
            remember(el);
            el.dataset.lyoTarget = "1";
            el.style.outline = "3px solid #4c8dff";
            el.style.outlineOffset = "2px";
          }

          function generalize(el, tag) {
            if (!tag) return { repeating: false, targetLocator: locatorFor(el) };
            const upper = tag.toUpperCase();
            const clicked = el.tagName === upper ? el : (el.closest(upper) || el);
            let node = clicked;
            while (node.parentElement) {
              const parent = node.parentElement;
              const kids = [...parent.children].filter((child) => child.tagName === upper);
              if (kids.length >= 2 && kids.includes(clicked)) {
                return { repeating: true, parentLocator: locatorFor(parent), descendantTag: tag, targetLocator: locatorFor(clicked) };
              }
              node = parent;
            }
            return { repeating: false, targetLocator: locatorFor(clicked), descendantTag: tag };
          }

          document.documentElement.addEventListener("mouseleave", () => leave());
          document.addEventListener("mouseout", (event) => {
            if (event.relatedTarget != null) return;
            const x = event.clientX;
            const y = event.clientY;
            if (x > 0 && y > 0 && x < window.innerWidth && y < window.innerHeight) return;
            leave();
          }, true);
          document.addEventListener("mouseover", (event) => paint(event.target), true);
          document.addEventListener("mousemove", (event) => paint(event.target), true);

          window.__lyoTool = {
            set(on) {
              grabbing = !!on;
              if (!grabbing) {
                clearHover();
                clearMarks();
              }
            },
            pointerInside() { return inside; },
            clearMarks() { clearMarks(); },
            mark,
            describe() { return inside ? describe(hoverEl || lastEl) : null; },
            grab(tool, variable) {
              const el = hoverEl || lastEl;
              if (!(el instanceof Element)) return null;
              const tag = tool === "links" ? "a" : tool === "images" ? "img" : null;
              const staged = generalize(tag ? (el.closest(tag) || el) : el, tag);
              return {
                tool,
                variableName: variable,
                repeating: !!staged.repeating,
                parentLocator: staged.parentLocator || null,
                descendantTag: staged.descendantTag || null,
                locator: staged.targetLocator || null
              };
            }
          };
          window.__lyoTool.set(true);
        })()
        """;
}
