import { group, sleep } from "k6";
import { toBool } from "./env.js";
import { createMatrixCaseRunner } from "./client.js";
import { getCaseDefinitions, getEndpointCaseIds, resolveCaseSlowMs } from "./cases.js";
import { loadMatrixConfig, nextStartAmount, resolveCaseIdsForEndpoint } from "./config.js";
import { CacheModePolicy } from "./cacheModePolicy.js";
import { MatrixCell } from "./matrixCell.js";
import { scenarioDuration } from "./metrics.js";
import { ProfileOptionsBuilder } from "./profiles.js";
import { createSeededRng, requestSeed, resolveCaseWeight, weightedPick } from "./workloadShape.js";

/**
 * Composes MatrixCell + cases + profile options + case runner into a k6 scenario.
 */
export class ScenarioFactory {
  /**
   * @param {MatrixCell} cell
   * @param {{ testTag?: string }} [opts]
   */
  static create(cell, opts = {}) {
    if (!(cell instanceof MatrixCell)) {
      throw new Error("ScenarioFactory.create requires a MatrixCell");
    }

    const cachePolicy = new CacheModePolicy(cell.cacheMode);
    const config = loadMatrixConfig({
      endpointKind: cell.endpointKind,
      profile: cell.profile,
      cachePolicy,
    });

    const availableCaseIds = getEndpointCaseIds(cell.endpointKind);
    const caseIds = resolveCaseIdsForEndpoint(
      cell.endpointKind,
      config.requestedCases,
      availableCaseIds
    );
    const caseDefs = getCaseDefinitions(cell.endpointKind, caseIds).map((caseDef) => ({
      ...caseDef,
      slowMs: resolveCaseSlowMs(caseDef),
      caseWeight: resolveCaseWeight({
        endpointKind: cell.endpointKind,
        profile: cell.profile,
        caseId: caseDef.caseId,
        fallback: 1,
      }),
    }));

    if (caseDefs.length === 0) {
      throw new Error(
        `No cases resolved for cell='${cell.cellId}'. Requested: ${config.requestedCases.join(",")}`
      );
    }

    const runner = createMatrixCaseRunner(config);
    const testTag = opts.testTag || cell.cellId;
    const options = new ProfileOptionsBuilder(cell.intensity).matrixOptions(cell.profile, caseDefs, {
      tags: cell.toK6Tags({ testTag }),
    });

    return {
      cell,
      options,
      run() {
        const startedAt = Date.now();
        const useWeightedSelection = cachePolicy.useWeightedCaseSelection(
          toBool("RANDOMIZE_CASE_SELECTION", true)
        );
        const selectedCase = useWeightedSelection
          ? weightedPick(
              caseDefs,
              (c) => c.caseWeight ?? 1,
              createSeededRng(
                requestSeed({
                  namespace: "matrix-case-select",
                  endpointKind: cell.endpointKind,
                  profile: cell.profile,
                  iter: __ITER,
                  vu: __VU,
                })
              )
            ) || caseDefs[0]
          : caseDefs[(__ITER + __VU) % caseDefs.length];
        const startAmount = nextStartAmount(config, __ITER, __VU);

        group(`matrix_${cell.cellId}`, () => {
          runner.runCase(selectedCase, {
            ...startAmount,
            iter: __ITER,
            vu: __VU,
            profile: cell.profile,
            endpointKind: cell.endpointKind,
          });
        });

        scenarioDuration.add(Date.now() - startedAt, {
          endpoint_kind: cell.endpointKind,
          profile: cell.profile,
          intensity: cell.intensity,
          cache_mode: cell.cacheMode,
          cell: cell.cellId,
        });
        sleep(config.sleepSeconds);
      },
    };
  }
}
