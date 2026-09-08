# Usurper Reborn v1.1.5

The world boss, redone: the second half, the fight. v1.1.4 made the boss
arrive on a schedule, made it killable, and made it pay. This release makes
fighting it a game: the boss tells you what it is about to do, and the round
gives you a way to answer. It is milestone B of `DOCS/WORLD_BOSS_PLAN.md`,
settled by the same council; the deviations from the plan's letter are
recorded there.

## Why

Before this, the boss's turn was a random roll: sixty percent of the time
one of its abilities, chosen at random, with nothing you could do about it
but drink. Three people fighting the same boss had no reason to know about
each other. The plan's second ruling was that every boss action should be
readable and answerable, and that some answers should need more than one
person.

## The boss telegraphs

- **A fixed cycle, not a roll.** Every couple of minutes the boss readies its next
  ability from its phase's list, in order. The round shows it, with its
  answer and its cost, and how long until it lands. Later phases add their
  abilities to the cycle.
- **A Strike is yours.** Brace, and it lands for a tenth of your health.
  Ignore it, and it lands for three tenths and brings its status along.
  Bracing costs the round.
- **A Channel is everyone's.** A battlefield-wide, unavoidable, or healing
  ability is a channel. Interrupt it, and if enough of you do before it
  lands (two, or one if you are alone), it breaks and the boss staggers:
  for the next minute every blow on it lands half again as hard. If it
  is not broken, it lands on everyone fighting for three tenths, halved for
  anyone who braced, and a healing channel heals the boss.
- **What you were in for follows you.** Leave while a telegraph is up and
  it still lands on you when you come back. Anything the boss readied while
  you were gone is not yours.
- **A landing never stuns, freezes, or paralyses you.** A player who cannot
  act cannot answer the next one.

## The boss focuses

- **It goes for whoever hurts it most.** Every minute the boss turns to the
  fighter who dealt the most in that minute, and hits them half again as
  hard; everyone else takes half. Alone, it is always you.
- **Challenge it.** Take its focus for a minute so a wounded friend can
  breathe. One challenger at a time.

## The screen

- **Under twenty rows a round.** One line for the boss (health, phase, and
  the stagger timer), one for you, one for who is fighting beside you (your
  group first, five names and a count), the telegraph with its answer key
  and cost, and a one-line menu in place of the old seven-row box. Plain
  lines in screen-reader mode.
- **Your answers are seen.** When you brace, interrupt, or challenge, the
  other fighters get a line at their next prompt, in their own language, and
  so does a stagger.

## Not in this release

Cover, immunity windows by phase, per-boss scripts, an enrage, companions in
the fight, the boss's currency and vendor (1.2), the trophy (1.3).

## Tests

1,150 passing, up from 1,134. `WorldBossTelegraphTests` drives the tick and
the fight against a real database: the tick issues from the cycle only while
someone is engaged and never over a live telegraph, resolves a landing once,
breaks a channel that met its need and staggers the boss, heals the pool once;
the interrupt counter never passes its need under sixteen concurrent answers;
a braced strike lands for a tenth and an unanswered one for three tenths,
once per seq across sessions; a returning player takes only what they were in
for; focus follows window damage unless a Challenge holds it; a round with a
telegraph, a status, and seven fighters fits in twenty rows.
