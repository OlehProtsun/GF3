import { createHash } from "node:crypto";
import { readFile, writeFile } from "node:fs/promises";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const directory = dirname(fileURLToPath(import.meta.url));
const sourcePath = join(directory, "umowa-saas-b2b-pl.md");
const outputPath = join(directory, "umowa-saas-b2b-pl.html");
const readmePath = join(directory, "README.md");
const markdown = (await readFile(sourcePath, "utf8")).replace(/\r\n/g, "\n");

const escapeHtml = (value) => value
  .replaceAll("&", "&amp;")
  .replaceAll("<", "&lt;")
  .replaceAll(">", "&gt;")
  .replaceAll('"', "&quot;");

const inline = (value) => escapeHtml(value)
  .replace(/\*\*(.+?)\*\*/g, "<strong>$1</strong>")
  .replace(/`(.+?)`/g, "<code>$1</code>");

const isTableDivider = (line) => /^\s*\|?(?:\s*:?-{3,}:?\s*\|)+\s*:?-{3,}:?\s*\|?\s*$/.test(line);
const tableCells = (line) => line.trim().replace(/^\|/, "").replace(/\|$/, "").split("|").map((cell) => cell.trim());
const isBlockStart = (line, nextLine = "") =>
  /^#{1,3}\s+/.test(line) ||
  /^>\s?/.test(line) ||
  /^\s*\d+\.\s+/.test(line) ||
  /^\s*-\s+/.test(line) ||
  (line.includes("|") && isTableDivider(nextLine));

function renderMarkdown(source) {
  const lines = source.split("\n");
  const blocks = [];
  let index = 0;

  while (index < lines.length) {
    const line = lines[index];
    if (!line.trim()) {
      index += 1;
      continue;
    }

    const heading = /^(#{1,3})\s+(.+)$/.exec(line);
    if (heading) {
      const level = heading[1].length;
      blocks.push(`<h${level}>${inline(heading[2])}</h${level}>`);
      index += 1;
      continue;
    }

    if (/^>\s?/.test(line)) {
      const quote = [];
      while (index < lines.length && /^>\s?/.test(lines[index])) {
        quote.push(lines[index].replace(/^>\s?/, "").trim());
        index += 1;
      }
      blocks.push(`<aside class="legal-notice">${inline(quote.join(" "))}</aside>`);
      continue;
    }

    if (line.includes("|") && index + 1 < lines.length && isTableDivider(lines[index + 1])) {
      const headers = tableCells(line);
      index += 2;
      const rows = [];
      while (index < lines.length && lines[index].includes("|") && lines[index].trim()) {
        rows.push(tableCells(lines[index]));
        index += 1;
      }
      blocks.push([
        "<table>",
        `<thead><tr>${headers.map((cell) => `<th>${inline(cell)}</th>`).join("")}</tr></thead>`,
        `<tbody>${rows.map((row) => `<tr>${row.map((cell) => `<td>${inline(cell)}</td>`).join("")}</tr>`).join("")}</tbody>`,
        "</table>",
      ].join(""));
      continue;
    }

    const numbered = /^(\s*)(\d+)\.\s+(.+)$/.exec(line);
    if (numbered) {
      const depth = Math.min(3, Math.floor(numbered[1].replace(/\t/g, "   ").length / 3));
      blocks.push(`<p class="clause clause-${depth}"><span class="clause-number">${numbered[2]}.</span><span>${inline(numbered[3])}</span></p>`);
      index += 1;
      continue;
    }

    const bullet = /^(\s*)-\s+(.+)$/.exec(line);
    if (bullet) {
      const depth = Math.min(3, Math.floor(bullet[1].replace(/\t/g, "   ").length / 3));
      blocks.push(`<p class="bullet bullet-${depth}"><span>•</span><span>${inline(bullet[2])}</span></p>`);
      index += 1;
      continue;
    }

    const paragraph = [line.trim()];
    index += 1;
    while (index < lines.length && lines[index].trim() && !isBlockStart(lines[index], lines[index + 1] ?? "")) {
      paragraph.push(lines[index].trim());
      index += 1;
    }
    const indentClass = /^\s{2,}/.test(line) ? " indented" : "";
    blocks.push(`<p class="paragraph${indentClass}">${inline(paragraph.join(" "))}</p>`);
  }

  return blocks.join("\n");
}

const sourceHash = createHash("sha256").update(markdown, "utf8").digest("hex");
const body = renderMarkdown(markdown);
const html = `<!doctype html>
<html lang="pl">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <meta name="source-sha256" content="${sourceHash}" />
  <title>Umowa o świadczenie usług SaaS – GF</title>
  <style>
    @page {
      size: A4;
      margin: 20mm 18mm 20mm 18mm;
      @bottom-center {
        content: "Strona " counter(page) " z " counter(pages);
        color: #667085;
        font: 8.5pt Arial, sans-serif;
      }
    }
    :root {
      color: #182230;
      background: #edf1f5;
      font-family: Georgia, "Times New Roman", serif;
      font-size: 11pt;
      line-height: 1.48;
      font-synthesis: none;
    }
    *, *::before, *::after { box-sizing: border-box; }
    html, body { margin: 0; min-height: 100%; }
    body { padding: 20px; }
    .document {
      width: 210mm;
      margin: 0 auto;
      padding: 20mm 18mm;
      background: #fff;
      box-shadow: 0 18px 55px rgba(24, 34, 48, 0.16);
    }
    h1, h2, h3 { color: #101828; break-after: avoid; page-break-after: avoid; }
    h1 {
      margin: 0 0 8mm;
      padding-bottom: 5mm;
      border-bottom: 1.5px solid #98a2b3;
      font-family: Arial, sans-serif;
      font-size: 21pt;
      line-height: 1.15;
      letter-spacing: 0.03em;
      text-align: center;
    }
    h2 {
      margin: 9mm 0 4mm;
      padding-bottom: 1.8mm;
      border-bottom: 1px solid #d0d5dd;
      font-family: Arial, sans-serif;
      font-size: 14pt;
      line-height: 1.25;
    }
    h3 {
      margin: 5mm 0 2.5mm;
      font-family: Arial, sans-serif;
      font-size: 11.5pt;
    }
    p, li { orphans: 3; widows: 3; }
    .paragraph { margin: 0 0 3.2mm; text-align: justify; }
    .paragraph.indented { margin-left: 9mm; }
    .legal-notice {
      margin: 0 0 7mm;
      padding: 4mm 5mm;
      border: 1px solid #f2c94c;
      border-left: 4px solid #d69e00;
      background: #fffae8;
      font-family: Arial, sans-serif;
      font-size: 9.2pt;
      line-height: 1.45;
      break-inside: avoid;
    }
    .clause, .bullet {
      display: grid;
      grid-template-columns: 8mm 1fr;
      gap: 1.5mm;
      margin: 0 0 2.7mm;
      text-align: justify;
      orphans: 3;
      widows: 3;
    }
    .clause-1, .bullet-1 { margin-left: 9.5mm; }
    .clause-2, .bullet-2 { margin-left: 19mm; }
    .clause-3, .bullet-3 { margin-left: 28.5mm; }
    .clause-number { font-weight: 700; }
    strong { color: #101828; }
    code {
      padding: 0.2mm 1mm;
      border-radius: 1mm;
      background: #f2f4f7;
      font: 9pt Consolas, monospace;
    }
    table {
      width: 100%;
      margin: 4mm 0 7mm;
      border-collapse: collapse;
      font-family: Arial, sans-serif;
      font-size: 9pt;
      line-height: 1.35;
      break-inside: avoid;
    }
    thead { display: table-header-group; }
    tr { break-inside: avoid; page-break-inside: avoid; }
    th, td { padding: 2.5mm 2.8mm; border: 1px solid #d0d5dd; vertical-align: top; }
    th { background: #f2f4f7; text-align: left; }
    h2 + .clause, h2 + .paragraph, h3 + .clause, h3 + .paragraph { margin-top: 0; }
    @media screen and (max-width: 850px) {
      body { padding: 0; background: #fff; }
      .document { width: 100%; padding: 28px 22px; box-shadow: none; }
      h1 { font-size: 18pt; }
      table { display: block; overflow-x: auto; }
    }
    @media print {
      html, body { min-height: 0; background: #fff; }
      body {
        padding: 0;
        print-color-adjust: exact;
        -webkit-print-color-adjust: exact;
      }
      .document { width: auto; margin: 0; padding: 0; box-shadow: none; }
      h1, h2, h3 { break-after: avoid; page-break-after: avoid; }
      p, li { orphans: 3; widows: 3; }
      table, .legal-notice { break-inside: avoid; page-break-inside: avoid; }
    }
  </style>
</head>
<body>
  <main class="document">
${body}
  </main>
</body>
</html>
`;

await writeFile(outputPath, html, "utf8");

const inventorySources = await Promise.all([
  readFile(sourcePath, "utf8"),
  readFile(join(directory, "zalacznik-1-zakres-uslug.md"), "utf8"),
  readFile(join(directory, "checklista-przed-podpisaniem.md"), "utf8"),
]);
const placeholderPattern = /\[(?:DO WYPEŁNIENIA|DO WERYFIKACJI PRAWNEJ|DO WERYFIKACJI KSIĘGOWEJ|DO WERYFIKACJI TECHNICZNEJ|DO WERYFIKACJI Z KSIĘGOWYM LUB PRAWNIKIEM)[^\]\n]*\]/g;
const placeholders = [...new Set(inventorySources.flatMap((content) => content.match(placeholderPattern) ?? []))]
  .sort((left, right) => left.localeCompare(right, "pl"));
const inventory = placeholders.map((placeholder) => `- \`${placeholder}\``).join("\n");
const inventoryStart = "<!-- PLACEHOLDER_INVENTORY_START -->";
const inventoryEnd = "<!-- PLACEHOLDER_INVENTORY_END -->";
const readme = await readFile(readmePath, "utf8");
const inventoryExpression = new RegExp(`${inventoryStart}[\\s\\S]*?${inventoryEnd}`);
if (!inventoryExpression.test(readme)) {
  throw new Error("Nie znaleziono znaczników inwentarza placeholderów w README.md.");
}
await writeFile(
  readmePath,
  readme.replace(inventoryExpression, `${inventoryStart}\n\n${inventory}\n\n${inventoryEnd}`),
  "utf8",
);

console.log(`Wygenerowano ${outputPath}`);
console.log(`SHA-256 źródła: ${sourceHash}`);
console.log(`Zaktualizowano inwentarz: ${placeholders.length} unikalnych placeholderów`);
