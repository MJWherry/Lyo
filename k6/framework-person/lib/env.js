export function toInt(name, fallback) {
  const raw = __ENV[name];
  if (raw === undefined || raw === null || raw === "") return fallback;
  const value = Number(raw);
  return Number.isFinite(value) ? Math.trunc(value) : fallback;
}

export function toFloat(name, fallback) {
  const raw = __ENV[name];
  if (raw === undefined || raw === null || raw === "") return fallback;
  const value = Number(raw);
  return Number.isFinite(value) ? value : fallback;
}

export function toBool(name, fallback = false) {
  const raw = (__ENV[name] || "").toLowerCase().trim();
  if (!raw) return fallback;
  return raw === "1" || raw === "true" || raw === "yes" || raw === "y";
}

export function env(name, fallback) {
  const raw = __ENV[name];
  return raw === undefined || raw === null || raw === "" ? fallback : raw;
}

/** Absolute URL for a path under BASE_URL (leading slash optional). */
export function apiUrl(relativePath) {
  const base = env("BASE_URL", "http://localhost:5251").replace(/\/+$/, "");
  const path = relativePath.startsWith("/") ? relativePath : `/${relativePath}`;
  return `${base}${path}`;
}

export function endpointUrl() {
  return apiUrl(env("ENDPOINT_PATH", "/person/query"));
}

/** POST body for projected query (QueryProject). Prefer QUERY_PROJECT_PATH; QUERY_SELECT_PATH is a legacy fallback. */
export function queryProjectUrl() {
  return apiUrl(env("QUERY_PROJECT_PATH", env("QUERY_SELECT_PATH", "/person/QueryProject")));
}

/** Vary amount within [min, max] to avoid REM cache hits. Uses iter + vu for spread. */
export function variedAmount(min, max, iter, vu) {
  const span = Math.max(1, max - min + 1);
  return min + ((iter * 17 + vu * 13) % span);
}

/** Vary start offset to avoid REM cache hits. */
export function variedStart(maxStart, iter, vu) {
  return (iter * 7 + vu * 11) % Math.max(1, maxStart);
}
