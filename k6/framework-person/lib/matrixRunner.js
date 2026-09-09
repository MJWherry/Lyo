import { MatrixCell } from "./matrixCell.js";
import { ScenarioFactory } from "./scenarioFactory.js";

/**
 * @deprecated Prefer ScenarioFactory.create(MatrixCell.fromEnv(...)).
 * Kept so legacy imports and thin stubs keep working during migration.
 */
export function createEndpointProfileScenario({ endpointKind, profile, testTag }) {
  const cell = MatrixCell.fromEnv({ endpointKind, profile });
  return ScenarioFactory.create(cell, { testTag });
}

export { ScenarioFactory } from "./scenarioFactory.js";
export { MatrixCell } from "./matrixCell.js";
