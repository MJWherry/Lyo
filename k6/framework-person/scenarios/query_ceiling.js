import { createEndpointProfileScenario } from "../lib/matrixRunner.js";

const scenario = createEndpointProfileScenario({
  endpointKind: "query",
  profile: "ceiling",
  testTag: "query-ceiling",
});

export const options = scenario.options;

export default function () {
  scenario.run();
}
