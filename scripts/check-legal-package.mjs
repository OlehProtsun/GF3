import { readFile, readdir, access } from 'node:fs/promises';
import { dirname, resolve, extname } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const roots = ['docs/legal', 'docs/client-launch-package', 'docs/commercial-offer', 'FrontEnd/public/legal'];
const errors = [];
async function filesAt(directory) {
  const files = [];
  for (const item of await readdir(directory, { withFileTypes: true })) {
    const path = resolve(directory, item.name);
    if (item.isDirectory()) files.push(...await filesAt(path));
    else if (/\.(md|html)$/i.test(item.name)) files.push(path);
  }
  return files;
}
const slug = value => value.toLowerCase().replace(/<[^>]+>/g, '').replace(/[^\p{L}\p{N}_\s-]/gu, '').replace(/\s/g, '-');
let links = 0;
const files = (await Promise.all(roots.map(path => filesAt(resolve(root, path))))).flat();
for (const file of files) {
  const source = await readFile(file, 'utf8');
  if (source.includes('\uFFFD')) errors.push(`${file}: replacement character`);
  const references = extname(file) === '.html'
    ? [...source.matchAll(/(?:href|src)="([^"<>]+)"/g)].map(match => match[1])
    : [...source.matchAll(/(?<!!)\[[^\]\n]+\]\(([^)\s]+)\)/g)].map(match => match[1]);
  for (const reference of references) {
    if (/^(?:https?:|mailto:|data:|tel:)/i.test(reference) || reference === '/login') continue;
    const [path, fragment] = reference.split('#');
    const target = path.startsWith('/') ? resolve(root, 'FrontEnd/public', path.slice(1)) : resolve(dirname(file), decodeURI(path || file));
    links++;
    try {
      await access(target);
      if (fragment) {
        const targetSource = await readFile(target, 'utf8');
        const anchors = extname(target) === '.html'
          ? [...targetSource.matchAll(/id="([^"]+)"/g)].map(match => match[1])
          : [...targetSource.matchAll(/^#{1,6}\s+(.+)$/gm)].map(match => slug(match[1]));
        if (!anchors.includes(decodeURI(fragment))) errors.push(`${file}: missing fragment ${reference}`);
      }
    } catch { errors.push(`${file}: missing target ${reference}`); }
  }
}
// These assertions were materially false in the old legal package.
const obsolete = /Platforma nie zapisuje obecnie wersji akceptacji|Platforma nie ma potwierdzonego mechanizmu rejestrowania akceptacji|aplikacja nie loguje akceptacji|brak self-registration i logu akceptacji|brak potwierdzonego.*service worker/i;
for (const file of files.filter(file => !file.endsWith('rejestr-placeholderow.md'))) {
  if (obsolete.test(await readFile(file, 'utf8'))) errors.push(`${file}: obsolete functional assertion`);
}
for (const name of ['regulamin', 'polityka-prywatnosci', 'pliki-cookies']) {
  const source = await readFile(resolve(root, `FrontEnd/public/legal/${name}.html`), 'utf8');
  if (/href="[^"\n]*(?:docs\/|client-launch-package|saas-agreement|internal\/)/i.test(source)) errors.push(`${name}: private document link in public page`);
}
if (errors.length) {
  errors.forEach(error => console.error(error));
  process.exitCode = 1;
} else console.log(`Legal package: ${files.length} MD/HTML files, ${links} local links/fragments checked; no stale assertions or private public-page links detected.`);
