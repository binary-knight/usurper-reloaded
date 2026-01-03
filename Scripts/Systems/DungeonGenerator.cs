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

            // Determine floor size based on level - EXPANDED for epic dungeons
            // Base 15 rooms, scaling up to 25 for deeper levels
            int roomCount = 15 + (level / 8);
            roomCount = Math.Clamp(roomCount, 15, 25);

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
            // Standard room types (weighted for variety)
            var standardRoomTypes = new List<RoomType>
            {
                RoomType.Corridor, RoomType.Corridor, RoomType.Corridor,
                RoomType.Chamber, RoomType.Chamber, RoomType.Chamber, RoomType.Chamber,
                RoomType.Hall, RoomType.Hall,
                RoomType.Alcove, RoomType.Alcove, RoomType.Alcove,
                RoomType.Shrine,
                RoomType.Crypt, RoomType.Crypt
            };

            // Special room types (appear less frequently)
            var specialRoomTypes = new List<RoomType>
            {
                RoomType.PuzzleRoom,
                RoomType.RiddleGate,
                RoomType.LoreLibrary,
                RoomType.MeditationChamber,
                RoomType.TrapGauntlet,
                RoomType.ArenaRoom
            };

            // Calculate special room distribution based on floor level
            int puzzleRooms = 1 + (floor.Level / 20);       // 1-5 puzzle rooms
            int secretRooms = 1 + (floor.Level / 25);       // 1-4 secret rooms
            int loreRooms = floor.Level >= 15 ? 1 : 0;      // Lore rooms after level 15
            int meditationRooms = floor.Level >= 10 ? 1 : 0; // Meditation after level 10
            int memoryRooms = floor.Level >= 20 && floor.Level % 15 == 0 ? 1 : 0; // Memory fragments on specific floors

            // Generate standard rooms first
            int standardCount = count - puzzleRooms - secretRooms - loreRooms - meditationRooms - memoryRooms;
            standardCount = Math.Max(standardCount, count / 2); // At least half are standard

            for (int i = 0; i < count; i++)
            {
                RoomType roomType;

                if (i == 0)
                {
                    // First room is always entrance-friendly
                    roomType = RoomType.Hall;
                }
                else if (i == count - 1)
                {
                    // Last room is boss antechamber leading to boss
                    roomType = RoomType.BossAntechamber;
                }
                else if (i < standardCount)
                {
                    roomType = standardRoomTypes[random.Next(standardRoomTypes.Count)];
                }
                else
                {
                    // Distribute special rooms
                    int specialIndex = i - standardCount;
                    if (specialIndex < puzzleRooms)
                        roomType = random.NextDouble() < 0.5 ? RoomType.PuzzleRoom : RoomType.RiddleGate;
                    else if (specialIndex < puzzleRooms + secretRooms)
                        roomType = RoomType.SecretVault;
                    else if (specialIndex < puzzleRooms + secretRooms + loreRooms)
                        roomType = RoomType.LoreLibrary;
                    else if (specialIndex < puzzleRooms + secretRooms + loreRooms + meditationRooms)
                        roomType = RoomType.MeditationChamber;
                    else if (specialIndex < puzzleRooms + secretRooms + loreRooms + meditationRooms + memoryRooms)
                        roomType = RoomType.MemoryFragment;
                    else
                        roomType = specialRoomTypes[random.Next(specialRoomTypes.Count)];
                }

                var room = CreateRoom(floor.Theme, roomType, i, floor.Level);
                floor.Rooms.Add(room);
            }

            // Shuffle rooms (except first and last) for randomness
            var middleRooms = floor.Rooms.Skip(1).Take(floor.Rooms.Count - 2).ToList();
            for (int i = middleRooms.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                var temp = middleRooms[i];
                middleRooms[i] = middleRooms[j];
                middleRooms[j] = temp;
            }

            // Reconstruct with shuffled middle
            var first = floor.Rooms[0];
            var last = floor.Rooms[floor.Rooms.Count - 1];
            floor.Rooms.Clear();
            floor.Rooms.Add(first);
            floor.Rooms.AddRange(middleRooms);
            floor.Rooms.Add(last);

            // Re-assign IDs after shuffle
            for (int i = 0; i < floor.Rooms.Count; i++)
            {
                floor.Rooms[i].Id = $"room_{i}";
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

                // ═══════════════════════════════════════════════════════════════
                // NEW SPECIAL ROOM TYPES (theme-agnostic with thematic variations)
                // ═══════════════════════════════════════════════════════════════

                // Puzzle Rooms
                (_, RoomType.PuzzleRoom) => GetPuzzleRoomFlavor(theme, level),
                (_, RoomType.RiddleGate) => GetRiddleGateFlavor(theme),
                (_, RoomType.SecretVault) => GetSecretVaultFlavor(theme),
                (_, RoomType.LoreLibrary) => GetLoreLibraryFlavor(theme),
                (_, RoomType.MeditationChamber) => GetMeditationChamberFlavor(theme),
                (_, RoomType.BossAntechamber) => GetBossAntechamberFlavor(theme),
                (_, RoomType.TrapGauntlet) => GetTrapGauntletFlavor(theme),
                (_, RoomType.ArenaRoom) => GetArenaRoomFlavor(theme),
                (_, RoomType.MerchantDen) => (
                    "Wanderer's Haven",
                    "A hidden alcove where a merchant has set up shop, seemingly unbothered by the dangers.",
                    "Exotic goods glitter in the lamplight. How does he survive down here?"
                ),
                (_, RoomType.MemoryFragment) => GetMemoryFragmentFlavor(theme),

                // Default fallback
                _ => (
                    $"{type} ({theme})",
                    $"A {type.ToString().ToLower()} in the {theme.ToString().ToLower()}.",
                    "The darkness presses in around you."
                )
            };
        }

        #region Special Room Flavor Methods

        private static (string name, string desc, string atmosphere) GetPuzzleRoomFlavor(DungeonTheme theme, int level)
        {
            return theme switch
            {
                DungeonTheme.Catacombs => (
                    "Chamber of Bones",
                    "Skeletal arms protrude from the walls, each holding a lever. The sequence matters.",
                    "A cryptic inscription reads: 'The dead remember the order of their passing.'"
                ),
                DungeonTheme.AncientRuins => (
                    "Hall of Glyphs",
                    "Glowing symbols cover every surface. Some respond to touch, others to sound.",
                    "The air hums with dormant magic, waiting to be awakened... or angered."
                ),
                DungeonTheme.Caverns => (
                    "Crystal Resonance Chamber",
                    "Massive crystals of different colors stand in a circle. They hum when approached.",
                    "Strike them in the right order, and perhaps the way forward will reveal itself."
                ),
                DungeonTheme.DemonLair => (
                    "Trial of Pain",
                    "Five altars, each demanding a different sacrifice. Blood, tears, fear, hope, and truth.",
                    "The demons value cleverness as much as suffering."
                ),
                _ => (
                    "Puzzle Chamber",
                    "An ancient mechanism dominates the room, its purpose unclear but clearly important.",
                    "Something here must be solved before you can proceed."
                )
            };
        }

        private static (string name, string desc, string atmosphere) GetRiddleGateFlavor(DungeonTheme theme)
        {
            return theme switch
            {
                DungeonTheme.Catacombs => (
                    "Guardian's Gate",
                    "A spectral figure blocks the passage, its empty eyes regarding you with ancient intelligence.",
                    "'Answer my riddle, or join the dead who failed before you.'"
                ),
                DungeonTheme.AncientRuins => (
                    "Sphinx's Threshold",
                    "A stone face carved into the door speaks with a voice like grinding rocks.",
                    "'Wisdom is the key that opens all doors. Prove yours.'"
                ),
                DungeonTheme.AbyssalVoid => (
                    "The Questioning Dark",
                    "The darkness itself forms a face, asking questions that cut to the core of existence.",
                    "'What are you, wave, but water that has forgotten itself?'"
                ),
                _ => (
                    "Riddle Gate",
                    "A mystical barrier blocks your path. An ancient voice demands you answer its challenge.",
                    "Fail, and there will be consequences..."
                )
            };
        }

        private static (string name, string desc, string atmosphere) GetSecretVaultFlavor(DungeonTheme theme)
        {
            return theme switch
            {
                DungeonTheme.Catacombs => (
                    "Hidden Ossuary",
                    "Behind a false wall lies a chamber filled with treasures buried with the dead.",
                    "The previous owners no longer need these things. Probably."
                ),
                DungeonTheme.AncientRuins => (
                    "Sealed Treasury",
                    "A vault that has remained hidden for millennia, its seals now broken by your discovery.",
                    "Artifacts of immense power rest on pedestals of pure gold."
                ),
                DungeonTheme.DemonLair => (
                    "Forbidden Hoard",
                    "A demon's personal collection of cursed artifacts and stolen souls.",
                    "Every item here has a terrible price. Worth paying?"
                ),
                _ => (
                    "Secret Vault",
                    "A hidden chamber filled with treasures that someone went to great lengths to conceal.",
                    "The air is thick with the smell of gold and ancient secrets."
                )
            };
        }

        private static (string name, string desc, string atmosphere) GetLoreLibraryFlavor(DungeonTheme theme)
        {
            return theme switch
            {
                DungeonTheme.AncientRuins => (
                    "Archive of the First Age",
                    "Crystalline tablets line the walls, each containing memories from before the gods fell.",
                    "The knowledge here predates Manwe's sorrow. Handle it carefully."
                ),
                DungeonTheme.AbyssalVoid => (
                    "Library of Unwritten Truths",
                    "Books float in the void, their pages filled with words that haven't been thought yet.",
                    "One tome catches your eye: 'What the Wave Forgot.'"
                ),
                _ => (
                    "Fragment Repository",
                    "Ancient texts and carved stones preserve knowledge from a forgotten age.",
                    "Here lie pieces of truth that the world has tried to forget."
                )
            };
        }

        private static (string name, string desc, string atmosphere) GetMeditationChamberFlavor(DungeonTheme theme)
        {
            return theme switch
            {
                DungeonTheme.Caverns => (
                    "Pool of Stillness",
                    "An underground spring feeds a perfectly calm pool. The silence here is absolute.",
                    "Sit. Breathe. The water remembers what you have forgotten."
                ),
                DungeonTheme.AncientRuins => (
                    "Sanctuary of the Old Ways",
                    "A meditation circle surrounded by faded murals of gods in harmony.",
                    "Before the corruption, the gods would rest here. Their peace lingers."
                ),
                DungeonTheme.AbyssalVoid => (
                    "Eye of the Storm",
                    "A bubble of calm exists here, surrounded by the chaos of the void.",
                    "In the center of madness, you find surprising clarity."
                ),
                _ => (
                    "Meditation Chamber",
                    "A peaceful alcove where weary souls can find rest and insight.",
                    "The walls seem to absorb your troubles. Rest here. Dream."
                )
            };
        }

        private static (string name, string desc, string atmosphere) GetBossAntechamberFlavor(DungeonTheme theme)
        {
            return theme switch
            {
                DungeonTheme.Catacombs => (
                    "Threshold of the Bone King",
                    "Skulls of a thousand warriors line the walls, their empty gazes fixed on the door ahead.",
                    "Turn back now, or add your skull to the collection."
                ),
                DungeonTheme.DemonLair => (
                    "Gates of Torment",
                    "The screaming grows louder. The walls pulse with trapped souls.",
                    "Beyond this door, something ancient and terrible awaits."
                ),
                DungeonTheme.AbyssalVoid => (
                    "Edge of Understanding",
                    "Reality thins here. Through the door, you sense... yourself?",
                    "The greatest enemy is the one you cannot escape."
                ),
                _ => (
                    "Boss Antechamber",
                    "The air grows heavy. Power radiates from beyond the door ahead.",
                    "Prepare yourself. There is no turning back."
                )
            };
        }

        private static (string name, string desc, string atmosphere) GetTrapGauntletFlavor(DungeonTheme theme)
        {
            return theme switch
            {
                DungeonTheme.AncientRuins => (
                    "Corridor of Trials",
                    "The ancients protected their treasures well. Pressure plates, darts, and worse await.",
                    "Tread carefully. The builders were paranoid for good reason."
                ),
                DungeonTheme.DemonLair => (
                    "Passage of Suffering",
                    "Every step brings new pain. The demons designed this as entertainment.",
                    "Your screams will echo forever in these halls."
                ),
                _ => (
                    "Trap Gauntlet",
                    "A long corridor filled with obvious traps. And probably some not-so-obvious ones.",
                    "Speed and caution - you'll need both."
                )
            };
        }

        private static (string name, string desc, string atmosphere) GetArenaRoomFlavor(DungeonTheme theme)
        {
            return theme switch
            {
                DungeonTheme.DemonLair => (
                    "Blood Arena",
                    "A fighting pit surrounded by demonic spectators frozen in time, waiting for combat.",
                    "Fight, and they will watch. Die, and they will feast."
                ),
                DungeonTheme.Caverns => (
                    "Natural Amphitheater",
                    "The cave opens into a circular arena. Bones of previous combatants litter the floor.",
                    "Something here enjoys watching fights. Something large."
                ),
                _ => (
                    "Combat Arena",
                    "A circular chamber clearly designed for battle. Multiple opponents await.",
                    "Survive the gauntlet, and the way forward opens."
                )
            };
        }

        private static (string name, string desc, string atmosphere) GetMemoryFragmentFlavor(DungeonTheme theme)
        {
            return theme switch
            {
                DungeonTheme.AbyssalVoid => (
                    "Echo of Self",
                    "A mirror that doesn't show your reflection. It shows someone else. Someone... familiar.",
                    "You have been here before. You have been everywhere before."
                ),
                DungeonTheme.AncientRuins => (
                    "Chamber of Remembrance",
                    "Faded murals depict your face. But these are thousands of years old.",
                    "The amnesia cracks. Something wants to be remembered."
                ),
                _ => (
                    "Memory Fragment",
                    "This place triggers something deep in your mind. You've seen this before.",
                    "Close your eyes. Let it come back to you..."
                )
            };
        }

        #endregion

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

            // Add stairs down (placed in middle-ish area, not too easy to find)
            int stairsIndex = random.Next(floor.Rooms.Count / 3, (floor.Rooms.Count * 2) / 3);
            var stairsRoom = floor.Rooms[stairsIndex];
            if (stairsRoom.IsBossRoom || stairsRoom == treasureRoom)
            {
                stairsRoom = floor.Rooms.FirstOrDefault(r => !r.IsBossRoom && r != treasureRoom);
            }
            if (stairsRoom != null)
            {
                stairsRoom.HasStairsDown = true;
                floor.StairsDownRoomId = stairsRoom.Id;
            }

            // Create secret rooms with hidden exits (10% of connections)
            CreateSecretConnections(floor);

            // Set up special room properties based on type
            ConfigureSpecialRoomTypes(floor);

            // Place lore fragments in lore libraries
            PlaceLoreFragments(floor);

            // Potentially place a secret boss on certain floors
            PlaceSecretBoss(floor);
        }

        private static void CreateSecretConnections(DungeonFloor floor)
        {
            // Find all SecretVault rooms and make their entrances hidden
            foreach (var room in floor.Rooms.Where(r => r.Type == RoomType.SecretVault))
            {
                room.IsSecretRoom = true;

                // Find any exits leading TO this room and mark them hidden
                foreach (var otherRoom in floor.Rooms)
                {
                    foreach (var exit in otherRoom.Exits.Values)
                    {
                        if (exit.TargetRoomId == room.Id)
                        {
                            exit.IsHidden = true;
                            exit.IsRevealed = false;
                            exit.Description = "A faint draft suggests a hidden passage...";
                        }
                    }
                }
            }

            // Additionally, 10% of random connections become hidden passages
            int hiddenCount = Math.Max(1, floor.Rooms.Count / 10);
            var eligibleRooms = floor.Rooms
                .Where(r => !r.IsBossRoom && !r.IsSecretRoom && r.Exits.Count > 1)
                .ToList();

            for (int i = 0; i < hiddenCount && eligibleRooms.Count > 0; i++)
            {
                var room = eligibleRooms[random.Next(eligibleRooms.Count)];
                var exitDir = room.Exits.Keys.ToList()[random.Next(room.Exits.Count)];
                var exit = room.Exits[exitDir];

                if (!exit.IsHidden)
                {
                    exit.IsHidden = true;
                    exit.IsRevealed = false;
                    exit.Description = GetHiddenExitDescription(floor.Theme, exitDir);
                }
            }
        }

        private static string GetHiddenExitDescription(DungeonTheme theme, Direction dir)
        {
            return theme switch
            {
                DungeonTheme.Catacombs => "A loose stone conceals a narrow passage.",
                DungeonTheme.Sewers => "Behind the flowing water, a gap in the wall...",
                DungeonTheme.Caverns => "A crevice, barely visible in the crystal light.",
                DungeonTheme.AncientRuins => "A concealed door, marked only by faded runes.",
                DungeonTheme.DemonLair => "A portal of shadow, visible only to those who look with fear.",
                _ => "A hidden passage reveals itself to careful eyes."
            };
        }

        private static void ConfigureSpecialRoomTypes(DungeonFloor floor)
        {
            foreach (var room in floor.Rooms)
            {
                switch (room.Type)
                {
                    case RoomType.PuzzleRoom:
                        room.HasEvent = true;
                        room.EventType = DungeonEventType.Puzzle;
                        room.HasMonsters = false; // Puzzles first, then maybe combat
                        room.RequiresPuzzle = true;
                        room.PuzzleDifficulty = 1 + (floor.Level / 20);
                        break;

                    case RoomType.RiddleGate:
                        room.HasEvent = true;
                        room.EventType = DungeonEventType.Riddle;
                        room.HasMonsters = false;
                        room.RequiresRiddle = true;
                        room.RiddleDifficulty = 1 + (floor.Level / 25);
                        break;

                    case RoomType.LoreLibrary:
                        room.HasEvent = true;
                        room.EventType = DungeonEventType.LoreDiscovery;
                        room.HasMonsters = random.NextDouble() < 0.3; // Guardian?
                        room.ContainsLore = true;
                        break;

                    case RoomType.MeditationChamber:
                        room.HasEvent = true;
                        room.EventType = DungeonEventType.RestSpot;
                        room.HasMonsters = false;
                        room.IsSafeRoom = true;
                        room.GrantsInsight = floor.Level >= 30;
                        break;

                    case RoomType.TrapGauntlet:
                        room.HasTrap = true;
                        room.TrapCount = 2 + random.Next(3);
                        room.HasMonsters = false;
                        break;

                    case RoomType.ArenaRoom:
                        room.HasMonsters = true;
                        room.MonsterCount = 2 + random.Next(3);
                        room.IsArena = true;
                        room.HasTreasure = true; // Reward for surviving
                        break;

                    case RoomType.MerchantDen:
                        room.HasEvent = true;
                        room.EventType = DungeonEventType.Merchant;
                        room.HasMonsters = false;
                        room.IsSafeRoom = true;
                        break;

                    case RoomType.MemoryFragment:
                        room.HasEvent = true;
                        room.EventType = DungeonEventType.MemoryFlash;
                        room.HasMonsters = false;
                        room.TriggersMemory = true;
                        room.MemoryFragmentLevel = floor.Level / 15; // Which fragment
                        break;

                    case RoomType.BossAntechamber:
                        room.HasMonsters = random.NextDouble() < 0.5; // Elite guards?
                        room.HasTrap = random.NextDouble() < 0.3;
                        room.RequiresPuzzle = floor.Level >= 50; // Deeper floors need puzzle
                        break;

                    case RoomType.SecretVault:
                        room.HasTreasure = true;
                        room.TreasureQuality = TreasureQuality.Legendary;
                        room.HasTrap = true;
                        room.HasMonsters = random.NextDouble() < 0.4; // Guardian
                        break;
                }
            }
        }

        private static void PlaceLoreFragments(DungeonFloor floor)
        {
            var loreRooms = floor.Rooms.Where(r => r.Type == RoomType.LoreLibrary).ToList();

            foreach (var room in loreRooms)
            {
                // Determine which lore fragment based on floor level
                room.LoreFragmentType = floor.Level switch
                {
                    <= 20 => LoreFragmentType.OceanOrigin,
                    <= 35 => LoreFragmentType.FirstSeparation,
                    <= 50 => LoreFragmentType.TheForgetting,
                    <= 65 => LoreFragmentType.ManwesChoice,
                    <= 80 => LoreFragmentType.TheCorruption,
                    <= 95 => LoreFragmentType.TheCycle,
                    _ => LoreFragmentType.TheTruth
                };
            }
        }

        private static void PlaceSecretBoss(DungeonFloor floor)
        {
            // Secret bosses on specific floors
            int[] secretBossFloors = { 25, 50, 75, 99 };

            if (secretBossFloors.Contains(floor.Level))
            {
                // Find a SecretVault or create a hidden area for the secret boss
                var bossRoom = floor.Rooms.FirstOrDefault(r => r.Type == RoomType.SecretVault);

                if (bossRoom != null)
                {
                    bossRoom.HasSecretBoss = true;
                    bossRoom.SecretBossType = floor.Level switch
                    {
                        25 => SecretBossType.TheFirstWave,
                        50 => SecretBossType.TheForgottenEighth,
                        75 => SecretBossType.EchoOfSelf,
                        99 => SecretBossType.TheOceanSpeaks,
                        _ => SecretBossType.TheFirstWave
                    };
                    bossRoom.EventType = DungeonEventType.SecretBoss;
                    floor.HasSecretBoss = true;
                    floor.SecretBossRoomId = bossRoom.Id;
                }
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

        // New properties for expanded dungeons
        public bool HasSecretBoss { get; set; } = false;
        public string SecretBossRoomId { get; set; } = "";
        public bool SecretBossDefeated { get; set; } = false;
        public int PuzzlesSolved { get; set; } = 0;
        public int RiddlesAnswered { get; set; } = 0;
        public int SecretsFound { get; set; } = 0;
        public int LoreFragmentsCollected { get; set; } = 0;
        public List<string> RevealedSecretRooms { get; set; } = new();

        // Seven Seals story integration
        public bool HasUncollectedSeal { get; set; } = false;
        public UsurperRemake.Systems.SealType? SealType { get; set; }
        public bool SealCollected { get; set; } = false;
        public string SealRoomId { get; set; } = "";

        public DungeonRoom GetCurrentRoom() => Rooms.FirstOrDefault(r => r.Id == CurrentRoomId);
        public DungeonRoom GetRoom(string id) => Rooms.FirstOrDefault(r => r.Id == id);

        /// <summary>
        /// Get all visible exits from current room (respects hidden status)
        /// </summary>
        public Dictionary<Direction, RoomExit> GetVisibleExits()
        {
            var room = GetCurrentRoom();
            if (room == null) return new Dictionary<Direction, RoomExit>();

            return room.Exits
                .Where(e => !e.Value.IsHidden || e.Value.IsRevealed)
                .ToDictionary(e => e.Key, e => e.Value);
        }

        /// <summary>
        /// Reveal a hidden exit
        /// </summary>
        public bool RevealHiddenExit(string roomId, Direction direction)
        {
            var room = GetRoom(roomId);
            if (room == null || !room.Exits.TryGetValue(direction, out var exit))
                return false;

            if (exit.IsHidden && !exit.IsRevealed)
            {
                exit.IsRevealed = true;
                SecretsFound++;
                return true;
            }
            return false;
        }
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

        // ═══════════════════════════════════════════════════════════════
        // New properties for expanded dungeon system
        // ═══════════════════════════════════════════════════════════════

        // Secret room properties
        public bool IsSecretRoom { get; set; } = false;

        // Puzzle room properties
        public bool RequiresPuzzle { get; set; } = false;
        public int PuzzleDifficulty { get; set; } = 1;
        public bool PuzzleSolved { get; set; } = false;
        public PuzzleType? AssignedPuzzle { get; set; }

        // Riddle room properties
        public bool RequiresRiddle { get; set; } = false;
        public int RiddleDifficulty { get; set; } = 1;
        public bool RiddleAnswered { get; set; } = false;
        public int? AssignedRiddleId { get; set; }

        // Lore room properties
        public bool ContainsLore { get; set; } = false;
        public LoreFragmentType? LoreFragmentType { get; set; }
        public bool LoreCollected { get; set; } = false;

        // Safe room / meditation properties
        public bool IsSafeRoom { get; set; } = false;
        public bool GrantsInsight { get; set; } = false;
        public bool InsightGranted { get; set; } = false;

        // Arena properties
        public bool IsArena { get; set; } = false;
        public int MonsterCount { get; set; } = 1;

        // Trap gauntlet properties
        public int TrapCount { get; set; } = 1;
        public int TrapsDisarmed { get; set; } = 0;

        // Memory fragment properties
        public bool TriggersMemory { get; set; } = false;
        public int MemoryFragmentLevel { get; set; } = 0;
        public bool MemoryTriggered { get; set; } = false;

        // Treasure quality
        public TreasureQuality TreasureQuality { get; set; } = TreasureQuality.Normal;

        // Secret boss properties
        public bool HasSecretBoss { get; set; } = false;
        public SecretBossType? SecretBossType { get; set; }
        public bool SecretBossDefeated { get; set; } = false;

        /// <summary>
        /// Check if room is blocked by an unsolved puzzle or riddle
        /// </summary>
        public bool IsBlocked => (RequiresPuzzle && !PuzzleSolved) || (RequiresRiddle && !RiddleAnswered);

        /// <summary>
        /// Check if room has any unresolved content
        /// </summary>
        public bool HasUnresolvedContent =>
            (HasMonsters && !IsCleared) ||
            (HasTreasure && !TreasureLooted) ||
            (HasEvent && !EventCompleted) ||
            (HasTrap && !TrapTriggered) ||
            (RequiresPuzzle && !PuzzleSolved) ||
            (RequiresRiddle && !RiddleAnswered) ||
            (ContainsLore && !LoreCollected) ||
            (TriggersMemory && !MemoryTriggered) ||
            (HasSecretBoss && !SecretBossDefeated);
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
    public enum RoomType
    {
        Corridor, Chamber, Hall, Alcove, Shrine, Crypt,
        // New room types for expanded dungeons
        PuzzleRoom,         // Logic/environmental puzzle required
        RiddleGate,         // Guardian asks riddle to pass
        SecretVault,        // Hidden room with rare treasure
        LoreLibrary,        // Wave/Ocean philosophy fragments
        BossAntechamber,    // Pre-boss puzzle challenge
        MeditationChamber,  // Rest + Ocean insights
        TrapGauntlet,       // Multiple traps in sequence
        ArenaRoom,          // Combat challenge room
        MerchantDen,        // Hidden merchant location
        MemoryFragment      // Amnesia system reveals
    }
    public enum DungeonTheme { Catacombs, Sewers, Caverns, AncientRuins, DemonLair, FrozenDepths, VolcanicPit, AbyssalVoid }
    public enum FeatureInteraction { Examine, Open, Search, Read, Take, Use, Break, Enter }
    public enum DungeonEventType { None, TreasureChest, Merchant, Shrine, Trap, NPCEncounter, Puzzle, RestSpot, MysteryEvent, Riddle, LoreDiscovery, MemoryFlash, SecretBoss }

    /// <summary>
    /// Types of puzzles that can appear in dungeon rooms
    /// </summary>
    public enum PuzzleType
    {
        LeverSequence,      // Pull levers in correct order
        SymbolAlignment,    // Rotate/align symbols
        PressurePlates,     // Step on plates in order or with weight
        LightDarkness,      // Manipulate light sources
        NumberGrid,         // Solve number puzzle
        MemoryMatch,        // Remember and repeat pattern
        ItemCombination,    // Combine items to solve
        EnvironmentChange,  // Change room state (water, fire, etc.)
        CoordinationPuzzle, // Requires companion help
        ReflectionPuzzle    // Use mirrors/reflections
    }

    /// <summary>
    /// Lore fragment types that reveal Ocean Philosophy
    /// </summary>
    public enum LoreFragmentType
    {
        OceanOrigin,        // The vast Ocean before creation
        FirstSeparation,    // The Ocean dreams of waves
        TheForgetting,      // Waves must forget to feel separate
        ManwesChoice,       // The first wave's deep forgetting
        TheSevenDrops,      // The Old Gods as fragments
        TheCorruption,      // Separation becomes pain
        TheCycle,           // Why Manwe sends fragments
        TheReturn,          // Death is returning home
        TheTruth            // "You ARE the ocean"
    }

    /// <summary>
    /// Treasure quality tiers
    /// </summary>
    public enum TreasureQuality
    {
        Poor,       // Common items
        Normal,     // Standard loot
        Good,       // Above average
        Rare,       // Uncommon finds
        Epic,       // Very rare
        Legendary   // Best possible
    }

    /// <summary>
    /// Secret boss types hidden in dungeons
    /// </summary>
    public enum SecretBossType
    {
        TheFirstWave,       // Floor 25: The first being to separate from Ocean
        TheForgottenEighth, // Floor 50: A god Manwe erased from memory
        EchoOfSelf,         // Floor 75: Fight your past life
        TheOceanSpeaks      // Floor 99: The Ocean itself manifests
    }
}
