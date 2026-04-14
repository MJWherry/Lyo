const states = new WeakMap();

function getState(canvas) {
    let state = states.get(canvas);
    if (!state) {
        state = {
            canvas,
            ctx: canvas.getContext("2d"),
            currentFrame: 0,
            fps: 12,
            frames: [],
            image: null,
            isPlaying: false,
            rafId: 0,
            source: null,
            loadGeneration: 0,
            tick: null,
            lastTimestamp: 0
        };
        states.set(canvas, state);
    }

    return state;
}

function cancelPlayback(state) {
    if (state.rafId) {
        cancelAnimationFrame(state.rafId);
        state.rafId = 0;
    }
}

function clearCanvas(state) {
    const width = state.canvas.width || 320;
    const height = state.canvas.height || 320;
    state.canvas.width = width;
    state.canvas.height = height;
    state.ctx.clearRect(0, 0, width, height);
}

function computeScale(frame) {
    const maxCanvasSize = 480;
    const widthScale = Math.max(1, Math.floor(maxCanvasSize / Math.max(1, frame.width)));
    const heightScale = Math.max(1, Math.floor(maxCanvasSize / Math.max(1, frame.height)));
    return Math.max(1, Math.min(widthScale, heightScale));
}

function render(state) {
    if (!state.image || state.frames.length === 0) {
        clearCanvas(state);
        return;
    }

    const frame = state.frames[state.currentFrame] ?? state.frames[0];
    const scale = computeScale(frame);
    const targetWidth = Math.max(1, frame.width * scale);
    const targetHeight = Math.max(1, frame.height * scale);

    if (state.canvas.width !== targetWidth || state.canvas.height !== targetHeight) {
        state.canvas.width = targetWidth;
        state.canvas.height = targetHeight;
    }

    state.ctx.imageSmoothingEnabled = false;
    state.ctx.clearRect(0, 0, targetWidth, targetHeight);
    state.ctx.drawImage(
        state.image,
        frame.x,
        frame.y,
        frame.width,
        frame.height,
        0,
        0,
        targetWidth,
        targetHeight
    );
}

function startPlayback(state) {
    if (state.isPlaying || state.frames.length === 0) {
        render(state);
        return;
    }

    state.isPlaying = true;
    state.lastTimestamp = 0;
    state.tick = timestamp => {
        if (!state.isPlaying) {
            cancelPlayback(state);
            return;
        }

        const frameDuration = 1000 / Math.max(1, state.fps);
        if (!state.lastTimestamp) {
            state.lastTimestamp = timestamp;
        }

        if (timestamp - state.lastTimestamp >= frameDuration) {
            const elapsedFrames = Math.max(1, Math.floor((timestamp - state.lastTimestamp) / frameDuration));
            state.currentFrame = (state.currentFrame + elapsedFrames) % state.frames.length;
            state.lastTimestamp = timestamp;
            render(state);
        }

        state.rafId = requestAnimationFrame(state.tick);
    };

    state.rafId = requestAnimationFrame(state.tick);
}

async function loadImage(source) {
    if (!source) {
        return null;
    }

    const image = new Image();
    image.decoding = "async";
    image.src = source;

    if (image.decode) {
        await image.decode();
    } else {
        await new Promise((resolve, reject) => {
            image.onload = () => resolve();
            image.onerror = error => reject(error);
        });
    }

    return image;
}

export function initialize(canvas) {
    const state = getState(canvas);
    clearCanvas(state);
}

export async function setSpriteSheet(canvas, options) {
    const state = getState(canvas);
    const nextSource = options?.source ?? null;
    if (state.source !== nextSource) {
        state.loadGeneration = (state.loadGeneration ?? 0) + 1;
        const gen = state.loadGeneration;
        state.source = nextSource;
        state.image = nextSource ? await loadImage(nextSource) : null;
        if (gen !== state.loadGeneration) {
            return;
        }
    }

    state.frames = Array.isArray(options?.frames) ? options.frames : [];
    state.fps = Math.max(1, Math.min(60, options?.fps ?? 12));

    if (state.frames.length === 0) {
        state.currentFrame = 0;
        state.isPlaying = false;
        cancelPlayback(state);
        clearCanvas(state);
        return;
    }

    state.currentFrame = Math.max(0, Math.min(state.frames.length - 1, options?.currentFrame ?? state.currentFrame));
    render(state);
}

export function play(canvas) {
    const state = getState(canvas);
    startPlayback(state);
}

export function pause(canvas) {
    const state = getState(canvas);
    state.isPlaying = false;
    cancelPlayback(state);
    render(state);
}

export function reset(canvas) {
    const state = getState(canvas);
    state.isPlaying = false;
    state.currentFrame = 0;
    cancelPlayback(state);
    render(state);
}

export function setFrame(canvas, frameIndex) {
    const state = getState(canvas);
    if (state.frames.length === 0) {
        clearCanvas(state);
        return;
    }

    state.currentFrame = Math.max(0, Math.min(state.frames.length - 1, frameIndex ?? 0));
    state.isPlaying = false;
    cancelPlayback(state);
    render(state);
}

export function setFps(canvas, fps) {
    const state = getState(canvas);
    state.fps = Math.max(1, Math.min(60, fps ?? 12));
}

export function getCurrentFrame(canvas) {
    const state = getState(canvas);
    if (!state.frames || state.frames.length === 0) {
        return 0;
    }

    return state.currentFrame;
}

export function dispose(canvas) {
    const state = getState(canvas);
    state.isPlaying = false;
    cancelPlayback(state);
    states.delete(canvas);
}
