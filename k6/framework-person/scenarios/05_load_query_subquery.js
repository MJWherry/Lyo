import { group, sleep } from "k6";
import { postQuery } from "../lib/client.js";
import { toFloat, toInt, variedAmount, variedStart } from "../lib/env.js";
import { scenarioDuration } from "../lib/metrics.js";
import { loadOptions } from "../lib/profiles.js";
import { twoPhaseSubQuery } from "../lib/queryFactory.js";

export const options = loadOptions({
  tags: { suite: "framework-person", profile: "load", test: "query-subquery" },
  thresholds: {
    checks: ["rate>0.99"],
    http_req_failed: ["rate<0.01"],
    http_req_duration: [__ENV.SUBQUERY_HTTP_P95_THRESHOLD || "p(95)<3500"],
    query_duration: [__ENV.SUBQUERY_QUERY_P95_THRESHOLD || "p(95)<3500"],
  },
});

const sleepSeconds = toFloat("SLEEP_SECONDS", 0.1);
const amountMin = toInt("SUBQUERY_AMOUNT_MIN", 1000);
const amountMax = toInt("SUBQUERY_AMOUNT_MAX", 1200);
const startMax = toInt("SUBQUERY_START_MAX", 1200);

export default function () {
  const startMs = Date.now();
  const start = variedStart(startMax, __ITER, __VU);
  const amount = variedAmount(amountMin, amountMax, __ITER, __VU);

  group("person_load_query_subquery", () => {
    postQuery({
      name: "query_subquery",
      slowMs: toInt("SUBQUERY_SLOW_MS", 3000),
      body: twoPhaseSubQuery({ start, amount }),
    });
  });

  scenarioDuration.add(Date.now() - startMs);
  sleep(sleepSeconds);
}
