# Council brief: redesigning the world boss fights (Usurper Reborn 1.1.4)

## The ask, in the maintainer's words

"For 1.1.4, I want to look at the world boss fights. They are pretty hokey. How can we redo these to make people want to actually participate and fix the fighting mechanics so it isn't just... silly."

Two questions, then: (1) participation: why would a player show up, and show up when others do; (2) mechanics: what makes the fight itself a fight rather than a menu of "A" fifty times. The deliverable is a design the council agrees on, sliced so that 1.1.4 is one patch release, with anything larger held for a later slice. Numbers are welcome, but every number must be labelled a starting setting unless it is derived from something in the code.

## The game, for anyone new to it

Usurper Reborn is a C#/.NET 8 remake of a 1993 BBS door game. Text terminal, 80x25 (door sessions page at 23 rows), five languages (all player text through `Loc.Get` keys; boss names are the one untranslated layer), a screen-reader mode that must get plain lines. It runs as a console game, a BBS door, a multiplayer MUD server on SQLite (play.usurper-reborn.net, also reachable via web and the Steam desktop client over SSH), with a world simulation service ticking every 30 seconds that runs NPC life, a 7 PM ET daily reset, and the world boss spawn check. There is a Discord bridge (`DiscordBridge.QueueSystemEvent`, `QueueOutbound`) that relays chat and system events to a Discord channel, and an in-game mail system (`SqlSaveBackend.SendMessage(from, to, type, text)`) that reaches offline players at next login. The news feed shows events in town.

Population is small. The alpha audit (v0.60.0) recorded that 92 percent of bosses (387 of 422) expired unkilled; the fix was to spawn less often, extend the window, and lower HP scaling. Expect 2 to 10 concurrent players at peak, often 0 to 2 otherwise.

The regular combat engine has the good boss mechanics already: the dungeon's Old Gods fights have phases with immunity windows (physical or magical for four rounds), an enrage timer with warnings at 50 and 75 percent, a two-round channel ("devastating party-wide damage") that any party member can interrupt, minion summons, and a party of NPC teammates and companions. 1.1.3 just shipped a party survivability release (teammate stances, self-preservation, fight summaries) for dungeon parties. None of that reaches the world boss, which is a separate system.

## What the world boss is today (code: `Scripts/Systems/WorldBossSystem.cs`, `Scripts/Data/WorldBossData.cs`, config in `GameConfig.cs`, SQL in `SqlSaveBackend.cs`)

Spawn: every 30 s tick, if no boss is active, at least 2 players are online, and 8 hours have passed since the last boss *started*, pick one of eight bosses at random (base levels 35, 40, 45, 50, 55, 60, 70, 80; base HP 180k to 500k). Level = max(base, average online level); HP = base x (1 + 0.07 x onlineCount) x the global difficulty scale. Window: 6 hours. Broadcast to online players and the news feed; nothing to offline players, nothing in advance.

The fight: each player runs their own private combat loop against the shared HP number in SQLite. Per round: the loop re-reads the boss row, checks a phase transition (65 and 30 percent, shared through the boss JSON), shows an action box (Attack, Cast, Defend, Item, Power attack 75 percent hit x1.5, Precise strike 80 percent damage with double crit, class ability, Retreat), records the damage atomically, then the boss takes 2 or 3 attacks (plus one in phase 3), each 60 percent likely to be a random ability from the phases unlocked so far, else a basic hit; then an unavoidable "aura" of 5 to 8 percent of the player's max HP (x1.5 in phase 2, x2 in phase 3; halved by Defend). Boss damage = STR minus half the player's defence, floored at 20 percent of STR. Abilities apply status effects (stun, freeze, silence, fear, poison...) with a 30 percent plus 0.5 per level resist. Death is non-lethal: revive at 25 percent HP, 60 second cooldown, and the session ends. Max 50 rounds per session. A per-spawn lock (v0.60.0, added to stop fight-leave-heal-return soloing) means any exit but the kill locks the player out of that spawn; that includes dying and hitting the 50-round cap, so every player gets exactly one session of at most 50 rounds per boss.

Rewards on the kill, by damage rank: MVP x3, top 3 x2.5, top 25 percent x2, top 50 percent x1.5, else x1, applied to XP = 10 x bossLevel and gold = 200 x bossLevel, plus one generated item with a minimum rarity by tier (Legendary for MVP, Epic for top 3, Rare, Uncommon, Common), 25 fame for the killer and 15 for others, mail to everyone on the leaderboard, a news line. Achievements for first kill, five unique bosses, 25 kills, MVP.

## What I (the implementing session) found wrong, in order of how much it matters

1. **Nobody is fighting together.** The only shared state is the HP number and the phase. Players never see each other, never affect each other, and the boss never reacts to the group. It is a single-player DPS check run in parallel; the "raid" is a leaderboard. Being online at the same time as someone else has no mechanical meaning except that it raised the boss's HP.
2. **The math makes it unkillable for the population it has.** A player's damage per round is roughly STR + WeapPow minus half the boss's defence, times 0.7 to 1.3, times 1.5 on a crit; call it a few hundred at level 40. Fifty rounds is the cap, and the lock means one session. Fifty rounds times a few hundred is 10 to 20 thousand damage per player per spawn against 200 to 500 thousand HP. The v0.60 comment says HP was eased "so 2-3 players can finish"; the lock in the same release made that arithmetically impossible. The 92 percent expiry figure predates the lock; it can only be worse now. Check my arithmetic against real level 40 to 80 stats before believing it, but the shape is right.
3. **The boss is random, not readable.** 60 percent chance of a random ability from a pool, no telegraph, no counterplay; Defend is the only reaction and it is blind. The aura is pure attrition. Phases add abilities and multiply the aura; nothing changes what the player should do. This is what "silly" means in play: press A, watch numbers, get stunned, die, wait.
4. **Things that print but do nothing.** `SelfHealPercent` prints "regenerates" and never heals the shared pool. `IsAoE` is set on many abilities and read nowhere (there is nobody else in the room to hit). The status screen says "Each boss lasts for 1 hour" and "spawns automatically when 2+ players are online" (the loc keys are stale: it is 6 hours and there is an 8 hour cooldown). `HasSpecialEffect` (BossSlayer, TitanResolve) scans the whole inventory, not equipped items, so a bag item counts.
5. **Rewards are flat and small.** XP = 10 x bossLevel x tier: 400 to 800 XP base at a level where a dungeon floor gives far more. Gold 8k to 16k base. The rank tiers collapse with a small population: with three participants everyone is "top 3" and gets Epic or better. There is no reason for a level 80 to fight a level 40 boss, and a level 15 (min level 10) cannot survive a level 80 boss that spawned because the average online level was 15 and the random pick landed on the Nameless Horror (level = max(base, avg), so the base wins).
6. **Timing is invisible.** Random moment inside an 8 hour cooldown, six hour window, no advance notice, no schedule, nothing to offline players, nothing to Discord. The world's daily reset is a fixed 7 PM ET, so a fixed schedule is already a concept the game has.
7. **The party is left at the door.** The game just spent a release on companions and NPC teammates; the world boss is the one fight they cannot join. Grouped players (the leader-follower group system) cannot fight it as a group either.
8. **The world does not care.** The boss has no effect on the town while it lives and no consequence when it leaves unkilled; the "rampaging" line on every screen is text.

## Constraints on any design

- One patch release for the first slice; no new NuGet packages; SQLite is the only shared state; the world sim tick is 30 s and is the only server-side clock; each player's loop is their own thread reading and writing rows, there is no shared round. A design that needs a shared clock must say how it lives in the boss row and the tick, and what a player sees when they arrive mid-mechanic.
- Save compatibility: new persisted player fields go in `PlayerData` and the three save sites; enums append only; new SQL columns need `ALTER TABLE ... ADD COLUMN` guarded for existing databases (the pattern exists in `SqlSaveBackend`).
- All text through loc keys in five languages; screen-reader mode gets plain lines; door sessions must not exceed the pager mid-mechanic.
- No real-time input: the terminal reads a line; a mechanic cannot depend on reacting inside a few seconds. It can depend on choosing a response before the boss's next telegraphed action.
- Offline players cannot be pulled in; they can be told (mail, Discord, news, a countdown on the town screen).
- Balance numbers are starting settings; the plan must say what to log so they can be tuned from live data (the audit that found the 92 percent figure was possible because bosses are rows in a table).
- Roadmap context: 1.2 has faction vendor gear and endgame gear sets (Runed, Plate, Titan's, Dragon, Holy) planned. A world boss currency or vendor could land there rather than here. The Player District is 1.3. Do not design those; do say where a world boss reward hook should attach.

## Questions the council must answer

1. What is the participation loop: why does a player log in for this, and why at the same time as others? Schedule, notice, and what "being there together" does mechanically.
2. What is the fight: what does a round look like, what does the player read and decide, what does the boss do that can be answered, and how do several players' choices interact through the shared row?
3. What replaces the one-session lock and the attrition aura so that a small population can kill a boss without a single tank soloing it across sessions?
4. Rewards: what is worth showing up for at level 15 and at level 80, how is contribution measured fairly across levels, and what happens when the boss is not killed.
5. Should the party come (companions, NPC teammates, grouped players), and if so how without making the shared row explode.
6. Should the boss touch the world while alive or when it wins, and what.
7. The 1.1.4 slice: what ships first, what is held, and what is logged to tune it.

Answer with rulings, not surveys. Where you disagree with my findings above, say so with the reason. Where a number is a guess, label it.
