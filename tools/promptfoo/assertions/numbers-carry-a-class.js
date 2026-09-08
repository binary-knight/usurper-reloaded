// The councils' labelling rule: a line that states a percent, a multiple (1.5x) or a money figure
// must say where the number comes from: MEASURED, DERIVED, GUESS, "starting" (a starting setting),
// or a file cite (File.cs, File.cs:123). Attach this to cases whose brief asks for labels; it will
// not catch a mislabel, only an unlabelled number.
'use strict';

const numberRe = /(\d+(?:\.\d+)?\s*%|\b\d+(?:\.\d+)?\s*x\b|\$\s*\d)/i;
const classRe = /\b(MEASURED|DERIVED|GUESS|[Ss]tarting)\b|\b[A-Z][A-Za-z0-9_]*\.(?:cs|md|json)(?::\d+)?\b|\b(?:Scripts|DOCS|Tests)\/[^\s)]+/;

module.exports = (output) => {
  const lines = String(output || '').split('\n');
  const flagged = lines.filter((l) => numberRe.test(l));
  if (flagged.length === 0) return { pass: true, score: 1, reason: 'no percent, multiple, or money figure' };
  const bare = flagged.filter((l) => !classRe.test(l));
  if (bare.length > 0) {
    return { pass: false, score: 1 - bare.length / flagged.length, reason: `unlabelled figures on ${bare.length} line(s): ${bare.slice(0, 3).map((l) => `"${l.trim().slice(0, 70)}"`).join('; ')}` };
  }
  return { pass: true, score: 1, reason: `${flagged.length} line(s) with figures, all labelled` };
};
