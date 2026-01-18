using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using UsurperRemake.Utils;
using UsurperRemake.Data;

namespace UsurperRemake.Systems
{
    /// <summary>
    /// Old God Boss System - Handles epic multi-phase boss encounters with the Old Gods
    /// Each boss has unique mechanics, dialogue, and can potentially be saved or allied with
    /// </summary>
    public class OldGodBossSystem
    {
        private static OldGodBossSystem? instance;
        public static OldGodBossSystem Instance => instance ??= new OldGodBossSystem();

        private Dictionary<OldGodType, OldGodBossData> bossData = new();
        private OldGodBossData? currentBoss;
        private int currentPhase = 1;
        private long bossCurrentHP;
        private bool bossDefeated;
        private bool bossSaved;

        // Team combat support - includes both companions and NPC teammates from dungeon
        private List<BossFightTeammate> activeTeammates = new();
        private List<Character>? dungeonTeammates; // Passed from DungeonLocation

        /// <summary>
        /// Represents any teammate fighting in the boss battle (companion or NPC)
        /// </summary>
        private class BossFightTeammate
        {
            public string Name { get; set; } = "";
            public long CurrentHP { get; set; }
            public long MaxHP { get; set; }
            public long Attack { get; set; }
            public long Defense { get; set; }
            public string Role { get; set; } = "Fighter"; // Tank, DPS, Healer, Hybrid, Fighter
            public string[] Abilities { get; set; } = Array.Empty<string>();
            public bool IsCompanion { get; set; } // True if from CompanionSystem, false if NPC
            public Companion? CompanionRef { get; set; } // Reference if this is a companion
            public Character? CharacterRef { get; set; } // Reference if this is an NPC
            public bool IsAlive => CurrentHP > 0;
        }

        /// <summary>
        /// Check if the current boss was defeated
        /// </summary>
        public bool IsBossDefeated => bossDefeated;

        public event Action<OldGodType>? OnBossDefeated;
        public event Action<OldGodType>? OnBossSaved;
        public event Action<OldGodType, int>? OnPhaseChange;

        public OldGodBossSystem()
        {
            LoadBossData();
        }

        /// <summary>
        /// Load boss data from OldGodsData
        /// </summary>
        private void LoadBossData()
        {
            var allBosses = OldGodsData.GetAllOldGods();
            foreach (var boss in allBosses)
            {
                bossData[boss.Type] = boss;
            }
            // GD.Print($"[BossSystem] Loaded {bossData.Count} Old God bosses");
        }

        /// <summary>
        /// Check if player can encounter a specific Old God
        /// </summary>
        public bool CanEncounterBoss(Character player, OldGodType type)
        {
            if (!bossData.TryGetValue(type, out var boss))
                return false;

            var story = StoryProgressionSystem.Instance;

            // Check if already dealt with (defeated, saved, allied, etc.)
            if (story.OldGodStates.TryGetValue(type, out var state))
            {
                // Gods that have been resolved cannot be encountered again
                if (state.Status == GodStatus.Defeated ||
                    state.Status == GodStatus.Saved ||
                    state.Status == GodStatus.Allied ||
                    state.Status == GodStatus.Awakened ||
                    state.Status == GodStatus.Consumed)
                    return false;
            }

            // Check level requirement based on dungeon floor where god appears, not boss combat level
            // Gods appear on specific floors (25, 40, 55, 70, 85, 95, 100)
            // Player should be within 10 levels of the floor to encounter the boss
            int floorLevel = boss.DungeonFloor;
            if (player.Level < floorLevel - 10) // Allow 10 levels of leeway
                return false;

            // Check prerequisites
            return CheckPrerequisites(type);
        }

        /// <summary>
        /// Check if prerequisites are met for encountering a god
        /// </summary>
        private bool CheckPrerequisites(OldGodType type)
        {
            var story = StoryProgressionSystem.Instance;

            switch (type)
            {
                case OldGodType.Maelketh:
                    // First god, no prerequisites
                    return true;

                case OldGodType.Veloura:
                    // Must have defeated or encountered Maelketh
                    return story.HasStoryFlag("maelketh_defeated") ||
                           story.HasStoryFlag("maelketh_encountered");

                case OldGodType.Thorgrim:
                    // Must have defeated at least one god
                    return story.OldGodStates.Values.Any(s => s.Status == GodStatus.Defeated);

                case OldGodType.Noctura:
                    // Must have defeated at least two gods
                    return story.OldGodStates.Values.Count(s => s.Status == GodStatus.Defeated) >= 2;

                case OldGodType.Aurelion:
                    // Must have defeated at least three gods
                    return story.OldGodStates.Values.Count(s => s.Status == GodStatus.Defeated) >= 3;

                case OldGodType.Terravok:
                    // Must have defeated at least four gods
                    return story.OldGodStates.Values.Count(s => s.Status == GodStatus.Defeated) >= 4;

                case OldGodType.Manwe:
                    // Must have all artifacts and faced all other gods
                    return story.CollectedArtifacts.Count >= 6 &&
                           story.HasStoryFlag("void_key_obtained");

                default:
                    return false;
            }
        }

        /// <summary>
        /// Start a boss encounter
        /// </summary>
        /// <param name="player">The player character</param>
        /// <param name="type">Which Old God to fight</param>
        /// <param name="terminal">Terminal for output</param>
        /// <param name="teammates">Optional list of dungeon teammates (NPCs traveling with player)</param>
        public async Task<BossEncounterResult> StartBossEncounter(
            Character player, OldGodType type, TerminalEmulator terminal, List<Character>? teammates = null)
        {
            if (!bossData.TryGetValue(type, out var boss))
            {
                return new BossEncounterResult { Success = false };
            }

            currentBoss = boss;
            currentPhase = 1;
            bossCurrentHP = boss.HP;
            bossDefeated = false;
            bossSaved = false;
            dungeonTeammates = teammates; // Store for InitializeTeammates

            // GD.Print($"[BossSystem] Starting encounter with {boss.Name}");

            // Play introduction
            await PlayBossIntroduction(boss, terminal);

            // Run dialogue
            var dialogueResult = await DialogueSystem.Instance.StartDialogue(
                player, $"{type.ToString().ToLower()}_encounter", terminal);

            // Check if dialogue led to non-combat resolution
            var story = StoryProgressionSystem.Instance;
            if (story.HasStoryFlag($"{type.ToString().ToLower()}_ally"))
            {
                // Allied with the god
                return new BossEncounterResult
                {
                    Success = true,
                    Outcome = BossOutcome.Allied,
                    God = type
                };
            }

            if (story.HasStoryFlag($"{type.ToString().ToLower()}_spared"))
            {
                // Spared the god (save quest started)
                return new BossEncounterResult
                {
                    Success = true,
                    Outcome = BossOutcome.Spared,
                    God = type
                };
            }

            // Combat time!
            if (story.HasStoryFlag($"{type.ToString().ToLower()}_combat_start"))
            {
                var combatResult = await RunBossCombat(player, boss, terminal);
                return combatResult;
            }

            // Dialogue ended without resolution - shouldn't happen normally
            return new BossEncounterResult { Success = false };
        }

        /// <summary>
        /// Play boss introduction sequence
        /// </summary>
        private async Task PlayBossIntroduction(OldGodBossData boss, TerminalEmulator terminal)
        {
            terminal.Clear();
            terminal.WriteLine("");

            // Build dramatic entrance
            terminal.WriteLine("The ground trembles.", "red");
            await Task.Delay(800);

            terminal.WriteLine("Ancient power stirs.", "bright_red");
            await Task.Delay(800);

            terminal.WriteLine($"The seal around {boss.Name} shatters.", "bright_magenta");
            await Task.Delay(1200);

            terminal.WriteLine("");
            terminal.WriteLine($"╔════════════════════════════════════════════════════════════════╗", boss.ThemeColor);
            terminal.WriteLine($"║                                                                ║", boss.ThemeColor);
            terminal.WriteLine($"║     {CenterText(boss.Name.ToUpper(), 58)}     ║", boss.ThemeColor);
            terminal.WriteLine($"║     {CenterText(boss.Title, 58)}     ║", boss.ThemeColor);
            terminal.WriteLine($"║                                                                ║", boss.ThemeColor);
            terminal.WriteLine($"╚════════════════════════════════════════════════════════════════╝", boss.ThemeColor);
            terminal.WriteLine("");

            await Task.Delay(2000);

            // Show boss stats
            terminal.WriteLine($"  Level: {boss.Level}", "gray");
            terminal.WriteLine($"  HP: {boss.HP:N0}", "red");
            terminal.WriteLine("");

            await terminal.GetInputAsync("  Press Enter to face the Old God...");
        }

        /// <summary>
        /// Run the boss combat encounter
        /// </summary>
        private async Task<BossEncounterResult> RunBossCombat(
            Character player, OldGodBossData boss, TerminalEmulator terminal)
        {
            var story = StoryProgressionSystem.Instance;
            bool playerDead = false;

            // Initialize teammates from companions
            InitializeTeammates();

            while (bossCurrentHP > 0 && !playerDead && !bossSaved)
            {
                terminal.Clear();

                // Display combat status (including teammates)
                DisplayCombatStatus(player, boss, terminal);

                // Check for phase transitions
                await CheckPhaseTransition(boss, terminal);

                // Player turn
                var action = await GetPlayerAction(player, boss, terminal);

                switch (action)
                {
                    case BossAction.Attack:
                        await PlayerAttack(player, boss, terminal);
                        break;

                    case BossAction.Special:
                        await PlayerSpecialAttack(player, boss, terminal);
                        break;

                    case BossAction.Heal:
                        await PlayerHeal(player, terminal);
                        break;

                    case BossAction.Save:
                        if (await AttemptToSaveBoss(player, boss, terminal))
                        {
                            bossSaved = true;
                            continue;
                        }
                        break;

                    case BossAction.Flee:
                        if (new Random().NextDouble() < 0.2) // 20% flee chance
                        {
                            terminal.WriteLine("You manage to escape!", "green");
                            await Task.Delay(1500);
                            return new BossEncounterResult
                            {
                                Success = false,
                                Outcome = BossOutcome.Fled,
                                God = boss.Type
                            };
                        }
                        terminal.WriteLine($"{boss.Name} blocks your escape!", "red");
                        await Task.Delay(1000);
                        break;
                }

                // Teammates attack the boss
                if (bossCurrentHP > 0 && !bossSaved)
                {
                    await TeammatesAttack(boss, terminal);
                }

                // Boss turn (if still alive)
                if (bossCurrentHP > 0 && !bossSaved)
                {
                    playerDead = await BossTurn(player, boss, terminal);
                }
            }

            // Determine outcome
            if (bossSaved)
            {
                return await HandleBossSaved(player, boss, terminal);
            }
            else if (bossCurrentHP <= 0)
            {
                bossDefeated = true;
                return await HandleBossDefeated(player, boss, terminal);
            }
            else
            {
                return await HandlePlayerDefeated(player, boss, terminal);
            }
        }

        /// <summary>
        /// Display combat status
        /// </summary>
        private void DisplayCombatStatus(Character player, OldGodBossData boss, TerminalEmulator terminal)
        {
            terminal.WriteLine("");
            terminal.WriteLine($"══════════════════════════════════════════════════════════════", boss.ThemeColor);
            terminal.WriteLine($"  {boss.Name} - Phase {currentPhase}", boss.ThemeColor);
            terminal.WriteLine($"══════════════════════════════════════════════════════════════", boss.ThemeColor);
            terminal.WriteLine("");

            // Boss HP bar
            var hpPercent = (double)bossCurrentHP / boss.HP;
            var hpBar = RenderHealthBar(hpPercent, 40);
            var hpColor = hpPercent > 0.5 ? "green" : hpPercent > 0.2 ? "yellow" : "red";
            terminal.WriteLine($"  Boss HP: [{hpBar}] {bossCurrentHP:N0}/{boss.HP:N0}", hpColor);
            terminal.WriteLine("");

            // Player HP bar
            terminal.WriteLine($"  ═══ YOUR PARTY ═══", "bright_cyan");
            var playerPercent = (double)player.HP / player.MaxHP;
            var playerBar = RenderHealthBar(playerPercent, 30);
            var playerColor = playerPercent > 0.5 ? "green" : playerPercent > 0.2 ? "yellow" : "red";
            terminal.WriteLine($"  You:     [{playerBar}] {player.HP}/{player.MaxHP}", playerColor);

            // Teammate HP bars
            foreach (var teammate in activeTeammates)
            {
                var tmPercent = (double)teammate.CurrentHP / teammate.MaxHP;
                var tmBar = RenderHealthBar(tmPercent, 30);
                var tmColor = teammate.IsAlive ? (tmPercent > 0.5 ? "cyan" : tmPercent > 0.2 ? "yellow" : "red") : "dark_gray";
                var status = teammate.IsAlive ? $"{teammate.CurrentHP}/{teammate.MaxHP}" : "FALLEN";
                var roleTag = teammate.IsCompanion ? "" : $" ({teammate.Role})";
                terminal.WriteLine($"  {teammate.Name,-8} [{tmBar}] {status}{roleTag}", tmColor);
            }

            terminal.WriteLine("");
            if (player.MaxMana > 0)
            {
                terminal.WriteLine($"  Mana:    {player.Mana}/{player.MaxMana}", "cyan");
            }
            terminal.WriteLine($"  Potions: {player.Healing}", "green");
            terminal.WriteLine("");
        }

        /// <summary>
        /// Check and handle phase transitions
        /// </summary>
        private async Task CheckPhaseTransition(OldGodBossData boss, TerminalEmulator terminal)
        {
            var hpPercent = (double)bossCurrentHP / boss.HP;

            int newPhase = currentPhase;
            if (hpPercent <= 0.2 && currentPhase < 3)
                newPhase = 3;
            else if (hpPercent <= 0.5 && currentPhase < 2)
                newPhase = 2;

            if (newPhase != currentPhase)
            {
                currentPhase = newPhase;
                OnPhaseChange?.Invoke(boss.Type, currentPhase);

                terminal.WriteLine("");
                terminal.WriteLine($"═══ PHASE {currentPhase} ═══", "bright_red");
                terminal.WriteLine("");

                // Play phase dialogue
                var dialogue = currentPhase switch
                {
                    2 => boss.Phase2Dialogue,
                    3 => boss.Phase3Dialogue,
                    _ => Array.Empty<string>()
                };

                foreach (var line in dialogue)
                {
                    terminal.WriteLine($"  \"{line}\"", boss.ThemeColor);
                    await Task.Delay(200);
                }

                terminal.WriteLine("");
                await Task.Delay(1500);
            }
        }

        /// <summary>
        /// Get player action choice
        /// </summary>
        private async Task<BossAction> GetPlayerAction(
            Character player, OldGodBossData boss, TerminalEmulator terminal)
        {
            terminal.WriteLine("  What do you do?", "cyan");
            terminal.WriteLine("");
            terminal.WriteLine("  [A] Attack", "white");
            terminal.WriteLine("  [S] Special Attack (costs mana)", player.Mana > 0 ? "bright_cyan" : "gray");

            if (boss.CanBeSaved && ArtifactSystem.Instance.HasArtifact(ArtifactType.SoulweaversLoom))
            {
                terminal.WriteLine("  [R] Attempt to Save (Soulweaver's Loom)", "bright_magenta");
            }

            terminal.WriteLine("  [H] Heal (use potion)", player.Healing > 0 ? "green" : "gray");
            terminal.WriteLine("  [F] Attempt to Flee", "yellow");
            terminal.WriteLine("");

            while (true)
            {
                var input = await terminal.GetInputAsync("  Choice: ");

                switch (input.ToUpper())
                {
                    case "A":
                        return BossAction.Attack;
                    case "S":
                        if (player.Mana > 0) return BossAction.Special;
                        terminal.WriteLine("  No mana!", "red");
                        break;
                    case "R":
                        if (boss.CanBeSaved && ArtifactSystem.Instance.HasArtifact(ArtifactType.SoulweaversLoom))
                            return BossAction.Save;
                        terminal.WriteLine("  Cannot save this god.", "red");
                        break;
                    case "H":
                        if (player.Healing > 0) return BossAction.Heal;
                        terminal.WriteLine("  No potions!", "red");
                        break;
                    case "F":
                        return BossAction.Flee;
                }
            }
        }

        /// <summary>
        /// Process player attack
        /// </summary>
        private async Task PlayerAttack(Character player, OldGodBossData boss, TerminalEmulator terminal)
        {
            var random = new Random();
            long baseDamage = player.Strength + player.WeapPow;
            long damage = baseDamage + random.Next((int)(baseDamage / 2));

            // Artifact bonuses
            damage += ArtifactSystem.Instance.GetTotalArtifactPower() / 10;

            // Phase resistance
            damage = (long)(damage * (1.0 - (currentPhase - 1) * 0.1));

            terminal.WriteLine($"  You strike {boss.Name}!", "bright_yellow");
            terminal.WriteLine($"  Dealt {damage:N0} damage!", "green");

            bossCurrentHP -= damage;

            await Task.Delay(800);
        }

        /// <summary>
        /// Process player special attack
        /// </summary>
        private async Task PlayerSpecialAttack(Character player, OldGodBossData boss, TerminalEmulator terminal)
        {
            var random = new Random();
            int manaCost = 20 + player.Level;

            if (player.Mana < manaCost)
            {
                terminal.WriteLine("  Not enough mana!", "red");
                await Task.Delay(500);
                return;
            }

            player.Mana -= manaCost;

            long baseDamage = (player.Strength + player.WeapPow) * 2 + player.Wisdom;
            long damage = baseDamage + random.Next((int)Math.Min(baseDamage, int.MaxValue));

            // Special attack ignores some phase resistance
            damage = (long)(damage * (1.0 - (currentPhase - 1) * 0.05));

            terminal.WriteLine($"  You unleash a devastating special attack!", "bright_magenta");
            terminal.WriteLine($"  Dealt {damage:N0} damage!", "bright_green");

            bossCurrentHP -= damage;

            await Task.Delay(1000);
        }

        /// <summary>
        /// Process player healing
        /// </summary>
        private async Task PlayerHeal(Character player, TerminalEmulator terminal)
        {
            if (player.Healing <= 0)
            {
                terminal.WriteLine("  No healing potions!", "red");
                return;
            }

            player.Healing--;
            long healAmount = player.MaxHP / 3;
            long oldHP = player.HP;
            player.HP = Math.Min(player.HP + healAmount, player.MaxHP);
            long actualHeal = player.HP - oldHP;

            // Track statistics
            player.Statistics.RecordPotionUsed(actualHeal);

            terminal.WriteLine($"  You drink a healing potion!", "green");
            terminal.WriteLine($"  Restored {actualHeal} HP!", "bright_green");

            await Task.Delay(800);
        }

        /// <summary>
        /// Attempt to save a god using the Soulweaver's Loom
        /// </summary>
        private async Task<bool> AttemptToSaveBoss(
            Character player, OldGodBossData boss, TerminalEmulator terminal)
        {
            if (!boss.CanBeSaved)
            {
                terminal.WriteLine($"  {boss.Name} cannot be saved.", "red");
                return false;
            }

            if (!ArtifactSystem.Instance.HasArtifact(ArtifactType.SoulweaversLoom))
            {
                terminal.WriteLine("  You need the Soulweaver's Loom!", "red");
                return false;
            }

            // Saving requires boss to be below 50% HP and player to have high alignment
            var hpPercent = (double)bossCurrentHP / boss.HP;
            if (hpPercent > 0.5)
            {
                terminal.WriteLine($"  {boss.Name} is too strong to save yet.", "yellow");
                terminal.WriteLine("  Weaken them first.", "gray");
                return false;
            }

            if (player.Chivalry - player.Darkness < 200)
            {
                terminal.WriteLine("  Your heart is too dark to use the Loom.", "dark_red");
                return false;
            }

            terminal.WriteLine("");
            terminal.WriteLine("  The Soulweaver's Loom glows with ancient power.", "bright_magenta");
            await Task.Delay(800);

            terminal.WriteLine($"  You reach out to {boss.Name}'s corrupted essence...", "white");
            await Task.Delay(1000);

            // Display save dialogue
            foreach (var line in boss.SaveDialogue)
            {
                terminal.WriteLine($"  \"{line}\"", "bright_cyan");
                await Task.Delay(300);
            }

            terminal.WriteLine("");
            terminal.WriteLine($"  {boss.Name} is cleansed of corruption!", "bright_green");

            return true;
        }

        /// <summary>
        /// Process boss turn - attacks player and teammates
        /// </summary>
        private async Task<bool> BossTurn(Character player, OldGodBossData boss, TerminalEmulator terminal)
        {
            var random = new Random();

            // Boss attacks multiple times based on AttacksPerRound
            int attackCount = boss.AttacksPerRound;

            terminal.WriteLine("");
            terminal.WriteLine($"  ═══ {boss.Name.ToUpper()}'S TURN ═══", boss.ThemeColor);

            for (int attack = 0; attack < attackCount; attack++)
            {
                if (player.HP <= 0) break;

                // Select ability based on phase
                var abilities = boss.Abilities.Where(a => a.Phase <= currentPhase).ToList();
                var ability = abilities[random.Next(abilities.Count)];

                terminal.WriteLine("");
                terminal.WriteLine($"  {boss.Name} uses {ability.Name}!", boss.ThemeColor);

                long damage = ability.BaseDamage + random.Next(ability.BaseDamage / 2);
                damage = (long)(damage * (1.0 + (currentPhase - 1) * 0.15)); // Phase scaling

                // Select target: player or a teammate
                // 60% chance to target player, 40% to target a random alive teammate
                var aliveTeammates = activeTeammates.Where(t => t.IsAlive).ToList();
                bool targetPlayer = aliveTeammates.Count == 0 || random.Next(100) < 60;

                if (targetPlayer)
                {
                    // Attack player
                    long defense = player.Defence + player.ArmPow;
                    double defenseReduction = Math.Min(0.75, defense / (double)(defense + 200));
                    long actualDamage = (long)(damage * (1.0 - defenseReduction));
                    actualDamage = Math.Max(10, actualDamage);

                    player.HP -= actualDamage;

                    terminal.WriteLine($"  You take {actualDamage} damage!", "red");
                }
                else
                {
                    // Attack a random teammate
                    var target = aliveTeammates[random.Next(aliveTeammates.Count)];

                    // Apply teammate defense as percentage reduction
                    double teammateDefenseReduction = Math.Min(0.50, target.Defense / (double)(target.Defense + 100));
                    long actualDamage = (long)(damage * (1.0 - teammateDefenseReduction));
                    actualDamage = Math.Max(15, actualDamage);

                    target.CurrentHP -= actualDamage;

                    // Also damage the underlying character if it's an NPC
                    if (!target.IsCompanion && target.CharacterRef != null)
                    {
                        target.CharacterRef.HP = Math.Max(0, target.CharacterRef.HP - actualDamage);
                    }

                    if (target.CurrentHP <= 0)
                    {
                        target.CurrentHP = 0;
                        terminal.WriteLine($"  {target.Name} takes {actualDamage} damage and falls!", "dark_red");
                        terminal.WriteLine($"  \"{target.Name}! NO!\"", "red");
                    }
                    else
                    {
                        terminal.WriteLine($"  {target.Name} takes {actualDamage} damage!", "yellow");
                    }
                }

                // Special effects
                if (ability.HasSpecialEffect)
                {
                    terminal.WriteLine($"  {ability.EffectDescription}", "dark_red");
                }

                await Task.Delay(500);
            }

            if (attackCount > 1)
            {
                terminal.WriteLine($"  ({attackCount} attacks this round)", "gray");
            }

            await Task.Delay(500);

            return player.HP <= 0;
        }

        /// <summary>
        /// Handle boss being saved
        /// </summary>
        private async Task<BossEncounterResult> HandleBossSaved(
            Character player, OldGodBossData boss, TerminalEmulator terminal)
        {
            terminal.Clear();
            terminal.WriteLine("");
            terminal.WriteLine($"╔═══════════════════════════════════════════════════════════════╗", "bright_green");
            terminal.WriteLine($"║                  {boss.Name.ToUpper()} SAVED                          ║", "bright_green");
            terminal.WriteLine($"╚═══════════════════════════════════════════════════════════════╝", "bright_green");
            terminal.WriteLine("");

            await Task.Delay(1500);

            terminal.WriteLine($"  The corruption fades from {boss.Name}.", "white");
            terminal.WriteLine("  For the first time in millennia, they see clearly.", "white");
            terminal.WriteLine("");

            foreach (var line in boss.SaveDialogue)
            {
                terminal.WriteLine($"  \"{line}\"", "bright_cyan");
                await Task.Delay(300);
            }

            terminal.WriteLine("");

            // Update story state
            var story = StoryProgressionSystem.Instance;
            story.UpdateGodState(boss.Type, GodStatus.Saved);
            story.SetStoryFlag($"{boss.Type.ToString().ToLower()}_saved", true);

            // Award experience
            long xpReward = boss.Level * 1000;
            player.Experience += xpReward;
            terminal.WriteLine($"  (+{xpReward:N0} Experience for showing mercy)", "cyan");

            // Award chivalry
            player.Chivalry += 100;
            terminal.WriteLine($"  (+100 Chivalry)", "bright_green");

            OnBossSaved?.Invoke(boss.Type);

            await terminal.GetInputAsync("  Press Enter to continue...");

            return new BossEncounterResult
            {
                Success = true,
                Outcome = BossOutcome.Saved,
                God = boss.Type,
                XPGained = xpReward
            };
        }

        /// <summary>
        /// Handle boss being defeated
        /// </summary>
        private async Task<BossEncounterResult> HandleBossDefeated(
            Character player, OldGodBossData boss, TerminalEmulator terminal)
        {
            terminal.Clear();
            terminal.WriteLine("");
            terminal.WriteLine($"╔═══════════════════════════════════════════════════════════════╗", "bright_yellow");
            terminal.WriteLine($"║                {boss.Name.ToUpper()} DEFEATED                         ║", "bright_yellow");
            terminal.WriteLine($"╚═══════════════════════════════════════════════════════════════╝", "bright_yellow");
            terminal.WriteLine("");

            await Task.Delay(1500);

            foreach (var line in boss.DefeatDialogue)
            {
                terminal.WriteLine($"  \"{line}\"", boss.ThemeColor);
                await Task.Delay(300);
            }

            terminal.WriteLine("");
            terminal.WriteLine($"  {boss.Name}'s divine essence scatters to the winds.", "white");
            terminal.WriteLine("");

            // Update story state
            var story = StoryProgressionSystem.Instance;
            story.UpdateGodState(boss.Type, GodStatus.Defeated);
            story.SetStoryFlag($"{boss.Type.ToString().ToLower()}_destroyed", true);

            // Award artifact
            await ArtifactSystem.Instance.CollectArtifact(player, boss.ArtifactDropped, terminal);

            // Award experience
            long xpReward = boss.Level * 2000;
            player.Experience += xpReward;
            terminal.WriteLine($"  (+{xpReward:N0} Experience)", "cyan");

            // Award gold
            int goldReward = boss.Level * 500;
            player.Gold += goldReward;
            terminal.WriteLine($"  (+{goldReward:N0} Gold)", "yellow");

            OnBossDefeated?.Invoke(boss.Type);

            await terminal.GetInputAsync("  Press Enter to continue...");

            return new BossEncounterResult
            {
                Success = true,
                Outcome = BossOutcome.Defeated,
                God = boss.Type,
                XPGained = xpReward,
                GoldGained = goldReward
            };
        }

        /// <summary>
        /// Handle player being defeated
        /// </summary>
        private async Task<BossEncounterResult> HandlePlayerDefeated(
            Character player, OldGodBossData boss, TerminalEmulator terminal)
        {
            terminal.Clear();
            terminal.WriteLine("");
            terminal.WriteLine("╔═══════════════════════════════════════════════════════════════╗", "dark_red");
            terminal.WriteLine("║                       D E F E A T                             ║", "dark_red");
            terminal.WriteLine("╚═══════════════════════════════════════════════════════════════╝", "dark_red");
            terminal.WriteLine("");

            await Task.Delay(1500);

            terminal.WriteLine($"  {boss.Name} stands over your broken form.", "red");
            terminal.WriteLine("");

            terminal.WriteLine($"  \"Pathetic mortal. Did you truly think", boss.ThemeColor);
            terminal.WriteLine($"   you could challenge a god?\"", boss.ThemeColor);
            terminal.WriteLine("");

            await Task.Delay(2000);

            // Player doesn't die permanently in boss fights - they're sent back
            terminal.WriteLine("  As darkness claims you, a voice speaks.", "gray");
            terminal.WriteLine("  \"Not yet. The wheel has not finished turning.\"", "bright_magenta");
            terminal.WriteLine("");
            terminal.WriteLine("  You awaken at the dungeon entrance, weakened but alive.", "white");

            player.HP = player.MaxHP / 4;
            player.Experience = Math.Max(0, player.Experience - (boss.Level * 100));

            await terminal.GetInputAsync("  Press Enter to continue...");

            return new BossEncounterResult
            {
                Success = false,
                Outcome = BossOutcome.PlayerDefeated,
                God = boss.Type
            };
        }

        #region Helper Methods

        /// <summary>
        /// Initialize teammates from both active companions AND dungeon teammates (NPCs)
        /// </summary>
        private void InitializeTeammates()
        {
            activeTeammates.Clear();

            // Add companions from CompanionSystem
            var companionSystem = CompanionSystem.Instance;
            if (companionSystem != null)
            {
                foreach (var companion in companionSystem.GetActiveCompanions())
                {
                    if (companion != null && !companion.IsDead)
                    {
                        // Scale companion HP based on their level and base stats
                        long scaledHP = companion.BaseStats.HP * (1 + companion.Level / 10);

                        // Determine role string from CombatRole
                        string role = companion.CombatRole switch
                        {
                            CombatRole.Tank => "Tank",
                            CombatRole.Damage => "DPS",
                            CombatRole.Healer => "Healer",
                            CombatRole.Hybrid => "Hybrid",
                            _ => "Fighter"
                        };

                        activeTeammates.Add(new BossFightTeammate
                        {
                            Name = companion.Name,
                            CurrentHP = scaledHP,
                            MaxHP = scaledHP,
                            Attack = companion.BaseStats.Attack + companion.Level * 2,
                            Defense = companion.BaseStats.Defense,
                            Role = role,
                            Abilities = companion.Abilities ?? Array.Empty<string>(),
                            IsCompanion = true,
                            CompanionRef = companion
                        });
                    }
                }
            }

            // Add NPC teammates from dungeon (spouses, team members, lovers, etc.)
            if (dungeonTeammates != null)
            {
                foreach (var npc in dungeonTeammates)
                {
                    if (npc != null && npc.IsAlive)
                    {
                        // Skip if this is a companion Character (already added above)
                        if (npc.IsCompanion) continue;

                        // Determine role based on class
                        string role = npc.Class switch
                        {
                            CharacterClass.Warrior or CharacterClass.Barbarian or CharacterClass.Paladin => "Tank",
                            CharacterClass.Assassin or CharacterClass.Ranger => "DPS",
                            CharacterClass.Cleric => "Healer",
                            CharacterClass.Magician or CharacterClass.Sage => "Hybrid",
                            _ => "Fighter"
                        };

                        // Generate abilities based on class
                        string[] abilities = npc.Class switch
                        {
                            CharacterClass.Warrior => new[] { "Cleave", "Shield Bash", "Battle Cry" },
                            CharacterClass.Barbarian => new[] { "Rage Strike", "Reckless Attack", "Intimidate" },
                            CharacterClass.Paladin => new[] { "Holy Strike", "Lay on Hands", "Divine Shield" },
                            CharacterClass.Assassin => new[] { "Backstab", "Poison Strike", "Shadow Step" },
                            CharacterClass.Ranger => new[] { "Precise Shot", "Twin Strike", "Hunter's Mark" },
                            CharacterClass.Cleric => new[] { "Divine Smite", "Heal", "Holy Light" },
                            CharacterClass.Magician => new[] { "Fireball", "Lightning Bolt", "Arcane Blast" },
                            CharacterClass.Sage => new[] { "Mind Blast", "Energy Drain", "Mystic Strike" },
                            CharacterClass.Bard => new[] { "Inspiring Song", "Cutting Words", "Vicious Mockery" },
                            CharacterClass.Alchemist => new[] { "Acid Flask", "Healing Bomb", "Explosive Mixture" },
                            CharacterClass.Jester => new[] { "Trick Attack", "Confuse", "Lucky Strike" },
                            _ => new[] { "Attack", "Power Strike", "Defend" }
                        };

                        activeTeammates.Add(new BossFightTeammate
                        {
                            Name = npc.DisplayName,
                            CurrentHP = npc.HP,
                            MaxHP = npc.MaxHP,
                            Attack = npc.Strength + npc.WeapPow,
                            Defense = npc.Defence + npc.ArmPow,
                            Role = role,
                            Abilities = abilities,
                            IsCompanion = false,
                            CharacterRef = npc
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Process all teammates' attacks against the boss
        /// </summary>
        private async Task TeammatesAttack(OldGodBossData boss, TerminalEmulator terminal)
        {
            var aliveTeammates = activeTeammates.Where(t => t.IsAlive).ToList();
            if (aliveTeammates.Count == 0) return;

            terminal.WriteLine("");
            terminal.WriteLine($"  ═══ YOUR ALLIES ATTACK ═══", "bright_cyan");

            var random = new Random();

            foreach (var teammate in aliveTeammates)
            {
                if (bossCurrentHP <= 0) break;

                // Calculate teammate damage based on their role and stats
                long baseDamage = teammate.Attack;

                // Add role-specific bonuses
                switch (teammate.Role)
                {
                    case "DPS":
                        baseDamage = (long)(baseDamage * 1.5); // DPS does 50% more damage
                        break;
                    case "Hybrid":
                        baseDamage = (long)(baseDamage * 1.2); // Hybrids do 20% more
                        break;
                    case "Tank":
                        baseDamage = (long)(baseDamage * 0.8); // Tanks do less damage
                        break;
                    case "Healer":
                        baseDamage = (long)(baseDamage * 0.6); // Healers do least damage
                        break;
                    case "Fighter":
                    default:
                        // Standard damage for generic fighters
                        break;
                }

                // Add some variance
                long damage = baseDamage + random.Next(Math.Max(1, (int)(baseDamage / 3)));

                // Phase resistance affects teammates
                damage = (long)(damage * (1.0 - (currentPhase - 1) * 0.05));
                damage = Math.Max(5, damage);

                bossCurrentHP -= damage;

                // Pick an ability name
                string abilityName = GetTeammateAbilityName(teammate);

                terminal.WriteLine($"  {teammate.Name} uses {abilityName}!", "cyan");
                terminal.WriteLine($"    Dealt {damage:N0} damage to {boss.Name}!", "bright_cyan");

                await Task.Delay(400);
            }

            // Healers may heal the party
            foreach (var teammate in aliveTeammates.Where(t => t.Role == "Healer" || t.Role == "Hybrid"))
            {
                // 50% chance to heal
                if (random.Next(100) < 50)
                {
                    long healAmount = 20 + random.Next(30);
                    if (teammate.IsCompanion && teammate.CompanionRef != null)
                    {
                        healAmount = teammate.CompanionRef.BaseStats.HealingPower * 2 + random.Next(20);
                    }

                    terminal.WriteLine($"  {teammate.Name} channels healing energy!", "green");
                    terminal.WriteLine($"    The party is healed for {healAmount} HP!", "bright_green");

                    await Task.Delay(300);
                }
            }

            await Task.Delay(500);
        }

        /// <summary>
        /// Get a thematic ability name for a teammate based on their role and abilities
        /// </summary>
        private string GetTeammateAbilityName(BossFightTeammate teammate)
        {
            var random = new Random();

            if (teammate.Abilities != null && teammate.Abilities.Length > 0)
            {
                // Use one of their defined abilities (skip "Sacrifice")
                var usableAbilities = teammate.Abilities.Where(a => !a.Contains("Sacrifice")).ToArray();
                if (usableAbilities.Length > 0)
                {
                    return usableAbilities[random.Next(usableAbilities.Length)];
                }
            }

            // Fallback generic attacks by role
            return teammate.Role switch
            {
                "Tank" => "Shield Bash",
                "DPS" => "Vicious Strike",
                "Healer" => "Divine Smite",
                "Hybrid" => "Power Attack",
                "Fighter" => "Heavy Blow",
                _ => "Attack"
            };
        }

        private string CenterText(string text, int width)
        {
            if (text.Length >= width) return text;
            int padding = (width - text.Length) / 2;
            return text.PadLeft(padding + text.Length).PadRight(width);
        }

        private string RenderHealthBar(double percent, int width)
        {
            int filled = (int)(percent * width);
            return new string('█', filled) + new string('░', width - filled);
        }

        #endregion
    }

    #region Boss System Data Classes

    public enum BossAction
    {
        Attack,
        Special,
        Heal,
        Save,
        Flee
    }

    public enum BossOutcome
    {
        Defeated,
        Saved,
        Allied,
        Spared,
        PlayerDefeated,
        Fled
    }

    public class BossEncounterResult
    {
        public bool Success { get; set; }
        public BossOutcome Outcome { get; set; }
        public OldGodType God { get; set; }
        public long XPGained { get; set; }
        public int GoldGained { get; set; }
    }

    #endregion
}
