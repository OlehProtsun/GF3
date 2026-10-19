export function createSwapLink(swapId: number, origin: string): string {
  if (!Number.isSafeInteger(swapId) || swapId <= 0) throw new RangeError("Invalid swap ID");
  const url = new URL("/swap", origin);
  url.searchParams.set("swapId", String(swapId));
  return url.href;
}

export function parseSwapLinkId(search: string): number | null {
  if (/%(?![0-9a-f]{2})/i.test(search)) return null;
  const values = new URLSearchParams(search).getAll("swapId");
  if (values.length !== 1 || !/^[0-9]+$/.test(values[0])) return null;
  const id = Number(values[0]);
  return Number.isSafeInteger(id) && id > 0 ? id : null;
}
