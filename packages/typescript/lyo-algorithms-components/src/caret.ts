const caretStyleProps = [
  "direction", "boxSizing", "width", "height",
  "overflowX", "overflowY",
  "borderTopWidth", "borderRightWidth", "borderBottomWidth", "borderLeftWidth", "borderStyle",
  "paddingTop", "paddingRight", "paddingBottom", "paddingLeft",
  "fontStyle", "fontVariant", "fontWeight", "fontStretch", "fontSize", "fontSizeAdjust", "lineHeight", "fontFamily",
  "textAlign", "textTransform", "textIndent", "textDecoration",
  "letterSpacing", "wordSpacing", "tabSize", "MozTabSize",
  "whiteSpace", "wordBreak", "wordWrap", "overflowWrap",
] as const;

function caretCoordinates(element: HTMLTextAreaElement, position: number) {
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
  for (const prop of caretStyleProps) style.setProperty(prop.replace(/[A-Z]/g, (letter) => `-${letter.toLowerCase()}`), computed.getPropertyValue(prop.replace(/[A-Z]/g, (letter) => `-${letter.toLowerCase()}`)));
  div.textContent = element.value.substring(0, position);
  const span = document.createElement("span");
  span.textContent = element.value.substring(position) || ".";
  div.appendChild(span);
  document.body.appendChild(div);
  const lineHeight = Number.parseFloat(computed.lineHeight) || span.offsetHeight;
  const coordinates = {
    top: span.offsetTop + (Number.parseFloat(computed.borderTopWidth) || 0),
    left: span.offsetLeft + (Number.parseFloat(computed.borderLeftWidth) || 0),
    height: lineHeight,
  };
  document.body.removeChild(div);
  return coordinates;
}

function suggestBounds(textarea: HTMLTextAreaElement) {
  const inset = 8;
  const bounds = {left: inset, top: inset, right: window.innerWidth - inset, bottom: window.innerHeight - inset};
  const container = textarea.closest(".MuiDialog-paper, .mud-dialog");
  if (!container) return bounds;
  const rect = container.getBoundingClientRect();
  return {
    left: Math.max(bounds.left, rect.left + 4),
    top: Math.max(bounds.top, rect.top + 4),
    right: Math.min(bounds.right, rect.right - 4),
    bottom: Math.min(bounds.bottom, rect.bottom - 4),
  };
}

/** Pins a suggestion list to the caret, flipping above the line when the list would leave the viewport. */
export function placeSuggest(list: HTMLElement, textarea: HTMLTextAreaElement, caretPos: number) {
  const pos = Math.max(0, Math.min(caretPos, textarea.value.length));
  const coords = caretCoordinates(textarea, pos);
  const rect = textarea.getBoundingClientRect();
  const caret = {top: rect.top - textarea.scrollTop + coords.top, left: rect.left - textarea.scrollLeft + coords.left, height: coords.height};
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
