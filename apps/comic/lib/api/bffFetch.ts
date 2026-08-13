/** Browser fetch against this app's BFF. 401 → login with return path. */
export async function bffFetch(input: RequestInfo | URL, init?: RequestInit): Promise<Response> {
  const res = await fetch(input, init);
  if (res.status === 401 && typeof window !== "undefined") {
    const ret = window.location.pathname + window.location.search;
    window.location.replace(`/login?return=${encodeURIComponent(ret)}`);
  }
  return res;
}
