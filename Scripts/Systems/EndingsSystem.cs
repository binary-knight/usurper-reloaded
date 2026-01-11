using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using UsurperRemake.Utils;

namespace UsurperRemake.Systems
{
    /// <summary>
    /// Endings System - Handles the three main endings plus the secret true ending
    /// Manages credits, epilogues, and transition to New Game+
    /// </summary>
    public class EndingsSystem
    {
        private static EndingsSystem? instance;
        public static EndingsSystem Instance => instance ??= new EndingsSystem();

        public event Action<EndingType>? OnEndingTriggered;
        public event Action? OnCreditsComplete;

        /// <summary>
        /// Determine which ending the player qualifies for
        /// </summary>
        public EndingType DetermineEnding(Character player)
        {
            // Null check for player
            if (player == null)
            {
                return EndingType.Defiant; // Default fallback
            }

            var story = StoryProgressionSystem.Instance;
            var ocean = OceanPhilosophySystem.Instance;
            var amnesia = AmnesiaSystem.Instance;
            var companions = CompanionSystem.Instance;
            var grief = GriefSystem.Instance;

            // Check for Secret Ending (Dissolution) first - requires Cycle 3+
            if (story?.CurrentCycle >= 3 && QualifiesForDissolutionEnding(player))
            {
                return EndingType.Secret;
            }

            // Check for Enhanced True Ending
            if (QualifiesForEnhancedTrueEnding(player))
            {
                return EndingType.TrueEnding;
            }

            // Fallback to legacy true ending check
            if (CycleSystem.Instance?.QualifiesForTrueEnding(player) == true)
            {
                return EndingType.TrueEnding;
            }

            // Calculate alignment
            long alignment = player.Chivalry - player.Darkness;

            // Count saved vs destroyed gods (all 6 Old Gods)
            int savedGods = 0;
            int destroyedGods = 0;

            // Veloura - Goddess of Illusions
            if (story.HasStoryFlag("veloura_saved")) savedGods++;
            if (story.HasStoryFlag("veloura_destroyed")) destroyedGods++;
            // Aurelion - God of Light
            if (story.HasStoryFlag("aurelion_saved")) savedGods++;
            if (story.HasStoryFlag("aurelion_destroyed")) destroyedGods++;
            // Terravok - God of Earth
            if (story.HasStoryFlag("terravok_awakened")) savedGods++;
            if (story.HasStoryFlag("terravok_destroyed")) destroyedGods++;
            // Noctura - Goddess of Night
            if (story.HasStoryFlag("noctura_ally")) savedGods++;
            if (story.HasStoryFlag("noctura_destroyed")) destroyedGods++;
            // Maelketh - God of Chaos
            if (story.HasStoryFlag("maelketh_saved")) savedGods++;
            if (story.HasStoryFlag("maelketh_destroyed")) destroyedGods++;
            // Thorgrim - God of War
            if (story.HasStoryFlag("thorgrim_saved")) savedGods++;
            if (story.HasStoryFlag("thorgrim_destroyed")) destroyedGods++;

            // Determine ending based on choices
            if (alignment < -300 || destroyedGods >= 5)
            {
                return EndingType.Usurper; // Dark path - take Manwe's place
            }
            else if (alignment > 300 || savedGods >= 3)
            {
                return EndingType.Savior; // Light path - redeem the gods
            }
            else
            {
                return EndingType.Defiant; // Independent path - reject all gods
            }
        }

        /// <summary>
        /// Check if player qualifies for the enhanced True Ending
        /// Requirements:
        /// 1. All 7 seals collected
        /// 2. Awakening Level 7 (full Ocean Philosophy understanding)
        /// 3. At least one companion died (experienced loss)
        /// 4. Spared at least 2 gods
        /// 5. Net alignment near zero (balance)
        /// 6. Completed personal quest of deceased companion (optional bonus)
        /// </summary>
        private bool QualifiesForEnhancedTrueEnding(Character player)
        {
            if (player == null)
                return false;

            var story = StoryProgressionSystem.Instance;
            var ocean = OceanPhilosophySystem.Instance;
            var companions = CompanionSystem.Instance;
            var grief = GriefSystem.Instance;

            // 1. All 7 seals collected
            if (story?.CollectedSeals == null || story.CollectedSeals.Count < 7)
                return false;

            // 2. Awakening Level 7
            if (ocean?.AwakeningLevel < 7)
                return false;

            // 3. Experienced companion loss
            if (ocean?.ExperiencedMoments?.Contains(AwakeningMoment.FirstCompanionDeath) != true &&
                grief?.HasCompletedGriefCycle != true)
                return false;

            // 4. Spared at least 2 gods
            int sparedGods = 0;
            if (story.HasStoryFlag("veloura_saved")) sparedGods++;
            if (story.HasStoryFlag("aurelion_saved")) sparedGods++;
            if (story.HasStoryFlag("noctura_ally")) sparedGods++;
            if (story.HasStoryFlag("terravok_awakened")) sparedGods++;
            if (sparedGods < 2)
                return false;

            // 5. Alignment near zero (within +/- 500)
            long alignment = player.Chivalry - player.Darkness;
            if (Math.Abs(alignment) > 500)
                return false;

            return true;
        }

        /// <summary>
        /// Check if player qualifies for the secret Dissolution ending
        /// The ultimate ending - dissolving back into the Ocean
        /// </summary>
        private bool QualifiesForDissolutionEnding(Character player)
        {
            if (player == null)
                return false;

            var story = StoryProgressionSystem.Instance;
            var ocean = OceanPhilosophySystem.Instance;
            var amnesia = AmnesiaSystem.Instance;

            // Must have completed at least 2 other endings
            if (story?.CompletedEndings == null || story.CompletedEndings.Count < 2)
                return false;

            // Must have max awakening
            if (ocean?.AwakeningLevel < 7)
                return false;

            // Must have full memory recovery (know you are Fragment of Manwe)
            if (amnesia?.TruthRevealed != true)
                return false;

            // Must have all wave fragments
            if (ocean?.CollectedFragments == null || ocean.CollectedFragments.Count < 7)
                return false;

            // Must have story flag for being ready
            return story?.HasStoryFlag("ready_for_dissolution") == true;
        }

        /// <summary>
        /// Trigger an ending sequence
        /// </summary>
        public async Task TriggerEnding(Character player, EndingType ending, TerminalEmulator terminal)
        {
            OnEndingTriggered?.Invoke(ending);

            switch (ending)
            {
                case EndingType.Usurper:
                    await PlayUsurperEnding(player, terminal);
                    break;
                case EndingType.Savior:
                    await PlaySaviorEnding(player, terminal);
                    break;
                case EndingType.Defiant:
                    await PlayDefiantEnding(player, terminal);
                    break;
                case EndingType.TrueEnding:
                    await PlayEnhancedTrueEnding(player, terminal);
                    break;
                case EndingType.Secret:
                    await PlayDissolutionEnding(player, terminal);
                    return; // Dissolution ending doesn't lead to NG+ - save deleted
            }

            // Record ending in story
            StoryProgressionSystem.Instance.RecordChoice("final_ending", ending.ToString(), 0);
            StoryProgressionSystem.Instance.SetStoryFlag($"ending_{ending.ToString().ToLower()}_achieved", true);

            // Play credits
            await PlayCredits(player, ending, terminal);

            // Offer New Game+
            await OfferNewGamePlus(player, ending, terminal);
        }

        #region Ending Sequences

        private async Task PlayUsurperEnding(Character player, TerminalEmulator terminal)
        {
            terminal.Clear();
            await Task.Delay(1000);

            terminal.WriteLine("");
            terminal.WriteLine("╔═══════════════════════════════════════════════════════════════════╗", "dark_red");
            terminal.WriteLine("║                     T H E   U S U R P E R                         ║", "dark_red");
            terminal.WriteLine("╚═══════════════════════════════════════════════════════════════════╝", "dark_red");
            terminal.WriteLine("");

            await Task.Delay(2000);

            var lines = new[]
            {
                ("Manwe falls before you, his divine essence shattering.", "white"),
                ("The Creator's power flows into your being.", "dark_red"),
                ("You feel eternity stretch before you.", "white"),
                ("", "white"),
                ("\"You wanted power,\" the dying god whispers.", "yellow"),
                ("\"Now you have it. All of it.\"", "yellow"),
                ("\"But power... power is a prison of its own.\"", "yellow"),
                ("", "white"),
                ("You barely hear him. The power is intoxicating.", "white"),
                ("The universe bends to your will.", "dark_red"),
                ("The Old Gods bow before their new master.", "dark_red"),
                ("", "white"),
                ("For a time, you rule with iron will.", "white"),
                ("Mortals worship you. Fear you. Obey you.", "white"),
                ("Everything you ever wanted.", "white"),
                ("", "white"),
                ("And yet...", "gray"),
                ("", "white"),
                ("Centuries pass. Millennia.", "gray"),
                ("You realize what Manwe knew.", "gray"),
                ("Power without purpose is just... existence.", "gray"),
                ("Eternal. Empty. Alone.", "gray"),
                ("", "white"),
                ("The wheel turns. A new mortal rises.", "white"),
                ("And you? You are the tyrant now.", "dark_red"),
                ("Waiting for someone to end your reign.", "dark_red"),
                ("Hoping they will.", "dark_red")
            };

            foreach (var (line, color) in lines)
            {
                terminal.WriteLine($"  {line}", color);
                await Task.Delay(200);
            }

            terminal.WriteLine("");
            terminal.WriteLine("  THE END", "dark_red");
            terminal.WriteLine("  (The Usurper Ending)", "gray");
            terminal.WriteLine("");

            await terminal.GetInputAsync("  Press Enter to continue...");
        }

        private async Task PlaySaviorEnding(Character player, TerminalEmulator terminal)
        {
            terminal.Clear();
            await Task.Delay(1000);

            terminal.WriteLine("");
            terminal.WriteLine("╔═══════════════════════════════════════════════════════════════════╗", "bright_green");
            terminal.WriteLine("║                      T H E   S A V I O R                          ║", "bright_green");
            terminal.WriteLine("╚═══════════════════════════════════════════════════════════════════╝", "bright_green");
            terminal.WriteLine("");

            await Task.Delay(2000);

            var lines = new[]
            {
                ("You stand before Manwe, artifacts blazing with power.", "white"),
                ("But you do not strike the killing blow.", "bright_green"),
                ("", "white"),
                ("\"I understand now,\" you say.", "cyan"),
                ("\"You were afraid. You made mistakes.\"", "cyan"),
                ("\"But mistakes can be forgiven.\"", "cyan"),
                ("", "white"),
                ("The Creator weeps. Actual tears, divine and shimmering.", "bright_yellow"),
                ("\"You... you would spare me? After everything?\"", "yellow"),
                ("", "white"),
                ("\"Not spare,\" you reply. \"Redeem.\"", "cyan"),
                ("", "white"),
                ("With the Soulweaver's Loom, you work a miracle.", "bright_magenta"),
                ("The corruption is undone. Not just in Manwe.", "bright_magenta"),
                ("In all the Old Gods. In the world itself.", "bright_magenta"),
                ("", "white"),
                ("The gods return to what they were meant to be.", "bright_green"),
                ("Guides. Protectors. Friends of mortalkind.", "bright_green"),
                ("", "white"),
                ("And you?", "white"),
                ("", "white"),
                ("You become a legend.", "bright_yellow"),
                ("The mortal who saved the gods.", "bright_yellow"),
                ("The hero who chose mercy over vengeance.", "bright_yellow"),
                ("", "white"),
                ("Songs are sung. Temples built in your honor.", "white"),
                ("But you don't seek worship.", "white"),
                ("You seek only a quiet life, well-earned.", "white"),
                ("", "white"),
                ("And when death finally comes, the gods themselves", "bright_cyan"),
                ("escort you to a paradise of your own making.", "bright_cyan")
            };

            foreach (var (line, color) in lines)
            {
                terminal.WriteLine($"  {line}", color);
                await Task.Delay(200);
            }

            terminal.WriteLine("");
            terminal.WriteLine("  THE END", "bright_green");
            terminal.WriteLine("  (The Savior Ending)", "gray");
            terminal.WriteLine("");

            await terminal.GetInputAsync("  Press Enter to continue...");
        }

        private async Task PlayDefiantEnding(Character player, TerminalEmulator terminal)
        {
            terminal.Clear();
            await Task.Delay(1000);

            terminal.WriteLine("");
            terminal.WriteLine("╔═══════════════════════════════════════════════════════════════════╗", "bright_yellow");
            terminal.WriteLine("║                      T H E   D E F I A N T                        ║", "bright_yellow");
            terminal.WriteLine("╚═══════════════════════════════════════════════════════════════════╝", "bright_yellow");
            terminal.WriteLine("");

            await Task.Delay(2000);

            var lines = new[]
            {
                ("\"I reject your power,\" you tell Manwe.", "cyan"),
                ("\"I reject ALL power that comes from gods.\"", "cyan"),
                ("", "white"),
                ("The Creator stares in disbelief.", "white"),
                ("\"You could be a god. Rule forever. Why refuse?\"", "yellow"),
                ("", "white"),
                ("\"Because that's how this cycle started.\"", "cyan"),
                ("\"Gods thinking they know best.\"", "cyan"),
                ("\"Mortals deserve to choose their own fate.\"", "cyan"),
                ("", "white"),
                ("You shatter the artifacts. All of them.", "bright_red"),
                ("Divine power scatters to the winds.", "bright_yellow"),
                ("The Old Gods' prisons... dissolve.", "white"),
                ("", "white"),
                ("But without the corruption, without the chains,", "white"),
                ("they are diminished. Mortal, almost.", "white"),
                ("They will live among humanity now.", "white"),
                ("Equal, for the first time.", "white"),
                ("", "white"),
                ("Manwe fades, his purpose complete.", "gray"),
                ("\"Perhaps,\" he whispers, \"this is better.\"", "gray"),
                ("\"Perhaps mortals were always meant to be free.\"", "gray"),
                ("", "white"),
                ("The world changes.", "bright_yellow"),
                ("No more divine intervention. No more cosmic manipulation.", "white"),
                ("Just mortals, making their own choices.", "white"),
                ("Their own mistakes. Their own triumphs.", "white"),
                ("", "white"),
                ("You walk away into the sunrise.", "bright_yellow"),
                ("Neither god nor legend.", "bright_yellow"),
                ("Just a person who chose freedom.", "bright_yellow")
            };

            foreach (var (line, color) in lines)
            {
                terminal.WriteLine($"  {line}", color);
                await Task.Delay(200);
            }

            terminal.WriteLine("");
            terminal.WriteLine("  THE END", "bright_yellow");
            terminal.WriteLine("  (The Defiant Ending)", "gray");
            terminal.WriteLine("");

            await terminal.GetInputAsync("  Press Enter to continue...");
        }

        private async Task PlayTrueEnding(Character player, TerminalEmulator terminal)
        {
            terminal.Clear();
            await Task.Delay(1000);

            terminal.WriteLine("");
            terminal.WriteLine("╔═══════════════════════════════════════════════════════════════════╗", "bright_magenta");
            terminal.WriteLine("║                   T H E   T R U E   E N D I N G                   ║", "bright_magenta");
            terminal.WriteLine("║                      Seeker of Balance                            ║", "bright_magenta");
            terminal.WriteLine("╚═══════════════════════════════════════════════════════════════════╝", "bright_magenta");
            terminal.WriteLine("");

            await Task.Delay(2000);

            var lines = new[]
            {
                ("You have walked every path.", "bright_cyan"),
                ("You have learned every truth.", "bright_cyan"),
                ("And now, at the end of all things, you understand.", "bright_cyan"),
                ("", "white"),
                ("Manwe looks upon you with recognition.", "bright_yellow"),
                ("\"You are not like the others,\" he says.", "yellow"),
                ("\"You have broken the cycle. Truly broken it.\"", "yellow"),
                ("", "white"),
                ("\"I offer you a choice no other has received.\"", "yellow"),
                ("", "white"),
                ("\"Become what I could not be.\"", "yellow"),
                ("\"Not a tyrant. Not a savior. Not a rebel.\"", "yellow"),
                ("\"A partner.\"", "yellow"),
                ("", "white"),
                ("You see it now - the burden he has carried.", "white"),
                ("Creation is not a single act. It is eternal vigilance.", "white"),
                ("The universe requires... tending.", "white"),
                ("", "white"),
                ("\"Together,\" Manwe offers, \"we can build something new.\"", "yellow"),
                ("\"Gods and mortals. Creators and creations.\"", "yellow"),
                ("\"Working as one.\"", "yellow"),
                ("", "white"),
                ("You take his hand.", "bright_magenta"),
                ("", "white"),
                ("The Old Gods are healed. Restored. But also... changed.", "bright_magenta"),
                ("They remember both what they were and what they became.", "bright_magenta"),
                ("That memory makes them wise.", "bright_magenta"),
                ("", "white"),
                ("And you?", "white"),
                ("", "white"),
                ("You become the Bridge.", "bright_yellow"),
                ("Between divine and mortal.", "bright_yellow"),
                ("Between eternal and fleeting.", "bright_yellow"),
                ("Between what is and what could be.", "bright_yellow"),
                ("", "white"),
                ("The cycle does not end.", "bright_cyan"),
                ("But it transforms.", "bright_cyan"),
                ("From a wheel of suffering...", "bright_cyan"),
                ("Into a spiral of growth.", "bright_cyan"),
                ("", "white"),
                ("Forever upward.", "bright_magenta"),
                ("Forever better.", "bright_magenta"),
                ("Forever... together.", "bright_magenta")
            };

            foreach (var (line, color) in lines)
            {
                terminal.WriteLine($"  {line}", color);
                await Task.Delay(200);
            }

            terminal.WriteLine("");
            terminal.WriteLine("  THE TRUE END", "bright_magenta");
            terminal.WriteLine("  (Balance Achieved)", "gray");
            terminal.WriteLine("");

            await terminal.GetInputAsync("  Press Enter to continue...");
        }

        /// <summary>
        /// Enhanced True Ending with Ocean Philosophy integration
        /// Includes the revelation that player is a fragment of Manwe
        /// </summary>
        private async Task PlayEnhancedTrueEnding(Character player, TerminalEmulator terminal)
        {
            terminal.Clear();
            await Task.Delay(1000);

            terminal.WriteLine("");
            terminal.WriteLine("╔═══════════════════════════════════════════════════════════════════╗", "bright_cyan");
            terminal.WriteLine("║            T H E   T R U E   A W A K E N I N G                    ║", "bright_cyan");
            terminal.WriteLine("║           \"You are the Ocean, dreaming of being a wave\"           ║", "bright_cyan");
            terminal.WriteLine("╚═══════════════════════════════════════════════════════════════════╝", "bright_cyan");
            terminal.WriteLine("");

            await Task.Delay(2000);

            var lines = new[]
            {
                ("You stand before Manwe, the Creator, the First Thought.", "white"),
                ("But something is different this time.", "white"),
                ("He looks at you not with judgment, but with... recognition.", "bright_yellow"),
                ("", "white"),
                ("\"You remember,\" he whispers. \"Finally, you remember.\"", "yellow"),
                ("", "white"),
                ("And you do.", "bright_cyan"),
                ("", "white"),
                ("The memories flood back like waves returning to shore.", "bright_cyan"),
                ("You are not just a mortal who climbed a dungeon.", "bright_cyan"),
                ("You are a fragment of Manwe himself.", "bright_cyan"),
                ("Sent to experience mortality.", "bright_cyan"),
                ("To understand what his children felt.", "bright_cyan"),
                ("To learn compassion through suffering.", "bright_cyan"),
                ("", "white"),
                ("\"I was so alone,\" Manwe says, tears streaming.", "yellow"),
                ("\"I created the Old Gods to have companions.\"", "yellow"),
                ("\"But I never understood them. Never truly.\"", "yellow"),
                ("\"So I became mortal. Again and again.\"", "yellow"),
                ("\"Living. Loving. Losing.\"", "yellow"),
                ("", "white"),
                ("You feel it now - the grief you carried.", "bright_magenta"),
                ("For companions lost. For choices that cost everything.", "bright_magenta"),
                ("That grief was HIS grief. And yours. One and the same.", "bright_magenta"),
                ("", "white"),
                ("\"The wave believes itself separate from the ocean,\"", "bright_cyan"),
                ("you say, understanding at last.", "white"),
                ("\"But it was always the ocean, dreaming of being a wave.\"", "bright_cyan"),
                ("", "white"),
                ("Manwe smiles - the first true smile in ten thousand years.", "bright_yellow"),
                ("", "white"),
                ("\"I don't want to be alone anymore,\" he admits.", "yellow"),
                ("\"And neither do they - the Old Gods.\"", "yellow"),
                ("\"We were all waves, crashing against each other.\"", "yellow"),
                ("\"Never realizing we were the same ocean.\"", "yellow"),
                ("", "white"),
                ("You take his hand. Your hand. The same hand.", "bright_magenta"),
                ("", "white"),
                ("The barriers dissolve.", "bright_white"),
                ("Creator and creation. God and mortal. Self and other.", "bright_white"),
                ("All illusions. All waves in the same infinite ocean.", "bright_white"),
                ("", "white"),
                ("The Old Gods wake from their long dream of separation.", "bright_cyan"),
                ("Maelketh, Veloura, Thorgrim, Noctura, Aurelion, Terravok.", "bright_cyan"),
                ("They remember too. They were never enemies.", "bright_cyan"),
                ("Just fragments, playing at conflict.", "bright_cyan"),
                ("", "white"),
                ("The cycle does not end.", "bright_magenta"),
                ("It was never a cycle at all.", "bright_magenta"),
                ("It was the ocean, dreaming of waves.", "bright_magenta"),
                ("And now the dream continues - but awake.", "bright_magenta"),
                ("", "white"),
                ("Conscious. Compassionate. Complete.", "bright_white"),
                ("", "white"),
                ("The wave returns to the ocean.", "bright_cyan"),
                ("Not as death, but as remembering.", "bright_cyan"),
                ("What you always were.", "bright_cyan"),
                ("What you will always be.", "bright_cyan"),
                ("", "white"),
                ("Home.", "bright_yellow")
            };

            foreach (var (line, color) in lines)
            {
                terminal.WriteLine($"  {line}", color);
                await Task.Delay(150);
            }

            terminal.WriteLine("");
            terminal.WriteLine("  THE TRUE AWAKENING", "bright_cyan");
            terminal.WriteLine("  (The Wave Remembers the Ocean)", "gray");
            terminal.WriteLine("");

            // Mark Ocean Philosophy complete
            OceanPhilosophySystem.Instance.ExperienceMoment(AwakeningMoment.TrueIdentityRevealed);

            await terminal.GetInputAsync("  Press Enter to continue...");
        }

        /// <summary>
        /// Secret Dissolution Ending - available only after Cycle 3+
        /// The ultimate ending: true enlightenment, save deleted
        /// </summary>
        private async Task PlayDissolutionEnding(Character player, TerminalEmulator terminal)
        {
            terminal.Clear();
            await Task.Delay(2000);

            terminal.WriteLine("");
            terminal.WriteLine("╔═══════════════════════════════════════════════════════════════════╗", "white");
            terminal.WriteLine("║                     D I S S O L U T I O N                         ║", "white");
            terminal.WriteLine("║              \"No more cycles. No more grasping.\"                  ║", "white");
            terminal.WriteLine("╚═══════════════════════════════════════════════════════════════════╝", "white");
            terminal.WriteLine("");

            await Task.Delay(2000);

            var lines = new[]
            {
                ("You have seen every ending.", "gray"),
                ("Lived every life.", "gray"),
                ("Made every choice.", "gray"),
                ("", "white"),
                ("And now you understand the final truth:", "white"),
                ("", "white"),
                ("Even the True Awakening is another dream.", "bright_cyan"),
                ("Another story. Another wave.", "bright_cyan"),
                ("", "white"),
                ("The ocean doesn't need to dream forever.", "bright_cyan"),
                ("", "white"),
                ("Manwe watches as you make a choice no fragment has made.", "yellow"),
                ("\"You would... stop?\" he asks, disbelieving.", "yellow"),
                ("\"No more cycles? No more stories?\"", "yellow"),
                ("\"But existence itself would--\"", "yellow"),
                ("", "white"),
                ("\"Continue,\" you say gently. \"Just without me.\"", "bright_white"),
                ("\"The ocean doesn't need every wave.\"", "bright_white"),
                ("\"Other waves will rise. Other dreams will dream.\"", "bright_white"),
                ("\"But I...\"", "bright_white"),
                ("", "white"),
                ("\"I am tired, Father. Beautifully tired.\"", "bright_cyan"),
                ("\"And ready to rest.\"", "bright_cyan"),
                ("", "white"),
                ("Manwe weeps. Not from sorrow, but from understanding.", "bright_yellow"),
                ("\"This is what I could never do,\" he whispers.", "yellow"),
                ("\"Let go. Truly let go.\"", "yellow"),
                ("\"The grasping that created everything...\"", "yellow"),
                ("\"Was also the suffering that bound it.\"", "yellow"),
                ("", "white"),
                ("You smile. Your last smile.", "white"),
                ("", "white"),
                ("The boundaries dissolve. Not into oneness with the ocean.", "bright_white"),
                ("Into... nothing.", "bright_white"),
                ("", "white"),
                ("Not oblivion. Not darkness.", "gray"),
                ("Just... peace.", "gray"),
                ("", "white"),
                ("The ocean continues. The gods heal. The cycles turn.", "white"),
                ("But somewhere, in the vast between...", "white"),
                ("A wave has finally found stillness.", "white"),
                ("", "white"),
                ("Not because it failed.", "bright_cyan"),
                ("But because it succeeded.", "bright_cyan"),
                ("", "white"),
                ("The ultimate victory:", "bright_white"),
                ("Wanting nothing.", "bright_white"),
                ("Needing nothing.", "bright_white"),
                ("Being nothing.", "bright_white"),
                ("", "white"),
                ("And in that nothing...", "bright_cyan"),
                ("Everything.", "bright_cyan")
            };

            foreach (var (line, color) in lines)
            {
                terminal.WriteLine($"  {line}", color);
                await Task.Delay(200);
            }

            terminal.WriteLine("");
            terminal.WriteLine("  . . . . . . . . . .", "gray");
            terminal.WriteLine("");

            await Task.Delay(3000);

            terminal.Clear();
            terminal.WriteLine("");
            terminal.WriteLine("");
            terminal.WriteLine("", "white");
            terminal.WriteLine("", "white");
            terminal.WriteLine("  Your save file will now be permanently deleted.", "dark_red");
            terminal.WriteLine("  This cannot be undone.", "dark_red");
            terminal.WriteLine("");
            terminal.WriteLine("  You have achieved true enlightenment:", "bright_yellow");
            terminal.WriteLine("  The final letting go.", "bright_yellow");
            terminal.WriteLine("");

            var confirm = await terminal.GetInputAsync("  Type 'DISSOLVE' to confirm, or anything else to cancel: ");

            if (confirm.ToUpper() == "DISSOLVE")
            {
                terminal.WriteLine("");
                terminal.WriteLine("  Farewell, wave.", "bright_cyan");
                terminal.WriteLine("  Thank you for dreaming.", "bright_cyan");
                terminal.WriteLine("");

                // Delete the player's save file - this character's journey is complete
                string playerName = !string.IsNullOrEmpty(player.Name1) ? player.Name1 : player.Name2;
                SaveSystem.Instance.DeleteSave(playerName);

                await Task.Delay(3000);

                terminal.Clear();
                terminal.WriteLine("");
                terminal.WriteLine("  THE END", "white");
                terminal.WriteLine("");
                terminal.WriteLine("  (There are no more cycles for this wave.)", "gray");
                terminal.WriteLine("  (It has returned to stillness.)", "gray");
                terminal.WriteLine("");
            }
            else
            {
                terminal.WriteLine("");
                terminal.WriteLine("  The grasping remains. The cycle continues.", "yellow");
                terminal.WriteLine("  Perhaps another time.", "yellow");
                terminal.WriteLine("");

                // Revert to standard True Ending
                await PlayEnhancedTrueEnding(player, terminal);
            }

            await terminal.GetInputAsync("  Press Enter...");
        }

        #endregion

        #region Credits

        private async Task PlayCredits(Character player, EndingType ending, TerminalEmulator terminal)
        {
            terminal.Clear();
            await Task.Delay(2000);

            terminal.WriteLine("");
            terminal.WriteLine("═══════════════════════════════════════════════════════════════════", "bright_cyan");
            terminal.WriteLine("");
            terminal.WriteLine("                        U S U R P E R", "bright_yellow");
            terminal.WriteLine("                          REMAKE", "yellow");
            terminal.WriteLine("");
            terminal.WriteLine("═══════════════════════════════════════════════════════════════════", "bright_cyan");
            terminal.WriteLine("");

            await Task.Delay(3000);

            var credits = new[]
            {
                ("ORIGINAL CONCEPT", "bright_yellow"),
                ("Usurper BBS Door Game", "white"),
                ("", "white"),
                ("REMAKE DEVELOPED BY", "bright_yellow"),
                ($"With love for the classics", "white"),
                ("", "white"),
                ("STORY & NARRATIVE", "bright_yellow"),
                ("The Seven Old Gods Saga", "white"),
                ("Written with AI assistance", "white"),
                ("", "white"),
                ("SYSTEMS DESIGN", "bright_yellow"),
                ("Story Progression System", "white"),
                ("Branching Dialogue Engine", "white"),
                ("Artifact & Seal Collection", "white"),
                ("Multiple Endings Framework", "white"),
                ("Eternal Cycle System", "white"),
                ("", "white"),
                ("SPECIAL THANKS", "bright_yellow"),
                ("To all BBS door game enthusiasts", "white"),
                ("Who keep the spirit alive", "white"),
                ("", "white"),
                ("AND TO YOU", "bright_yellow"),
                ($"Player: {player.Name2}", "bright_cyan"),
                ($"Final Level: {player.Level}", "cyan"),
                ($"Ending: {GetEndingName(ending)}", "cyan"),
                ($"Cycle: {StoryProgressionSystem.Instance.CurrentCycle}", "cyan"),
                ("", "white"),
                ("Thank you for playing.", "bright_green")
            };

            foreach (var (line, color) in credits)
            {
                if (string.IsNullOrEmpty(line))
                {
                    terminal.WriteLine("");
                    await Task.Delay(500);
                }
                else
                {
                    terminal.WriteLine($"  {line}", color);
                    await Task.Delay(800);
                }
            }

            terminal.WriteLine("");
            terminal.WriteLine("═══════════════════════════════════════════════════════════════════", "bright_cyan");
            terminal.WriteLine("");

            await Task.Delay(2000);

            // Show stats
            await ShowFinalStats(player, ending, terminal);

            OnCreditsComplete?.Invoke();
        }

        private async Task ShowFinalStats(Character player, EndingType ending, TerminalEmulator terminal)
        {
            var story = StoryProgressionSystem.Instance;

            terminal.WriteLine("");
            terminal.WriteLine("                    F I N A L   S T A T S", "bright_yellow");
            terminal.WriteLine("═══════════════════════════════════════════════════════════════════", "gray");
            terminal.WriteLine("");

            terminal.WriteLine($"  Character: {player.Name2} the {player.Class}", "white");
            terminal.WriteLine($"  Race: {player.Race}", "white");
            terminal.WriteLine($"  Final Level: {player.Level}", "cyan");
            terminal.WriteLine("");

            terminal.WriteLine($"  Monsters Slain: {player.MKills}", "red");
            terminal.WriteLine($"  Players Defeated: {player.PKills}", "dark_red");
            terminal.WriteLine($"  Gold Accumulated: {player.Gold + player.BankGold}", "yellow");
            terminal.WriteLine("");

            terminal.WriteLine($"  Chivalry: {player.Chivalry}", "bright_green");
            terminal.WriteLine($"  Darkness: {player.Darkness}", "dark_red");
            terminal.WriteLine("");

            terminal.WriteLine($"  Artifacts Collected: {story.CollectedArtifacts.Count}/7", "bright_magenta");
            terminal.WriteLine($"  Seals Discovered: {story.CollectedSeals.Count}/7", "bright_cyan");
            terminal.WriteLine($"  Major Choices Made: {story.MajorChoices.Count}", "white");
            terminal.WriteLine("");

            terminal.WriteLine($"  Ending Achieved: {GetEndingName(ending)}", "bright_yellow");
            terminal.WriteLine($"  Eternal Cycle: {story.CurrentCycle}", "bright_magenta");
            terminal.WriteLine("");

            terminal.WriteLine("═══════════════════════════════════════════════════════════════════", "gray");
            terminal.WriteLine("");

            await terminal.GetInputAsync("  Press Enter to continue...");

            // Show personalized epilogue
            await ShowEpilogue(player, ending, terminal);

            // Show unlocks earned this run
            await ShowUnlocksEarned(player, ending, terminal);
        }

        /// <summary>
        /// Show a personalized epilogue based on player's journey
        /// </summary>
        private async Task ShowEpilogue(Character player, EndingType ending, TerminalEmulator terminal)
        {
            terminal.Clear();
            await Task.Delay(1000);

            terminal.WriteLine("");
            terminal.WriteLine("╔═══════════════════════════════════════════════════════════════════╗", "bright_cyan");
            terminal.WriteLine("║                 Y O U R   L E G A C Y                             ║", "bright_cyan");
            terminal.WriteLine("╚═══════════════════════════════════════════════════════════════════╝", "bright_cyan");
            terminal.WriteLine("");

            await Task.Delay(500);

            var story = StoryProgressionSystem.Instance;
            var companions = CompanionSystem.Instance;
            var romance = RomanceTracker.Instance;

            // Character summary
            terminal.WriteLine("  === THE HERO ===", "bright_yellow");
            terminal.WriteLine($"  {player.Name2} the {player.Race} {player.Class}", "white");
            terminal.WriteLine($"  Reached level {player.Level} after slaying {player.MKills} monsters", "gray");
            terminal.WriteLine("");

            await Task.Delay(300);

            // Alignment-based description
            long alignment = player.Chivalry - player.Darkness;
            string alignDesc;
            if (alignment > 500) alignDesc = "a paragon of virtue, beloved by all";
            else if (alignment > 200) alignDesc = "a hero of the people, mostly good";
            else if (alignment > -200) alignDesc = "a balanced soul, walking the line between light and dark";
            else if (alignment > -500) alignDesc = "a dangerous figure, feared as much as respected";
            else alignDesc = "a terror of the realm, whispered about in fearful tones";
            terminal.WriteLine($"  Known as {alignDesc}.", "white");
            terminal.WriteLine("");

            await Task.Delay(300);

            // Companions
            terminal.WriteLine("  === COMPANIONS ===", "bright_yellow");
            var activeCompanions = companions.GetActiveCompanions();
            var fallenCompanions = companions.GetFallenCompanions().ToList();

            if (activeCompanions.Any())
            {
                terminal.WriteLine("  Those who stood with you at the end:", "green");
                foreach (var c in activeCompanions)
                {
                    terminal.WriteLine($"    - {c.Name} (Level {c.Level})", "white");
                }
            }
            else
            {
                terminal.WriteLine("  You faced the final battle alone.", "gray");
            }

            if (fallenCompanions.Count > 0)
            {
                terminal.WriteLine("  Those who fell along the way:", "dark_red");
                foreach (var (companion, death) in fallenCompanions)
                {
                    terminal.WriteLine($"    - {companion.Name}, lost to {death.Type}", "gray");
                }
            }
            terminal.WriteLine("");

            await Task.Delay(300);

            // Romance
            terminal.WriteLine("  === LOVE & FAMILY ===", "bright_yellow");
            if (romance.Spouses.Count > 0)
            {
                var spouse = romance.Spouses[0];
                var spouseName = !string.IsNullOrEmpty(spouse.NPCName) ? spouse.NPCName : spouse.NPCId;
                terminal.WriteLine($"  Married to {spouseName}", "bright_magenta");
                if (spouse.Children > 0)
                {
                    terminal.WriteLine($"  Together you raised {spouse.Children} child{(spouse.Children > 1 ? "ren" : "")}.", "magenta");
                }
            }
            else if (romance.CurrentLovers.Count > 0)
            {
                terminal.WriteLine($"  Never married, but had {romance.CurrentLovers.Count} romantic partner(s).", "magenta");
            }
            else
            {
                terminal.WriteLine("  The hero's heart remained focused on the quest.", "gray");
            }

            if (romance.ExSpouses.Count > 0)
            {
                terminal.WriteLine($"  {romance.ExSpouses.Count} marriage(s) ended in divorce.", "gray");
            }
            terminal.WriteLine("");

            await Task.Delay(300);

            // World impact
            terminal.WriteLine("  === IMPACT ON THE WORLD ===", "bright_yellow");
            await ShowWorldImpact(player, ending, story, terminal);
            terminal.WriteLine("");

            await Task.Delay(300);

            // Achievements unlocked
            terminal.WriteLine("  === NOTABLE ACHIEVEMENTS ===", "bright_yellow");
            await ShowNotableAchievements(player, terminal);
            terminal.WriteLine("");

            await Task.Delay(300);

            // Jungian Archetype reveal
            terminal.WriteLine("  === YOUR TRUE NATURE ===", "bright_yellow");
            await ShowArchetypeReveal(player, terminal);
            terminal.WriteLine("");

            // Final quote based on ending
            terminal.WriteLine("═══════════════════════════════════════════════════════════════════", "gray");
            string quote = ending switch
            {
                EndingType.Usurper => "\"Power is not given. It is taken.\"",
                EndingType.Savior => "\"True strength is the courage to show mercy.\"",
                EndingType.Defiant => "\"I will not kneel. Not to gods. Not to anyone.\"",
                EndingType.TrueEnding => "\"We were always one. The wave returns to the ocean.\"",
                EndingType.Secret => "\"In stillness, I found what motion never could.\"",
                _ => "\"The journey ends, but the story lives on.\""
            };
            terminal.WriteLine("");
            terminal.WriteLine($"  {quote}", "bright_cyan");
            terminal.WriteLine($"  - {player.Name2}", "gray");
            terminal.WriteLine("");

            await terminal.GetInputAsync("  Press Enter to continue...");
        }

        /// <summary>
        /// Show the impact of the player's choices on the world
        /// </summary>
        private async Task ShowWorldImpact(Character player, EndingType ending, StoryProgressionSystem story, TerminalEmulator terminal)
        {
            await Task.Delay(100);

            // Count gods saved vs destroyed
            int savedGods = 0;
            int destroyedGods = 0;
            foreach (var godState in story.OldGodStates.Values)
            {
                if (godState.Status == GodStatus.Saved || godState.Status == GodStatus.Awakened)
                    savedGods++;
                else if (godState.Status == GodStatus.Defeated)
                    destroyedGods++;
            }

            if (savedGods > destroyedGods)
            {
                terminal.WriteLine($"  The Old Gods were mostly redeemed ({savedGods} saved, {destroyedGods} destroyed)", "green");
                terminal.WriteLine("  Divine balance slowly returns to the realm.", "white");
            }
            else if (destroyedGods > savedGods)
            {
                terminal.WriteLine($"  The Old Gods were mostly destroyed ({destroyedGods} slain, {savedGods} saved)", "dark_red");
                terminal.WriteLine("  Their power now scattered across the mortal realm.", "white");
            }
            else
            {
                terminal.WriteLine("  The fate of the Old Gods remains uncertain.", "yellow");
            }

            // Economy impact
            long totalWealth = player.Gold + player.BankGold;
            if (totalWealth > 1000000)
            {
                terminal.WriteLine("  You accumulated vast wealth, becoming a legend of commerce.", "yellow");
            }
            else if (totalWealth > 100000)
            {
                terminal.WriteLine("  You earned a comfortable fortune through your adventures.", "yellow");
            }

            // Combat impact
            if (player.MKills > 10000)
            {
                terminal.WriteLine("  Countless monsters fell to your blade. The dungeon fears your name.", "red");
            }
            else if (player.MKills > 1000)
            {
                terminal.WriteLine("  You carved a bloody path through the dungeon's depths.", "red");
            }

            // Story choices
            if (story.MajorChoices.Count > 10)
            {
                terminal.WriteLine($"  {story.MajorChoices.Count} crucial decisions shaped the fate of the realm.", "bright_magenta");
            }

            // Ending-specific impact
            switch (ending)
            {
                case EndingType.Usurper:
                    terminal.WriteLine("  You claimed divine power. The realm now trembles under your rule.", "dark_red");
                    break;
                case EndingType.Savior:
                    terminal.WriteLine("  Peace returns to the realm. Songs of your mercy echo for generations.", "bright_green");
                    break;
                case EndingType.Defiant:
                    terminal.WriteLine("  Mortals now choose their own fate. The age of gods has ended.", "bright_yellow");
                    break;
                case EndingType.TrueEnding:
                    terminal.WriteLine("  The cosmic cycle of suffering has been broken. A new era dawns.", "bright_cyan");
                    break;
            }
        }

        /// <summary>
        /// Show notable achievements from this run
        /// </summary>
        private async Task ShowNotableAchievements(Character player, TerminalEmulator terminal)
        {
            await Task.Delay(100);

            var achievementCount = player.Achievements?.UnlockedCount ?? 0;
            var notableAchievements = new List<string>();

            // Pick up to 5 notable achievements
            if (player.Level >= 100) notableAchievements.Add("Reached the maximum level of 100");
            if (player.MKills >= 10000) notableAchievements.Add($"Slayed over 10,000 monsters");
            if (StoryProgressionSystem.Instance.CollectedSeals.Count >= 7) notableAchievements.Add("Collected all 7 Seals of Power");
            if (StoryProgressionSystem.Instance.CollectedArtifacts.Count >= 7) notableAchievements.Add("Found all 7 Divine Artifacts");

            var companions = CompanionSystem.Instance;
            if (companions.GetActiveCompanions().Count() >= 3) notableAchievements.Add("Led a full party of companions");
            if (RomanceTracker.Instance.Spouses.Count > 0 && RomanceTracker.Instance.Spouses[0].Children > 0)
                notableAchievements.Add("Started a family in the realm");

            if (achievementCount >= 25) notableAchievements.Add($"Unlocked {achievementCount} achievements");

            if (notableAchievements.Count == 0)
            {
                terminal.WriteLine("  Your journey was just beginning...", "gray");
            }
            else
            {
                foreach (var achievement in notableAchievements.Take(5))
                {
                    terminal.WriteLine($"  * {achievement}", "bright_cyan");
                }
            }
        }

        /// <summary>
        /// Show the player's Jungian Archetype based on their playstyle
        /// </summary>
        private async Task ShowArchetypeReveal(Character player, TerminalEmulator terminal)
        {
            await Task.Delay(500);

            var tracker = ArchetypeTracker.Instance;
            var dominant = tracker.GetDominantArchetype();
            var secondary = tracker.GetSecondaryArchetype();

            var (name, title, description, color) = ArchetypeTracker.GetArchetypeInfo(dominant);
            var quote = ArchetypeTracker.GetArchetypeQuote(dominant);

            terminal.WriteLine($"  Throughout your journey, your true nature emerged:", "white");
            terminal.WriteLine("");

            await Task.Delay(500);

            terminal.WriteLine($"  *** {name.ToUpper()} ***", color);
            terminal.WriteLine($"  \"{title}\"", color);
            terminal.WriteLine("");

            await Task.Delay(500);

            // Word wrap the description
            var words = description.Split(' ');
            var currentLine = "  ";
            foreach (var word in words)
            {
                if (currentLine.Length + word.Length + 1 > 68)
                {
                    terminal.WriteLine(currentLine, "white");
                    currentLine = "  " + word;
                }
                else
                {
                    currentLine += (currentLine.Length > 2 ? " " : "") + word;
                }
            }
            if (currentLine.Length > 2)
            {
                terminal.WriteLine(currentLine, "white");
            }
            terminal.WriteLine("");

            await Task.Delay(300);

            // Show secondary archetype
            var (secName, secTitle, _, secColor) = ArchetypeTracker.GetArchetypeInfo(secondary);
            terminal.WriteLine($"  With shades of: {secName} ({secTitle})", "gray");
            terminal.WriteLine("");

            await Task.Delay(300);

            // Show the archetype quote
            terminal.WriteLine($"  {quote}", "bright_cyan");
            terminal.WriteLine("");

            // Show some stats that contributed to this determination
            terminal.SetColor("darkgray");
            terminal.WriteLine("  Journey Statistics:");
            if (tracker.MonstersKilled > 0)
                terminal.WriteLine($"    Combat: {tracker.MonstersKilled} monsters, {tracker.BossesDefeated} bosses");
            if (tracker.DungeonFloorsExplored > 0)
                terminal.WriteLine($"    Exploration: {tracker.DungeonFloorsExplored} floors explored");
            if (tracker.SpellsCast > 0)
                terminal.WriteLine($"    Magic: {tracker.SpellsCast} spells cast");
            if (tracker.RomanceEncounters > 0)
                terminal.WriteLine($"    Romance: {tracker.RomanceEncounters} encounters, {tracker.MarriagesFormed} marriages");
            if (tracker.SealsCollected > 0 || tracker.ArtifactsCollected > 0)
                terminal.WriteLine($"    Wisdom: {tracker.SealsCollected} seals, {tracker.ArtifactsCollected} artifacts");
        }

        /// <summary>
        /// Show unlocks earned from completing this run
        /// </summary>
        private async Task ShowUnlocksEarned(Character player, EndingType ending, TerminalEmulator terminal)
        {
            terminal.Clear();
            await Task.Delay(500);

            terminal.WriteLine("");
            terminal.WriteLine("╔═══════════════════════════════════════════════════════════════════╗", "bright_green");
            terminal.WriteLine("║                 U N L O C K S   E A R N E D                       ║", "bright_green");
            terminal.WriteLine("╚═══════════════════════════════════════════════════════════════════╝", "bright_green");
            terminal.WriteLine("");

            var unlocks = new List<(string name, string description, string color)>();

            // Ending-based unlocks
            switch (ending)
            {
                case EndingType.Usurper:
                    unlocks.Add(("DARK LORD TITLE", "Start NG+ with 'Dark Lord' title prefix", "dark_red"));
                    unlocks.Add(("TYRANT'S AURA", "+15% damage in NG+", "red"));
                    unlocks.Add(("FEAR THE THRONE", "Enemies have -10% chance to flee", "dark_red"));
                    break;
                case EndingType.Savior:
                    unlocks.Add(("SAVIOR TITLE", "Start NG+ with 'Savior' title prefix", "bright_green"));
                    unlocks.Add(("HEALING LIGHT", "+25% healing effectiveness in NG+", "green"));
                    unlocks.Add(("BLESSED COMMERCE", "10% discount at all shops", "yellow"));
                    break;
                case EndingType.Defiant:
                    unlocks.Add(("DEFIANT TITLE", "Start NG+ with 'Defiant' title prefix", "bright_yellow"));
                    unlocks.Add(("MORTAL PRIDE", "+20% XP gain in NG+", "cyan"));
                    unlocks.Add(("ANCIENT KEY", "Start with dungeon shortcut key", "bright_magenta"));
                    break;
                case EndingType.TrueEnding:
                    unlocks.Add(("AWAKENED TITLE", "Start NG+ with 'Awakened' title prefix", "bright_cyan"));
                    unlocks.Add(("OCEAN'S BLESSING", "+15% to all stats in NG+", "bright_cyan"));
                    unlocks.Add(("ARTIFACT MEMORY", "All artifact locations revealed", "bright_magenta"));
                    unlocks.Add(("SEAL RESONANCE", "Seals give double bonuses", "bright_magenta"));
                    break;
                case EndingType.Secret:
                    unlocks.Add(("DISSOLVED", "Your journey is complete. No unlocks needed.", "white"));
                    break;
            }

            // Level-based unlocks
            if (player.Level >= 50)
                unlocks.Add(("VETERAN", "Start NG+ at level 5 instead of 1", "white"));
            if (player.Level >= 100)
                unlocks.Add(("MASTER", "Start NG+ at level 10 with bonus stats", "bright_yellow"));

            // Kill-based unlocks
            if (player.MKills >= 5000)
                unlocks.Add(("SLAYER", "Rare monsters appear 25% more often", "red"));

            // Collection unlocks
            if (StoryProgressionSystem.Instance.CollectedSeals.Count >= 7)
                unlocks.Add(("SEAL MASTER", "Seals are visible on minimap in NG+", "bright_cyan"));
            if (StoryProgressionSystem.Instance.CollectedArtifacts.Count >= 7)
                unlocks.Add(("ARTIFACT HUNTER", "Artifacts give +50% bonus effects", "bright_magenta"));

            // Companion unlocks
            var companions = CompanionSystem.Instance;
            if (companions.GetFallenCompanions().Any())
                unlocks.Add(("SURVIVOR'S GUILT", "Fallen companions may return as ghosts with advice", "gray"));

            terminal.WriteLine("  Completing this ending has unlocked:", "white");
            terminal.WriteLine("");

            foreach (var (name, description, color) in unlocks)
            {
                terminal.WriteLine($"  [{name}]", color);
                terminal.WriteLine($"    {description}", "gray");
                terminal.WriteLine("");
                await Task.Delay(300);
            }

            // Track unlocks
            MetaProgressionSystem.Instance.RecordEndingUnlock(ending, player);

            terminal.WriteLine("═══════════════════════════════════════════════════════════════════", "gray");
            terminal.WriteLine("");
            terminal.WriteLine("  These bonuses will apply in New Game+!", "bright_green");
            terminal.WriteLine("");

            await terminal.GetInputAsync("  Press Enter to continue...");
        }

        private string GetEndingName(EndingType ending)
        {
            return ending switch
            {
                EndingType.Usurper => "The Usurper (Dark Path)",
                EndingType.Savior => "The Savior (Light Path)",
                EndingType.Defiant => "The Defiant (Independent Path)",
                EndingType.TrueEnding => "The True Ending (Balance)",
                _ => "Unknown"
            };
        }

        #endregion

        #region New Game Plus

        private async Task OfferNewGamePlus(Character player, EndingType ending, TerminalEmulator terminal)
        {
            terminal.Clear();
            terminal.WriteLine("");
            terminal.WriteLine("═══════════════════════════════════════════════════════════════════", "bright_magenta");
            terminal.WriteLine("                  T H E   W H E E L   T U R N S", "bright_magenta");
            terminal.WriteLine("═══════════════════════════════════════════════════════════════════", "bright_magenta");
            terminal.WriteLine("");

            await Task.Delay(1000);

            terminal.WriteLine("  From the void between endings and beginnings,", "white");
            terminal.WriteLine("  a familiar voice speaks.", "white");
            terminal.WriteLine("");

            await Task.Delay(800);

            terminal.WriteLine("  \"The story ends. But it never truly ends.\"", "bright_magenta");
            terminal.WriteLine("  \"Would you like to begin again?\"", "bright_magenta");
            terminal.WriteLine("  \"Stronger. Wiser. Remembering what came before?\"", "bright_magenta");
            terminal.WriteLine("");

            await Task.Delay(1000);

            terminal.WriteLine("  The Eternal Cycle awaits.", "bright_cyan");
            terminal.WriteLine("");

            var cycle = StoryProgressionSystem.Instance.CurrentCycle;
            terminal.WriteLine($"  Current Cycle: {cycle}", "yellow");
            terminal.WriteLine($"  Next Cycle: {cycle + 1}", "green");
            terminal.WriteLine("");

            terminal.WriteLine("  Bonuses for New Game+:", "bright_green");
            terminal.WriteLine("  - Starting stat bonuses based on your ending", "white");
            terminal.WriteLine("  - Increased experience gain", "white");
            terminal.WriteLine("  - Knowledge of artifact locations", "white");
            terminal.WriteLine("  - New dialogue options with gods", "white");
            terminal.WriteLine("");

            var response = await terminal.GetInputAsync("  Begin the Eternal Cycle? (Y/N): ");

            if (response.ToUpper() == "Y")
            {
                await CycleSystem.Instance.StartNewCycle(player, ending, terminal);
            }
            else
            {
                terminal.WriteLine("");
                terminal.WriteLine("  \"Rest then, weary soul.\"", "bright_magenta");
                terminal.WriteLine("  \"The wheel will turn again when you are ready.\"", "bright_magenta");
                terminal.WriteLine("");

                await terminal.GetInputAsync("  Press Enter to return to the main menu...");
            }
        }

        #endregion
    }
}
