// Shared runner for the council's model seats as promptfoo custom providers.
// The prompt goes to the CLI on stdin (a brief can be 76 KB; an argv prompt is fragile), the raw
// stream is teed to the evidence directory so the model/effort header survives per run, and the
// final answer comes back as the provider output.
'use strict';
const { spawn } = require('child_process');
const fs = require('fs');
const os = require('os');
const path = require('path');

const evidenceDir = process.env.USURPER_EVIDENCE || path.join(os.homedir(), 'usurper', 'evidence', 'codex');

function stamp() {
  return new Date().toISOString().replace(/[-:]/g, '').replace(/\..+/, '').replace('T', '-');
}

function safeName(s) {
  return String(s || 'case').replace(/[^A-Za-z0-9_.-]+/g, '-').slice(0, 60);
}

/**
 * Run a CLI with the prompt on stdin. Returns { code, stdout, stderr, rawPath }.
 * @param {string} cmd
 * @param {string[]} args
 * @param {string} prompt
 * @param {{ name?: string, seat: string, env?: NodeJS.ProcessEnv, timeoutMs?: number }} opts
 */
function runCli(cmd, args, prompt, opts) {
  fs.mkdirSync(evidenceDir, { recursive: true });
  const rawPath = path.join(evidenceDir, `pf-${safeName(opts.name)}-${opts.seat}-${stamp()}.txt`);
  // promptfoo depends on an older @openai/codex whose `codex` shim sits in node_modules/.bin ahead of
  // the installed CLI; the seats must run the user's own CLIs, so strip those entries from PATH.
  const env = Object.assign({}, opts.env || process.env);
  env.PATH = (env.PATH || '').split(path.delimiter).filter((p) => !/node_modules[\\/]\.bin$/.test(p)).join(path.delimiter);
  return new Promise((resolve, reject) => {
    const child = spawn(cmd, args, { env, stdio: ['pipe', 'pipe', 'pipe'] });
    let stdout = '';
    let stderr = '';
    const timer = setTimeout(() => child.kill('SIGTERM'), opts.timeoutMs || 20 * 60 * 1000);
    child.stdout.on('data', (d) => { stdout += d; });
    child.stderr.on('data', (d) => { stderr += d; });
    child.on('error', (err) => { clearTimeout(timer); reject(err); });
    child.on('close', (code) => {
      clearTimeout(timer);
      fs.writeFileSync(rawPath, `# ${cmd} ${args.join(' ')}\n# case=${opts.name || ''} seat=${opts.seat} exit=${code}\n\n${stdout}\n\n# stderr\n${stderr}`);
      resolve({ code, stdout, stderr, rawPath });
    });
    child.stdin.on('error', () => {});
    child.stdin.end(prompt);
  });
}

module.exports = { runCli, evidenceDir };
