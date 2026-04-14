import { group, sleep } from "k6";
import { postQuery } from "../lib/client.js";
import { toFloat, toInt, variedAmount, variedStart } from "../lib/env.js";
import { scenarioDuration } from "../lib/metrics.js";
import { loadOptions } from "../lib/profiles.js";
import {
  baselineQuery,
  complexWhereClause,
  filterSortQuery,
  selectProjectionQuery,
  twoPhaseSubQuery,
} from "../lib/queryFactory.js";

export const options = loadOptions({
  tags: { suite: "framework-person", profile: "load", test: "mixed-queries" },
});

const sleepSeconds = toFloat("SLEEP_SECONDS", 0.1);
const amountMin = toInt("LOAD_AMOUNT_MIN", 1100);
const amountMax = toInt("LOAD_AMOUNT_MAX", 1300);
const startMax = toInt("LOAD_START_MAX", 1200);

export default function () {
  const startMs = Date.now();
  const selector = (__ITER + __VU) % 5;
  const start = variedStart(startMax, __ITER, __VU);
  const amount = variedAmount(amountMin, amountMax, __ITER, __VU);

  group("person_load_mixed_queries", () => {
    if (selector === 0) {
      postQuery({
        name: "baseline",
        slowMs: toInt("BASELINE_SLOW_MS", 1200),
        body: baselineQuery({ start, amount }),
      });
    } else if (selector === 1) {
      postQuery({
        name: "filter_sort",
        slowMs: toInt("FILTER_SORT_SLOW_MS", 1800),
        body: filterSortQuery({ start, amount }),
      });
    } else if (selector === 2) {
      postQuery({
        name: "complex_querynode",
        slowMs: toInt("QUERYNODE_SLOW_MS", 2200),
        body: complexWhereClause({ start, amount }),
      });
    } else if (selector === 3) {
      postQuery({
        name: "select_projection",
        slowMs: toInt("SELECT_SLOW_MS", 1800),
        body: selectProjectionQuery({ start, amount }),
      });
    } else {
      postQuery({
        name: "query_with_subquery",
        slowMs: toInt("SUBQUERY_SLOW_MS", 2500),
        body: twoPhaseSubQuery({ start, amount }),
      });
    }
  });

  scenarioDuration.add(Date.now() - startMs);
  sleep(sleepSeconds);
}
