import { group, sleep } from "k6";
import { postQuery } from "../lib/client.js";
import { toFloat, toInt } from "../lib/env.js";
import { scenarioDuration } from "../lib/metrics.js";
import { spikeOptions } from "../lib/profiles.js";
import { selectProjectionQuery } from "../lib/queryFactory.js";

export const options = spikeOptions({
  tags: { suite: "framework-person", profile: "spike", test: "select-fields" },
});

const sleepSeconds = toFloat("SLEEP_SECONDS", 0.02);

export default function () {
  const startMs = Date.now();
  const start = (__ITER * 5) % 500;
  const amount = toInt("AMOUNT", 1000);

  group("person_spike_select_fields", () => {
    postQuery({
      name: "select_fields_spike",
      slowMs: toInt("SELECT_SLOW_MS", 1800),
      body: selectProjectionQuery({ start, amount }),
    });
  });

  scenarioDuration.add(Date.now() - startMs);
  sleep(sleepSeconds);
}
