import { MatrixCell } from "../lib/matrixCell.js";
import { ScenarioFactory } from "../lib/scenarioFactory.js";

const cell = MatrixCell.fromEnv({ endpointKind: "query", profile: "soak" });
const scenario = ScenarioFactory.create(cell);

export const options = scenario.options;

export default function () {
  scenario.run();
}
