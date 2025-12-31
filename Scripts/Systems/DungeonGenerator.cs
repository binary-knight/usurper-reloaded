using System;
using System.Collections.Generic;
using System.Linq;

namespace UsurperRemake.Systems
{
    /// <summary>
    /// Procedural dungeon generator - creates interesting, explorable dungeon floors
    /// with rooms, corridors, events, and atmosphere
    /// </summary>
    public static class DungeonGenerator
    {
        private static Random random = new Random();

        /// <summary>
        /// Generate a complete dungeon floor with interconnected rooms
        /// </summary>
        public static DungeonFloor GenerateFloor(int level)
        {
            var floor = new DungeonFloor
            {
                Level = level,
                Theme = GetThemeForLevel(level),
                DangerLevel = CalculateDangerLevel(level)
            };

            // Determine floor size based on level
            int roomCount = 6 + (level / 10); // 6-16 rooms
            roomCount = Math.Min(roomCount, 16);

            // Generate rooms
            GenerateRooms(floor, roomCount);

            // Connect rooms into a navigable layout
            ConnectRooms(floor);

            // Place special rooms (boss, treasure, etc.)
            PlaceSpecialRooms(floor);

            // Populate with events
            PopulateEvents(floor);

            // Set entrance
            floor.EntranceRoomId = floor.Rooms.First().Id;
            floor.CurrentRoomId = floor.EntranceRoomId;

            return floor;
        }

        private static DungeonTheme GetThemeForLevel(int level)
        {
            return level switch
            {
                <= 10 => DungeonTheme.Catacombs,
                <= 20 => DungeonTheme.Sewers,
                <= 35 => DungeonTheme.Caverns,
                <= 50 => DungeonTheme.AncientRuins,
                <= 65 => DungeonTheme.DemonLair,
                <= 80 => DungeonTheme.FrozenDepths,
                <= 90 => DungeonTheme.VolcanicPit,
                _ => DungeonTheme.AbyssalVoid
            };
        }

        private static int CalculateDangerLevel(int level)
        {
            // 1-10 danger rating
            return Math.Min(10, 1 + (level / 10));
        }

        private static void GenerateRooms(DungeonFloor floor, int count)
        {
            var roomTypes = new List<RoomType>
            {
                RoomType.Corridor, RoomType.Corridor, // More corridors
                RoomType.Chamber, RoomType.Chamber, RoomType.Chamber,
                RoomType.Hall,
                RoomType.Alcove, RoomType.Alcove,
                RoomType.Shrine,
                RoomType.Crypt
            };

            for (int i = 0; i < count; i++)
            {
                var roomType = roomTypes[random.Next(roomTypes.Count)];
                var room = CreateRoom(floor.Theme, roomType, i, floor.Level);
                floor.Rooms.Add(room);
            }
        }

        private static DungeonRoom CreateRoom(DungeonTheme theme, RoomType type, int index, int level)
        {
            var room = new DungeonRoom
            {
                Id = $"room_{index}",
                Type = type,
                Theme = theme,
                IsExplored = false,
                IsCleared = false,
                DangerRating = random.Next(1, 4) // 1-3 danger per room
            };

            // Generate room name and description based on theme and type
            (room.Name, room.Description, room.AtmosphereText) = GenerateRoomFlavor(theme, type, level);

            // Determine what's in the room
            room.HasMonsters = random.NextDouble() < 0.6; // 60% chance of monsters
            room.HasTreasure = random.NextDouble() < 0.25; // 25% chance of treasure
            room.HasEvent = random.NextDouble() < 0.3; // 30% chance of special event
            room.HasTrap = random.NextDouble() < 0.15; // 15% chance of trap

            // Generate features to examine
            room.Features = GenerateRoomFeatures(theme, type);

            return room;
        }

        private static (string name, string desc, string atmosphere) GenerateRoomFlavor(
            DungeonTheme theme, RoomType type, int level)
        {
            return (theme, type) switch
            {
                // Catacombs
                (DungeonTheme.Catacombs, RoomType.Corridor) => (
                    "Dusty Passage",
                    "A narrow corridor lined with ancient bones set into alcoves.",
                    "The air is thick with dust. Cobwebs hang from the ceiling."
                ),
                (DungeonTheme.Catacombs, RoomType.Chamber) => (
                    "Burial Chamber",
                    "Stone sarcophagi line the walls of this circular room.",
                    "You hear faint scratching from within one of the coffins..."
                ),
                (DungeonTheme.Catacombs, RoomType.Hall) => (
                    "Hall of the Dead",
                    "A grand hall with rows of skeletal remains in ceremonial poses.",
                    "Candles that should have burned out centuries ago still flicker."
                ),
                (DungeonTheme.Catacombs, RoomType.Shrine) => (
                    "Forgotten Altar",
                    "A crumbling altar dedicated to a god whose name has been lost.",
                    "Something about this place makes you uneasy."
                ),
                (DungeonTheme.Catacombs, RoomType.Crypt) => (
                    "Noble's Crypt",
                    "The ornate tomb of someone important lies here, partially looted.",
                    "Gold leaf still clings to the walls despite the decay."
                ),
                (DungeonTheme.Catacombs, RoomType.Alcove) => (
                    "Hidden Nook",
                    "A small alcove carved into the stone, perhaps once a prayer spot.",
                    "Dried flowers crumble at your touch."
                ),

                // Sewers
                (DungeonTheme.Sewers, RoomType.Corridor) => (
                    "Waste Channel",
                    "Murky water flows through this tunnel. The smell is overwhelming.",
                    "Something moves in the water. Probably just rats. Probably."
                ),
                (DungeonTheme.Sewers, RoomType.Chamber) => (
                    "Junction Room",
                    "Several pipes meet here, creating a round chamber.",
                    "The walls are slick with unidentifiable slime."
                ),
                (DungeonTheme.Sewers, RoomType.Hall) => (
                    "Maintenance Hall",
                    "An old maintenance area, now home to things that prefer the dark.",
                    "Rusted tools hang on pegs. A workbench lies overturned."
                ),

                // Caverns
                (DungeonTheme.Caverns, RoomType.Corridor) => (
                    "Winding Tunnel",
                    "Natural stone formations create a twisting path.",
                    "Phosphorescent fungi cast an eerie blue glow."
                ),
                (DungeonTheme.Caverns, RoomType.Chamber) => (
                    "Crystal Grotto",
                    "Clusters of crystals jut from every surface.",
                    "The crystals hum with a frequency you can feel in your teeth."
                ),
                (DungeonTheme.Caverns, RoomType.Hall) => (
                    "Vast Cavern",
                    "The ceiling stretches into darkness. Stalactites hang like teeth.",
                    "Your footsteps echo endlessly. You are not alone."
                ),

                // Ancient Ruins
                (DungeonTheme.AncientRuins, RoomType.Corridor) => (
                    "Ruined Hallway",
                    "Crumbling pillars line what was once a grand corridor.",
                    "Faded murals depict scenes of a forgotten civilization."
                ),
                (DungeonTheme.AncientRuins, RoomType.Chamber) => (
                    "Ritual Chamber",
                    "Strange symbols cover the floor in concentric circles.",
                    "The air crackles with residual magical energy."
                ),
                (DungeonTheme.AncientRuins, RoomType.Hall) => (
                    "Great Library",
                    "Shelves of rotted books stretch to the ceiling.",
                    "Some tomes seem to whisper as you pass."
                ),
                (DungeonTheme.AncientRuins, RoomType.Shrine) => (
                    "Temple of the Old Gods",
                    "A massive statue looms over a blood-stained altar.",
                    "You feel watched. Judged."
                ),

                // Demon Lair
                (DungeonTheme.DemonLair, RoomType.Corridor) => (
                    "Bone Corridor",
                    "The walls are made entirely of fused bones.",
                    "Screams echo from somewhere ahead. Or is it behind?"
                ),
                (DungeonTheme.DemonLair, RoomType.Chamber) => (
                    "Torture Chamber",
                    "Hooks and chains hang from the ceiling. Fresh blood pools on the floor.",
                    "Something terrible happened here. Recently."
                ),
                (DungeonTheme.DemonLair, RoomType.Hall) => (
                    "Summoning Hall",
                    "A massive pentagram is carved into the floor.",
                    "The temperature drops. Your breath fogs."
                ),

                // Frozen Depths
                (DungeonTheme.FrozenDepths, RoomType.Corridor) => (
                    "Ice Tunnel",
                    "Walls of solid ice reflect your torchlight infinitely.",
                    "Your breath crystallizes instantly. It's dangerously cold."
                ),
                (DungeonTheme.FrozenDepths, RoomType.Chamber) => (
                    "Frozen Tomb",
                    "Figures are preserved in the ice, their faces frozen in terror.",
                    "One of them blinks."
                ),

                // Volcanic Pit
                (DungeonTheme.VolcanicPit, RoomType.Corridor) => (
                    "Lava Tube",
                    "The rock here is smooth, formed by ancient magma flows.",
                    "The heat is intense. Sweat pours down your face."
                ),
                (DungeonTheme.VolcanicPit, RoomType.Chamber) => (
                    "Magma Chamber",
                    "A river of molten rock flows through the center of this room.",
                    "The air shimmers with heat. One wrong step means death."
                ),

                // Abyssal Void
                (DungeonTheme.AbyssalVoid, RoomType.Corridor) => (
                    "Void Passage",
                    "This corridor seems to exist outside normal space.",
                    "Gravity feels... optional. Your sanity strains."
                ),
                (DungeonTheme.AbyssalVoid, RoomType.Chamber) => (
                    "Heart of Madness",
                    "Reality bends here. Up is down. Inside is outside.",
                    "You're not sure you're still you."
                ),

                // Default fallback
                _ => (
                    $"{type} ({theme})",
                    $"A {type.ToString().ToLower()} in the {theme.ToString().ToLower()}.",
                    "The darkness presses in around you."
                )
            };
        }

        private static List<RoomFeature> GenerateRoomFeatures(DungeonTheme theme, RoomType type)
        {
            var features = new List<RoomFeature>();
            int featureCount = random.Next(1, 4);

            var possibleFeatures = GetThemeFeatures(theme);

            for (int i = 0; i < featureCount && possibleFeatures.Count > 0; i++)
            {
                var idx = random.Next(possibleFeatures.Count);
                features.Add(possibleFeatures[idx]);
                possibleFeatures.RemoveAt(idx);
            }

            return features;
        }

        private static List<RoomFeature> GetThemeFeatures(DungeonTheme theme)
        {
            return theme switch
            {
                DungeonTheme.Catacombs => new List<RoomFeature>
                {
                    new("pile of bones", "Ancient bones, picked clean long ago.", FeatureInteraction.Examine),
                    new("stone coffin", "A heavy stone lid covers this sarcophagus.", FeatureInteraction.Open),
                    new("crumbling wall", "This section of wall looks weak.", FeatureInteraction.Break),
                    new("faded inscription", "Words carved into stone, mostly illegible.", FeatureInteraction.Read),
                    new("rusted gate", "A corroded iron gate blocks a passage.", FeatureInteraction.Open),
                    new("burial urn", "A clay urn that might contain valuables. Or ashes.", FeatureInteraction.Open)
                },
                DungeonTheme.Sewers => new List<RoomFeature>
                {
                    new("drainage grate", "Something glints beneath the grate.", FeatureInteraction.Open),
                    new("suspicious pile", "A mound of refuse. Something might be hidden in it.", FeatureInteraction.Search),
                    new("rusted valve", "An old valve. Turning it might do something.", FeatureInteraction.Use),
                    new("dead body", "A recent victim. They might have supplies.", FeatureInteraction.Search),
                    new("crack in the wall", "Wide enough to squeeze through?", FeatureInteraction.Enter)
                },
                DungeonTheme.Caverns => new List<RoomFeature>
                {
                    new("crystal cluster", "Beautiful crystals. Might be valuable.", FeatureInteraction.Take),
                    new("underground pool", "Dark water. Something ripples beneath.", FeatureInteraction.Examine),
                    new("narrow crevice", "A tight squeeze, but passable.", FeatureInteraction.Enter),
                    new("glowing mushrooms", "Bioluminescent fungi. Edible?", FeatureInteraction.Take),
                    new("rock formation", "An unusual shape. Natural? Or carved?", FeatureInteraction.Examine)
                },
                DungeonTheme.AncientRuins => new List<RoomFeature>
                {
                    new("ancient chest", "A ornate chest, surprisingly intact.", FeatureInteraction.Open),
                    new("magical runes", "Glowing symbols pulse with power.", FeatureInteraction.Read),
                    new("broken statue", "Only the base remains. Something was here.", FeatureInteraction.Examine),
                    new("hidden alcove", "A concealed space behind a tapestry.", FeatureInteraction.Search),
                    new("mechanism", "Gears and levers. An ancient device.", FeatureInteraction.Use)
                },
                DungeonTheme.DemonLair => new List<RoomFeature>
                {
                    new("blood pool", "Fresh blood. Still warm.", FeatureInteraction.Examine),
                    new("torture device", "A horrific contraption. Something is strapped to it.", FeatureInteraction.Examine),
                    new("demonic altar", "An altar radiating evil. Offerings sit upon it.", FeatureInteraction.Take),
                    new("cage", "Someone is locked inside, barely alive.", FeatureInteraction.Open),
                    new("portal fragment", "A tear in reality. Looking into it hurts.", FeatureInteraction.Examine)
                },
                _ => new List<RoomFeature>
                {
                    new("old chest", "A weathered chest.", FeatureInteraction.Open),
                    new("strange markings", "Symbols you don't recognize.", FeatureInteraction.Read),
                    new("pile of debris", "Might be hiding something.", FeatureInteraction.Search)
                }
            };
        }

        private static void ConnectRooms(DungeonFloor floor)
        {
            // Create a connected graph of rooms
            // First, ensure all rooms are reachable (minimum spanning tree)
            var connected = new HashSet<string> { floor.Rooms[0].Id };
            var unconnected = new HashSet<string>(floor.Rooms.Skip(1).Select(r => r.Id));

            while (unconnected.Count > 0)
            {
                // Pick a random connected room and connect it to a random unconnected room
                var fromRoom = floor.Rooms.First(r => connected.Contains(r.Id) && r.Exits.Count < 4);
                var toRoomId = unconnected.First();
                var toRoom = floor.Rooms.First(r => r.Id == toRoomId);

                // Determine exit directions
                var availableDirs = GetAvailableDirections(fromRoom);
                if (availableDirs.Count > 0)
                {
                    var dir = availableDirs[random.Next(availableDirs.Count)];
                    var oppositeDir = GetOppositeDirection(dir);

                    fromRoom.Exits[dir] = new RoomExit(toRoomId, GetExitDescription(dir, floor.Theme));
                    toRoom.Exits[oppositeDir] = new RoomExit(fromRoom.Id, GetExitDescription(oppositeDir, floor.Theme));

                    connected.Add(toRoomId);
                    unconnected.Remove(toRoomId);
                }
            }

            // Add some extra connections for variety (loops)
            int extraConnections = floor.Rooms.Count / 3;
            for (int i = 0; i < extraConnections; i++)
            {
                var room1 = floor.Rooms[random.Next(floor.Rooms.Count)];
                var room2 = floor.Rooms[random.Next(floor.Rooms.Count)];

                if (room1.Id != room2.Id && room1.Exits.Count < 4 && room2.Exits.Count < 4)
                {
                    var availableDirs1 = GetAvailableDirections(room1);
                    var availableDirs2 = GetAvailableDirections(room2);

                    if (availableDirs1.Count > 0 && availableDirs2.Count > 0)
                    {
                        var dir = availableDirs1[random.Next(availableDirs1.Count)];
                        var oppositeDir = GetOppositeDirection(dir);

                        if (availableDirs2.Contains(oppositeDir))
                        {
                            room1.Exits[dir] = new RoomExit(room2.Id, GetExitDescription(dir, floor.Theme));
                            room2.Exits[oppositeDir] = new RoomExit(room1.Id, GetExitDescription(oppositeDir, floor.Theme));
                        }
                    }
                }
            }
        }

        private static List<Direction> GetAvailableDirections(DungeonRoom room)
        {
            var all = new List<Direction> { Direction.North, Direction.South, Direction.East, Direction.West };
            return all.Where(d => !room.Exits.ContainsKey(d)).ToList();
        }

        private static Direction GetOppositeDirection(Direction dir)
        {
            return dir switch
            {
                Direction.North => Direction.South,
                Direction.South => Direction.North,
                Direction.East => Direction.West,
                Direction.West => Direction.East,
                _ => Direction.North
            };
        }

        private static string GetExitDescription(Direction dir, DungeonTheme theme)
        {
            var dirName = dir.ToString().ToLower();
            return theme switch
            {
                DungeonTheme.Catacombs => $"A dark passage leads {dirName}.",
                DungeonTheme.Sewers => $"A tunnel continues {dirName}.",
                DungeonTheme.Caverns => $"The cave extends {dirName}.",
                DungeonTheme.AncientRuins => $"An archway opens to the {dirName}.",
                DungeonTheme.DemonLair => $"A blood-red portal flickers to the {dirName}.",
                DungeonTheme.FrozenDepths => $"An icy corridor stretches {dirName}.",
                DungeonTheme.VolcanicPit => $"A heat-warped passage leads {dirName}.",
                DungeonTheme.AbyssalVoid => $"Reality bends {dirName}ward.",
                _ => $"An exit leads {dirName}."
            };
        }

        private static void PlaceSpecialRooms(DungeonFloor floor)
        {
            // Mark the last room as boss room
            var bossRoom = floor.Rooms.Last();
            bossRoom.IsBossRoom = true;
            bossRoom.Name = GetBossRoomName(floor.Theme);
            bossRoom.Description = GetBossRoomDescription(floor.Theme);
            bossRoom.HasMonsters = true;
            floor.BossRoomId = bossRoom.Id;

            // Add a treasure room
            var treasureRoom = floor.Rooms[floor.Rooms.Count / 2];
            if (!treasureRoom.IsBossRoom)
            {
                treasureRoom.Name = "Hidden Treasury";
                treasureRoom.HasTreasure = true;
                treasureRoom.HasTrap = true;
                floor.TreasureRoomId = treasureRoom.Id;
            }

            // Add stairs down
            var stairsRoom = floor.Rooms.FirstOrDefault(r => !r.IsBossRoom && r != treasureRoom);
            if (stairsRoom != null)
            {
                stairsRoom.HasStairsDown = true;
                floor.StairsDownRoomId = stairsRoom.Id;
            }
        }

        private static string GetBossRoomName(DungeonTheme theme)
        {
            return theme switch
            {
                DungeonTheme.Catacombs => "The Bone Throne",
                DungeonTheme.Sewers => "The Abomination's Nest",
                DungeonTheme.Caverns => "Crystal Heart",
                DungeonTheme.AncientRuins => "The Sealed Sanctum",
                DungeonTheme.DemonLair => "Throne of Suffering",
                DungeonTheme.FrozenDepths => "The Frozen Core",
                DungeonTheme.VolcanicPit => "Magma Lord's Chamber",
                DungeonTheme.AbyssalVoid => "The End of All Things",
                _ => "Boss Chamber"
            };
        }

        private static string GetBossRoomDescription(DungeonTheme theme)
        {
            return theme switch
            {
                DungeonTheme.Catacombs => "A massive chamber dominated by a throne made entirely of bones. Something ancient stirs.",
                DungeonTheme.Sewers => "The stench is overwhelming. Something massive has made this place its home.",
                DungeonTheme.Caverns => "A giant crystal dominates the room, pulsing with malevolent energy.",
                DungeonTheme.AncientRuins => "The final chamber, sealed for millennia. You have awoken what sleeps within.",
                DungeonTheme.DemonLair => "Chains rattle. The floor is a carpet of tortured souls. The demon lord awaits.",
                DungeonTheme.FrozenDepths => "Ice so cold it burns. A massive figure frozen in the wall begins to crack free.",
                DungeonTheme.VolcanicPit => "A river of magma encircles an obsidian platform. A creature of fire rises.",
                DungeonTheme.AbyssalVoid => "This is it. The heart of madness. Reality itself screams.",
                _ => "A powerful presence fills this room."
            };
        }

        private static void PopulateEvents(DungeonFloor floor)
        {
            foreach (var room in floor.Rooms)
            {
                if (room.HasEvent && !room.IsBossRoom)
                {
                    room.EventType = GetRandomEventType(floor.Theme);
                }
            }
        }

        private static DungeonEventType GetRandomEventType(DungeonTheme theme)
        {
            var events = new[]
            {
                DungeonEventType.TreasureChest,
                DungeonEventType.Merchant,
                DungeonEventType.Shrine,
                DungeonEventType.Trap,
                DungeonEventType.NPCEncounter,
                DungeonEventType.Puzzle,
                DungeonEventType.RestSpot,
                DungeonEventType.MysteryEvent
            };

            return events[random.Next(events.Length)];
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // DATA STRUCTURES
    // ═══════════════════════════════════════════════════════════════════════════

    public class DungeonFloor
    {
        public int Level { get; set; }
        public DungeonTheme Theme { get; set; }
        public int DangerLevel { get; set; }
        public List<DungeonRoom> Rooms { get; set; } = new();
        public string EntranceRoomId { get; set; } = "";
        public string CurrentRoomId { get; set; } = "";
        public string BossRoomId { get; set; } = "";
        public string TreasureRoomId { get; set; } = "";
        public string StairsDownRoomId { get; set; } = "";
        public bool BossDefeated { get; set; } = false;
        public int MonstersKilled { get; set; } = 0;
        public int TreasuresFound { get; set; } = 0;
        public DateTime EnteredAt { get; set; } = DateTime.Now;

        public DungeonRoom GetCurrentRoom() => Rooms.FirstOrDefault(r => r.Id == CurrentRoomId);
        public DungeonRoom GetRoom(string id) => Rooms.FirstOrDefault(r => r.Id == id);
    }

    public class DungeonRoom
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string AtmosphereText { get; set; } = "";
        public RoomType Type { get; set; }
        public DungeonTheme Theme { get; set; }
        public Dictionary<Direction, RoomExit> Exits { get; set; } = new();
        public List<RoomFeature> Features { get; set; } = new();
        public bool IsExplored { get; set; } = false;
        public bool IsCleared { get; set; } = false;
        public bool HasMonsters { get; set; } = false;
        public bool HasTreasure { get; set; } = false;
        public bool HasEvent { get; set; } = false;
        public bool HasTrap { get; set; } = false;
        public bool HasStairsDown { get; set; } = false;
        public bool IsBossRoom { get; set; } = false;
        public int DangerRating { get; set; } = 1;
        public DungeonEventType EventType { get; set; }
        public List<Monster> Monsters { get; set; } = new();
        public bool TrapTriggered { get; set; } = false;
        public bool TreasureLooted { get; set; } = false;
        public bool EventCompleted { get; set; } = false;
    }

    public class RoomExit
    {
        public string TargetRoomId { get; set; }
        public string Description { get; set; }
        public bool IsLocked { get; set; } = false;
        public bool IsHidden { get; set; } = false;
        public bool IsRevealed { get; set; } = true;

        public RoomExit(string targetId, string desc)
        {
            TargetRoomId = targetId;
            Description = desc;
        }
    }

    public class RoomFeature
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public FeatureInteraction Interaction { get; set; }
        public bool IsInteracted { get; set; } = false;

        public RoomFeature(string name, string desc, FeatureInteraction interaction)
        {
            Name = name;
            Description = desc;
            Interaction = interaction;
        }
    }

    public enum Direction { North, South, East, West }
    public enum RoomType { Corridor, Chamber, Hall, Alcove, Shrine, Crypt }
    public enum DungeonTheme { Catacombs, Sewers, Caverns, AncientRuins, DemonLair, FrozenDepths, VolcanicPit, AbyssalVoid }
    public enum FeatureInteraction { Examine, Open, Search, Read, Take, Use, Break, Enter }
    public enum DungeonEventType { None, TreasureChest, Merchant, Shrine, Trap, NPCEncounter, Puzzle, RestSpot, MysteryEvent }
}
