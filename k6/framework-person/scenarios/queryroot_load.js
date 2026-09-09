import { MatrixCell } from "../lib/matrixCell.js";
import { ScenarioFactory } from "../lib/scenarioFactory.js";

const cell = MatrixCell.fromEnv({ endpointKind: "queryroot", profile: "load" });
const scenario = ScenarioFactory.create(cell);

export const options = scenario.options;

export default function () {
  scenario.run();
}
