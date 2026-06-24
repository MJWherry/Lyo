import { createEndpointProfileScenario } from "../lib/matrixRunner.js";

const scenario = createEndpointProfileScenario({
  endpointKind: "query",
  profile: "load",
  testTag: "query-load",
});

export const options = scenario.options;

export default function () {
  scenario.run();
}
