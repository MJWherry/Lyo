/**
 * Load test: POST .../QueryProject — computed fields (SmartFormat templates on projected rows).
 * Metrics: query_duration tagged by query_case (computed_collection_parallel | computed_scalar).
 *
 * Run:
 *   k6 run k6/framework-person/scenarios/07_load_queryselect_computed.js
 *   COMPUTED_TEMPLATE='{contactaddresses.address.streettype} {contactaddresses.address.streetname}' k6 run ...
 */
import { group, sleep } from "k6";
import { postQuery } from "../lib/client.js";
import { queryProjectUrl, toFloat, toInt, variedAmount, variedStart } from "../lib/env.js";
import { scenarioDuration } from "../lib/metrics.js";
import { loadOptions } from "../lib/profiles.js";
import {
  computedCollectionParallelQuery,
  computedScalarTemplateQuery,
  projectionSlowMs,
} from "../lib/projectionQueries.js";

export const options = loadOptions({
  tags: { suite: "framework-person", profile: "load", test: "queryproject-computed" },
});

const sleepSeconds = toFloat("COMPUTED_SLEEP_SECONDS", 0.08);
const amountMin = toInt("COMPUTED_AMOUNT_MIN", 150);
const amountMax = toInt("COMPUTED_AMOUNT_MAX", 220);
const startMax = toInt("COMPUTED_START_MAX", 800);

export default function () {
  const startMs = Date.now();
  const selector = (__ITER + __VU) % 2;
  const start = variedStart(startMax, __ITER, __VU);
  const amount = variedAmount(amountMin, amountMax, __ITER, __VU);
  const url = queryProjectUrl();

  group("person_load_queryproject_computed", () => {
    if (selector === 0) {
      postQuery({
        url,
        name: "computed_collection_parallel",
        slowMs: projectionSlowMs("computed"),
        body: computedCollectionParallelQuery({ start, amount }),
      });
    } else {
      postQuery({
        url,
        name: "computed_scalar",
        slowMs: projectionSlowMs("computed"),
        body: computedScalarTemplateQuery({ start, amount }),
      });
    }
  });

  scenarioDuration.add(Date.now() - startMs);
  sleep(sleepSeconds);
}
