# promptfoo for the council briefs

The game itself calls no model. What does are the design consultations: the
briefs sent to Codex and Claude when the council settles a plan. This directory
runs those briefs through both seats with [promptfoo](https://www.promptfoo.dev)
and checks the answers for the things a usable ruling has.

A run is not a council seat. The tally rule stays majority of four with the
code breaking ties. What a run tells you is whether a brief, as written,
produces an answer worth reading from each model, and it does the first pass
of "treat the output as leads to verify against the tree".

## Setup

Node 20 or newer, and the two CLIs logged in on this machine: `codex` (the
OpenAI Codex CLI, `codex exec` with the ChatGPT login) and `claude` (Claude
Code). No API keys are used or needed.

```
cd tools/promptfoo
npm install
npm run ping      # both seats answer PONG; seconds
npm run eval      # the briefs; minutes per case per seat
npm run view      # browse results in the local viewer
```

promptfoo is pinned in `package.json`. Telemetry is off in every script.
Do not run `promptfoo share`: it uploads the briefs and the answers to a
hosted service, and the briefs are the maintainer's design work. promptfoo
also keeps every eval (prompts and answers) in `~/.promptfoo` on this
machine, which is what `promptfoo view` reads; that store persists outside
the repo, so clear it if the machine changes hands.

## What runs

- `promptfooconfig.yaml`: the cases and the assertions.
- `prompts/council.txt`: the brief, then an optional attachment (the world
  boss decisions brief attaches `DOCS/WORLD_BOSS_PLAN.md`).
- `briefs/`: copies of real briefs from `~/usurper/evidence/codex/`.
- `providers/codex.js`: `codex exec -m gpt-6-astra -c model_reasoning_effort=high`
  with the brief on stdin, the same invocation the council practice uses. The
  output is the final message; the raw stream with its `model:` and
  `reasoning effort:` header is teed to the evidence directory as
  `pf-<case>-codex-<stamp>.txt`. Check the header before trusting a run,
  and check `metadata.sandbox` in the results: it is false when the raw
  stream carries Codex's "needs access to create user namespaces" warning,
  which means the sandbox never started and the seat answered from the
  inlined brief alone, with no tree access. On this machine that has been
  every run so far (AppArmor restricts unprivileged user namespaces); fixing
  it is a system decision for the maintainer, not something a run changes.
- `providers/claude.js`: `claude -p --model claude-fable-5-1` with the brief
  on stdin, raw stream teed the same way, and Bash, Edit, Write and
  NotebookEdit disallowed so the seat reads only, like the Codex seat's
  read-only sandbox.
- `assertions/cited-files-exist.js` (on the design brief case): every
  repository place the answer cites exists: a path, a bare file name found
  under the source trees, or a `File.cs:123` cite inside the file's length.
  An answer that cites nothing fails. A short decisions ballot is not asked
  to cite code, so the check is per case.
- `assertions/answers-every-question.js`: every numbered question in the
  brief (the last numbered list in it) is answered under its own number, in
  any of the seats' heading forms (`1.`, `Q1:`, `**Q1 — RULING:**`). Nested
  numbered lists inside an answer do not count.
- `assertions/quoted-text-in-input.js`: every quotation of five or more
  words appears verbatim in the brief or its attachment; invented quotes
  fail.
- `assertions/numbers-carry-a-class.js`: on both cases (both briefs say a
  number is a starting setting unless derived), every line with a percent, a multiple, or a money figure says
  MEASURED, DERIVED, GUESS, "starting", or cites a file. It catches an
  unlabelled figure, not a mislabelled one.
- Inline: the answer names a dissent, a falsifier, or what would reverse or
  overturn it;
  and a floor of ten numbers (a floor, not a signal). The decisions case
  also has a word budget.

Results land in `results/latest.json` (ignored by git); copy a run you want
to keep next to its raw streams under `~/usurper/evidence/codex/`.

## Known false claims

The class of error no deterministic assertion catches is a fabricated read
of a real source. On 2026-09-08 the Codex seat, with no sandbox, said it had
fetched `Scripts/Core/GameConfig.cs` from the public main branch and that it
"identifies itself as 1.0.3"; the file on main said 1.1.6. The cite was
real and the content was invented. Read a seat's claims about what a file
says against the file.

## What it is not

It is not part of the test gate or the CI pipeline: a model call in the
gate would make every commit nondeterministic, and CI has no logins. It does
not grade with a model (`llm-rubric` needs an API key); every assertion is
deterministic. Add a case by copying a brief into `briefs/` and a test entry
into the config.
