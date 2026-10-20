import { readFile, readdir, writeFile } from "node:fs/promises";
import { dirname, join, relative } from "node:path";
import { fileURLToPath } from "node:url";

// Existing Markdown is authoritative. This command only renders derivatives and inventories fields.
const packageRoot = dirname(fileURLToPath(import.meta.url));
const repoRoot = join(packageRoot, "../..");
const check = process.argv.includes("--check");
async function output(path, content) {
  if (check) {
    if (await readFile(path, "utf8") !== content) throw new Error(`Stale derivative: ${relative(repoRoot, path)}`);
  } else await writeFile(path, content);
}
function esc(value) {
  return String(value).replaceAll("&", "&amp;").replaceAll("<", "&lt;").replaceAll(">", "&gt;").replaceAll('"', "&quot;");
}

function inline(value) {
  let out = esc(value);
  out = out.replace(/\[([^\]]+)\]\(([^)]+)\)/g, (match, label, href) =>
    /^(?:https?:|mailto:)/i.test(href) || !/^[a-z][a-z0-9+.-]*:/i.test(href)
      ? `<a href="${href}">${label}</a>` : label);
  out = out.replace(/\*\*([^*]+)\*\*/g, "<strong>$1</strong>");
  out = out.replace(/`([^`]+)`/g, "<code>$1</code>");
  return out;
}

function listBlock(lines, start) {
  const items = [];
  let end = start;
  for (; end < lines.length; end++) {
    const match = /^(\s*)([-*]|\d+\.)\s+(.+)$/.exec(lines[end]);
    if (!match) break;
    items.push({ indent: match[1].length, ordered: /\d/.test(match[2]), start: parseInt(match[2], 10), text: match[3] });
  }
  let index = 0;
  function level(indent) {
    let result = '';
    while (index < items.length && items[index].indent === indent) {
      const ordered = items[index].ordered;
      const tag = ordered ? 'ol' : 'ul';
      const start = ordered && items[index].start !== 1 ? ` start="${items[index].start}"` : '';
      result += `<${tag}${start}>`;
      while (index < items.length && items[index].indent === indent && items[index].ordered === ordered) {
        result += `<li>${inline(items[index++].text)}`;
        if (index < items.length && items[index].indent > indent) result += level(items[index].indent);
        result += '</li>';
      }
      result += `</${tag}>`;
    }
    return result;
  }
  return { html: level(items[0].indent), end };
}

function markdownBody(md) {
  const lines = md.replace(/\r/g, "").split("\n");
  const out = [];
  let i = 0;
  while (i < lines.length) {
    const line = lines[i];
    if (!line.trim()) { i++; continue; }
    if (line.startsWith("```")) {
      const lang = line.slice(3).trim(); const block = []; i++;
      while (i < lines.length && !lines[i].startsWith("```")) block.push(lines[i++]);
      i++;
      out.push(`<pre data-language="${esc(lang)}"><code>${esc(block.join("\n"))}</code></pre>`); continue;
    }
    const heading = /^(#{1,4})\s+(.+)$/.exec(line);
    if (heading) { const level = Math.min(heading[1].length, 4); out.push(`<h${level}>${inline(heading[2])}</h${level}>`); i++; continue; }
    if (line.startsWith("> ")) { out.push(`<blockquote>${inline(line.slice(2))}</blockquote>`); i++; continue; }
    if (line.includes("|") && i + 1 < lines.length && /^\s*\|?\s*:?-+/.test(lines[i + 1])) {
      const rows = []; const split = x => x.trim().replace(/^\||\|$/g, "").split("|").map(v => v.trim());
      const headers = split(line); i += 2;
      while (i < lines.length && lines[i].includes("|") && lines[i].trim()) rows.push(split(lines[i++]));
      out.push(`<div class="table-wrap"><table><thead><tr>${headers.map(h => `<th>${inline(h)}</th>`).join("")}</tr></thead><tbody>${rows.map(r => `<tr>${headers.map((_, n) => `<td>${inline(r[n] ?? "")}</td>`).join("")}</tr>`).join("")}</tbody></table></div>`); continue;
    }
    if (/^\s*(?:[-*]|\d+\.)\s+/.test(line)) {
      const list = listBlock(lines, i);
      out.push(list.html); i = list.end; continue;
    }
    const p = [line]; i++;
    while (i < lines.length && lines[i].trim() && !/^(?:#{1,4}\s|>|```|\s*(?:[-*]|\d+\.)\s+)/.test(lines[i]) && !(lines[i].includes("|") && i + 1 < lines.length && /^\s*\|?\s*:?-+/.test(lines[i + 1]))) p.push(lines[i++]);
    out.push(`<p>${inline(p.join(" "))}</p>`);
  }
  return out.join("\n");
}

function htmlDocument(title, main, documentType = "Dokument") {
  return `<!doctype html>
<html lang="pl"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>${esc(title)} — DRAFT</title><style>
:root{--ink:#17202a;--muted:#5f6b76;--line:#d7dde3;--accent:#245b78;--paper:#fff;--warn:#8b2f2f}*{box-sizing:border-box}body{margin:0;background:#eef1f4;color:var(--ink);font:15px/1.55 system-ui,-apple-system,"Segoe UI",Arial,sans-serif}.sheet{width:min(210mm,100%);min-height:297mm;margin:18px auto;padding:16mm 17mm;background:var(--paper);box-shadow:0 6px 26px #1a2a3617}.docbar{display:flex;justify-content:space-between;gap:12px;border-bottom:2px solid var(--accent);padding-bottom:8px;color:var(--muted);font-size:12px}.draft{color:var(--warn);font-weight:800;letter-spacing:.08em}h1{font-size:25px;line-height:1.2;margin:20px 0 14px;color:#123c54}h2{font-size:18px;margin:25px 0 8px;color:#184e68;border-bottom:1px solid var(--line);padding-bottom:4px}h3{font-size:15px;margin:18px 0 6px}p{margin:7px 0}a{color:#145f84;overflow-wrap:anywhere}blockquote{margin:12px 0;padding:10px 12px;border-left:4px solid #d69b2d;background:#fff8e8}.table-wrap{overflow-x:auto;margin:10px 0 16px}table{border-collapse:collapse;width:100%;font-size:12.5px}th,td{border:1px solid var(--line);padding:6px 7px;text-align:left;vertical-align:top}th{background:#edf4f7}ul,ol{padding-left:22px}li{margin:3px 0}code,pre{font-family:ui-monospace,Consolas,monospace}pre{white-space:pre-wrap;border:1px solid var(--line);background:#f7f9fa;padding:10px}.footer{margin-top:28px;border-top:1px solid var(--line);padding-top:7px;color:var(--muted);font-size:11px}.amount{font-size:23px;font-weight:800;color:#123c54}.notice{border:2px solid #b56b20;background:#fff8e8;padding:12px;margin:12px 0}.signature-grid{display:grid;grid-template-columns:1fr 1fr;gap:25px;margin-top:32px}.signature{border-top:1px solid #444;padding-top:6px;margin-top:36px}@media(max-width:720px){.sheet{margin:0;padding:22px 16px;min-height:0}.docbar,.signature-grid{grid-template-columns:1fr;display:grid}.table-wrap{margin-right:-8px}h1{font-size:22px}}@media print{@page{size:A4;margin:18mm}body{background:#fff}.sheet{width:auto;min-height:0;margin:0;padding:0;box-shadow:none}.table-wrap{overflow:visible}a{color:inherit;text-decoration:none}h2,table,blockquote,pre,.notice{break-inside:avoid}h1,h2,h3,h4{break-after:avoid}p,li{orphans:3;widows:3}.docbar{position:static}}
</style></head><body><main class="sheet"><header class="docbar"><span>GF · ${esc(documentType)}</span><span>Wersja: 0.2.0-draft · <span class="draft">DRAFT</span></span></header>${main}<footer class="footer">Projekt do profesjonalnej weryfikacji. Data obowiązywania: [DO WYPEŁNIENIA] · Właściciel: [DO WYPEŁNIENIA] · Recenzent: [DO WYPEŁNIENIA]</footer></main></body></html>`;
}


const derivatives = [
  ["01-data-processing-agreement/umowa-powierzenia-przetwarzania-danych-pl.md", "Umowa powierzenia"],
  ["02-privacy/polityka-prywatnosci-pl.md", "Polityka prywatności"],
  ["03-cookies-and-storage/polityka-cookies-pl.md", "Polityka pamięci"],
  ["06-client-launch/protokol-uruchomienia-platformy.md", "Protokół uruchomienia"],
];
for (const [source, type] of derivatives) {
  const md = await readFile(join(packageRoot, source), "utf8");
  const title = /^# (.+)$/m.exec(md)[1];
  await output(join(packageRoot, source.replace(/\.md$/, ".html")), htmlDocument(title, markdownBody(md), type));
}
const publicSources = [
  ["docs/legal/electronic-services-regulations/regulamin-swiadczenia-uslug-elektronicznych-pl.md", "regulamin"],
  ["docs/client-launch-package/02-privacy/polityka-prywatnosci-pl.md", "polityka-prywatnosci"],
  ["docs/client-launch-package/03-cookies-and-storage/polityka-cookies-pl.md", "pliki-cookies"],
];
for (const [source, name] of publicSources) {
  const md = await readFile(join(repoRoot, source), "utf8");
  const destination = join(repoRoot, `FrontEnd/public/legal/${name}.html`);
  const old = await readFile(destination, "utf8");
  const title = /^# (.+)$/m.exec(md)[1];
  // Exclude internal package metadata, retain all substantive sections and draft status.
  const start = md.indexOf("\n## ");
  if (start < 0) throw new Error(`No substantive sections in ${source}`);
  const main = `<p class="draft">WERSJA ROBOCZA — DO WERYFIKACJI PRAWNEJ</p><h1>${esc(title)}</h1><p>Wersja: <strong>0.2.0-draft</strong> · Obowiązuje od: [OWNER_REQUIRED: data po zatwierdzeniu]</p><p>Operator: [OWNER_REQUIRED: zweryfikowana tożsamość i kontakt]</p>${markdownBody(md.slice(start))}`;
  await output(destination, old.replace(/<main id="tresc">[\s\S]*?<\/main>/, () => `<main id="tresc">${main}</main>`));
}
async function filesAt(directory) {
  const result = [];
  for (const entry of (await readdir(directory, { withFileTypes: true })).sort((a,b) => a.name.localeCompare(b.name))) {
    const full = join(directory, entry.name);
    if (entry.isDirectory()) result.push(...await filesAt(full));
    else if (/\.(md|html|csv)$/i.test(entry.name)) result.push(full);
  }
  return result;
}
const classes = {
  OWNER_REQUIRED: ["Właściciel Operatora", "Tożsamość, decyzja lub fakt biznesowy nie wynika z kodu", "Dokumenty rejestrowe, decyzja właściciela i sprawdzony kontakt", "podpis/publikacja, gdy pole dotyczy warunku usługi"],
  CLIENT_REQUIRED: ["Upoważniony Klient", "Dane osoby/organizacji, instrukcja lub rzeczywisty przebieg sprawy są zewnętrzne", "Karta Klienta, instrukcja i dowód w chronionym archiwum poza Git", "konkretne wdrożenie/sprawę; pusty formularz nie blokuje samego przeglądu"],
  ACCOUNTANT_REQUIRED: ["Księgowy z właścicielem", "Obowiązek zależy od całości działalności i statusu podatkowego", "Aktualne przepisy, ewidencja i pisemna kwalifikacja", "rozliczenie i podpis warunków finansowych"],
  LAWYER_REQUIRED: ["Prawnik / specjalista RODO", "Kwalifikacja prawna wymaga aktualnego prawa i okoliczności, nie wynika z kodu", "Opinia dla konkretnego modelu i potwierdzony aktualny tekst aktu", "zatwierdzenie i wydanie w ocenianym zakresie"],
  INFRASTRUCTURE_REQUIRED: ["Operator infrastruktury", "Repozytorium nie dowodzi dostawcy, regionu lub konfiguracji produkcji", "Panel usługi, umowa, konfiguracja i chroniony dowód", "produkcja/powierzenie przed ustaleniem faktów"],
  PRODUCTION_TEST_REQUIRED: ["Właściciel techniczny / tester", "Mechanizm kodu nie dowodzi skuteczności rzeczywistej instancji", "Kontrolowany test syntetyczny z wersją, datą i wynikiem poza Git", "produkcja i twierdzenie o skuteczności kontroli"],
};
function classify(value, context) {
  const explicit = Object.keys(classes).find(c => value.includes(c));
  if (explicit) return explicit;
  const text = `${value} ${context}`.toLocaleLowerCase("pl");
  if (/vat|ksef|księgow|podatkow|nip.*faktur|pit\/zus/.test(text)) return "ACCOUNTANT_REQUIRED";
  if (/test|wynik|dowód testu|odtworz|restore|rpo|rto/.test(text)) return "PRODUCTION_TEST_REQUIRED";
  if (/hosting|smtp|region|dostawc|podproces|podprzetwarz|szyfrow|infrastrukt|transfer/.test(text)) return "INFRASTRUCTURE_REQUIRED";
  if (/prawn|rodo|iod|dpia|podstaw|odpowiedzialnoś|profilowan/.test(text)) return "LAWYER_REQUIRED";
  if (/klient|pracodawc|pracownik|zgłoszeni|sprawa|incydent|podpis|data przekazania/.test(text)) return "CLIENT_REQUIRED";
  return "OWNER_REQUIRED";
}
const registry = join(packageRoot, "00-master-index/rejestr-placeholderow.md");
const report = join(repoRoot, "docs/legal/LEGAL_COMPLETION_REPORT_2026-10-19.md");
const rows = [];
const escapeCell = x => String(x).replaceAll("|", "&#124;").replaceAll("\n", " ");
for (const file of [...await filesAt(join(repoRoot, "docs/legal")), ...await filesAt(packageRoot), ...await filesAt(join(repoRoot, "docs/commercial-offer")), ...await filesAt(join(repoRoot, "FrontEnd/public/legal"))]) {
  if (file === registry || file === report) continue;
  const source = await readFile(file, "utf8");
  const pattern = /\[(?:DO (?:WYPEŁNIENIA|UZUPEŁNIENIA|WERYFIKACJI|ZATWIERDZENIA)[^\]\n]*|(?:OWNER|CLIENT|ACCOUNTANT|LAWYER|INFRASTRUCTURE|PRODUCTION_TEST)_REQUIRED[^\]\n]*)\]|(?<!\[)DO UZUPEŁNIENIA(?!\])/g;
  for (const match of source.matchAll(pattern)) {
    const before = source.slice(0, match.index);
    const line = before.split("\n").length;
    const heading = [...before.matchAll(/^(#{1,6})\s+(.+)$/gm)].at(-1)?.[2] ?? [...before.matchAll(/<h[1-6][^>]*>(.*?)<\/h[1-6]>/g)].at(-1)?.[1]?.replace(/<[^>]+>/g, "") ?? "Metadane / formularz";
    const context = source.slice(Math.max(0, match.index - 200), match.index + match[0].length + 100);
    const category = classify(match[0], context);
    rows.push([relative(repoRoot,file).replaceAll("\\","/"), line, heading, match[0], category, ...classes[category]]);
  }
}
let md = "# Rejestr otwartych pól — 2026-10-19\n\nWersja: 0.2.0-draft. WERSJA ROBOCZA — DO WERYFIKACJI PRAWNEJ.\n\nKażdy wiersz identyfikuje plik, linię, sekcję i brakującą informację. Klasyfikacja wstępna wymaga potwierdzenia odpowiedzialnego; podpisy i dane realnych spraw wypełnia się wyłącznie poza Git. Puste pola protokołów oznaczają zdarzenia przyszłe, nie brak merytorycznych klauzul. Powtórzenia HTML są pochodnymi Markdown. Pole w historycznym wzorze nie tworzy odrębnego zobowiązania. Status każdego wiersza OPEN; nie wpisano fikcyjnych dowodów.\n\n| ID | Plik | Linia | Sekcja | Brak / pole | Klasa | Odpowiedzialny | Dlaczego kod nie wystarcza | Metoda | Wpływ / blokada |\n|---|---|---:|---|---|---|---|---|---|---|\n";
md += rows.map((r,i) => `| F-${String(i+1).padStart(4,"0")} | ${[r[0],...r.slice(1)].map(escapeCell).join(" | ")} |`).join("\n") + "\n";
await output(registry, md);
console.log(`${check ? 'Checked' : 'Rendered'} ${derivatives.length} private HTML and ${publicSources.length} public HTML; inventoried ${rows.length} unresolved occurrences. Markdown sources preserved.`);
