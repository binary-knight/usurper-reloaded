using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using UsurperRemake.Data;

namespace UsurperRemake.Systems
{
    /// <summary>
    /// NPC Spawn System - Creates and manages classic Usurper NPCs in the game world
    /// </summary>
    public class NPCSpawnSystem
    {
        private static NPCSpawnSystem? instance;
        public static NPCSpawnSystem Instance => instance ??= new NPCSpawnSystem();

        private List<NPC> spawnedNPCs = new();
        private Random random = new();
        private bool npcsInitialized = false;

        public List<NPC> ActiveNPCs => spawnedNPCs;

        /// <summary>
        /// Initialize all classic Usurper NPCs and spawn them into the world
        /// </summary>
        public async Task InitializeClassicNPCs()
        {
            // Check both the flag AND the actual count to be safe
            // This handles cases where the flag is set but NPCs weren't actually created
            if (npcsInitialized && spawnedNPCs.Count > 0)
            {
                GD.Print($"[NPCSpawn] NPCs already initialized ({spawnedNPCs.Count} active)");
                return;
            }

            GD.Print("[NPCSpawn] Initializing 50 classic Usurper NPCs...");

            var npcTemplates = ClassicNPCs.GetClassicNPCs();
            spawnedNPCs.Clear();

            foreach (var template in npcTemplates)
            {
                var npc = CreateNPCFromTemplate(template);
                spawnedNPCs.Add(npc);
            }

            // Distribute NPCs across locations
            DistributeNPCsAcrossWorld();

            npcsInitialized = true;
            GD.Print($"[NPCSpawn] Successfully initialized {spawnedNPCs.Count} NPCs");

            await Task.CompletedTask;
        }

        /// <summary>
        /// Force re-initialization of NPCs (for loading saves or debugging)
        /// </summary>
        public async Task ForceReinitializeNPCs()
        {
            GD.Print("[NPCSpawn] Force reinitializing NPCs...");
            npcsInitialized = false;
            spawnedNPCs.Clear();
            await InitializeClassicNPCs();
        }

        /// <summary>
        /// Create an NPC from a template
        /// </summary>
        private NPC CreateNPCFromTemplate(NPCTemplate template)
        {
            // Map GenderIdentity to CharacterSex
            CharacterSex sex = template.Gender switch
            {
                GenderIdentity.Female => CharacterSex.Female,
                GenderIdentity.TransFemale => CharacterSex.Female,
                GenderIdentity.Male => CharacterSex.Male,
                GenderIdentity.TransMale => CharacterSex.Male,
                GenderIdentity.NonBinary => random.Next(2) == 0 ? CharacterSex.Male : CharacterSex.Female,
                GenderIdentity.Genderfluid => random.Next(2) == 0 ? CharacterSex.Male : CharacterSex.Female,
                _ => random.Next(2) == 0 ? CharacterSex.Male : CharacterSex.Female
            };

            var npc = new NPC
            {
                Name1 = template.Name,
                Name2 = template.Name,
                ID = $"npc_{template.Name.ToLower().Replace(" ", "_")}",  // Generate unique ID from name
                Class = template.Class,
                Race = template.Race,
                Level = template.StartLevel,
                Age = random.Next(18, 50),
                Sex = sex,
                AI = CharacterAI.Computer,
                // Copy story role info - story NPCs have special narrative roles
                StoryRole = template.StoryRole ?? "",
                LoreNote = template.LoreNote ?? ""
            };

            // Generate stats based on level and class
            GenerateNPCStats(npc, template);

            // Set personality and alignment (including romance traits from template)
            SetNPCPersonality(npc, template);

            // Give starting equipment
            GiveStartingEquipment(npc);

            // Set random starting location
            npc.CurrentLocation = GetRandomStartLocation();

            return npc;
        }

        /// <summary>
        /// Generate stats for NPC based on level and class
        /// </summary>
        private void GenerateNPCStats(NPC npc, NPCTemplate template)
        {
            var level = template.StartLevel;

            // Base stats increase with level
            npc.Strength = 10 + (level * 5) + random.Next(-5, 6);
            npc.Defence = 10 + (level * 3) + random.Next(-3, 4);
            npc.Stamina = 10 + (level * 4) + random.Next(-4, 5);
            npc.Agility = 10 + (level * 3) + random.Next(-3, 4);
            npc.Charisma = 10 + random.Next(-5, 6);
            npc.Dexterity = 10 + (level * 2) + random.Next(-3, 4);
            npc.Wisdom = 10 + (level * 2) + random.Next(-3, 4);
            npc.Intelligence = 10 + (level * 2) + random.Next(-3, 4);

            // Class-specific stat bonuses
            switch (npc.Class)
            {
                case CharacterClass.Warrior:
                case CharacterClass.Barbarian:
                    npc.Strength += level * 2;
                    npc.HP = 100 + (level * 50);
                    break;
                case CharacterClass.Magician:
                    npc.Intelligence += level * 3;
                    npc.Mana = 50 + (level * 30);
                    npc.HP = 50 + (level * 25);
                    break;
                case CharacterClass.Cleric:
                case CharacterClass.Paladin:
                    npc.Wisdom += level * 2;
                    npc.HP = 80 + (level * 40);
                    npc.Mana = 40 + (level * 20);
                    break;
                case CharacterClass.Assassin:
                    npc.Dexterity += level * 3;
                    npc.Agility += level * 2;
                    npc.HP = 70 + (level * 35);
                    break;
                case CharacterClass.Sage:
                    npc.Agility += level * 2;
                    npc.Wisdom += level * 2;
                    npc.HP = 90 + (level * 45);
                    break;
                default:
                    npc.HP = 80 + (level * 40);
                    break;
            }

            npc.MaxHP = npc.HP;
            npc.MaxMana = npc.Mana;
            // Use same XP curve as players: level^1.8 * 50 per level
            npc.Experience = GetExperienceForLevel(level);
            npc.Gold = random.Next(level * 100, level * 500);

            // Add Constitution stat (was missing)
            npc.Constitution = 10 + (level * 2) + random.Next(-3, 4);

            // CRITICAL: Initialize base stats from current values
            // This ensures RecalculateStats() works correctly for NPCs
            npc.BaseStrength = npc.Strength;
            npc.BaseDexterity = npc.Dexterity;
            npc.BaseConstitution = npc.Constitution;
            npc.BaseIntelligence = npc.Intelligence;
            npc.BaseWisdom = npc.Wisdom;
            npc.BaseCharisma = npc.Charisma;
            npc.BaseMaxHP = npc.MaxHP;
            npc.BaseMaxMana = npc.MaxMana;
            npc.BaseDefence = npc.Defence;
            npc.BaseStamina = npc.Stamina;
            npc.BaseAgility = npc.Agility;
        }

        /// <summary>
        /// Set NPC personality based on template (including romance traits)
        /// </summary>
        private void SetNPCPersonality(NPC npc, NPCTemplate template)
        {
            string personality = template.Personality;
            string alignment = template.Alignment;

            // Create personality profile with randomized base values
            // These ensure NPCs have varying personalities for team formation
            var profile = new PersonalityProfile
            {
                Archetype = "commoner", // Default archetype, prevents null reference
                // Initialize core traits with random base values (0.3-0.7 range for variety)
                Aggression = 0.3f + (float)(random.NextDouble() * 0.4),
                Greed = 0.3f + (float)(random.NextDouble() * 0.4),
                Courage = 0.4f + (float)(random.NextDouble() * 0.4),
                Loyalty = 0.4f + (float)(random.NextDouble() * 0.4),
                Vengefulness = 0.2f + (float)(random.NextDouble() * 0.4),
                Impulsiveness = 0.3f + (float)(random.NextDouble() * 0.4),
                Sociability = 0.4f + (float)(random.NextDouble() * 0.4),
                Ambition = 0.3f + (float)(random.NextDouble() * 0.5),
                Trustworthiness = 0.4f + (float)(random.NextDouble() * 0.4),
                Caution = 0.3f + (float)(random.NextDouble() * 0.4),
                Intelligence = 0.3f + (float)(random.NextDouble() * 0.4),
                Mysticism = 0.2f + (float)(random.NextDouble() * 0.3),
                Patience = 0.3f + (float)(random.NextDouble() * 0.4)
            };

            // Modify traits based on personality type - these override the base values
            switch (personality.ToLower())
            {
                case "aggressive":
                case "fierce":
                case "brutal":
                    profile.Aggression = 0.7f + (float)(random.NextDouble() * 0.3); // 0.7-1.0
                    profile.Courage = 0.6f + (float)(random.NextDouble() * 0.3);
                    profile.Sociability = 0.5f + (float)(random.NextDouble() * 0.3); // Warriors are social
                    break;
                case "honorable":
                case "noble":
                case "righteous":
                    profile.Trustworthiness = 0.8f + (float)(random.NextDouble() * 0.2);
                    profile.Loyalty = 0.7f + (float)(random.NextDouble() * 0.3);
                    profile.Courage = 0.6f + (float)(random.NextDouble() * 0.3);
                    break;
                case "cunning":
                case "scheming":
                case "sneaky":
                    profile.Intelligence = 0.7f + (float)(random.NextDouble() * 0.3);
                    profile.Greed = 0.6f + (float)(random.NextDouble() * 0.3);
                    profile.Ambition = 0.6f + (float)(random.NextDouble() * 0.3);
                    break;
                case "wise":
                case "eccentric":
                    profile.Intelligence = 0.8f + (float)(random.NextDouble() * 0.2);
                    profile.Impulsiveness = 0.1f + (float)(random.NextDouble() * 0.2);
                    profile.Patience = 0.7f + (float)(random.NextDouble() * 0.3);
                    break;
                case "greedy":
                    profile.Greed = 0.8f + (float)(random.NextDouble() * 0.2);
                    profile.Ambition = 0.6f + (float)(random.NextDouble() * 0.3);
                    break;
                case "kind":
                case "compassionate":
                case "gentle":
                    profile.Sociability = 0.7f + (float)(random.NextDouble() * 0.3);
                    profile.Aggression = 0.1f + (float)(random.NextDouble() * 0.2);
                    profile.Loyalty = 0.6f + (float)(random.NextDouble() * 0.3);
                    break;
                case "cowardly":
                    profile.Courage = 0.1f + (float)(random.NextDouble() * 0.2);
                    profile.Caution = 0.7f + (float)(random.NextDouble() * 0.3);
                    break;
                case "ambitious":
                case "driven":
                    profile.Ambition = 0.7f + (float)(random.NextDouble() * 0.3);
                    profile.Courage = 0.5f + (float)(random.NextDouble() * 0.3);
                    break;
                case "loyal":
                case "steadfast":
                    profile.Loyalty = 0.8f + (float)(random.NextDouble() * 0.2);
                    profile.Sociability = 0.5f + (float)(random.NextDouble() * 0.3);
                    break;
                default:
                    // Neutral personality - keep base random values
                    break;
            }

            // Apply romance traits from template
            profile.Gender = template.Gender;
            profile.Orientation = template.Orientation;
            profile.IntimateStyle = template.IntimateStyle;
            profile.RelationshipPref = template.RelationshipPref;

            // Apply optional romance personality modifiers from template
            if (template.Romanticism.HasValue)
                profile.Romanticism = template.Romanticism.Value;
            else
                profile.Romanticism = 0.5f + (float)(random.NextDouble() * 0.3 - 0.15); // 0.35-0.65

            if (template.Sensuality.HasValue)
                profile.Sensuality = template.Sensuality.Value;
            else
                profile.Sensuality = 0.5f + (float)(random.NextDouble() * 0.3 - 0.15);

            if (template.Passion.HasValue)
                profile.Passion = template.Passion.Value;
            else
                profile.Passion = 0.5f + (float)(random.NextDouble() * 0.3 - 0.15);

            if (template.Adventurousness.HasValue)
                profile.Adventurousness = template.Adventurousness.Value;
            else
                profile.Adventurousness = 0.4f + (float)(random.NextDouble() * 0.3);

            // Generate remaining romance traits randomly based on personality
            profile.Flirtatiousness = profile.Sociability * 0.5f + (float)(random.NextDouble() * 0.3);
            profile.Tenderness = personality.ToLower() switch
            {
                "gentle" or "kind" or "compassionate" => 0.8f + (float)(random.NextDouble() * 0.2),
                "brutal" or "cruel" or "merciless" => 0.1f + (float)(random.NextDouble() * 0.2),
                _ => 0.4f + (float)(random.NextDouble() * 0.3)
            };
            profile.Jealousy = 0.3f + (float)(random.NextDouble() * 0.4);
            profile.Commitment = profile.RelationshipPref == RelationshipPreference.Monogamous ? 0.8f : 0.4f;
            profile.Exhibitionism = (float)(random.NextDouble() * 0.3);
            profile.Voyeurism = (float)(random.NextDouble() * 0.3);

            // Create NPCBrain with the personality profile
            npc.Brain = new NPCBrain(npc, profile);

            // Adjust based on alignment
            switch (alignment.ToLower())
            {
                case "good":
                    npc.Chivalry = random.Next(500, 1000);
                    npc.Darkness = 0;
                    break;
                case "evil":
                    npc.Darkness = random.Next(500, 1000);
                    npc.Chivalry = 0;
                    break;
                case "neutral":
                    npc.Chivalry = random.Next(0, 300);
                    npc.Darkness = random.Next(0, 300);
                    break;
            }
        }

        /// <summary>
        /// Give starting equipment to NPC
        /// </summary>
        private void GiveStartingEquipment(NPC npc)
        {
            // Give appropriate weapon and armor based on level and class
            npc.WeapPow = npc.Level * 3 + random.Next(5, 15);
            npc.ArmPow = npc.Level * 2 + random.Next(3, 10);

            // Give some healing potions
            npc.Healing = random.Next(npc.Level, npc.Level * 3);

            // Initialize inventory if needed
            if (npc.Item == null)
                npc.Item = new List<int>();
            if (npc.ItemType == null)
                npc.ItemType = new List<global::ObjType>();
        }

        /// <summary>
        /// Get a random starting location for NPCs
        /// </summary>
        private string GetRandomStartLocation()
        {
            // Place NPCs in various town locations with readable names
            // These must match the GetNPCLocationString() mapping in BaseLocation.cs
            var locations = new[]
            {
                "Main Street",
                "Main Street",   // More NPCs on Main Street (high traffic)
                "Market",
                "Inn",
                "Inn",           // More NPCs at the Inn
                "Temple",
                "Gym",
                "Weapon Shop",
                "Armor Shop",
                "Magic Shop",
                "Tavern",
                "Tavern",        // More NPCs at Tavern
                "Bank",
                "Healer",
                "Dark Alley"     // Some shady characters in the alley
            };

            return locations[random.Next(locations.Length)];
        }

        /// <summary>
        /// Distribute NPCs across the world
        /// </summary>
        private void DistributeNPCsAcrossWorld()
        {
            // Spread NPCs across different locations
            var locationCounts = new Dictionary<string, int>();

            foreach (var npc in spawnedNPCs)
            {
                if (!locationCounts.ContainsKey(npc.CurrentLocation))
                    locationCounts[npc.CurrentLocation] = 0;

                locationCounts[npc.CurrentLocation]++;

                // If too many NPCs in one location, move some
                if (locationCounts[npc.CurrentLocation] > 5)
                {
                    npc.CurrentLocation = GetRandomStartLocation();
                }
            }

            GD.Print($"[NPCSpawn] NPCs distributed across {locationCounts.Count} locations");
        }

        /// <summary>
        /// Get all NPCs at a specific location
        /// </summary>
        public List<NPC> GetNPCsAtLocation(string locationId)
        {
            return spawnedNPCs.Where(npc => npc.CurrentLocation == locationId).ToList();
        }

        /// <summary>
        /// Get an NPC by name
        /// </summary>
        public NPC? GetNPCByName(string name)
        {
            return spawnedNPCs.FirstOrDefault(npc =>
                npc.Name2.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Reset all NPCs (for new game)
        /// </summary>
        public void ResetNPCs()
        {
            spawnedNPCs.Clear();
            npcsInitialized = false;
            GD.Print("[NPCSpawn] NPCs reset");
        }

        /// <summary>
        /// Get all NPCs currently in prison
        /// </summary>
        public List<NPC> GetPrisoners()
        {
            return spawnedNPCs.Where(npc => npc.DaysInPrison > 0).ToList();
        }

        /// <summary>
        /// Find a prisoner by partial name match
        /// </summary>
        public NPC? FindPrisoner(string searchName)
        {
            if (string.IsNullOrWhiteSpace(searchName)) return null;

            return spawnedNPCs.FirstOrDefault(npc =>
                npc.DaysInPrison > 0 &&
                npc.Name2.Contains(searchName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Imprison an NPC for a number of days
        /// </summary>
        public void ImprisonNPC(NPC npc, int days)
        {
            if (npc == null) return;
            npc.DaysInPrison = (byte)Math.Min(255, days);
            npc.CellDoorOpen = false;
            npc.RescuedBy = "";
            npc.CurrentLocation = "Prison";
            GD.Print($"[NPCSpawn] {npc.Name2} imprisoned for {days} days");
        }

        /// <summary>
        /// Release an NPC from prison
        /// </summary>
        public void ReleaseNPC(NPC npc, string rescuerName = "")
        {
            if (npc == null) return;
            npc.DaysInPrison = 0;
            npc.CellDoorOpen = false;
            npc.RescuedBy = rescuerName;
            npc.HP = npc.MaxHP;
            npc.CurrentLocation = "Main Street";
            GD.Print($"[NPCSpawn] {npc.Name2} released from prison" +
                (string.IsNullOrEmpty(rescuerName) ? "" : $" by {rescuerName}"));
        }

        /// <summary>
        /// Mark an NPC's cell door as open (rescued)
        /// </summary>
        public void OpenCellDoor(NPC npc, string rescuerName)
        {
            if (npc == null) return;
            npc.CellDoorOpen = true;
            npc.RescuedBy = rescuerName;
            GD.Print($"[NPCSpawn] Cell door opened for {npc.Name2} by {rescuerName}");
        }

        /// <summary>
        /// Clear all NPCs (for loading saves)
        /// </summary>
        public void ClearAllNPCs()
        {
            spawnedNPCs.Clear();
            npcsInitialized = false;
            GD.Print("[NPCSpawn] All NPCs cleared for save restoration");
        }

        /// <summary>
        /// Add a restored NPC from save data
        /// </summary>
        public void AddRestoredNPC(NPC npc)
        {
            if (npc != null)
            {
                spawnedNPCs.Add(npc);
            }
        }

        /// <summary>
        /// Mark NPCs as initialized after restoration
        /// </summary>
        public void MarkAsInitialized()
        {
            npcsInitialized = true;
            GD.Print($"[NPCSpawn] Marked as initialized with {spawnedNPCs.Count} NPCs");
        }

        /// <summary>
        /// Calculate experience points needed for a given level using same curve as players
        /// Formula: Sum of level^1.8 * 50 for each level from 2 to target
        /// </summary>
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
    }
}
