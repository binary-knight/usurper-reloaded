// Codex seat: `codex exec` with the brief on stdin, the same invocation the council practice uses.
// The output is the final assistant message; the raw stream (with its model and reasoning-effort
// header) is teed to the evidence directory. Verify the header before trusting a run.
'use strict';
const fs = require('fs');
const os = require('os');
const path = require('path');
const { runCli } = require('./run');

class CodexProvider {
  constructor(options = {}) {
    this.providerId = options.id || 'codex';
    this.config = Object.assign({ model: 'gpt-6-astra', effort: 'high', serviceTier: 'default' }, options.config || {});
  }

  id() { return this.providerId; }

  async callApi(prompt, context) {
    const last = path.join(os.tmpdir(), `pf-codex-last-${process.pid}-${Date.now()}.txt`);
    const args = [
      'exec', '-m', this.config.model,
      '-c', `model_reasoning_effort=${this.config.effort}`,
      '-c', `service_tier=${this.config.serviceTier}`,
      '--sandbox', 'read-only', '--skip-git-repo-check',
      '--output-last-message', last, '-',
    ];
    const name = context && context.vars ? context.vars.name : undefined;
    const r = await runCli('codex', args, prompt, { name, seat: 'codex' });
    const header = /model:\s*(\S+)[\s\S]*?reasoning effort:\s*(\S+)/.exec(r.stdout) || [];
    let output = '';
    try { output = fs.readFileSync(last, 'utf8'); fs.unlinkSync(last); } catch { /* no last message */ }
    if (r.code !== 0 && !output) return { error: `codex exited ${r.code}: ${r.stderr.slice(-500)} (raw: ${r.rawPath})` };
    const tokens = /tokens used\s*:?\s*([\d,]+)/i.exec(r.stdout);
    return {
      output,
      metadata: { model: header[1], effort: header[2], tokensUsed: tokens ? tokens[1] : undefined, raw: r.rawPath },
    };
  }
}

module.exports = CodexProvider;
