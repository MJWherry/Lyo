const editors = new Map();
let highlightJsPromise = null;

export function initialize(root, editor, dotNetRef, initialHtml) {
    if (!editor || !root) {
        return;
    }

    setHtml(editor, initialHtml ?? "");

    const state = {
        root,
        editor,
        dotNetRef,
        savedRange: null,
        activeCodeBlock: null,
        highlightTimer: 0,
        highlightVersion: 0,
        lastToolbarStateKey: "",
        suspendCodeBlockTracking: false,
        history: [],
        historyIndex: -1,
        suppressHistory: false
    };

    state.onInput = async () => {
        saveSelection(state);
        syncActiveCodeBlock(state);
        scheduleCodeHighlighting(state, 180);
        await notifyChange(state);
    };

    state.onKeyUp = async () => {
        saveSelection(state);
        syncActiveCodeBlock(state);
        scheduleCodeHighlighting(state, 120);
        await notifyToolbarState(state);
    };

    state.onKeyDown = async event => {
        if (event.defaultPrevented || event.isComposing || event.ctrlKey || event.metaKey || event.altKey) {
            return;
        }

        const selectedPre = getSelectedPre(editor);
        if (selectedPre && isEditingKey(event)) {
            prepareCodeBlockForEditing(state, selectedPre);
        }

        if (event.key === "Tab") {
            event.preventDefault();
            insertTextAtSelection(selectedPre ? "    " : "\u00A0\u00A0\u00A0\u00A0");
            saveSelection(state);
            syncActiveCodeBlock(state);
            scheduleCodeHighlighting(state, 220, true);
            await notifyChange(state);
            return;
        }

        if (event.key === "Enter" && selectedPre) {
            event.preventDefault();
            insertTextAtSelection("\n");
            saveSelection(state);
            syncActiveCodeBlock(state);
            scheduleCodeHighlighting(state, 220, true);
            await notifyChange(state);
        }
    };

    state.onMouseUp = async () => {
        saveSelection(state);
        syncActiveCodeBlock(state);
        scheduleCodeHighlighting(state, 120);
        await notifyToolbarState(state);
    };

    state.onFocus = async () => {
        state.suspendCodeBlockTracking = false;
        saveSelection(state);
        syncActiveCodeBlock(state);
        scheduleCodeHighlighting(state, 120);
        await notifyToolbarState(state);
    };

    state.onBlur = event => {
        const nextTarget = event.relatedTarget;
        if (nextTarget instanceof Node && state.root.contains(nextTarget)) {
            return;
        }

        void finalizeActiveCodeBlock(state);
    };

    state.onSelectionChange = async () => {
        if (!selectionBelongsToEditor(editor)) {
            return;
        }

        saveSelection(state);
        syncActiveCodeBlock(state);
        scheduleCodeHighlighting(state, 120);
        await notifyToolbarState(state);
    };

    state.onRootFocusOut = event => {
        const nextTarget = event.relatedTarget;
        if (nextTarget instanceof Node && state.root.contains(nextTarget)) {
            return;
        }

        void finalizeActiveCodeBlock(state);
    };

    state.onDocumentMouseDown = event => {
        const target = event.target;
        if (target instanceof Node && state.root.contains(target)) {
            return;
        }

        void finalizeActiveCodeBlock(state);
    };

    editor.addEventListener("input", state.onInput);
    editor.addEventListener("keydown", state.onKeyDown);
    editor.addEventListener("keyup", state.onKeyUp);
    editor.addEventListener("mouseup", state.onMouseUp);
    editor.addEventListener("focus", state.onFocus);
    editor.addEventListener("blur", state.onBlur);
    root.addEventListener("focusout", state.onRootFocusOut);
    document.addEventListener("mousedown", state.onDocumentMouseDown, true);
    document.addEventListener("selectionchange", state.onSelectionChange);
    wireToolbar(state);

    editors.set(editor, state);
    syncActiveCodeBlock(state);
    scheduleCodeHighlighting(state, 0);
    pushHistorySnapshot(state, normalizeHtml(editor.innerHTML));
    void notifyToolbarState(state);
}

export function dispose(editor) {
    const state = editors.get(editor);
    if (!state) {
        return;
    }

    editor.removeEventListener("input", state.onInput);
    editor.removeEventListener("keydown", state.onKeyDown);
    editor.removeEventListener("keyup", state.onKeyUp);
    editor.removeEventListener("mouseup", state.onMouseUp);
    editor.removeEventListener("focus", state.onFocus);
    editor.removeEventListener("blur", state.onBlur);
    state.root?.removeEventListener("focusout", state.onRootFocusOut);
    document.removeEventListener("mousedown", state.onDocumentMouseDown, true);
    document.removeEventListener("selectionchange", state.onSelectionChange);
    unwireToolbar(state);
    editors.delete(editor);
}

export function setHtml(editor, html) {
    if (!editor) {
        return;
    }

    editor.innerHTML = normalizeHtml(html);
    const state = editors.get(editor);
    if (state) {
        syncActiveCodeBlock(state);
        scheduleCodeHighlighting(state, 0);
        if (!state.suppressHistory) {
            resetHistory(state, normalizeHtml(editor.innerHTML));
        }
    }
}

export function runCommand(editor, command, value) {
    const state = editors.get(editor);
    if (!state) {
        return;
    }

    restoreSelection(state);
    editor.focus();

    if (isCodeBlockCommandBlocked(editor, command, value)) {
        void notifyToolbarState(state);
        return;
    }

    if (command === "undo") {
        restoreHistorySnapshot(state, -1);
        return;
    }

    if (command === "redo") {
        restoreHistorySnapshot(state, 1);
        return;
    }

    const handled = executeEditorCommand(state, command, value);
    if (!handled) {
        return;
    }

    normalizeEditorMarkup(editor);
    saveSelection(state);
    syncActiveCodeBlock(state);
    scheduleCodeHighlighting(state, 120);
    void notifyChange(state);
}

function executeEditorCommand(state, command, value) {
    const {editor} = state;
    switch (command) {
        case "formatBlock":
            if ((value || "p") === "pre") {
                if (shouldToggleCurrentCodeBlock(editor)) {
                    toggleCodeBlockLine(editor, state);
                } else {
                    applyCodeBlockFormat(editor, state);
                }
                return true;
            }

            return applyBlockFormat(editor, value || "p");
        case "fontName":
            return applyInlineStyle(editor, "fontFamily", value || "Arial, Helvetica, sans-serif");
        case "foreColor":
            return applyInlineStyle(editor, "color", value || "#000000");
        case "hiliteColor":
            return applyInlineStyle(editor, "backgroundColor", value || "#ffff00");
        case "fontSizePx":
            return applyCodeBlockFontSize(editor, value) || applyInlineStyle(editor, "fontSize", `${value || "16px"}`.trim() || "16px");
        case "bold":
            return toggleInlineTag(editor, "strong");
        case "italic":
            return toggleInlineTag(editor, "em");
        case "underline":
            return toggleInlineStyle(editor, "textDecorationLine", "underline");
        case "strikeThrough":
            return toggleInlineStyle(editor, "textDecorationLine", "line-through");
        case "justifyLeft":
            return applyTextAlignment(editor, "left");
        case "justifyCenter":
            return applyTextAlignment(editor, "center");
        case "justifyRight":
            return applyTextAlignment(editor, "right");
        case "insertOrderedList":
            return toggleListFormat(editor, "ol");
        case "insertUnorderedList":
            return toggleListFormat(editor, "ul");
        case "indent":
            return adjustBlockIndentation(editor, 24);
        case "outdent":
            return adjustBlockIndentation(editor, -24);
        case "unlink":
            return removeSelectedLink(editor);
        case "insertHorizontalRule":
            return insertHorizontalRuleAtSelection(editor);
        case "removeFormat":
            return clearSelectionFormatting(editor);
        default:
            return false;
    }
}

function getEditorSelectionRange(editor) {
    const selection = window.getSelection();
    if (!selection || selection.rangeCount === 0) {
        return null;
    }

    const range = selection.getRangeAt(0);
    return editor.contains(range.commonAncestorContainer) || range.commonAncestorContainer === editor
        ? range
        : null;
}

function getClosestAncestor(node, predicate, stopNode) {
    let current = node?.nodeType === Node.TEXT_NODE ? node.parentElement : node;
    while (current && current !== stopNode) {
        if (predicate(current)) {
            return current;
        }

        current = current.parentElement;
    }

    return null;
}

function applyBlockFormat(editor, tagName) {
    const range = getEditorSelectionRange(editor);
    const blocks = getSelectedBlocks(editor, range);
    if (blocks.length === 0) {
        return wrapSelectionInBlock(editor, tagName);
    }

    for (const block of blocks) {
        if (block.tagName?.toLowerCase() === tagName) {
            continue;
        }

        replaceBlockTag(block, tagName);
    }

    return true;
}

function wrapSelectionInBlock(editor, tagName) {
    const range = getEditorSelectionRange(editor);
    if (!range) {
        return false;
    }

    const block = document.createElement(tagName);
    if (range.collapsed) {
        block.appendChild(document.createElement("br"));
        range.insertNode(block);
        placeCaretAtBlockStart(block);
        return true;
    }

    const fragment = range.extractContents();
    if (!fragment.childNodes.length) {
        return false;
    }

    block.appendChild(fragment);
    range.insertNode(block);
    selectNodeContents(block);
    return true;
}

function replaceBlockTag(block, tagName) {
    const replacement = document.createElement(tagName);
    if (block.style.cssText) {
        replacement.style.cssText = block.style.cssText;
    }

    if (block.tagName?.toLowerCase() === "pre") {
        const code = ensureCodeElement(block);
        replacement.textContent = code.textContent ?? "";
    } else {
        while (block.firstChild) {
            replacement.appendChild(block.firstChild);
        }
    }

    if (!replacement.childNodes.length) {
        replacement.appendChild(document.createElement("br"));
    }

    block.replaceWith(replacement);
}

function toggleInlineTag(editor, tagName) {
    const range = getEditorSelectionRange(editor);
    if (!range) {
        return false;
    }

    const activeAncestor = getClosestAncestor(range.startContainer, element => element.tagName?.toLowerCase() === tagName, editor);
    if (activeAncestor && activeAncestor.contains(range.endContainer)) {
        unwrapNode(activeAncestor);
        return true;
    }

    return wrapSelectionWithNode(range, () => document.createElement(tagName));
}

function toggleInlineStyle(editor, styleName, styleValue) {
    const range = getEditorSelectionRange(editor);
    if (!range) {
        return false;
    }

    const activeSpan = getClosestAncestor(
        range.startContainer,
        element => element instanceof HTMLElement && hasStyleValue(window.getComputedStyle(element), styleName, styleValue),
        editor
    );

    if (activeSpan && activeSpan.contains(range.endContainer)) {
        if (activeSpan instanceof HTMLElement) {
            clearStyleValue(activeSpan, styleName, styleValue);
            if (!activeSpan.getAttribute("style")) {
                unwrapNode(activeSpan);
            }
        }

        return true;
    }

    return applyInlineStyle(editor, styleName, styleValue);
}

function applyInlineStyle(editor, styleName, styleValue) {
    const range = getEditorSelectionRange(editor);
    if (!range) {
        return false;
    }

    if (range.collapsed) {
        const span = document.createElement("span");
        setStyleValue(span, styleName, styleValue);
        const textNode = document.createTextNode("\u200B");
        span.appendChild(textNode);
        range.insertNode(span);

        const selection = window.getSelection();
        if (selection) {
            const caret = document.createRange();
            caret.setStart(textNode, textNode.textContent?.length ?? 0);
            caret.collapse(true);
            selection.removeAllRanges();
            selection.addRange(caret);
        }

        return true;
    }

    return wrapSelectionWithNode(range, () => {
        const span = document.createElement("span");
        setStyleValue(span, styleName, styleValue);
        return span;
    });
}

function wrapSelectionWithNode(range, createWrapper) {
    const wrapper = createWrapper();
    if (range.collapsed) {
        return false;
    }

    const fragment = range.extractContents();
    wrapper.appendChild(fragment);
    range.insertNode(wrapper);
    selectNodeContents(wrapper);
    return true;
}

function applyTextAlignment(editor, alignment) {
    const blocks = getSelectedBlocks(editor, getEditorSelectionRange(editor));
    if (blocks.length === 0) {
        return false;
    }

    for (const block of blocks) {
        if (!(block instanceof HTMLElement) || block.tagName?.toLowerCase() === "pre") {
            continue;
        }

        block.style.textAlign = alignment === "left" ? "" : alignment;
    }

    return true;
}

function toggleListFormat(editor, listTagName) {
    const range = getEditorSelectionRange(editor);
    const blocks = getSelectedBlocks(editor, range).filter(block => block.tagName?.toLowerCase() !== "pre");
    if (blocks.length === 0) {
        return false;
    }

    if (blocks.every(block => getClosestAncestor(block, element => element.tagName?.toLowerCase() === listTagName, editor))) {
        for (const block of blocks) {
            const item = block.tagName?.toLowerCase() === "li" ? block : getClosestAncestor(block, element => element.tagName?.toLowerCase() === "li", editor);
            if (!(item instanceof HTMLElement)) {
                continue;
            }

            const paragraph = document.createElement("p");
            while (item.firstChild) {
                paragraph.appendChild(item.firstChild);
            }

            item.replaceWith(paragraph);
        }

        cleanupEmptyLists(editor);
        return true;
    }

    const list = document.createElement(listTagName);
    const firstBlock = blocks[0];
    firstBlock.parentNode?.insertBefore(list, firstBlock);
    for (const block of blocks) {
        const item = document.createElement("li");
        while (block.firstChild) {
            item.appendChild(block.firstChild);
        }

        if (!item.childNodes.length) {
            item.appendChild(document.createElement("br"));
        }

        list.appendChild(item);
        block.remove();
    }

    selectNodeContents(list);
    return true;
}

function cleanupEmptyLists(editor) {
    for (const list of editor.querySelectorAll("ol, ul")) {
        if (!list.children.length) {
            list.remove();
        }
    }
}

function adjustBlockIndentation(editor, delta) {
    const blocks = getSelectedBlocks(editor, getEditorSelectionRange(editor));
    if (blocks.length === 0) {
        return false;
    }

    for (const block of blocks) {
        if (!(block instanceof HTMLElement) || block.tagName?.toLowerCase() === "pre") {
            continue;
        }

        const current = Number.parseInt(block.style.marginLeft || "0", 10) || 0;
        const next = Math.max(0, current + delta);
        block.style.marginLeft = next > 0 ? `${next}px` : "";
    }

    return true;
}

function removeSelectedLink(editor) {
    const anchor = getSelectedAnchor(editor);
    if (!anchor) {
        return false;
    }

    unwrapNode(anchor);
    return true;
}

function insertHorizontalRuleAtSelection(editor) {
    const range = getEditorSelectionRange(editor);
    if (!range) {
        return false;
    }

    const hr = document.createElement("hr");
    range.deleteContents();
    range.insertNode(hr);
    range.setStartAfter(hr);
    range.collapse(true);
    const selection = window.getSelection();
    selection?.removeAllRanges();
    selection?.addRange(range);
    return true;
}

function insertLinkAtSelection(editor, url, fallbackText) {
    const range = getEditorSelectionRange(editor);
    if (!range) {
        return false;
    }

    const anchor = document.createElement("a");
    anchor.setAttribute("href", url);
    anchor.setAttribute("target", "_blank");
    anchor.setAttribute("rel", "noopener noreferrer");

    if (range.collapsed) {
        anchor.textContent = fallbackText || url;
        range.insertNode(anchor);
        selectNodeContents(anchor);
        return true;
    }

    const fragment = range.extractContents();
    anchor.appendChild(fragment);
    range.insertNode(anchor);
    selectNodeContents(anchor);
    return true;
}

function insertImageAtSelection(editor, url) {
    const range = getEditorSelectionRange(editor);
    if (!range) {
        return false;
    }

    const image = document.createElement("img");
    image.setAttribute("src", url);
    image.setAttribute("alt", "");
    range.deleteContents();
    range.insertNode(image);
    range.setStartAfter(image);
    range.collapse(true);
    const selection = window.getSelection();
    selection?.removeAllRanges();
    selection?.addRange(range);
    return true;
}

function clearSelectionFormatting(editor) {
    const range = getEditorSelectionRange(editor);
    if (!range) {
        return false;
    }

    const selectedBlocks = getSelectedBlocks(editor, range);
    if (range.collapsed) {
        return clearCurrentLineFormatting(editor, selectedBlocks[0]);
    }

    const selectedListItems = selectedBlocks.filter(block => block.tagName?.toLowerCase() === "li");
    if (selectedListItems.length > 0) {
        return clearSelectedListFormatting(selectedListItems);
    }

    const fragment = sanitizeFormattingFragment(range.extractContents());
    const insertedNodes = Array.from(fragment.childNodes);
    if (insertedNodes.length === 0) {
        return false;
    }

    range.insertNode(fragment);
    selectInsertedNodes(insertedNodes);
    return true;
}

function clearCurrentLineFormatting(editor, block) {
    if (!(block instanceof HTMLElement)) {
        return false;
    }

    if (block.tagName?.toLowerCase() === "li") {
        return clearCurrentListItemFormatting(block);
    }

    const replacement = createPlainBlockReplacement(block);
    const selectionSnapshot = captureSelectionOffsets(block);
    block.replaceWith(replacement);

    if (selectionSnapshot) {
        restoreSelectionOffsets(replacement, selectionSnapshot);
    } else {
        placeCaretAtBlockStart(replacement);
    }

    return true;
}

function clearCurrentListItemFormatting(item) {
    const paragraph = createPlainBlockReplacement(item);
    const selectionSnapshot = captureSelectionOffsets(item);
    replaceSelectedListItems([item], [paragraph]);

    if (selectionSnapshot) {
        restoreSelectionOffsets(paragraph, selectionSnapshot);
    } else {
        placeCaretAtBlockStart(paragraph);
    }

    return true;
}

function clearSelectedListFormatting(items) {
    const replacements = items.map(item => createPlainBlockReplacement(item));
    replaceSelectedListItems(items, replacements);
    selectInsertedNodes(replacements);
    return replacements.length > 0;
}

function replaceSelectedListItems(items, replacements) {
    const replacementsByItem = new Map(items.map((item, index) => [item, replacements[index]]));
    const selectedItems = new Set(items);
    const processedLists = new Set();

    for (const item of items) {
        const list = item.parentElement;
        if (!list || processedLists.has(list)) {
            continue;
        }

        processedLists.add(list);
        const parent = list.parentNode;
        if (!parent) {
            continue;
        }

        const nodesToInsert = [];
        let run = [];
        let runSelected = null;
        for (const child of Array.from(list.children)) {
            const isSelected = selectedItems.has(child);
            if (runSelected === null || runSelected === isSelected) {
                run.push(child);
                runSelected = isSelected;
                continue;
            }

            appendListReplacementRun(nodesToInsert, list, run, runSelected, replacementsByItem);
            run = [child];
            runSelected = isSelected;
        }

        appendListReplacementRun(nodesToInsert, list, run, runSelected, replacementsByItem);

        for (const node of nodesToInsert) {
            parent.insertBefore(node, list);
        }

        list.remove();
    }
}

function appendListReplacementRun(target, templateList, items, isSelected, replacementsByItem) {
    if (items.length === 0) {
        return;
    }

    if (isSelected) {
        for (const item of items) {
            const replacement = replacementsByItem.get(item);
            if (replacement) {
                target.push(replacement);
            }
        }

        return;
    }

    const list = templateList.cloneNode(false);
    for (const item of items) {
        list.appendChild(item);
    }

    if (list.childNodes.length > 0) {
        target.push(list);
    }
}

function createPlainBlockReplacement(block) {
    const replacement = document.createElement("p");
    appendUnformattedChildren(replacement, block);
    ensureBlockHasContent(replacement);
    return replacement;
}

function sanitizeFormattingFragment(fragment) {
    const sanitized = document.createDocumentFragment();
    appendUnformattedChildren(sanitized, fragment);
    return sanitized;
}

function appendUnformattedChildren(target, source) {
    for (const child of Array.from(source.childNodes)) {
        appendUnformattedNode(target, child);
    }
}

function appendUnformattedNode(target, node) {
    if (node.nodeType === Node.TEXT_NODE) {
        target.appendChild(document.createTextNode(node.textContent ?? ""));
        return;
    }

    if (node.nodeType !== Node.ELEMENT_NODE) {
        return;
    }

    const element = node;
    const tagName = element.tagName?.toLowerCase() ?? "";
    if (!tagName) {
        return;
    }

    if (isFormattingOnlyTag(tagName)) {
        appendUnformattedChildren(target, element);
        return;
    }

    if (tagName === "br") {
        target.appendChild(document.createElement("br"));
        return;
    }

    if (tagName === "img" || tagName === "hr") {
        target.appendChild(element.cloneNode(true));
        return;
    }

    if (tagName === "ul" || tagName === "ol") {
        appendUnformattedChildren(target, element);
        return;
    }

    if (tagName === "li") {
        const paragraph = document.createElement("p");
        appendUnformattedChildren(paragraph, element);
        ensureBlockHasContent(paragraph);
        target.appendChild(paragraph);
        return;
    }

    if (isBlockFormattingTag(tagName)) {
        const paragraph = document.createElement("p");
        appendUnformattedChildren(paragraph, element);
        ensureBlockHasContent(paragraph);
        target.appendChild(paragraph);
        return;
    }

    appendUnformattedChildren(target, element);
}

function isFormattingOnlyTag(tagName) {
    return tagName === "a"
        || tagName === "b"
        || tagName === "em"
        || tagName === "font"
        || tagName === "i"
        || tagName === "mark"
        || tagName === "s"
        || tagName === "span"
        || tagName === "strike"
        || tagName === "strong"
        || tagName === "u";
}

function isBlockFormattingTag(tagName) {
    return tagName === "blockquote"
        || tagName === "div"
        || tagName === "h1"
        || tagName === "h2"
        || tagName === "h3"
        || tagName === "h4"
        || tagName === "h5"
        || tagName === "h6"
        || tagName === "p"
        || tagName === "pre";
}

function ensureBlockHasContent(block) {
    if (block.childNodes.length === 0) {
        block.appendChild(document.createElement("br"));
    }
}

function selectInsertedNodes(nodes) {
    const firstNode = nodes[0];
    const lastNode = nodes[nodes.length - 1];
    if (!firstNode || !lastNode) {
        return;
    }

    const selection = window.getSelection();
    if (!selection) {
        return;
    }

    const range = document.createRange();
    range.setStartBefore(firstNode);
    range.setEndAfter(lastNode);
    selection.removeAllRanges();
    selection.addRange(range);
}

function placeCaretAtBlockStart(block) {
    const selection = window.getSelection();
    if (!selection) {
        return;
    }

    const node = getFirstTextNode(block) ?? block;
    const range = document.createRange();
    range.setStart(node, 0);
    range.collapse(true);
    selection.removeAllRanges();
    selection.addRange(range);
}

function getFirstTextNode(container) {
    const walker = document.createTreeWalker(container, NodeFilter.SHOW_TEXT);
    return walker.nextNode();
}

function selectNodeContents(node) {
    const selection = window.getSelection();
    if (!selection) {
        return;
    }

    const range = document.createRange();
    range.selectNodeContents(node);
    selection.removeAllRanges();
    selection.addRange(range);
}

function unwrapNode(node) {
    const parent = node.parentNode;
    if (!parent) {
        return;
    }

    while (node.firstChild) {
        parent.insertBefore(node.firstChild, node);
    }

    node.remove();
}

function getSelectedBlocks(editor, range) {
    const blocks = [];
    const seen = new Set();
    const blockSelector = "p, h1, h2, h3, h4, h5, blockquote, pre, li";
    if (!range) {
        return blocks;
    }

    const blockAncestor = getClosestAncestor(range.startContainer, element => element.matches?.(blockSelector), editor);
    if (range.collapsed && blockAncestor) {
        return [blockAncestor];
    }

    for (const block of editor.querySelectorAll(blockSelector)) {
        if (range.intersectsNode(block) && !seen.has(block)) {
            seen.add(block);
            blocks.push(block);
        }
    }

    return blocks;
}

function setStyleValue(element, styleName, value) {
    switch (styleName) {
        case "fontFamily":
            element.style.fontFamily = value;
            break;
        case "fontSize":
            element.style.fontSize = value;
            break;
        case "color":
            element.style.color = value;
            break;
        case "backgroundColor":
            element.style.backgroundColor = value;
            break;
        case "textDecorationLine":
            element.style.textDecorationLine = value;
            break;
        default:
            element.style[styleName] = value;
            break;
    }
}

function clearStyleValue(element, styleName, value) {
    if (!(element instanceof HTMLElement)) {
        return;
    }

    if (styleName === "textDecorationLine") {
        const remaining = (element.style.textDecorationLine || "")
            .split(" ")
            .map(part => part.trim())
            .filter(part => part && part !== value);
        element.style.textDecorationLine = remaining.join(" ");
        return;
    }

    element.style[styleName] = "";
}

function hasStyleValue(computedStyle, styleName, value) {
    if (!computedStyle) {
        return false;
    }

    const candidate = styleName === "textDecorationLine"
        ? computedStyle.textDecorationLine
        : computedStyle[styleName];
    return `${candidate || ""}`.toLowerCase().includes(`${value}`.toLowerCase());
}

function applyCodeBlockFormat(editor, state) {
    const range = getEditorSelectionRange(editor);
    const isCollapsed = !range || range.collapsed;

    if (range && !range.collapsed) {
        applyCodeBlockToSelection(editor, state, range);
        return;
    }

    const pre = createCollapsedCodeBlock(editor, range);
    if (!pre) {
        return;
    }

    const code = ensureCodeElement(pre);
    normalizeCodeBlockContents(code);
    placeCaretInsideCodeBlock(code, isCollapsed);
    state.activeCodeBlock = pre;
    saveSelection(state);
}

function createCollapsedCodeBlock(editor, range) {
    if (!range) {
        return null;
    }

    const block = getSelectedBlocks(editor, range)[0];
    if (block && block.tagName?.toLowerCase() !== "pre") {
        const pre = document.createElement("pre");
        pre.setAttribute("data-language", "plaintext");
        const code = ensureCodeElement(pre);
        code.textContent = block.textContent ?? "";
        block.replaceWith(pre);
        return pre;
    }

    const pre = document.createElement("pre");
    pre.setAttribute("data-language", "plaintext");
    const code = ensureCodeElement(pre);
    code.textContent = "";
    range.insertNode(pre);
    return pre;
}

function applyCodeBlockToSelection(editor, state, range) {
    const selectedText = extractRangeText(range);
    const pre = document.createElement("pre");
    pre.setAttribute("data-language", "plaintext");

    const code = ensureCodeElement(pre);
    code.textContent = selectedText;
    normalizeCodeBlockContents(code);

    range.deleteContents();
    range.insertNode(pre);

    const selection = window.getSelection();
    if (selection) {
        const newRange = document.createRange();
        newRange.selectNodeContents(code);
        selection.removeAllRanges();
        selection.addRange(newRange);
    }

    state.activeCodeBlock = pre;
    saveSelection(state);
}

function extractRangeText(range) {
    const fragment = range.cloneContents();
    const parts = [];
    collectRangeText(fragment, parts);
    return parts.join("").replaceAll("\u00A0", " ").replace(/\n$/, "");
}

function collectRangeText(node, parts) {
    if (node.nodeType === Node.TEXT_NODE) {
        parts.push(node.textContent ?? "");
        return;
    }

    if (node.nodeType !== Node.ELEMENT_NODE && node.nodeType !== Node.DOCUMENT_FRAGMENT_NODE) {
        return;
    }

    const element = node.nodeType === Node.ELEMENT_NODE ? node : null;
    const tagName = element?.tagName?.toLowerCase() ?? "";
    if (tagName === "br") {
        parts.push("\n");
        return;
    }

    for (const child of node.childNodes) {
        collectRangeText(child, parts);
    }

    if (isBlockTextBoundary(tagName) && !endsWithNewline(parts)) {
        parts.push("\n");
    }
}

function isBlockTextBoundary(tagName) {
    return tagName === "p"
        || tagName === "div"
        || tagName === "li"
        || tagName === "blockquote"
        || tagName === "pre"
        || tagName === "h1"
        || tagName === "h2"
        || tagName === "h3"
        || tagName === "h4"
        || tagName === "h5"
        || tagName === "h6";
}

function endsWithNewline(parts) {
    if (parts.length === 0) {
        return false;
    }

    return parts[parts.length - 1].endsWith("\n");
}

function applyCodeBlockFontSize(editor, value) {
    const pre = getSelectedPre(editor);
    if (!pre) {
        return false;
    }

    const fontSize = `${value || "16px"}`.trim() || "16px";
    pre.style.fontSize = fontSize;

    const code = pre.querySelector(":scope > code");
    if (code instanceof HTMLElement) {
        code.style.fontSize = "inherit";
    }

    return true;
}

function toggleCodeBlockLine(editor, state) {
    const pre = getSelectedPre(editor);
    if (!pre) {
        return;
    }

    const code = ensureCodeElement(pre);
    const selectionSnapshot = captureSelectionOffsets(code) ?? {start: 0, end: 0};
    const text = code.textContent ?? "";
    const lineStart = text.lastIndexOf("\n", Math.max(0, selectionSnapshot.start - 1)) + 1;

    let lineEnd = text.indexOf("\n", selectionSnapshot.end);
    if (lineEnd < 0) {
        lineEnd = text.length;
    }

    let beforeText = text.slice(0, lineStart);
    let currentLineText = text.slice(lineStart, lineEnd);
    let afterText = text.slice(lineEnd);

    if (beforeText.endsWith("\n")) {
        beforeText = beforeText.slice(0, -1);
    }

    if (afterText.startsWith("\n")) {
        afterText = afterText.slice(1);
    }

    const paragraph = document.createElement("p");
    if (currentLineText) {
        paragraph.textContent = currentLineText;
    } else {
        paragraph.appendChild(document.createElement("br"));
    }

    const fragments = [];
    if (beforeText) {
        fragments.push(createCodeBlockFragment(pre, beforeText));
    }

    fragments.push(paragraph);

    if (afterText) {
        fragments.push(createCodeBlockFragment(pre, afterText));
    }

    for (const fragment of fragments) {
        pre.parentNode?.insertBefore(fragment, pre);
    }

    pre.remove();
    state.activeCodeBlock = null;
    placeCaretAtParagraphEnd(paragraph);
    saveSelection(state);
}

function createCodeBlockFragment(templatePre, text) {
    const pre = templatePre.cloneNode(false);
    const code = ensureCodeElement(pre);
    code.textContent = text;
    return pre;
}

function placeCaretAtParagraphEnd(paragraph) {
    const selection = window.getSelection();
    if (!selection) {
        return;
    }

    const range = document.createRange();
    if (paragraph.lastChild?.nodeType === Node.TEXT_NODE) {
        const length = paragraph.lastChild.textContent?.length ?? 0;
        range.setStart(paragraph.lastChild, length);
    } else {
        range.setStart(paragraph, paragraph.childNodes.length);
    }

    range.collapse(true);
    selection.removeAllRanges();
    selection.addRange(range);
}

function isCodeBlockCommandBlocked(editor, command, value) {
    const pre = getSelectedPre(editor);
    if (!pre) {
        return false;
    }

    if (command === "formatBlock" && (value || "").toLowerCase() === "pre") {
        return false;
    }

    return command !== "fontName"
        && command !== "fontSizePx"
        && command !== "undo"
        && command !== "redo";
}

function shouldToggleCurrentCodeBlock(editor) {
    const pre = getSelectedPre(editor);
    if (!pre) {
        return false;
    }

    const selection = window.getSelection();
    if (!selection || selection.rangeCount === 0) {
        return true;
    }

    const range = selection.getRangeAt(0);
    if (range.collapsed) {
        return true;
    }

    return pre.contains(range.startContainer) && pre.contains(range.endContainer);
}

export function promptForLink(editor) {
    const state = editors.get(editor);
    if (!state) {
        return;
    }

    if (getSelectedPre(editor)) {
        void notifyToolbarState(state);
        return;
    }

    restoreSelection(state);
    editor.focus();

    const existingAnchor = getSelectedAnchor(editor);
    const initialValue = existingAnchor?.getAttribute("href") || "https://";
    const rawUrl = window.prompt("Enter link URL", initialValue);
    if (rawUrl === null) {
        return;
    }

    const url = normalizeUrl(rawUrl);
    if (!url) {
        removeSelectedLink(editor);
        saveSelection(state);
        void notifyChange(state);
        return;
    }

    const selection = window.getSelection();
    const selectedText = selection ? selection.toString().trim() : "";
    if (!selectedText) {
        insertLinkAtSelection(editor, url, url);
    } else {
        insertLinkAtSelection(editor, url, null);
    }

    saveSelection(state);
    void notifyChange(state);
}

export function promptForImage(editor) {
    const state = editors.get(editor);
    if (!state) {
        return;
    }

    if (getSelectedPre(editor)) {
        void notifyToolbarState(state);
        return;
    }

    restoreSelection(state);
    editor.focus();

    const rawUrl = window.prompt("Enter image URL", "https://");
    if (rawUrl === null) {
        return;
    }

    const url = normalizeUrl(rawUrl);
    if (!url) {
        return;
    }

    insertImageAtSelection(editor, url);
    saveSelection(state);
    void notifyChange(state);
}

function saveSelection(state) {
    const selection = window.getSelection();
    if (!selection || selection.rangeCount === 0 || !selectionBelongsToEditor(state.editor)) {
        return;
    }

    state.savedRange = selection.getRangeAt(0).cloneRange();
}

function wireToolbar(state) {
    state.onToolbarMouseDown = () => {
        saveSelection(state);
    };

    state.onToolbarChange = event => {
        const target = event.target;
        if (!(target instanceof HTMLElement)) {
            return;
        }

        if (target.matches(".lyo-rte-format-select")) {
            runCommand(state.editor, "formatBlock", target.value || "p");
            return;
        }

        if (target.matches(".lyo-rte-font-select")) {
            runCommand(state.editor, "fontName", extractPrimaryFontFamily(target.value || "Arial, Helvetica, sans-serif"));
            return;
        }

        if (target.matches(".lyo-rte-size-select")) {
            runCommand(state.editor, "fontSizePx", target.value || "16px");
            return;
        }

        if (target.matches(".lyo-rte-code-language-select")) {
            applyCodeLanguage(state.editor, target.value || "plaintext");
        }
    };

    state.onToolbarInput = event => {
        const target = event.target;
        if (!(target instanceof HTMLElement) || !target.matches(".lyo-rte-color-picker")) {
            return;
        }

        const role = target.dataset?.colorRole;
        const value = target.value;
        if (!value) {
            return;
        }

        runCommand(state.editor, role === "highlight" ? "hiliteColor" : "foreColor", value);
    };

    state.root.addEventListener("mousedown", state.onToolbarMouseDown, true);
    state.root.addEventListener("change", state.onToolbarChange);
    state.root.addEventListener("input", state.onToolbarInput);
}

function unwireToolbar(state) {
    state.root?.removeEventListener("mousedown", state.onToolbarMouseDown, true);
    state.root?.removeEventListener("change", state.onToolbarChange);
    state.root?.removeEventListener("input", state.onToolbarInput);
    if (state.highlightTimer) {
        window.clearTimeout(state.highlightTimer);
        state.highlightTimer = 0;
    }
}

function restoreSelection(state) {
    if (!state.savedRange) {
        return;
    }

    const selection = window.getSelection();
    if (!selection) {
        return;
    }

    selection.removeAllRanges();
    selection.addRange(state.savedRange);
}

function resetHistory(state, html) {
    state.history = [];
    state.historyIndex = -1;
    pushHistorySnapshot(state, html);
}

function pushHistorySnapshot(state, html) {
    if (state.suppressHistory) {
        return;
    }

    const normalizedHtml = normalizeHtml(html);
    if (state.historyIndex >= 0 && state.history[state.historyIndex] === normalizedHtml) {
        return;
    }

    state.history = state.history.slice(0, state.historyIndex + 1);
    state.history.push(normalizedHtml);
    state.historyIndex = state.history.length - 1;
}

function restoreHistorySnapshot(state, direction) {
    const nextIndex = state.historyIndex + direction;
    if (nextIndex < 0 || nextIndex >= state.history.length) {
        return;
    }

    state.historyIndex = nextIndex;
    state.suppressHistory = true;
    setHtml(state.editor, state.history[state.historyIndex]);
    state.suppressHistory = false;
    saveSelection(state);
    syncActiveCodeBlock(state);
    scheduleCodeHighlighting(state, 0, true);
    void notifyChange(state);
}

function insertTextAtSelection(text) {
    const selection = window.getSelection();
    if (!selection || selection.rangeCount === 0) {
        return;
    }

    const range = selection.getRangeAt(0);
    range.deleteContents();

    const node = document.createTextNode(text);
    range.insertNode(node);

    range.setStartAfter(node);
    range.collapse(true);
    selection.removeAllRanges();
    selection.addRange(range);
}

function selectionBelongsToEditor(editor) {
    const selection = window.getSelection();
    if (!selection || selection.rangeCount === 0) {
        return false;
    }

    const node = selection.anchorNode;
    return !!node && (node === editor || editor.contains(node));
}

async function notifyChange(state) {
    normalizeEditorMarkup(state.editor);
    const html = await serializeEditorHtml(state.editor);
    pushHistorySnapshot(state, html);
    await state.dotNetRef.invokeMethodAsync("NotifyHtmlChanged", html);
    await notifyToolbarState(state);
}

async function notifyToolbarState(state) {
    const toolbarState = getToolbarState(state.editor);
    const nextKey = JSON.stringify(toolbarState);
    if (nextKey === state.lastToolbarStateKey) {
        return;
    }

    state.lastToolbarStateKey = nextKey;
    await state.dotNetRef.invokeMethodAsync("NotifyToolbarState", toolbarState);
}

function getToolbarState(editor) {
    const selectedElement = getSelectedElement(editor);
    const computedStyle = selectedElement ? window.getComputedStyle(selectedElement) : null;
    const state = editors.get(editor);

    return {
        blockTag: getCurrentBlockTag(editor),
        codeLanguage: getCodeLanguage(editor),
        fontFamily: getNormalizedFontFamily(computedStyle?.fontFamily),
        fontSize: getNormalizedFontSize(computedStyle?.fontSize),
        foreColor: toHexColor(computedStyle?.color, "#000000"),
        highlightColor: getHighlightColor(editor, selectedElement),
        isBold: isSelectionBold(editor),
        isItalic: isSelectionItalic(editor),
        isUnderline: hasTextDecoration(editor, "underline"),
        isStrikeThrough: hasTextDecoration(editor, "line-through"),
        isOrderedList: isSelectionInList(editor, "ol"),
        isUnorderedList: isSelectionInList(editor, "ul"),
        isAlignLeft: getSelectionAlignment(editor) === "left",
        isAlignCenter: getSelectionAlignment(editor) === "center",
        isAlignRight: getSelectionAlignment(editor) === "right",
        isLink: !!getSelectedAnchor(editor),
        canUndo: !!state && state.historyIndex > 0,
        canRedo: !!state && state.historyIndex >= 0 && state.historyIndex < state.history.length - 1
    };
}

function getCodeLanguage(editor) {
    const pre = getSelectedPre(editor);
    if (!pre) {
        return "plaintext";
    }

    const dataLanguage = pre.getAttribute("data-language");
    if (dataLanguage) {
        return dataLanguage;
    }

    const code = pre.querySelector("code");
    for (const className of code?.classList || []) {
        if (className.startsWith("language-")) {
            return className.slice("language-".length);
        }
    }

    return "plaintext";
}

function syncActiveCodeBlock(state) {
    if (state.suspendCodeBlockTracking && !isRootFocused(state.root)) {
        return;
    }

    state.suspendCodeBlockTracking = false;
    const activePre = getSelectedPre(state.editor);
    if (state.activeCodeBlock !== activePre) {
        if (state.activeCodeBlock) {
            void highlightCodeBlock(state.activeCodeBlock);
        }

        state.activeCodeBlock = activePre;
    }
}

async function finalizeActiveCodeBlock(state) {
    if (!state.activeCodeBlock) {
        state.suspendCodeBlockTracking = true;
        return;
    }

    const pre = state.activeCodeBlock;
    window.clearTimeout(state.highlightTimer);
    state.highlightTimer = 0;
    state.highlightVersion += 1;
    await highlightCodeBlock(pre);
    state.activeCodeBlock = null;
    state.suspendCodeBlockTracking = true;
    await notifyChange(state);
}

function scheduleCodeHighlighting(state, delayMs, includeActive = false) {
    state.highlightVersion += 1;
    const version = state.highlightVersion;
    if (state.highlightTimer) {
        window.clearTimeout(state.highlightTimer);
    }

    state.highlightTimer = window.setTimeout(() => {
        state.highlightTimer = 0;
        void highlightCodeBlocks(state, version, includeActive);
    }, Math.max(0, delayMs));
}

async function highlightCodeBlocks(state, version, includeActive) {
    const blocks = state.editor.querySelectorAll("pre");
    for (const pre of blocks) {
        if (version !== state.highlightVersion) {
            return;
        }

        if (pre !== state.activeCodeBlock || includeActive) {
            await highlightCodeBlock(pre);
        }
    }
}

async function ensureHighlightJs() {
    if (!highlightJsPromise) {
        highlightJsPromise = import("https://cdn.jsdelivr.net/npm/highlight.js/+esm")
            .then(module => module.default || module)
            .catch(() => null);
    }

    return await highlightJsPromise;
}

async function highlightCodeBlock(pre) {
    if (!pre) {
        return;
    }

    const hljs = await ensureHighlightJs();
    if (!hljs) {
        return;
    }

    const code = ensureCodeElement(pre);
    const language = getPreLanguage(pre);

    clearHighlightedCodeBlock(pre);
    code.classList.add("hljs");
    if (language && language !== "plaintext") {
        code.classList.add(`language-${language}`);
    }

    if (!code.textContent?.trim()) {
        return;
    }

    try {
        if (language && language !== "plaintext") {
            code.innerHTML = hljs.highlight(code.textContent, {language}).value;
            return;
        }

        code.innerHTML = hljs.highlightAuto(code.textContent).value;
    } catch {
        code.textContent = code.textContent;
    }
}

function clearHighlightedCodeBlock(pre, preserveSelection = false) {
    if (!pre) {
        return;
    }

    const code = ensureCodeElement(pre);
    const language = getPreLanguage(pre);
    const selectionSnapshot = preserveSelection ? captureSelectionOffsets(code) : null;
    const text = code.textContent ?? "";
    code.textContent = text;
    code.className = "";
    if (language && language !== "plaintext") {
        code.classList.add(`language-${language}`);
    }

    if (selectionSnapshot) {
        restoreSelectionOffsets(code, selectionSnapshot);
    }
}

function prepareCodeBlockForEditing(state, pre) {
    if (!pre) {
        return;
    }

    const code = ensureCodeElement(pre);
    if (!code.classList.contains("hljs")) {
        return;
    }

    clearHighlightedCodeBlock(pre, true);
    state.activeCodeBlock = pre;
    saveSelection(state);
}

function isEditingKey(event) {
    return event.key === "Backspace"
        || event.key === "Delete"
        || event.key === "Enter"
        || event.key === "Tab"
        || event.key.length === 1;
}

function isRootFocused(root) {
    const activeElement = document.activeElement;
    return !!activeElement && root.contains(activeElement);
}

function ensureCodeElement(pre) {
    let code = pre.querySelector(":scope > code");
    if (!code) {
        code = document.createElement("code");
        while (pre.firstChild) {
            code.appendChild(pre.firstChild);
        }
        pre.appendChild(code);
    }

    pre.setAttribute("spellcheck", "false");
    code.setAttribute("spellcheck", "false");

    return code;
}

function normalizeCodeBlockContents(code) {
    const childNodes = Array.from(code.childNodes);
    for (const node of childNodes) {
        if (node.nodeType === Node.ELEMENT_NODE && node.nodeName === "BR" && childNodes.length === 1) {
            node.remove();
        }
    }
}

function placeCaretInsideCodeBlock(code, moveToEnd) {
    const selection = window.getSelection();
    if (!selection) {
        return;
    }

    if (!code.firstChild) {
        code.appendChild(document.createTextNode(""));
    }

    const targetNode = getLastTextNode(code) || code.firstChild;
    if (!targetNode) {
        return;
    }

    const range = document.createRange();
    if (targetNode.nodeType === Node.TEXT_NODE) {
        const offset = moveToEnd ? (targetNode.textContent?.length ?? 0) : 0;
        range.setStart(targetNode, offset);
    } else {
        range.setStart(targetNode, 0);
    }

    range.collapse(true);
    selection.removeAllRanges();
    selection.addRange(range);
}

function getLastTextNode(container) {
    const walker = document.createTreeWalker(container, NodeFilter.SHOW_TEXT);
    let last = null;
    while (walker.nextNode()) {
        last = walker.currentNode;
    }

    return last;
}

function getPreLanguage(pre) {
    const dataLanguage = pre.getAttribute("data-language");
    if (dataLanguage) {
        return dataLanguage;
    }

    const code = pre.querySelector(":scope > code");
    for (const className of code?.classList || []) {
        if (className.startsWith("language-")) {
            return className.slice("language-".length);
        }
    }

    return "plaintext";
}

async function serializeEditorHtml(editor) {
    const clone = editor.cloneNode(true);
    const hljs = await ensureHighlightJs();
    for (const pre of clone.querySelectorAll("pre")) {
        const language = getPreLanguage(pre);
        const code = ensureCodeElement(pre);
        const text = code.textContent ?? "";

        code.className = "";
        if (language) {
            pre.setAttribute("data-language", language);
        }

        code.classList.add("hljs");
        if (language && language !== "plaintext") {
            code.classList.add(`language-${language}`);
        }

        if (!text.trim()) {
            code.textContent = text;
            continue;
        }

        if (!hljs) {
            code.textContent = text;
            continue;
        }

        try {
            if (language && language !== "plaintext") {
                code.innerHTML = hljs.highlight(text, {language}).value;
            } else {
                code.innerHTML = hljs.highlightAuto(text).value;
            }
        } catch {
            code.textContent = text;
        }
    }

    return normalizeHtml(clone.innerHTML);
}

function captureSelectionOffsets(container) {
    const selection = window.getSelection();
    if (!selection || selection.rangeCount === 0) {
        return null;
    }

    const range = selection.getRangeAt(0);
    if (!container.contains(range.startContainer) || !container.contains(range.endContainer)) {
        return null;
    }

    return {
        start: getTextOffset(container, range.startContainer, range.startOffset),
        end: getTextOffset(container, range.endContainer, range.endOffset)
    };
}

function restoreSelectionOffsets(container, snapshot) {
    if (!snapshot) {
        return;
    }

    const selection = window.getSelection();
    if (!selection) {
        return;
    }

    const start = resolveTextOffset(container, snapshot.start);
    const end = resolveTextOffset(container, snapshot.end);
    if (!start || !end) {
        return;
    }

    const range = document.createRange();
    range.setStart(start.node, start.offset);
    range.setEnd(end.node, end.offset);
    selection.removeAllRanges();
    selection.addRange(range);
}

function getTextOffset(container, targetNode, targetOffset) {
    try {
        const range = document.createRange();
        range.selectNodeContents(container);
        range.setEnd(targetNode, targetOffset);
        return range.toString().length;
    } catch {
        return container.textContent?.length ?? 0;
    }
}

function resolveTextOffset(container, targetOffset) {
    let offset = Math.max(0, targetOffset);
    const walker = document.createTreeWalker(container, NodeFilter.SHOW_TEXT);
    while (walker.nextNode()) {
        const node = walker.currentNode;
        const length = node.textContent?.length ?? 0;
        if (offset <= length) {
            return {node, offset};
        }

        offset -= length;
    }

    if (container.lastChild?.nodeType === Node.TEXT_NODE) {
        const text = container.lastChild.textContent?.length ?? 0;
        return {node: container.lastChild, offset: text};
    }

    const fallback = document.createTextNode("");
    container.appendChild(fallback);
    return {node: fallback, offset: 0};
}

function extractPrimaryFontFamily(value) {
    const text = `${value || ""}`.trim();
    if (!text) {
        return "Arial";
    }

    const first = text.split(",")[0].trim();
    return first.replace(/^['"]|['"]$/g, "");
}

function getSelectedPre(editor) {
    const selection = window.getSelection();
    let node = selection?.anchorNode;
    if (!node) {
        return null;
    }

    if (node.nodeType === Node.TEXT_NODE) {
        node = node.parentElement;
    }

    while (node && node !== editor) {
        if (node.tagName?.toLowerCase() === "pre") {
            return node;
        }

        node = node.parentElement;
    }

    return null;
}

function applyCodeLanguage(editor, language) {
    const state = editors.get(editor);
    if (!state) {
        return;
    }

    restoreSelection(state);
    editor.focus();

    const pre = getSelectedPre(editor);
    if (!pre) {
        return;
    }

    const normalizedLanguage = `${language || "plaintext"}`.trim().toLowerCase() || "plaintext";
    pre.setAttribute("data-language", normalizedLanguage);

    let code = ensureCodeElement(pre);

    const classesToRemove = [];
    for (const className of code.classList) {
        if (className.startsWith("language-")) {
            classesToRemove.push(className);
        }
    }

    for (const className of classesToRemove) {
        code.classList.remove(className);
    }

    code.classList.add(`language-${normalizedLanguage}`);
    saveSelection(state);
    syncActiveCodeBlock(state);
    scheduleCodeHighlighting(state, 0);
    void notifyChange(state);
}

function getSelectedElement(editor) {
    const selection = window.getSelection();
    let node = selection?.anchorNode || editor;
    if (node.nodeType === Node.TEXT_NODE) {
        node = node.parentElement;
    }

    return node && (node === editor || editor.contains(node)) ? node : editor;
}

function getCurrentBlockTag(editor) {
    const selection = window.getSelection();
    let node = selection?.anchorNode || editor;
    if (node.nodeType === Node.TEXT_NODE) {
        node = node.parentElement;
    }

    while (node && node !== editor) {
        const tagName = node.tagName?.toLowerCase();
        if (tagName === "p" || tagName === "h1" || tagName === "h2" || tagName === "h3" || tagName === "h4" || tagName === "h5" || tagName === "blockquote" || tagName === "pre") {
            return tagName;
        }

        node = node.parentElement;
    }

    return "p";
}

function getSelectedAnchor(editor) {
    const selection = window.getSelection();
    let node = selection?.anchorNode;
    if (!node) {
        return null;
    }

    if (node.nodeType === Node.TEXT_NODE) {
        node = node.parentElement;
    }

    while (node && node !== editor) {
        if (node.tagName?.toLowerCase() === "a") {
            return node;
        }

        node = node.parentElement;
    }

    return null;
}

function isSelectionBold(editor) {
    const selectedElement = getSelectedElement(editor);
    const computedStyle = selectedElement ? window.getComputedStyle(selectedElement) : null;
    const weight = Number.parseInt(computedStyle?.fontWeight || "400", 10);
    return weight >= 600 || !!getClosestAncestor(selectedElement, element => ["strong", "b"].includes(element.tagName?.toLowerCase()), editor);
}

function isSelectionItalic(editor) {
    const selectedElement = getSelectedElement(editor);
    const computedStyle = selectedElement ? window.getComputedStyle(selectedElement) : null;
    return (computedStyle?.fontStyle || "").includes("italic")
        || !!getClosestAncestor(selectedElement, element => ["em", "i"].includes(element.tagName?.toLowerCase()), editor);
}

function hasTextDecoration(editor, decoration) {
    const selectedElement = getSelectedElement(editor);
    const computedStyle = selectedElement ? window.getComputedStyle(selectedElement) : null;
    return `${computedStyle?.textDecorationLine || ""}`.includes(decoration);
}

function isSelectionInList(editor, tagName) {
    const selectedElement = getSelectedElement(editor);
    return !!getClosestAncestor(selectedElement, element => element.tagName?.toLowerCase() === tagName, editor);
}

function getSelectionAlignment(editor) {
    const blocks = getSelectedBlocks(editor, getEditorSelectionRange(editor));
    const block = blocks[0];
    if (!(block instanceof HTMLElement)) {
        return "left";
    }

    const value = (window.getComputedStyle(block).textAlign || "").toLowerCase();
    return value === "center" || value === "right" ? value : "left";
}

function normalizeHtml(html) {
    const normalized = (html ?? "").replaceAll("\u200B", "").trim();
    if (normalized === "" || normalized === "<br>" || normalized === "<div><br></div>" || normalized === "<p><br></p>") {
        return "";
    }

    return normalized;
}

function normalizeEditorMarkup(editor) {
    if (!editor) {
        return;
    }

    const fontElements = editor.querySelectorAll("font");
    for (const fontElement of fontElements) {
        const span = document.createElement("span");
        const size = fontElement.getAttribute("size");
        const color = fontElement.getAttribute("color");
        const face = fontElement.getAttribute("face");

        if (size) {
            span.style.fontSize = mapLegacyFontSizeToPx(size);
        }

        if (color) {
            span.style.color = color;
        }

        if (face) {
            span.style.fontFamily = face;
        }

        while (fontElement.firstChild) {
            span.appendChild(fontElement.firstChild);
        }

        fontElement.replaceWith(span);
    }
}

function mapLegacyFontSizeToPx(value) {
    switch (`${value}`) {
        case "1":
            return "10px";
        case "2":
            return "12px";
        case "3":
            return "14px";
        case "4":
            return "16px";
        case "5":
            return "18px";
        case "6":
            return "24px";
        case "7":
            return "32px";
        default:
            return "16px";
    }
}

function getNormalizedFontFamily(value) {
    const normalized = `${value || ""}`.toLowerCase();
    if (normalized.includes("times new roman") || normalized.includes("times")) {
        return "'Times New Roman', Times, serif";
    }

    if (normalized.includes("georgia")) {
        return "Georgia, serif";
    }

    if (normalized.includes("courier")) {
        return "'Courier New', Courier, monospace";
    }

    if (normalized.includes("verdana")) {
        return "Verdana, Geneva, sans-serif";
    }

    return "Arial, Helvetica, sans-serif";
}

function getNormalizedFontSize(value) {
    const px = Number.parseInt(`${value || "16px"}`.replace("px", ""), 10);
    if (px <= 12) {
        return "12px";
    }

    if (px <= 14) {
        return "14px";
    }

    if (px <= 16) {
        return "16px";
    }

    if (px <= 18) {
        return "18px";
    }

    if (px <= 24) {
        return "24px";
    }

    return "32px";
}

function getHighlightColor(editor, selectedElement) {
    let node = selectedElement;
    while (node && node !== editor) {
        const backgroundColor = window.getComputedStyle(node).backgroundColor;
        if (backgroundColor && backgroundColor !== "rgba(0, 0, 0, 0)" && backgroundColor !== "transparent") {
            return toHexColor(backgroundColor, "#ffff00");
        }

        node = node.parentElement;
    }

    return "#ffff00";
}

function toHexColor(value, fallback) {
    if (!value) {
        return fallback;
    }

    if (value.startsWith("#")) {
        return value;
    }

    const match = value.match(/\d+(\.\d+)?/g);
    if (!match || match.length < 3) {
        return fallback;
    }

    const [r, g, b] = match.slice(0, 3).map(component => {
        const intValue = Math.max(0, Math.min(255, Math.round(Number.parseFloat(component))));
        return intValue.toString(16).padStart(2, "0");
    });

    return `#${r}${g}${b}`;
}

function normalizeUrl(url) {
    const value = (url ?? "").trim();
    if (!value) {
        return "";
    }

    if (value.startsWith("http://") || value.startsWith("https://") || value.startsWith("mailto:") || value.startsWith("tel:")) {
        return value;
    }

    return `https://${value}`;
}
