/** Canonical matrix axes shared by cells, presets, and docs. */

export const MATRIX_SEED = 20260623;

export const ENDPOINTS = Object.freeze(["query", "queryproject", "queryroot"]);
export const PROFILES = Object.freeze(["load", "stress", "spike", "soak", "ceiling"]);
export const INTENSITIES = Object.freeze(["low", "med", "high"]);
export const CACHE_MODES = Object.freeze(["uncached", "cached"]);

export const DEFAULT_INTENSITY = "med";
export const DEFAULT_CACHE_MODE = "uncached";

/**
 * @param {string} value
 * @param {readonly string[]} allowed
 * @param {string} label
 */
export function assertAxisValue(value, allowed, label) {
  if (!allowed.includes(value)) {
    throw new Error(`Unknown ${label} '${value}'. Expected one of: ${allowed.join(", ")}`);
  }
  return value;
}
