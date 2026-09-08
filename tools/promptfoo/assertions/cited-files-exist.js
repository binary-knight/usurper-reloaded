// Every repository place the answer cites must exist: a path exists, a bare file name (GameConfig.cs)
// is found somewhere under the source trees, and a `File.cs:123` cite points inside the file. This
// automates the first pass of the council practice: treat a model's output as leads to verify
// against the tree. An answer that cites no file at all fails too; a ruling about this code names
// the code.
'use strict';
const fs = require('fs');
const path = require('path');

const repoRoot = path.resolve(__dirname, '..', '..', '..');
const searchRoots = ['Scripts', 'Tests', 'DOCS', 'Localization', 'web', 'tools', '.github'];
const pathRe = /(?:\b(?:Scripts|DOCS|Tests|Localization|web|tools)|(?<![\w.])\.github)\/[A-Za-z0-9_./-]*[A-Za-z0-9_]\.(?:cs|md|json|txt|js|yml|yaml|html|py|csproj)(?::(\d+))?\b/g;
const bareRe = /\b([A-Z][A-Za-z0-9_.]*\.(?:cs|csproj))(?::(\d+))?\b/g;

let index = null;
function basenameIndex() {
  if (index) return index;
  index = new Map();
  const walk = (dir) => {
    let entries = [];
    try { entries = fs.readdirSync(dir, { withFileTypes: true }); } catch { return; }
    for (const e of entries) {
      if (e.name === 'node_modules' || e.name === 'bin' || e.name === 'obj') continue;
      const full = path.join(dir, e.name);
      if (e.isDirectory()) walk(full);
      else if (!index.has(e.name)) index.set(e.name, full);
    }
  };
  for (const r of searchRoots) walk(path.join(repoRoot, r));
  return index;
}

function lineCount(file) {
  try {
    const lines = fs.readFileSync(file, 'utf8').split('\n');
    return lines.length - (lines[lines.length - 1] === '' ? 1 : 0);
  } catch { return 0; }
}

module.exports = (output) => {
  const cites = new Map(); // key -> { file, line }
  let m;
  while ((m = pathRe.exec(output)) !== null) {
    const p = m[0].replace(/:\d+$/, '');
    cites.set(m[0], { file: path.join(repoRoot, p), line: m[1] ? Number(m[1]) : 0 });
  }
  while ((m = bareRe.exec(output)) !== null) {
    if ([...cites.keys()].some((k) => k.endsWith(m[1]) || k.includes(m[1] + ':'))) continue;
    const found = basenameIndex().get(m[1]);
    cites.set(m[0], { file: found || path.join(repoRoot, m[1]), line: m[2] ? Number(m[2]) : 0 });
  }
  if (cites.size === 0) return { pass: false, score: 0, reason: 'no repository file cited' };
  const missing = [];
  for (const [cite, { file, line }] of cites) {
    if (!fs.existsSync(file)) missing.push(cite);
    else if (line > 0 && lineCount(file) < line) missing.push(`${cite} (file has ${lineCount(file)} lines)`);
  }
  if (missing.length > 0) {
    return { pass: false, score: 1 - missing.length / cites.size, reason: `cited but not in the tree: ${missing.join(', ')}` };
  }
  return { pass: true, score: 1, reason: `${cites.size} cite(s) all exist` };
};
