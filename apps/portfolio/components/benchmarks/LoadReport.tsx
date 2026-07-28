"use client";

import { useState } from "react";
import {
  formatDeltaPct,
  formatMs,
  slaBadgeClass,
} from "@/lib/benchmarks/format";
import type { BenchReport, LoadCase, LoadScenario } from "@/lib/benchmarks/types";
import { ReportMeta } from "./ReportMeta";

function caseId(c: LoadCase): string {
  return c.case ?? c.id ?? c.name ?? "—";
}

function ScenarioRow({
  scenario,
  casesById,
}: {
  scenario: LoadScenario;
  casesById: Map<string, LoadCase>;
}) {
  const [open, setOpen] = useState(false);
  const hotspots = scenario.hotspots ?? [];
  const steps = scenario.steps ?? [];
  const expandable = hotspots.length > 0 || steps.length > 0;
  const delta = formatDeltaPct(scenario.deltaP95Pct);

  return (
    <>
      <tr>
        <td>
          {expandable ? (
            <button
              type="button"
              className="btn btn-ghost"
              style={{ padding: "0.25rem 0.55rem", fontSize: "0.78rem" }}
              onClick={() => setOpen((v) => !v)}
            >
              {open ? "▾" : "▸"} {scenario.name}
            </button>
          ) : (
            scenario.name
          )}
        </td>
        <td>{scenario.profile ?? "—"}</td>
        <td>{scenario.endpoint ?? "—"}</td>
        <td>{scenario.latency?.p95 != null ? formatMs(scenario.latency.p95) : "—"}</td>
        <td className={delta.className}>{delta.text}</td>
        <td>{scenario.latency?.p99 != null ? formatMs(scenario.latency.p99) : "—"}</td>
        <td>{scenario.latency?.avg != null ? formatMs(scenario.latency.avg) : "—"}</td>
        <td>{scenario.throughput != null ? scenario.throughput.toFixed(1) : "—"}</td>
        <td>{scenario.requests ?? "—"}</td>
        <td>{scenario.checksPass != null ? `${scenario.checksPass.toFixed(1)}%` : "—"}</td>
        <td>{scenario.droppedIterations ?? "—"}</td>
      </tr>
      {open && expandable ? (
        <tr>
          <td colSpan={11} className="wrap">
            {hotspots.length > 0 ? (
              <div style={{ marginBottom: "0.75rem" }}>
                <strong>Per-case breakdown</strong>
                <div className="table-wrap" style={{ marginTop: "0.4rem" }}>
                  <table className="data">
                    <thead>
                      <tr>
                        <th>Case</th>
                        <th>avg</th>
                        <th>p95</th>
                        <th>p99</th>
                      </tr>
                    </thead>
                    <tbody>
                      {[...hotspots]
                        .sort((a, b) => (b.avg ?? 0) - (a.avg ?? 0))
                        .map((h, i) => {
                          const id = h.case ?? h.caseId ?? h.name ?? "—";
                          const meta = casesById.get(id);
                          return (
                            <tr key={`${id}-${i}`}>
                              <td className="wrap">
                                {id}
                                {meta?.description ? (
                                  <div className="faint" style={{ fontSize: "0.78rem" }}>
                                    {meta.description}
                                  </div>
                                ) : null}
                              </td>
                              <td>{h.avg != null ? formatMs(h.avg) : "—"}</td>
                              <td>{h.p95 != null ? formatMs(h.p95) : "—"}</td>
                              <td>{h.p99 != null ? formatMs(h.p99) : "—"}</td>
                            </tr>
                          );
                        })}
                    </tbody>
                  </table>
                </div>
              </div>
            ) : null}
            {steps.length > 0 ? (
              <div>
                <strong>Rate ladder</strong>
                <div className="table-wrap" style={{ marginTop: "0.4rem" }}>
                  <table className="data">
                    <thead>
                      <tr>
                        <th>Target rate</th>
                        <th>avg</th>
                        <th>p95</th>
                        <th>p99</th>
                        <th>Requests</th>
                        <th>Dropped</th>
                      </tr>
                    </thead>
                    <tbody>
                      {steps.map((s, i) => (
                        <tr key={`${s.targetRate}-${i}`}>
                          <td>{s.targetRate ?? "—"}</td>
                          <td>{s.avg != null ? formatMs(s.avg) : "—"}</td>
                          <td>{s.p95 != null ? formatMs(s.p95) : "—"}</td>
                          <td>{s.p99 != null ? formatMs(s.p99) : "—"}</td>
                          <td>{s.requests ?? "—"}</td>
                          <td>{s.droppedIterations ?? "—"}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>
            ) : null}
          </td>
        </tr>
      ) : null}
    </>
  );
}

export function LoadReport({ report }: { report: BenchReport }) {
  const cases = report.cases ?? [];
  const casesById = new Map(cases.map((c) => [caseId(c), c]));

  return (
    <>
      <ReportMeta report={report} />
      {report.description ? <p className="muted">{report.description}</p> : null}

      <section className="section" style={{ paddingTop: 0 }}>
        <h2 style={{ fontSize: "1.35rem" }}>Scenarios</h2>
        {(report.scenarios ?? []).length === 0 ? (
          <p className="muted">No scenarios in this report.</p>
        ) : (
          <div className="table-wrap">
            <table className="data">
              <thead>
                <tr>
                  <th>Scenario</th>
                  <th>Profile</th>
                  <th>Endpoint</th>
                  <th>p95</th>
                  <th>Δ p95</th>
                  <th>p99</th>
                  <th>avg</th>
                  <th>rps</th>
                  <th>Requests</th>
                  <th>Checks</th>
                  <th>Dropped</th>
                </tr>
              </thead>
              <tbody>
                {(report.scenarios ?? []).map((s) => (
                  <ScenarioRow key={s.name} scenario={s} casesById={casesById} />
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      {cases.length > 0 ? (
        <section className="section" style={{ paddingTop: 0 }}>
          <h2 style={{ fontSize: "1.35rem" }}>Query cases</h2>
          <div className="table-wrap">
            <table className="data">
              <thead>
                <tr>
                  <th>Case</th>
                  <th>Endpoint</th>
                  <th>Where</th>
                  <th>Sort</th>
                  <th>Includes</th>
                  <th>Description</th>
                </tr>
              </thead>
              <tbody>
                {cases.map((c, i) => (
                  <tr key={caseId(c) + i}>
                    <td>{caseId(c)}</td>
                    <td>{c.endpoint ?? "—"}</td>
                    <td>
                      {c.whereClauses ?? c.whereClauseCount ?? c.filterCount ?? "—"}
                    </td>
                    <td>
                      {c.sortCount ??
                        (Array.isArray(c.sortFields) ? c.sortFields.length : "—")}
                    </td>
                    <td>
                      {c.includeCount ??
                        (Array.isArray(c.includes) ? c.includes.length : "—")}
                    </td>
                    <td className="wrap">{c.description ?? "—"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      ) : null}

      {(report.rollups ?? []).length > 0 ? (
        <section className="section" style={{ paddingTop: 0 }}>
          <h2 style={{ fontSize: "1.35rem" }}>Endpoint rollups</h2>
          <div className="table-wrap">
            <table className="data">
              <thead>
                <tr>
                  <th>Endpoint</th>
                  <th>Requests</th>
                  <th>Checks</th>
                  <th>Status</th>
                  <th>Shape</th>
                  <th>Latency</th>
                </tr>
              </thead>
              <tbody>
                {(report.rollups ?? []).map((r, i) => (
                  <tr key={`${r.endpoint}-${i}`}>
                    <td>{r.endpoint ?? "—"}</td>
                    <td>{r.totalRequests ?? "—"}</td>
                    <td>{r.checksPass != null ? `${r.checksPass.toFixed(1)}%` : "—"}</td>
                    <td>{r.statusPass != null ? `${r.statusPass.toFixed(1)}%` : "—"}</td>
                    <td>{r.shapePass != null ? `${r.shapePass.toFixed(1)}%` : "—"}</td>
                    <td>{r.latencyPass != null ? `${r.latencyPass.toFixed(1)}%` : "—"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      ) : null}

      {report.slo?.length ? (
        <section className="section" style={{ paddingTop: 0 }}>
          <h2 style={{ fontSize: "1.35rem" }}>SLO assessment</h2>
          <div className="table-wrap">
            <table className="data">
              <thead>
                <tr>
                  <th>Area</th>
                  <th>Target</th>
                  <th>Latest</th>
                  <th>Result</th>
                </tr>
              </thead>
              <tbody>
                {report.slo.map((s, i) => (
                  <tr key={`${s.area ?? s.name}-${i}`}>
                    <td className="wrap">{s.area ?? s.name ?? "—"}</td>
                    <td className="wrap">{s.target ?? "—"}</td>
                    <td>{s.latest ?? s.actual ?? "—"}</td>
                    <td>
                      <span className={slaBadgeClass(s.result)}>{s.result ?? "—"}</span>
                    </td>
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
