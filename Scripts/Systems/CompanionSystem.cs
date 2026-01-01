using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using UsurperRemake.Utils;

namespace UsurperRemake.Systems
{
    /// <summary>
    /// Companion System - Manages NPC companions who can join the player's party
    /// Handles recruitment, loyalty, romance, personal quests, and permanent death
    ///
    /// Companions:
    /// - Lyris: Tragic romance interest, connected to Old Gods
    /// - Aldric: Loyal shield, may sacrifice himself if player becomes too dark
    /// - Mira: Broken healer seeking meaning, sacrifices herself to save player
    /// - Vex: Trickster thief with hidden depth, dying of wasting disease
    /// </summary>
    public class CompanionSystem
    {
        private static CompanionSystem? _instance;
        public static CompanionSystem Instance => _instance ??= new CompanionSystem();

        // All available companions
        private Dictionary<CompanionId, Companion> companions = new();

        // Currently active companions (max 2 in dungeon)
        private List<CompanionId> activeCompanions = new();

        // Fallen companions (permanent death)
        private Dictionary<CompanionId, CompanionDeath> fallenCompanions = new();

        public const int MaxActiveCompanions = 2;

        public event Action<CompanionId>? OnCompanionRecruited;
        public event Action<CompanionId, DeathType>? OnCompanionDeath;
        public event Action<CompanionId>? OnCompanionRomanceAdvanced;
        public event Action<CompanionId>? OnCompanionQuestCompleted;

        public CompanionSystem()
        {
            _instance = this;
            InitializeCompanions();
        }

        private void InitializeCompanions()
        {
            // ═══════════════════════════════════════════════════════════════
            // LYRIS - The Tragic Love Interest
            // ═══════════════════════════════════════════════════════════════
            companions[CompanionId.Lyris] = new Companion
            {
                Id = CompanionId.Lyris,
                Name = "Lyris",
                Title = "The Wandering Star",
                Type = CompanionType.Romance,

                Description = "A mysterious traveler with silver-streaked hair and eyes that seem to hold " +
                             "ancient sorrow. She speaks in riddles and appears to know more than she reveals.",

                BackstoryBrief = "Lyris was once a priestess of Aurelion, the god of light. When Manwe corrupted " +
                                "the Old Gods, she was cast out, neither fully mortal nor divine. She wanders " +
                                "endlessly, seeking a way to heal the gods she once served.",

                RecruitLevel = 15,
                RecruitLocation = "Dungeon Level 15 - Forgotten Shrine",

                BaseStats = new CompanionStats
                {
                    HP = 200,
                    Attack = 25,
                    Defense = 15,
                    MagicPower = 50,
                    Speed = 35,
                    HealingPower = 30
                },

                CombatRole = CombatRole.Hybrid,
                Abilities = new[] { "Starlight Heal", "Divine Smite", "Aurora Shield", "Sacrifice" },

                PersonalQuestName = "The Light That Was",
                PersonalQuestDescription = "Help Lyris recover an artifact that could restore Aurelion's true nature.",

                RomanceAvailable = true,
                CanDiePermanently = true,

                DeathTriggers = new Dictionary<DeathType, string>
                {
                    [DeathType.Sacrifice] = "Lyris may sacrifice herself to save you from a killing blow",
                    [DeathType.ChoiceBased] = "You may be forced to choose between Lyris and saving Veloura",
                    [DeathType.Combat] = "Lyris can die in combat if not protected"
                },

                DialogueHints = new[]
                {
                    "There is something... familiar about you.",
                    "The stars have shown me many things. Your face was among them.",
                    "Do you ever feel like you've forgotten something important?"
                },

                OceanPhilosophyAwareness = 4 // She senses the player's true nature early
            };

            // ═══════════════════════════════════════════════════════════════
            // ALDRIC - The Loyal Shield
            // ═══════════════════════════════════════════════════════════════
            companions[CompanionId.Aldric] = new Companion
            {
                Id = CompanionId.Aldric,
                Name = "Aldric",
                Title = "The Unbroken Shield",
                Type = CompanionType.Combat,

                Description = "A grizzled warrior with scars mapping decades of battle. His eyes hold " +
                             "the weight of those he couldn't save, and the determination to never fail again.",

                BackstoryBrief = "Once the captain of the King's Guard, Aldric lost his entire unit to a demonic " +
                                "incursion he blames himself for. He now wanders, seeking redemption through " +
                                "protecting those who need it most.",

                RecruitLevel = 10,
                RecruitLocation = "Tavern - After defending you from bandits",

                BaseStats = new CompanionStats
                {
                    HP = 350,
                    Attack = 45,
                    Defense = 40,
                    MagicPower = 5,
                    Speed = 20,
                    HealingPower = 0
                },

                CombatRole = CombatRole.Tank,
                Abilities = new[] { "Shield Wall", "Taunt", "Last Stand", "Sacrifice" },

                PersonalQuestName = "Ghosts of the Guard",
                PersonalQuestDescription = "Help Aldric find closure by confronting the demon that killed his unit.",

                RomanceAvailable = false,
                CanDiePermanently = true,

                DeathTriggers = new Dictionary<DeathType, string>
                {
                    [DeathType.MoralTrigger] = "If your Darkness exceeds 5000 AND loyalty > 80, he will turn against you",
                    [DeathType.Sacrifice] = "Will throw himself in front of any attack targeting you",
                    [DeathType.Combat] = "Can die in combat, especially if protecting you"
                },

                DialogueHints = new[]
                {
                    "I've failed people before. I won't fail you.",
                    "The measure of a warrior isn't in victories, but in who they protect.",
                    "Sometimes I wonder... are we fighting for the right reasons?"
                },

                LoyaltyThreshold = 80, // High loyalty required for moral trigger death
                DarknessThreshold = 5000 // Darkness level that triggers confrontation
            };

            // ═══════════════════════════════════════════════════════════════
            // MIRA - The Broken Healer
            // ═══════════════════════════════════════════════════════════════
            companions[CompanionId.Mira] = new Companion
            {
                Id = CompanionId.Mira,
                Name = "Mira",
                Title = "The Faded Light",
                Type = CompanionType.Support,

                Description = "A former priestess whose faith shattered when her temple was destroyed. " +
                             "Her healing powers remain, but her spirit is hollow. She helps others because " +
                             "she no longer knows what else to do.",

                BackstoryBrief = "Mira devoted her life to healing in Veloura's temple. When Veloura was corrupted, " +
                                "the temple turned on itself - healers became reapers. Mira escaped, but left her " +
                                "faith behind. She seeks to understand if healing still has meaning.",

                RecruitLevel = 20,
                RecruitLocation = "Temple Ruins - Found praying to an empty altar",

                BaseStats = new CompanionStats
                {
                    HP = 180,
                    Attack = 10,
                    Defense = 12,
                    MagicPower = 35,
                    Speed = 25,
                    HealingPower = 60
                },

                CombatRole = CombatRole.Healer,
                Abilities = new[] { "Greater Heal", "Purify", "Sanctuary", "Final Sacrifice" },

                PersonalQuestName = "The Meaning of Mercy",
                PersonalQuestDescription = "Help Mira decide if healing is worth continuing, culminating in a choice.",

                RomanceAvailable = false,
                CanDiePermanently = true,

                DeathTriggers = new Dictionary<DeathType, string>
                {
                    [DeathType.Sacrifice] = "Will sacrifice herself to cure you of a fatal curse",
                    [DeathType.QuestRelated] = "May die completing her personal quest, finding meaning in the act",
                    [DeathType.Inevitable] = "Her story arc leads toward sacrifice - it cannot be prevented"
                },

                DialogueHints = new[]
                {
                    "I heal because I don't know what else to do. Is that enough?",
                    "Sometimes the kindest thing is to let go.",
                    "If I save you... will that mean something? Will it matter?"
                },

                TeachesLettingGo = true // Her arc is about acceptance
            };

            // ═══════════════════════════════════════════════════════════════
            // VEX - The Trickster
            // ═══════════════════════════════════════════════════════════════
            companions[CompanionId.Vex] = new Companion
            {
                Id = CompanionId.Vex,
                Name = "Vex",
                Title = "The Laughing Shadow",
                Type = CompanionType.Utility,

                Description = "A charming thief with a quick wit and quicker fingers. Vex treats everything " +
                             "as a joke - including death. But there's a sadness behind the smile that " +
                             "he never speaks of.",

                BackstoryBrief = "Vex was born with a wasting disease - he's been dying his whole life, one day " +
                                "at a time. Rather than despair, he chose to laugh. He steals to survive, jokes " +
                                "to cope, and refuses to take anything seriously - including his own mortality.",

                RecruitLevel = 25,
                RecruitLocation = "Prison - Helps you escape, decides to tag along",

                BaseStats = new CompanionStats
                {
                    HP = 150,
                    Attack = 35,
                    Defense = 15,
                    MagicPower = 10,
                    Speed = 50,
                    HealingPower = 0
                },

                CombatRole = CombatRole.Damage,
                Abilities = new[] { "Backstab", "Smoke Bomb", "Disarm Trap", "Last Laugh" },

                PersonalQuestName = "One More Sunrise",
                PersonalQuestDescription = "Help Vex accomplish everything on his 'before I die' list.",

                RomanceAvailable = false,
                CanDiePermanently = true,

                DeathTriggers = new Dictionary<DeathType, string>
                {
                    [DeathType.Inevitable] = "The disease WILL claim him - this cannot be prevented",
                    [DeathType.Sacrifice] = "May choose to go out fighting instead of fading away",
                    [DeathType.TimeBased] = "After ~30 in-game days with him, symptoms worsen"
                },

                DialogueHints = new[]
                {
                    "Life's too short to take seriously. Trust me, I know.",
                    "Why am I doing this? Because tomorrow isn't guaranteed.",
                    "Everyone dies. I just have a more specific schedule."
                },

                HasTimedDeath = true,
                DaysUntilDeath = 30, // Approximately 30 in-game days
                TeachesAcceptance = true
            };

            GD.Print($"[CompanionSystem] Initialized {companions.Count} companions");
        }

        #region Companion Management

        /// <summary>
        /// Get a companion by ID
        /// </summary>
        public Companion? GetCompanion(CompanionId id)
        {
            return companions.TryGetValue(id, out var companion) ? companion : null;
        }

        /// <summary>
        /// Get all companions
        /// </summary>
        public IEnumerable<Companion> GetAllCompanions()
        {
            return companions.Values;
        }

        /// <summary>
        /// Get companions that can be recruited at the player's current level
        /// </summary>
        public IEnumerable<Companion> GetRecruitableCompanions(int playerLevel)
        {
            return companions.Values
                .Where(c => !c.IsRecruited && !c.IsDead && playerLevel >= c.RecruitLevel);
        }

        /// <summary>
        /// Get currently active companions
        /// </summary>
        public IEnumerable<Companion> GetActiveCompanions()
        {
            return activeCompanions
                .Select(id => companions.TryGetValue(id, out var c) ? c : null)
                .Where(c => c != null)!;
        }

        /// <summary>
        /// Get fallen (dead) companions
        /// </summary>
        public IEnumerable<(Companion Companion, CompanionDeath Death)> GetFallenCompanions()
        {
            foreach (var kvp in fallenCompanions)
            {
                if (companions.TryGetValue(kvp.Key, out var companion))
                {
                    yield return (companion, kvp.Value);
                }
            }
        }

        /// <summary>
        /// Recruit a companion
        /// </summary>
        public async Task<bool> RecruitCompanion(CompanionId id, Character player, TerminalEmulator terminal)
        {
            if (!companions.TryGetValue(id, out var companion))
                return false;

            if (companion.IsRecruited || companion.IsDead)
                return false;

            if (player.Level < companion.RecruitLevel)
            {
                terminal.WriteLine($"{companion.Name} doesn't think you're ready yet.", "yellow");
                return false;
            }

            // Display recruitment scene
            await DisplayRecruitmentScene(companion, terminal);

            companion.IsRecruited = true;
            companion.RecruitedDay = GetGameDay();

            // Auto-add to active if room
            if (activeCompanions.Count < MaxActiveCompanions)
            {
                activeCompanions.Add(id);
                companion.IsActive = true;
                terminal.WriteLine($"{companion.Name} joins your party!", "bright_green");
            }
            else
            {
                terminal.WriteLine($"{companion.Name} will wait for you at the tavern.", "cyan");
            }

            OnCompanionRecruited?.Invoke(id);

            // Track for Ocean Philosophy
            OceanPhilosophySystem.Instance.ExperienceMoment(AwakeningMoment.SacrificedForAnother);

            return true;
        }

        /// <summary>
        /// Check if a companion has been recruited
        /// </summary>
        public bool IsCompanionRecruited(CompanionId id)
        {
            return companions.TryGetValue(id, out var c) && c.IsRecruited;
        }

        /// <summary>
        /// Check if a companion is alive
        /// </summary>
        public bool IsCompanionAlive(CompanionId id)
        {
            return companions.TryGetValue(id, out var c) && !c.IsDead;
        }

        /// <summary>
        /// Set active companions for dungeon
        /// </summary>
        public bool SetActiveCompanions(List<CompanionId> companionIds)
        {
            if (companionIds.Count > MaxActiveCompanions)
                return false;

            foreach (var id in companionIds)
            {
                if (!companions.TryGetValue(id, out var c) || !c.IsRecruited || c.IsDead)
                    return false;
            }

            // Deactivate current
            foreach (var id in activeCompanions)
            {
                if (companions.TryGetValue(id, out var c))
                    c.IsActive = false;
            }

            // Activate new
            activeCompanions.Clear();
            activeCompanions.AddRange(companionIds);

            foreach (var id in companionIds)
            {
                if (companions.TryGetValue(id, out var c))
                    c.IsActive = true;
            }

            return true;
        }

        #endregion

        #region Relationship Management

        /// <summary>
        /// Modify loyalty for a companion
        /// </summary>
        public void ModifyLoyalty(CompanionId id, int amount, string reason = "")
        {
            if (!companions.TryGetValue(id, out var companion))
                return;

            companion.LoyaltyLevel = Math.Clamp(companion.LoyaltyLevel + amount, 0, 100);

            if (!string.IsNullOrEmpty(reason))
            {
                companion.AddHistory(new CompanionEvent
                {
                    Type = CompanionEventType.LoyaltyChange,
                    Description = reason,
                    LoyaltyChange = amount,
                    Timestamp = DateTime.Now
                });
            }
        }

        /// <summary>
        /// Modify trust for a companion
        /// </summary>
        public void ModifyTrust(CompanionId id, int amount, string reason = "")
        {
            if (!companions.TryGetValue(id, out var companion))
                return;

            companion.TrustLevel = Math.Clamp(companion.TrustLevel + amount, 0, 100);
        }

        /// <summary>
        /// Advance romance with a companion (if available)
        /// </summary>
        public bool AdvanceRomance(CompanionId id, int amount = 1)
        {
            if (!companions.TryGetValue(id, out var companion))
                return false;

            if (!companion.RomanceAvailable)
                return false;

            companion.RomanceLevel = Math.Clamp(companion.RomanceLevel + amount, 0, 10);

            companion.AddHistory(new CompanionEvent
            {
                Type = CompanionEventType.RomanceAdvanced,
                Description = GetRomanceMilestone(companion.RomanceLevel),
                Timestamp = DateTime.Now
            });

            OnCompanionRomanceAdvanced?.Invoke(id);
            return true;
        }

        private string GetRomanceMilestone(int level)
        {
            return level switch
            {
                1 => "A moment of connection",
                2 => "Shared vulnerability",
                3 => "Growing closer",
                4 => "A stolen glance",
                5 => "The first confession",
                6 => "A promise made",
                7 => "Hearts intertwined",
                8 => "Inseparable",
                9 => "True love",
                10 => "Soulbound",
                _ => "Unknown milestone"
            };
        }

        #endregion

        #region Death System

        /// <summary>
        /// Kill a companion permanently
        /// </summary>
        public async Task KillCompanion(CompanionId id, DeathType type, string circumstance, TerminalEmulator terminal)
        {
            if (!companions.TryGetValue(id, out var companion))
                return;

            if (companion.IsDead)
                return;

            // Display death scene
            await DisplayDeathScene(companion, type, circumstance, terminal);

            companion.IsDead = true;
            companion.IsActive = false;
            companion.DeathType = type;

            activeCompanions.Remove(id);

            fallenCompanions[id] = new CompanionDeath
            {
                CompanionId = id,
                Type = type,
                Circumstance = circumstance,
                LastWords = GetLastWords(companion, type),
                DeathDay = GetGameDay(),
                PlayerLevel = GetPlayerLevel()
            };

            // Trigger grief system
            GriefSystem.Instance.BeginGrief(id, companion.Name, type);

            // Trigger Ocean Philosophy awakening
            if (!OceanPhilosophySystem.Instance.ExperiencedMoments.Contains(AwakeningMoment.FirstCompanionDeath))
            {
                OceanPhilosophySystem.Instance.ExperienceMoment(AwakeningMoment.FirstCompanionDeath);
            }

            OnCompanionDeath?.Invoke(id, type);
        }

        private string GetLastWords(Companion companion, DeathType type)
        {
            return (companion.Id, type) switch
            {
                (CompanionId.Lyris, DeathType.Sacrifice) =>
                    "I finally understand... what I was seeking... was always... with you...",

                (CompanionId.Lyris, DeathType.ChoiceBased) =>
                    "Tell Aurelion... I never stopped believing...",

                (CompanionId.Aldric, DeathType.MoralTrigger) =>
                    "I'm sorry... but I can't let you become... what you're becoming...",

                (CompanionId.Aldric, DeathType.Sacrifice) =>
                    "Finally... I protected someone... this time... I didn't fail...",

                (CompanionId.Mira, DeathType.Sacrifice) =>
                    "At last... I understand... healing was never about... the body...",

                (CompanionId.Mira, DeathType.QuestRelated) =>
                    "I found it... the meaning... it was always... in the giving...",

                (CompanionId.Vex, DeathType.Inevitable) =>
                    "Heh... beat the schedule... by... a few hours... worth it...",

                (CompanionId.Vex, DeathType.Sacrifice) =>
                    "Always wanted to go out... with a punchline... did I... make you laugh...?",

                _ => "It was... worth it... all of it..."
            };
        }

        /// <summary>
        /// Check if any companions should trigger their death conditions
        /// </summary>
        public DeathTriggerCheck CheckDeathTriggers(Character player)
        {
            var result = new DeathTriggerCheck();

            // Check Aldric's moral trigger
            var aldric = GetCompanion(CompanionId.Aldric);
            if (aldric != null && aldric.IsRecruited && !aldric.IsDead)
            {
                if (player.Darkness >= aldric.DarknessThreshold && aldric.LoyaltyLevel >= aldric.LoyaltyThreshold)
                {
                    result.TriggeredCompanion = CompanionId.Aldric;
                    result.TriggerType = DeathType.MoralTrigger;
                    result.TriggerReason = "Your darkness has grown too great. Aldric cannot stand by.";
                }
            }

            // Check Vex's timed death
            var vex = GetCompanion(CompanionId.Vex);
            if (vex != null && vex.IsRecruited && !vex.IsDead && vex.HasTimedDeath)
            {
                int daysWithVex = GetGameDay() - vex.RecruitedDay;
                if (daysWithVex >= vex.DaysUntilDeath)
                {
                    result.TriggeredCompanion = CompanionId.Vex;
                    result.TriggerType = DeathType.Inevitable;
                    result.TriggerReason = "The disease has finally claimed Vex.";
                }
            }

            return result;
        }

        #endregion

        #region Personal Quests

        /// <summary>
        /// Start a companion's personal quest
        /// </summary>
        public bool StartPersonalQuest(CompanionId id)
        {
            if (!companions.TryGetValue(id, out var companion))
                return false;

            if (companion.PersonalQuestStarted || companion.PersonalQuestCompleted)
                return false;

            if (companion.LoyaltyLevel < 50)
                return false; // Need sufficient loyalty

            companion.PersonalQuestStarted = true;
            companion.AddHistory(new CompanionEvent
            {
                Type = CompanionEventType.QuestStarted,
                Description = $"Began personal quest: {companion.PersonalQuestName}",
                Timestamp = DateTime.Now
            });

            return true;
        }

        /// <summary>
        /// Complete a companion's personal quest
        /// </summary>
        public void CompletePersonalQuest(CompanionId id, bool success)
        {
            if (!companions.TryGetValue(id, out var companion))
                return;

            companion.PersonalQuestCompleted = true;
            companion.PersonalQuestSuccess = success;

            companion.AddHistory(new CompanionEvent
            {
                Type = CompanionEventType.QuestCompleted,
                Description = success
                    ? $"Successfully completed: {companion.PersonalQuestName}"
                    : $"Quest failed: {companion.PersonalQuestName}",
                Timestamp = DateTime.Now
            });

            OnCompanionQuestCompleted?.Invoke(id);
        }

        /// <summary>
        /// Trigger a companion's death from a moral paradox choice
        /// This is a simplified version that doesn't require terminal for async display
        /// </summary>
        public void TriggerCompanionDeathByParadox(string companionName)
        {
            // Find companion by name
            var companion = companions.Values.FirstOrDefault(c =>
                c.Name.Equals(companionName, StringComparison.OrdinalIgnoreCase));

            if (companion == null || companion.IsDead)
                return;

            companion.IsDead = true;
            companion.IsActive = false;
            companion.DeathType = DeathType.ChoiceBased;

            activeCompanions.Remove(companion.Id);

            fallenCompanions[companion.Id] = new CompanionDeath
            {
                CompanionId = companion.Id,
                Type = DeathType.ChoiceBased,
                Circumstance = "Died as a consequence of a moral choice",
                LastWords = GetLastWords(companion, DeathType.ChoiceBased),
                DeathDay = GetGameDay(),
                PlayerLevel = GetPlayerLevel()
            };

            // Trigger grief system
            GriefSystem.Instance.BeginGrief(companion.Id, companion.Name, DeathType.ChoiceBased);

            // Trigger Ocean Philosophy awakening for sacrifice
            OceanPhilosophySystem.Instance.ExperienceMoment(AwakeningMoment.CompanionSacrifice);

            OnCompanionDeath?.Invoke(companion.Id, DeathType.ChoiceBased);

            GD.Print($"[Companion] {companionName} died from moral paradox choice");
        }

        #endregion

        #region Display Methods

        private async Task DisplayRecruitmentScene(Companion companion, TerminalEmulator terminal)
        {
            terminal.Clear();
            terminal.WriteLine("╔══════════════════════════════════════════════════════════════════╗", "bright_cyan");
            terminal.WriteLine("║                  N E W   C O M P A N I O N                       ║", "bright_cyan");
            terminal.WriteLine("╚══════════════════════════════════════════════════════════════════╝", "bright_cyan");
            terminal.WriteLine("");

            terminal.WriteLine($"  {companion.Name}", "bright_white");
            terminal.WriteLine($"  \"{companion.Title}\"", "cyan");
            terminal.WriteLine("");

            terminal.WriteLine(companion.Description, "white");
            terminal.WriteLine("");

            terminal.WriteLine($"  Role: {companion.CombatRole}", "yellow");
            terminal.WriteLine($"  Abilities: {string.Join(", ", companion.Abilities)}", "yellow");
            terminal.WriteLine("");

            // Show a hint of their deeper story
            if (companion.DialogueHints.Length > 0)
            {
                terminal.WriteLine($"  \"{companion.DialogueHints[0]}\"", "dark_cyan");
            }

            await terminal.GetInputAsync("\nPress Enter to welcome them...");
        }

        private async Task DisplayDeathScene(Companion companion, DeathType type, string circumstance, TerminalEmulator terminal)
        {
            terminal.Clear();
            terminal.WriteLine("╔══════════════════════════════════════════════════════════════════╗", "dark_red");
            terminal.WriteLine("║                                                                    ║", "dark_red");
            terminal.WriteLine("║                    F   A   L   L   E   N                          ║", "dark_red");
            terminal.WriteLine("║                                                                    ║", "dark_red");
            terminal.WriteLine("╚══════════════════════════════════════════════════════════════════╝", "dark_red");
            terminal.WriteLine("");

            await Task.Delay(1000);

            terminal.WriteLine($"  {companion.Name}", "white");
            terminal.WriteLine($"  \"{companion.Title}\"", "gray");
            terminal.WriteLine("");

            await Task.Delay(500);

            terminal.WriteLine($"  {circumstance}", "white");
            terminal.WriteLine("");

            await Task.Delay(1000);

            string lastWords = GetLastWords(companion, type);
            terminal.WriteLine($"  \"{lastWords}\"", "dark_cyan");
            terminal.WriteLine("");

            await Task.Delay(1500);

            terminal.WriteLine("  They are gone.", "gray");
            terminal.WriteLine("  There is no resurrection. No second chance.", "gray");
            terminal.WriteLine("  Only memory remains.", "gray");
            terminal.WriteLine("");

            // Ocean philosophy moment
            terminal.WriteLine("  The wave returns to the ocean...", "dark_magenta");
            terminal.WriteLine("");

            await terminal.GetInputAsync("Press Enter to continue...");
        }

        #endregion

        #region Serialization

        /// <summary>
        /// Serialize companion state for saving
        /// </summary>
        public CompanionSystemData Serialize()
        {
            return new CompanionSystemData
            {
                CompanionStates = companions.Values.Select(c => new CompanionSaveData
                {
                    Id = c.Id,
                    IsRecruited = c.IsRecruited,
                    IsActive = c.IsActive,
                    IsDead = c.IsDead,
                    LoyaltyLevel = c.LoyaltyLevel,
                    TrustLevel = c.TrustLevel,
                    RomanceLevel = c.RomanceLevel,
                    PersonalQuestStarted = c.PersonalQuestStarted,
                    PersonalQuestCompleted = c.PersonalQuestCompleted,
                    PersonalQuestSuccess = c.PersonalQuestSuccess,
                    RecruitedDay = c.RecruitedDay,
                    DeathType = c.DeathType,
                    History = c.History.ToList()
                }).ToList(),
                ActiveCompanions = activeCompanions.ToList(),
                FallenCompanions = fallenCompanions.Values.ToList()
            };
        }

        /// <summary>
        /// Restore companion state from save
        /// </summary>
        public void Deserialize(CompanionSystemData data)
        {
            if (data == null) return;

            foreach (var save in data.CompanionStates)
            {
                if (companions.TryGetValue(save.Id, out var companion))
                {
                    companion.IsRecruited = save.IsRecruited;
                    companion.IsActive = save.IsActive;
                    companion.IsDead = save.IsDead;
                    companion.LoyaltyLevel = save.LoyaltyLevel;
                    companion.TrustLevel = save.TrustLevel;
                    companion.RomanceLevel = save.RomanceLevel;
                    companion.PersonalQuestStarted = save.PersonalQuestStarted;
                    companion.PersonalQuestCompleted = save.PersonalQuestCompleted;
                    companion.PersonalQuestSuccess = save.PersonalQuestSuccess;
                    companion.RecruitedDay = save.RecruitedDay;
                    companion.DeathType = save.DeathType;
                    companion.History = save.History?.ToList() ?? new List<CompanionEvent>();
                }
            }

            activeCompanions = data.ActiveCompanions?.ToList() ?? new List<CompanionId>();

            fallenCompanions.Clear();
            if (data.FallenCompanions != null)
            {
                foreach (var death in data.FallenCompanions)
                {
                    fallenCompanions[death.CompanionId] = death;
                }
            }
        }

        #endregion

        #region Helpers

        private int GetGameDay()
        {
            // Would integrate with game's day counter
            return StoryProgressionSystem.Instance.CurrentGameDay;
        }

        private int GetPlayerLevel()
        {
            // Would get from game state
            return 1;
        }

        #endregion
    }

    #region Companion Data Classes

    public enum CompanionId
    {
        Lyris,
        Aldric,
        Mira,
        Vex
    }

    public enum CompanionType
    {
        Combat,
        Support,
        Romance,
        Utility
    }

    public enum CombatRole
    {
        Tank,
        Damage,
        Healer,
        Hybrid
    }

    public enum DeathType
    {
        Combat,         // Died in battle
        Sacrifice,      // Chose to die for player
        ChoiceBased,    // Player made a choice
        MoralTrigger,   // Triggered by player's alignment
        QuestRelated,   // Died completing quest
        Inevitable,     // Scripted/unavoidable
        TimeBased       // Time-based trigger (disease, curse)
    }

    public enum CompanionEventType
    {
        Recruited,
        LoyaltyChange,
        TrustChange,
        RomanceAdvanced,
        QuestStarted,
        QuestCompleted,
        NearDeath,
        Saved,
        Conflict,
        Conversation
    }

    public class Companion
    {
        public CompanionId Id { get; set; }
        public string Name { get; set; } = "";
        public string Title { get; set; } = "";
        public CompanionType Type { get; set; }
        public string Description { get; set; } = "";
        public string BackstoryBrief { get; set; } = "";

        public int RecruitLevel { get; set; }
        public string RecruitLocation { get; set; } = "";

        public CompanionStats BaseStats { get; set; } = new();
        public CombatRole CombatRole { get; set; }
        public string[] Abilities { get; set; } = Array.Empty<string>();

        public string PersonalQuestName { get; set; } = "";
        public string PersonalQuestDescription { get; set; } = "";
        public bool PersonalQuestStarted { get; set; }
        public bool PersonalQuestCompleted { get; set; }
        public bool PersonalQuestSuccess { get; set; }

        public bool RomanceAvailable { get; set; }
        public bool CanDiePermanently { get; set; }

        public Dictionary<DeathType, string> DeathTriggers { get; set; } = new();
        public string[] DialogueHints { get; set; } = Array.Empty<string>();

        public int OceanPhilosophyAwareness { get; set; } = 0;
        public int LoyaltyThreshold { get; set; } = 80;
        public int DarknessThreshold { get; set; } = 5000;
        public bool HasTimedDeath { get; set; }
        public int DaysUntilDeath { get; set; }
        public bool TeachesLettingGo { get; set; }
        public bool TeachesAcceptance { get; set; }

        // Runtime state
        public bool IsRecruited { get; set; }
        public bool IsActive { get; set; }
        public bool IsDead { get; set; }
        public DeathType? DeathType { get; set; }
        public int LoyaltyLevel { get; set; } = 50;
        public int TrustLevel { get; set; } = 50;
        public int RomanceLevel { get; set; } = 0;
        public int RecruitedDay { get; set; }

        public List<CompanionEvent> History { get; set; } = new();

        public void AddHistory(CompanionEvent evt)
        {
            History.Add(evt);
            if (History.Count > 100)
                History.RemoveAt(0);
        }
    }

    public class CompanionStats
    {
        public int HP { get; set; }
        public int Attack { get; set; }
        public int Defense { get; set; }
        public int MagicPower { get; set; }
        public int Speed { get; set; }
        public int HealingPower { get; set; }
    }

    public class CompanionEvent
    {
        public CompanionEventType Type { get; set; }
        public string Description { get; set; } = "";
        public int LoyaltyChange { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class CompanionDeath
    {
        public CompanionId CompanionId { get; set; }
        public DeathType Type { get; set; }
        public string Circumstance { get; set; } = "";
        public string LastWords { get; set; } = "";
        public int DeathDay { get; set; }
        public int PlayerLevel { get; set; }
    }

    public class DeathTriggerCheck
    {
        public CompanionId? TriggeredCompanion { get; set; }
        public DeathType? TriggerType { get; set; }
        public string TriggerReason { get; set; } = "";
    }

    public class CompanionSystemData
    {
        public List<CompanionSaveData> CompanionStates { get; set; } = new();
        public List<CompanionId> ActiveCompanions { get; set; } = new();
        public List<CompanionDeath> FallenCompanions { get; set; } = new();
    }

    public class CompanionSaveData
    {
        public CompanionId Id { get; set; }
        public bool IsRecruited { get; set; }
        public bool IsActive { get; set; }
        public bool IsDead { get; set; }
        public int LoyaltyLevel { get; set; }
        public int TrustLevel { get; set; }
        public int RomanceLevel { get; set; }
        public bool PersonalQuestStarted { get; set; }
        public bool PersonalQuestCompleted { get; set; }
        public bool PersonalQuestSuccess { get; set; }
        public int RecruitedDay { get; set; }
        public DeathType? DeathType { get; set; }
        public List<CompanionEvent> History { get; set; } = new();
    }

    #endregion
}
