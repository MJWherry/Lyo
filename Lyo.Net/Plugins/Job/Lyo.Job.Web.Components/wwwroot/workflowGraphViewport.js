export function origin(element, clientX, clientY) {
    const rect = element.getBoundingClientRect();
    return {x: clientX - rect.left, y: clientY - rect.top};
}

export function center(element) {
    const rect = element.getBoundingClientRect();
    return {x: rect.width / 2, y: rect.height / 2};
}

export function capture(element, pointerId) {
    element.setPointerCapture(pointerId);
}
