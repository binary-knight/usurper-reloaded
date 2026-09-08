// Claude seat: `claude -p` (Claude Code in print mode) with the brief on stdin. CLAUDECODE is
// unset so the CLI runs even when promptfoo itself was started from inside a Claude Code session.
// The seat reads only: the Codex seat runs read-only, and a brief or attachment must not be able
// to make this one edit the tree or run commands, so the writing tools are disallowed.
'use strict';
const { runCli } = require('./run');

class ClaudeProvider {
  constructor(options = {}) {
    this.providerId = options.id || 'claude';
    this.config = Object.assign({ model: 'claude-fable-5-1', disallowedTools: 'Bash,Edit,Write,NotebookEdit' }, options.config || {});
  }

  id() { return this.providerId; }

  async callApi(prompt, context) {
    const env = Object.assign({}, process.env);
    delete env.CLAUDECODE;
    const args = ['-p', '--output-format', 'text', '--model', this.config.model, '--disallowedTools', this.config.disallowedTools];
    const name = context && context.vars ? context.vars.name : undefined;
    const r = await runCli('claude', args, prompt, { name, seat: 'claude', env });
    if (r.code !== 0) return { error: `claude exited ${r.code}: ${r.stderr.slice(-500)} (raw: ${r.rawPath})` };
    return { output: r.stdout.trim(), metadata: { model: this.config.model, raw: r.rawPath } };
  }
}

module.exports = ClaudeProvider;
