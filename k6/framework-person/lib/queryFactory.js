import { toInt, env } from "./env.js";

export const PERSON_INCLUDES = (env(
  "INCLUDES",
  "contactphonenumbers.phonenumber,contactemailaddresses.emailaddress,contactaddresses.address"
)
  .split(",")
  .map((x) => x.trim())
  .filter(Boolean));

export const PERSON_SELECT_FIELDS = (env(
  "SELECT_FIELDS",
  "Id,FirstName,LastName,Source,contactaddresses.address.city"
)
  .split(",")
  .map((x) => x.trim())
  .filter(Boolean));

export function buildOptions({
  totalCountMode = env("TOTAL_COUNT_MODE", "None"),
  includeFilterMode = env("INCLUDE_FILTER_MODE", "Full"),
} = {}) {
  return {
    TotalCountMode: totalCountMode,
    IncludeFilterMode: includeFilterMode,
  };
}

export function baselineQuery({ start = 0, amount = 1000 } = {}) {
  return {
    Options: buildOptions(),
    Start: start,
    Amount: amount,
    Include: [],
    Select: [],
    SortBy: [],
  };
}

export function selectProjectionQuery({ start = 0, amount = 1200, include = [] } = {}) {
  return {
    Options: buildOptions(),
    Start: start,
    Amount: amount,
    Include: include,
    Select: PERSON_SELECT_FIELDS,
    SortBy: [
      { PropertyName: "LastName", Direction: "Asc", Priority: 0 },
      { PropertyName: "FirstName", Direction: "Asc", Priority: 1 },
    ],
  };
}

export function filterSortQuery({ start = 0, amount = 1000 } = {}) {
  return {
    Options: buildOptions(),
    Start: start,
    Amount: amount,
    whereClause: {
      $type: "group",
      Operator: "Or",
      Children: [
        {
          $type: "group",
          Operator: "And",
          Children: [
            { $type: "condition", Field: "FirstName", Comparison: "NotEquals", Value: null },
            { $type: "condition", Field: "LastName", Comparison: "NotEquals", Value: null },
          ],
        },
        {
          $type: "condition",
          Field: "Source",
          Comparison: "In",
          Value: "A,B,C,D,E,F",
        },
      ],
    },
    SortBy: [
      { PropertyName: "LastName", Direction: "Asc", Priority: 0 },
      { PropertyName: "FirstName", Direction: "Asc", Priority: 1 },
      { PropertyName: "Id", Direction: "Desc", Priority: 2 },
    ],
  };
}

export function complexWhereClause({ include = [], start = 0, amount = 1200 } = {}) {
  return {
    Options: buildOptions(),
    Start: start,
    Amount: amount,
    Include: include,
    SortBy: [
      { PropertyName: "LastName", Direction: "Asc", Priority: 0 },
      { PropertyName: "FirstName", Direction: "Asc", Priority: 1 },
    ],
    whereClause: {
      $type: "group",
      Operator: "And",
      Children: [
        {
          $type: "condition",
          Field: "FirstName",
          Comparison: "NotEquals",
          Value: null,
        },
        {
          $type: "group",
          Operator: "Or",
          Children: [
            {
              $type: "condition",
              Field: "LastName",
              Comparison: "NotEquals",
              Value: null,
            },
            {
              $type: "condition",
              Field: "Source",
              Comparison: "In",
              Value: "A,B,C,D",
            },
          ],
        },
      ],
    },
  };
}

export function twoPhaseSubQuery({ include = [], start = 0, amount = 1000 } = {}) {
  return {
    Options: buildOptions(),
    Start: start,
    Amount: amount,
    Include: include,
    whereClause: {
      $type: "condition",
      Field: "IsActive",
      Comparison: "Equals",
      Value: true,
      subClause: {
        $type: "group",
        Operator: "And",
        Children: [
          {
            $type: "condition",
            Field: "FirstName",
            Comparison: "NotEquals",
            Value: null,
          },
          {
            $type: "group",
            Operator: "Or",
            Children: [
              {
                $type: "condition",
                Field: "Source",
                Comparison: "NotEquals",
                Value: null,
              },
              {
                $type: "condition",
                Field: "LastName",
                Comparison: "Regex",
                Value: "^[A-Z][a-z]+$",
              },
            ],
          },
        ],
      },
    },
    SortBy: [{ PropertyName: "Id", Direction: "Asc", Priority: 0 }],
  };
}

export function heavyIncludeQuery({ iter = 0, bypassCache = true } = {}) {
  const baseAmount = toInt("HEAVY_AMOUNT", 1998);
  const minAmount = toInt("HEAVY_MIN_AMOUNT", 1900);
  const maxAmount = toInt("HEAVY_MAX_AMOUNT", 2000);
  const amountSpan = Math.max(1, maxAmount - minAmount + 1);
  const amount = bypassCache ? minAmount + ((baseAmount + iter) % amountSpan) : baseAmount;
  const start = bypassCache ? Math.max(0, toInt("START", 0) + ((iter * 5) % 200)) : toInt("START", 0);

  return {
    Options: buildOptions({ totalCountMode: env("HEAVY_TOTAL_COUNT_MODE", "None") }),
    Start: start,
    Amount: amount,
    Include: PERSON_INCLUDES,
    Select: [],
    SortBy: [],
  };
}

/** Realistic include query: 100–300 items, 3 table hops (contactaddresses.address only). Cache-bypassing via randomized start/amount. */
export function realisticIncludeQuery({ iter = 0 } = {}) {
  const minAmount = toInt("REALISTIC_MIN_AMOUNT", 100);
  const maxAmount = toInt("REALISTIC_MAX_AMOUNT", 300);
  const amountSpan = Math.max(1, maxAmount - minAmount + 1);
  const amount = minAmount + ((iter * 17 + 13) % amountSpan);
  const start = Math.max(0, toInt("REALISTIC_START", 0) + ((iter * 13) % 500));

  return {
    Options: buildOptions({ totalCountMode: env("REALISTIC_TOTAL_COUNT_MODE", "None") }),
    Start: start,
    Amount: amount,
    Include: ["contactaddresses.address"],
    Select: [],
    SortBy: [],
  };
}
