import { createEndpointProfileScenario } from "../lib/matrixRunner.js";

const scenario = createEndpointProfileScenario({
  endpointKind: "queryroot",
  profile: "ceiling",
  testTag: "queryroot-ceiling",
});

export const options = scenario.options;

export default function () {
  scenario.run();
}
