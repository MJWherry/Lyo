import http from "k6/http";
import { check } from "k6";
import { endpointUrl, env } from "./env.js";
import {
  failedStatusCount,
  requestBytes,
  requestDuration,
  responseBytes,
  slowResponseCount,
  successRate,
} from "./metrics.js";

const TOKEN = env("TOKEN", "");

export function postQuery({ name, body, url, slowMs = 1500, expectedStatus = 200, tags = {} }) {
  const targetUrl = url ?? endpointUrl();
  const payload = JSON.stringify(body);
  const headers = {
    "Content-Type": "application/json",
    "Accept-Encoding": "br, gzip, deflate",
  };
  if (TOKEN) headers.Authorization = `Bearer ${TOKEN}`;

  requestBytes.add(payload.length);
  const res = http.post(targetUrl, payload, { headers, tags: { query_case: name, ...tags } });

  const contentLength = Number(res.headers["Content-Length"] || 0);
  const measuredBytes = contentLength > 0 ? contentLength : res.body ? res.body.length : 0;
  responseBytes.add(measuredBytes);
  requestDuration.add(res.timings.duration, { query_case: name });

  const ok = check(res, {
    [`${name}: status ${expectedStatus}`]: (r) => r.status === expectedStatus,
    [`${name}: under ${slowMs}ms`]: (r) => r.timings.duration < slowMs,
  });
  successRate.add(ok);

  if (res.status !== expectedStatus) failedStatusCount.add(1);
  if (res.timings.duration >= slowMs) slowResponseCount.add(1);
  return res;
}
