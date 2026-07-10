import { createEndpointProfileScenario } from "../lib/matrixRunner.js";

const scenario = createEndpointProfileScenario({
  endpointKind: "queryroot",
  profile: "stress",
  testTag: "queryroot-stress",
});

export const options = scenario.options;

export default function () {
  scenario.run();
}
