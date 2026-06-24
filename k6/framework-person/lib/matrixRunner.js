import { group, sleep } from "k6";
import { createMatrixCaseRunner } from "./client.js";
import { getCaseDefinitions, getEndpointCaseIds, resolveCaseSlowMs } from "./cases.js";
import { loadMatrixConfig, nextStartAmount, resolveCaseIdsForEndpoint } from "./config.js";
import { scenarioDuration } from "./metrics.js";
import { matrixOptions } from "./profiles.js";

export function createEndpointProfileScenario({ endpointKind, profile, testTag }) {
  const config = loadMatrixConfig({ endpointKind, profile });
  const availableCaseIds = getEndpointCaseIds(endpointKind);
  const caseIds = resolveCaseIdsForEndpoint(
    endpointKind,
    config.requestedCases,
    availableCaseIds
  );
  const caseDefs = getCaseDefinitions(endpointKind, caseIds).map((caseDef) => ({
    ...caseDef,
    slowMs: resolveCaseSlowMs(caseDef),
  }));

  if (caseDefs.length === 0) {
    throw new Error(
      `No cases resolved for endpoint='${endpointKind}' profile='${profile}'. Requested: ${config.requestedCases.join(",")}`
    );
  }

  const runner = createMatrixCaseRunner(config);
  const options = matrixOptions(profile, caseDefs, {
    tags: {
      suite: "framework-person",
      endpoint: endpointKind,
      profile,
      test: testTag,
    },
  });

  return {
    options,
    run() {
      const startedAt = Date.now();
      const selector = (__ITER + __VU) % caseDefs.length;
      const selectedCase = caseDefs[selector];
      const startAmount = nextStartAmount(config, __ITER, __VU);

      group(`matrix_${endpointKind}_${profile}`, () => {
        runner.runCase(selectedCase, {
          ...startAmount,
          iter: __ITER,
          vu: __VU,
          profile,
          endpointKind,
        });
      });

      scenarioDuration.add(Date.now() - startedAt, {
        endpoint_kind: endpointKind,
        profile,
      });
      sleep(config.sleepSeconds);
    },
  };
}
