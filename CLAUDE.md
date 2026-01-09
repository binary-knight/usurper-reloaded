# Claude Instructions for Usurper Remake

## Project Overview
This is a remake of "Usurper: Halls of Avarice", a classic BBS door game from the early 1990s. The remake is built in C# targeting .NET 8.0 (LTS). The game runs in console/terminal mode.

**Discord**: https://discord.gg/BqY66QkPGE

## Build & Run Commands
```bash
# Build
dotnet build usurper-reloaded.csproj --configuration Release

# Publish (ALWAYS use ./publish directory)
dotnet publish usurper-reloaded.csproj --configuration Release --output ./publish

# Run
./publish/UsurperRemake.exe
```

## Key Architecture

### Game Flow
- `GameEngine.cs` - Main game loop, handles login, character selection, entering game world
- `LocationManager.cs` - Manages navigation between locations
- `BaseLocation.cs` - Base class for all locations (shops, dungeon, etc.)

### Character System
- `Character.cs` - Base class for all characters (players and NPCs)
- `Player.cs` - Extends Character for human players
- `NPC.cs` - Extends Character with AI behavior (NPCBrain, personality, memory)
- `CharacterSex` enum: `Male`, `Female`
- `CharacterClass` enum: `Warrior`, `Magician`, `Cleric`, `Assassin`, `Barbarian`, `Paladin`, `Sage`, etc.

### NPC System
- `NPCSpawnSystem.cs` - Singleton that manages 50 classic NPCs
  - `Instance.ActiveNPCs` - List of all spawned NPCs
  - `InitializeClassicNPCs()` - Called on game start, spawns NPCs from templates
  - NPCs have readable location names: "Main Street", "Dungeon", "Weapon Shop", etc.
- `ClassicNPCs.cs` - Templates for the 50 NPCs with names, classes, personalities
- `WorldSimulator.cs` - Background simulation (every 60 seconds)
  - NPCs explore dungeons, shop, train, level up, heal, move around
  - Actions generate news via `NewsSystem.Instance.Newsy()`

### Combat System
- `CombatEngine.cs` - Player vs monster combat
- `MonsterGenerator.cs` - Creates monsters for dungeon levels 1-100
- `MonsterFamilies.cs` - Monster types organized by family (Goblinoid, Undead, Draconic, etc.)

### Save System
- `SaveSystem.cs` - JSON-based save/load
- `SaveDataStructures.cs` - PlayerData class with all saveable fields
- Saves go to `./saves/` directory

### Terminal/UI
- `TerminalEmulator.cs` - Main UI class for terminal output
- `ConsoleTerminal.cs` - Console-specific terminal implementation
- Color markup: `[red]text[/]`, `[bright_cyan]text[/]`, etc.
- Key input methods:
  - `GetKeyInput()` - Returns `Task<string>` with single key
  - `WaitForKey()` - Returns `Task` (void), just waits
  - `ReadLineAsync()` - Returns `Task<string>` for full line input

## Important Formulas

### XP Requirements (level^1.8 * 50 curve)
```csharp
private static long GetExperienceForLevel(int level)
{
    if (level <= 1) return 0;
    long exp = 0;
    for (int i = 2; i <= level; i++)
    {
        exp += (long)(Math.Pow(i, 1.8) * 50);
    }
    return exp;
}
```
This formula is used in: `LevelMasterLocation.cs`, `WorldSimulator.cs`, `BaseLocation.cs`, `NPC.cs`

### Monster XP Reward
```csharp
long baseExp = (long)(Math.Pow(level, 1.5) * 15);
```

### Monster Stat Scaling (reduced for balance)
- HP: `(40 * level) + level^1.2 * 15`
- Strength: `(4 * level) + level^1.15 * 2`
- Defence: `(2 * level) + level^1.1 * 1.5` (then * 0.7)

## Common Patterns

### Adding a menu option to a location
```csharp
// In DisplayLocation() - show the option
terminal.WriteLine("(X) Do Something");

// In ProcessChoice() - handle the option
case "X":
    await DoSomething();
    return false; // false = stay in location, true = exit location
```

### Generating news
```csharp
NewsSystem.Instance.Newsy(true, "Something happened!");
NewsSystem.Instance.WriteDeathNews(victimName, killerName, location);
```

### Checking NPC properties
```csharp
npc.Name          // Display name (from Name2)
npc.Level         // Current level
npc.Class         // CharacterClass enum
npc.Sex           // CharacterSex enum (Male/Female)
npc.IsAlive       // HP > 0
npc.CurrentLocation // String like "Main Street", "Dungeon"
```

## Recent Changes (Jan 2025)

### Equipment Expansion
- **Weapons**: Expanded from ~14 to 120+ weapons across all types (swords, axes, daggers, maces, etc.)
- **Armor**: Full slot coverage with tiered progression (Head, Body, Arms, Hands, Legs, Feet, Waist, Face, Cloak)
- **Shields**: Complete shield lineup (Bucklers, Kite Shields, Tower Shields, etc.)
- **Accessories**: 35+ Neck items, 40+ Rings with stat bonuses

### Economy Rebalance
- **Monster Gold**: Changed from `level^1.3 * 8` to `level^1.5 * 12` (better endgame scaling)
- **Disease Costs**: Changed from linear to diminishing curve `baseCost * (1 + level^0.6)`
- **Guard Salary**: Increased from 50 to 150 gold per level
- **Rare Encounters**: Tripled all gold rewards

### Bug Fixes
- **CRITICAL**: Fixed equipment stat bonuses not applying on save/load (added `RecalculateStats()`)
- Fixed display text mismatches in rare encounters showing wrong gold amounts

### Previous (Dec 2024)
1. **Balance Overhaul**: Reduced monster scaling, gentler XP curve (1.8 instead of 2.5)
2. **NPC Activity System**: NPCs now actively explore dungeons, shop, train, level up
3. **Character Listing**: Main Street [L] option shows all characters with pagination
4. **NPC Initialization Fix**: Check both flag AND count to ensure NPCs spawn

## Files You'll Likely Need

| Purpose | File |
|---------|------|
| Main game loop | `Scripts/Core/GameEngine.cs` |
| Combat | `Scripts/Systems/CombatEngine.cs` |
| Monster generation | `Scripts/Systems/MonsterGenerator.cs` |
| NPC spawning | `Scripts/Systems/NPCSpawnSystem.cs` |
| NPC AI simulation | `Scripts/Systems/WorldSimulator.cs` |
| Saving/Loading | `Scripts/Systems/SaveSystem.cs` |
| Main Street | `Scripts/Locations/MainStreetLocation.cs` |
| Dungeon | `Scripts/Locations/DungeonLocation.cs` |
| Level up | `Scripts/Locations/LevelMasterLocation.cs` |
| Character data | `Scripts/Core/Character.cs` |

## Equipment System (Planned)
There's a plan file at `~/.claude/plans/nifty-wobbling-cerf.md` for overhauling the equipment system to support:
- Individual armor slots (head, body, arms, hands, legs, feet, etc.)
- Weapon handedness (1H/2H) with dual-wield and shield support
- This is NOT yet implemented - currently uses simplified WeapPow/ArmPow values

## Things to Remember

1. **Always publish to `./publish`** - not `./bin/Publish` or anywhere else
2. **NPC locations are strings** - Use readable names like "Main Street", not numeric IDs
3. **CharacterClass is an enum** - Don't use nullable operator `?.` on it
4. **Terminal key input** - Use `GetKeyInput()` for getting key presses that return values
5. **Max level is 100** - Both players and NPCs cap at level 100
6. **WorldSimulator runs every 60 seconds** - NPCs do activities in background

---

## Feature Roadmap

Progress tracker for features to broaden appeal. Organized by priority and effort.

### Priority 1: Quick Wins (Low Effort, High Impact)

| Status | Feature | Description | Files |
|--------|---------|-------------|-------|
| [x] | **Difficulty Modes** | Easy/Normal/Hard/Nightmare affecting XP rates, monster damage, gold drops | `DifficultySystem.cs`, `CombatEngine.cs`, `Monster.cs`, `CharacterCreationSystem.cs` |
| [ ] | **Combat Speed Options** | Instant/Fast/Normal text speed toggle | `CombatEngine.cs`, `GameConfig.cs` |
| [ ] | **Auto-Combat Toggle** | Let players auto-fight weaker monsters | `CombatEngine.cs` |
| [x] | **Achievement System** | 50+ achievements across Combat, Progression, Economy, Exploration, Social, Challenge categories | `AchievementSystem.cs`, `CombatEngine.cs`, `SaveDataStructures.cs`, `MainStreetLocation.cs` |
| [x] | **Statistics Screen** | Track kills, deaths, gold earned, time played, etc. | `StatisticsSystem.cs`, `MainStreetLocation.cs` |

### Priority 2: Guidance Systems (Medium Effort, High Impact)

| Status | Feature | Description | Files |
|--------|---------|-------------|-------|
| [ ] | **Quest Journal** | Track active quests and objectives | New: `QuestJournal.cs` |
| [ ] | **Daily Summary** | "While you were away..." recap of NPC activities | `WorldSimulator.cs`, `MainStreetLocation.cs` |
| [ ] | **Location Hints** | Brief tips when entering new locations | All location files |
| [ ] | **Monster Bestiary** | Track monsters encountered with stats/drops | New: `Bestiary.cs` |
| [ ] | **NPC Relationship Summary** | Quick view of standing with all NPCs | `MainStreetLocation.cs` |

### Priority 3: Engagement Loops (Medium Effort, High Impact)

| Status | Feature | Description | Files |
|--------|---------|-------------|-------|
| [ ] | **Daily Challenges** | "Kill 5 Goblins" type objectives with rewards | New: `DailyChallenges.cs` |
| [ ] | **Bounty Board** | Posted bounties on specific monsters/NPCs | New: `BountySystem.cs` |
| [ ] | **Milestone Rewards** | Bonuses at levels 10, 25, 50, 75, 100 | `LevelMasterLocation.cs` |
| [ ] | **Prestige/New Game+** | Reset with bonuses after beating the game | `GameEngine.cs` |
| [ ] | **Seasonal Events** | Holiday-themed content and rewards | New: `SeasonalEvents.cs` |

### Priority 4: Narrative Hooks (High Effort, High Impact)

| Status | Feature | Description | Files |
|--------|---------|-------------|-------|
| [ ] | **Main Story Arc** | Overarching narrative with villain and conclusion | Multiple files |
| [ ] | **NPC Storylines** | Personal quests for key NPCs | `NPC.cs`, new quest files |
| [ ] | **Faction System** | Join guilds with unique benefits/quests | New: `FactionSystem.cs` |
| [ ] | **Rival NPC System** | Nemesis NPC that grows with you | `NPCSpawnSystem.cs` |
| [ ] | **Multiple Endings** | Different outcomes based on choices | `GameEngine.cs` |

### Priority 5: Presentation Polish (Variable Effort)

| Status | Feature | Description | Files |
|--------|---------|-------------|-------|
| [ ] | **Animated Title Screen** | ASCII art animation on startup | `GameEngine.cs` |
| [ ] | **Sound Effects** | Combat sounds, UI feedback (optional) | New: `SoundSystem.cs` |
| [ ] | **Background Music** | Atmospheric music (optional) | New: `MusicSystem.cs` |
| [ ] | **Visual Themes** | Color scheme options (Classic, Dark, Light) | `TerminalEmulator.cs` |
| [ ] | **Combat Log Export** | Save combat logs to file | `CombatEngine.cs` |

### Implementation Priority Matrix

**Start Here (Quick Wins):**
1. Difficulty Modes - Immediate accessibility improvement
2. Statistics Screen - Players love tracking progress
3. Achievement System - Natural engagement hook

**Next Phase (Guidance):**
4. Quest Journal - Helps players track goals
5. Daily Summary - Makes NPC world feel alive
6. Monster Bestiary - Collection incentive

**Growth Phase (Engagement):**
7. Daily Challenges - Gives players daily reasons to return
8. Bounty Board - Directed gameplay goals
9. Milestone Rewards - Celebration of progress

**Polish Phase (Narrative):**
10. Main Story Arc - Gives the game a "point"

### Estimated Playtime

- **Casual Playthrough** (Level 30-40): 20-40 hours
- **Serious Run** (Level 60-70): 100-200 hours
- **Completionist** (Level 100): 1,000-1,500 hours

### Status Legend
- `[ ]` Not Started
- `[~]` In Progress
- `[x]` Completed

---

## Code Audit (Jan 2025)

Comprehensive audit of the codebase for bugs, broken systems, and missing implementations.

### Fixes Applied

#### Critical Bug Fixes
| File | Issue | Fix |
|------|-------|-----|
| `TerminalEmulator.cs` | Null reference crash in `ColorNameToConsole()` | Added `?.ToLower()` and `null or _` pattern matching |
| `RomanceTracker.cs` | `null!` passed to `UpdateRelationship()` causing NullReferenceException | Added `player` parameter, fixed 4 call sites |
| `GameEngine.cs` | Missing system initialization | Added `AchievementSystem.Initialize()` and `QuestSystem.EnsureQuestsExist()` |
| `CombatEngine.cs` | Difficulty multipliers not applied to rewards | Added XP/Gold multipliers in 3 reward calculation paths |
| `SaveSystem.cs` | No backup before overwriting saves | Added `CreateBackup()` call in `SaveGame()` |

#### Statistics Tracking Added
| File | Change |
|------|--------|
| `LevelMasterLocation.cs` | Added `RecordLevelUp(newLevel)` call for each level gained |
| `DungeonLocation.cs` | Added `RecordDungeonLevel()` when entering new floors |
| `BaseLocation.cs` | Added `RecordLocationVisit()` on location entry |
| `StatisticsSystem.cs` | Added new `RecordPlayerKill()` method |
| `CombatEngine.cs` | Added PvP kill/death tracking in `DeterminePvPOutcome()` |

#### Defensive Null Checks Added
| File | Method |
|------|--------|
| `SpellSystem.cs` | `GetAvailableSpells()`, `CanCastSpell()` |
| `ClassAbilitySystem.cs` | `GetAvailableAbilities()`, `CanUseAbility()` |
| `Items.cs` | `ApplyToCharacter()`, `RemoveFromCharacter()` |

#### Code Cleanup
| File | Change |
|------|--------|
| `DungeonLocation.cs` | Removed DEBUG output block |
| `MainStreetLocation.cs` | Removed unreachable `return true;` after throw |
| `RareEncounters.cs` | Replaced emojis with ASCII (`🧙` → `===`, `👑` → `***`) |
| `BaseLocation.cs` | Replaced emoji with ASCII |

### Systems Verified Working
- **Monster damage multiplier**: Already applied in 2 places in CombatEngine
- **MemorySystem bounds**: MAX_MEMORIES limit properly enforced
- **GoalSystem null handling**: `GetPriorityGoal()` returns safely handled
- **Item.Clone**: Already deep clones Description and MagicProperties
- **PuzzleSystem**: Array indexing and null handling are safe

### Additional Fixes Applied (Session 2)

#### EndingsSystem.cs - Null Safety
| Method | Fix |
|--------|-----|
| `DetermineEnding()` | Added player null check, used `?.` for singleton access |
| `QualifiesForEnhancedTrueEnding()` | Added null checks for story, ocean, grief systems |
| `QualifiesForDissolutionEnding()` | Added null checks for all singleton accesses |

#### NPCSpawnSystem.cs - Duplicate Prevention
| Method | Fix |
|--------|-----|
| `AddRestoredNPC()` | Added duplicate check by ID and Name before adding |

#### MonsterGenerator.cs - Bounds Validation
| Method | Fix |
|--------|-----|
| `GenerateMonster()` | Added `Math.Max(1, Math.Min(100, dungeonLevel))` clamping |
| `GenerateMonsterGroup()` | Added same level bounds clamping |

#### Code Cleanup
| File | Change |
|------|--------|
| `NewsSystem.cs` | Removed unused `_newsFileAnsiOpen`, `_newsFileAsciiOpen` fields |
| `ChurchLocation.cs` | Removed unused `refreshMenu` field |
| `AdvancedMagicShopLocation.cs` | Removed unused `counter` field |

### Remaining Issues (Not Yet Fixed)

#### Medium Priority
| Category | File | Issue |
|----------|------|-------|
| **Combat** | `AdvancedCombatEngine.cs` | Unused fields (`globalPlayerInFight`, `globalBegged`) - set but never read |
| **Combat** | `CombatEngine.cs` | Unused fields (`globalPlayerInFight`, `globalKilled`) - set but never read |
| **Maintenance** | `NPCMaintenanceEngine.cs` | Unused feature flags (set but never read) |

#### Low Priority (Code Quality)
- 567 compiler warnings (down from 570, mostly unused variables, nullable reference types)
- Some methods could use better error handling
- Consider adding XML documentation to public APIs

### New Systems Added (Working)

#### Companion System (`CompanionSystem.cs`)
- 4 companions: Aldric (Tank), Lyris (Damage), Mira (Healer), Vex (Hybrid)
- Combat integration with HP tracking
- Sacrifice mechanic for player death prevention
- Recruitment triggers in Temple (Mira), Prison (Vex), Dungeon (Aldric, Lyris)

#### Grief System (`GriefSystem.cs`)
- Tracks companion deaths with philosophical progression
- Failed resurrection attempts with meaningful messages
- Integrates with Ocean Philosophy system

#### Difficulty System (`DifficultySystem.cs`)
- 4 modes: Easy, Normal, Hard, Nightmare
- Affects XP rates, gold drops, monster damage

#### Achievement System (`AchievementSystem.cs`)
- 50+ achievements across 6 categories
- Persistent tracking via save system

#### Statistics System (`StatisticsSystem.cs`)
- Combat stats, economic activity, exploration tracking
- Session time, streaks, win rates

### Architecture Notes for Future Work

1. **Singleton Pattern**: Many systems use `Instance` pattern - ensure initialization order in `GameEngine.cs`
2. **Save/Load**: All new systems need serialization support in `SaveDataStructures.cs`
3. **Null Safety**: Always use `?.` when accessing singleton instances or player references
4. **Terminal Output**: Use `terminal.WriteLine(text, color)` pattern, not raw Console calls

---

## Comprehensive Code Audit (Jan 5, 2026)

Full codebase audit covering every system, integration, story point, companion, NPC, equipment, boss fight, seal, and line of code.

### Executive Summary

**Total Issues Found: 180+**
- **Critical (P0)**: 28 issues - Must fix before alpha
- **High Priority (P1)**: 45 issues - Should fix before beta
- **Medium Priority (P2)**: 62 issues - Technical debt
- **Low Priority (P3)**: 45+ issues - Code quality

### Issue Breakdown by System

#### 1. Core Game Systems (GameEngine, Character, Player, NPC, SaveSystem)

| Severity | File:Line | Issue |
|----------|-----------|-------|
| **P0** | GameEngine.cs:31 | Singleton not thread-safe (`instance ??= new GameEngine()`) |
| **P0** | GameEngine.cs:105 | Fire-and-forget async in `_Ready()` swallows exceptions |
| **P0** | GameEngine.cs:393, 2089 | `terminal.WaitForKey()` not awaited - race condition |
| **P0** | GameEngine.cs:1845 | `Environment.Exit(0)` without ensuring saves complete |
| **P0** | Character.cs:716 | Dictionary modification during iteration potential |
| **P0** | Player.cs:73-88 | `ConfigManager.GetConfig()` returns null, crashes on access |
| **P0** | SaveSystem.cs:429-651 | `ActiveStatuses`, `MKills/PKills` not serialized |
| **P1** | GameEngine.cs:153-155 | `worldSimulator.StartSimulation()` called before `worldNPCs` initialized |
| **P1** | Character.cs:1004-1008 | Duplicate `Defense`/`Defence` properties |
| **P1** | Player.cs:9, 41-47 | Multiple properties shadow base class |
| **P1** | NPC.cs:706-727 | Silent exception swallow in `UpdateLocation()` |
| **P1** | SaveSystem.cs:806-810, 944-1024 | Many serialization methods return empty |

#### 2. Combat Systems (CombatEngine, MonsterGenerator, SpellSystem)

| Severity | File:Line | Issue |
|----------|-----------|-------|
| **P1** | CombatEngine.cs | Unused fields `globalPlayerInFight`, `globalKilled` - set never read |
| **P1** | AdvancedCombatEngine.cs | Unused feature flags |
| **P1** | MonsterAbilities.cs | Special abilities defined but not all integrated |
| **P2** | CombatEngine.cs:460, 474 | Uses `dark_gray`, `dark_red` colors not fully supported |

#### 3. Old God Boss System

| Severity | File:Line | Issue |
|----------|-----------|-------|
| **CRITICAL** | OldGodsData.cs:153-154 | Veloura (Floor 40) NOT in dungeon - DungeonFloor=0 |
| **CRITICAL** | OldGodsData.cs:258-259 | Thorgrim (Floor 55) NOT in dungeon - DungeonFloor=0 |
| **CRITICAL** | OldGodsData.cs:349-350 | Noctura (Floor 70) NOT in dungeon - DungeonFloor=0 |
| **CRITICAL** | OldGodsData.cs:460-461 | Aurelion (Floor 85) NOT in dungeon - DungeonFloor=0 |
| **CRITICAL** | DungeonLocation.cs:2156-2164 | Only 3/7 gods have dungeon encounters |
| **HIGH** | OldGodsData.cs:57, 566 | Maelketh floor 60 vs spec 25, Terravok floor 80 vs spec 95 |
| **HIGH** | DungeonLocation.cs:6272-6279 | Artifact floors inconsistent with god locations |
| **MEDIUM** | OldGodBossSystem.cs:554-555 | Special abilities not implemented from SpecialMechanics dict |
| **MEDIUM** | All gods | ThemeColor never set, defaults to "white" |

**Gods Status:**
| God | Spec Floor | Actual | In Dungeon? | Beatable? |
|-----|-----------|--------|-------------|-----------|
| Maelketh | 25 | 60 | YES | YES |
| Veloura | 40 | 0 | **NO** | **NO** |
| Thorgrim | 55 | 0 | **NO** | **NO** |
| Noctura | 70 | 0 | **NO** | **NO** |
| Aurelion | 85 | 0 | **NO** | **NO** |
| Terravok | 95 | 80 | YES | YES |
| Manwe | 100 | 100 | YES | YES |

#### 4. Companion System

| Severity | File:Line | Issue |
|----------|-----------|-------|
| **CRITICAL** | InnLocation.cs | Aldric has NO recruitment trigger - "Tavern bandits" event missing |
| **CRITICAL** | SaveSystem.cs:1055-1170 | Companion state NEVER saved/loaded |
| **MEDIUM** | CompanionSystem.cs | Personal quests defined but never triggered |
| **MEDIUM** | SaveSystem.cs:1069-1074 | GriefSystem only saves stage, not full state |

**Companion Recruitment Status:**
| Companion | Location | Implemented? |
|-----------|----------|--------------|
| Lyris | Dungeon Level 15 | ✓ DungeonLocation.cs:3504-3636 |
| Aldric | Tavern (bandits) | ✗ NOT IMPLEMENTED |
| Mira | Temple Ruins | ✓ TempleLocation.cs:1787-1989 |
| Vex | Prison | ✓ PrisonLocation.cs:635-873 |

#### 5. Quest & Achievement Systems

| Severity | File:Line | Issue |
|----------|-----------|-------|
| **HIGH** | QuestSystem.cs:471-490 | Quest validation bypasses objectives for dungeon quests |
| **HIGH** | Quest.cs:206-210 | `AreAllObjectivesComplete()` ignores `IsOptional` flag |
| **MEDIUM** | QuestSystem.cs:477 | Monster quest uses global `MKills` counter, not quest-specific |
| **MEDIUM** | StatisticsSystem.cs | `QuestsCompleted` field never updated by QuestSystem |
| **LOW** | AchievementSystem.cs | Only 47 achievements (target: 50+) |
| **LOW** | N/A | Daily Challenges NOT implemented |
| **LOW** | N/A | Dedicated Bounty System NOT implemented |

**Seals & Artifacts Verified:**
- 7 Seals: Temple(0), 15, 30, 45, 60, 80, 99 ✓
- 7 Artifacts: 25, 40, 50, 60, 70, 80, 100 (VoidKey auto-triggers)

#### 6. NPC/AI Systems

| Severity | File:Line | Issue |
|----------|-----------|-------|
| **P1** | NPC.cs:666-676 | `GetPlayersInLocation()`, `GetNPCsInLocation()` always return empty |
| **P1** | NPC.cs:835-889 | Multiple `AddRelationship()` and `ChangeActivity()` methods are stubs |
| **P1** | RelationshipManager.cs | `GetRelationshipWith()` may return null without caller checks |
| **P2** | WorldSimulator.cs | Creates `new Random()` every update cycle |
| **P2** | ClassicNPCs.cs | 60 NPCs defined but spawning limited to subset |

#### 7. Romance/Social Systems

| Severity | File:Line | Issue |
|----------|-----------|-------|
| **P0** | RelationshipSystem.cs:487-491 | `UpdateKillStats()` called but method doesn't exist |
| **P1** | PersonalityProfile.cs:476, 482 | Spelling: `IsMalePresentinig`, `IsFemalePresentinig` |
| **P1** | PersonalityProfile.cs:643 | Spelling: `IsLikelyToBetrray` |
| **P1** | IntimacySystem.cs:134, 141 | Spelling: `AnnouncePregancy` |
| **P1** | RomanceTracker.cs:254-337 | `ProcessJealousyConsequences(player=null)` used without null check |
| **P2** | LoveCornerLocation.cs:302-307 | Marriage doesn't call `RelationshipSystem.PerformMarriage()` |
| **P2** | LoveCornerLocation.cs:511-528 | Uses hardcoded sample couples, not real data |

**Placeholder Implementations:**
- `RelationshipSystem.HandleChildCustodyAfterDivorce()` - stub
- `TeamSystem.CheckInventory()` - stub
- `NewsSystem.ArchiveOldNewsFiles()` - stub
- `LoveCornerLocation`: 5 features marked "future update"

#### 8. UI/Terminal Systems

| Severity | File:Line | Issue |
|----------|-----------|-------|
| **P0** | TerminalEmulator.cs:498-501 | `GetKeyInput()` reads full line, not single key (inconsistent with ConsoleTerminal) |
| **P1** | TerminalEmulator.cs:295-311 | Missing `dark_*` color mappings used in codebase |
| **P1** | TerminalEmulator.cs:27-52 | Missing `brown` color used in ANSIArt.cs |
| **P2** | TerminalEmulator.cs:701-711 | `.Result` blocking calls can cause deadlocks |
| **P2** | Multiple locations | Inconsistent menu formatting |

#### 9. Story Systems

| Severity | File:Line | Issue |
|----------|-----------|-------|
| **P1** | EndingsSystem.cs:62-71 | Missing save flags for Maelketh/Thorgrim in ending calculation |
| **P1** | StoryProgressionSystem.cs:59-134 | Only 3 gods have DungeonFloor set |
| **P2** | No CycleSystem.cs | New Game+ not implemented |

#### 10. Equipment/Economy

| Severity | File:Line | Issue |
|----------|-----------|-------|
| **P2** | Character.cs:989-994 | `GetEquippedItemName()` returns placeholder |
| **P2** | EquipmentData.cs | 120+ weapons defined but shop integration limited |
| **P2** | MarketplaceSystem.cs | NPC trading not fully integrated |

### Quick Reference: Must-Fix Before Alpha

~~1. **Old Gods**: Add dungeon encounters for Veloura, Thorgrim, Noctura, Aurelion~~ ✅ FIXED - All 7 gods have DungeonFloor set (25, 40, 55, 70, 85, 95, 100)
~~2. **Companions**: Add Aldric recruitment trigger, implement save/load~~ ✅ FIXED - Aldric in InnLocation.cs, companions serialized in SaveSystem
~~3. **SaveSystem**: Serialize combat stamina, status effects, kill statistics~~ ✅ FIXED - ActiveStatuses, MKills/PKills all serialized
~~4. **Thread Safety**: Use `Lazy<T>` for all singletons~~ ✅ FIXED - GameEngine uses Lazy<T>
~~5. **Quests**: Fix `ValidateQuestCompletion()` to check objectives~~ ✅ FIXED - Respects IsOptional flag
~~6. **Terminal**: Fix `GetKeyInput()` to return single key~~ ✅ VERIFIED - Returns first char from input
~~7. **Spelling Fixes**: `IsMalePresentinig`, `IsLikelyToBetrray`, `AnnouncePregancy`~~ ✅ VERIFIED - All spelled correctly
~~8. **Missing Method**: Implement `RelationshipSystem.UpdateKillStats()`~~ ✅ FIXED - Implemented in LegacyCompat.cs

### Additional Fixes Applied (Jan 2026)

- **GameEngine.cs**: Added try-catch around fire-and-forget async in `_Ready()`
- **LoveCornerLocation.cs**: Fixed `HandleMarry()` to call `RelationshipSystem.PerformMarriage()`
- **LoveCornerLocation.cs**: Fixed `HandleMarriedCouples()` to use `RelationshipSystem.GetMarriedCouples()` instead of hardcoded data
- **NPC.cs**: Added `override`/`new` keywords for inherited members (CurrentLocation, Location, IsNPC, ControlsTurf, TeamPassword)
- **Player.cs**: Added `new` keywords for shadowed properties (RealName, LastLogin, Achievements, TurnsRemaining, etc.)
- **Quest.cs**: Added `QuestTarget.DefeatNPC` enum value for bounty system
- **EndingsSystem.cs**: Added Maelketh/Thorgrim god flags to ending calculation
- **RelationshipSystem.cs**: Implemented `HandleChildCustodyAfterDivorce()` - properly transfers children to parent1
- **NewsSystem.cs**: Implemented `ArchiveOldNewsFiles()` - trims in-memory news to prevent unbounded growth
- **TeamSystem.cs**: Implemented `CheckInventory()` - calls `RecalculateStats()` for equipment bonuses
- **CombatEngine.cs**: Removed unused `globalKilled`, `globalPlayerInFight` fields
- **AdvancedCombatEngine.cs**: Removed unused `globalPlayerInFight`, fixed `globalBegged` to properly prevent double-begging
- **OnlineDuelSystem.cs**: Fixed to use `NewsSystem.Instance.Newsy()` instead of removed field
- **GameEngine.cs**: Removed unused `config`, `multiNodeMode`, `kingRecord`, `scoreManager` fields
- **MagicShopLocation.cs**: Removed unused `currentPlayer`, `shopInventory` fields
- **NPCMaintenanceEngine.cs**: Commented out unused feature flags
- **OldGodBossSystem.cs**: Fixed to set `bossDefeated = true` when boss HP reaches 0

### Additional Fixes Applied (Jan 5, 2026 - Session 2)

- **WorldSimulator.cs**: Removed "Gym" from location list (GymLocation.cs was deleted)
- **WorldSimulator.cs**: Added NPC respawn system - dead NPCs respawn after 10 simulation ticks with death penalty
- **MainStreetLocation.cs**: Fixed seal floor arrays - Creation seal at floor 0 (Temple), not 15
- **Character.cs**: Fixed array initialization - uses capacity instead of pre-filling with defaults
- **ObjType.cs**: Renamed namespace enum to `ItemCategory` to avoid confusion with global ObjType

### Remaining Technical Debt (P2/P3)

1. **Character.cs**: Duplicate `Defense`/`Defence` properties (intentional alias)
2. **OldGodBossSystem.cs**: `bossDefeated` field assigned but value not yet used in logic
3. **Build now at 0 errors, 0 warnings** (down from 539)

### Recommendations

1. ~~**Implement proper singleton pattern** with `Lazy<T>` for all singleton classes~~ ✅ Done
2. **Add comprehensive null checks** or use nullable reference types (`#nullable enable`)
3. ~~**Complete save serialization** for all player data fields~~ ✅ Done
4. ~~**Add async exception handling** with proper logging~~ ✅ Done for GameEngine
5. **Consolidate duplicate properties** and establish naming conventions
6. **Add unit tests** for save/load round-trips
7. **Replace static mutable state** with dependency injection
8. ~~**Wire up all 7 Old Gods** to dungeon floor encounters~~ ✅ Done
9. ~~**Complete companion serialization** - call Serialize/Deserialize in SaveSystem~~ ✅ Done
10. ~~**Fix quest validation** to properly check objective completion~~ ✅ Done
