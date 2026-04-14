export function getSplitPercent(element, clientX) {
    if (!element || typeof element.getBoundingClientRect !== "function")
        return 50;

    const rect = element.getBoundingClientRect();
    if (!rect || rect.width <= 0)
        return 50;

    const relativeX = clientX - rect.left;
    return (relativeX / rect.width) * 100;
}

/**
 * Uses X-axis when flex is row (side-by-side); Y-axis when flex is column (stacked / narrow view).
 */
export function getSplitPercentAdaptive(element, clientX, clientY) {
    if (!element || typeof element.getBoundingClientRect !== "function")
        return 50;

    const rect = element.getBoundingClientRect();
    let flexDir = "row";
    try {
        flexDir = getComputedStyle(element).flexDirection || "row";
    } catch {
        /* ignore */
    }

    if (flexDir === "column" || flexDir === "column-reverse") {
        if (rect.height <= 0)
            return 50;

        const relativeY = clientY - rect.top;
        return (relativeY / rect.height) * 100;
    }

    if (rect.width <= 0)
        return 50;

    const relativeX = clientX - rect.left;
    return (relativeX / rect.width) * 100;
}
