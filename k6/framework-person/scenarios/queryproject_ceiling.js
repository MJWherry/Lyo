import { createEndpointProfileScenario } from "../lib/matrixRunner.js";

const scenario = createEndpointProfileScenario({
  endpointKind: "queryproject",
  profile: "ceiling",
  testTag: "queryproject-ceiling",
});

export const options = scenario.options;

export default function () {
  scenario.run();
}
