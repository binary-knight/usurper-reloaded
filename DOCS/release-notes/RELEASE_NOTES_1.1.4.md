# Usurper Reborn v1.1.4

The world boss, redone: the first half. The plan is
`DOCS/WORLD_BOSS_PLAN.md`, settled by a council of Codex, two design agents,
and the supervisor session, and it names two milestones. This release is
milestone A, "killable and known": the boss arrives on a schedule everyone
can see, it can actually be killed by the people who show up, and it pays
them properly. Milestone B, the fight itself (telegraphed attacks you answer,
shared interrupts, a boss that focuses whoever is hurting it), is 1.1.5.

## Why

A player asked why nobody fought the world boss. The code review answered
plainly: nobody could. Spawn HP carried the global 2.25 difficulty scale on
top of the base, so the smallest boss with two players online was 462
thousand HP; a level-40 Warrior died in ten to fourteen rounds to the
unavoidable aura, potions cost the round, and the one-session lock meant
three players together removed two to four percent. The alpha audit's
finding that 92 percent of bosses expired unkilled predates that lock. On
top of that, the boss's actions were a random roll with no counterplay,
"regenerates" healed nothing, rewards were a few hundred XP at any level,
and no notice reached anyone who was not online at the moment it spawned.

## The boss comes on a schedule

- **Every evening at 8 PM Eastern**, whether or not anyone is online. The
  next boss is chosen the moment the last one ends, by the median level of
  everyone who played in the last seven days, and never more than ten levels
  above it: a level-15 cohort does not meet the Nameless Horror.
- **You are told.** A countdown line on the town screen names the next boss
  and the time left; `/boss` shows the same when no boss is up. When a boss
  is scheduled, everyone active in the last week gets a mail in their own
  language, the news carries it, and the Discord bridge posts it. One hour
  before, a broadcast. While it is up, the town line shows its health, how
  many heroes are fighting, and the time left.
- **The window is three hours.** A boss that survives it withdraws with its
  wounds and returns the next evening healed twenty percent, up to three
  nights; on the third it leaves, and the news names everyone who stood. If
  the server was down for a whole window, the boss does not appear at two
  in the morning; the schedule rolls forward.

## The boss can be killed

- **HP is a budget, not a population multiplier.** A boss has three
  reference fighters' worth of a window's damage at its level: about 42
  thousand for the smallest, 101 thousand for the largest. Someone logging
  into town no longer makes the fight harder.
- **The boss meets you at your level.** A player below the boss's level
  fights it as if it were their level, and their blows count in full on the
  shared pool. A level 15 who fights like a level 15 lands the same wound as
  a level 80 who fights like a level 80. At or above the boss's level you
  fight it raw. Every blow is capped per round at six tenths of a percent of
  the boss's health, which closes the twenty-fold gap a top-level Magician
  had over a Warrior.
- **No lock, no aura.** Retreat, fall, or rest at fifty rounds, then come
  back: two minutes after a retreat or a rest, five after a fall. A fall is
  still non-lethal. The presence aura is gone; the boss acts once a round,
  twice in its last phase, and no single ability takes more than thirty
  percent of your health. A healing ability heals the boss's real pool now.
- **Rally.** Leave a boss alone for ten minutes and it starts to heal, half
  a percent every thirty seconds, so a boss is killed by people fighting it,
  not by a slow chip across days. A returned boss keeps its wounds until
  someone hits it.

## The boss pays properly

- **Rewards by effort, at your level.** Your contribution is your damage
  against one reference fighter's budget. A full contributor earns about an
  hour in the dungeon at their own level, capped at half a level, plus gold
  on the same scale, plus a bonus of ten percent per other contributor, up
  to half again. Anyone at a tenth of the budget qualifies. Items come by
  contribution, not rank: Epic at three quarters, Rare at half, Uncommon at
  a quarter; the MVP of three or more gets Legendary. Items roll at your
  level.
- **A boss that withdraws pays a quarter share** for the night to everyone
  who qualified on it.
- **Delivered the same online or offline.** Rewards are settled once into
  a ledger and delivered by your own session at login or at `/boss`, once
  each, with the statistics, fame, and achievements that offline players
  used to be denied. A full pack sends the item to your inheritance.
- **The realm celebrates:** a kill gives everyone ten percent more
  experience for a day.

## Fixed on the way

- "Regenerates" heals; boss-slayer gear counts only when equipped, not in
  the bag; Precise Strike rolled its critical twice; damage was credited to
  the leaderboard after the boss was already dead, overkill and all; a
  session could overwrite the phase with a stale copy; the status screen
  said one hour and "2+ players online".

## Not in this release

Milestone B, the fight: telegraphed actions with Brace and Interrupt,
shared channels two players break together, a stagger window, Focus and
Challenge, the compact round screen. Companions in the fight, per-boss
scripts, and the boss's currency are later still.

## Tests

1,134 passing, up from 1,101. The first world boss tests:
`WorldBossMathTests`, `WorldBossBackendTests`, `WorldBossTickTests`,
`WorldBossLoopTests`, `WorldBossSettleTests`; the fight itself is driven
through a scripted terminal against a real database.
