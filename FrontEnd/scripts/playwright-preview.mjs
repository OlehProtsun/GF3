import { build, preview } from "vite";
import { resolve } from "node:path";

const root = process.cwd();
const port = Number(process.argv[2]);
if (!Number.isInteger(port) || port < 1024 || port > 65535) {
  throw new Error("A valid loopback test port is required.");
}
const outDir = resolve(root, `../GF3.Tests/TestResults/playwright-preview/${port}`);
await build({
  configFile: resolve(root, "vite.config.ts"),
  build: {
    outDir,
    emptyOutDir: true,
    rollupOptions: {
      input: {
        app: resolve(root, "index.html"),
        promo: resolve(root, "promo.html"),
        viewportFixture: resolve(root, "e2e/fixtures/viewport.html"),
      },
    },
  },
});

await preview({
  configFile: false,
  build: { outDir },
  preview: { host: "127.0.0.1", port, strictPort: true },
});
