import { env, toBool, toInt } from "./env.js";
import {
  assertAxisValue,
  CACHE_MODES,
  DEFAULT_CACHE_MODE,
  DEFAULT_INTENSITY,
  INTENSITIES,
  MATRIX_SEED,
  PROFILES,
} from "./matrixAxes.js";

/**
 * Immutable identity for one matrix run cell:
 * endpoint × profile × intensity × cacheMode (+ seed).
 */
export class MatrixCell {
  /**
   * @param {{
   *   endpointKind: string,
   *   profile: string,
   *   intensity?: string,
   *   cacheMode?: string,
   *   seed?: number,
   * }} args
   */
  constructor({ endpointKind, profile, intensity = DEFAULT_INTENSITY, cacheMode = DEFAULT_CACHE_MODE, seed = MATRIX_SEED }) {
    this.endpointKind = String(endpointKind || "").trim();
    this.profile = assertAxisValue(String(profile || "").trim(), PROFILES, "profile");
    this.intensity = assertAxisValue(String(intensity || "").trim().toLowerCase(), INTENSITIES, "intensity");
    this.cacheMode = assertAxisValue(String(cacheMode || "").trim().toLowerCase(), CACHE_MODES, "cacheMode");
    this.seed = (Number(seed) >>> 0) || MATRIX_SEED;
    this.cellId = `${this.endpointKind}_${this.profile}_${this.intensity}_${this.cacheMode}`;
    Object.freeze(this);
  }

  /** @returns {boolean} */
  get isCached() {
    return this.cacheMode === "cached";
  }

  /**
   * Build a cell from env, fixing endpoint/profile from the scenario stub.
   * @param {{ endpointKind: string, profile: string }} fixed
   */
  static fromEnv({ endpointKind, profile }) {
    const intensity = env("INTENSITY", DEFAULT_INTENSITY).toLowerCase().trim();
    const cacheMode = MatrixCell.resolveCacheModeFromEnv();
    const seed = toInt("RANDOM_SEED", MATRIX_SEED);
    return new MatrixCell({ endpointKind, profile, intensity, cacheMode, seed });
  }

  /** Prefer CACHE_MODE=cached|uncached; fall back to CACHE_HIT_MODE bool. */
  static resolveCacheModeFromEnv() {
    const raw = env("CACHE_MODE", "").toLowerCase().trim();
    if (raw) {
      return assertAxisValue(raw, CACHE_MODES, "CACHE_MODE");
    }
    return toBool("CACHE_HIT_MODE", false) ? "cached" : "uncached";
  }

  /** Tags attached to k6 options / custom metrics. */
  toK6Tags({ testTag } = {}) {
    return {
      suite: "framework-person",
      endpoint: this.endpointKind,
      profile: this.profile,
      intensity: this.intensity,
      cache_mode: this.cacheMode,
      cell: this.cellId,
      test: testTag || this.cellId,
    };
  }

  /** Env vars that fully identify this cell for a k6 process. */
  toEnv() {
    return {
      INTENSITY: this.intensity,
      CACHE_MODE: this.cacheMode,
      CACHE_HIT_MODE: this.isCached ? "true" : "false",
      RANDOM_SEED: String(this.seed),
    };
  }
}
