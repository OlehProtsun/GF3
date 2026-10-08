import { readFileSync, existsSync } from 'node:fs';
import { resolve, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { createHash } from 'node:crypto';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const names = ['index', 'regulamin', 'polityka-prywatnosci', 'pliki-cookies', 'zasady-korzystania', 'podwykonawcy', 'bezpieczenstwo'];
const release = process.argv.includes('--release');
const errors = [];
const reviewPath = resolve(root, 'docs/legal/LEGAL_REVIEW.md');
const review = existsSync(reviewPath) ? readFileSync(reviewPath, 'utf8') : '';
if (!review) errors.push('docs/legal/LEGAL_REVIEW.md: missing');
for (const name of [...names.map(n => `${n}.html`), 'legal.css']) {
  const relative = `FrontEnd/public/legal/${name}`;
  const path = resolve(root, relative);
  if (!existsSync(path)) { errors.push(`${relative}: missing`); continue; }
  const bytes = readFileSync(path);
  let source;
  try { source = new TextDecoder('utf-8', { fatal: true }).decode(bytes); }
  catch { errors.push(`${relative}: invalid UTF-8`); continue; }
  if (release && /DO UZUPEŁNIENIA|\[PLACEHOLDER\]|0\.1\.0-draft|WERSJA ROBOCZA/i.test(source)) {
    errors.push(`${relative}: draft or placeholder`);
  }
  if (!name.endsWith('.html')) continue;
  for (const [label, pattern] of [
    ['lang', /<html\b[^>]*lang="pl"/i], ['charset', /<meta\b[^>]*charset="utf-8"/i],
    ['title', /<title>[^<]+<\/title>/i], ['viewport', /<meta\b[^>]*name="viewport"/i],
    ['version', /Wersja:\s*(?:<[^>]+>)*\d+\.\d+\.\d+(?:-[a-z0-9.-]+)?/i],
    ['effective date', /Obowiązuje od:/], ['hub link', /href="\/legal\/index\.html"/],
    ['login link', /href="\/login"/],
  ]) if (!pattern.test(source)) errors.push(`${relative}: missing ${label}`);
  for (const [, href] of source.matchAll(/href="([^"]+)"/g)) {
    if (/^(?:https?:|mailto:)/i.test(href) || href === '/login') continue;
    const [target, fragment] = href.split('#');
    const targetPath = target.startsWith('/') ? resolve(root, 'FrontEnd/public', target.slice(1)) : resolve(dirname(path), target || name);
    if (!existsSync(targetPath)) errors.push(`${relative}: broken link`);
    else if (fragment && !readFileSync(targetPath, 'utf8').includes(`id="${fragment}"`)) errors.push(`${relative}: broken fragment`);
  }
  if (release) {
    const version = /Wersja:\s*(?:<[^>]+>)*(\d+\.\d+\.\d+(?:-[a-z0-9.-]+)?)/i.exec(source)?.[1];
    const hash = createHash('sha256').update(bytes).digest('hex');
    const rows = review.split(/\r?\n/).filter(line => line.startsWith('|')).map(line => line.split('|').slice(1, -1).map(cell => cell.trim()));
    const row = rows.find(cells => cells[0] === relative);
    if (!row || row[1] !== 'APPROVED' || row[4] !== version || row[5] !== hash ||
        [row[2], row[3], row[6], row[7], row[8]].some(cell => !cell || /DO UZUPEŁNIENIA|DRAFT/i.test(cell))) {
      errors.push(`${relative}: missing matching genuine approval/version/hash`);
    }
  }
}
if (errors.length) { errors.forEach(error => console.error(error)); process.exitCode = 1; }
else console.log(`Legal documents: ${release ? 'release' : 'draft'} validation passed (7 pages).`);
