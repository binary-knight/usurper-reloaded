using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using UsurperRemake.Utils;

namespace UsurperRemake.Systems
{
    /// <summary>
    /// Grief System - Handles the emotional aftermath of companion death
    ///
    /// When a companion dies permanently, the player experiences realistic grief stages:
    /// - Denial (3 days): -20% combat effectiveness, denial dialogue options
    /// - Anger (5 days): +30% damage, -20% defense
    /// - Bargaining (3 days): Can try resurrection (all fail), desperate dialogue
    /// - Depression (7 days): -30% all stats, sad dialogue, NPCs react
    /// - Acceptance (Permanent): Scars remain, +5 Wisdom, unlocks "Memory" feature
    ///
    /// DEATH IS PERMANENT - No resurrection tricks. This makes grief meaningful.
    /// </summary>
    public class GriefSystem
    {
        private static GriefSystem? _instance;
        public static GriefSystem Instance => _instance ??= new GriefSystem();

        // Active grief states
        private Dictionary<CompanionId, GriefState> activeGrief = new();

        // Memories of fallen companions
        private List<CompanionMemory> memories = new();

        public event Action<CompanionId, GriefStage>? OnGriefStageChanged;
        public event Action<CompanionId>? OnGriefComplete;

        /// <summary>
        /// Check if player is currently grieving any companion
        /// </summary>
        public bool IsGrieving => activeGrief.Values.Any(g => !g.IsComplete);

        /// <summary>
        /// Get the current grief stage (returns highest priority active grief)
        /// </summary>
        public GriefStage CurrentStage => activeGrief.Values
            .Where(g => !g.IsComplete)
            .Select(g => g.CurrentStage)
            .DefaultIfEmpty(GriefStage.None)
            .First();

        /// <summary>
        /// Check if player has completed at least one full grief cycle
        /// </summary>
        public bool HasCompletedGriefCycle => activeGrief.Values.Any(g => g.IsComplete);

        public GriefSystem()
        {
            _instance = this;
        }

        /// <summary>
        /// Begin grieving for a fallen companion
        /// </summary>
        public void BeginGrief(CompanionId companionId, string companionName, DeathType deathType)
        {
            var griefState = new GriefState
            {
                CompanionId = companionId,
                CompanionName = companionName,
                DeathType = deathType,
                CurrentStage = GriefStage.Denial,
                StageStartDay = GetCurrentDay(),
                GriefStartDay = GetCurrentDay(),
                ResurrectionAttempts = 0
            };

            activeGrief[companionId] = griefState;

            // Create initial memory
            memories.Add(new CompanionMemory
            {
                CompanionId = companionId,
                CompanionName = companionName,
                MemoryText = GetInitialMemory(companionName, deathType),
                CreatedDay = GetCurrentDay()
            });

            GD.Print($"[Grief] Began grieving for {companionName}. Stage: Denial");
        }

        /// <summary>
        /// Update grief states based on time passed
        /// </summary>
        public void UpdateGrief(int currentDay)
        {
            foreach (var kvp in activeGrief)
            {
                var grief = kvp.Value;
                if (grief.IsComplete)
                    continue;

                int daysInStage = currentDay - grief.StageStartDay;
                int stageDuration = GetStageDuration(grief.CurrentStage);

                if (daysInStage >= stageDuration)
                {
                    AdvanceGriefStage(grief, currentDay);
                }
            }
        }

        /// <summary>
        /// Get current grief effects on player stats
        /// </summary>
        public GriefEffects GetCurrentEffects()
        {
            var effects = new GriefEffects();

            foreach (var grief in activeGrief.Values)
            {
                if (grief.IsComplete)
                    continue;

                var stageEffects = GetStageEffects(grief.CurrentStage);
                effects.CombatModifier += stageEffects.CombatModifier;
                effects.DamageModifier += stageEffects.DamageModifier;
                effects.DefenseModifier += stageEffects.DefenseModifier;
                effects.AllStatModifier += stageEffects.AllStatModifier;
            }

            // Also add permanent wisdom bonus from completed grief
            int completedGriefs = 0;
            foreach (var grief in activeGrief.Values)
            {
                if (grief.IsComplete)
                    completedGriefs++;
            }
            effects.PermanentWisdomBonus = completedGriefs * 5;

            return effects;
        }

        /// <summary>
        /// Attempt resurrection (always fails - but advances bargaining)
        /// </summary>
        public async Task<bool> AttemptResurrection(CompanionId companionId, string method, TerminalEmulator terminal)
        {
            if (!activeGrief.TryGetValue(companionId, out var grief))
                return false;

            grief.ResurrectionAttempts++;

            // Display the attempt
            terminal.WriteLine("");
            terminal.WriteLine($"You attempt to resurrect {grief.CompanionName} using {method}...", "yellow");
            await Task.Delay(1500);

            // ALL resurrection attempts fail
            string failureReason = GetResurrectionFailure(method, grief.ResurrectionAttempts);
            terminal.WriteLine("");
            terminal.WriteLine(failureReason, "red");
            terminal.WriteLine("");

            // The more attempts, the more desperate the messages
            if (grief.ResurrectionAttempts >= 3)
            {
                terminal.WriteLine("A voice echoes in your mind:", "dark_magenta");
                terminal.WriteLine("\"The wave cannot uncrash upon the shore.\"", "magenta");
                terminal.WriteLine("\"Death is not an ending. It is returning home.\"", "magenta");
                terminal.WriteLine("");

                // This contributes to Ocean Philosophy understanding
                OceanPhilosophySystem.Instance.CollectFragment(WaveFragment.TheReturn);
            }

            await terminal.GetInputAsync("Press Enter to continue...");
            return false;
        }

        /// <summary>
        /// Get dialogue options based on grief stage
        /// </summary>
        public List<GriefDialogueOption> GetDialogueOptions(CompanionId companionId)
        {
            if (!activeGrief.TryGetValue(companionId, out var grief))
                return new List<GriefDialogueOption>();

            return grief.CurrentStage switch
            {
                GriefStage.Denial => new List<GriefDialogueOption>
                {
                    new("They're not really gone.", "They're just... resting. They'll be back."),
                    new("I don't want to talk about it.", "It didn't happen. It can't have happened."),
                    new("There must be a way.", "I'll find a way to bring them back.")
                },

                GriefStage.Anger => new List<GriefDialogueOption>
                {
                    new("This is YOUR fault!", "If you had helped, they would still be alive!"),
                    new("I'll destroy everything.", "Nothing matters anymore. Only vengeance."),
                    new("Why did this happen?!", "It's not FAIR! They didn't deserve this!")
                },

                GriefStage.Bargaining => new List<GriefDialogueOption>
                {
                    new("I'll do anything to bring them back.", "Take my soul. Take my life. Just bring them back."),
                    new("Maybe if I pray harder...", "The gods must know a way. There's always a way."),
                    new("What if I had done things differently?", "If only I had been faster... stronger...")
                },

                GriefStage.Depression => new List<GriefDialogueOption>
                {
                    new("[Say nothing]", "You stare into the distance, lost in memory."),
                    new("What's the point anymore?", "They're gone. Nothing I do will change that."),
                    new("I just want to be alone.", "Please... I need time.")
                },

                GriefStage.Acceptance => new List<GriefDialogueOption>
                {
                    new("I carry them with me.", "They're gone, but what they taught me remains."),
                    new("Tell me about them.", "I want to remember. To honor their memory."),
                    new("I'm ready to move forward.", "They would want me to continue. To live.")
                },

                _ => new List<GriefDialogueOption>()
            };
        }

        /// <summary>
        /// Get NPC reactions to player's grief
        /// </summary>
        public string GetNPCReaction(GriefStage stage, bool isWise)
        {
            if (isWise)
            {
                return stage switch
                {
                    GriefStage.Denial => "I see the shadow of loss upon you. But you are not yet ready to face it.",
                    GriefStage.Anger => "Your rage burns bright. Use it, but do not let it consume you.",
                    GriefStage.Bargaining => "There are no bargains to be made with death. Only acceptance.",
                    GriefStage.Depression => "The weight of sorrow is heavy. But it, too, shall pass.",
                    GriefStage.Acceptance => "You have weathered the storm. You are stronger for it.",
                    _ => ""
                };
            }
            else
            {
                return stage switch
                {
                    GriefStage.Denial => "You seem... distracted. Is everything alright?",
                    GriefStage.Anger => "Whoa, easy there! What's got you so worked up?",
                    GriefStage.Bargaining => "You're looking for something. I can tell. What is it?",
                    GriefStage.Depression => "Hey... you don't look so good. Need to talk?",
                    GriefStage.Acceptance => "There's something different about you. Wiser, maybe.",
                    _ => ""
                };
            }
        }

        /// <summary>
        /// Display grief-stage-appropriate atmospheric text
        /// </summary>
        public string GetAtmosphericText(GriefStage stage, string companionName)
        {
            return stage switch
            {
                GriefStage.Denial =>
                    $"You could swear you see {companionName}'s face in the crowd. But when you look again, they're gone.",

                GriefStage.Anger =>
                    "Your blood runs hot. Every enemy reminds you of what you've lost.",

                GriefStage.Bargaining =>
                    $"If only you could go back. Change things. Save {companionName}. There must be a way...",

                GriefStage.Depression =>
                    $"The world seems gray. {companionName}'s absence is a weight on your chest.",

                GriefStage.Acceptance =>
                    $"You think of {companionName} and smile. The pain remains, but it no longer defines you.",

                _ => ""
            };
        }

        /// <summary>
        /// View memory of a fallen companion
        /// </summary>
        public async Task ViewMemory(CompanionId companionId, TerminalEmulator terminal)
        {
            var companionMemories = memories.FindAll(m => m.CompanionId == companionId);
            if (companionMemories.Count == 0)
                return;

            terminal.Clear();
            terminal.WriteLine("╔══════════════════════════════════════════════════════════════════╗", "dark_cyan");
            terminal.WriteLine("║                     M E M O R I E S                              ║", "dark_cyan");
            terminal.WriteLine("╚══════════════════════════════════════════════════════════════════╝", "dark_cyan");
            terminal.WriteLine("");

            foreach (var memory in companionMemories)
            {
                terminal.WriteLine($"  [{memory.CompanionName}]", "bright_cyan");
                terminal.WriteLine($"  {memory.MemoryText}", "white");
                terminal.WriteLine("");
                await Task.Delay(200);
            }

            // Get current grief state if any
            if (activeGrief.TryGetValue(companionId, out var grief))
            {
                if (!grief.IsComplete)
                {
                    terminal.WriteLine($"  Current grief stage: {grief.CurrentStage}", "yellow");
                    terminal.WriteLine($"  Days grieving: {GetCurrentDay() - grief.GriefStartDay}", "gray");
                }
                else
                {
                    terminal.WriteLine("  The grief has passed. The memory remains.", "dark_magenta");
                }
            }

            terminal.WriteLine("");
            await terminal.GetInputAsync("Press Enter to continue...");
        }

        /// <summary>
        /// Add a new memory for a fallen companion
        /// </summary>
        public void AddMemory(CompanionId companionId, string companionName, string memoryText)
        {
            memories.Add(new CompanionMemory
            {
                CompanionId = companionId,
                CompanionName = companionName,
                MemoryText = memoryText,
                CreatedDay = GetCurrentDay()
            });
        }

        #region Private Methods

        private void AdvanceGriefStage(GriefState grief, int currentDay)
        {
            var previousStage = grief.CurrentStage;

            grief.CurrentStage = grief.CurrentStage switch
            {
                GriefStage.Denial => GriefStage.Anger,
                GriefStage.Anger => GriefStage.Bargaining,
                GriefStage.Bargaining => GriefStage.Depression,
                GriefStage.Depression => GriefStage.Acceptance,
                GriefStage.Acceptance => GriefStage.Acceptance, // Terminal state
                _ => GriefStage.Acceptance
            };

            grief.StageStartDay = currentDay;

            if (grief.CurrentStage == GriefStage.Acceptance && previousStage != GriefStage.Acceptance)
            {
                grief.IsComplete = true;

                // Add acceptance memory
                AddMemory(grief.CompanionId, grief.CompanionName,
                    "You have found peace with their passing. They live on in your memory.");

                // Grant wisdom
                GD.Print($"[Grief] Grief complete for {grief.CompanionName}. Player gains +5 Wisdom.");

                OnGriefComplete?.Invoke(grief.CompanionId);
            }

            OnGriefStageChanged?.Invoke(grief.CompanionId, grief.CurrentStage);
            GD.Print($"[Grief] {grief.CompanionName} grief advanced to: {grief.CurrentStage}");
        }

        private int GetStageDuration(GriefStage stage)
        {
            return stage switch
            {
                GriefStage.Denial => 3,
                GriefStage.Anger => 5,
                GriefStage.Bargaining => 3,
                GriefStage.Depression => 7,
                GriefStage.Acceptance => int.MaxValue, // Permanent
                _ => 3
            };
        }

        private GriefEffects GetStageEffects(GriefStage stage)
        {
            return stage switch
            {
                GriefStage.Denial => new GriefEffects
                {
                    CombatModifier = -0.20f,
                    Description = "Your denial clouds your focus."
                },
                GriefStage.Anger => new GriefEffects
                {
                    DamageModifier = 0.30f,
                    DefenseModifier = -0.20f,
                    Description = "Rage empowers your attacks but leaves you vulnerable."
                },
                GriefStage.Bargaining => new GriefEffects
                {
                    CombatModifier = -0.10f,
                    Description = "Your desperate search distracts you."
                },
                GriefStage.Depression => new GriefEffects
                {
                    AllStatModifier = -0.30f,
                    Description = "The weight of sorrow crushes your spirit."
                },
                GriefStage.Acceptance => new GriefEffects
                {
                    PermanentWisdomBonus = 5,
                    Description = "You have made peace with loss."
                },
                _ => new GriefEffects()
            };
        }

        private string GetResurrectionFailure(string method, int attempts)
        {
            var failures = new Dictionary<string, string[]>
            {
                ["temple"] = new[]
                {
                    "The priests shake their heads. 'The soul has moved on. We cannot reach them.'",
                    "'Even the gods cannot reverse true death,' the high priest says gently.",
                    "'Stop this. You are only prolonging your own suffering.'"
                },
                ["necromancy"] = new[]
                {
                    "The dark ritual fails. What rises is not them. It is a hollow shell. You destroy it in horror.",
                    "The spirits mock you. 'You cannot bind a soul that has found peace.'",
                    "The necromancer backs away. 'Their soul is beyond my reach. In the Ocean, perhaps...'"
                },
                ["divine"] = new[]
                {
                    "You pray until your voice gives out. The gods do not answer.",
                    "A whisper in your mind: 'We cannot undo what must be. They are home now.'",
                    "The altar cracks. Your plea has been heard and denied."
                },
                ["artifact"] = new[]
                {
                    "The artifact crumbles to dust. Its power was not meant for this.",
                    "The relic flares and dies. Some barriers cannot be crossed, even with magic.",
                    "You feel the artifact's rejection. It shows you a vision: your companion, at peace."
                }
            };

            var methodKey = method.ToLower() switch
            {
                var s when s.Contains("temple") || s.Contains("priest") => "temple",
                var s when s.Contains("necro") || s.Contains("dark") => "necromancy",
                var s when s.Contains("pray") || s.Contains("god") => "divine",
                _ => "artifact"
            };

            var options = failures[methodKey];
            return options[Math.Min(attempts - 1, options.Length - 1)];
        }

        private string GetInitialMemory(string name, DeathType deathType)
        {
            return deathType switch
            {
                DeathType.Sacrifice =>
                    $"{name} gave their life for you. Their final act was one of love.",
                DeathType.Combat =>
                    $"{name} fell in battle, fighting to the end. They never gave up.",
                DeathType.MoralTrigger =>
                    $"{name} could not stand by and watch you become what they feared. Their opposition was an act of love.",
                DeathType.Inevitable =>
                    $"{name}'s time had come. They knew it, and they faced it with courage.",
                DeathType.ChoiceBased =>
                    $"You made an impossible choice. {name} paid the price. Were they at peace?",
                _ =>
                    $"{name} is gone. But what they meant to you... that remains."
            };
        }

        private int GetCurrentDay()
        {
            return StoryProgressionSystem.Instance.CurrentGameDay;
        }

        #endregion

        #region Serialization

        public GriefSystemData Serialize()
        {
            return new GriefSystemData
            {
                ActiveGrief = new List<GriefState>(activeGrief.Values),
                Memories = new List<CompanionMemory>(memories)
            };
        }

        public void Deserialize(GriefSystemData data)
        {
            if (data == null) return;

            activeGrief.Clear();
            if (data.ActiveGrief != null)
            {
                foreach (var grief in data.ActiveGrief)
                {
                    activeGrief[grief.CompanionId] = grief;
                }
            }

            memories = data.Memories ?? new List<CompanionMemory>();
        }

        #endregion
    }

    #region Grief Data Classes

    public enum GriefStage
    {
        None,        // No active grief
        Denial,
        Anger,
        Bargaining,
        Depression,
        Acceptance
    }

    public class GriefState
    {
        public CompanionId CompanionId { get; set; }
        public string CompanionName { get; set; } = "";
        public DeathType DeathType { get; set; }
        public GriefStage CurrentStage { get; set; }
        public int StageStartDay { get; set; }
        public int GriefStartDay { get; set; }
        public int ResurrectionAttempts { get; set; }
        public bool IsComplete { get; set; }
    }

    public class GriefEffects
    {
        public float CombatModifier { get; set; } = 0;
        public float DamageModifier { get; set; } = 0;
        public float DefenseModifier { get; set; } = 0;
        public float AllStatModifier { get; set; } = 0;
        public int PermanentWisdomBonus { get; set; } = 0;
        public string Description { get; set; } = "";
    }

    public class CompanionMemory
    {
        public CompanionId CompanionId { get; set; }
        public string CompanionName { get; set; } = "";
        public string MemoryText { get; set; } = "";
        public int CreatedDay { get; set; }
    }

    public class GriefDialogueOption
    {
        public string Label { get; set; }
        public string Text { get; set; }

        public GriefDialogueOption(string label, string text)
        {
            Label = label;
            Text = text;
        }
    }

    public class GriefSystemData
    {
        public List<GriefState> ActiveGrief { get; set; } = new();
        public List<CompanionMemory> Memories { get; set; } = new();
    }

    #endregion
}
