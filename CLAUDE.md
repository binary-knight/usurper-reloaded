# Claude Instructions for Usurper Remake

## Project Overview
This is a remake of "Usurper: Halls of Avarice", a classic BBS door game from the early 1990s. The remake is built in C# targeting .NET 6.0, with optional Godot integration for GUI mode. The game runs primarily in console/terminal mode.

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
- `TerminalEmulator.cs` - Main UI class for both console and Godot modes
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
