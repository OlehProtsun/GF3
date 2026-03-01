import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import path from "node:path";

export default defineConfig({
  plugins: [react()],
  resolve: {
    dedupe: ["react", "react-dom"],
    alias: {
      "@app": path.resolve(__dirname, "src/app"),
      "@pages": path.resolve(__dirname, "src/pages"),
      "@features": path.resolve(__dirname, "src/features"),
      "@entities": path.resolve(__dirname, "src/entities"),
      "@shared": path.resolve(__dirname, "src/shared"),
      "@tanstack/react-query": path.resolve(__dirname, "src/shared/lib/tanstack/react-query.tsx"),
      "@tanstack/react-query-devtools": path.resolve(__dirname, "src/shared/lib/tanstack/react-query-devtools.tsx"),
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
      },
    },
  },
});
