// The brief's numbered questions (the last numbered run in the brief; earlier runs are findings)
// must each be answered under their own number. An answer marker is a heading, a bold run, or an
// unindented numbered line, in any of the forms the seats use: "1.", "1)", "Q1:", "**Q1 — RULING:**",
// "## 1 The hour". A numbered reason list nested inside another answer is indented and does not count.
'use strict';

function questionNumbers(brief) {
  const runs = [];
  let run = [];
  for (const line of String(brief || '').split('\n')) {
    const m = /^\s*(\d+)\.\s+\S/.exec(line);
    if (m) {
      const n = Number(m[1]);
      if (run.length === 0 || n === run[run.length - 1] + 1) run.push(n);
      else { runs.push(run); run = [n]; }
    } else if (run.length > 0 && /^\s*$/.test(line)) {
      // blank lines inside a list are fine
    } else if (run.length > 0 && /^#/.test(line)) {
      runs.push(run); run = [];
    }
  }
  if (run.length > 0) runs.push(run);
  return runs.length > 0 ? runs[runs.length - 1] : [];
}

const marker = /^(?:#+\s*|\*\*\s*|>\s*)?(?:Q|Question\s*|Decision\s*|Ruling\s*)?(\d+)\s*(?:[.):]|[—–-]|\b)/i;

function answeredNumbers(output) {
  const answered = new Set();
  for (const line of String(output || '').split('\n')) {
    const heading = /^#/.test(line);
    const bold = /^\s*\*\*/.test(line);
    const unindented = /^\S/.test(line);
    if (!heading && !bold && !unindented) continue;
    const m = marker.exec(line.replace(/^\s+/, ''));
    if (m) answered.add(Number(m[1]));
  }
  return answered;
}

module.exports = (output, context) => {
  const brief = context && context.vars ? context.vars.brief : '';
  const questions = questionNumbers(brief);
  if (questions.length === 0) return { pass: true, score: 1, reason: 'brief has no numbered questions' };
  const answered = answeredNumbers(output);
  const missing = questions.filter((q) => !answered.has(q));
  if (missing.length > 0) {
    return { pass: false, score: 1 - missing.length / questions.length, reason: `questions without a numbered answer: ${missing.join(', ')}` };
  }
  return { pass: true, score: 1, reason: `all ${questions.length} questions answered under their number` };
};

module.exports.questionNumbers = questionNumbers;
module.exports.answeredNumbers = answeredNumbers;
