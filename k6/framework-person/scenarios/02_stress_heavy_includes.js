import { group, sleep } from "k6";
import { postQuery } from "../lib/client.js";
import { toFloat, toInt } from "../lib/env.js";
import { scenarioDuration } from "../lib/metrics.js";
import { stressOptions } from "../lib/profiles.js";
import { realisticIncludeQuery } from "../lib/queryFactory.js";

export const options = stressOptions({
  tags: { suite: "framework-person", profile: "stress", test: "heavy-includes" },
});

const sleepSeconds = toFloat("SLEEP_SECONDS", 0.05);

export function setup() {
  postQuery({
    name: "stress_include_warmup",
    slowMs: toInt("WARMUP_SLOW_MS", 3000),
    body: realisticIncludeQuery({ iter: 0 }),
  });
}

export default function () {
  const startMs = Date.now();
  group("person_stress_heavy_includes", () => {
    postQuery({
      name: "realistic_include",
      slowMs: toInt("HEAVY_SLOW_MS", 2500),
      body: realisticIncludeQuery({ iter: __ITER + __VU }),
    });
  });

  scenarioDuration.add(Date.now() - startMs);
  sleep(sleepSeconds);
}
