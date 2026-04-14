import { group, sleep } from "k6";
import { postQuery } from "../lib/client.js";
import { toBool, toFloat, toInt, variedAmount, variedStart } from "../lib/env.js";
import { scenarioDuration } from "../lib/metrics.js";
import { soakOptions } from "../lib/profiles.js";
import {
  baselineQuery,
  complexWhereClause,
  filterSortQuery,
  heavyIncludeQuery,
  selectProjectionQuery,
} from "../lib/queryFactory.js";

export const options = soakOptions({
  tags: { suite: "framework-person", profile: "soak", test: "mixed-leak-watch" },
});

const sleepSeconds = toFloat("SLEEP_SECONDS", 0.15);
const amountMin = toInt("SOAK_AMOUNT_MIN", 900);
const amountMax = toInt("SOAK_AMOUNT_MAX", 1100);
const startMax = toInt("SOAK_START_MAX", 1500);
const includeEvery = toInt("SOAK_HEAVY_EVERY", 20);
const bypassCache = toBool("BYPASS_CACHE", true);

export default function () {
  const startMs = Date.now();
  const start = variedStart(startMax, __ITER, __VU);
  const amount = variedAmount(amountMin, amountMax, __ITER, __VU);
  const pick = (__ITER + __VU) % 4;

  group("person_soak_mixed_leak_watch", () => {
    if (__ITER > 0 && __ITER % includeEvery === 0) {
      postQuery({
        name: "soak_heavy_include",
        slowMs: toInt("SOAK_HEAVY_SLOW_MS", 5000),
        body: heavyIncludeQuery({ iter: __ITER + __VU, bypassCache }),
      });
      return;
    }

    if (pick === 0) {
      postQuery({
        name: "soak_baseline",
        slowMs: toInt("SOAK_BASELINE_SLOW_MS", 1500),
        body: baselineQuery({ start, amount }),
      });
    } else if (pick === 1) {
      postQuery({
        name: "soak_filter_sort",
        slowMs: toInt("SOAK_FILTER_SLOW_MS", 2000),
        body: filterSortQuery({ start, amount }),
      });
    } else if (pick === 2) {
      postQuery({
        name: "soak_select_projection",
        slowMs: toInt("SOAK_SELECT_SLOW_MS", 1800),
        body: selectProjectionQuery({ start, amount }),
      });
    } else {
      postQuery({
        name: "soak_complex_node",
        slowMs: toInt("SOAK_QUERYNODE_SLOW_MS", 2400),
        body: complexWhereClause({ start, amount }),
      });
    }
  });

  scenarioDuration.add(Date.now() - startMs);
  sleep(sleepSeconds);
}
