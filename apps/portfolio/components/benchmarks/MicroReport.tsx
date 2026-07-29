import {
  displayParamLabel,
  formatBytes,
  formatDeltaPct,
  formatNs,
  formatTimestamp,
  paramLabel,
  slaBadgeClass,
} from "@/lib/benchmarks/format";
import type { BenchReport } from "@/lib/benchmarks/types";
import { ReportMeta } from "./ReportMeta";

export function MicroReport({ report }: { report: BenchReport }) {
  return (
    <>
      <ReportMeta report={report} />
      {report.description ? <p className="muted">{report.description}</p> : null}
      {report.deltaBaseline?.runId ? (
        <p className="faint" style={{ fontSize: "0.88rem" }}>
          Δ vs {report.deltaBaseline.kind === "previousRun" ? "prior run" : "selected run"}:{" "}
          {report.deltaBaseline.runId}
          {report.deltaBaseline.runEnded || report.deltaBaseline.runStarted
            ? ` (${formatTimestamp(report.deltaBaseline.runEnded || report.deltaBaseline.runStarted)})`
            : ""}
        </p>
      ) : (report.history?.length ?? 0) > 1 ? (
        <p className="faint" style={{ fontSize: "0.88rem" }}>
          Δ columns hidden — pick a run under Compare against.
        </p>
      ) : null}

      {report.comparison?.groups?.length ? (
        <section className="section" style={{ paddingTop: 0 }}>
          <h2 style={{ fontSize: "1.35rem" }}>Comparison</h2>
          {report.comparison.description ? (
            <p className="muted">{report.comparison.description}</p>
          ) : null}
          {report.comparison.groups.map((g, i) => (
            <div key={g.axis ?? i} className="panel">
              {g.axis ? <h3 style={{ fontSize: "1.05rem" }}>{g.axis}</h3> : null}
              <div className="table-wrap">
                <table className="data">
                  <thead>
                    <tr>
                      <th>Algorithm</th>
                      <th>Params</th>
                      <th>Mean</th>
                      <th>Alloc</th>
                      <th>Δ mean</th>
                      <th>Δ alloc</th>
                    </tr>
                  </thead>
                  <tbody>
                    {g.rows.map((row, ri) => {
                      const deltaMean = formatDeltaPct(row.deltaMeanPct);
                      const deltaAlloc = formatDeltaPct(row.deltaAllocPct);
                      return (
                        <tr key={`${row.algorithm}-${ri}`}>
                          <td>{row.algorithm}</td>
                          <td>{displayParamLabel(row.parameters, row.paramLabel)}</td>
                          <td>{formatNs(row.meanNs)}</td>
                          <td>{formatBytes(row.allocatedBytes)}</td>
                          <td className={deltaMean.className}>{deltaMean.text}</td>
                          <td className={deltaAlloc.className}>{deltaAlloc.text}</td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            </div>
          ))}
        </section>
      ) : null}

      <section className="section" style={{ paddingTop: 0 }}>
        <h2 style={{ fontSize: "1.35rem" }}>Benchmark classes</h2>
        {(report.groups ?? []).map((group) => (
          <div key={group.name} className="panel">
            <h3 style={{ fontSize: "1.15rem" }}>{group.name}</h3>
            {group.description ? <p className="muted">{group.description}</p> : null}
            {group.parameters?.length ? (
              <ul className="muted" style={{ fontSize: "0.88rem" }}>
                {group.parameters.map((p) => (
                  <li key={p.name}>
                    <strong>{p.name}</strong>
                    {p.unit ? ` (${p.unit})` : ""}
                    {p.description ? ` — ${p.description}` : ""}
                  </li>
                ))}
              </ul>
            ) : null}
            <div className="table-wrap">
              <table className="data">
                <thead>
                  <tr>
                    <th>Method</th>
                    <th>Params</th>
                    <th>Mean</th>
                    <th>Alloc</th>
                    <th>Δ mean</th>
                    <th>Δ alloc</th>
                    <th>SLA</th>
                  </tr>
                </thead>
                <tbody>
                  {(group.measurements ?? []).length === 0 ? (
                    <tr>
                      <td colSpan={7} className="muted">
                        No measurements in this group.
                      </td>
                    </tr>
                  ) : (
                    (group.measurements ?? []).map((m, i) => {
                      const deltaMean = formatDeltaPct(m.deltaMeanPct);
                      const deltaAlloc = formatDeltaPct(m.deltaAllocPct);
                      return (
                        <tr key={`${m.method}-${i}`}>
                          <td className="wrap">
                            {m.method}
                            {m.isBaseline ? (
                              <span className="badge badge-accent" style={{ marginLeft: "0.4rem" }}>
                                baseline
                              </span>
                            ) : null}
                            {m.description ? (
                              <div className="faint" style={{ fontSize: "0.78rem", whiteSpace: "normal" }}>
                                {m.description}
                              </div>
                            ) : null}
                          </td>
                          <td>{paramLabel(m.parameters)}</td>
                          <td>{formatNs(m.meanNs)}</td>
                          <td>{formatBytes(m.allocatedBytes)}</td>
                          <td className={deltaMean.className}>{deltaMean.text}</td>
                          <td className={deltaAlloc.className}>{deltaAlloc.text}</td>
                          <td>
                            <span className={slaBadgeClass(m.slaResult)} title={m.slaTarget}>
                              {m.slaResult ?? "—"}
                            </span>
                          </td>
                        </tr>
                      );
                    })
                  )}
                </tbody>
              </table>
            </div>
          </div>
        ))}
      </section>

      {report.slo?.length ? (
        <section className="section" style={{ paddingTop: 0 }}>
          <h2 style={{ fontSize: "1.35rem" }}>SLA assessment</h2>
          <div className="table-wrap">
            <table className="data">
              <thead>
                <tr>
                  <th>Area</th>
                  <th>Result</th>
                  <th>Target</th>
                  <th>Latest</th>
                  <th>Standard</th>
                </tr>
              </thead>
              <tbody>
                {report.slo.map((s, i) => (
                  <tr key={`${s.area ?? s.name}-${i}`}>
                    <td className="wrap">{s.area ?? s.name ?? "—"}</td>
                    <td>
                      <span className={slaBadgeClass(s.result)}>{s.result ?? "—"}</span>
                    </td>
                    <td className="wrap">{s.target ?? "—"}</td>
                    <td>{s.latest ?? s.actual ?? "—"}</td>
                    <td className="wrap">{s.standard ?? "—"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      ) : null}

      {report.notes?.length ? (
        <section className="panel">
          <h2 style={{ fontSize: "1.15rem" }}>Notes</h2>
          {report.notes.map((n) => (
            <p key={n} className="muted" style={{ marginBottom: "0.5rem" }}>
              {n}
            </p>
          ))}
        </section>
      ) : null}
    </>
  );
}
