import { createEndpointProfileScenario } from "../lib/matrixRunner.js";

const scenario = createEndpointProfileScenario({
  endpointKind: "queryproject",
  profile: "soak",
  testTag: "queryproject-soak",
});

export const options = scenario.options;

export default function () {
  scenario.run();
}
