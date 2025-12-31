using UsurperRemake.Utils;
using UsurperRemake.Systems;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Dungeon Location - Room-based exploration with atmosphere and tension
/// Features: Procedural floors, room navigation, feature interaction, combat, events
/// </summary>
public class DungeonLocation : BaseLocation
{
    private List<Character> teammates = new();
    private int currentDungeonLevel = 1;
    private int maxDungeonLevel = 100;
    private Random dungeonRandom = new Random();
    private DungeonTerrain currentTerrain = DungeonTerrain.Underground;

    // Legacy encounter chances (for old ExploreLevel fallback)
    private const float MonsterEncounterChance = 0.90f;
    private const float SpecialEventChance = 0.10f;

    // Current floor state
    private DungeonFloor currentFloor;
    private bool inRoomMode = false; // Are we exploring a room?

    // Player state tracking for tension
    private int consecutiveMonsterRooms = 0;
    private int roomsExploredThisFloor = 0;
    private bool hasRestThisFloor = false;

    public DungeonLocation() : base(
        GameLocation.Dungeons,
        "The Dungeons",
        "You stand before the entrance to the ancient dungeons. Dark passages lead deep into the earth."
    ) { }

    protected override void SetupLocation()
    {
        base.SetupLocation();

        var playerLevel = GetCurrentPlayer()?.Level ?? 1;
        currentDungeonLevel = Math.Max(1, playerLevel);

        if (currentDungeonLevel > maxDungeonLevel)
            currentDungeonLevel = maxDungeonLevel;

        // Generate initial floor if we don't have one
        if (currentFloor == null || currentFloor.Level != currentDungeonLevel)
        {
            currentFloor = DungeonGenerator.GenerateFloor(currentDungeonLevel);
            roomsExploredThisFloor = 0;
            hasRestThisFloor = false;
        }
    }
    
    protected override void DisplayLocation()
    {
        terminal.ClearScreen();

        // Get current room
        var room = currentFloor?.GetCurrentRoom();

        if (room != null && inRoomMode)
        {
            DisplayRoomView(room);
        }
        else
        {
            DisplayFloorOverview();
        }
    }

    /// <summary>
    /// Display when player is in a specific room - the main exploration view
    /// </summary>
    private void DisplayRoomView(DungeonRoom room)
    {
        var player = GetCurrentPlayer();

        // Room header with theme color
        terminal.SetColor(GetThemeColor(currentFloor.Theme));
        terminal.WriteLine($"╔{new string('═', 55)}╗");
        terminal.WriteLine($"║  {room.Name.PadRight(52)} ║");
        terminal.WriteLine($"╚{new string('═', 55)}╝");

        // Show danger indicators
        ShowDangerIndicators(room);

        terminal.WriteLine("");

        // Room description
        terminal.SetColor("white");
        terminal.WriteLine(room.Description);
        terminal.WriteLine("");

        // Atmospheric text (builds tension)
        terminal.SetColor("gray");
        terminal.WriteLine(room.AtmosphereText);
        terminal.WriteLine("");

        // Show what's in the room
        ShowRoomContents(room);

        // Show exits
        ShowExits(room);

        // Show room actions
        ShowRoomActions(room);

        // Quick status bar
        ShowQuickStatus(player);
    }

    private void ShowDangerIndicators(DungeonRoom room)
    {
        terminal.SetColor("darkgray");
        terminal.Write($"Level {currentDungeonLevel} | ");

        // Show floor theme
        terminal.SetColor(GetThemeColor(currentFloor.Theme));
        terminal.Write($"{currentFloor.Theme} | ");

        // Danger rating
        terminal.SetColor(room.DangerRating >= 3 ? "red" : room.DangerRating >= 2 ? "yellow" : "green");
        terminal.Write($"Danger: ");
        for (int i = 0; i < room.DangerRating; i++) terminal.Write("*");
        for (int i = room.DangerRating; i < 3; i++) terminal.Write(".");

        // Room status
        if (room.IsCleared)
        {
            terminal.SetColor("green");
            terminal.Write(" [CLEARED]");
        }
        else if (room.HasMonsters)
        {
            terminal.SetColor("red");
            terminal.Write(" [DANGER]");
        }

        if (room.IsBossRoom)
        {
            terminal.SetColor("bright_red");
            terminal.Write(" [BOSS]");
        }

        terminal.WriteLine("");
    }

    private void ShowRoomContents(DungeonRoom room)
    {
        bool hasAnything = false;

        // Monsters present (not yet cleared)
        if (room.HasMonsters && !room.IsCleared)
        {
            terminal.SetColor("red");
            if (room.IsBossRoom)
            {
                terminal.WriteLine(">> A powerful presence dominates this room! <<");
            }
            else
            {
                var monsterHints = new[]
                {
                    "Shadows move at the edge of your torchlight...",
                    "You hear hostile sounds from the darkness...",
                    "Something is watching you from the shadows...",
                    "The air feels thick with menace..."
                };
                terminal.WriteLine(monsterHints[dungeonRandom.Next(monsterHints.Length)]);
            }
            hasAnything = true;
        }

        // Treasure
        if (room.HasTreasure && !room.TreasureLooted)
        {
            terminal.SetColor("yellow");
            terminal.WriteLine(">> Something valuable glints in the darkness! <<");
            hasAnything = true;
        }

        // Trap (hidden hint)
        if (room.HasTrap && !room.TrapTriggered && dungeonRandom.NextDouble() < 0.3)
        {
            terminal.SetColor("magenta");
            terminal.WriteLine(">> You sense hidden danger... <<");
            hasAnything = true;
        }

        // Event
        if (room.HasEvent && !room.EventCompleted)
        {
            terminal.SetColor("cyan");
            terminal.WriteLine(GetEventHint(room.EventType));
            hasAnything = true;
        }

        // Stairs
        if (room.HasStairsDown)
        {
            terminal.SetColor("blue");
            terminal.WriteLine(">> Stairs lead down to a deeper level <<");
            hasAnything = true;
        }

        // Features to examine
        if (room.Features.Any(f => !f.IsInteracted))
        {
            terminal.SetColor("cyan");
            terminal.WriteLine("");
            terminal.WriteLine("You notice:");
            foreach (var feature in room.Features.Where(f => !f.IsInteracted))
            {
                terminal.Write("  - ");
                terminal.SetColor("white");
                terminal.WriteLine(feature.Name);
                terminal.SetColor("cyan");
            }
            hasAnything = true;
        }

        if (hasAnything)
            terminal.WriteLine("");
    }

    private string GetEventHint(DungeonEventType eventType)
    {
        return eventType switch
        {
            DungeonEventType.TreasureChest => ">> An old chest sits in the corner <<",
            DungeonEventType.Merchant => ">> You see a figure by a small campfire <<",
            DungeonEventType.Shrine => ">> A strange altar radiates energy <<",
            DungeonEventType.NPCEncounter => ">> Someone else is here <<",
            DungeonEventType.Puzzle => ">> Strange mechanisms cover one wall <<",
            DungeonEventType.RestSpot => ">> This area seems relatively safe <<",
            DungeonEventType.MysteryEvent => ">> Something unusual catches your eye <<",
            _ => ">> Something interesting is here <<"
        };
    }

    private void ShowExits(DungeonRoom room)
    {
        terminal.SetColor("white");
        terminal.WriteLine("Exits:");

        foreach (var exit in room.Exits)
        {
            var targetRoom = currentFloor.GetRoom(exit.Value.TargetRoomId);
            var dirKey = GetDirectionKey(exit.Key);

            terminal.SetColor("darkgray");
            terminal.Write("  [");
            terminal.SetColor("bright_cyan");
            terminal.Write(dirKey);
            terminal.SetColor("darkgray");
            terminal.Write("] ");

            terminal.SetColor("gray");
            terminal.Write(exit.Value.Description);

            // Show target room status
            if (targetRoom != null)
            {
                if (targetRoom.IsCleared)
                {
                    terminal.SetColor("green");
                    terminal.Write(" (cleared)");
                }
                else if (targetRoom.IsExplored)
                {
                    terminal.SetColor("yellow");
                    terminal.Write(" (explored)");
                }
                else
                {
                    terminal.SetColor("darkgray");
                    terminal.Write(" (unknown)");
                }
            }

            terminal.WriteLine("");
        }
        terminal.WriteLine("");
    }

    private void ShowRoomActions(DungeonRoom room)
    {
        terminal.SetColor("white");
        terminal.WriteLine("Actions:");

        // Fight monsters
        if (room.HasMonsters && !room.IsCleared)
        {
            terminal.SetColor("darkgray");
            terminal.Write("  [");
            terminal.SetColor("red");
            terminal.Write("F");
            terminal.SetColor("darkgray");
            terminal.Write("] ");
            terminal.SetColor("white");
            terminal.WriteLine("Fight the monsters");
        }

        // Search for treasure
        if (room.HasTreasure && !room.TreasureLooted && room.IsCleared)
        {
            terminal.SetColor("darkgray");
            terminal.Write("  [");
            terminal.SetColor("yellow");
            terminal.Write("T");
            terminal.SetColor("darkgray");
            terminal.Write("] ");
            terminal.SetColor("white");
            terminal.WriteLine("Collect treasure");
        }

        // Interact with event
        if (room.HasEvent && !room.EventCompleted)
        {
            terminal.SetColor("darkgray");
            terminal.Write("  [");
            terminal.SetColor("cyan");
            terminal.Write("E");
            terminal.SetColor("darkgray");
            terminal.Write("] ");
            terminal.SetColor("white");
            terminal.WriteLine("Investigate the event");
        }

        // Examine features
        if (room.Features.Any(f => !f.IsInteracted))
        {
            terminal.SetColor("darkgray");
            terminal.Write("  [");
            terminal.SetColor("magenta");
            terminal.Write("X");
            terminal.SetColor("darkgray");
            terminal.Write("] ");
            terminal.SetColor("white");
            terminal.WriteLine("Examine features");
        }

        // Use stairs
        if (room.HasStairsDown && room.IsCleared)
        {
            terminal.SetColor("darkgray");
            terminal.Write("  [");
            terminal.SetColor("blue");
            terminal.Write("D");
            terminal.SetColor("darkgray");
            terminal.Write("] ");
            terminal.SetColor("white");
            terminal.WriteLine("Descend stairs");
        }

        // Rest (if safe)
        if (room.IsCleared && !hasRestThisFloor)
        {
            terminal.SetColor("darkgray");
            terminal.Write("  [");
            terminal.SetColor("green");
            terminal.Write("R");
            terminal.SetColor("darkgray");
            terminal.Write("] ");
            terminal.SetColor("white");
            terminal.WriteLine("Rest and recover");
        }

        // General options
        terminal.SetColor("darkgray");
        terminal.Write("  [");
        terminal.SetColor("cyan");
        terminal.Write("M");
        terminal.SetColor("darkgray");
        terminal.Write("] ");
        terminal.SetColor("white");
        terminal.Write("Map  ");

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("cyan");
        terminal.Write("I");
        terminal.SetColor("darkgray");
        terminal.Write("] ");
        terminal.SetColor("white");
        terminal.Write("Inventory  ");

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("cyan");
        terminal.Write("P");
        terminal.SetColor("darkgray");
        terminal.Write("] ");
        terminal.SetColor("white");
        terminal.Write("Potions  ");

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("red");
        terminal.Write("Q");
        terminal.SetColor("darkgray");
        terminal.Write("] ");
        terminal.SetColor("white");
        terminal.WriteLine("Leave dungeon");

        terminal.WriteLine("");
    }

    private void ShowQuickStatus(Character player)
    {
        terminal.SetColor("darkgray");
        terminal.Write(new string('─', 57));
        terminal.WriteLine("");

        // Health bar
        terminal.SetColor("white");
        terminal.Write("HP: ");
        DrawBar(player.HP, player.MaxHP, 20, "red", "darkgray");
        terminal.Write($" {player.HP}/{player.MaxHP}");

        terminal.Write("  ");

        // Potions
        terminal.SetColor("green");
        terminal.Write($"Potions: {player.Healing}/{player.MaxPotions}");

        terminal.Write("  ");

        // Gold
        terminal.SetColor("yellow");
        terminal.Write($"Gold: {player.Gold:N0}");

        terminal.WriteLine("");
    }

    private void DrawBar(long current, long max, int width, string fillColor, string emptyColor)
    {
        int filled = max > 0 ? (int)((current * width) / max) : 0;
        filled = Math.Max(0, Math.Min(width, filled));

        terminal.Write("[");
        terminal.SetColor(fillColor);
        terminal.Write(new string('█', filled));
        terminal.SetColor(emptyColor);
        terminal.Write(new string('░', width - filled));
        terminal.SetColor("white");
        terminal.Write("]");
    }

    private string GetDirectionKey(Direction dir)
    {
        return dir switch
        {
            Direction.North => "N",
            Direction.South => "S",
            Direction.East => "E",
            Direction.West => "W",
            _ => "?"
        };
    }

    private string GetThemeColor(DungeonTheme theme)
    {
        return theme switch
        {
            DungeonTheme.Catacombs => "gray",
            DungeonTheme.Sewers => "green",
            DungeonTheme.Caverns => "cyan",
            DungeonTheme.AncientRuins => "yellow",
            DungeonTheme.DemonLair => "red",
            DungeonTheme.FrozenDepths => "bright_cyan",
            DungeonTheme.VolcanicPit => "bright_red",
            DungeonTheme.AbyssalVoid => "magenta",
            _ => "white"
        };
    }

    /// <summary>
    /// Display floor overview before entering
    /// </summary>
    private void DisplayFloorOverview()
    {
        ShowBreadcrumb();

        terminal.SetColor(GetThemeColor(currentFloor.Theme));
        terminal.WriteLine("╔═══════════════════════════════════════════════════════╗");
        terminal.WriteLine($"║          DUNGEON LEVEL {currentDungeonLevel.ToString().PadLeft(3)}                             ║");
        terminal.WriteLine($"║          Theme: {currentFloor.Theme.ToString().PadRight(20)}            ║");
        terminal.WriteLine("╚═══════════════════════════════════════════════════════╝");
        terminal.WriteLine("");

        // Floor stats
        terminal.SetColor("white");
        terminal.WriteLine($"Rooms: {currentFloor.Rooms.Count}");
        terminal.WriteLine($"Danger Level: {currentFloor.DangerLevel}/10");

        int explored = currentFloor.Rooms.Count(r => r.IsExplored);
        int cleared = currentFloor.Rooms.Count(r => r.IsCleared);
        terminal.WriteLine($"Explored: {explored}/{currentFloor.Rooms.Count}");
        terminal.WriteLine($"Cleared: {cleared}/{currentFloor.Rooms.Count}");
        terminal.WriteLine("");

        // Floor flavor
        terminal.SetColor("gray");
        terminal.WriteLine(GetFloorFlavorText(currentFloor.Theme));
        terminal.WriteLine("");

        // Team info
        terminal.SetColor("cyan");
        terminal.WriteLine($"Your Party: {1 + teammates.Count} member{(teammates.Count > 0 ? "s" : "")}");
        terminal.WriteLine("");

        // Options
        terminal.SetColor("white");
        terminal.WriteLine("[E] Enter the dungeon");
        terminal.WriteLine("[T] Team management");
        terminal.WriteLine("[S] Your status");
        terminal.WriteLine("[L] Change level (+/- 10)");
        terminal.WriteLine("[Q] Return to town");
        terminal.WriteLine("");
    }

    private string GetFloorFlavorText(DungeonTheme theme)
    {
        return theme switch
        {
            DungeonTheme.Catacombs => "Ancient burial grounds stretch before you. The dead do not rest easy here.",
            DungeonTheme.Sewers => "The stench is overwhelming. Things lurk in the fetid waters below.",
            DungeonTheme.Caverns => "Natural caves twist into darkness. Bioluminescent life casts eerie shadows.",
            DungeonTheme.AncientRuins => "The ruins of a forgotten civilization. Magic still lingers in these stones.",
            DungeonTheme.DemonLair => "Hell has bled into this place. Tortured screams echo endlessly.",
            DungeonTheme.FrozenDepths => "Impossible cold. Your breath freezes. Things are preserved in the ice.",
            DungeonTheme.VolcanicPit => "Rivers of magma light the way. The heat is almost unbearable.",
            DungeonTheme.AbyssalVoid => "Reality breaks down here. What lurks beyond sanity itself?",
            _ => "Darkness awaits."
        };
    }

    protected override string GetBreadcrumbPath()
    {
        var room = currentFloor?.GetCurrentRoom();
        if (room != null && inRoomMode)
        {
            return $"Dungeons → Level {currentDungeonLevel} → {room.Name}";
        }
        return $"Main Street → Dungeons → Level {currentDungeonLevel}";
    }

    protected override async Task<bool> ProcessChoice(string choice)
    {
        if (string.IsNullOrWhiteSpace(choice))
            return false;

        var upperChoice = choice.ToUpper().Trim();

        // Different handling based on whether we're in room mode or floor overview
        if (inRoomMode)
        {
            return await ProcessRoomChoice(upperChoice);
        }
        else
        {
            return await ProcessOverviewChoice(upperChoice);
        }
    }

    /// <summary>
    /// Process input when viewing floor overview
    /// </summary>
    private async Task<bool> ProcessOverviewChoice(string choice)
    {
        switch (choice)
        {
            case "E":
                // Enter the dungeon - go to first room
                inRoomMode = true;
                currentFloor.CurrentRoomId = currentFloor.EntranceRoomId;
                var entranceRoom = currentFloor.GetCurrentRoom();
                if (entranceRoom != null)
                {
                    entranceRoom.IsExplored = true;
                    roomsExploredThisFloor++;
                }
                terminal.WriteLine("You enter the dungeon...", "gray");
                await Task.Delay(1500);

                // Rare encounter check on dungeon entry
                var player = GetCurrentPlayer();
                if (player != null)
                {
                    await RareEncounters.TryRareEncounter(
                        terminal,
                        player,
                        currentFloor.Theme,
                        currentDungeonLevel
                    );
                }
                return false;

            case "T":
                await ManageTeam();
                return false;

            case "S":
                await ShowStatus();
                return false;

            case "L":
                await ChangeDungeonLevel();
                return false;

            case "Q":
                await NavigateToLocation(GameLocation.MainStreet);
                return true;

            default:
                terminal.WriteLine("Invalid choice.", "red");
                await Task.Delay(1000);
                return false;
        }
    }

    /// <summary>
    /// Process input when exploring a room
    /// </summary>
    private async Task<bool> ProcessRoomChoice(string choice)
    {
        var room = currentFloor.GetCurrentRoom();
        if (room == null) return false;

        // Check for directional movement
        var direction = choice switch
        {
            "N" => Direction.North,
            "S" => Direction.South,
            "E" when !room.HasEvent || room.EventCompleted => Direction.East, // E is also event
            "W" => Direction.West,
            _ => (Direction?)null
        };

        if (direction.HasValue && room.Exits.ContainsKey(direction.Value))
        {
            await MoveToRoom(room.Exits[direction.Value].TargetRoomId);
            return false;
        }

        // Action-based commands
        switch (choice)
        {
            case "F":
                if (room.HasMonsters && !room.IsCleared)
                {
                    await FightRoomMonsters(room);
                }
                return false;

            case "T":
                if (room.HasTreasure && !room.TreasureLooted && room.IsCleared)
                {
                    await CollectTreasure(room);
                }
                return false;

            case "E":
                if (room.HasEvent && !room.EventCompleted)
                {
                    await HandleRoomEvent(room);
                }
                return false;

            case "X":
                if (room.Features.Any(f => !f.IsInteracted))
                {
                    await ExamineFeatures(room);
                }
                return false;

            case "D":
                if (room.HasStairsDown && room.IsCleared)
                {
                    await DescendStairs();
                }
                return false;

            case "R":
                if (room.IsCleared && !hasRestThisFloor)
                {
                    await RestInRoom();
                }
                return false;

            case "M":
                await ShowDungeonMap();
                return false;

            case "I":
                await ShowStatus();
                return false;

            case "P":
                await UsePotions();
                return false;

            case "Q":
                // Leave dungeon
                inRoomMode = false;
                return false;

            default:
                terminal.WriteLine("Invalid choice. Use direction keys (N/S/E/W) or action keys.", "red");
                await Task.Delay(1000);
                return false;
        }
    }

    /// <summary>
    /// Move to another room
    /// </summary>
    private async Task MoveToRoom(string targetRoomId)
    {
        var targetRoom = currentFloor.GetRoom(targetRoomId);
        if (targetRoom == null) return;

        // Moving transition
        terminal.ClearScreen();
        terminal.SetColor("gray");
        terminal.WriteLine("You move through the passage...");
        await Task.Delay(800);

        // Check for trap on entering unexplored room
        if (!targetRoom.IsExplored && targetRoom.HasTrap && !targetRoom.TrapTriggered)
        {
            await TriggerTrap(targetRoom);
        }

        // Update current room
        currentFloor.CurrentRoomId = targetRoomId;

        if (!targetRoom.IsExplored)
        {
            targetRoom.IsExplored = true;
            roomsExploredThisFloor++;

            // Room discovery message
            terminal.SetColor(GetThemeColor(currentFloor.Theme));
            terminal.WriteLine($"You enter: {targetRoom.Name}");
            await Task.Delay(500);

            // Rare encounter check on first visit to a room
            var player = GetCurrentPlayer();
            if (player != null)
            {
                bool hadEncounter = await RareEncounters.TryRareEncounter(
                    terminal,
                    player,
                    currentFloor.Theme,
                    currentDungeonLevel
                );

                if (hadEncounter)
                {
                    // Give a brief pause after rare encounter before showing room
                    await Task.Delay(500);
                }
            }
        }

        // If room has monsters and player enters, auto-engage (ambush chance)
        if (targetRoom.HasMonsters && !targetRoom.IsCleared)
        {
            consecutiveMonsterRooms++;

            if (dungeonRandom.NextDouble() < 0.3 && !targetRoom.IsBossRoom)
            {
                terminal.SetColor("red");
                terminal.WriteLine("AMBUSH! The monsters attack!");
                await Task.Delay(1000);
                await FightRoomMonsters(targetRoom);
            }
        }
        else
        {
            consecutiveMonsterRooms = 0;
        }
    }

    /// <summary>
    /// Trigger a trap when entering a room
    /// </summary>
    private async Task TriggerTrap(DungeonRoom room)
    {
        room.TrapTriggered = true;
        var player = GetCurrentPlayer();

        terminal.SetColor("red");
        terminal.WriteLine("*** TRAP! ***");
        await Task.Delay(500);

        var trapType = dungeonRandom.Next(6);
        switch (trapType)
        {
            case 0:
                var pitDmg = currentDungeonLevel * 3 + dungeonRandom.Next(10);
                player.HP -= pitDmg;
                terminal.WriteLine($"The floor gives way! You fall into a pit for {pitDmg} damage!");
                break;

            case 1:
                var dartDmg = currentDungeonLevel * 2 + dungeonRandom.Next(8);
                player.HP -= dartDmg;
                player.Poison = Math.Max(player.Poison, 1);
                terminal.WriteLine($"Poison darts! You take {dartDmg} damage and are poisoned!");
                break;

            case 2:
                var fireDmg = currentDungeonLevel * 4 + dungeonRandom.Next(12);
                player.HP -= fireDmg;
                terminal.WriteLine($"A gout of flame! You take {fireDmg} fire damage!");
                break;

            case 3:
                var goldLost = player.Gold / 10;
                player.Gold -= goldLost;
                terminal.WriteLine($"Acid sprays your belongings! You lose {goldLost} gold!");
                break;

            case 4:
                var expLost = currentDungeonLevel * 50;
                player.Experience = Math.Max(0, player.Experience - expLost);
                terminal.WriteLine($"A curse drains you! You lose {expLost} experience!");
                break;

            case 5:
                terminal.SetColor("green");
                terminal.WriteLine("The trap mechanism is broken. Nothing happens!");
                long bonusGold = currentDungeonLevel * 20;
                player.Gold += bonusGold;
                terminal.WriteLine($"You salvage {bonusGold} gold from the trap parts.");
                break;
        }

        await Task.Delay(2000);
    }

    /// <summary>
    /// Fight the monsters in a room
    /// </summary>
    private async Task FightRoomMonsters(DungeonRoom room)
    {
        var player = GetCurrentPlayer();

        terminal.ClearScreen();
        terminal.SetColor("red");

        if (room.IsBossRoom)
        {
            terminal.WriteLine("╔═══════════════════════════════════════════════════╗");
            terminal.WriteLine("║              *** BOSS ENCOUNTER ***               ║");
            terminal.WriteLine("╚═══════════════════════════════════════════════════╝");
            terminal.WriteLine("");
            terminal.WriteLine(room.Description);
        }
        else
        {
            terminal.WriteLine("═══ COMBAT! ═══");
        }

        terminal.WriteLine("");
        await Task.Delay(1000);

        // Generate monsters appropriate for this room
        var monsters = MonsterGenerator.GenerateMonsterGroup(currentDungeonLevel, dungeonRandom);

        // Make boss room monsters tougher
        if (room.IsBossRoom)
        {
            foreach (var m in monsters)
            {
                m.HP = (long)(m.HP * 1.5);
                m.Strength = (int)(m.Strength * 1.3);
            }
            // Ensure there's a boss
            if (!monsters.Any(m => m.IsBoss))
            {
                monsters[0].IsBoss = true;
                monsters[0].Name = GetBossName(currentFloor.Theme);
            }
        }

        // Display what we're fighting
        if (monsters.Count == 1)
        {
            var monster = monsters[0];
            terminal.SetColor(monster.MonsterColor);
            terminal.WriteLine($"A {monster.Name} attacks!");
        }
        else
        {
            terminal.SetColor("yellow");
            terminal.WriteLine($"You face {monsters.Count} {monsters[0].Name}{(monsters.Count > 1 ? "s" : "")}!");
        }

        terminal.WriteLine("");
        await Task.Delay(1500);

        // Combat
        var combatEngine = new CombatEngine(terminal);
        await combatEngine.PlayerVsMonsters(player, monsters, teammates, offerMonkEncounter: true);

        // Check if player survived
        if (player.HP > 0)
        {
            room.IsCleared = true;
            currentFloor.MonstersKilled += monsters.Count;

            terminal.SetColor("green");
            terminal.WriteLine("The room is cleared!");

            if (room.IsBossRoom)
            {
                currentFloor.BossDefeated = true;
                terminal.SetColor("bright_yellow");
                terminal.WriteLine("*** BOSS DEFEATED! ***");
                terminal.WriteLine("");

                // Bonus rewards for boss
                long bossGold = currentDungeonLevel * 500 + dungeonRandom.Next(1000);
                long bossExp = currentDungeonLevel * 300;
                player.Gold += bossGold;
                player.Experience += bossExp;

                terminal.WriteLine($"Bonus: {bossGold} gold, {bossExp} experience!");
            }

            await Task.Delay(2000);
        }

        await terminal.PressAnyKey();
    }

    private string GetBossName(DungeonTheme theme)
    {
        return theme switch
        {
            DungeonTheme.Catacombs => "Bone Lord",
            DungeonTheme.Sewers => "Sludge Abomination",
            DungeonTheme.Caverns => "Crystal Guardian",
            DungeonTheme.AncientRuins => "Awakened Golem",
            DungeonTheme.DemonLair => "Pit Fiend",
            DungeonTheme.FrozenDepths => "Frost Wyrm",
            DungeonTheme.VolcanicPit => "Magma Elemental",
            DungeonTheme.AbyssalVoid => "Void Horror",
            _ => "Dungeon Boss"
        };
    }

    /// <summary>
    /// Collect treasure from a cleared room
    /// </summary>
    private async Task CollectTreasure(DungeonRoom room)
    {
        var player = GetCurrentPlayer();
        room.TreasureLooted = true;
        currentFloor.TreasuresFound++;

        terminal.ClearScreen();
        terminal.SetColor("yellow");
        terminal.WriteLine("*** TREASURE! ***");
        terminal.WriteLine("");

        // Scale rewards with level
        long goldFound = currentDungeonLevel * 100 + dungeonRandom.Next(currentDungeonLevel * 200);
        long expFound = currentDungeonLevel * 50 + dungeonRandom.Next(100);

        player.Gold += goldFound;
        player.Experience += expFound;

        terminal.WriteLine($"You find {goldFound} gold pieces!");
        terminal.WriteLine($"You gain {expFound} experience!");

        // Chance for bonus items
        if (dungeonRandom.NextDouble() < 0.3)
        {
            int potions = dungeonRandom.Next(1, 3);
            player.Healing = Math.Min(player.MaxPotions, player.Healing + potions);
            terminal.SetColor("green");
            terminal.WriteLine($"You also find {potions} healing potion{(potions > 1 ? "s" : "")}!");
        }

        await Task.Delay(2500);
        await terminal.PressAnyKey();
    }

    /// <summary>
    /// Handle room-specific events
    /// </summary>
    private async Task HandleRoomEvent(DungeonRoom room)
    {
        room.EventCompleted = true;

        switch (room.EventType)
        {
            case DungeonEventType.TreasureChest:
                await TreasureChestEncounter();
                break;
            case DungeonEventType.Merchant:
                await MerchantEncounter();
                break;
            case DungeonEventType.Shrine:
                await MysteriousShrine();
                break;
            case DungeonEventType.NPCEncounter:
                await NPCEncounter();
                break;
            case DungeonEventType.Puzzle:
                await PuzzleEncounter();
                break;
            case DungeonEventType.RestSpot:
                await RestSpotEncounter();
                break;
            case DungeonEventType.MysteryEvent:
                await MysteryEventEncounter();
                break;
            default:
                await RandomDungeonEvent();
                break;
        }
    }

    /// <summary>
    /// Examine room features
    /// </summary>
    private async Task ExamineFeatures(DungeonRoom room)
    {
        var unexamined = room.Features.Where(f => !f.IsInteracted).ToList();
        if (unexamined.Count == 0) return;

        terminal.ClearScreen();
        terminal.SetColor("cyan");
        terminal.WriteLine("What do you want to examine?");
        terminal.WriteLine("");

        for (int i = 0; i < unexamined.Count; i++)
        {
            terminal.SetColor("white");
            terminal.Write($"[{i + 1}] ");
            terminal.SetColor("yellow");
            terminal.Write(unexamined[i].Name);
            terminal.SetColor("gray");
            terminal.WriteLine($" ({unexamined[i].Interaction})");
        }

        terminal.WriteLine("");
        terminal.SetColor("white");
        terminal.WriteLine("[0] Cancel");
        terminal.WriteLine("");

        var input = await terminal.GetInput("Choice: ");

        if (int.TryParse(input, out int idx) && idx >= 1 && idx <= unexamined.Count)
        {
            await InteractWithFeature(unexamined[idx - 1]);
        }
    }

    /// <summary>
    /// Interact with a specific feature
    /// </summary>
    private async Task InteractWithFeature(RoomFeature feature)
    {
        feature.IsInteracted = true;
        var player = GetCurrentPlayer();

        terminal.ClearScreen();
        terminal.SetColor("cyan");
        terminal.WriteLine($"You {feature.Interaction.ToString().ToLower()} the {feature.Name}...");
        terminal.WriteLine("");
        terminal.SetColor("white");
        terminal.WriteLine(feature.Description);
        terminal.WriteLine("");

        await Task.Delay(1000);

        // Random outcome based on interaction type
        var outcome = dungeonRandom.NextDouble();

        switch (feature.Interaction)
        {
            case FeatureInteraction.Examine:
                if (outcome < 0.3)
                {
                    long expGain = currentDungeonLevel * 20;
                    player.Experience += expGain;
                    terminal.SetColor("yellow");
                    terminal.WriteLine($"You learn something useful! +{expGain} experience.");
                }
                else if (outcome < 0.5)
                {
                    terminal.SetColor("gray");
                    terminal.WriteLine("You find nothing of interest.");
                }
                else
                {
                    terminal.SetColor("cyan");
                    terminal.WriteLine("This might be important to remember...");
                }
                break;

            case FeatureInteraction.Open:
                if (outcome < 0.4)
                {
                    long goldFound = currentDungeonLevel * 50 + dungeonRandom.Next(100);
                    player.Gold += goldFound;
                    terminal.SetColor("yellow");
                    terminal.WriteLine($"Inside you find {goldFound} gold!");
                }
                else if (outcome < 0.6)
                {
                    int potions = dungeonRandom.Next(1, 3);
                    player.Healing = Math.Min(player.MaxPotions, player.Healing + potions);
                    terminal.SetColor("green");
                    terminal.WriteLine($"You find {potions} healing potion{(potions > 1 ? "s" : "")}!");
                }
                else if (outcome < 0.75)
                {
                    terminal.SetColor("gray");
                    terminal.WriteLine("It's empty.");
                }
                else
                {
                    var damage = currentDungeonLevel * 2;
                    player.HP -= damage;
                    terminal.SetColor("red");
                    terminal.WriteLine($"A trap! You take {damage} damage!");
                }
                break;

            case FeatureInteraction.Search:
                if (outcome < 0.5)
                {
                    long goldFound = currentDungeonLevel * 30 + dungeonRandom.Next(50);
                    player.Gold += goldFound;
                    terminal.SetColor("yellow");
                    terminal.WriteLine($"Hidden among the debris: {goldFound} gold!");
                }
                else
                {
                    terminal.SetColor("gray");
                    terminal.WriteLine("You find nothing useful.");
                }
                break;

            case FeatureInteraction.Read:
                long expBonus = currentDungeonLevel * 30;
                player.Experience += expBonus;
                terminal.SetColor("cyan");
                terminal.WriteLine($"Ancient knowledge! +{expBonus} experience.");
                break;

            case FeatureInteraction.Take:
                if (outcome < 0.6)
                {
                    long value = currentDungeonLevel * 40 + dungeonRandom.Next(80);
                    player.Gold += value;
                    terminal.SetColor("yellow");
                    terminal.WriteLine($"You pocket something worth {value} gold.");
                }
                else
                {
                    terminal.SetColor("gray");
                    terminal.WriteLine("It crumbles as you touch it. Worthless.");
                }
                break;

            case FeatureInteraction.Use:
                if (outcome < 0.3)
                {
                    player.HP = Math.Min(player.MaxHP, player.HP + player.MaxHP / 4);
                    terminal.SetColor("green");
                    terminal.WriteLine("A healing aura washes over you!");
                }
                else if (outcome < 0.6)
                {
                    long goldBonus = currentDungeonLevel * 100;
                    player.Gold += goldBonus;
                    terminal.SetColor("yellow");
                    terminal.WriteLine($"A hidden cache opens! {goldBonus} gold!");
                }
                else
                {
                    terminal.SetColor("gray");
                    terminal.WriteLine("Nothing happens.");
                }
                break;

            case FeatureInteraction.Break:
                if (outcome < 0.4)
                {
                    long goldFound = currentDungeonLevel * 60 + dungeonRandom.Next(100);
                    player.Gold += goldFound;
                    terminal.SetColor("yellow");
                    terminal.WriteLine($"Valuables spill out! {goldFound} gold!");
                }
                else if (outcome < 0.7)
                {
                    terminal.SetColor("gray");
                    terminal.WriteLine("Just rubble now.");
                }
                else
                {
                    var damage = currentDungeonLevel * 3;
                    player.HP -= damage;
                    terminal.SetColor("red");
                    terminal.WriteLine($"Something explodes! {damage} damage!");
                }
                break;

            case FeatureInteraction.Enter:
                // Secret passage - random bonus room
                terminal.SetColor("cyan");
                terminal.WriteLine("You squeeze through into a hidden space...");
                await Task.Delay(1000);
                if (outcome < 0.5)
                {
                    long treasureGold = currentDungeonLevel * 150 + dungeonRandom.Next(200);
                    long treasureExp = currentDungeonLevel * 75;
                    player.Gold += treasureGold;
                    player.Experience += treasureExp;
                    terminal.SetColor("yellow");
                    terminal.WriteLine($"A secret cache! {treasureGold} gold, {treasureExp} exp!");
                }
                else
                {
                    terminal.SetColor("gray");
                    terminal.WriteLine("Just a dead end with some old bones.");
                }
                break;
        }

        await Task.Delay(2000);
        await terminal.PressAnyKey();
    }

    /// <summary>
    /// Descend to the next floor
    /// </summary>
    private async Task DescendStairs()
    {
        var playerLevel = GetCurrentPlayer()?.Level ?? 1;
        int maxAllowed = Math.Min(maxDungeonLevel, playerLevel + 10);

        if (currentDungeonLevel >= maxAllowed)
        {
            terminal.WriteLine("A mysterious force prevents you from going deeper.", "red");
            terminal.WriteLine("You need to grow stronger first.", "yellow");
            await Task.Delay(2000);
            return;
        }

        terminal.ClearScreen();
        terminal.SetColor("blue");
        terminal.WriteLine("You descend the ancient stairs...");
        terminal.WriteLine("The darkness grows deeper.");
        terminal.WriteLine("The air grows colder.");
        await Task.Delay(2000);

        currentDungeonLevel++;
        currentFloor = DungeonGenerator.GenerateFloor(currentDungeonLevel);
        roomsExploredThisFloor = 0;
        hasRestThisFloor = false;
        consecutiveMonsterRooms = 0;

        // Start in entrance room
        currentFloor.CurrentRoomId = currentFloor.EntranceRoomId;
        var entranceRoom = currentFloor.GetCurrentRoom();
        if (entranceRoom != null)
        {
            entranceRoom.IsExplored = true;
            roomsExploredThisFloor++;
        }

        terminal.SetColor(GetThemeColor(currentFloor.Theme));
        terminal.WriteLine("");
        terminal.WriteLine($"You arrive at Level {currentDungeonLevel}");
        terminal.WriteLine($"Theme: {currentFloor.Theme}");
        terminal.WriteLine("");
        terminal.SetColor("gray");
        terminal.WriteLine(GetFloorFlavorText(currentFloor.Theme));

        await Task.Delay(2500);
    }

    /// <summary>
    /// Rest in a cleared room (once per floor)
    /// </summary>
    private async Task RestInRoom()
    {
        var player = GetCurrentPlayer();

        terminal.ClearScreen();
        terminal.SetColor("green");
        terminal.WriteLine("You find a defensible corner and rest...");
        terminal.WriteLine("");

        await Task.Delay(1500);

        // Heal 25% of max HP
        long healAmount = player.MaxHP / 4;
        player.HP = Math.Min(player.MaxHP, player.HP + healAmount);

        terminal.WriteLine($"You recover {healAmount} hit points.");
        terminal.WriteLine($"HP: {player.HP}/{player.MaxHP}");

        hasRestThisFloor = true;

        terminal.WriteLine("");
        terminal.SetColor("gray");
        terminal.WriteLine("You feel rested, but dare not linger too long.");

        await Task.Delay(2500);
        await terminal.PressAnyKey();
    }

    /// <summary>
    /// Change dungeon level from overview
    /// </summary>
    private async Task ChangeDungeonLevel()
    {
        var playerLevel = GetCurrentPlayer()?.Level ?? 1;
        int minAllowed = Math.Max(1, playerLevel - 10);
        int maxAllowed = Math.Min(maxDungeonLevel, playerLevel + 10);

        terminal.WriteLine("");
        terminal.WriteLine($"Current level: {currentDungeonLevel}", "white");
        terminal.WriteLine($"Your level: {playerLevel}", "cyan");
        terminal.WriteLine($"Accessible range: {minAllowed} - {maxAllowed} (±10 from your level)", "yellow");
        terminal.WriteLine("");

        var input = await terminal.GetInput("Enter target level (or +/- for relative): ");

        int targetLevel = currentDungeonLevel;

        if (input.StartsWith("+") && int.TryParse(input.Substring(1), out int plus))
        {
            targetLevel = currentDungeonLevel + plus;
        }
        else if (input.StartsWith("-") && int.TryParse(input.Substring(1), out int minus))
        {
            targetLevel = currentDungeonLevel - minus;
        }
        else if (int.TryParse(input, out int absolute))
        {
            targetLevel = absolute;
        }

        // Clamp to allowed range
        targetLevel = Math.Max(minAllowed, Math.Min(maxAllowed, targetLevel));

        if (targetLevel != currentDungeonLevel)
        {
            currentDungeonLevel = targetLevel;
            currentFloor = DungeonGenerator.GenerateFloor(currentDungeonLevel);
            roomsExploredThisFloor = 0;
            hasRestThisFloor = false;
            consecutiveMonsterRooms = 0;

            terminal.WriteLine($"Dungeon level set to {currentDungeonLevel}.", "green");
        }
        else
        {
            terminal.WriteLine("No change to dungeon level.", "gray");
        }

        await Task.Delay(1500);
    }
    
    /// <summary>
    /// Main exploration mechanic - Pascal encounter system
    /// </summary>
    private async Task ExploreLevel()
    {
        var currentPlayer = GetCurrentPlayer();

        // No turn/fight limits in the new persistent system - explore freely!

        terminal.ClearScreen();
        terminal.SetColor("yellow");
        terminal.WriteLine("═══ EXPLORING ═══");
        terminal.WriteLine("");

        // Atmospheric exploration text
        await ShowExplorationText();

        // Determine encounter type: 90% monsters, 10% special events
        var encounterRoll = dungeonRandom.NextDouble();

        if (encounterRoll < MonsterEncounterChance)
        {
            await MonsterEncounter();
        }
        else
        {
            await SpecialEventEncounter();
        }

        await Task.Delay(1000);
        await terminal.PressAnyKey();
    }
    
    /// <summary>
    /// Monster encounter - Pascal DUNGEVC.PAS mechanics
    /// </summary>
    private async Task MonsterEncounter()
    {
        terminal.SetColor("red");
        terminal.WriteLine("▼ MONSTER ENCOUNTER ▼");
        terminal.WriteLine("");

        // Use new MonsterGenerator to create level-appropriate monsters
        var monsters = MonsterGenerator.GenerateMonsterGroup(currentDungeonLevel, dungeonRandom);

        var combatEngine = new CombatEngine(terminal);

        // Display encounter message with color
        if (monsters.Count == 1)
        {
            var monster = monsters[0];
            if (monster.IsBoss)
            {
                terminal.SetColor("bright_red");
                terminal.WriteLine($"⚠ A powerful [{monster.MonsterColor}]{monster.Name}[/] blocks your path! ⚠");
            }
            else
            {
                terminal.SetColor(monster.MonsterColor);
                terminal.WriteLine($"A [{monster.MonsterColor}]{monster.Name}[/] appears!");
            }
        }
        else
        {
            terminal.SetColor("yellow");
            terminal.Write($"You encounter a group of [{monsters[0].MonsterColor}]{monsters.Count} {monsters[0].Name}");
            if (monsters[0].FamilyName != "")
            {
                terminal.Write($"[/] from the {monsters[0].FamilyName} family!");
            }
            else
            {
                terminal.Write("[/]!");
            }
            terminal.WriteLine("");
        }

        terminal.WriteLine("");
        await Task.Delay(2000);

        // Use new PlayerVsMonsters method - ALL monsters fight at once!
        // Monk will appear after ALL monsters are defeated
        await combatEngine.PlayerVsMonsters(GetCurrentPlayer(), monsters, teammates, offerMonkEncounter: true);
    }
    
    /// <summary>
    /// Magic scroll encounter - Pascal scroll mechanics
    /// </summary>
    private async Task HandleMagicScroll()
    {
        terminal.WriteLine("You have found a scroll! It reads:");
        terminal.WriteLine("");
        
        var scrollType = dungeonRandom.Next(3);
        var currentPlayer = GetCurrentPlayer();
        
        switch (scrollType)
        {
            case 0: // Blessing scroll
                terminal.SetColor("bright_white");
                terminal.WriteLine("Utter: 'XAVARANTHE JHUSULMAX VASWIUN'");
                terminal.WriteLine("And you will receive a blessing.");
                break;
                
            case 1: // Undead summon scroll  
                terminal.SetColor("red");
                terminal.WriteLine("Utter: 'ZASHNIVANTHE ULIPMAN NO SEE'");
                terminal.WriteLine("And you will see ancient power rise again.");
                break;
                
            case 2: // Secret cave scroll
                terminal.SetColor("cyan");
                terminal.WriteLine("Utter: 'RANTVANTHI SHGELUUIM VARTHMIOPLXH'");
                terminal.WriteLine("And you will be given opportunities.");
                break;
        }
        
        terminal.WriteLine("");
        var recite = await terminal.GetInput("Recite the scroll? (Y/N): ");
        
        if (recite.ToUpper() == "Y")
        {
            await ExecuteScrollMagic(scrollType, currentPlayer);
        }
        else
        {
            terminal.WriteLine("You carefully store the scroll for later.", "gray");
        }
    }
    
    /// <summary>
    /// Execute scroll magic effects
    /// </summary>
    private async Task ExecuteScrollMagic(int scrollType, Character player)
    {
        terminal.WriteLine("");
        terminal.WriteLine("The ancient words resonate with power...", "bright_white");
        await Task.Delay(2000);
        
        switch (scrollType)
        {
            case 0: // Blessing
                {
                    long chivalryGain = dungeonRandom.Next(500) + 50;
                    player.Chivalry += chivalryGain;
                    
                    terminal.SetColor("bright_yellow");
                    terminal.WriteLine("Divine light surrounds you!");
                    terminal.WriteLine($"Your chivalry increases by {chivalryGain}!");
                }
                break;
                
            case 1: // Undead summon (triggers combat)
                {
                    terminal.SetColor("red");
                    terminal.WriteLine("The ground trembles as ancient evil awakens!");
                    await Task.Delay(2000);
                    
                    // Create undead monster
                    var undead = CreateUndeadMonster();
                    terminal.WriteLine($"You have summoned a {undead.Name}!");
                    
                    // Fight the undead
                    var combatEngine = new CombatEngine(terminal);
                    await combatEngine.PlayerVsMonster(player, undead, teammates);
                }
                break;
                
            case 2: // Secret opportunity
                {
                    terminal.SetColor("bright_cyan");
                    terminal.WriteLine("A hidden passage opens before you!");
                    
                    long bonusGold = currentDungeonLevel * 2000;
                    player.Gold += bonusGold;
                    
                    terminal.WriteLine($"You discover {bonusGold} gold in the secret chamber!");
                }
                break;
        }
    }
    
    /// <summary>
    /// Special event encounters - Based on Pascal DUNGEVC.PAS and DUNGEV2.PAS
    /// Includes positive, negative, and neutral events for variety
    /// </summary>
    private async Task SpecialEventEncounter()
    {
        // 12 different event types based on original Pascal
        var eventType = dungeonRandom.Next(12);

        switch (eventType)
        {
            case 0:
                await TreasureChestEncounter();
                break;
            case 1:
                await PotionCacheEncounter();
                break;
            case 2:
                await MerchantEncounter();
                break;
            case 3:
                await WitchDoctorEncounter();
                break;
            case 4:
                await BeggarEncounter();
                break;
            case 5:
                await StrangersEncounter();
                break;
            case 6:
                await HarassedWomanEncounter();
                break;
            case 7:
                await WoundedManEncounter();
                break;
            case 8:
                await MysteriousShrine();
                break;
            case 9:
                await TrapEncounter();
                break;
            case 10:
                await AncientScrollEncounter();
                break;
            case 11:
                await GamblingGhostEncounter();
                break;
        }
    }

    /// <summary>
    /// Treasure chest encounter - Classic Pascal treasure mechanics
    /// </summary>
    private async Task TreasureChestEncounter()
    {
        terminal.SetColor("yellow");
        terminal.WriteLine("★ TREASURE CHEST ★");
        terminal.WriteLine("");

        terminal.WriteLine("You discover an ancient chest hidden in the shadows!", "cyan");
        terminal.WriteLine("");

        var choice = await terminal.GetInput("(O)pen the chest or (L)eave it alone? ");

        var currentPlayer = GetCurrentPlayer();

        if (choice.ToUpper() == "O")
        {
            // 70% good, 20% trap, 10% mimic
            var chestRoll = dungeonRandom.Next(10);

            if (chestRoll < 7)
            {
                // Good treasure!
                long goldFound = currentDungeonLevel * 1500 + dungeonRandom.Next(5000);
                long expGained = currentDungeonLevel * 200 + dungeonRandom.Next(3000);

                terminal.SetColor("green");
                terminal.WriteLine("The chest opens to reveal glittering treasure!");
                terminal.WriteLine($"You find {goldFound} gold pieces!");
                terminal.WriteLine($"You gain {expGained} experience!");

                currentPlayer.Gold += goldFound;
                currentPlayer.Experience += expGained;
            }
            else if (chestRoll < 9)
            {
                // Trap!
                terminal.SetColor("red");
                terminal.WriteLine("CLICK! It's a trap!");

                var trapType = dungeonRandom.Next(3);
                switch (trapType)
                {
                    case 0:
                        var poisonDmg = currentDungeonLevel * 5;
                        currentPlayer.HP -= poisonDmg;
                        terminal.WriteLine($"Poison gas! You take {poisonDmg} damage!");
                        currentPlayer.Poison = Math.Max(currentPlayer.Poison, 1);
                        terminal.WriteLine("You have been poisoned!", "magenta");
                        break;
                    case 1:
                        var spikeDmg = currentDungeonLevel * 8;
                        currentPlayer.HP -= spikeDmg;
                        terminal.WriteLine($"Spikes shoot out! You take {spikeDmg} damage!");
                        break;
                    case 2:
                        var goldLost = currentPlayer.Gold / 10;
                        currentPlayer.Gold -= goldLost;
                        terminal.WriteLine($"Acid sprays your coin pouch! You lose {goldLost} gold!");
                        break;
                }
            }
            else
            {
                // Mimic! (triggers combat)
                terminal.SetColor("bright_red");
                terminal.WriteLine("The chest MOVES! It's a MIMIC!");
                await Task.Delay(1500);

                var mimic = Monster.CreateMonster(
                    currentDungeonLevel, "Mimic",
                    currentDungeonLevel * 15, currentDungeonLevel * 4, 0,
                    "Fooled you!", false, false, "Teeth", "Wooden Shell",
                    false, false, currentDungeonLevel * 5, currentDungeonLevel * 3, currentDungeonLevel * 3
                );
                mimic.IsBoss = true;
                mimic.Level = currentDungeonLevel;

                var combatEngine = new CombatEngine(terminal);
                await combatEngine.PlayerVsMonster(currentPlayer, mimic, teammates);
            }
        }
        else
        {
            terminal.WriteLine("You wisely leave the chest alone and continue on.", "gray");
        }

        await Task.Delay(2000);
    }

    /// <summary>
    /// Strangers encounter - Band of rogues/orcs (from Pascal DUNGEV2.PAS)
    /// </summary>
    private async Task StrangersEncounter()
    {
        terminal.SetColor("red");
        terminal.WriteLine("⚔ STRANGERS APPROACH ⚔");
        terminal.WriteLine("");

        var currentPlayer = GetCurrentPlayer();
        var groupType = dungeonRandom.Next(4);
        string groupName;
        string[] memberTypes;

        switch (groupType)
        {
            case 0:
                groupName = "orcs";
                memberTypes = new[] { "Orc", "Half-Orc", "Orc Raider" };
                terminal.WriteLine("A group of orcs emerges from the shadows!", "gray");
                terminal.WriteLine("They are poorly armed with sticks and clubs.", "gray");
                break;
            case 1:
                groupName = "trolls";
                memberTypes = new[] { "Troll", "Half-Troll", "Lumber-Troll" };
                terminal.WriteLine("A band of trolls blocks your path!", "green");
                terminal.WriteLine("They carry clubs and spears.", "gray");
                break;
            case 2:
                groupName = "rogues";
                memberTypes = new[] { "Rogue", "Thief", "Pirate" };
                terminal.WriteLine("A gang of rogues surrounds you!", "cyan");
                terminal.WriteLine("They brandish knives and rapiers.", "gray");
                break;
            default:
                groupName = "dwarves";
                memberTypes = new[] { "Dwarf", "Dwarf Warrior", "Dwarf Scout" };
                terminal.WriteLine("A group of armed dwarves approaches!", "yellow");
                terminal.WriteLine("They carry swords and axes.", "gray");
                break;
        }

        terminal.WriteLine("");
        terminal.WriteLine("Their leader demands your gold!", "white");
        terminal.WriteLine("");

        var choice = await terminal.GetInput("(F)ight them, (P)ay them off, or try to (E)scape? ");

        if (choice.ToUpper() == "F")
        {
            terminal.WriteLine("You draw your weapon and prepare for battle!", "yellow");
            await Task.Delay(1500);

            // Create the group
            int groupSize = dungeonRandom.Next(3, 6);
            var monsters = new List<Monster>();

            for (int i = 0; i < groupSize; i++)
            {
                var name = memberTypes[dungeonRandom.Next(memberTypes.Length)];
                if (i == 0) name = name + " Leader";

                var monster = Monster.CreateMonster(
                    currentDungeonLevel, name,
                    currentDungeonLevel * (i == 0 ? 8 : 4),
                    currentDungeonLevel * 2, 0,
                    "Attack!", false, false, "Weapon", "Armor",
                    false, false, currentDungeonLevel * 2, currentDungeonLevel, currentDungeonLevel * 2
                );
                monster.Level = currentDungeonLevel;
                if (i == 0) monster.IsBoss = true;
                monsters.Add(monster);
            }

            var combatEngine = new CombatEngine(terminal);
            await combatEngine.PlayerVsMonsters(currentPlayer, monsters, teammates, offerMonkEncounter: false);
        }
        else if (choice.ToUpper() == "P")
        {
            long bribe = currentDungeonLevel * 500 + dungeonRandom.Next(1000);
            if (currentPlayer.Gold >= bribe)
            {
                currentPlayer.Gold -= bribe;
                terminal.WriteLine($"You reluctantly hand over {bribe} gold.", "yellow");
                terminal.WriteLine($"The {groupName} leave you in peace.", "gray");
            }
            else
            {
                terminal.WriteLine("You don't have enough gold!", "red");
                terminal.WriteLine("They attack anyway!", "red");
                await Task.Delay(1500);
                // Trigger simplified combat
                var monster = Monster.CreateMonster(
                    currentDungeonLevel, $"{groupName.Substring(0, 1).ToUpper()}{groupName.Substring(1)} Leader",
                    currentDungeonLevel * 10, currentDungeonLevel * 3, 0,
                    "No gold means death!", false, false, "Weapon", "Armor",
                    false, false, currentDungeonLevel * 3, currentDungeonLevel * 2, currentDungeonLevel * 2
                );
                var combatEngine = new CombatEngine(terminal);
                await combatEngine.PlayerVsMonster(currentPlayer, monster, teammates);
            }
        }
        else
        {
            // Escape attempt - 60% chance
            if (dungeonRandom.NextDouble() < 0.6)
            {
                terminal.WriteLine("You manage to slip away into the shadows!", "green");
            }
            else
            {
                terminal.WriteLine("They catch you trying to escape!", "red");
                long stolen = currentPlayer.Gold / 5;
                currentPlayer.Gold -= stolen;
                terminal.WriteLine($"They beat you and steal {stolen} gold!", "red");
            }
        }

        await Task.Delay(2000);
    }

    /// <summary>
    /// Harassed woman encounter - Moral choice event (from Pascal DUNGEVC.PAS)
    /// </summary>
    private async Task HarassedWomanEncounter()
    {
        terminal.SetColor("magenta");
        terminal.WriteLine("♀ DAMSEL IN DISTRESS ♀");
        terminal.WriteLine("");

        terminal.WriteLine("You hear screams echoing through the corridor!", "white");
        terminal.WriteLine("A woman is being harassed by a band of ruffians.", "gray");
        terminal.WriteLine("");

        var choice = await terminal.GetInput("(H)elp her, (I)gnore the situation, or (J)oin the ruffians? ");

        var currentPlayer = GetCurrentPlayer();

        if (choice.ToUpper() == "H")
        {
            terminal.WriteLine("You rush to her defense!", "green");
            terminal.WriteLine("\"Unhand her, villains!\"", "yellow");
            await Task.Delay(1500);

            // Fight ruffians
            var monsters = new List<Monster>();
            int count = dungeonRandom.Next(2, 4);
            for (int i = 0; i < count; i++)
            {
                var name = i == 0 ? "Ruffian Leader" : "Ruffian";
                var monster = Monster.CreateMonster(
                    currentDungeonLevel, name,
                    currentDungeonLevel * (i == 0 ? 6 : 3), currentDungeonLevel * 2, 0,
                    "Mind your own business!", false, false, "Knife", "Rags",
                    false, false, currentDungeonLevel * 2, currentDungeonLevel, currentDungeonLevel
                );
                monster.Level = currentDungeonLevel;
                monsters.Add(monster);
            }

            var combatEngine = new CombatEngine(terminal);
            await combatEngine.PlayerVsMonsters(currentPlayer, monsters, teammates, offerMonkEncounter: false);

            if (currentPlayer.HP > 0)
            {
                terminal.WriteLine("");
                terminal.WriteLine("The woman thanks you profusely!", "cyan");
                long reward = currentDungeonLevel * 300 + dungeonRandom.Next(500);
                long chivGain = dungeonRandom.Next(50) + 30;

                terminal.WriteLine($"She rewards you with {reward} gold!", "yellow");
                terminal.WriteLine($"Your chivalry increases by {chivGain}!", "white");

                currentPlayer.Gold += reward;
                currentPlayer.Chivalry += chivGain;
            }
        }
        else if (choice.ToUpper() == "J")
        {
            terminal.SetColor("red");
            terminal.WriteLine("You join the ruffians in their villainy!");
            terminal.WriteLine("This is a shameful act!");

            long stolen = dungeonRandom.Next(200) + 50;
            long darkGain = dungeonRandom.Next(75) + 50;

            currentPlayer.Gold += stolen;
            currentPlayer.Darkness += darkGain;

            terminal.WriteLine($"You steal {stolen} gold from the woman.", "yellow");
            terminal.WriteLine($"Your darkness increases by {darkGain}!", "magenta");
        }
        else
        {
            terminal.WriteLine("You turn away and pretend not to notice.", "gray");
            terminal.WriteLine("Her cries fade as you continue your journey...", "gray");
        }

        await Task.Delay(2500);
    }

    /// <summary>
    /// Wounded man encounter - Healing quest (from Pascal DUNGEVC.PAS)
    /// </summary>
    private async Task WoundedManEncounter()
    {
        terminal.SetColor("cyan");
        terminal.WriteLine("✚ WOUNDED STRANGER ✚");
        terminal.WriteLine("");

        terminal.WriteLine("You find a wounded man lying against the wall.", "white");
        terminal.WriteLine("He is bleeding heavily and begs for help.", "gray");
        terminal.WriteLine("\"Please... I need healing... I can pay...\"", "yellow");
        terminal.WriteLine("");

        var choice = await terminal.GetInput("(H)elp him, (R)ob him, or (L)eave him? ");

        var currentPlayer = GetCurrentPlayer();

        if (choice.ToUpper() == "H")
        {
            if (currentPlayer.Healing > 0)
            {
                currentPlayer.Healing--;
                terminal.WriteLine("You use a healing potion on the wounded stranger.", "green");
                terminal.WriteLine("He recovers enough to stand.", "white");

                long reward = currentDungeonLevel * 500 + dungeonRandom.Next(1000);
                long chivGain = dungeonRandom.Next(40) + 20;

                terminal.WriteLine($"\"Thank you, hero! Take this reward: {reward} gold!\"", "yellow");
                terminal.WriteLine($"Your chivalry increases by {chivGain}!", "white");

                currentPlayer.Gold += reward;
                currentPlayer.Chivalry += chivGain;
            }
            else
            {
                terminal.WriteLine("You have no healing potions to spare!", "red");
                terminal.WriteLine("You try to bandage his wounds with cloth...", "gray");

                if (dungeonRandom.NextDouble() < 0.5)
                {
                    terminal.WriteLine("It seems to help a little.", "green");
                    currentPlayer.Chivalry += 10;
                }
                else
                {
                    terminal.WriteLine("Unfortunately, he dies from his wounds.", "red");
                }
            }
        }
        else if (choice.ToUpper() == "R")
        {
            terminal.SetColor("red");
            terminal.WriteLine("You search the dying man's belongings...");

            long stolen = dungeonRandom.Next(500) + 100;
            long darkGain = dungeonRandom.Next(80) + 60;

            terminal.WriteLine($"You find {stolen} gold in his purse.", "yellow");
            terminal.WriteLine($"Your darkness increases by {darkGain}!", "magenta");

            currentPlayer.Gold += stolen;
            currentPlayer.Darkness += darkGain;

            terminal.WriteLine("He dies cursing your name...", "gray");
        }
        else
        {
            terminal.WriteLine("You step over the dying man and continue on.", "gray");
            terminal.WriteLine("His moans fade behind you...", "gray");
        }

        await Task.Delay(2500);
    }

    /// <summary>
    /// Mysterious shrine - Random buff or debuff
    /// </summary>
    private async Task MysteriousShrine()
    {
        terminal.SetColor("bright_cyan");
        terminal.WriteLine("✦ MYSTERIOUS SHRINE ✦");
        terminal.WriteLine("");

        terminal.WriteLine("You discover an ancient shrine glowing with strange light.", "white");
        terminal.WriteLine("Offerings of gold and bones surround a stone altar.", "gray");
        terminal.WriteLine("");

        var choice = await terminal.GetInput("(P)ray at the shrine, (D)esecrate it, or (L)eave? ");

        var currentPlayer = GetCurrentPlayer();

        if (choice.ToUpper() == "P")
        {
            terminal.WriteLine("You kneel before the ancient shrine...", "cyan");
            await Task.Delay(1500);

            // Random blessing or curse
            var outcome = dungeonRandom.Next(6);
            switch (outcome)
            {
                case 0:
                    terminal.WriteLine("Divine light fills you!", "bright_yellow");
                    currentPlayer.HP = currentPlayer.MaxHP;
                    terminal.WriteLine("You are fully healed!", "green");
                    break;
                case 1:
                    var strBonus = dungeonRandom.Next(5) + 1;
                    currentPlayer.Strength += strBonus;
                    terminal.WriteLine($"You feel stronger! +{strBonus} Strength!", "green");
                    break;
                case 2:
                    var expBonus = currentDungeonLevel * 500;
                    currentPlayer.Experience += expBonus;
                    terminal.WriteLine($"Ancient wisdom flows into you! +{expBonus} EXP!", "yellow");
                    break;
                case 3:
                    terminal.WriteLine("The shrine is silent...", "gray");
                    terminal.WriteLine("Nothing happens.", "gray");
                    break;
                case 4:
                    var hpLoss = currentPlayer.HP / 4;
                    currentPlayer.HP -= hpLoss;
                    terminal.WriteLine("The shrine drains your life force!", "red");
                    terminal.WriteLine($"You lose {hpLoss} HP!", "red");
                    break;
                case 5:
                    var goldLoss = currentPlayer.Gold / 5;
                    currentPlayer.Gold -= goldLoss;
                    terminal.WriteLine("Your gold dissolves into the altar!", "red");
                    terminal.WriteLine($"You lose {goldLoss} gold!", "red");
                    break;
            }
        }
        else if (choice.ToUpper() == "D")
        {
            terminal.SetColor("red");
            terminal.WriteLine("You smash the shrine and steal the offerings!");

            long stolen = currentDungeonLevel * 200 + dungeonRandom.Next(500);
            long darkGain = dungeonRandom.Next(50) + 30;

            terminal.WriteLine($"You find {stolen} gold among the offerings!", "yellow");
            terminal.WriteLine($"Your darkness increases by {darkGain}!", "magenta");

            currentPlayer.Gold += stolen;
            currentPlayer.Darkness += darkGain;

            // Chance of angering spirits
            if (dungeonRandom.NextDouble() < 0.3)
            {
                terminal.WriteLine("");
                terminal.WriteLine("An angry spirit emerges from the ruined shrine!", "bright_red");
                await Task.Delay(1500);

                var spirit = Monster.CreateMonster(
                    currentDungeonLevel + 5, "Vengeful Spirit",
                    currentDungeonLevel * 12, currentDungeonLevel * 4, 0,
                    "You will pay for your sacrilege!", false, false, "Spectral Claws", "Ethereal Form",
                    false, false, currentDungeonLevel * 4, currentDungeonLevel * 3, currentDungeonLevel * 3
                );
                spirit.IsBoss = true;

                var combatEngine = new CombatEngine(terminal);
                await combatEngine.PlayerVsMonster(currentPlayer, spirit, teammates);
            }
        }
        else
        {
            terminal.WriteLine("You wisely leave the mysterious shrine alone.", "gray");
        }

        await Task.Delay(2000);
    }

    /// <summary>
    /// Trap encounter - Various dungeon hazards
    /// </summary>
    private async Task TrapEncounter()
    {
        terminal.SetColor("red");
        terminal.WriteLine("⚠ TRAP! ⚠");
        terminal.WriteLine("");

        var currentPlayer = GetCurrentPlayer();
        var trapType = dungeonRandom.Next(5);

        switch (trapType)
        {
            case 0:
                terminal.WriteLine("The floor gives way beneath you!", "white");
                var fallDmg = currentDungeonLevel * 3 + dungeonRandom.Next(10);
                currentPlayer.HP -= fallDmg;
                terminal.WriteLine($"You fall into a pit and take {fallDmg} damage!", "red");
                break;
            case 1:
                terminal.WriteLine("Poison darts shoot from the walls!", "white");
                var dartDmg = currentDungeonLevel * 2 + dungeonRandom.Next(8);
                currentPlayer.HP -= dartDmg;
                currentPlayer.Poison = Math.Max(currentPlayer.Poison, 1);
                terminal.WriteLine($"You take {dartDmg} damage and are poisoned!", "magenta");
                break;
            case 2:
                terminal.WriteLine("A magical rune explodes beneath your feet!", "bright_magenta");
                var runeDmg = currentDungeonLevel * 5 + dungeonRandom.Next(15);
                currentPlayer.HP -= runeDmg;
                terminal.WriteLine($"You take {runeDmg} magical damage!", "red");
                break;
            case 3:
                terminal.WriteLine("A net falls from above, trapping you!", "white");
                terminal.WriteLine("You struggle free, but lose time...", "gray");
                // Could implement time/turn penalty here
                break;
            case 4:
                terminal.WriteLine("You trigger a tripwire!", "white");
                terminal.WriteLine("But nothing happens... the trap is broken.", "green");
                terminal.WriteLine("You find some gold hidden near the mechanism.", "yellow");
                currentPlayer.Gold += currentDungeonLevel * 50;
                break;
        }

        await Task.Delay(2500);
    }

    /// <summary>
    /// Ancient scroll encounter - Magic scroll discovery
    /// </summary>
    private async Task AncientScrollEncounter()
    {
        terminal.SetColor("bright_cyan");
        terminal.WriteLine("📜 ANCIENT SCROLL 📜");
        terminal.WriteLine("");

        terminal.WriteLine("You discover an ancient scroll tucked into a wall crevice.", "white");
        terminal.WriteLine("Strange symbols glow faintly on the parchment.", "gray");

        await HandleMagicScroll();
    }

    /// <summary>
    /// Gambling ghost encounter - Risk/reward minigame
    /// </summary>
    private async Task GamblingGhostEncounter()
    {
        terminal.SetColor("bright_white");
        terminal.WriteLine("👻 GAMBLING GHOST 👻");
        terminal.WriteLine("");

        terminal.WriteLine("A spectral figure materializes before you!", "cyan");
        terminal.WriteLine("\"Greetings, mortal! Care for a game of chance?\"", "yellow");
        terminal.WriteLine("The ghost produces a pair of ethereal dice.", "gray");
        terminal.WriteLine("");

        var currentPlayer = GetCurrentPlayer();
        long minBet = 100;
        long maxBet = currentPlayer.Gold / 2;

        if (currentPlayer.Gold < minBet)
        {
            terminal.WriteLine("\"Bah! You have no gold worth gambling for!\"", "yellow");
            terminal.WriteLine("The ghost fades away in disappointment.", "gray");
            await Task.Delay(2000);
            return;
        }

        terminal.WriteLine($"\"Place your bet! (Minimum {minBet}, Maximum {maxBet})\"", "yellow");
        var betStr = await terminal.GetInput("Your bet (or 0 to decline): ");

        if (!long.TryParse(betStr, out long bet) || bet < minBet || bet > maxBet)
        {
            terminal.WriteLine("\"Coward! Perhaps next time...\"", "yellow");
            terminal.WriteLine("The ghost fades away.", "gray");
            await Task.Delay(2000);
            return;
        }

        terminal.WriteLine($"You bet {bet} gold!", "white");
        terminal.WriteLine("The ghost rolls the dice...", "gray");
        await Task.Delay(1500);

        var ghostRoll = dungeonRandom.Next(1, 7) + dungeonRandom.Next(1, 7);
        terminal.WriteLine($"Ghost rolls: {ghostRoll}", "cyan");

        terminal.WriteLine("Your turn to roll...", "gray");
        await Task.Delay(1000);

        var playerRoll = dungeonRandom.Next(1, 7) + dungeonRandom.Next(1, 7);
        terminal.WriteLine($"You roll: {playerRoll}", "yellow");

        if (playerRoll > ghostRoll)
        {
            terminal.SetColor("green");
            terminal.WriteLine("YOU WIN!");
            terminal.WriteLine($"The ghost begrudgingly pays you {bet} gold!", "yellow");
            currentPlayer.Gold += bet;
        }
        else if (playerRoll < ghostRoll)
        {
            terminal.SetColor("red");
            terminal.WriteLine("YOU LOSE!");
            terminal.WriteLine($"The ghost cackles as your gold vanishes!", "yellow");
            currentPlayer.Gold -= bet;
        }
        else
        {
            terminal.SetColor("yellow");
            terminal.WriteLine("TIE!");
            terminal.WriteLine("\"Interesting... keep your gold, mortal. Until next time!\"", "yellow");
        }

        terminal.WriteLine("The ghost fades into the shadows...", "gray");
        await Task.Delay(2500);
    }

    /// <summary>
    /// Potion cache encounter - find random potions
    /// </summary>
    private async Task PotionCacheEncounter()
    {
        terminal.SetColor("bright_green");
        terminal.WriteLine("✚ POTION CACHE ✚");
        terminal.WriteLine("");

        var currentPlayer = GetCurrentPlayer();

        // Random potion messages
        string[] messages = new[]
        {
            "You discover an abandoned healer's satchel!",
            "A fallen adventurer's pack contains healing supplies!",
            "You find a monk's abandoned cache of potions!",
            "A hidden alcove reveals a stash of healing elixirs!",
            "The corpse of a cleric clutches a bag of potions!"
        };

        terminal.WriteLine(messages[dungeonRandom.Next(messages.Length)], "cyan");
        terminal.WriteLine("");

        // Give 1-5 potions, but don't exceed max
        int potionsFound = dungeonRandom.Next(1, 6);
        int currentPotions = (int)currentPlayer.Healing;
        int maxPotions = currentPlayer.MaxPotions;
        int roomAvailable = maxPotions - currentPotions;

        if (roomAvailable <= 0)
        {
            terminal.WriteLine("You already have the maximum number of potions!", "yellow");
            terminal.WriteLine("You leave the potions for another adventurer.", "gray");
        }
        else
        {
            int actualGained = Math.Min(potionsFound, roomAvailable);
            currentPlayer.Healing += actualGained;

            terminal.SetColor("green");
            terminal.WriteLine($"You collect {actualGained} healing potion{(actualGained > 1 ? "s" : "")}!");
            terminal.WriteLine($"Potions: {currentPlayer.Healing}/{currentPlayer.MaxPotions}", "cyan");

            if (actualGained < potionsFound)
            {
                terminal.WriteLine($"(You had to leave {potionsFound - actualGained} potion{(potionsFound - actualGained > 1 ? "s" : "")} behind - at maximum capacity)", "gray");
            }
        }

        await Task.Delay(2500);
    }
    
    /// <summary>
    /// Merchant encounter - Pascal DUNGEV2.PAS
    /// </summary>
    private async Task MerchantEncounter()
    {
        var player = GetCurrentPlayer();

        terminal.ClearScreen();
        terminal.SetColor("green");
        terminal.WriteLine("╔═══════════════════════════════════════════════════════╗");
        terminal.WriteLine("║            ♦ TRAVELING MERCHANT ♦                     ║");
        terminal.WriteLine("╚═══════════════════════════════════════════════════════╝");
        terminal.WriteLine("");
        terminal.SetColor("white");
        terminal.WriteLine("A traveling merchant appears from the shadows!");
        terminal.WriteLine("\"Greetings, brave adventurer! Care to trade?\"");
        terminal.WriteLine("");

        var choice = await terminal.GetInput("(T)rade with merchant or (A)ttack for goods? ");

        if (choice.ToUpper() == "T")
        {
            await MerchantTradeMenu(player);
        }
        else if (choice.ToUpper() == "A")
        {
            terminal.SetColor("red");
            terminal.WriteLine("You decide to rob the poor merchant!");
            terminal.WriteLine("");
            await Task.Delay(1000);

            // Create merchant monster for combat
            var merchant = CreateMerchantMonster();
            var combatEngine = new CombatEngine(terminal);
            var result = await combatEngine.PlayerVsMonster(player, merchant, teammates);

            if (result.Outcome == CombatOutcome.Victory)
            {
                // Loot the merchant
                long loot = currentDungeonLevel * 100 + dungeonRandom.Next(200);
                player.Gold += loot;
                player.Healing = Math.Min(player.MaxPotions, player.Healing + 3);
                terminal.SetColor("yellow");
                terminal.WriteLine($"You loot {loot} gold and 3 healing potions from the merchant!");
            }

            // Evil deed
            player.Darkness += 10;
            terminal.SetColor("red");
            terminal.WriteLine("+10 Darkness for attacking an innocent merchant!");
        }
        else
        {
            terminal.SetColor("gray");
            terminal.WriteLine("The merchant waves goodbye and vanishes into the shadows.");
        }

        await terminal.PressAnyKey();
    }

    private async Task MerchantTradeMenu(Character player)
    {
        int potionPrice = 40 + (currentDungeonLevel * 5);
        int megaPotionPrice = currentDungeonLevel * 100;
        int antidotePrice = 75;

        // Generate rare items based on dungeon level
        var rareItems = GenerateMerchantRareItems(currentDungeonLevel);

        bool trading = true;
        while (trading)
        {
            terminal.ClearScreen();
            terminal.SetColor("green");
            terminal.WriteLine("╔═══════════════════════════════════════════════════════╗");
            terminal.WriteLine("║            MERCHANT'S WARES                           ║");
            terminal.WriteLine("╚═══════════════════════════════════════════════════════╝");
            terminal.WriteLine("");

            terminal.SetColor("yellow");
            terminal.WriteLine($"Your Gold: {player.Gold:N0}");
            terminal.WriteLine($"Your Potions: {player.Healing}/{player.MaxPotions}");
            terminal.SetColor("white");
            terminal.WriteLine($"Weapon Power: {player.WeapPow}  |  Armor Power: {player.ArmPow}");
            terminal.WriteLine("");

            terminal.SetColor("white");
            terminal.WriteLine("═══ SUPPLIES ═══");
            terminal.WriteLine("");

            terminal.SetColor("darkgray");
            terminal.Write("  [");
            terminal.SetColor("green");
            terminal.Write("1");
            terminal.SetColor("darkgray");
            terminal.Write("] ");
            terminal.SetColor("white");
            terminal.WriteLine($"Healing Potion ({potionPrice}g)");

            terminal.SetColor("darkgray");
            terminal.Write("  [");
            terminal.SetColor("bright_green");
            terminal.Write("2");
            terminal.SetColor("darkgray");
            terminal.Write("] ");
            terminal.SetColor("white");
            terminal.WriteLine($"Mega Potion ({megaPotionPrice}g) - Full heal!");

            terminal.SetColor("darkgray");
            terminal.Write("  [");
            terminal.SetColor("cyan");
            terminal.Write("3");
            terminal.SetColor("darkgray");
            terminal.Write("] ");
            terminal.SetColor("white");
            terminal.WriteLine($"Antidote ({antidotePrice}g) - Cures poison");

            terminal.SetColor("darkgray");
            terminal.Write("  [");
            terminal.SetColor("yellow");
            terminal.Write("4");
            terminal.SetColor("darkgray");
            terminal.Write("] ");
            terminal.SetColor("white");
            terminal.WriteLine($"Buy Max Potions ({potionPrice * (player.MaxPotions - player.Healing)}g)");

            terminal.WriteLine("");
            terminal.SetColor("bright_magenta");
            terminal.WriteLine("═══ RARE ITEMS (Dungeon Exclusive!) ═══");
            terminal.WriteLine("");

            for (int i = 0; i < rareItems.Count; i++)
            {
                var item = rareItems[i];
                terminal.SetColor("darkgray");
                terminal.Write("  [");
                terminal.SetColor("bright_magenta");
                terminal.Write($"{(char)('A' + i)}");
                terminal.SetColor("darkgray");
                terminal.Write("] ");
                terminal.SetColor(item.Sold ? "darkgray" : "bright_yellow");
                if (item.Sold)
                {
                    terminal.WriteLine($"{item.Name} - SOLD");
                }
                else
                {
                    terminal.WriteLine($"{item.Name} ({item.Price:N0}g) - {item.Description}");
                }
            }

            terminal.WriteLine("");
            terminal.SetColor("darkgray");
            terminal.Write("  [");
            terminal.SetColor("red");
            terminal.Write("L");
            terminal.SetColor("darkgray");
            terminal.Write("] ");
            terminal.SetColor("white");
            terminal.WriteLine("Leave shop");

            terminal.WriteLine("");
            terminal.SetColor("cyan");
            terminal.Write("Choice: ");
            terminal.SetColor("white");

            var choice = (await terminal.GetInput("")).Trim().ToUpper();

            switch (choice)
            {
                case "1":
                    if (player.Healing >= player.MaxPotions)
                    {
                        terminal.SetColor("yellow");
                        terminal.WriteLine("\"You can't carry any more potions, friend!\"");
                    }
                    else if (player.Gold >= potionPrice)
                    {
                        player.Gold -= potionPrice;
                        player.Healing++;
                        terminal.SetColor("green");
                        terminal.WriteLine($"Purchased 1 healing potion! ({player.Healing}/{player.MaxPotions})");
                    }
                    else
                    {
                        terminal.SetColor("red");
                        terminal.WriteLine("\"Not enough gold, friend.\"");
                    }
                    await Task.Delay(1500);
                    break;

                case "2":
                    if (player.Gold >= megaPotionPrice)
                    {
                        player.Gold -= megaPotionPrice;
                        player.HP = player.MaxHP;
                        terminal.SetColor("bright_green");
                        terminal.WriteLine("You drink the mega potion - FULL HEALTH RESTORED!");
                        terminal.WriteLine($"HP: {player.HP}/{player.MaxHP}");
                    }
                    else
                    {
                        terminal.SetColor("red");
                        terminal.WriteLine("\"Not enough gold, friend.\"");
                    }
                    await Task.Delay(1500);
                    break;

                case "3":
                    if (player.Poison <= 0)
                    {
                        terminal.SetColor("yellow");
                        terminal.WriteLine("\"You're not poisoned! Save your gold.\"");
                    }
                    else if (player.Gold >= antidotePrice)
                    {
                        player.Gold -= antidotePrice;
                        player.Poison = 0;
                        terminal.SetColor("green");
                        terminal.WriteLine("The poison drains from your body!");
                    }
                    else
                    {
                        terminal.SetColor("red");
                        terminal.WriteLine("\"Not enough gold, friend.\"");
                    }
                    await Task.Delay(1500);
                    break;

                case "4":
                    int potionsNeeded = player.MaxPotions - (int)player.Healing;
                    if (potionsNeeded <= 0)
                    {
                        terminal.SetColor("yellow");
                        terminal.WriteLine("\"You're already full on potions!\"");
                    }
                    else
                    {
                        long totalCost = potionsNeeded * potionPrice;
                        if (player.Gold >= totalCost)
                        {
                            player.Gold -= totalCost;
                            player.Healing = player.MaxPotions;
                            terminal.SetColor("green");
                            terminal.WriteLine($"Purchased {potionsNeeded} potions for {totalCost}g!");
                            terminal.WriteLine($"Potions: {player.Healing}/{player.MaxPotions}");
                        }
                        else
                        {
                            int canAfford = (int)(player.Gold / potionPrice);
                            if (canAfford > 0)
                            {
                                player.Gold -= canAfford * potionPrice;
                                player.Healing += canAfford;
                                terminal.SetColor("yellow");
                                terminal.WriteLine($"Could only afford {canAfford} potions.");
                                terminal.WriteLine($"Potions: {player.Healing}/{player.MaxPotions}");
                            }
                            else
                            {
                                terminal.SetColor("red");
                                terminal.WriteLine("\"Not enough gold, friend.\"");
                            }
                        }
                    }
                    await Task.Delay(1500);
                    break;

                case "A":
                case "B":
                case "C":
                case "D":
                    int itemIndex = choice[0] - 'A';
                    if (itemIndex >= 0 && itemIndex < rareItems.Count)
                    {
                        await PurchaseRareItem(player, rareItems[itemIndex]);
                    }
                    break;

                case "L":
                case "":
                    trading = false;
                    terminal.SetColor("gray");
                    terminal.WriteLine("\"Safe travels, adventurer!\"");
                    await Task.Delay(1000);
                    break;

                default:
                    terminal.SetColor("red");
                    terminal.WriteLine("Invalid choice.");
                    await Task.Delay(1000);
                    break;
            }
        }
    }

    private class MerchantRareItem
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public long Price { get; set; }
        public string Type { get; set; } = ""; // weapon, armor, ring, amulet, special
        public int Power { get; set; }
        public bool Sold { get; set; } = false;
        public Action<Character>? Effect { get; set; }
    }

    private List<MerchantRareItem> GenerateMerchantRareItems(int level)
    {
        var items = new List<MerchantRareItem>();
        int basePower = level + 5;
        long basePrice = level * 500;

        // Weapon options
        var weapons = new[]
        {
            ("Shadow Blade", "Strikes from darkness", basePower + 3),
            ("Flame Tongue", "Burns with eternal fire", basePower + 4),
            ("Frost Brand", "Chills to the bone", basePower + 4),
            ("Thunderclap", "Echoes with lightning", basePower + 5),
            ("Venom Fang", "Drips with poison", basePower + 3),
            ("Soul Reaver", "Drains life force", basePower + 6),
            ("Demon Slayer", "Bane of evil", basePower + 5),
            ("Dragon Tooth", "From an ancient wyrm", basePower + 7),
        };

        // Armor options
        var armors = new[]
        {
            ("Shadowmail", "Blends with darkness", basePower + 2),
            ("Dragonscale Vest", "Resistant to fire", basePower + 4),
            ("Mithril Chain", "Light as a feather", basePower + 3),
            ("Void Armor", "Absorbs magic", basePower + 5),
            ("Phoenix Plate", "Regenerates slowly", basePower + 4),
            ("Titan's Guard", "Immense protection", basePower + 6),
        };

        // Ring options
        var rings = new[]
        {
            ("Ring of Might", "+5 Strength", 5),
            ("Ring of Vitality", "+50 Max HP", 50),
            ("Ring of the Thief", "+5 Dexterity", 5),
            ("Ring of Wisdom", "+5 Intelligence", 5),
            ("Ring of Fortune", "+10% Gold Find", 10),
            ("Ring of Protection", "+3 Defense", 3),
        };

        // Amulet/Special options
        var specials = new[]
        {
            ("Amulet of Life", "+100 Max HP", 100),
            ("Charm of Speed", "+1 Attack per round", 1),
            ("Talisman of Power", "+10 All Stats", 10),
            ("Lucky Coin", "Extra gold from enemies", 0),
        };

        // Pick random items for this merchant
        var weaponChoice = weapons[dungeonRandom.Next(weapons.Length)];
        items.Add(new MerchantRareItem
        {
            Name = weaponChoice.Item1,
            Description = $"+{weaponChoice.Item3} Weapon Power - {weaponChoice.Item2}",
            Price = basePrice + (weaponChoice.Item3 * 100),
            Type = "weapon",
            Power = weaponChoice.Item3,
            Effect = (p) => { p.WeapPow += weaponChoice.Item3; }
        });

        var armorChoice = armors[dungeonRandom.Next(armors.Length)];
        items.Add(new MerchantRareItem
        {
            Name = armorChoice.Item1,
            Description = $"+{armorChoice.Item3} Armor Power - {armorChoice.Item2}",
            Price = basePrice + (armorChoice.Item3 * 80),
            Type = "armor",
            Power = armorChoice.Item3,
            Effect = (p) => { p.ArmPow += armorChoice.Item3; }
        });

        var ringChoice = rings[dungeonRandom.Next(rings.Length)];
        items.Add(new MerchantRareItem
        {
            Name = ringChoice.Item1,
            Description = ringChoice.Item2,
            Price = basePrice / 2 + (ringChoice.Item3 * 50),
            Type = "ring",
            Power = ringChoice.Item3,
            Effect = ringChoice.Item1 switch
            {
                "Ring of Might" => (p) => { p.Strength += 5; },
                "Ring of Vitality" => (p) => { p.MaxHP += 50; p.HP += 50; },
                "Ring of the Thief" => (p) => { p.Dexterity += 5; },
                "Ring of Wisdom" => (p) => { p.Intelligence += 5; },
                "Ring of Fortune" => (p) => { }, // passive effect, just mark as owned
                "Ring of Protection" => (p) => { p.ArmPow += 3; },
                _ => (p) => { }
            }
        });

        var specialChoice = specials[dungeonRandom.Next(specials.Length)];
        items.Add(new MerchantRareItem
        {
            Name = specialChoice.Item1,
            Description = specialChoice.Item2,
            Price = basePrice + 1000,
            Type = "special",
            Power = specialChoice.Item3,
            Effect = specialChoice.Item1 switch
            {
                "Amulet of Life" => (p) => { p.MaxHP += 100; p.HP += 100; },
                "Charm of Speed" => (p) => { p.Dexterity += 10; p.Agility += 10; },
                "Talisman of Power" => (p) => {
                    p.Strength += 10; p.Intelligence += 10; p.Wisdom += 10;
                    p.Dexterity += 10; p.Constitution += 10; p.Charisma += 10;
                },
                "Lucky Coin" => (p) => { }, // passive effect
                _ => (p) => { }
            }
        });

        return items;
    }

    private async Task PurchaseRareItem(Character player, MerchantRareItem item)
    {
        if (item.Sold)
        {
            terminal.SetColor("gray");
            terminal.WriteLine("\"That item has already been sold, friend.\"");
            await Task.Delay(1500);
            return;
        }

        if (player.Gold < item.Price)
        {
            terminal.SetColor("red");
            terminal.WriteLine($"\"You need {item.Price:N0} gold for the {item.Name}.\"");
            terminal.WriteLine($"\"Come back when you have more coin!\"");
            await Task.Delay(2000);
            return;
        }

        terminal.SetColor("cyan");
        terminal.WriteLine($"Purchase {item.Name} for {item.Price:N0} gold? (Y/N)");
        var confirm = (await terminal.GetInput("")).Trim().ToUpper();

        if (confirm == "Y")
        {
            player.Gold -= item.Price;
            item.Sold = true;
            item.Effect?.Invoke(player);

            terminal.SetColor("bright_yellow");
            terminal.WriteLine("");
            terminal.WriteLine("═══════════════════════════════════════");
            terminal.WriteLine($"  ★ ACQUIRED: {item.Name.ToUpper()} ★");
            terminal.WriteLine("═══════════════════════════════════════");
            terminal.SetColor("green");
            terminal.WriteLine($"{item.Description}");
            terminal.WriteLine("");
            terminal.SetColor("gray");
            terminal.WriteLine("\"A fine choice! Use it well.\"");
        }
        else
        {
            terminal.SetColor("gray");
            terminal.WriteLine("\"Perhaps another time.\"");
        }

        await Task.Delay(2000);
    }
    
    /// <summary>
    /// Witch Doctor encounter - Pascal DUNGEV2.PAS
    /// </summary>
    private async Task WitchDoctorEncounter()
    {
        terminal.SetColor("magenta");
        terminal.WriteLine("🔮 WITCH DOCTOR ENCOUNTER 🔮");
        terminal.WriteLine("");
        
        var currentPlayer = GetCurrentPlayer();
        long cost = currentPlayer.Level * 12500;
        
        if (currentPlayer.Gold >= cost)
        {
            terminal.WriteLine("You meet the evil Witch-Doctor Mbluta!");
            terminal.WriteLine($"He demands {cost} gold or he will curse you!");
            terminal.WriteLine("");
            
            var choice = await terminal.GetInput("(P)ay the witch doctor or (R)un away? ");
            
            if (choice.ToUpper() == "P")
            {
                currentPlayer.Gold -= cost;
                terminal.WriteLine("You reluctantly pay the witch doctor.", "yellow");
                terminal.WriteLine("He vanishes into the darkness...");
            }
            else
            {
                // 50% chance to escape
                if (dungeonRandom.Next(2) == 0)
                {
                    terminal.WriteLine("You manage to flee the evil witch doctor!", "green");
                }
                else
                {
                    terminal.WriteLine("You fail to escape and are cursed!", "red");
                    
                    // Random curse effect
                    var curseType = dungeonRandom.Next(3);
                    switch (curseType)
                    {
                        case 0:
                            var expLoss = currentPlayer.Level * 1500;
                            currentPlayer.Experience = Math.Max(0, currentPlayer.Experience - expLoss);
                            terminal.WriteLine($"You lose {expLoss} experience points!");
                            break;
                        case 1:
                            var fightLoss = dungeonRandom.Next(5) + 1;
                            currentPlayer.Fights = Math.Max(0, currentPlayer.Fights - fightLoss);
                            terminal.WriteLine($"You lose {fightLoss} dungeon fights!");
                            break;
                        case 2:
                            var pfightLoss = dungeonRandom.Next(3) + 1;
                            currentPlayer.PFights = Math.Max(0, currentPlayer.PFights - pfightLoss);
                            terminal.WriteLine($"You lose {pfightLoss} player fights!");
                            break;
                    }
                }
            }
        }
        else
        {
            terminal.WriteLine("A witch doctor appears but sees you have no gold and leaves.", "gray");
        }
        
        await Task.Delay(3000);
    }
    
    /// <summary>
    /// Create dungeon monster based on level and terrain
    /// </summary>
    private Monster CreateDungeonMonster(bool isLeader = false)
    {
        var monsterNames = GetMonsterNamesForTerrain(currentTerrain);
        var weaponArmor = GetWeaponArmorForTerrain(currentTerrain);
        
        var name = monsterNames[dungeonRandom.Next(monsterNames.Length)];
        var weapon = weaponArmor.weapons[dungeonRandom.Next(weaponArmor.weapons.Length)];
        var armor = weaponArmor.armor[dungeonRandom.Next(weaponArmor.armor.Length)];
        
        if (isLeader)
        {
            name = GetLeaderName(name);
        }
        
        // Smooth scaling factors – tuned for balanced difficulty curve
        float scaleFactor = 1f + (currentDungeonLevel / 20f); // every 20 levels → +100 %

        // Regular monsters are weaker, bosses are tougher (like the original game)
        float monsterMultiplier = isLeader ? 1.8f : 0.6f; // Regular monsters are 60% strength, bosses are 180%

        long hp = (long)(currentDungeonLevel * 4 * scaleFactor * monsterMultiplier); // survivability

        int strength = (int)(currentDungeonLevel * 1.5f * scaleFactor * monsterMultiplier); // base damage
        int punch    = (int)(currentDungeonLevel * 1.2f * scaleFactor * monsterMultiplier); // natural attacks
        int weapPow  = (int)(currentDungeonLevel * 0.9f * scaleFactor * monsterMultiplier); // weapon bonus
        int armPow   = (int)(currentDungeonLevel * 0.9f * scaleFactor * monsterMultiplier); // defense bonus

        var monster = Monster.CreateMonster(
            nr: currentDungeonLevel,
            name: name,
            hps: hp,
            strength: strength,
            defence: 0,
            phrase: GetMonsterPhrase(currentTerrain),
            grabweap: dungeonRandom.NextDouble() < 0.3,
            grabarm: false,
            weapon: weapon,
            armor: armor,
            poisoned: false,
            disease: false,
            punch: punch,
            armpow: armPow,
            weappow: weapPow
        );
        
        if (isLeader)
        {
            monster.IsBoss = true;
        }
        
        // Store level for other systems (initiative scaling etc.)
        monster.Level = currentDungeonLevel;
        
        return monster;
    }
    
    // Helper methods for monster creation
    private string[] GetMonsterNamesForTerrain(DungeonTerrain terrain)
    {
        return terrain switch
        {
            DungeonTerrain.Underground => new[] { "Orc", "Half-Orc", "Goblin", "Troll", "Skeleton" },
            DungeonTerrain.Mountains => new[] { "Mountain Bandit", "Hill Giant", "Stone Golem", "Dwarf Warrior" },
            DungeonTerrain.Desert => new[] { "Robber Knight", "Robber Squire", "Desert Nomad", "Sand Troll" },
            DungeonTerrain.Forest => new[] { "Tree Hunter", "Green Threat", "Forest Bandit", "Wild Beast" },
            DungeonTerrain.Caves => new[] { "Cave Troll", "Underground Drake", "Deep Dweller", "Rock Monster" },
            _ => new[] { "Monster", "Creature", "Beast", "Fiend" }
        };
    }
    
    private (string[] weapons, string[] armor) GetWeaponArmorForTerrain(DungeonTerrain terrain)
    {
        return terrain switch
        {
            DungeonTerrain.Underground => (
                new[] { "Sword", "Spear", "Axe", "Club" },
                new[] { "Leather", "Chain-mail", "Cloth" }
            ),
            DungeonTerrain.Mountains => (
                new[] { "War Hammer", "Battle Axe", "Mace" },
                new[] { "Chain-mail", "Scale Mail", "Plate" }
            ),
            DungeonTerrain.Desert => (
                new[] { "Lance", "Scimitar", "Javelin" },
                new[] { "Chain-Mail", "Leather", "Robes" }
            ),
            DungeonTerrain.Forest => (
                new[] { "Silver Dagger", "Sling", "Sharp Stick", "Bow" },
                new[] { "Cloth", "Leather", "Bark Armor" }
            ),
            _ => (
                new[] { "Rusty Sword", "Broken Spear", "Old Club" },
                new[] { "Torn Clothes", "Rags", "Nothing" }
            )
        };
    }
    
    private string GetLeaderName(string baseName)
    {
        return baseName + " Leader";
    }
    
    private string GetMonsterPhrase(DungeonTerrain terrain)
    {
        var phrases = terrain switch
        {
            DungeonTerrain.Underground => new[] { "Trespasser!", "Attack!", "Kill them!", "No mercy!" },
            DungeonTerrain.Mountains => new[] { "Give yourself up!", "Take no prisoners!", "For the clan!" },
            DungeonTerrain.Desert => new[] { "No prisoners!", "Your gold or your life!", "Die, infidel!" },
            DungeonTerrain.Forest => new[] { "Wrong way, lads!", "Protect the trees!", "Nature's revenge!" },
            _ => new[] { "Grrargh!", "Attack!", "Die!", "No escape!" }
        };
        
        return phrases[dungeonRandom.Next(phrases.Length)];
    }
    
    // Additional helper methods
    private async Task ShowExplorationText()
    {
        var explorationTexts = new[]
        {
            "You cautiously advance through the shadowy corridors...",
            "Your footsteps echo in the ancient stone passages...",
            "Flickering torchlight reveals mysterious doorways ahead...",
            "The air grows colder as you venture deeper into the dungeon...",
            "Strange sounds echo from the darkness beyond..."
        };
        
        terminal.WriteLine(explorationTexts[dungeonRandom.Next(explorationTexts.Length)], "gray");
        await Task.Delay(2000);
    }
    
    private string GetTerrainDescription(DungeonTerrain terrain)
    {
        return terrain switch
        {
            DungeonTerrain.Underground => "Ancient Underground Tunnels",
            DungeonTerrain.Mountains => "Rocky Mountain Passages",
            DungeonTerrain.Desert => "Desert Ruins and Tombs",
            DungeonTerrain.Forest => "Overgrown Forest Caves",
            DungeonTerrain.Caves => "Deep Natural Caverns",
            _ => "Unknown Territory"
        };
    }
    
    // Placeholder methods for features to implement
    private async Task DescendDeeper()
    {
        var playerLevel = GetCurrentPlayer()?.Level ?? 1;
        int deepestAllowed = Math.Min(maxDungeonLevel, playerLevel + 10);

        if (currentDungeonLevel < deepestAllowed)
        {
            currentDungeonLevel++;
            terminal.WriteLine($"You descend to dungeon level {currentDungeonLevel}.", "yellow");
        }
        else
        {
            terminal.WriteLine("A mysterious force bars your way – it seems too dangerous to venture deeper right now.", "red");
        }
        await Task.Delay(1500);
    }
    
    private async Task AscendToSurface()
    {
        var playerLevel = GetCurrentPlayer()?.Level ?? 1;
        int minAllowed = Math.Max(1, playerLevel - 10);

        if (currentDungeonLevel > minAllowed)
        {
            currentDungeonLevel--;
            terminal.WriteLine($"You ascend to dungeon level {currentDungeonLevel}.", "green");
        }
        else if (currentDungeonLevel <= 1)
        {
            await NavigateToLocation(GameLocation.MainStreet);
        }
        else
        {
            terminal.WriteLine("These upper levels hold nothing for you now. Seek deeper challenges.", "yellow");
        }
        await Task.Delay(1500);
    }
    
    private async Task ManageTeam()
    {
        var player = GetCurrentPlayer();

        terminal.ClearScreen();
        terminal.SetColor("bright_cyan");
        terminal.WriteLine("╔═══════════════════════════════════════════════════╗");
        terminal.WriteLine("║               TEAM MANAGEMENT                     ║");
        terminal.WriteLine("╚═══════════════════════════════════════════════════╝");
        terminal.WriteLine("");

        // Check if player is in a team
        if (string.IsNullOrEmpty(player.Team))
        {
            terminal.SetColor("yellow");
            terminal.WriteLine("You are not in a team.");
            terminal.WriteLine("Visit the Team Corner in town to create or join a team!");
            terminal.WriteLine("");
            await terminal.PressAnyKey();
            return;
        }

        terminal.SetColor("white");
        terminal.WriteLine($"Team: {player.Team}");
        terminal.WriteLine($"Team controls turf: {(player.CTurf ? "Yes" : "No")}");
        terminal.WriteLine("");

        // Show current dungeon party
        terminal.SetColor("cyan");
        terminal.WriteLine("Current Dungeon Party:");
        terminal.WriteLine($"  1. {player.DisplayName} (You) - Level {player.Level} {player.Class}");
        for (int i = 0; i < teammates.Count; i++)
        {
            var tm = teammates[i];
            string status = tm.IsAlive ? $"HP: {tm.HP}/{tm.MaxHP}" : "[INJURED]";
            terminal.WriteLine($"  {i + 2}. {tm.DisplayName} - Level {tm.Level} {tm.Class} - {status}");
        }
        terminal.WriteLine("");

        // Get available NPC teammates from same team
        var npcTeammates = UsurperRemake.Systems.NPCSpawnSystem.Instance.ActiveNPCs
            .Where(n => n.Team == player.Team && n.IsAlive && !teammates.Contains(n))
            .ToList();

        if (npcTeammates.Count > 0)
        {
            terminal.SetColor("green");
            terminal.WriteLine("Available Team Members (not in dungeon party):");
            for (int i = 0; i < npcTeammates.Count; i++)
            {
                var npc = npcTeammates[i];
                terminal.WriteLine($"  [{i + 1}] {npc.DisplayName} - Level {npc.Level} {npc.Class} - HP: {npc.HP}/{npc.MaxHP}");
            }
            terminal.WriteLine("");
        }
        else if (teammates.Count == 0)
        {
            terminal.SetColor("gray");
            terminal.WriteLine("No team members available to join your dungeon party.");
            terminal.WriteLine("Recruit NPCs at the Team Corner or Hall of Recruitment!");
            terminal.WriteLine("");
        }

        // Show options
        terminal.SetColor("white");
        terminal.WriteLine("Options:");
        if (npcTeammates.Count > 0 && teammates.Count < 4) // Max 4 teammates + player = 5
        {
            terminal.WriteLine("  [A]dd a team member to dungeon party");
        }
        if (teammates.Count > 0)
        {
            terminal.WriteLine("  [R]emove a team member from party");
        }
        terminal.WriteLine("  [B]ack to dungeon menu");
        terminal.WriteLine("");

        var choice = await terminal.GetInput("Choice: ");
        choice = choice.ToUpper().Trim();

        switch (choice)
        {
            case "A":
                if (npcTeammates.Count > 0 && teammates.Count < 4)
                {
                    await AddTeammateToParty(npcTeammates);
                }
                else if (teammates.Count >= 4)
                {
                    terminal.WriteLine("Your dungeon party is full (max 4 teammates)!", "yellow");
                    await Task.Delay(1500);
                }
                else
                {
                    terminal.WriteLine("No team members available to add.", "gray");
                    await Task.Delay(1500);
                }
                break;

            case "R":
                if (teammates.Count > 0)
                {
                    await RemoveTeammateFromParty();
                }
                else
                {
                    terminal.WriteLine("No teammates to remove.", "gray");
                    await Task.Delay(1500);
                }
                break;

            case "B":
            default:
                break;
        }
    }

    private async Task AddTeammateToParty(List<NPC> available)
    {
        terminal.WriteLine("");
        terminal.SetColor("white");
        terminal.Write("Enter number of team member to add (1-");
        terminal.Write($"{available.Count}");
        terminal.Write("): ");
        var input = await terminal.GetInput("");

        if (int.TryParse(input, out int index) && index >= 1 && index <= available.Count)
        {
            var npc = available[index - 1];
            teammates.Add(npc);

            // Move NPC to dungeon
            npc.UpdateLocation("Dungeon");

            terminal.SetColor("green");
            terminal.WriteLine($"{npc.DisplayName} joins your dungeon party!");
            terminal.WriteLine("They will fight alongside you against monsters.");

            // 15% team XP/gold bonus for having teammates
            terminal.SetColor("cyan");
            terminal.WriteLine("Team bonus: +15% XP and gold from battles!");
        }
        else
        {
            terminal.WriteLine("Invalid selection.", "red");
        }
        await Task.Delay(2000);
    }

    private async Task RemoveTeammateFromParty()
    {
        terminal.WriteLine("");
        terminal.SetColor("white");
        terminal.Write("Enter number of teammate to remove (1-");
        terminal.Write($"{teammates.Count}");
        terminal.Write("): ");
        var input = await terminal.GetInput("");

        if (int.TryParse(input, out int index) && index >= 1 && index <= teammates.Count)
        {
            var member = teammates[index - 1];
            teammates.RemoveAt(index - 1);

            // Move NPC back to town (cast to NPC if applicable)
            if (member is NPC npc)
            {
                npc.UpdateLocation("Main Street");
            }

            terminal.SetColor("yellow");
            terminal.WriteLine($"{member.DisplayName} leaves the dungeon party and returns to town.");
        }
        else
        {
            terminal.WriteLine("Invalid selection.", "red");
        }
        await Task.Delay(1500);
    }
    
    private async Task ShowDungeonStatus()
    {
        await ShowStatus();
    }
    
    private async Task UsePotions()
    {
        var player = GetCurrentPlayer();

        while (true)
        {
            terminal.ClearScreen();
            terminal.SetColor("cyan");
            terminal.WriteLine("╔═══════════════════════════════════════════════════════╗");
            terminal.WriteLine("║                    POTIONS MENU                       ║");
            terminal.WriteLine("╚═══════════════════════════════════════════════════════╝");
            terminal.WriteLine("");

            // Show current status
            terminal.SetColor("white");
            terminal.Write("HP: ");
            DrawBar(player.HP, player.MaxHP, 25, "red", "darkgray");
            terminal.WriteLine($" {player.HP}/{player.MaxHP}");

            terminal.SetColor("yellow");
            terminal.WriteLine($"Healing Potions: {player.Healing}/{player.MaxPotions}");
            terminal.WriteLine($"Gold: {player.Gold:N0}");
            terminal.WriteLine("");

            // Calculate heal amount (potions heal 25% of max HP)
            long healAmount = player.MaxHP / 4;

            terminal.SetColor("white");
            terminal.WriteLine("Options:");
            terminal.WriteLine("");

            // Use potion option
            if (player.Healing > 0)
            {
                terminal.SetColor("darkgray");
                terminal.Write("  [");
                terminal.SetColor("green");
                terminal.Write("U");
                terminal.SetColor("darkgray");
                terminal.Write("] ");
                terminal.SetColor("white");
                terminal.WriteLine($"Use Healing Potion (heals ~{healAmount} HP)");
            }
            else
            {
                terminal.SetColor("darkgray");
                terminal.WriteLine("  [U] Use Healing Potion - NO POTIONS!");
            }

            // Buy potions option
            int costPerPotion = 50 + (player.Level * 10);
            terminal.SetColor("darkgray");
            terminal.Write("  [");
            terminal.SetColor("yellow");
            terminal.Write("B");
            terminal.SetColor("darkgray");
            terminal.Write("] ");
            terminal.SetColor("white");
            terminal.WriteLine($"Buy Potions from Monk ({costPerPotion}g each)");

            // Quick heal - use potions until full
            if (player.Healing > 0 && player.HP < player.MaxHP)
            {
                terminal.SetColor("darkgray");
                terminal.Write("  [");
                terminal.SetColor("bright_green");
                terminal.Write("H");
                terminal.SetColor("darkgray");
                terminal.Write("] ");
                terminal.SetColor("white");
                terminal.WriteLine("Heal to Full (use multiple potions)");
            }

            terminal.WriteLine("");
            terminal.SetColor("darkgray");
            terminal.Write("  [");
            terminal.SetColor("red");
            terminal.Write("Q");
            terminal.SetColor("darkgray");
            terminal.Write("] ");
            terminal.SetColor("white");
            terminal.WriteLine("Return to Dungeon");

            terminal.WriteLine("");
            terminal.SetColor("cyan");
            terminal.Write("Choice: ");
            terminal.SetColor("white");

            string choice = (await terminal.GetInput("")).Trim().ToUpper();

            switch (choice)
            {
                case "U":
                    if (player.Healing > 0)
                    {
                        await UseHealingPotion(player);
                    }
                    else
                    {
                        terminal.SetColor("red");
                        terminal.WriteLine("You don't have any healing potions!");
                        await Task.Delay(1500);
                    }
                    break;

                case "B":
                    await BuyPotionsFromMonk(player);
                    break;

                case "H":
                    if (player.Healing > 0 && player.HP < player.MaxHP)
                    {
                        await HealToFull(player);
                    }
                    break;

                case "Q":
                case "":
                    return;

                default:
                    terminal.SetColor("red");
                    terminal.WriteLine("Invalid choice.");
                    await Task.Delay(1000);
                    break;
            }
        }
    }

    private async Task UseHealingPotion(Character player)
    {
        if (player.Healing <= 0)
        {
            terminal.SetColor("red");
            terminal.WriteLine("You don't have any healing potions!");
            await Task.Delay(1500);
            return;
        }

        if (player.HP >= player.MaxHP)
        {
            terminal.SetColor("yellow");
            terminal.WriteLine("You're already at full health!");
            await Task.Delay(1500);
            return;
        }

        // Use one potion
        player.Healing--;
        long healAmount = player.MaxHP / 4;
        long oldHP = player.HP;
        player.HP = Math.Min(player.MaxHP, player.HP + healAmount);
        long actualHeal = player.HP - oldHP;

        terminal.SetColor("bright_green");
        terminal.WriteLine("");
        terminal.WriteLine("*glug glug glug*");
        terminal.WriteLine($"You drink a healing potion and recover {actualHeal} HP!");
        terminal.Write("HP: ");
        DrawBar(player.HP, player.MaxHP, 25, "red", "darkgray");
        terminal.WriteLine($" {player.HP}/{player.MaxHP}");
        terminal.SetColor("gray");
        terminal.WriteLine($"Potions remaining: {player.Healing}/{player.MaxPotions}");

        await Task.Delay(2000);
    }

    private async Task HealToFull(Character player)
    {
        if (player.Healing <= 0)
        {
            terminal.SetColor("red");
            terminal.WriteLine("You don't have any healing potions!");
            await Task.Delay(1500);
            return;
        }

        if (player.HP >= player.MaxHP)
        {
            terminal.SetColor("yellow");
            terminal.WriteLine("You're already at full health!");
            await Task.Delay(1500);
            return;
        }

        long healAmount = player.MaxHP / 4;
        int potionsNeeded = (int)Math.Ceiling((double)(player.MaxHP - player.HP) / healAmount);
        int potionsToUse = Math.Min(potionsNeeded, (int)player.Healing);

        terminal.SetColor("cyan");
        terminal.WriteLine($"This will use {potionsToUse} potion(s). Continue? (Y/N)");
        string confirm = (await terminal.GetInput("")).Trim().ToUpper();

        if (confirm != "Y")
        {
            terminal.SetColor("gray");
            terminal.WriteLine("Cancelled.");
            await Task.Delay(1000);
            return;
        }

        long oldHP = player.HP;
        for (int i = 0; i < potionsToUse; i++)
        {
            player.Healing--;
            player.HP = Math.Min(player.MaxHP, player.HP + healAmount);
            if (player.HP >= player.MaxHP) break;
        }
        long actualHeal = player.HP - oldHP;

        terminal.SetColor("bright_green");
        terminal.WriteLine("");
        terminal.WriteLine("*glug glug glug* *glug glug*");
        terminal.WriteLine($"You drink {potionsToUse} healing potion(s) and recover {actualHeal} HP!");
        terminal.Write("HP: ");
        DrawBar(player.HP, player.MaxHP, 25, "red", "darkgray");
        terminal.WriteLine($" {player.HP}/{player.MaxHP}");
        terminal.SetColor("gray");
        terminal.WriteLine($"Potions remaining: {player.Healing}/{player.MaxPotions}");

        await Task.Delay(2000);
    }

    private async Task BuyPotionsFromMonk(Character player)
    {
        terminal.ClearScreen();
        terminal.SetColor("cyan");
        terminal.WriteLine("");
        terminal.WriteLine("A wandering monk materializes from the shadows...");
        terminal.WriteLine("");
        terminal.SetColor("white");
        terminal.WriteLine("\"Greetings, traveler. I sense you need healing supplies.\"");
        terminal.WriteLine($"\"You carry {player.Healing} of {player.MaxPotions} potions.\"");
        terminal.WriteLine("");

        // Calculate cost per potion (scales with level)
        int costPerPotion = 50 + (player.Level * 10);

        terminal.SetColor("yellow");
        terminal.WriteLine($"Price: {costPerPotion} gold per potion");
        terminal.WriteLine($"Your gold: {player.Gold:N0}");
        terminal.WriteLine("");

        // Calculate max potions player can buy
        int roomForPotions = player.MaxPotions - (int)player.Healing;
        int maxAffordable = (int)(player.Gold / costPerPotion);
        int maxCanBuy = Math.Min(roomForPotions, maxAffordable);

        if (maxCanBuy <= 0)
        {
            if (roomForPotions <= 0)
            {
                terminal.SetColor("yellow");
                terminal.WriteLine("\"You already carry all the potions you can hold!\"");
            }
            else
            {
                terminal.SetColor("red");
                terminal.WriteLine("\"I'm afraid you lack the gold, my friend.\"");
            }
            await terminal.PressAnyKey();
            return;
        }

        terminal.SetColor("cyan");
        terminal.WriteLine($"How many potions would you like? (Max: {maxCanBuy}, 0 to cancel)");
        terminal.Write("> ");
        terminal.SetColor("white");

        var amountInput = await terminal.GetInput("");

        if (!int.TryParse(amountInput.Trim(), out int amount) || amount < 1)
        {
            terminal.SetColor("gray");
            terminal.WriteLine("\"Perhaps another time, then.\"");
            await Task.Delay(1500);
            return;
        }

        if (amount > maxCanBuy)
        {
            terminal.SetColor("yellow");
            terminal.WriteLine($"\"I can only provide you with {maxCanBuy} potions.\"");
            amount = maxCanBuy;
        }

        // Complete the purchase
        long totalCost = amount * costPerPotion;
        player.Gold -= totalCost;
        player.Healing += amount;

        terminal.WriteLine("");
        terminal.SetColor("bright_green");
        terminal.WriteLine($"You purchase {amount} healing potion{(amount > 1 ? "s" : "")} for {totalCost:N0} gold.");
        terminal.SetColor("cyan");
        terminal.WriteLine($"Potions: {player.Healing}/{player.MaxPotions}");
        terminal.SetColor("yellow");
        terminal.WriteLine($"Gold remaining: {player.Gold:N0}");

        terminal.WriteLine("");
        terminal.SetColor("gray");
        terminal.WriteLine("The monk bows and fades back into the shadows...");
        await Task.Delay(2000);
    }
    
    private async Task ShowDungeonMap()
    {
        if (currentFloor == null)
        {
            terminal.WriteLine("No floor to map.", "gray");
            await Task.Delay(1500);
            return;
        }

        terminal.ClearScreen();
        terminal.SetColor(GetThemeColor(currentFloor.Theme));
        terminal.WriteLine($"╔═══════════════════════════════════════════════════════╗");
        terminal.WriteLine($"║  DUNGEON MAP - Level {currentDungeonLevel} ({currentFloor.Theme})".PadRight(56) + "║");
        terminal.WriteLine($"╚═══════════════════════════════════════════════════════╝");
        terminal.WriteLine("");

        var currentRoom = currentFloor.GetCurrentRoom();

        // Display rooms in a simple list format with connections
        foreach (var room in currentFloor.Rooms)
        {
            bool isCurrentRoom = room.Id == currentFloor.CurrentRoomId;

            // Room indicator
            if (isCurrentRoom)
            {
                terminal.SetColor("bright_yellow");
                terminal.Write(">> ");
            }
            else if (room.IsExplored)
            {
                terminal.SetColor("white");
                terminal.Write("   ");
            }
            else
            {
                terminal.SetColor("darkgray");
                terminal.Write("   ");
            }

            // Room status icons
            if (room.IsBossRoom)
            {
                terminal.SetColor("bright_red");
                terminal.Write("[B]");
            }
            else if (room.HasStairsDown)
            {
                terminal.SetColor("blue");
                terminal.Write("[↓]");
            }
            else if (room.IsCleared)
            {
                terminal.SetColor("green");
                terminal.Write("[✓]");
            }
            else if (room.HasMonsters && room.IsExplored)
            {
                terminal.SetColor("red");
                terminal.Write("[!]");
            }
            else if (room.IsExplored)
            {
                terminal.SetColor("cyan");
                terminal.Write("[·]");
            }
            else
            {
                terminal.SetColor("darkgray");
                terminal.Write("[?]");
            }

            // Room name
            terminal.SetColor(room.IsExplored ? "white" : "darkgray");
            terminal.Write($" {room.Name}");

            // Exits
            if (room.IsExplored)
            {
                terminal.SetColor("darkgray");
                terminal.Write(" → ");
                foreach (var exit in room.Exits)
                {
                    terminal.Write($"{GetDirectionKey(exit.Key)} ");
                }
            }

            terminal.WriteLine("");
        }

        terminal.WriteLine("");
        terminal.SetColor("gray");
        terminal.WriteLine("Legend: [B]=Boss [↓]=Stairs [✓]=Cleared [!]=Danger [·]=Safe [?]=Unknown");
        terminal.WriteLine("        >> = Your location");
        terminal.WriteLine("");

        // Floor stats
        int explored = currentFloor.Rooms.Count(r => r.IsExplored);
        int cleared = currentFloor.Rooms.Count(r => r.IsCleared);
        terminal.SetColor("white");
        terminal.WriteLine($"Explored: {explored}/{currentFloor.Rooms.Count}  Cleared: {cleared}/{currentFloor.Rooms.Count}");
        if (currentFloor.BossDefeated)
        {
            terminal.SetColor("green");
            terminal.WriteLine("BOSS DEFEATED!");
        }

        terminal.WriteLine("");
        await terminal.PressAnyKey();
    }

    /// <summary>
    /// NPC encounter in dungeon
    /// </summary>
    private async Task NPCEncounter()
    {
        terminal.ClearScreen();
        terminal.SetColor("cyan");
        terminal.WriteLine("*** ANOTHER ADVENTURER ***");
        terminal.WriteLine("");

        var player = GetCurrentPlayer();
        var npcType = dungeonRandom.Next(5);

        switch (npcType)
        {
            case 0: // Friendly trader
                terminal.WriteLine("A fellow adventurer hails you!", "white");
                terminal.WriteLine("\"Greetings, friend! I have supplies for sale.\"", "yellow");
                terminal.WriteLine("");

                terminal.WriteLine("[B] Buy healing potions (500 gold each)");
                terminal.WriteLine("[I] Trade information (100 gold)");
                terminal.WriteLine("[L] Leave");

                var tradeChoice = await terminal.GetInput("Choice: ");
                if (tradeChoice.ToUpper() == "B" && player.Gold >= 500)
                {
                    player.Gold -= 500;
                    player.Healing = Math.Min(player.MaxPotions, player.Healing + 1);
                    terminal.WriteLine("You purchase a healing potion.", "green");
                }
                else if (tradeChoice.ToUpper() == "I" && player.Gold >= 100)
                {
                    player.Gold -= 100;
                    terminal.SetColor("cyan");
                    terminal.WriteLine("\"The boss room is to the far end of the dungeon.\"");
                    terminal.WriteLine("\"Watch for traps near treasure rooms.\"");
                    terminal.WriteLine("\"Resting recovers health, but only once per floor.\"");
                }
                break;

            case 1: // Wounded adventurer
                terminal.WriteLine("A wounded adventurer lies against the wall!", "red");
                terminal.WriteLine("\"Please... take my map... avenge me...\"", "yellow");
                terminal.WriteLine("");

                // Mark more rooms as explored
                foreach (var room in currentFloor.Rooms.Take(currentFloor.Rooms.Count / 2))
                {
                    room.IsExplored = true;
                }
                terminal.WriteLine("You gain knowledge of the dungeon layout!", "green");
                break;

            case 2: // Rival adventurer
                terminal.WriteLine("A rival adventurer blocks your path!", "red");
                terminal.WriteLine("\"This treasure is MINE! Get out!\"", "yellow");
                terminal.WriteLine("");

                var rivalChoice = await terminal.GetInput("(F)ight, (N)egotiate, or (L)eave? ");
                if (rivalChoice.ToUpper() == "F")
                {
                    var rival = Monster.CreateMonster(
                        currentDungeonLevel, "Rival Adventurer",
                        currentDungeonLevel * 10, currentDungeonLevel * 3, 0,
                        "Die!", false, false, "Steel Sword", "Chain Mail",
                        false, false, currentDungeonLevel * 3, currentDungeonLevel * 2, currentDungeonLevel * 2
                    );
                    rival.Level = currentDungeonLevel;

                    var combatEngine = new CombatEngine(terminal);
                    await combatEngine.PlayerVsMonster(player, rival, teammates);
                }
                else if (rivalChoice.ToUpper() == "N")
                {
                    long bribe = currentDungeonLevel * 200;
                    if (player.Gold >= bribe)
                    {
                        player.Gold -= bribe;
                        terminal.WriteLine($"You pay {bribe} gold to pass.", "yellow");
                    }
                    else
                    {
                        terminal.WriteLine("\"No gold? Then fight!\"", "red");
                    }
                }
                break;

            case 3: // Lost explorer
                terminal.WriteLine("A lost explorer stumbles towards you!", "white");
                terminal.WriteLine("\"Oh thank the gods! I've been lost for days!\"", "yellow");
                terminal.WriteLine("");
                terminal.WriteLine("You guide them to safety.", "green");
                long reward = currentDungeonLevel * 150;
                player.Gold += reward;
                player.Chivalry += 20;
                terminal.WriteLine($"They reward you with {reward} gold!", "yellow");
                terminal.WriteLine("Your chivalry increases!", "white");
                break;

            case 4: // Mysterious stranger
                terminal.WriteLine("A cloaked figure emerges from the shadows...", "magenta");
                terminal.WriteLine("\"Fate has brought us together...\"", "yellow");
                terminal.WriteLine("");

                if (dungeonRandom.NextDouble() < 0.5)
                {
                    terminal.WriteLine("They offer you a blessing!", "green");
                    player.HP = Math.Min(player.MaxHP, player.HP + player.MaxHP / 2);
                    terminal.WriteLine("You feel revitalized!");
                }
                else
                {
                    terminal.WriteLine("\"Beware the darkness ahead...\"", "red");
                    terminal.WriteLine("They vanish as mysteriously as they appeared.");
                }
                break;
        }

        await Task.Delay(2000);
        await terminal.PressAnyKey();
    }

    /// <summary>
    /// Puzzle encounter
    /// </summary>
    private async Task PuzzleEncounter()
    {
        terminal.ClearScreen();
        terminal.SetColor("cyan");
        terminal.WriteLine("*** ANCIENT PUZZLE ***");
        terminal.WriteLine("");

        var player = GetCurrentPlayer();
        var puzzleType = dungeonRandom.Next(3);

        switch (puzzleType)
        {
            case 0: // Riddle
                terminal.WriteLine("An ancient stone door blocks your path.", "white");
                terminal.WriteLine("Carved letters form a riddle:", "gray");
                terminal.WriteLine("");
                terminal.SetColor("yellow");
                terminal.WriteLine("\"I have cities, but no houses.\"");
                terminal.WriteLine("\"I have mountains, but no trees.\"");
                terminal.WriteLine("\"I have water, but no fish.\"");
                terminal.WriteLine("\"What am I?\"");
                terminal.WriteLine("");

                var answer = await terminal.GetInput("Your answer: ");
                if (answer.ToLower().Contains("map"))
                {
                    terminal.SetColor("green");
                    terminal.WriteLine("The door slides open!");
                    long goldReward = currentDungeonLevel * 300;
                    long expReward = currentDungeonLevel * 100;
                    player.Gold += goldReward;
                    player.Experience += expReward;
                    terminal.WriteLine($"Behind it: {goldReward} gold and ancient wisdom (+{expReward} exp)!");
                }
                else
                {
                    terminal.SetColor("red");
                    terminal.WriteLine("The door remains sealed. Perhaps another time.");
                }
                break;

            case 1: // Lever puzzle
                terminal.WriteLine("Three levers protrude from the wall.", "white");
                terminal.WriteLine("A plaque reads: \"Two truths, one lie.\"", "gray");
                terminal.WriteLine("");
                terminal.WriteLine("[1] Left lever   [2] Middle lever   [3] Right lever");

                var leverChoice = await terminal.GetInput("Pull which lever? ");
                if (leverChoice == "2")
                {
                    terminal.SetColor("green");
                    terminal.WriteLine("A hidden compartment opens!");
                    long reward = currentDungeonLevel * 250;
                    player.Gold += reward;
                    terminal.WriteLine($"You find {reward} gold!");
                }
                else
                {
                    terminal.SetColor("red");
                    terminal.WriteLine("A dart shoots from the wall!");
                    int damage = currentDungeonLevel * 3;
                    player.HP -= damage;
                    terminal.WriteLine($"You take {damage} damage!");
                }
                break;

            case 2: // Pattern matching
                terminal.WriteLine("Glowing runes appear on the floor.", "white");
                terminal.WriteLine("They flash in sequence: RED, BLUE, RED, BLUE, ???", "gray");
                terminal.WriteLine("");
                terminal.WriteLine("[R] Red   [B] Blue   [G] Green");

                var patternChoice = await terminal.GetInput("What comes next? ");
                if (patternChoice.ToUpper() == "R")
                {
                    terminal.SetColor("green");
                    terminal.WriteLine("The runes glow brightly and a treasure rises from the floor!");
                    long reward = currentDungeonLevel * 200;
                    player.Gold += reward;
                    player.Experience += currentDungeonLevel * 75;
                    terminal.WriteLine($"You gain {reward} gold and experience!");
                }
                else
                {
                    terminal.SetColor("yellow");
                    terminal.WriteLine("The runes fade. Nothing happens.");
                }
                break;
        }

        await Task.Delay(2000);
        await terminal.PressAnyKey();
    }

    /// <summary>
    /// Rest spot encounter
    /// </summary>
    private async Task RestSpotEncounter()
    {
        terminal.ClearScreen();
        terminal.SetColor("green");
        terminal.WriteLine("*** SAFE HAVEN ***");
        terminal.WriteLine("");

        var player = GetCurrentPlayer();

        terminal.WriteLine("You discover a hidden sanctuary!", "white");
        terminal.WriteLine("The air here is calm, protected by ancient magic.", "gray");
        terminal.WriteLine("");

        if (!hasRestThisFloor)
        {
            terminal.WriteLine("You rest and recover your strength.", "green");
            long healAmount = player.MaxHP / 3;
            player.HP = Math.Min(player.MaxHP, player.HP + healAmount);
            terminal.WriteLine($"You recover {healAmount} HP!");

            // Cure poison
            if (player.Poison > 0)
            {
                player.Poison = 0;
                terminal.WriteLine("The sanctuary's magic cures your poison!", "cyan");
            }

            hasRestThisFloor = true;
        }
        else
        {
            terminal.WriteLine("You've already rested on this floor.", "gray");
            terminal.WriteLine("The sanctuary offers no additional benefit.");
        }

        await Task.Delay(2500);
        await terminal.PressAnyKey();
    }

    /// <summary>
    /// Mystery event encounter
    /// </summary>
    private async Task MysteryEventEncounter()
    {
        terminal.ClearScreen();
        terminal.SetColor("magenta");
        terminal.WriteLine("*** MYSTERIOUS OCCURRENCE ***");
        terminal.WriteLine("");

        var player = GetCurrentPlayer();
        var mysteryType = dungeonRandom.Next(5);

        switch (mysteryType)
        {
            case 0: // Vision
                terminal.WriteLine("A strange vision overtakes you...", "cyan");
                await Task.Delay(1500);
                terminal.WriteLine("You see the layout of this floor!", "yellow");
                foreach (var room in currentFloor.Rooms)
                {
                    room.IsExplored = true;
                }
                terminal.WriteLine("All rooms are now revealed on your map!", "green");
                break;

            case 1: // Time warp
                terminal.WriteLine("Reality warps around you!", "red");
                await Task.Delay(1000);
                terminal.SetColor("green");
                terminal.WriteLine("When it clears, you feel younger, stronger!");
                player.Experience += currentDungeonLevel * 200;
                terminal.WriteLine($"+{currentDungeonLevel * 200} experience!");
                break;

            case 2: // Ghostly message
                terminal.WriteLine("A ghostly figure appears!", "white");
                terminal.WriteLine("\"Seek the chamber of bones...\"", "yellow");
                terminal.WriteLine("\"There you will find what you seek...\"", "yellow");
                await Task.Delay(1500);
                terminal.WriteLine("The ghost points towards a direction and fades.", "gray");
                break;

            case 3: // Random teleport
                terminal.WriteLine("A magical portal suddenly opens beneath you!", "bright_magenta");
                await Task.Delay(1000);
                var randomRoom = currentFloor.Rooms[dungeonRandom.Next(currentFloor.Rooms.Count)];
                currentFloor.CurrentRoomId = randomRoom.Id;
                randomRoom.IsExplored = true;
                terminal.WriteLine($"You are transported to: {randomRoom.Name}!", "yellow");
                break;

            case 4: // Treasure rain
                terminal.WriteLine("Gold coins rain from the ceiling!", "yellow");
                long goldRain = currentDungeonLevel * 100 + dungeonRandom.Next(500);
                player.Gold += goldRain;
                terminal.WriteLine($"You gather {goldRain} gold!");
                break;
        }

        await Task.Delay(2500);
        await terminal.PressAnyKey();
    }

    /// <summary>
    /// Random fallback dungeon event
    /// </summary>
    private async Task RandomDungeonEvent()
    {
        // Pick a random existing event
        var eventType = dungeonRandom.Next(6);
        switch (eventType)
        {
            case 0: await TreasureChestEncounter(); break;
            case 1: await PotionCacheEncounter(); break;
            case 2: await MysteriousShrine(); break;
            case 3: await GamblingGhostEncounter(); break;
            case 4: await BeggarEncounter(); break;
            case 5: await WoundedManEncounter(); break;
        }
    }
    
    private async Task QuitToDungeon()
    {
        await NavigateToLocation(GameLocation.MainStreet);
    }
    
    private async Task ShowDungeonHelp()
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_cyan");
        terminal.WriteLine("Dungeon Help");
        terminal.WriteLine("============");
        terminal.WriteLine("");
        terminal.SetColor("white");
        terminal.WriteLine("E - Explore the current level");
        terminal.WriteLine("D - Descend to a deeper, more dangerous level");
        terminal.WriteLine("A - Ascend to a safer level or return to town");
        terminal.WriteLine("T - Manage your team members");
        terminal.WriteLine("S - View your character status");
        terminal.WriteLine("P - Buy potions from the wandering monk");
        terminal.WriteLine("M - View the dungeon map");
        terminal.WriteLine("Q - Quit and return to town");
        terminal.WriteLine("");
        await terminal.PressAnyKey();
    }
    
    /// <summary>
    /// Increase dungeon level directly up to +10 levels above the player (original mechanic).
    /// </summary>
    private async Task IncreaseDifficulty()
    {
        var playerLevel = GetCurrentPlayer()?.Level ?? 1;
        int targetLevel = currentDungeonLevel + 10;
        int maxAllowed = Math.Min(maxDungeonLevel, playerLevel + 10);

        if (targetLevel > maxAllowed)
            targetLevel = maxAllowed;

        if (targetLevel == currentDungeonLevel)
        {
            terminal.WriteLine("You cannot raise the difficulty any higher right now.", "yellow");
        }
        else
        {
            currentDungeonLevel = targetLevel;
            terminal.WriteLine($"You steel your nerves. The dungeon now feels like level {currentDungeonLevel}!", "magenta");
        }

        await Task.Delay(1500);
    }
    
    // Additional encounter methods
    private async Task BeggarEncounter()
    {
        terminal.SetColor("cyan");
        terminal.WriteLine("☂ BEGGAR ENCOUNTER ☂");
        terminal.WriteLine("");
        terminal.WriteLine("A poor beggar approaches you with outstretched hands.");
        terminal.WriteLine("'Please, kind sir/madam, spare some gold for a poor soul?'");
        terminal.WriteLine("");
        
        var choice = await terminal.GetInput("(G)ive gold to beggar or (I)gnore them? ");
        
        if (choice.ToUpper() == "G")
        {
            var currentPlayer = GetCurrentPlayer();
            if (currentPlayer.Gold >= 10)
            {
                currentPlayer.Gold -= 10;
                currentPlayer.Chivalry += 5;
                terminal.WriteLine("The beggar thanks you profusely for your kindness!", "green");
                terminal.WriteLine("Your chivalry increases!");
            }
            else
            {
                terminal.WriteLine("You don't have enough gold to spare.", "red");
            }
        }
        else
        {
            terminal.WriteLine("You ignore the beggar and continue on your way.", "gray");
        }
        
        await Task.Delay(2000);
    }
    
    private Monster CreateMerchantMonster()
    {
        return Monster.CreateMonster(1, "Frightened Merchant", 30, 10, 0,
            "Help me!", false, false, "Walking Stick", "Robes", 
            false, false, 5, 1, 3);
    }
    
    private Monster CreateUndeadMonster()
    {
        var names = new[] { "Undead", "Zombie", "Skeleton Warrior" };
        var name = names[dungeonRandom.Next(names.Length)];
        
        return Monster.CreateMonster(currentDungeonLevel, name, 
            currentDungeonLevel * 5, currentDungeonLevel * 2, 0,
            "...", false, false, "Rusty Sword", "Tattered Armor",
            false, false, currentDungeonLevel * 70, 0, currentDungeonLevel * 2);
    }
}

/// <summary>
/// Dungeon terrain types affecting encounters
/// </summary>
public enum DungeonTerrain
{
    Underground,
    Mountains, 
    Desert,
    Forest,
    Caves
} 
