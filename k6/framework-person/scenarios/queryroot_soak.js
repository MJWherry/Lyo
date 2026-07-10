import { createEndpointProfileScenario } from "../lib/matrixRunner.js";

const scenario = createEndpointProfileScenario({
  endpointKind: "queryroot",
  profile: "soak",
  testTag: "queryroot-soak",
});

export const options = scenario.options;

export default function () {
  scenario.run();
}
