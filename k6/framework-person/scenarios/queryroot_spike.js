import { createEndpointProfileScenario } from "../lib/matrixRunner.js";

const scenario = createEndpointProfileScenario({
  endpointKind: "queryroot",
  profile: "spike",
  testTag: "queryroot-spike",
});

export const options = scenario.options;

export default function () {
  scenario.run();
}
