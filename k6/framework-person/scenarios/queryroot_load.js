import { createEndpointProfileScenario } from "../lib/matrixRunner.js";

const scenario = createEndpointProfileScenario({
  endpointKind: "queryroot",
  profile: "load",
  testTag: "queryroot-load",
});

export const options = scenario.options;

export default function () {
  scenario.run();
}
