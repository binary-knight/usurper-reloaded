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

## Recent Changes (Dec 2024)

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
