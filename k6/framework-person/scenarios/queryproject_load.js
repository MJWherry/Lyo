import { createEndpointProfileScenario } from "../lib/matrixRunner.js";

const scenario = createEndpointProfileScenario({
  endpointKind: "queryproject",
  profile: "load",
  testTag: "queryproject-load",
});

export const options = scenario.options;

export default function () {
  scenario.run();
}
