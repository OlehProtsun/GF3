import { readFileSync, existsSync } from 'node:fs';
import { resolve, dirname, isAbsolute, relative, sep } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const pinnedCommit = 'd8e752a8b75c8f1542fdb1d95a80b2ab39e1ad38';

export function validateDesignSystem(snapshot) {
  const errors = [];
  const fail = (id, reason) => {
    const safeId = typeof id === 'string' && id.length <= 160 && /^[\w|/.:\-]+$/.test(id) && !/eyJ/.test(id) ? id : 'snapshot item';
    errors.push(`${safeId}: ${reason}`);
  };
  const array = (value, id) => {
    if (!Array.isArray(value)) { fail(id, 'expected array'); return []; }
    return value;
  };
  const text = value => typeof value === 'string' && value.trim().length > 0;
  const unique = (values, id) => {
    if (new Set(values).size !== values.length) fail(id, 'duplicate references or IDs');
  };
  const index = (values, id) => {
    unique(values.map(item => item?.id), id);
    for (const item of values) if (!text(item?.id)) fail(id, 'missing ID');
    return new Map(values.map(item => [item?.id, item]));
  };
  const source = (value, id) => {
    if (!text(value?.path) || !text(value?.symbolOrSelector)) {
      fail(id, 'invalid source reference'); return;
    }
    const repoPath = relative(root, resolve(root, value.path));
    if (isAbsolute(value.path) || /^[a-z]+:/i.test(value.path) || repoPath === '..' ||
        repoPath.startsWith(`..${sep}`) || isAbsolute(repoPath) || !existsSync(resolve(root, value.path))) {
      fail(id, 'source file does not resolve within repository');
    }
  };
  if (!snapshot || typeof snapshot !== 'object') return ['snapshot: expected object'];
  if (snapshot.schemaVersion !== '1.1.0') fail('schemaVersion', 'expected 1.1.0');
  if (snapshot.project?.commit !== pinnedCommit || snapshot.presentationCapture?.snapshotCommit !== pinnedCommit) {
    fail('project', 'snapshot commit mismatch');
  }
  if (snapshot.project?.runtimeSourceOfTruth !== false) fail('project', 'must be descriptive only');
  const pages = array(snapshot.pages, 'pages');
  const pageIndex = index(pages, 'pages');
  const layouts = index(array(snapshot.layouts, 'layouts'), 'layouts');
  const motion = index(array(snapshot.foundations?.motion, 'motion'), 'motion');
  const bindings = array(snapshot.routeBindings, 'routeBindings');
  const capture = snapshot.presentationCapture;
  for (const field of ['purpose', 'dataPolicy']) if (!text(capture?.[field])) fail('presentationCapture', `invalid ${field}`);
  const scenes = array(capture?.scenes, 'scenes');
  index(scenes, 'scenes');
  const key = item => `${item?.role}|${item?.path}`;
  unique(bindings.map(key), 'routeBindings');
  unique(scenes.map(key), 'scenes');
  if (bindings.length !== scenes.length) fail('scenes', 'route count mismatch');
  const viewports = array(capture?.viewports, 'viewports');
  const viewportIndex = index(viewports, 'viewports');
  for (const viewport of viewports) {
    if (![viewport.width, viewport.height].every(value => Number.isInteger(value) && value > 0) ||
        !Number.isFinite(viewport.pixelRatio) || viewport.pixelRatio <= 0) fail(viewport.id, 'invalid viewport dimensions');
  }
  const privilegedKeys = new Set(['manager|/information', 'manager|/database']);
  const privilegedPages = new Set(bindings.filter(item => privilegedKeys.has(key(item))).map(item => item.pageRef));
  if (privilegedPages.size !== 2 || bindings.filter(item => privilegedKeys.has(key(item))).length !== 2) {
    fail('routeBindings', 'missing privileged routes');
  }
  for (const page of pages) {
    if (!layouts.has(page.layoutRef)) fail(page.id, 'unknown layoutRef');
    if ((page.requiresSystemManager === true) !== privilegedPages.has(page.id)) fail(page.id, 'incorrect privilege metadata');
    source(page.source, page.id);
    for (const style of array(page.styleSources, page.id)) source(style, page.id);
    if (page.motion?.sourceRefs) {
      for (const item of array(page.motion.sourceRefs, page.id)) source(item, page.id);
    }
    index(array(page.controls, page.id), page.id);
    index(array(page.dialogs, page.id), page.id);
  }
  const secretKey = /^(?:password|access[_-]?token|refresh[_-]?token|api[_-]?key|signing[_-]?key|client[_-]?secret|authorization|credentials|rawSql|sqlContents)$/i;
  const secretValue = /(?:Bearer\s+[a-z0-9._-]+|eyJ[a-z0-9_-]+\.[a-z0-9_-]+\.[a-z0-9_-]+|(?:password|token|api[_-]?key|secret)\s*[:=]\s*\S+|[a-z0-9._%+-]+@[a-z0-9.-]+\.[a-z]{2,}|\b(?:SELECT\s+.+\s+FROM|INSERT\s+INTO|UPDATE\s+.+\s+SET|DELETE\s+FROM)\b)/i;
  const privacy = (value, id) => {
    if (typeof value === 'string' && secretValue.test(value)) fail(id, 'sensitive value pattern');
    else if (Array.isArray(value)) value.forEach(item => privacy(item, id));
    else if (value && typeof value === 'object') {
      for (const [field, item] of Object.entries(value)) {
        if (secretKey.test(field)) fail(id, 'sensitive key pattern');
        privacy(item, id);
      }
    }
  };
  bindings.forEach((binding, position) => {
    const id = key(binding);
    if (!['public', 'manager', 'employee'].includes(binding.role) || !text(binding.path)) fail('routeBindings', 'invalid route');
    if (!pageIndex.has(binding.pageRef)) fail(id, 'unknown pageRef');
    if ((binding.requiresSystemManager === true) !== privilegedKeys.has(id)) fail(id, 'incorrect privilege metadata');
    const scene = scenes[position];
    if (!scene || key(scene) !== id || scene.pageRef !== binding.pageRef) { fail(id, 'scene missing or out of order'); return; }
    const page = pageIndex.get(scene.pageRef);
    if (!page) return;
    if (typeof scene.requiresSystemManager !== 'boolean' || scene.requiresSystemManager !== privilegedKeys.has(id)) fail(id, 'incorrect scene privilege');
    if (!layouts.has(scene.layoutRef) || scene.layoutRef !== page.layoutRef) fail(id, 'unknown or mismatched layoutRef');
    const states = new Set([...array(page.states, page.id), ...page.dialogs.flatMap(dialog => array(dialog.states, page.id))]);
    if (scene.initialState !== 'default' && !states.has(scene.initialState)) fail(id, 'unknown initialState');
    const allowed = {
      controlRefs: new Set(page.controls.map(control => control.id)),
      dialogRefs: new Set(page.dialogs.map(dialog => dialog.id)),
      motionRefs: new Set(motion.keys()),
      viewportRefs: new Set(viewportIndex.keys()),
      captureStates: states,
    };
    for (const [field, options] of Object.entries(allowed)) {
      const refs = array(scene[field], id);
      unique(refs, id);
      if (refs.some(item => !text(item) || !options.has(item))) fail(id, `unresolved ${field}`);
      if (['controlRefs', 'dialogRefs'].includes(field) && refs.length !== options.size) fail(id, `incomplete ${field}`);
      if (field === 'viewportRefs' && (refs.length !== 2 || !refs.includes('desktop') || !refs.includes('mobile'))) {
        fail(id, 'both presentation viewport targets required');
      }
    }
    const needs = array(scene.fixtureNeeds, id);
    if (!needs.length || needs.some(item => !text(item) || item.length < 8)) fail(id, 'fixture descriptions required');
    for (const parameter of binding.path.matchAll(/:(\w+)/g)) {
      if (!needs.some(item => typeof item === 'string' && item.includes(parameter[1]))) fail(id, 'missing parameter fixture');
    }
    if (scene.requiresSystemManager && !needs.some(item => /synthetic system-manager account/i.test(item))) fail(id, 'missing authorized synthetic system account');
    const sources = array(scene.sourceRefs, id);
    if (!sources.length) fail(id, 'sourceRefs required');
    unique(sources.map(item => `${item?.path}|${item?.symbolOrSelector}`), id);
    for (const item of sources) source(item, id);
    if (scene.pageMotionRef !== undefined && scene.pageMotionRef !== `pages.${page.id}.motion`) fail(id, 'unknown pageMotionRef');
    if (scene.pageMotionRef && !page.motion) fail(id, 'missing page motion description');
    privacy(scene, id);
  });
  return errors;
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  try {
    const snapshot = JSON.parse(readFileSync(resolve(root, 'docs/design-system.json'), 'utf8'));
    const errors = validateDesignSystem(snapshot);
    if (errors.length) { errors.forEach(error => console.error(error)); process.exitCode = 1; }
    else console.log(`Valid snapshot: ${snapshot.routeBindings.length} routes, ${snapshot.presentationCapture.scenes.length} scenes, ${snapshot.pages.length} pages, ${snapshot.components.length} components; ${snapshot.project.commit}`);
  } catch {
    console.error('docs/design-system.json: cannot read or parse snapshot');
    process.exitCode = 1;
  }
}
