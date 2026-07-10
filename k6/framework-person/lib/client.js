import http from "k6/http";
import { check } from "k6";
import { endpointUrl, env } from "./env.js";
import { createApiClient } from "../../../packages/lyo-api-client/dist/index.js";
import {
  createPersonApiClient,
  isProjectedQueryRes,
  isQueryRes,
} from "../../../packages/lyo-person-api-client/dist/index.js";
import { createK6Transport } from "./k6Transport.js";
import {
  failedStatusCount,
  latencySuccessRate,
  requestBytes,
  requestDuration,
  responseBytes,
  shapeSuccessRate,
  slowResponseCount,
  statusSuccessRate,
  successRate,
} from "./metrics.js";

const TOKEN = env("TOKEN", "");

function inferResponseGuard(targetUrl) {
  const lower = targetUrl.toLowerCase();
  if (lower.includes("/queryproject") || lower.endsWith("/query") || lower.includes("/query?"))
    return isProjectedQueryRes;
  return isQueryRes;
}

function inferEndpointKind(targetUrl) {
  const lower = targetUrl.toLowerCase();
  if (lower.includes("/queryproject")) return "queryproject";
  if (lower.endsWith("/query") || lower.includes("/query?")) return "queryroot";
  return "query";
}

function parseBodyAsJson(body) {
  if (!body) {
    return null;
  }

  try {
    return JSON.parse(body);
  } catch {
    return null;
  }
}

export function postQuery({
  name,
  body,
  url,
  slowMs = 1500,
  expectedStatus = 200,
  tags = {},
  responseGuard,
}) {
  const targetUrl = url ?? endpointUrl();
  const payload = JSON.stringify(body);
  const endpointKind = inferEndpointKind(targetUrl);
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
  requestDuration.add(res.timings.duration, { query_case: name, endpoint_kind: endpointKind });

  const payloadJson = parseBodyAsJson(res.body);
  const guard = responseGuard ?? inferResponseGuard(targetUrl);
  const statusOk = res.status === expectedStatus;
  const latencyOk = res.timings.duration < slowMs;
  const shapeOk = statusOk && payloadJson !== null && guard(payloadJson);

  const ok = check(res, {
    [`${name}: status ${expectedStatus}`]: () => statusOk,
    [`${name}: under ${slowMs}ms`]: () => latencyOk,
    [`${name}: response shape`]: () => shapeOk,
  });
  const metricTags = { query_case: name, endpoint_kind: endpointKind };
  successRate.add(ok, metricTags);
  statusSuccessRate.add(statusOk, metricTags);
  latencySuccessRate.add(latencyOk, metricTags);
  shapeSuccessRate.add(shapeOk, metricTags);

  if (!statusOk) failedStatusCount.add(1, metricTags);
  if (!latencyOk) slowResponseCount.add(1, metricTags);
  return res;
}

export function createMatrixCaseRunner(config) {
  const transport = createK6Transport({
    queryPath: config.queryPath,
    queryProjectPath: config.queryProjectPath,
    rootQueryPath: config.rootQueryPath,
  });
  const apiClient = createApiClient({
    baseUrl: config.baseUrl,
    token: config.token,
    transport,
  });
  const personClient = createPersonApiClient(apiClient);

  return {
    runCase(caseDef, args) {
      const body = caseDef.buildBody(args);
      const startPayload = JSON.stringify(body);
      requestBytes.add(startPayload.length);

      const response =
        caseDef.endpointKind === "query"
          ? personClient.queryPerson(body)
          : caseDef.endpointKind === "queryroot"
            ? personClient.queryRoot(body)
            : personClient.queryPersonProjected(body);

      const metricTags = {
        query_case: caseDef.caseId,
        endpoint_kind: caseDef.endpointKind,
      };

      const rawBody = response.rawBody ?? "";
      const measuredBytes = response.headers?.["Content-Length"]
        ? Number(response.headers["Content-Length"])
        : rawBody.length;
      responseBytes.add(Number.isFinite(measuredBytes) ? measuredBytes : 0, metricTags);

      const duration = response.meta?.duration ?? 0;
      requestDuration.add(duration, metricTags);

      const expectedStatus = 200;
      const statusOk = response.status === expectedStatus;
      const slowMs = caseDef.slowMs;
      const latencyOk = duration < slowMs;
      const guard = caseDef.endpointKind === "query" ? isQueryRes : isProjectedQueryRes;
      const shapeOk = statusOk && guard(response.data);

      const ok = check(response, {
        [`${caseDef.caseId}: status ${expectedStatus}`]: () => statusOk,
        [`${caseDef.caseId}: under ${slowMs}ms`]: () => latencyOk,
        [`${caseDef.caseId}: response shape`]: () => shapeOk,
      });

      successRate.add(ok, metricTags);
      statusSuccessRate.add(statusOk, metricTags);
      latencySuccessRate.add(latencyOk, metricTags);
      shapeSuccessRate.add(shapeOk, metricTags);

      if (!statusOk) {
        failedStatusCount.add(1, metricTags);
      }
      if (!latencyOk) {
        slowResponseCount.add(1, metricTags);
      }

      return response;
    },
  };
}
