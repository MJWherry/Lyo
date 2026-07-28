import { env, toInt } from "./env.js";
import { resolveCaseSlowMs } from "./cases.js";

export function commonThresholds(extra = {}) {
  return {
    checks: ["rate>0.99"],
    http_req_failed: ["rate<0.01"],
    http_req_duration: [env("HTTP_P95_THRESHOLD", "p(95)<3000")],
    query_duration: [env("QUERY_P95_THRESHOLD", "p(95)<3000")],
    ...extra,
  };
}

export function loadOptions(extra = {}) {
  return {
    scenarios: {
      load: {
        executor: "constant-arrival-rate",
        rate: toInt("LOAD_RATE", 7),
        timeUnit: env("LOAD_TIME_UNIT", "1s"),
        duration: env("LOAD_DURATION", "3m"),
        preAllocatedVUs: toInt("LOAD_PREALLOCATED_VUS", 6),
        maxVUs: toInt("LOAD_MAX_VUS", 12),
      },
    },
    thresholds: commonThresholds(),
    summaryTrendStats: ["avg", "min", "med", "max", "p(90)", "p(95)", "p(99)"],
    ...extra,
  };
}

export function stressOptions(extra = {}) {
  return {
    scenarios: {
      stress: {
        executor: "ramping-vus",
        startVUs: toInt("STRESS_START_VUS", 5),
        stages: [
          { duration: env("STRESS_RAMP1", "2m"), target: toInt("STRESS_TARGET1", 20) },
          { duration: env("STRESS_RAMP2", "3m"), target: toInt("STRESS_TARGET2", 40) },
          { duration: env("STRESS_HOLD", "2m"), target: toInt("STRESS_TARGET2", 40) },
          { duration: env("STRESS_RAMP_DOWN", "1m"), target: 0 },
        ],
      },
    },
    thresholds: commonThresholds(),
    summaryTrendStats: ["avg", "min", "med", "max", "p(90)", "p(95)", "p(99)"],
    ...extra,
  };
}

export function spikeOptions(extra = {}) {
  return {
    scenarios: {
      spike: {
        executor: "ramping-arrival-rate",
        startRate: toInt("SPIKE_START_RATE", 5),
        timeUnit: env("SPIKE_TIME_UNIT", "1s"),
        preAllocatedVUs: toInt("SPIKE_PREALLOCATED_VUS", 10),
        maxVUs: toInt("SPIKE_MAX_VUS", 80),
        stages: [
          { duration: env("SPIKE_RAMP", "20s"), target: toInt("SPIKE_TARGET_RATE", 80) },
          { duration: env("SPIKE_HOLD", "40s"), target: toInt("SPIKE_TARGET_RATE", 80) },
          { duration: env("SPIKE_RECOVER", "60s"), target: toInt("SPIKE_RECOVER_RATE", 10) },
        ],
      },
    },
    thresholds: commonThresholds(),
    summaryTrendStats: ["avg", "min", "med", "max", "p(90)", "p(95)", "p(99)"],
    ...extra,
  };
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

/**
 * Saturation ("ceiling") profile: staggered constant-arrival-rate steps that climb until the
 * server saturates. Each step is its own scenario so the summary export carries per-step
 * latency and dropped-iteration submetrics — the knee is read straight from the summary JSON.
 * No pass/fail thresholds: saturation intentionally blows past every SLO, and a threshold
 * failure exit code would abort run_all.sh. The listed thresholds are tautologies whose only
 * purpose is to force k6 to export the per-step submetrics.
 */
export function ceilingOptions(extra = {}) {
  const rates = env("CEILING_RATES", "25,50,100,150,200,300,450,700,1000")
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

export function soakOptions(extra = {}) {
  return {
    scenarios: {
      soak: {
        executor: "constant-vus",
        vus: toInt("SOAK_VUS", 10),
        duration: env("SOAK_DURATION", "2h"),
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

export function matrixOptions(profile, caseDefs, extra = {}) {
  if (profile === "ceiling") {
    // No per-case SLO thresholds: a saturation run is expected to blow past them.
    return ceilingOptions(extra);
  }

  const thresholds = commonThresholds(perCaseThresholds(caseDefs));
  const mergedExtra = {
    ...extra,
    thresholds: {
      ...thresholds,
      ...(extra.thresholds ?? {}),
    },
  };

  if (profile === "load") {
    return loadOptions(mergedExtra);
  }
  if (profile === "stress") {
    return stressOptions(mergedExtra);
  }
  if (profile === "spike") {
    return spikeOptions(mergedExtra);
  }
  if (profile === "soak") {
    return soakOptions(mergedExtra);
  }

  throw new Error(`Unknown matrix profile '${profile}'`);
}
