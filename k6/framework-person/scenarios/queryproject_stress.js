import { createEndpointProfileScenario } from "../lib/matrixRunner.js";

const scenario = createEndpointProfileScenario({
  endpointKind: "queryproject",
  profile: "stress",
  testTag: "queryproject-stress",
});

export const options = scenario.options;

export default function () {
  scenario.run();
}
