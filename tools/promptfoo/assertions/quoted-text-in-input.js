// Every quoted span of five or more words in the answer must appear verbatim (whitespace and case
// folded) in the brief plus its attachment. Catches invented quotations: a seat "quoting" plan text
// that is not there, or an opener like "A player reported ..." that nobody said.
'use strict';

function fold(s) {
  return String(s || '').replace(/[‘’]/g, "'").replace(/[“”]/g, '"').replace(/\s+/g, ' ').toLowerCase();
}

module.exports = (output, context) => {
  const vars = (context && context.vars) || {};
  const input = fold(`${vars.brief || ''}\n${vars.attachment || ''}`);
  const text = String(output || '').replace(/[“”]/g, '"');
  const spans = [];
  const re = /"([^"\n]{12,400})"/g;
  let m;
  while ((m = re.exec(text)) !== null) {
    const span = m[1].trim().replace(/[.,;:!?]+$/, '');
    if (span.split(/\s+/).length >= 5) spans.push(span);
  }
  if (spans.length === 0) return { pass: true, score: 1, reason: 'no quotations of five or more words' };
  const invented = spans.filter((s) => !input.includes(fold(s)));
  if (invented.length > 0) {
    return { pass: false, score: 1 - invented.length / spans.length, reason: `quoted but not in the brief: ${invented.map((s) => `"${s.slice(0, 60)}"`).join('; ')}` };
  }
  return { pass: true, score: 1, reason: `${spans.length} quotation(s) all in the brief` };
};
