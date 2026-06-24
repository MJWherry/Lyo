import { createEndpointProfileScenario } from "../lib/matrixRunner.js";

const scenario = createEndpointProfileScenario({
  endpointKind: "query",
  profile: "soak",
  testTag: "query-soak",
});

export const options = scenario.options;

export default function () {
  scenario.run();
}
