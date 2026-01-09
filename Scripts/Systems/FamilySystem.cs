using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using UsurperRemake.Utils;

namespace UsurperRemake.Systems
{
    /// <summary>
    /// Family System - Manages children, family relationships, and child aging
    /// Children age over time and eventually grow into adult NPCs
    /// </summary>
    public class FamilySystem
    {
        private static FamilySystem? _instance;
        public static FamilySystem Instance => _instance ??= new FamilySystem();

        // All children in the game world
        private List<Child> _children = new();

        // Age at which children become adults (and turn into NPCs)
        public const int ADULT_AGE = 18;

        // Days per year of aging (children age faster than real time)
        public const int DAYS_PER_YEAR = 7; // 1 week real time = 1 year in-game

        public FamilySystem()
        {
            _instance = this;
        }

        /// <summary>
        /// Get all children
        /// </summary>
        public List<Child> AllChildren => _children;

        /// <summary>
        /// Add a new child to the registry
        /// </summary>
        public void RegisterChild(Child child)
        {
            if (child == null) return;
            if (_children.Any(c => c.Name == child.Name && c.MotherID == child.MotherID && c.FatherID == child.FatherID))
                return; // Prevent duplicates

            _children.Add(child);
            GD.Print($"[Family] Registered child: {child.Name}");
        }

        /// <summary>
        /// Get children belonging to a specific character (as parent)
        /// </summary>
        public List<Child> GetChildrenOf(Character parent)
        {
            if (parent == null) return new List<Child>();

            return _children.Where(c =>
                !c.Deleted &&
                (c.MotherID == parent.ID || c.FatherID == parent.ID ||
                 c.Mother == parent.Name || c.Father == parent.Name)
            ).ToList();
        }

        /// <summary>
        /// Get children of a married couple
        /// </summary>
        public List<Child> GetChildrenOfCouple(Character parent1, Character parent2)
        {
            if (parent1 == null || parent2 == null) return new List<Child>();

            return _children.Where(c =>
                !c.Deleted &&
                ((c.MotherID == parent1.ID || c.Mother == parent1.Name) &&
                 (c.FatherID == parent2.ID || c.Father == parent2.Name)) ||
                ((c.MotherID == parent2.ID || c.Mother == parent2.Name) &&
                 (c.FatherID == parent1.ID || c.Father == parent1.Name))
            ).ToList();
        }

        /// <summary>
        /// Process daily aging for all children
        /// Called from MaintenanceSystem or WorldSimulator
        /// </summary>
        public void ProcessDailyAging()
        {
            var childrenToAge = _children.Where(c => !c.Deleted && c.Age < ADULT_AGE).ToList();

            foreach (var child in childrenToAge)
            {
                int previousAge = child.Age;

                // Calculate age based on days since birth
                var daysSinceBirth = (DateTime.Now - child.BirthDate).Days;
                child.Age = daysSinceBirth / DAYS_PER_YEAR;

                // Check if child just came of age
                if (previousAge < ADULT_AGE && child.Age >= ADULT_AGE)
                {
                    ConvertChildToNPC(child);
                }
            }
        }

        /// <summary>
        /// Convert a child who has come of age into an NPC
        /// </summary>
        private void ConvertChildToNPC(Child child)
        {
            GD.Print($"[Family] {child.Name} has come of age and is now an adult!");

            // Create NPC from child
            var npc = new NPC
            {
                Name2 = child.Name,
                Sex = child.Sex,
                Age = ADULT_AGE,
                Race = DetermineChildRace(child),
                Class = DetermineChildClass(child),
                Level = 1,
                HP = 100,
                MaxHP = 100,
                Strength = 10 + new Random().Next(5),
                Defence = 10 + new Random().Next(5),
                Stamina = 10 + new Random().Next(5),
                Agility = 10 + new Random().Next(5),
                Intelligence = 10 + new Random().Next(5),
                Charisma = 10 + new Random().Next(5),
                Gold = 100,
                Experience = 0,
                CurrentLocation = "Main Street",
                AI = CharacterAI.Computer
            };

            // Set HP directly since IsAlive is computed from HP
            npc.HP = npc.MaxHP;

            // Inherit some traits based on soul
            if (child.Soul > 200)
            {
                npc.Chivalry = 50 + new Random().Next(50);
                npc.Darkness = 0;
            }
            else if (child.Soul < -200)
            {
                npc.Chivalry = 0;
                npc.Darkness = 50 + new Random().Next(50);
            }
            else
            {
                npc.Chivalry = 25;
                npc.Darkness = 25;
            }

            // Royal blood gives bonuses
            if (child.Royal > 0)
            {
                npc.Charisma += 10 * child.Royal;
                npc.Gold += 1000 * child.Royal;
            }

            // Create personality based on child's soul
            var personality = new PersonalityProfile();
            if (child.Soul > 100)
            {
                personality.Aggression = 0.2f;
                personality.Tenderness = 0.8f;  // Good kids are tender
                personality.Greed = 0.2f;
            }
            else if (child.Soul < -100)
            {
                personality.Aggression = 0.7f;
                personality.Tenderness = 0.2f;  // Bad kids are less tender
                personality.Greed = 0.7f;
            }

            npc.Brain = new NPCBrain(npc, personality);

            // Register with NPC system
            NPCSpawnSystem.Instance?.AddRestoredNPC(npc);

            // Mark child as "graduated" to adult
            child.Deleted = true;

            // Generate news
            NewsSystem.Instance?.Newsy(true,
                $"{child.Name}, child of {child.Mother} and {child.Father}, has come of age and joined the realm!");

            GD.Print($"[Family] Created adult NPC: {npc.Name2} ({npc.Class})");
        }

        /// <summary>
        /// Determine race based on parents
        /// </summary>
        private CharacterRace DetermineChildRace(Child child)
        {
            // Try to find parents and inherit race
            var mother = FindParentByID(child.MotherID) ?? FindParentByName(child.Mother);
            var father = FindParentByID(child.FatherID) ?? FindParentByName(child.Father);

            if (mother != null && father != null)
            {
                // 50/50 chance of inheriting either parent's race
                return new Random().Next(2) == 0 ? mother.Race : father.Race;
            }
            else if (mother != null)
            {
                return mother.Race;
            }
            else if (father != null)
            {
                return father.Race;
            }

            return CharacterRace.Human; // Default
        }

        /// <summary>
        /// Determine class based on parents and soul
        /// </summary>
        private CharacterClass DetermineChildClass(Child child)
        {
            var random = new Random();

            // Soul influences class selection
            if (child.Soul > 200)
            {
                // Good children become paladins, clerics
                return random.Next(3) switch
                {
                    0 => CharacterClass.Paladin,
                    1 => CharacterClass.Cleric,
                    _ => CharacterClass.Warrior
                };
            }
            else if (child.Soul < -200)
            {
                // Evil children become assassins, etc.
                return random.Next(3) switch
                {
                    0 => CharacterClass.Assassin,
                    1 => CharacterClass.Magician,
                    _ => CharacterClass.Barbarian
                };
            }
            else
            {
                // Neutral children - random class
                var classes = new[] {
                    CharacterClass.Warrior, CharacterClass.Magician, CharacterClass.Assassin,
                    CharacterClass.Ranger, CharacterClass.Bard, CharacterClass.Sage
                };
                return classes[random.Next(classes.Length)];
            }
        }

        /// <summary>
        /// Find parent character by ID
        /// </summary>
        private Character? FindParentByID(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            // Check NPCs
            var npc = NPCSpawnSystem.Instance?.ActiveNPCs?.FirstOrDefault(n => n.ID == id);
            if (npc != null) return npc;

            // Check current player
            var player = GameEngine.Instance?.CurrentPlayer;
            if (player?.ID == id) return player;

            return null;
        }

        /// <summary>
        /// Find parent character by name
        /// </summary>
        private Character? FindParentByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            // Check NPCs
            var npc = NPCSpawnSystem.Instance?.GetNPCByName(name);
            if (npc != null) return npc;

            // Check current player
            var player = GameEngine.Instance?.CurrentPlayer;
            if (player?.Name == name || player?.Name2 == name) return player;

            return null;
        }

        /// <summary>
        /// Handle custody transfer after divorce
        /// </summary>
        public void HandleDivorceCustody(Character parent1, Character parent2, Character custodialParent)
        {
            var children = GetChildrenOfCouple(parent1, parent2);
            var losingParent = custodialParent.ID == parent1.ID ? parent2 : parent1;

            foreach (var child in children)
            {
                child.HandleDivorceCustody(losingParent, custodialParent);
            }

            if (children.Count > 0)
            {
                NewsSystem.Instance?.Newsy(true,
                    $"{custodialParent.Name} has been granted custody of {children.Count} child(ren) in the divorce.");
            }
        }

        /// <summary>
        /// Serialize children for save system
        /// </summary>
        public List<ChildData> SerializeChildren()
        {
            return _children.Where(c => !c.Deleted).Select(c => new ChildData
            {
                Name = c.Name,
                Mother = c.Mother,
                Father = c.Father,
                MotherID = c.MotherID,
                FatherID = c.FatherID,
                OriginalMother = c.OriginalMother,
                OriginalFather = c.OriginalFather,
                Sex = (int)c.Sex,
                Age = c.Age,
                BirthDate = c.BirthDate,
                Named = c.Named,
                Location = c.Location,
                Health = c.Health,
                Soul = c.Soul,
                MotherAccess = c.MotherAccess,
                FatherAccess = c.FatherAccess,
                Kidnapped = c.Kidnapped,
                KidnapperName = c.KidnapperName,
                RansomDemanded = c.RansomDemanded,
                CursedByGod = c.CursedByGod,
                Royal = c.Royal
            }).ToList();
        }

        /// <summary>
        /// Deserialize children from save system
        /// </summary>
        public void DeserializeChildren(List<ChildData> childDataList)
        {
            _children.Clear();

            if (childDataList == null) return;

            foreach (var data in childDataList)
            {
                var child = new Child
                {
                    Name = data.Name,
                    Mother = data.Mother,
                    Father = data.Father,
                    MotherID = data.MotherID,
                    FatherID = data.FatherID,
                    OriginalMother = data.OriginalMother,
                    OriginalFather = data.OriginalFather,
                    Sex = (CharacterSex)data.Sex,
                    Age = data.Age,
                    BirthDate = data.BirthDate,
                    Named = data.Named,
                    Location = data.Location,
                    Health = data.Health,
                    Soul = data.Soul,
                    MotherAccess = data.MotherAccess,
                    FatherAccess = data.FatherAccess,
                    Kidnapped = data.Kidnapped,
                    KidnapperName = data.KidnapperName,
                    RansomDemanded = data.RansomDemanded,
                    CursedByGod = data.CursedByGod,
                    Royal = data.Royal
                };

                _children.Add(child);
            }

            GD.Print($"[Family] Loaded {_children.Count} children from save");
        }

        /// <summary>
        /// Reset family system (for new game)
        /// </summary>
        public void Reset()
        {
            _children.Clear();
        }

        /// <summary>
        /// Get family summary for a character
        /// </summary>
        public string GetFamilySummary(Character character)
        {
            var children = GetChildrenOf(character);
            if (children.Count == 0)
            {
                return $"{character.Name} has no children.";
            }

            var summary = $"{character.Name} has {children.Count} child(ren):\n";
            foreach (var child in children)
            {
                summary += $"  - {child.Name}, age {child.Age}, {child.GetSoulDescription()}\n";
            }
            return summary;
        }
    }
}
