import { createEndpointProfileScenario } from "../lib/matrixRunner.js";

const scenario = createEndpointProfileScenario({
  endpointKind: "query",
  profile: "spike",
  testTag: "query-spike",
});

export const options = scenario.options;

export default function () {
  scenario.run();
}
