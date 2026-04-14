import { env, toInt } from "./env.js";

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
        rate: toInt("LOAD_RATE", 20),
        timeUnit: env("LOAD_TIME_UNIT", "1s"),
        duration: env("LOAD_DURATION", "3m"),
        preAllocatedVUs: toInt("LOAD_PREALLOCATED_VUS", 10),
        maxVUs: toInt("LOAD_MAX_VUS", 50),
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
