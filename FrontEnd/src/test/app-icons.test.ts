import { readFileSync } from "node:fs";
import path from "node:path";
import { describe, expect, it } from "vitest";

const publicRoot = path.resolve(process.cwd(), "public");
const manifest = JSON.parse(readFileSync(path.join(publicRoot, "manifest.json"), "utf8"));
const pngDimensions = (src: string) => {
  const bytes = readFileSync(path.join(publicRoot, src));
  expect(bytes.length).toBeGreaterThan(24);
  expect([...bytes.subarray(0, 8)]).toEqual([137, 80, 78, 71, 13, 10, 26, 10]);
  expect(bytes.toString("ascii", 12, 16)).toBe("IHDR");
  return [bytes.readUInt32BE(16), bytes.readUInt32BE(20)];
};

describe("application icons", () => {
  it("declares the exact application identity and standard icons", () => {
    expect(manifest).toEqual({
      id: "/", name: "GF", short_name: "GF", start_url: "/", scope: "/",
      display: "standalone", background_color: "#FFFFFF", theme_color: "#FFFFFF",
      icons: [
        { src: "/icons/gf-icon-192.png", sizes: "192x192", type: "image/png", purpose: "any" },
        { src: "/icons/gf-icon-512.png", sizes: "512x512", type: "image/png", purpose: "any" },
      ],
    });
  });

  it("references each icon and manifest once in the app entry point", () => {
    const html = new DOMParser().parseFromString(readFileSync(path.resolve("index.html"), "utf8"), "text/html");
    for (const [rel, attributes] of [
      ["icon", { href: "/favicon-32x32.png", type: "image/png", sizes: "32x32" }],
      ["apple-touch-icon", { href: "/apple-touch-icon.png", sizes: "180x180" }],
      ["manifest", { href: "/manifest.json" }],
    ] as const) {
      const tags = html.head.querySelectorAll(`link[rel="${rel}"]`);
      expect(tags).toHaveLength(1);
      for (const [name, value] of Object.entries(attributes)) expect(tags[0].getAttribute(name)).toBe(value);
    }
    for (const [name, content] of [
      ["apple-mobile-web-app-title", "GF"],
      ["apple-mobile-web-app-capable", "yes"],
      ["theme-color", "#FFFFFF"],
    ]) {
      const tags = html.head.querySelectorAll(`meta[name="${name}"]`);
      expect(tags).toHaveLength(1);
      expect(tags[0].getAttribute("content")).toBe(content);
    }
    expect(html.title).toBe("GF");
  });

  it.each([
    ["apple-touch-icon.png", 180],
    ["icons/gf-icon-192.png", 192],
    ["icons/gf-icon-512.png", 512],
    ["favicon-32x32.png", 32],
  ])("saves %s as a PNG with the required dimensions", (src, size) => {
    expect(pngDimensions(String(src))).toEqual([size, size]);
  });

  it("resolves manifest icons inside public with matching declared sizes", () => {
    for (const icon of manifest.icons) {
      const relative = icon.src.replace(/^\//, "");
      const resolved = path.resolve(publicRoot, relative);
      expect(resolved.startsWith(publicRoot + path.sep)).toBe(true);
      expect(pngDimensions(relative).join("x")).toBe(icon.sizes);
    }
  });
});
