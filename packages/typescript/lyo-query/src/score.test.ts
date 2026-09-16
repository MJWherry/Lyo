import { describe, expect, it } from "vitest";
import { defaultQueryOptions, type ProjectionQueryReq, type QueryConcreteReq, type QueryReq } from "./models/queryRequest.js";
import { scoreQueryRequest } from "./score.js";

const exact = defaultQueryOptions({ TotalCountMode: "Exact" });
const none = defaultQueryOptions({ TotalCountMode: "None" });

describe("scoreQueryRequest", () => {
  it("returns zero for a missing request", () => {
    expect(scoreQueryRequest(null).totalScore).toBe(0);
  });

  it("scores paging, keys, sort, and count mode like QueryRequestScorer", () => {
    const req: QueryConcreteReq = {
      Options: exact,
      Start: 200,
      Amount: 300,
      Keys: [[1], [2], [3]],
      SortBy: [
        { PropertyName: "LastName", Direction: "Asc", Priority: 0 },
        { PropertyName: "FirstName", Direction: "Asc", Priority: 1 },
      ],
    };
    const score = scoreQueryRequest(req);
    expect(score).toMatchObject({ pagingScore: 7, keysScore: 9, sortScore: 7, totalCountModeScore: 12, whereClauseScore: 0, totalScore: 35 });
  });

  it("computed fields cost a base per template plus distinct placeholders", () => {
    const without: ProjectionQueryReq = { Options: none, Select: ["FirstName"] };
    const withFields: ProjectionQueryReq = {
      ...without,
      ComputedFields: [
        { Name: "FullName", Template: "{LastName}, {FirstName}" },
        { Name: "Greeting", Template: "Hello {FirstName}" },
      ],
    };
    expect(scoreQueryRequest(withFields).computedFieldsScore).toBe(14);
    expect(scoreQueryRequest(withFields).totalScore).toBe(scoreQueryRequest(without).totalScore + 14);
  });

  it("scores regex and In comparisons in the where clause", () => {
    const req: QueryConcreteReq = {
      Options: none,
      whereClause: {
        $type: "group",
        Operator: "And",
        Children: [
          { $type: "condition", Field: "FirstName", Comparison: "Regex", Value: "^(?i)m[a-z]+$" },
          { $type: "condition", Field: "Source", Comparison: "In", Value: "A,B,C,D,E,F,G,H,I,J" },
        ],
      },
    };
    expect(scoreQueryRequest(req).whereClauseScore).toBe(31);
  });

  it("sub clauses on groups and conditions raise the where clause score", () => {
    const leaf = { $type: "condition" as const, Field: "A", Comparison: "Equals" as const, Value: 1 };
    const flat: QueryConcreteReq = { Options: none, whereClause: { $type: "group", Operator: "And", Children: [leaf] } };
    const nested: QueryConcreteReq = {
      Options: none,
      whereClause: { $type: "group", Operator: "And", Children: [{ ...leaf, subClause: leaf }], subClause: { ...leaf } },
    };
    expect(scoreQueryRequest(flat).whereClauseScore).toBe(13);
    expect(scoreQueryRequest(nested).whereClauseScore).toBe(40);
  });

  it("root queries score Select, join paths, and a per-join bonus", () => {
    const req: QueryReq = {
      Options: none,
      Amount: 10,
      From: { Alias: "p", EntityType: "Person" },
      Joins: [{ Alias: "ca", EntityType: "ContactAddressEntity", Type: "Left", On: [{ From: "p.Id", To: "ca.PersonId" }] }],
      Select: ["p.Id"],
    };
    expect(scoreQueryRequest(req)).toMatchObject({ pagingScore: 2, includeScore: 51, totalScore: 53 });
  });
});
