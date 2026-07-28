import type { Metadata } from "next";
import { PageHero } from "@/components/PageHero";
import { FeatureNav } from "@/components/FeatureNav";
import { CodeBlock } from "@/components/CodeBlock";
import { snippets } from "@/content/snippets";

export const metadata: Metadata = {
  title: "Jobs",
};

export default function JobsFeaturePage() {
  return (
    <>
      <PageHero
        kicker="Lyo.Job"
        title="Definitions through live dashboards"
        description="A full job platform: PostgreSQL persistence, schedule evaluation, RabbitMQ-triggered workers, retries/SLA, alerts, SignalR fan-out, and MudBlazor management UI — not just a background timer."
      />
      <FeatureNav current="/features/jobs" />

      <section className="section shell">
        <div className="panel" style={{ marginBottom: "1rem" }}>
          <h2 style={{ fontSize: "1.25rem" }}>End-to-end pipeline</h2>
          <ol className="muted" style={{ paddingLeft: "1.2rem", margin: "0.5rem 0 0" }}>
            <li>
              <strong style={{ color: "var(--ink)" }}>Definition</strong> —{" "}
              <code>JobDefinitionBuilder</code>: worker type, parameters, retries, SLA, alerts,
              blackout calendar
            </li>
            <li>
              <strong style={{ color: "var(--ink)" }}>Schedule</strong> — cron / interval / one-shot
              via <code>JobScheduleBuilder</code>, misfire policies, workflows
            </li>
            <li>
              <strong style={{ color: "var(--ink)" }}>Scheduler</strong> — polls definitions, creates{" "}
              <code>JobRun</code> rows (<code>Queued</code>), publishes MQ triggers
            </li>
            <li>
              <strong style={{ color: "var(--ink)" }}>Worker</strong> —{" "}
              <code>JobWorkerBase</code> claims run → heartbeat/progress →{" "}
              <code>ExecuteAsync</code> → <code>Finished</code>
            </li>
            <li>
              <strong style={{ color: "var(--ink)" }}>Events</strong> —{" "}
              <code>IJobEventPublisher</code> on <code>job.events</code> (
              <code>run.*</code>, alerts, definition updates)
            </li>
            <li>
              <strong style={{ color: "var(--ink)" }}>Alerts & UI</strong> — webhook/notification
              consumers + SignalR <code>JobHub</code> + <code>JobManagement</code> grids
            </li>
          </ol>
          <p className="muted" style={{ marginTop: "1rem", marginBottom: 0 }}>
            Packages: <code>Lyo.Job.Models</code>, <code>.Postgres</code>, <code>.Client</code>,{" "}
            <code>.Scheduler</code>, <code>.Worker</code>, <code>.SignalR</code>,{" "}
            <code>.Alerts</code>, <code>.Web.Components</code>.
          </p>
        </div>

        <div className="grid-2">
          <div>
            <h2 style={{ fontSize: "1.15rem" }}>API host registration</h2>
            <CodeBlock code={snippets.jobRegisterApi} />
          </div>
          <div>
            <h2 style={{ fontSize: "1.15rem" }}>Definition + schedule</h2>
            <CodeBlock code={snippets.jobDefinition} />
          </div>
          <div>
            <h2 style={{ fontSize: "1.15rem" }}>Scheduler host</h2>
            <CodeBlock code={snippets.jobScheduler} />
          </div>
          <div>
            <h2 style={{ fontSize: "1.15rem" }}>Worker</h2>
            <CodeBlock code={snippets.jobWorker} />
          </div>
          <div>
            <h2 style={{ fontSize: "1.15rem" }}>SignalR live events</h2>
            <CodeBlock code={snippets.jobSignalR} />
          </div>
          <div>
            <h2 style={{ fontSize: "1.15rem" }}>Alerts</h2>
            <CodeBlock code={snippets.jobAlerts} />
          </div>
        </div>
      </section>
    </>
  );
}
