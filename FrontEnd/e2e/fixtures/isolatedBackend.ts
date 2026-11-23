import { spawn } from "node:child_process";
import { randomBytes } from "node:crypto";
import { once } from "node:events";
import { access, mkdtemp, realpath, rm, stat } from "node:fs/promises";
import { tmpdir } from "node:os";
import { dirname, isAbsolute, join, relative } from "node:path";
import { fileURLToPath } from "node:url";
import { test as base } from "@playwright/test";

export const isolationHeader = "X-GF3-E2E-Instance";
const publishDirectory = fileURLToPath(new URL("../../../GF3.Tests/TestResults/e2e-web/", import.meta.url));
const systemEnvironment = /^(PATH|SystemRoot|WINDIR|TEMP|TMP|TMPDIR|HOME|USERPROFILE|LOCALAPPDATA|APPDATA|DOTNET_ROOT|DOTNET_ROOT_X64|LANG|LC_ALL)$/i;

export async function assertIsolatedBackend(url: string, instance: string, databasePath?: string) {
  const origin = new URL(url);
  if (origin.protocol !== "http:" || origin.hostname !== "127.0.0.1" || !origin.port ||
      origin.pathname !== "/" || origin.username || origin.password || origin.search || origin.hash ||
      !/^[a-f0-9]{32}$/.test(instance))
    throw new Error("Expected an isolated loopback GF3 test backend.");
  const response = await fetch(new URL("/api/e2e-isolation", origin), {
    headers: { [isolationHeader]: instance }, redirect: "error", signal: AbortSignal.timeout(5_000),
  });
  if (!response.ok) throw new Error("The Web API did not confirm its isolated test instance.");
  const result = await response.json() as { databasePath: string };
  if (!result.databasePath || (databasePath !== undefined && result.databasePath !== databasePath))
    throw new Error("The Web API is using an unexpected test database.");
  const difference = relative(await realpath(tmpdir()), await realpath(result.databasePath));
  if (!difference || difference === ".." || difference.startsWith("../") || difference.startsWith("..\\") || isAbsolute(difference))
    throw new Error("The registered browser database is outside the OS temporary directory.");
  return result.databasePath;
}

export async function startIsolatedBackend() {
  const executable = join(publishDirectory, "WebApi.dll");
  await access(executable).catch(() => { throw new Error("Run npm run test:e2e:prepare before the live Playwright specs."); });
  const temporaryRoot = await realpath(tmpdir());
  const directory = await mkdtemp(join(temporaryRoot, "gf3-browser-"));
  const databasePath = join(directory, "fixture.sqlite");
  const instance = randomBytes(16).toString("hex");
  const child = spawn("dotnet", [executable, "--gf3-e2e"], {
    cwd: publishDirectory,
    env: {
      ...Object.fromEntries(Object.entries(process.env).filter(([key]) => systemEnvironment.test(key))),
      ASPNETCORE_ENVIRONMENT: "Testing",
      DOTNET_ENVIRONMENT: "Testing",
      GF3_E2E_DIRECTORY: directory,
      GF3_E2E_INSTANCE: instance,
      ConnectionStrings__Default: `Data Source=${databasePath}`,
      GF3_BOOTSTRAP_MANAGER_PASSWORD: "123456",
      Jwt__SigningKey: randomBytes(48).toString("hex"),
      Push__Enabled: "false",
      Logging__LogLevel__Default: "Warning",
      "Logging__LogLevel__Microsoft.Hosting.Lifetime": "Information",
    },
    windowsHide: true,
  });
  const close = async () => {
    if (child.pid && child.exitCode === null && child.signalCode === null) {
      const exited = once(child, "exit");
      child.kill();
      await exited;
    }
    const actual = await realpath(directory);
    if (actual !== directory || dirname(actual) !== temporaryRoot)
      throw new Error("Refusing to clean up an unexpected browser-test directory.");
    await rm(directory, { recursive: true, force: true, maxRetries: 3 });
  };

  try {
    const url = await new Promise<string>((resolve, reject) => {
      let output = "";
      const timer = setTimeout(() => reject(new Error(`Isolated Web API startup timed out.\n${output}`)), 25_000);
      child.once("error", error => { clearTimeout(timer); reject(error); });
      child.once("exit", code => { clearTimeout(timer); reject(new Error(`Isolated Web API exited with ${code}.\n${output}`)); });
      const collect = (chunk: Buffer) => {
        output = (output + chunk.toString()).slice(-6000);
        const match = /Now listening on: (http:\/\/127\.0\.0\.1:\d+)/.exec(output);
        if (match) {
          clearTimeout(timer);
          resolve(match[1]);
        }
      };
      child.stdout.on("data", collect);
      child.stderr.on("data", collect);
    });
    await assertIsolatedBackend(url, instance, databasePath);
    if (!(await stat(databasePath)).size) throw new Error("The isolated database was not initialized.");
    return { url, instance, databasePath, close };
  } catch (error) {
    await close();
    throw error;
  }
}

export const test = base.extend<{ isolatedBackend: Awaited<ReturnType<typeof startIsolatedBackend>> }>({
  // Playwright reads fixture dependencies from the destructured first argument.
  // eslint-disable-next-line no-empty-pattern
  isolatedBackend: async ({}, runTest) => {
    const backend = await startIsolatedBackend();
    try {
      await runTest(backend);
    } finally {
      await backend.close();
    }
  },
  baseURL: async ({ isolatedBackend }, runTest) => { await runTest(isolatedBackend.url); },
});
