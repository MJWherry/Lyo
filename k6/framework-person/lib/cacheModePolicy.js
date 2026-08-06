import { env, toBool, variedAmount, variedStart } from "./env.js";
import { assertAxisValue, CACHE_MODES, DEFAULT_CACHE_MODE } from "./matrixAxes.js";

/**
 * Cache-mode behavior for paging and shape randomization.
 * Callers should use this policy instead of scattering CACHE_HIT_MODE checks.
 */
export class CacheModePolicy {
  /** @param {"cached"|"uncached"} mode */
  constructor(mode = DEFAULT_CACHE_MODE) {
    this.mode = assertAxisValue(String(mode || "").trim().toLowerCase(), CACHE_MODES, "cacheMode");
    Object.freeze(this);
  }

  /** @returns {boolean} */
  get isCached() {
    return this.mode === "cached";
  }

  /** @returns {boolean} */
  get pinsShapes() {
    return this.isCached;
  }

  /**
   * Prefer CACHE_MODE; fall back to CACHE_HIT_MODE bool (legacy).
   * @returns {CacheModePolicy}
   */
  static fromEnv() {
    const raw = env("CACHE_MODE", "").toLowerCase().trim();
    if (raw) {
      return new CacheModePolicy(raw);
    }
    return new CacheModePolicy(toBool("CACHE_HIT_MODE", false) ? "cached" : "uncached");
  }

  /**
   * @param {boolean} [envAllowsRandomize]
   * @returns {boolean}
   */
  allowsShapeRandomization(envAllowsRandomize = true) {
    return !this.pinsShapes && !!envAllowsRandomize;
  }

  /**
   * @param {boolean} [envAllowsWeighted]
   * @returns {boolean}
   */
  useWeightedCaseSelection(envAllowsWeighted = true) {
    return !this.pinsShapes && !!envAllowsWeighted;
  }

  /**
   * @param {{ amountMin: number, amountMax: number, startMax: number }} config
   * @param {number} iter
   * @param {number} vu
   * @returns {{ start: number, amount: number }}
   */
  resolvePaging(config, iter, vu) {
    if (this.pinsShapes) {
      return { start: 0, amount: config.amountMin };
    }
    return {
      start: variedStart(config.startMax, iter, vu),
      amount: variedAmount(config.amountMin, config.amountMax, iter, vu),
    };
  }

  /** Whether heavy/realistic include helpers should vary Start/Amount. */
  varyPaging(bypassCache = true) {
    return !!bypassCache && !this.pinsShapes;
  }
}

/** Module-level policy resolved once per VU init from env. */
let _defaultPolicy = null;

export function defaultCacheModePolicy() {
  if (!_defaultPolicy) {
    _defaultPolicy = CacheModePolicy.fromEnv();
  }
  return _defaultPolicy;
}

/** Reset cached policy (tests / re-init). */
export function resetCacheModePolicy() {
  _defaultPolicy = null;
}
