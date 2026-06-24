import { createEndpointProfileScenario } from "../lib/matrixRunner.js";

const scenario = createEndpointProfileScenario({
  endpointKind: "queryproject",
  profile: "spike",
  testTag: "queryproject-spike",
});

export const options = scenario.options;

export default function () {
  scenario.run();
}
