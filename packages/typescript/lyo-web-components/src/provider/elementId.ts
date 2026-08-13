export function normalizeElementIdSegment(value: string | null | undefined, fallback = "default"): string {
  if (!value || !value.trim()) return fallback;
  const trimmed = value.trim();
  const out: string[] = [];
  let prevWasSep = false;
  for (let i = 0; i < trimmed.length; i++) {
    const c = trimmed[i];
    if (c === "_" || c === "-" || /\s/.test(c)) {
      if (out.length > 0 && out[out.length - 1] !== "-") out.push("-");
      prevWasSep = true;
      continue;
    }
    if (/[A-Z]/.test(c)) {
      if (out.length > 0 && !prevWasSep && /[a-z]/.test(trimmed[i - 1])) out.push("-");
      out.push(c.toLowerCase());
      prevWasSep = false;
      continue;
    }
    if (/[a-z0-9]/i.test(c)) {
      out.push(c.toLowerCase());
      prevWasSep = false;
      continue;
    }
    if (out.length > 0 && out[out.length - 1] !== "-") out.push("-");
    prevWasSep = true;
  }
  const normalized = out.join("").replace(/-+/g, "-").replace(/^-|-$/g, "");
  return normalized || fallback;
}

export function dataGridElementId(gridKey: string): string {
  return `lyo-data-grid-${normalizeElementIdSegment(gridKey)}`;
}

export function dataGridProjectedElementId(gridKey: string): string {
  return `lyo-data-grid-projected-${normalizeElementIdSegment(gridKey)}`;
}

export function resolveElementId(elementId: string | undefined, defaultId: string): string {
  return elementId?.trim() ? normalizeElementIdSegment(elementId) : defaultId;
}
