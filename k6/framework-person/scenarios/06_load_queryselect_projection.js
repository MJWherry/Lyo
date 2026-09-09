/**
 * Load test: POST .../QueryProject — projection only (no ComputedFields).
 * Metrics: query_duration tagged by query_case (projection_roots | projection_nested | projection_unified).
 *
 * Run:
 *   k6 run k6/framework-person/scenarios/06_load_queryselect_projection.js
 *   QUERY_PROJECT_PATH=/person/QueryProject BASE_URL=http://localhost:5251 k6 run ...
 */
import { group, sleep } from "k6";
import { postQuery } from "../lib/client.js";
import { queryProjectUrl, toFloat, toInt, variedAmount, variedStart } from "../lib/env.js";
import { scenarioDuration } from "../lib/metrics.js";
import { loadOptions } from "../lib/profiles.js";
import {
  projectionNestedSelectQuery,
  projectionRootScalarsQuery,
  projectionSlowMs,
  projectionUnifiedCollectionQuery,
} from "../lib/projectionQueries.js";

export const options = loadOptions({
  tags: { suite: "framework-person", profile: "load", test: "queryproject-projection" },
});

const sleepSeconds = toFloat("PROJECTION_SLEEP_SECONDS", 0.08);
const amountMin = toInt("PROJECTION_AMOUNT_MIN", 150);
const amountMax = toInt("PROJECTION_AMOUNT_MAX", 220);
const startMax = toInt("PROJECTION_START_MAX", 800);

export default function () {
  const startMs = Date.now();
  const selector = (__ITER + __VU) % 3;
  const start = variedStart(startMax, __ITER, __VU);
  const amount = variedAmount(amountMin, amountMax, __ITER, __VU);
  const url = queryProjectUrl();

  group("person_load_queryproject_projection", () => {
    if (selector === 0) {
      postQuery({
        url,
        name: "projection_roots",
        slowMs: projectionSlowMs("projection"),
        body: projectionRootScalarsQuery({ start, amount }),
      });
    } else if (selector === 1) {
      postQuery({
        url,
        name: "projection_nested",
        slowMs: projectionSlowMs("projection"),
        body: projectionNestedSelectQuery({ start, amount }),
      });
    } else {
      postQuery({
        url,
        name: "projection_unified",
        slowMs: projectionSlowMs("projection"),
        body: projectionUnifiedCollectionQuery({ start, amount }),
      });
    }
  });

  scenarioDuration.add(Date.now() - startMs);
  sleep(sleepSeconds);
}
