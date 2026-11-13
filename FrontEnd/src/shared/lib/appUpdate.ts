export type AppVersion = { buildId: string };

export async function fetchLatestBuildId(signal?: AbortSignal): Promise<string | null> {
  try {
    const response = await fetch("/version.json", {
      cache: "no-store", redirect: "error", headers: { Accept: "application/json" }, signal,
    });
    if (!response.ok || response.headers.get("Content-Type")?.split(";")[0].trim().toLowerCase() !== "application/json") return null;
    const value: unknown = await response.json();
    if (!value || typeof value !== "object" || !("buildId" in value)) return null;
    return typeof value.buildId === "string" && value.buildId.trim() ? value.buildId : null;
  } catch {
    return null;
  }
}

export function isNewBuild(currentBuildId: string, remoteBuildId: string | null): boolean {
  return !!currentBuildId.trim() && !!remoteBuildId?.trim() && currentBuildId !== remoteBuildId;
}
