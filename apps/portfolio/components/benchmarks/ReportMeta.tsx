import {
  formatBytes,
  formatDuration,
  formatTimestamp,
} from "@/lib/benchmarks/format";
import type { BenchReport } from "@/lib/benchmarks/types";

export function ReportMeta({ report }: { report: BenchReport }) {
  const env = report.environment ?? {};
  const chips = [
    report.runId ? `Run ${report.runId}` : null,
    env.tool ? `${env.tool}${env.toolVersion ? ` ${env.toolVersion}` : ""}` : null,
    env.runtime ?? null,
    env.dotnetSdkVersion ? `SDK ${env.dotnetSdkVersion}` : null,
    env.configuration ?? null,
    env.cpu ?? null,
    env.logicalCores
      ? `${env.logicalCores} vCPU${
          env.physicalCores && env.physicalCores !== env.logicalCores
            ? ` (${env.physicalCores} phys)`
            : ""
        }`
      : null,
    env.architecture ?? null,
    env.memoryBytes ? `${formatBytes(env.memoryBytes)} RAM` : null,
    env.gcMode ? `${env.gcMode} GC` : null,
    env.os ?? null,
    formatTimestamp(report.runStarted) ? `Started ${formatTimestamp(report.runStarted)}` : null,
    formatTimestamp(report.runEnded) ? `Ended ${formatTimestamp(report.runEnded)}` : null,
    formatDuration(report.durationSeconds)
      ? `Duration ${formatDuration(report.durationSeconds)}`
      : null,
  ].filter(Boolean) as string[];

  return (
    <div className="chip-row">
      {chips.map((c) => (
        <span key={c} className="meta-chip">
          {c}
        </span>
      ))}
    </div>
  );
}
