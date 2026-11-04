import { readFile, writeFile, access } from 'node:fs/promises';
import { dirname, resolve, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { homedir } from 'node:os';
import { spawnSync } from 'node:child_process';

const directory = dirname(fileURLToPath(import.meta.url));
const stem = 'regulamin-korzystania-z-gf-dla-pracownikow-pl';
const args = process.argv.slice(2);
if (args.some(arg => !arg.startsWith('--base-url=') && arg !== '--check')) throw new Error('Options: --check, --base-url=https://verified-host');
const check = args.includes('--check');
const baseArg = args.find(arg => arg.startsWith('--base-url='))?.slice(11);
let baseUrl = null;
if (baseArg) {
  const url = new URL(baseArg);
  if (url.protocol !== 'https:' || url.username || url.password || url.search || url.hash || url.pathname !== '/') {
    throw new Error('base-url must be the verified HTTPS origin without credentials, path, query or fragment.');
  }
  baseUrl = url.origin;
}
const markdown = (await readFile(join(directory, `${stem}.md`), 'utf8')).replace(/\r\n/g, '\n');
const headings = [...markdown.matchAll(/^## (§ (\d+)\. .+)$/gm)];
if (headings.length !== 14 || headings.some((match, index) => Number(match[2]) !== index + 1)) throw new Error('Expected consecutive §§ 1–14.');
const title = /^# (.+)$/m.exec(markdown)?.[1];
const version = /^Wersja: (.+)$/m.exec(markdown)?.[1];
const status = /^Status: (.+)$/m.exec(markdown)?.[1];
if (!title || !version || !status || title.length > 160 || version.length > 80) throw new Error('Missing/oversize title, version or status.');
if (!status.startsWith('DRAFT') && /\[DO (UZUPEŁNIENIA|ZATWIERDZENIA):/.test(markdown)) throw new Error('A non-draft source must not contain unresolved review fields.');
const logo = await readFile(resolve(directory, '../../../FrontEnd/public/icons/gf-icon-192.png'));
const localLinks = {
  '/legal/index.html': '../README.md',
  '/legal/regulamin.html': '../electronic-services-regulations/regulamin-swiadczenia-uslug-elektronicznych-pl.html',
  '/legal/polityka-prywatnosci.html': '../../client-launch-package/02-privacy/polityka-prywatnosci-pl.html',
  '/legal/pliki-cookies.html': '../../client-launch-package/03-cookies-and-storage/polityka-cookies-pl.html',
};
const escape = value => value.replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;').replaceAll('"', '&quot;');
function inline(value) {
  return escape(value).replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>').replace(/\[([^\]]+)\]\(([^)]+)\)/g, (_, label, href) => {
    const target = href.startsWith('/legal/') ? baseUrl ? baseUrl + href : localLinks[href] : href;
    if (!target || /^(?!https?:|mailto:)[a-z][a-z0-9+.-]*:/i.test(target)) throw new Error(`Unsafe link ${href}`);
    return `<a href="${target}"${href.startsWith('/legal/') ? ` data-route="${href}"` : ''}>${label}</a>`;
  });
}
function blocks(source) {
  return source.trim().split(/\n\s*\n|\n(?=\d+\. )/).map(block => {
    if (/^# /.test(block)) return `<h1>${inline(block.slice(2))}</h1>`;
    if (/^## /.test(block)) {
      const number = /^## § (\d+)/.exec(block)[1];
      return `<h2 id="paragraf-${number}">${inline(block.slice(3))}</h2>`;
    }
    const numbered = /^(\d+)\. ([\s\S]+)$/.exec(block);
    if (numbered) return `<p class="clause"><span class="number">${numbered[1]}.</span>${inline(numbered[2])}</p>`;
    const meta = /^(Wersja|Status|Obowiązuje od|Operator|Adres GF):/.test(block);
    return `<p${meta ? ' class="meta"' : block.startsWith('Dokument do przeglądu.') ? ' class="review-note"' : ''}>${inline(block)}</p>`;
  }).join('\n');
}
const pages = markdown.split(/\n<!-- pagebreak -->\n/);
if (pages.length !== 6) throw new Error('Expected six editorial page groups.');
const html = `<!doctype html>
<html lang="pl"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
<title>${escape(title)} — ${escape(version)}</title>
<meta name="description" content="Regulamin korzystania z GF dla pracowników. ${escape(status)}.">
<style>
:root{color-scheme:light;--ink:#181a20;--muted:#646775;--line:#e1e2e7;--accent:#60519a}*{box-sizing:border-box}body{margin:0;background:#eceef1;color:var(--ink);font:10.5pt/1.4 Arial,Helvetica,sans-serif}a{color:var(--accent);text-underline-offset:3px;overflow-wrap:anywhere}.sheet{position:relative;width:210mm;min-height:297mm;margin:24px auto;padding:24mm 21mm 21mm;background:white;box-shadow:0 4px 24px #1418200b}.running-header{position:absolute;top:10mm;left:21mm;right:21mm;display:flex;align-items:center;gap:9px;padding-bottom:9px;border-bottom:1px solid var(--line);font-size:8pt;color:var(--muted)}.running-header img{width:22px;height:22px}.running-header strong{letter-spacing:.12em;color:var(--ink);font-size:9pt}.running-header .version{margin-left:auto}h1{font-size:23pt;line-height:1.15;letter-spacing:-.025em;max-width:155mm;margin:3mm 0 5mm}h2{font-size:12.5pt;line-height:1.25;margin:5mm 0 2.5mm;color:var(--ink);break-after:avoid}p{margin:0 0 2.1mm;orphans:3;widows:3;overflow-wrap:anywhere}.meta{font-size:8.5pt;color:var(--muted);margin-bottom:1mm}.meta:nth-of-type(2){color:var(--accent);font-weight:bold}.review-note{font-size:8.3pt;line-height:1.35;border-left:2px solid var(--accent);padding-left:10px;margin:4mm 0 5mm;color:#50535f}.clause{position:relative;padding-left:1.45em;text-indent:0}.number{position:absolute;left:0;top:0;display:block;width:1.45em;text-indent:0;font-variant-numeric:tabular-nums;color:var(--muted)}footer{position:absolute;bottom:10mm;left:21mm;right:21mm;border-top:1px solid var(--line);padding-top:8px;display:flex;justify-content:space-between;font-size:7.2pt;color:var(--muted)}.screen-tools{max-width:210mm;margin:20px auto;padding:0 18px;font:14px/1.5 Arial,sans-serif;color:var(--muted)}.screen-tools details{margin-top:8px}.screen-tools ol{columns:2;padding-left:22px}.screen-tools a{color:inherit}
@media(max-width:800px){body{background:white;font-size:15px;line-height:1.65}.sheet{width:100%;min-height:0;margin:0;padding:26px 22px;box-shadow:none;border-top:1px solid var(--line)}.running-header{position:static;margin-bottom:26px;font-size:11px}h1{font-size:30px;margin:0 0 22px}h2{font-size:20px;margin:26px 0 12px}.meta{font-size:12px}.review-note{font-size:12px}p{margin-bottom:14px}footer{position:static;margin-top:26px;font-size:10px}.screen-tools ol{columns:1}.version{font-size:10px}}
@media print{@page{size:A4;margin:0}body{background:white}.screen-tools{display:none}.sheet{margin:0;box-shadow:none;break-after:page}.sheet:last-child{break-after:auto}a{color:inherit}}
</style></head><body>
<nav class="screen-tools" aria-label="Nawigacja dokumentu"><strong>${escape(status)}</strong><details><summary>Spis treści · 14 paragrafów</summary><ol>${headings.map(match => `<li><a href="#paragraf-${match[2]}">${escape(match[1])}</a></li>`).join('')}</ol></details></nav>
${pages.map((page, i) => `<article class="sheet" aria-label="Część ${i + 1}"><header class="running-header"><img src="data:image/png;base64,${logo.toString('base64')}" alt=""><strong>GF</strong><span>Regulamin dla pracowników</span><span class="version">${escape(version)} · PL</span></header>${blocks(page)}<footer><span>${escape(status)}</span><span>${i + 1} / ${pages.length}</span></footer></article>`).join('\n')}
</body></html>
`;
const htmlPath = join(directory, `${stem}.html`);
if (check) {
  if (await readFile(htmlPath, 'utf8') !== html) throw new Error('HTML is stale; regenerate with the same --base-url.');
} else await writeFile(htmlPath, html);

const bundled = join(homedir(), '.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe');
let python = process.env.GF_PDF_PYTHON;
if (!python) { try { await access(bundled); python = bundled; } catch { python = process.platform === 'win32' ? 'python' : 'python3'; } }
const result = spawnSync(python, [join(directory, 'render-pdf.py'), ...(check ? ['--check'] : [])], {
  cwd: directory, encoding: 'utf8', windowsHide: true, env: { ...process.env, PYTHONUTF8: '1' },
});
if (result.error) throw result.error;
process.stdout.write(result.stdout ?? '');
process.stderr.write(result.stderr ?? '');
if (result.status !== 0) process.exitCode = result.status ?? 1;
