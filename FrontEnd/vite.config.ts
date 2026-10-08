import { defineConfig } from "vitest/config";
import react from "@vitejs/plugin-react";
import path from "node:path";
import svgr from "vite-plugin-svgr";

const projectRoot = process.cwd();

export default defineConfig({
  root: projectRoot,
  plugins: [react(), svgr()],
  build: {
    rollupOptions: {
      input: {
        app: path.resolve(projectRoot, "index.html"),
        promo: path.resolve(projectRoot, "promo.html"),
      },
    },
  },
  resolve: {
    dedupe: ["react", "react-dom"],
    alias: {
      "@app": path.resolve(projectRoot, "src/app"),
      "@pages": path.resolve(projectRoot, "src/pages"),
      "@features": path.resolve(projectRoot, "src/features"),
      "@entities": path.resolve(projectRoot, "src/entities"),
      "@shared": path.resolve(projectRoot, "src/shared"),
      "@tanstack/react-query": path.resolve(projectRoot, "src/shared/lib/tanstack/react-query.tsx"),
      "@tanstack/react-query-devtools": path.resolve(projectRoot, "src/shared/lib/tanstack/react-query-devtools.tsx"),
      "react-router-dom": path.resolve(projectRoot, "src/shared/lib/react-router-dom.tsx"),
    },
  },
  server: {
    host: "localhost",
    port: 5173,
    strictPort: true,
    proxy: {
      "/api": {
        target: "https://localhost:54294",
        changeOrigin: true,
        secure: false,
        ws: true,
      },
    },
  },
  test: {
    environment: "jsdom",
    setupFiles: path.resolve(projectRoot, "src/test/setup.ts"),
    exclude: ["e2e/**", "node_modules/**", "dist/**"],
  },
});
