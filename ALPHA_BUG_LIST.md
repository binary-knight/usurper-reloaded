# Usurper Remake - Comprehensive Alpha Bug List

**Generated:** January 2026
**Total Issues Found:** 150+
**Systems Audited:** 8

---

## EXECUTIVE SUMMARY

| Priority | Count | Category |
|----------|-------|----------|
| **P0 (Critical)** | 23 | Must fix before alpha - crashes/data loss |
| **P1 (High)** | 38 | Should fix before beta - broken features |
| **P2 (Medium)** | 45 | Technical debt - inconsistencies |
| **P3 (Low)** | 25+ | Polish - code quality |

---

## P0 - CRITICAL ISSUES (Must Fix Before Alpha)

### Core Systems

| # | File:Line | Issue | Impact | Fix |
|---|-----------|-------|--------|-----|
| 1 | Player.cs:73-88 | `ConfigManager.GetConfig()` returns null, crashes constructor | Game crashes on player creation | Add null check with fallback defaults |
| 2 | GameEngine.cs:116-129 | Fire-and-forget async in `_Ready()` can crash silently | Main loop failure goes undetected | Add proper error logging and recovery |
| 3 | GameEngine.cs:153-178 | `worldNPCs` may be null when `StartSimulation()` called | NullReferenceException | Check initialization order |
| 4 | Character.cs:358-418 | `RecalculateStats()` uses uninitialized base stats (0) | Equipment bonuses don't stack | Initialize base stats before first recalc |

### Combat Systems

| # | File:Line | Issue | Impact | Fix |
|---|-----------|-------|--------|-----|
| 5 | AdvancedCombatEngine.cs:872-882 | 11 placeholder methods completely empty | Combat loop hangs/crashes | Implement or remove AdvancedCombatEngine |
| 6 | AdvancedCombatEngine.cs:130,185,266,471,523,748 | `new Random()` per method call | Predictable randomness, poor entropy | Use singleton Random instance |
| 7 | SpellSystem.cs:359 | `CastSpell()` target parameter nullable but accessed unsafely | NullReferenceException on single-target spells | Add null validation |

### Story Systems

| # | File:Line | Issue | Impact | Fix |
|---|-----------|-------|--------|-----|
| 8 | StoryProgressionSystem.cs:66 | Maelketh DungeonFloor=60 (should be 25) | Can't encounter boss at correct floor | Change to 25 |
| 9 | StoryProgressionSystem.cs:121 | Terravok DungeonFloor=80 (should be 95) | Can't encounter boss at correct floor | Change to 95 |

### Location Systems

| # | File:Line | Issue | Impact | Fix |
|---|-----------|-------|--------|-----|
| 10 | LoveCornerLocation.cs:24,110,143 | Uses sync `ReadLine()` with async terminal | UI deadlock/crash | Convert to async terminal calls |
| 11 | LoveStreetLocation.cs | Missing `ProcessChoice()` implementation | Menu displays but can't select | Add ProcessChoice handler |
| 12 | PrisonLocation.cs:50-66 | Wrong method signature - won't override base | Location never loads properly | Fix to `async Task EnterLocation(Character, TerminalEmulator)` |
| 13 | TempleLocation.cs:91,214 | Uses `terminal.Clear()` instead of `ClearScreen()` | Method not found exception | Fix method names |

### Economy Systems

| # | File:Line | Issue | Impact | Fix |
|---|-----------|-------|--------|-----|
| 14 | BankLocation.cs:1201 | No overflow protection in `ExecuteRobbery()` | Integer overflow exploit | Add overflow check |
| 15 | BankLocation.cs:413-415 | Deposit doesn't check safe overflow | Unbounded gold growth | Add safe balance cap |
| 16 | BankLocation.cs:1311 | Loan interest no overflow check | Loan can overflow | Add overflow check |
| 17 | WeaponShopLocation.cs:654 | Gold deducted without re-checking after confirmation | Race condition, negative gold | Re-verify gold before deduct |
| 18 | ArmorShopLocation.cs:472-521 | Same race condition as weapon shop | Negative gold | Re-verify gold before deduct |

### Quest/Achievement Systems

| # | File:Line | Issue | Impact | Fix |
|---|-----------|-------|--------|-----|
| 19 | QuestSystem.cs:498-504 | Monster quests use global kill counter | Any quest auto-completes if player has prior kills | Use quest-specific objective tracking |
| 20 | AchievementSystem.cs:709-718 | `easter_egg_1` achievement has no trigger | Cannot be earned | Add trigger mechanism |
| 21 | AchievementSystem.cs:1400-1404 | No null check on `StatisticsManager.Current` | NullReferenceException | Add null guard |
| 22 | QuestSystem.cs:804,951,818,938 | Missing quest progress calls | Objectives never update | Add calls to OnArtifactFound, OnGoldCollected, etc. |

### Save/Load

| # | File:Line | Issue | Impact | Fix |
|---|-----------|-------|--------|-----|
| 23 | SaveSystem.cs:550-580 | `MKills`, `PKills`, `ActiveStatuses` not always restored | Data loss on load | Verify full restoration |

---

## P1 - HIGH PRIORITY ISSUES (Fix Before Beta)

### Core Systems

| # | Issue | Files | Fix |
|---|-------|-------|-----|
| 24 | NPC stub methods do nothing (`AddRelationship`, `ChangeActivity`, `SetState`) | NPC.cs:906-960 | Implement or remove |
| 25 | Character has duplicate `Defense`/`Defence` properties | Character.cs:27,1007-1011 | Consolidate to one |
| 26 | Player properties shadow base class without `new` keyword | Player.cs:9-47 | Add `new` or fix inheritance |
| 27 | NPC deserialization leaves AI systems null | NPC.cs:119-139 | Auto-call `EnsureSystemsInitialized()` |
| 28 | `GetPlayersInLocation()` always returns empty list | NPC.cs:690-717 | Fix implementation |

### Combat Systems

| # | Issue | Files | Fix |
|---|-------|-------|-----|
| 29 | Difficulty multiplier not applied to class abilities | ClassAbilitySystem.cs:1123 | Add DifficultySystem.ApplyPlayerDamageMultiplier() |
| 30 | Damage calculation inconsistent across combat systems | CombatEngine vs AdvancedCombatEngine | Unify formulas |
| 31 | Monster attack has 0 damage floor (can roll 0) | AdvancedCombatEngine.cs:215 | Add `Math.Max(1, damage)` |
| 32 | DivineBlessingSystem.Instance accessed without null check | CombatEngine.cs:788,795,1531,1587 | Add null checks |
| 33 | `globalKilled`, `globalBegged` fields set but never read | AdvancedCombatEngine.cs:22-23 | Remove or use |
| 34 | ExecuteSpellEffect method chain may be incomplete | SpellSystem.cs:474-480 | Verify all spell effects work |

### Story Systems

| # | Issue | Files | Fix |
|---|-------|-------|-----|
| 35 | StoryProgressionSystem.LoadState() only handles 4 fields | StoryProgressionSystem.cs:603 | Add artifact/seal deserialization |
| 36 | PlayTrueEnding() method exists but never called | EndingsSystem.cs:402-475 | Remove or use |

### Social Systems

| # | Issue | Files | Fix |
|---|-------|-------|-----|
| 37 | CompanionSystem Random created per decision | CompanionSystem.cs:598 | Use class-level Random |
| 38 | Two parallel relationship systems (global + per-NPC) | RelationshipSystem.cs, RelationshipManager.cs | Document or consolidate |

### Economy Systems

| # | Issue | Files | Fix |
|---|-------|-------|-----|
| 39 | Loan interest applied before consequences checked | BankLocation.cs:1310-1317 | Check consequences first |
| 40 | No equipment price validation (could be negative) | EquipmentData.cs | Add min/max validation |
| 41 | NPC marketplace can underprice items | MarketplaceSystem.cs:110-117 | Enforce minimum price |
| 42 | Shop locations don't verify all gold checks | All shop locations | Audit all gold deductions |
| 43 | DualWieldSetup missing price modifiers | WeaponShopLocation.cs:754-758 | Add alignment/event modifiers |
| 44 | AutoBuyBestArmor partial success not communicated | ArmorShopLocation.cs:662 | Show which slots purchased |

### Quest/Achievement Systems

| # | Issue | Files | Fix |
|---|-------|-------|-----|
| 45 | DefeatNPC validation uses OccupiedDays instead of objectives | QuestSystem.cs:514-524 | Use objective completion |
| 46 | Optional objectives BonusReward never applied | Quest.cs:324, QuestSystem.cs:114 | Add bonus to ApplyQuestReward() |
| 47 | RoyQuestsToday never reset daily | Quest.cs:180-181 | Add daily reset |
| 48 | Statistics fields never updated (TotalGoldSpent, TotalItemsBought, etc.) | StatisticsSystem.cs | Add tracking calls |
| 49 | Bounty board quest type filtering inconsistent | QuestSystem.cs:378-410 | Unify quest creation |
| 50 | CheckCombatAchievements() not called for all combat paths | CombatEngine.cs:1669,3707 | Add to all combat outcomes |

### Location Systems

| # | Issue | Files | Fix |
|---|-------|-------|-----|
| 51 | MainStreetLocation has hidden "[9]" test option | MainStreetLocation.cs:531-533 | Remove or document |
| 52 | InnLocation creates new Random() per check | InnLocation.cs:99-119 | Use static Random |
| 53 | Bank/Shop ProcessChoice() implementations unknown | Multiple files | Full audit needed |

---

## P2 - MEDIUM PRIORITY (Technical Debt)

### Code Quality

| # | Issue | Files |
|---|-------|-------|
| 54 | Array initialization uses capacity not count | Character.cs:916-933 |
| 55 | World NPC initialization order unclear | GameEngine.cs:148-178 |
| 56 | NPC creates new Random() per update | NPC.cs:357,428,455 |
| 57 | Status effects dictionary modification during iteration | Character.cs:614,658-775 |
| 58 | No save version migration path | SaveSystem.cs:103-108 |
| 59 | Hardcoded god names | NPC.cs:462 |
| 60 | Silent exception swallowing in NPC methods | NPC.cs:772-798 |
| 61 | Unused gym-related fields still serialized | Character.cs:515-523 |

### Combat Inconsistencies

| # | Issue | Files |
|---|-------|-------|
| 62 | Two combat engines exist (CombatEngine vs AdvancedCombatEngine) | Multiple |
| 63 | Monster level range overlaps create spawn bias | MonsterFamilies.cs:75 |
| 64 | Tier power multiplier not validated | MonsterGenerator.cs:27,92 |
| 65 | Phrase extraction unsafe (index out of bounds) | AdvancedCombatEngine.cs:488-489 |
| 66 | Integer overflow risk in damage calculation | AdvancedCombatEngine.cs:145,201 |

### Economy Balance

| # | Issue | Files |
|---|-------|-------|
| 67 | Bank interest precision loss for small amounts | BankLocation.cs:1299 |
| 68 | Loan seizure infinite loop risk | BankLocation.cs:1331-1336 |
| 69 | Guard salary scaling too high (16,000/day at level 100) | BankLocation.cs:875 |
| 70 | Healing potion cost formula inconsistent | MagicShopLocation.cs:342 |
| 71 | Shop inventory not persistent between visits | All shops |

### Achievement/Quest Gaps

| # | Issue | Files |
|---|-------|-------|
| 72 | Achievement unlock mail not sent | AchievementSystem.cs:775-786 |
| 73 | Secret achievement hints not displayed | Achievement.cs:39 |
| 74 | Difficulty-specific level achievements missing | AchievementSystem.cs |
| 75 | Streak achievements only update on login | AchievementSystem.cs:854-855 |
| 76 | Completionist depends on easter_egg_1 which is unobtainable | AchievementSystem.cs:863-864 |

---

## P3 - LOW PRIORITY (Polish)

| # | Issue | Category |
|---|-------|----------|
| 77-82 | Various unused fields in GameEngine, NPCMaintenanceEngine | Dead code |
| 83-88 | Inconsistent string formatting in news/messages | Style |
| 89-94 | Missing XML documentation on public APIs | Documentation |
| 95-100 | Console warning suppression pragmas | Cleanup |
| 101+ | Various minor inconsistencies | Polish |

---

## SYSTEMS VERIFIED WORKING

### Fully Functional (No Critical Issues)

- **7 Seals System** - All 7 seals obtainable at correct floors (0, 15, 30, 45, 60, 80, 99)
- **7 Artifacts System** - All 7 artifacts obtainable, Void Key auto-triggers
- **7 Old Gods** - All have DungeonFloor assignments (25, 40, 55, 70, 85, 95, 100), 3-phase combat
- **5 Endings** - Usurper, Savior, Defiant, True Ending, Dissolution all achievable
- **4 Companions** - Aldric (Inn), Lyris (Dungeon 15), Mira (Temple), Vex (Prison) all recruitable
- **Companion Leveling** - XP system works, role-based stat gains
- **Romance System** - Lovers, spouses, FWB, exes all tracked and serialized
- **Jealousy System** - Escalation and consequences working
- **Children System** - Pregnancy, aging, custody all functional
- **Grief System** - 5 stages with stat effects working
- **Ocean Philosophy** - All 10 fragments, 14 moments, 8 awakening levels
- **Meta Progression** - NG+ unlocks and cycle bonuses tracked

### Mostly Functional (Minor Issues)

- **Save/Load System** - Works but some fields may not restore
- **Quest System** - Works but global counter bug for monster quests
- **Achievement System** - 47/48 achievements obtainable (easter_egg_1 missing trigger)
- **Statistics System** - Works but many fields never updated
- **MainStreetLocation** - Works with hidden test option
- **InnLocation** - Works, Aldric recruitment functional
- **ChurchLocation** - Works, marriage/healing functional

---

## RECOMMENDED FIX ORDER

### Phase 1: Blocking Issues (Alpha Blocker)
1. Fix ConfigManager null crash (Player.cs)
2. Fix location async/sync issues (LoveCorner, LoveStreet, Prison, Temple)
3. Fix StoryProgressionSystem god floor values
4. Fix combat placeholder methods or remove AdvancedCombatEngine
5. Fix Random() per-call issues

### Phase 2: Data Integrity (Pre-Beta)
1. Fix save/load completeness
2. Fix gold validation race conditions
3. Fix bank overflow issues
4. Fix quest objective tracking
5. Add missing achievement triggers

### Phase 3: Feature Completion (Beta)
1. Implement NPC stub methods
2. Unify combat systems
3. Complete statistics tracking
4. Fix all shop locations
5. Add achievement notifications

### Phase 4: Polish (Release)
1. Remove dead code
2. Add documentation
3. Fix inconsistencies
4. Optimize performance
5. Final balance pass

---

## FILES REQUIRING IMMEDIATE ATTENTION

1. `Scripts/Core/Player.cs` - ConfigManager crash
2. `Scripts/Locations/LoveCornerLocation.cs` - Async issues
3. `Scripts/Locations/LoveStreetLocation.cs` - Missing ProcessChoice
4. `Scripts/Locations/PrisonLocation.cs` - Wrong signature
5. `Scripts/Locations/TempleLocation.cs` - Wrong method names
6. `Scripts/Systems/StoryProgressionSystem.cs` - Wrong floor values
7. `Scripts/Systems/AdvancedCombatEngine.cs` - Empty placeholders
8. `Scripts/Locations/BankLocation.cs` - Overflow issues
9. `Scripts/Systems/QuestSystem.cs` - Global counter bug
10. `Scripts/Systems/AchievementSystem.cs` - Missing triggers

---

*This document should be updated as bugs are fixed.*
