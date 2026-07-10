import http from "k6/http";

function normalizePath(path) {
  if (!path) {
    return "/";
  }
  return path.startsWith("/") ? path : `/${path}`;
}

function splitBaseAndPath(url) {
  const schemeIdx = url.indexOf("://");
  if (schemeIdx < 0) {
    return { base: "", pathWithQuery: url };
  }

  const pathStart = url.indexOf("/", schemeIdx + 3);
  if (pathStart < 0) {
    return { base: url, pathWithQuery: "/" };
  }

  return {
    base: url.slice(0, pathStart),
    pathWithQuery: url.slice(pathStart),
  };
}

function replacePath(pathWithQuery, fromPath, toPath) {
  if (pathWithQuery === fromPath) {
    return toPath;
  }
  if (pathWithQuery.startsWith(`${fromPath}?`)) {
    return `${toPath}${pathWithQuery.slice(fromPath.length)}`;
  }
  return pathWithQuery;
}

function remapUrl(url, queryPath, queryProjectPath, rootQueryPath) {
  const { base, pathWithQuery } = splitBaseAndPath(url);
  const normalizedQueryPath = normalizePath(queryPath);
  const normalizedQueryProjectPath = normalizePath(queryProjectPath);
  const normalizedRootQueryPath = normalizePath(rootQueryPath);

  let mapped = replacePath(pathWithQuery, "/person/QueryConcrete", normalizedQueryPath);
  mapped = replacePath(mapped, "/person/QueryProject", normalizedQueryProjectPath);
  mapped = replacePath(mapped, "/Query", normalizedRootQueryPath);
  return `${base}${mapped}`;
}

function tryParseJson(body) {
  if (!body) {
    return undefined;
  }

  try {
    return JSON.parse(body);
  } catch {
    return undefined;
  }
}

function normalizeHeaders(headers = {}) {
  const mapped = {};
  for (const key of Object.keys(headers)) {
    const value = headers[key];
    mapped[key] = Array.isArray(value) ? String(value[0] ?? "") : String(value ?? "");
  }
  return mapped;
}

export function createK6Transport({ queryPath, queryProjectPath, rootQueryPath = "/Query", tags = {} }) {
  return function transport(request) {
    const targetUrl = remapUrl(request.url, queryPath, queryProjectPath, rootQueryPath);
    const response = http.request(request.method, targetUrl, request.body, {
      headers: request.headers,
      tags,
    });

    const data = tryParseJson(response.body);
    return {
      status: response.status,
      ok: response.status >= 200 && response.status < 300,
      headers: normalizeHeaders(response.headers),
      data,
      rawBody: response.body,
      meta: {
        duration: response.timings.duration,
      },
    };
  };
}
