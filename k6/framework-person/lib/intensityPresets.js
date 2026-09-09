import { env } from "./env.js";
import { assertAxisValue, DEFAULT_INTENSITY, INTENSITIES, PROFILES } from "./matrixAxes.js";

/**
 * Sole owner of low/med/high numeric tables. No k6 options knowledge —
 * ProfileOptionsBuilder (profiles.js) maps these knobs into executors.
 */
const PRESETS = Object.freeze({
  load: Object.freeze({
    low: Object.freeze({ rate: 7, duration: "3m", preAllocatedVUs: 6, maxVUs: 12 }),
    med: Object.freeze({ rate: 25, duration: "5m", preAllocatedVUs: 20, maxVUs: 40 }),
    high: Object.freeze({ rate: 40, duration: "5m", preAllocatedVUs: 30, maxVUs: 60 }),
  }),
  stress: Object.freeze({
    low: Object.freeze({ startVUs: 5, target1: 15, target2: 25 }),
    med: Object.freeze({ startVUs: 10, target1: 30, target2: 50 }),
    high: Object.freeze({ startVUs: 15, target1: 45, target2: 75 }),
  }),
  spike: Object.freeze({
    low: Object.freeze({ startRate: 5, targetRate: 40, recoverRate: 10, preAllocatedVUs: 10, maxVUs: 60 }),
    med: Object.freeze({ startRate: 15, targetRate: 100, recoverRate: 25, preAllocatedVUs: 25, maxVUs: 120 }),
    high: Object.freeze({ startRate: 25, targetRate: 150, recoverRate: 40, preAllocatedVUs: 40, maxVUs: 180 }),
  }),
  soak: Object.freeze({
    low: Object.freeze({ vus: 5, duration: "30m" }),
    med: Object.freeze({ vus: 15, duration: "1h" }),
    high: Object.freeze({ vus: 25, duration: "2h" }),
  }),
  ceiling: Object.freeze({
    low: Object.freeze({ rates: "10,25,50,75,100" }),
    med: Object.freeze({ rates: "25,50,100,150,200,300" }),
    high: Object.freeze({ rates: "25,50,100,150,200,300,450,700,1000" }),
  }),
});

export class IntensityPresets {
  /** @returns {"low"|"med"|"high"} */
  static resolveIntensity(raw = env("INTENSITY", DEFAULT_INTENSITY)) {
    return assertAxisValue(String(raw || "").trim().toLowerCase(), INTENSITIES, "intensity");
  }

  /**
   * @param {string} profile
   * @param {string} [intensity]
   * @returns {Readonly<Record<string, unknown>>}
   */
  static forProfile(profile, intensity = IntensityPresets.resolveIntensity()) {
    const p = assertAxisValue(String(profile || "").trim(), PROFILES, "profile");
    const i = IntensityPresets.resolveIntensity(intensity);
    const table = PRESETS[p];
    if (!table) {
      throw new Error(`No intensity presets for profile '${p}'`);
    }
    return table[i];
  }

  /** Snapshot of all presets (docs / tooling). */
  static all() {
    return PRESETS;
  }
}
