/** Catalog kind. Wire values match Lyo JSON camelCase enums. */
export type AlgorithmKind = "expression" | "named" | "discreteMap" | "rangeBand" | "linear" | "composite";

export interface AlgorithmDefinition {
  id?: string;
  key: string;
  name?: string | null;
  description?: string | null;
  kind: AlgorithmKind;
  currentRevision?: number;
  createdTimestamp?: string;
  updatedTimestamp?: string | null;
}

export interface AlgorithmRevision {
  definitionId?: string;
  revision: number;
  kind: AlgorithmKind;
  source: string;
  payload?: string | null;
  createdTimestamp?: string;
}

export interface AlgorithmRevisionRequest {
  kind: AlgorithmKind;
  source: string;
  payload?: string | null;
  name?: string | null;
  description?: string | null;
}

export interface AlgorithmRevertRequest {
  revision: number;
}

export interface AlgorithmLabel {
  key?: string;
  label?: string;
  revision: number;
  isMutable: boolean;
}

export interface AlgorithmLabelRequest {
  revision: number;
  isMutable: boolean;
}

export interface AlgorithmStep {
  key: string;
  kind: AlgorithmKind;
  input?: string | null;
  points: number;
  value?: unknown;
  children: AlgorithmStep[];
}

/** Unsaved draft. `context` values stay JSON and are converted with the revision type binding. */
export interface AlgorithmPreviewRequest {
  key?: string | null;
  kind: AlgorithmKind;
  source: string;
  payload?: string | null;
  context: Record<string, unknown>;
}

export interface AlgorithmPreview {
  value?: unknown;
  result: string;
  steps: AlgorithmStep[];
}
