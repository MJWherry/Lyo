import { group, sleep } from "k6";
import { toBool } from "./env.js";
import { createMatrixCaseRunner } from "./client.js";
import { getCaseDefinitions, getEndpointCaseIds, resolveCaseSlowMs } from "./cases.js";
import { loadMatrixConfig, nextStartAmount, resolveCaseIdsForEndpoint } from "./config.js";
import { scenarioDuration } from "./metrics.js";
import { matrixOptions } from "./profiles.js";
import { cacheHitMode, createSeededRng, requestSeed, resolveCaseWeight, weightedPick } from "./workloadShape.js";

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
    caseWeight: resolveCaseWeight({
      endpointKind,
      profile,
      caseId: caseDef.caseId,
      fallback: 1,
    }),
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
      // Cache-hit runs rotate cases round-robin so each case settles on one cache key.
      const useWeightedSelection = toBool("RANDOMIZE_CASE_SELECTION", true) && !cacheHitMode();
      const selectedCase = useWeightedSelection
        ? weightedPick(
            caseDefs,
            (c) => c.caseWeight ?? 1,
            createSeededRng(
              requestSeed({
                namespace: "matrix-case-select",
                endpointKind,
                profile,
                iter: __ITER,
                vu: __VU,
              })
            )
          ) || caseDefs[0]
        : caseDefs[(__ITER + __VU) % caseDefs.length];
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
