export type RegistryEntry = {
  name: string;
  title: string;
  type: "micro" | "load";
  description: string;
};

export type HistoryEntry = {
  file: string;
  runId?: string;
  runStarted?: string;
  runEnded?: string;
  generatedAt?: string;
  isCurrent?: boolean;
  measurementCount?: number;
  scenarioCount?: number;
  medianMeanNs?: number;
  medianP95Ms?: number;
};

export type BenchEnvironment = {
  tool?: string;
  toolVersion?: string;
  runtime?: string;
  dotnetSdkVersion?: string;
  configuration?: string;
  cpu?: string;
  logicalCores?: number;
  physicalCores?: number;
  architecture?: string;
  memoryBytes?: number;
  gcMode?: string;
  os?: string;
};

export type BenchMeasurement = {
  method: string;
  description?: string;
  parameters?: Record<string, string>;
  meanNs?: number;
  stdDevNs?: number;
  allocatedBytes?: number;
  ratioToBaseline?: number;
  isBaseline?: boolean;
  slaTarget?: string;
  slaResult?: string;
  slaStandard?: string;
  deltaMeanPct?: number;
  deltaAllocPct?: number;
};

export type BenchGroup = {
  name: string;
  description?: string;
  parameters?: Array<{ name: string; unit?: string; description?: string }>;
  dataset?: {
    typeName?: string;
    columnCount?: number;
    maxNestingDepth?: number;
    notes?: string;
    columns?: Array<{ name: string; type: string; kind: string; children?: unknown[] }>;
  };
  measurements: BenchMeasurement[];
};

export type ComparisonGroup = {
  axis?: string;
  rows: Array<{
    algorithm: string;
    paramLabel?: string;
    meanNs?: number;
    allocatedBytes?: number;
    deltaMeanPct?: number;
  }>;
};

export type LoadHotspot = {
  case?: string;
  caseId?: string;
  name?: string;
  p95?: number;
  p99?: number;
  avg?: number;
  requests?: number;
  checksPass?: number;
};

export type LoadScenario = {
  name: string;
  profile?: string;
  endpoint?: string;
  latency?: {
    min?: number;
    p50?: number;
    p90?: number;
    p95?: number;
    p99?: number;
    avg?: number;
    max?: number;
    unit?: string;
  };
  throughput?: number;
  requests?: number;
  checksPass?: number;
  statusPass?: number;
  shapePass?: number;
  latencyPass?: number;
  droppedIterations?: number;
  deltaP95Pct?: number;
  hotspots?: LoadHotspot[];
  steps?: Array<{
    targetRate?: number;
    avg?: number;
    p95?: number;
    p99?: number;
    requests?: number;
    droppedIterations?: number;
  }>;
};

/** k6 case catalog entry (lyo.bench/v1 load reports). */
export type LoadCase = {
  case?: string;
  id?: string;
  name?: string;
  endpoint?: string;
  description?: string;
  whereClauses?: number;
  whereClauseCount?: number;
  filterCount?: number;
  sortFields?: string[];
  sortCount?: number;
  includes?: string[];
  includeCount?: number;
  selectCount?: number;
};

export type SloItem = {
  /** Load reports use `area`; micro often uses `name`. */
  area?: string;
  name?: string;
  target?: string;
  /** Load reports use `latest`; micro often uses `actual`. */
  latest?: string;
  actual?: string;
  result?: string;
  standard?: string;
};

export type GradeItem = {
  category?: string;
  name?: string;
  grade?: string;
  note?: string;
};

export type EndpointRollup = {
  endpoint?: string;
  totalRequests?: number;
  checksPass?: number;
  statusPass?: number;
  shapePass?: number;
  latencyPass?: number;
};

export type BenchReport = {
  schema?: string;
  type: "micro" | "load";
  name: string;
  title?: string;
  description?: string;
  runId?: string;
  generatedAt?: string;
  runStarted?: string;
  runEnded?: string;
  durationSeconds?: number;
  environment?: BenchEnvironment;
  notes?: string[];
  history?: HistoryEntry[];
  deltaBaseline?: {
    kind?: string;
    runId?: string;
    runStarted?: string;
    runEnded?: string;
    runCount?: number;
  };
  groups?: BenchGroup[];
  comparison?: {
    baseline?: string;
    description?: string;
    groups?: ComparisonGroup[];
  };
  scenarios?: LoadScenario[];
  cases?: LoadCase[];
  rollups?: EndpointRollup[];
  slo?: SloItem[];
  grades?: GradeItem[];
};
