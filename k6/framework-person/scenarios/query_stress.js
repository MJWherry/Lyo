import { createEndpointProfileScenario } from "../lib/matrixRunner.js";

const scenario = createEndpointProfileScenario({
  endpointKind: "query",
  profile: "stress",
  testTag: "query-stress",
});

export const options = scenario.options;

export default function () {
  scenario.run();
}
