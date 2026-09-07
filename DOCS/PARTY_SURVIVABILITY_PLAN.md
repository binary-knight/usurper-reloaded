# Plan: NPC and companion survivability in a player's dungeon party

Written 2026-09-07 from three inputs: a twelve-idea brief by the implementing
agent, a critique with mechanics by Codex (gpt-6-astra, high effort, working
from the inlined teammate AI, targeting, damage, death, and potion code), and a
review by the supervisor session against the world death cascade and the
follower code. Where the two reviewers disagreed, the code and the maintainer
decide. Raw receipts under `~/usurper/evidence/codex/party-raw-*.txt`. Scope
is a single player who recruits a story companion or a world NPC and takes them
into the dungeon; multiplayer groups are out of scope.

## What was fixed before this plan (PR #134)

Both reviewers read PR #134 against the code and found gaps in the teammate AI
it built on. All are on that PR now: a wounded teammate reaches for a shield
or a sidestep before it casts an attack spell and ahead of the tank's taunt;
below thirty percent it drinks its own potion before triaging others (the old
order handed its last potion to a slightly less injured ally and kept
fighting); a brace halves a monster's special attack and life drain, not only
the ordinary hit; and an NPC ally who died beside the player was rolling the
eight percent "player killed an NPC" permadeath chance instead of the two
percent "died with team" one that existed two lines above it. That last line
alone removes most of the "companions die too permanently" feeling.

## Where the reviewers agree

- **Stances**, three presets, set from the existing party management menu and
  persisted. Codex's numbers, which the supervisor's framing accepts as the
  per-stance versions of PR #134's constants:

  | Behaviour | Aggressive | Balanced (default) | Cautious |
  |---|---:|---:|---:|
  | Emergency self-potion | 30% | 35% | 40% |
  | Potion on the most injured | 50% | 55% | 60% |
  | Defensive ability first | 40% | 45% | 55% |
  | Brace with nothing left | 35% | 40% | 50% |
  | Will start a taunt above | 40% | 60% | never |
  | Ordinary target weight | x1.00 | x1.00 | x0.70 |

  Thresholds are strictly below. Cautious changes what a teammate is willing
  to do, not only when; a threshold-only Cautious would disappoint. Keyed by
  companion id or world NPC id on the player's save, like the ability toggles;
  a missing value is Balanced. Two targeting changes ride with it: the generic
  wounded-target bonus (+10 below half, +25 below a quarter) comes out of the
  ordinary weighting and is reserved for an explicitly flagged predator
  behaviour, and the +40 defending weight stays for the player and Aggressive
  allies but not for Balanced or Cautious ones, whose brace is
  self-protection. Effort medium.
- **Feedback.** One line per ally after a fight (final HP, times targeted, hits
  landed, HP lost, potions used, who supplied them), and a warning below
  thirty percent before the player voluntarily starts a fight, with a route to
  party management. Ambushes stay ambushes. No state. Effort small. Ship its
  counters with the first change so later balance is measured, not guessed.
- **Shared belt**, off by default: a teammate whose own potions are gone may
  drink from the player's, at most two borrowed potions across the party per
  fight, never taking the player below three; plus a bulk "give N potions"
  action beside the existing one-at-a-time gift. Borrowing still costs the
  ally's action. Effort small.
- **Drop for now:** a full threat ledger (the class weights plus wounded,
  defend, and armour already are a threat model; a decaying per-actor table
  adds state to every hit for a difference a terminal player cannot see),
  recruitment level scaling (the catch-up multiplier exists; a warning solves
  the real case), equipping NPC allies (an inventory UI for allies who respawn
  in ten minutes), and morale refusal (a companion who refuses to fight is a
  companion the player abandons; loyalty and the world's memory already exist).

## Where they disagree, and the choice

**A downed state.** The supervisor ranks it first: at zero HP an ally is down
for the rest of the fight; a monster that lands a hit on a downed ally
finishes them, and only the finish runs the world death cascade; a downed ally
has zero targeting weight unless a monster kind is flagged as one that
finishes the fallen. Codex ranks it last: zero HP feeds `IsAlive` filtering,
the DoT death dispatch, teammate removal, equipment return, and the world
death; ordinary finishing hits would make permanent loss feel arbitrary; an
ally who gets up after every fight needs a fiction; and it does nothing for
the "three potions after every fight" complaint by itself.

Both are right about different things. The supervisor's version is the
realism the maintainer asked for; Codex's objections are the cost. The
resolution this plan proposes: the downed state is the centrepiece of a second
release, designed as its own lifecycle (`Active`, `Downed`, `Withdrawn`,
`Dead`) with an announced finishing action rather than a second swing in the
same attack, at least one player turn between down and finish, a rescue action
for the player that costs a turn and a potion (so "left to die" becomes a
choice the player made, and the resentment penalty keys on "did not pick up"
rather than on an inference about potions), the world cascade exactly once at
the finish, and the sacrifice mechanic given an explicit exception so it does
not become a repeatable knockout. Open question for that design: whether a
boss can finish a downed ally at all, because a boss that does turns the
downed state into a slower death.

**Withdrawal.** The supervisor would widen the existing follower-retreat path
(flee chance, "retreats from combat") to NPC allies below twenty percent with
no heal left, rejoining after the fight. Codex objects to instant safety at a
threshold and to forfeiting XP, which perpetuates the under-levelling that
made retreat necessary. Resolution: withdrawal ships with the second release
alongside the downed state, as an announced action that rolls the flee chance
like a player's, keeps participation credit, and is owned by the Cautious
stance.

## Ideas the brief missed

- **Rescue** (both): the player spends a turn and a potion to pick up a downed
  ally, or covers one ally against the next single-target attack. Direct
  protective agency beside the automation.
- **Wait here** (Codex, from Tales of Maj'Eyal escorts): an order that leaves a
  teammate in the cleared room, out of the next encounter, collected on the
  way back. Needs explicit pursuit and ambush rules.
- **Floor guard** (supervisor): when an ally is more than ten levels under the
  player, the descent prompt warns and their stance defaults to Cautious.
  Closes recruitment scaling with a message instead of a mechanic.
- **The cascade as the emotional system** (supervisor): an NPC ally who saw a
  teammate finished records the memory the petition system already records,
  and the town talks. More dynamic than a refusal flag, no new state.
- **One personal emergency potion** (Codex): an ally with exactly one potion
  left keeps it for themselves unless the player is critical, so the party
  does not pour every potion into the same tank.
- **Say why** (Codex): a short line when the AI changes behaviour ("Aldric
  holds his taunt while wounded"), on the change, not every round.
- **Field dressing, finite** (Codex, replacing the brief's per-room regen):
  each completed Inn or Home rest grants an ally a treatment allowance of
  sixty percent of their max HP; after each victory they recover the least of
  twenty percent of max HP, what it takes to reach seventy percent, and the
  remaining allowance. No refill from walking, reloading, or re-recruiting;
  the allowance is the NPC's own. This addresses between-fight attrition
  without unlimited healing.

## Proposed first release

| # | Item | Effort | Depends on |
|---|---|---|---|
| 1 | Stances (three presets) with the two targeting changes and "say why" | medium | PR #134 merged |
| 2 | Feedback: fight summary and low-HP warning | small | none |
| 3 | Finite field dressing | medium | none |
| 4 | Shared belt with cap and reserve, bulk give | small | none |
| 5 | Floor guard and the one-personal-potion rule | small | 1 |

Second release: the downed state with rescue and withdrawal, the cascade
memory, wait-here. Deferred: bodyguard as a Cautious option once a generic
intercept exists; gear for allies.

Validation for the first release, as Codex framed it: a wounded caster with
attack mana chooses survival first; a wounded tank does not taunt before
shielding; a healer at twenty percent heals itself before an ally at ten;
disabled and restricted abilities stay unavailable in emergencies; brace
reduces ordinary and special damage while taunt still overrides stance
targeting; dressing cannot be refilled by reload, dismissal, or revisiting;
and seeded comparisons of ally deaths, player deaths, party potion use, and
targeting concentration before and after.

## Decisions for the maintainer

1. The stance numbers in the table, and whether the wounded-target bonus
   leaves the ordinary weighting.
2. Whether the downed state is a second release (proposed) or joins the first.
3. Field dressing's allowance (sixty percent per rest) and per-fight cap
   (twenty percent, up to seventy percent).
4. The shared belt's cap (two per fight) and reserve (three potions).
