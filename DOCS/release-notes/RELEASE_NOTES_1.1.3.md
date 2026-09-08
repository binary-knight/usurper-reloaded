# Usurper Reborn v1.1.3

A player reported that their companion took nine of ten hits in a fight. That
is the targeting working as designed (a fighting-class ally beside an Assassin
draws most of the attacks), but it exposed that allies had no self-preservation
once their potions ran out, and that the player had no say in how an ally
fought. This release is about keeping the people you bring into the dungeon
alive. The plan behind it is `DOCS/PARTY_SURVIVABILITY_PLAN.md`: written from
a brainstorm between Codex, a Claude design agent, and the supervisor session,
with the open questions settled by the same council and recorded there. The
downed state and rescue, the biggest idea in the plan, is deliberately the
next release, not a shortcut in this one.

## Allies look after themselves (PR #134)

- **Wounded allies shield up.** Below 40 percent HP an ally prefers a
  defensive or evasive ability (Defense-type, dodge and smoke effects, buffs
  with a defense bonus), ahead of the tank taunt and ahead of any attack spell.
- **With nothing left, they brace.** Below 35 percent with no heal and no
  defensive ability, the ally braces for the round: half incoming damage, now
  on ordinary hits, monster special attacks, and life drain alike. The brace
  clears at round end and in the per-combat scrub; it used to leak into every
  later fight when it landed on the round the last monster fell.
- **They drink their own potion first.** Below 30 percent an ally drinks
  before triaging others; the old order handed its last potion to a slightly
  less injured friend.
- **The right permadeath rate.** An NPC ally who dies in your party rolls the
  2 percent "died with team" rate instead of the 8 percent "player killed an
  NPC" rate it had been passing since v0.42.

## Tactics: three stances

`[T]` in the dungeon party menu and the Inn's party management sets a stance
per ally, saved with your character. Balanced is exactly what PR #134 shipped
and is the default, so nothing changes for anyone who never touches it.

| | Aggressive | Balanced | Cautious |
|---|---:|---:|---:|
| Drinks own potion below | 25% | 30% | 40% |
| Shields up below | 35% | 40% | 55% |
| Braces below | 30% | 35% | 50% |
| Potions the most injured below | 45% | 50% | 60% |
| Taunts | yes | yes | never |
| Targeting weight | normal | normal | 70% of normal |

Two targeting rules changed for everyone. Ordinary monsters no longer prefer a
wounded target, so an ally is not singled out at the moment it starts
protecting itself. And an ally's brace no longer draws extra attacks the way a
player's Defend does; the +40 targeting weight stays for the player, for
grouped players, and for Aggressive allies, whose brace is meant to pull hits.
Grouped players, mercenaries, and echoes take no orders. When an ally changes
its behaviour because of its stance it says so once per fight ("hangs back",
"keeps the last potion", "holds the taunt") rather than every round.

## Supply

- **Shared potion belt**, off by default, `[B]` in the dungeon party menu.
  When it is on, an ally with no potions of its own may drink one of yours
  for its own emergency: at most two per fight across the party, never leaving
  you below three, never handed on to a third character, and never touching
  your own potion cooldown. Drinking costs the ally's action.
- **Give a number.** The issue-potion prompt offers `[N]` beside `[1]` and
  `[F]`, capped at what they can carry and what you hold. A deliberate gift
  ignores the belt's reserve.
- **One personal potion.** An ally holding exactly one potion keeps it for
  itself unless you are below 30 percent.

## Information

- **A fight summary** after every victory with allies: each ally's final HP,
  how many times the monsters chose them, hits landed, HP lost, and potions
  drunk, with how many were yours. A fallen ally is still reported.
- **A warning before a voluntary fight** when an ally is below 30 percent,
  with the choice to go anyway. Ambushes are still ambushes.
- **The floor guard.** Descending with an ally eleven or more levels below
  you warns you and offers Cautious for that ally. Nothing is switched
  without asking, and a declined offer is not repeated on the next floor.

## Fixed on the way

- Every potion an ally drank was recorded in the player's own potion
  statistics.

## Not in this release

The downed state with rescue, withdrawal from a fight, the "wait here" order,
and the finite field dressing after fights are designed in the plan and held
for the next release, together, because they change what death means and
belong in one piece.

## Tests

1,101 passing, up from 1,074. New: `TeammateDefenseTests`,
`TeammateStanceTests`, `PartySurvivabilityTests`.
