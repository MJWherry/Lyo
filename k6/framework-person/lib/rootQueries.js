import { buildOptions } from "../../../packages/typescript/lyo-person-api-client/dist/index.js";

/** Root POST /Query — Person only (no joins). */
export function rootFlatPersonQuery({ start = 0, amount = 100 } = {}) {
  return {
    Options: buildOptions({ TotalCountMode: "None" }),
    From: { Alias: "p", EntityType: "Person" },
    Joins: [],
    Select: ["p.FirstName", "p.LastName"],
    Start: start,
    Amount: amount,
    Keys: [],
    Include: [],
    SortBy: [],
    ComputedFields: [],
  };
}

/** Person ⟕ ContactAddress — exercises left-join fan-out collapse. */
export function rootLeftJoinContactAddressQuery({ start = 0, amount = 100 } = {}) {
  return {
    Options: buildOptions({ TotalCountMode: "None" }),
    From: { Alias: "p", EntityType: "Person" },
    Joins: [
      {
        Alias: "c",
        As: "c",
        EntityType: "ContactAddress",
        Type: "Left",
        On: [{ From: "p.Id", To: "c.PersonId" }],
      },
    ],
    Select: ["p.FirstName", "c.StartDate", "c.CreatedTimestamp"],
    Start: start,
    Amount: amount,
    Keys: [],
    Include: [],
    SortBy: [],
    ComputedFields: [],
  };
}

/** Person ⟕ ContactAddress ⟕ Address — chained joins (matches workbench smoke query). */
export function rootChainedJoinAddressQuery({ start = 0, amount = 100 } = {}) {
  return {
    Options: buildOptions({ TotalCountMode: "None" }),
    From: { Alias: "p", EntityType: "Person" },
    Joins: [
      {
        Alias: "c",
        As: "c",
        EntityType: "ContactAddress",
        Type: "Left",
        On: [{ From: "p.Id", To: "c.PersonId" }],
      },
      {
        Alias: "a",
        As: "a",
        EntityType: "Address",
        Type: "Left",
        On: [{ From: "c.AddressId", To: "a.Id" }],
      },
    ],
    Select: ["p.FirstName", "c.StartDate", "c.CreatedTimestamp", "a.StreetName"],
    Start: start,
    Amount: amount,
    Keys: [],
    Include: [],
    SortBy: [],
    ComputedFields: [],
  };
}

/** Chained left joins with Exact total count (From-side). */
export function rootChainedJoinExactCountQuery({ start = 0, amount = 100 } = {}) {
  const body = rootChainedJoinAddressQuery({ start, amount });
  body.Options = buildOptions({ TotalCountMode: "Exact" });
  return body;
}
