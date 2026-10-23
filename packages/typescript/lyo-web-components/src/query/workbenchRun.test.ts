import { describe, expect, it } from "vitest";
import { defaultQueryOptions, type ProjectionQueryReq, type QueryConcreteReq, type QueryReq } from "lyo-query";
import {
  WORKBENCH_SAVE_DEBOUNCE_MS,
  authHeaders,
  buildRunUrl,
  buildSqlUrl,
  defaultRunConfig,
  sqlPreviewBody,
  normalizeBaseUrl,
  normalizeRun,
  restoreWorkbenchState,
  type QueryWorkbenchPersistedState,
} from "./workbenchRun.js";

const hosts = { "localhost:5251": ["person", "Job/Definition"], "https://api.example": ["Reporting/Definition"] };

describe("normalizeBaseUrl", () => {
  it("adds http when the scheme is missing and keeps absolute urls", () => {
    expect(normalizeBaseUrl("localhost:5251")).toBe("http://localhost:5251");
    expect(normalizeBaseUrl(" https://api.example/ ")).toBe("https://api.example");
  });
});

describe("normalizeRun", () => {
  it("replaces a host or route that is not in the table", () => {
    const next = normalizeRun({
      ...defaultRunConfig(hosts),
      selectedHost: "https://missing",
      route: "nope",
    });
    expect(next.selectedHost).toBe("localhost:5251");
    expect(next.route).toBe("person");
  });
});

describe("buildRunUrl", () => {
  it("builds concrete, project, and root urls", () => {
    const base = { ...defaultRunConfig(hosts), selectedHost: "localhost:5251", route: "person" };
    expect(buildRunUrl({ ...base, runMode: "concrete" })).toBe("http://localhost:5251/person/QueryConcrete");
    expect(buildRunUrl({ ...base, runMode: "project" })).toBe("http://localhost:5251/person/QueryProject");
    expect(buildRunUrl({ ...base, runMode: "query" })).toBe("http://localhost:5251/Query");
  });
});

describe("buildSqlUrl", () => {
  it("posts concrete and projection to the entity QuerySql route and root to QuerySql", () => {
    const base = { ...defaultRunConfig(hosts), selectedHost: "localhost:5251", route: "person" };
    expect(buildSqlUrl({ ...base, runMode: "concrete" })).toBe("http://localhost:5251/person/QuerySql");
    expect(buildSqlUrl({ ...base, runMode: "project" })).toBe("http://localhost:5251/person/QuerySql");
    expect(buildSqlUrl({ ...base, runMode: "query" })).toBe("http://localhost:5251/QuerySql");
  });
});

describe("sqlPreviewBody", () => {
  it("stamps $type for concrete and projection and leaves a root body unchanged", () => {
    const options = defaultQueryOptions();
    const concrete = { Amount: 10, Options: options } as QueryConcreteReq;
    const project = { Amount: 10, Select: ["Id"], Options: options } as ProjectionQueryReq;
    const root = { Amount: 10, Options: options, From: { Alias: "p", EntityType: "Person" }, Select: ["p.Id"] } as QueryReq;
    expect(sqlPreviewBody("concrete", concrete)).toEqual({ $type: "concrete", ...concrete });
    expect(sqlPreviewBody("project", project)).toEqual({ $type: "project", ...project });
    expect(sqlPreviewBody("query", root)).toEqual(root);
    expect(sqlPreviewBody("get", concrete)).toBeNull();
  });
});

describe("authHeaders", () => {
  it("adds no header, a bearer token, or a named header", () => {
    const base = defaultRunConfig();
    expect(authHeaders({ ...base, authMode: "None", authHeaderValue: "secret" })).toEqual({});
    expect(authHeaders({ ...base, authMode: "Bearer", authHeaderValue: "Bearer secret" })).toEqual({
      Authorization: "Bearer secret",
    });
    expect(authHeaders({ ...base, authMode: "Header", authHeaderName: "X-Api-Key", authHeaderValue: "k" })).toEqual({
      "X-Api-Key": "k",
    });
  });
});

describe("restoreWorkbenchState", () => {
  it("round-trips queries, chip lists, and run config", () => {
    const options = defaultQueryOptions();
    const state: QueryWorkbenchPersistedState = {
      entityQuery: { Amount: 20, Options: options } as QueryConcreteReq,
      queryRequest: { Amount: 20, Select: ["Id"], Options: options } as ProjectionQueryReq,
      rootQuery: { Amount: 20, Options: options, From: { Alias: "p", EntityType: "Person" } } as QueryReq,
      includeAll: ["Address"],
      selectAll: ["Id"],
      keysAll: ["k1"],
      run: {
        ...defaultRunConfig(hosts),
        selectedHost: "https://api.example",
        route: "Reporting/Definition",
        runMode: "project",
        leftPanePercent: 40,
        requestEditorViewMode: "Text",
        authMode: "Header",
        authHeaderName: "X-Api-Key",
        authHeaderValue: "k",
      },
    };
    const restored = restoreWorkbenchState(JSON.parse(JSON.stringify(state)) as QueryWorkbenchPersistedState);
    expect(restored.entityQuery).toEqual(state.entityQuery);
    expect(restored.queryRequest).toEqual(state.queryRequest);
    expect(restored.rootQuery).toEqual(state.rootQuery);
    expect(restored.includeAll).toEqual(["Address"]);
    expect(restored.selectAll).toEqual(["Id"]);
    expect(restored.keysAll).toEqual(["k1"]);
    expect(restored.run).toMatchObject({
      selectedHost: "https://api.example",
      route: "Reporting/Definition",
      runMode: "project",
      leftPanePercent: 40,
      requestEditorViewMode: "Text",
      authMode: "Header",
      authHeaderName: "X-Api-Key",
      authHeaderValue: "k",
    });
    expect(WORKBENCH_SAVE_DEBOUNCE_MS).toBe(450);
  });
});
