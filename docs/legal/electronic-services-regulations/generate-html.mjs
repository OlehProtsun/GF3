import { createHash } from "node:crypto";
import { readFile, writeFile } from "node:fs/promises";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const directory = dirname(fileURLToPath(import.meta.url));
const sourcePath = join(directory, "regulamin-swiadczenia-uslug-elektronicznych-pl.md");
const outputPath = join(directory, "regulamin-swiadczenia-uslug-elektronicznych-pl.html");
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

const slugify = (value) => value
  .toLocaleLowerCase("pl")
  .replaceAll("ł", "l")
  .normalize("NFD")
  .replace(/[\u0300-\u036f]/g, "")
  .replace(/§/g, "paragraf")
  .replace(/[^a-z0-9]+/g, "-")
  .replace(/^-+|-+$/g, "");

const headings = [];
const usedSlugs = new Map();
for (const line of markdown.split("\n")) {
  const match = /^(#{2,3})\s+(.+)$/.exec(line);
  if (!match) continue;
  const base = slugify(match[2]) || "sekcja";
  const count = (usedSlugs.get(base) ?? 0) + 1;
  usedSlugs.set(base, count);
  headings.push({
    level: match[1].length,
    text: match[2],
    id: count === 1 ? base : `${base}-${count}`,
  });
}

const headingQueue = [...headings];
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
      const item = level >= 2 ? headingQueue.shift() : null;
      const id = item ? ` id="${item.id}"` : "";
      const anchor = item ? `<a class="heading-link" href="#${item.id}" aria-label="Odnośnik do tej sekcji">§</a>` : "";
      blocks.push(`<h${level}${id}>${inline(heading[2])}${anchor}</h${level}>`);
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
        "<div class=\"table-scroll\"><table>",
        `<thead><tr>${headers.map((cell) => `<th>${inline(cell)}</th>`).join("")}</tr></thead>`,
        `<tbody>${rows.map((row) => `<tr>${row.map((cell) => `<td>${inline(cell)}</td>`).join("")}</tr>`).join("")}</tbody>`,
        "</table></div>",
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
      blocks.push(`<p class="bullet bullet-${depth}"><span aria-hidden="true">•</span><span>${inline(bullet[2])}</span></p>`);
      index += 1;
      continue;
    }

    const paragraph = [line.trim()];
    index += 1;
    while (index < lines.length && lines[index].trim() && !isBlockStart(lines[index], lines[index + 1] ?? "")) {
      paragraph.push(lines[index].trim());
      index += 1;
    }
    blocks.push(`<p class="paragraph">${inline(paragraph.join(" "))}</p>`);
  }

  return blocks.join("\n");
}

const toc = headings
  .filter((item) => item.level === 2)
  .map((item) => `<li><a href="#${item.id}">${inline(item.text)}</a></li>`)
  .join("\n");
const sourceHash = createHash("sha256").update(markdown, "utf8").digest("hex");
const body = renderMarkdown(markdown);
const versionPlaceholder = "[DO WYPEŁNIENIA: NUMER WERSJI]";
const effectiveDatePlaceholder = "[DO WYPEŁNIENIA: DATA OBOWIĄZYWANIA]";
const tocMarkup = `<nav class="toc" aria-label="Spis treści">
      <h2>Spis treści</h2>
      <ol>
${toc}
      </ol>
    </nav>`;
const bodyWithToc = body.replace(/(?=<h2 id=)/, `${tocMarkup}\n`);

const html = `<!doctype html>
<html lang="pl">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <meta name="source-sha256" content="${sourceHash}" />
  <title>Regulamin świadczenia usług drogą elektroniczną – GF</title>
  <style>
    @page {
      size: A4;
      margin: 18mm;
      @bottom-left {
        content: "Regulamin GF • wersja ${versionPlaceholder}";
        color: #667085;
        font: 8pt Arial, sans-serif;
      }
      @bottom-right {
        content: "Strona " counter(page) " z " counter(pages);
        color: #667085;
        font: 8pt Arial, sans-serif;
      }
    }
    :root {
      color: #182230;
      background: #eef2f6;
      font-family: Georgia, "Times New Roman", serif;
      font-size: 11pt;
      line-height: 1.52;
      font-synthesis: none;
    }
    *, *::before, *::after { box-sizing: border-box; }
    html { scroll-behavior: smooth; }
    html, body { margin: 0; min-height: 100%; }
    body { padding: 20px; }
    .document {
      width: min(210mm, 100%);
      margin: 0 auto;
      padding: 18mm;
      background: #fff;
      box-shadow: 0 18px 55px rgba(24, 34, 48, 0.15);
    }
    h1, h2, h3 { color: #101828; break-after: avoid; page-break-after: avoid; scroll-margin-top: 18px; }
    h1 {
      margin: 0 0 7mm;
      padding-bottom: 5mm;
      border-bottom: 2px solid #344054;
      font: 700 20pt/1.18 Arial, sans-serif;
      letter-spacing: 0.025em;
      text-align: center;
    }
    h2 {
      position: relative;
      margin: 9mm 0 4mm;
      padding-bottom: 1.8mm;
      border-bottom: 1px solid #d0d5dd;
      font: 700 14pt/1.28 Arial, sans-serif;
    }
    h3 { margin: 5mm 0 2.5mm; font: 700 11.5pt/1.3 Arial, sans-serif; }
    .heading-link { float: right; color: #98a2b3; text-decoration: none; font-weight: 400; }
    .heading-link:hover, .heading-link:focus { color: #344054; }
    .toc {
      margin: 0 0 9mm;
      padding: 5mm 6mm;
      border: 1px solid #d0d5dd;
      border-radius: 3mm;
      background: #f8fafc;
      font-family: Arial, sans-serif;
      break-inside: avoid;
    }
    .toc h2 { margin: 0 0 3mm; padding: 0; border: 0; font-size: 12pt; }
    .toc ol { columns: 2; column-gap: 8mm; margin: 0; padding-left: 6mm; }
    .toc li { margin: 0 0 1.3mm; break-inside: avoid; font-size: 8.7pt; }
    .toc a { color: #344054; text-decoration: none; }
    .toc a:hover, .toc a:focus { text-decoration: underline; }
    p, li { orphans: 3; widows: 3; }
    .paragraph { margin: 0 0 3.2mm; text-align: justify; }
    .legal-notice {
      margin: 0 0 7mm;
      padding: 4mm 5mm;
      border: 1px solid #f2c94c;
      border-left: 4px solid #d69e00;
      background: #fffae8;
      font: 9.2pt/1.45 Arial, sans-serif;
      break-inside: avoid;
    }
    .clause, .bullet {
      display: grid;
      grid-template-columns: 8mm minmax(0, 1fr);
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
    code { padding: 0.2mm 1mm; border-radius: 1mm; background: #f2f4f7; font: 9pt Consolas, monospace; }
    .table-scroll { width: 100%; overflow-x: auto; }
    table {
      width: 100%;
      margin: 4mm 0 7mm;
      border-collapse: collapse;
      font: 9pt/1.35 Arial, sans-serif;
    }
    thead { display: table-header-group; }
    tr { break-inside: avoid; page-break-inside: avoid; }
    th, td { padding: 2.5mm 2.8mm; border: 1px solid #d0d5dd; vertical-align: top; }
    th { background: #f2f4f7; text-align: left; }
    .doc-footer {
      margin: 10mm auto 0;
      padding-top: 4mm;
      border-top: 1px solid #d0d5dd;
      color: #667085;
      font: 8.5pt/1.4 Arial, sans-serif;
      text-align: center;
    }
    @media screen and (max-width: 850px) {
      body { padding: 0; background: #fff; }
      .document { width: 100%; padding: 26px 20px; box-shadow: none; }
      h1 { font-size: 17pt; }
      h2 { font-size: 13pt; }
      .toc ol { columns: 1; }
      .clause-1, .bullet-1 { margin-left: 5mm; }
      .clause-2, .bullet-2 { margin-left: 10mm; }
      .clause-3, .bullet-3 { margin-left: 15mm; }
    }
    @media print {
      html, body { min-height: 0; background: #fff; }
      body { padding: 0; print-color-adjust: exact; -webkit-print-color-adjust: exact; }
      .document { width: auto; margin: 0; padding: 0; box-shadow: none; }
      nav { display: none; }
      .heading-link { display: none; }
      h1, h2, h3 { break-after: avoid; page-break-after: avoid; }
      p, li { orphans: 3; widows: 3; }
      table, .legal-notice { break-inside: avoid; page-break-inside: avoid; }
      .table-scroll { overflow: visible; }
    }
  </style>
</head>
<body>
  <main class="document">
    <article>
${bodyWithToc}
    </article>
    <footer class="doc-footer">
      Regulamin świadczenia usług drogą elektroniczną Platformy GF • wersja ${versionPlaceholder} • obowiązuje od ${effectiveDatePlaceholder}
    </footer>
  </main>
</body>
</html>
`;

await writeFile(outputPath, html, "utf8");

const inventorySources = await Promise.all([
  readFile(sourcePath, "utf8"),
  readFile(join(directory, "informacja-o-zagrozeniach-pl.md"), "utf8"),
  readFile(join(directory, "checklista-wdrozenia-regulaminu.md"), "utf8"),
]);
const placeholderPattern = /\[(?:DO WYPEŁNIENIA|DO WERYFIKACJI)[^\]\n]*\]/g;
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
