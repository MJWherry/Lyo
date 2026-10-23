import type {AlgorithmContextRow, AlgorithmDraft} from "./draft.js";
import {emptyDraft} from "./draft.js";

export const FISHING_FORMULA =
  "fish_type + (weight >= 5 && weight < 10 ? 3 : (weight >= 0 && weight < 5 ? 1 : 0)) + (length < 10 ? 0 : (length > 20 ? 50 : (length - 10) / 10 * 50))";

export function fishingDraft(): AlgorithmDraft {
  return {...emptyDraft(), key: "leaderboard.seed_fishing_0", kind: "expression", source: FISHING_FORMULA};
}

export function fishingContext(): AlgorithmContextRow[] {
  return [
    {name: "fish_type", alias: "double", text: "10"},
    {name: "weight", alias: "double", text: "6"},
    {name: "length", alias: "double", text: "15"},
  ];
}

export function attackDraft(): AlgorithmDraft {
  return {...emptyDraft(), key: "attack.physical", kind: "expression", source: "Atk * 2 - Def"};
}

export function attackContext(): AlgorithmContextRow[] {
  return [
    {name: "Atk", alias: "double", text: "3"},
    {name: "Def", alias: "double", text: "1"},
  ];
}

export function scheduleDraft(): AlgorithmDraft {
  return {
    ...emptyDraft(),
    key: "schedule.mix",
    kind: "expression",
    result: "decimal",
    source: "Rate * Convert.ToDecimal(Periods) + (Maturity > Issued && Dates[0] == Issued ? 1m : 0m)",
    parameters: [
      {name: "Rate", alias: "decimal"},
      {name: "Periods", alias: "int"},
      {name: "Issued", alias: "datetime"},
      {name: "Maturity", alias: "datetime"},
      {name: "Dates", alias: "datetime[]"},
    ],
  };
}

export function scheduleContext(): AlgorithmContextRow[] {
  return [
    {name: "Rate", alias: "decimal", text: "2.5"},
    {name: "Periods", alias: "int", text: "4"},
    {name: "Issued", alias: "datetime", text: "2020-01-01T00:00:00"},
    {name: "Maturity", alias: "datetime", text: "2020-06-01T00:00:00"},
    {name: "Dates", alias: "datetime[]", text: '["2020-01-01T00:00:00"]'},
  ];
}
