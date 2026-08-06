import { env, toInt } from "./env.js";
import { resolveCaseSlowMs } from "./cases.js";
import { IntensityPresets } from "./intensityPresets.js";

export function commonThresholds(extra = {}) {
  return {
    checks: ["rate>0.99"],
    http_req_failed: ["rate<0.01"],
    http_req_duration: [env("HTTP_P95_THRESHOLD", "p(95)<3000")],
    query_duration: [env("QUERY_P95_THRESHOLD", "p(95)<3000")],
    ...extra,
  };
}

/** ProfileOptionsBuilder: maps IntensityPresets knobs → k6 options. Env overrides win. */
export class ProfileOptionsBuilder {
  /**
   * @param {string} [intensity]
   */
  constructor(intensity = IntensityPresets.resolveIntensity()) {
    this.intensity = intensity;
  }

  loadOptions(extra = {}) {
    const knobs = IntensityPresets.forProfile("load", this.intensity);
    return {
      scenarios: {
        load: {
          executor: "constant-arrival-rate",
          rate: toInt("LOAD_RATE", knobs.rate),
          timeUnit: env("LOAD_TIME_UNIT", "1s"),
          duration: env("LOAD_DURATION", knobs.duration),
          preAllocatedVUs: toInt("LOAD_PREALLOCATED_VUS", knobs.preAllocatedVUs),
          maxVUs: toInt("LOAD_MAX_VUS", knobs.maxVUs),
        },
      },
      thresholds: commonThresholds(),
      summaryTrendStats: ["avg", "min", "med", "max", "p(90)", "p(95)", "p(99)"],
      ...extra,
    };
  }

  stressOptions(extra = {}) {
    const knobs = IntensityPresets.forProfile("stress", this.intensity);
    const target2 = toInt("STRESS_TARGET2", knobs.target2);
    return {
      scenarios: {
        stress: {
          executor: "ramping-vus",
          startVUs: toInt("STRESS_START_VUS", knobs.startVUs),
          stages: [
            { duration: env("STRESS_RAMP1", "2m"), target: toInt("STRESS_TARGET1", knobs.target1) },
            { duration: env("STRESS_RAMP2", "3m"), target: target2 },
            { duration: env("STRESS_HOLD", "2m"), target: target2 },
            { duration: env("STRESS_RAMP_DOWN", "1m"), target: 0 },
          ],
        },
      },
      thresholds: commonThresholds(),
      summaryTrendStats: ["avg", "min", "med", "max", "p(90)", "p(95)", "p(99)"],
      ...extra,
    };
  }

  spikeOptions(extra = {}) {
    const knobs = IntensityPresets.forProfile("spike", this.intensity);
    const targetRate = toInt("SPIKE_TARGET_RATE", knobs.targetRate);
    return {
      scenarios: {
        spike: {
          executor: "ramping-arrival-rate",
          startRate: toInt("SPIKE_START_RATE", knobs.startRate),
          timeUnit: env("SPIKE_TIME_UNIT", "1s"),
          preAllocatedVUs: toInt("SPIKE_PREALLOCATED_VUS", knobs.preAllocatedVUs),
          maxVUs: toInt("SPIKE_MAX_VUS", knobs.maxVUs),
          stages: [
            { duration: env("SPIKE_RAMP", "20s"), target: targetRate },
            { duration: env("SPIKE_HOLD", "40s"), target: targetRate },
            { duration: env("SPIKE_RECOVER", "60s"), target: toInt("SPIKE_RECOVER_RATE", knobs.recoverRate) },
          ],
        },
      },
      thresholds: commonThresholds(),
      summaryTrendStats: ["avg", "min", "med", "max", "p(90)", "p(95)", "p(99)"],
      ...extra,
    };
  }

  soakOptions(extra = {}) {
    const knobs = IntensityPresets.forProfile("soak", this.intensity);
    return {
      scenarios: {
        soak: {
          executor: "constant-vus",
          vus: toInt("SOAK_VUS", knobs.vus),
          duration: env("SOAK_DURATION", knobs.duration),
        },
      },
      thresholds: commonThresholds({
        http_req_duration: [env("SOAK_HTTP_P95_THRESHOLD", "p(95)<3500")],
        query_duration: [env("SOAK_QUERY_P95_THRESHOLD", "p(95)<3500")],
      }),
      summaryTrendStats: ["avg", "min", "med", "max", "p(90)", "p(95)", "p(99)"],
      ...extra,
    };
  }

  /**
   * Saturation profile: staggered constant-arrival-rate steps until the server saturates.
   * Thresholds are tautologies so k6 exports per-step submetrics without failing the run.
   */
  ceilingOptions(extra = {}) {
    const knobs = IntensityPresets.forProfile("ceiling", this.intensity);
    const rates = env("CEILING_RATES", knobs.rates)
      .split(",")
      .map((x) => Math.trunc(Number(x.trim())))
      .filter((x) => Number.isFinite(x) && x > 0);
    const stepDuration = env("CEILING_STEP_DURATION", "45s");
    const stepSeconds = parseDurationSeconds(stepDuration, 45);
    const maxVUs = toInt("CEILING_MAX_VUS", 400);

    const scenarios = {};
    const thresholds = {};
    rates.forEach((rate, index) => {
      const name = `r${rate}`;
      scenarios[name] = {
        executor: "constant-arrival-rate",
        rate,
        timeUnit: "1s",
        duration: stepDuration,
        startTime: `${Math.round(index * stepSeconds)}s`,
        preAllocatedVUs: Math.min(maxVUs, Math.max(20, Math.ceil(rate / 4))),
        maxVUs,
        gracefulStop: env("CEILING_GRACEFUL_STOP", "10s"),
      };
      thresholds[`http_req_duration{scenario:${name}}`] = ["p(95)>=0"];
      thresholds[`http_reqs{scenario:${name}}`] = ["count>=0"];
      thresholds[`dropped_iterations{scenario:${name}}`] = ["count>=0"];
    });

    return {
      scenarios,
      thresholds,
      summaryTrendStats: ["avg", "min", "med", "max", "p(90)", "p(95)", "p(99)"],
      ...extra,
    };
  }

  /**
   * @param {string} profile
   * @param {unknown[]} caseDefs
   * @param {Record<string, unknown>} [extra]
   */
  matrixOptions(profile, caseDefs, extra = {}) {
    if (profile === "ceiling") {
      return this.ceilingOptions(extra);
    }

    const thresholds = commonThresholds(perCaseThresholds(caseDefs));
    const mergedExtra = {
      ...extra,
      thresholds: {
        ...thresholds,
        ...(extra.thresholds ?? {}),
      },
    };

    if (profile === "load") return this.loadOptions(mergedExtra);
    if (profile === "stress") return this.stressOptions(mergedExtra);
    if (profile === "spike") return this.spikeOptions(mergedExtra);
    if (profile === "soak") return this.soakOptions(mergedExtra);

    throw new Error(`Unknown matrix profile '${profile}'`);
  }
}

function parseDurationSeconds(value, fallbackSeconds) {
  const match = /^(\d+(?:\.\d+)?)(ms|s|m|h)?$/.exec(String(value || "").trim());
  if (!match) {
    return fallbackSeconds;
  }
  const amount = Number(match[1]);
  const unit = match[2] || "s";
  const scale = unit === "ms" ? 0.001 : unit === "m" ? 60 : unit === "h" ? 3600 : 1;
  return amount * scale;
}

function perCaseThresholds(caseDefs) {
  const thresholds = {};
  for (const caseDef of caseDefs) {
    const slowMs = resolveCaseSlowMs(caseDef);
    thresholds[`query_duration{query_case:${caseDef.caseId}}`] = [`p(95)<${slowMs}`];
    thresholds[`status_success_rate{query_case:${caseDef.caseId}}`] = ["rate>0.99"];
    thresholds[`latency_success_rate{query_case:${caseDef.caseId}}`] = ["rate>0.99"];
    thresholds[`shape_success_rate{query_case:${caseDef.caseId}}`] = ["rate>0.99"];
  }
  return thresholds;
}

/** @deprecated Prefer ProfileOptionsBuilder; kept for legacy scenario imports. */
export function loadOptions(extra = {}) {
  return new ProfileOptionsBuilder().loadOptions(extra);
}

/** @deprecated Prefer ProfileOptionsBuilder */
export function stressOptions(extra = {}) {
  return new ProfileOptionsBuilder().stressOptions(extra);
}

/** @deprecated Prefer ProfileOptionsBuilder */
export function spikeOptions(extra = {}) {
  return new ProfileOptionsBuilder().spikeOptions(extra);
}

/** @deprecated Prefer ProfileOptionsBuilder */
export function soakOptions(extra = {}) {
  return new ProfileOptionsBuilder().soakOptions(extra);
}

/** @deprecated Prefer ProfileOptionsBuilder */
export function ceilingOptions(extra = {}) {
  return new ProfileOptionsBuilder().ceilingOptions(extra);
}

/** @deprecated Prefer ProfileOptionsBuilder.matrixOptions */
export function matrixOptions(profile, caseDefs, extra = {}) {
  return new ProfileOptionsBuilder().matrixOptions(profile, caseDefs, extra);
}
