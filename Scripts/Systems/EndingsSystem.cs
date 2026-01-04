using System;
using System.Collections.Generic;
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
            var story = StoryProgressionSystem.Instance;
            var ocean = OceanPhilosophySystem.Instance;
            var amnesia = AmnesiaSystem.Instance;
            var companions = CompanionSystem.Instance;
            var grief = GriefSystem.Instance;

            // Check for Secret Ending (Dissolution) first - requires Cycle 3+
            if (story.CurrentCycle >= 3 && QualifiesForDissolutionEnding(player))
            {
                return EndingType.Secret;
            }

            // Check for Enhanced True Ending
            if (QualifiesForEnhancedTrueEnding(player))
            {
                return EndingType.TrueEnding;
            }

            // Fallback to legacy true ending check
            if (CycleSystem.Instance.QualifiesForTrueEnding(player))
            {
                return EndingType.TrueEnding;
            }

            // Calculate alignment
            long alignment = player.Chivalry - player.Darkness;

            // Count saved vs destroyed gods
            int savedGods = 0;
            int destroyedGods = 0;

            if (story.HasStoryFlag("veloura_saved")) savedGods++;
            if (story.HasStoryFlag("veloura_destroyed")) destroyedGods++;
            if (story.HasStoryFlag("aurelion_saved")) savedGods++;
            if (story.HasStoryFlag("aurelion_destroyed")) destroyedGods++;
            if (story.HasStoryFlag("terravok_awakened")) savedGods++;
            if (story.HasStoryFlag("terravok_destroyed")) destroyedGods++;
            if (story.HasStoryFlag("noctura_ally")) savedGods++;
            if (story.HasStoryFlag("noctura_destroyed")) destroyedGods++;

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
            var story = StoryProgressionSystem.Instance;
            var ocean = OceanPhilosophySystem.Instance;
            var companions = CompanionSystem.Instance;
            var grief = GriefSystem.Instance;

            // 1. All 7 seals collected
            if (story.CollectedSeals.Count < 7)
                return false;

            // 2. Awakening Level 7
            if (ocean.AwakeningLevel < 7)
                return false;

            // 3. Experienced companion loss
            if (!ocean.ExperiencedMoments.Contains(AwakeningMoment.FirstCompanionDeath) &&
                !grief.HasCompletedGriefCycle)
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
            var story = StoryProgressionSystem.Instance;
            var ocean = OceanPhilosophySystem.Instance;
            var amnesia = AmnesiaSystem.Instance;

            // Must have completed at least 2 other endings
            if (story.CompletedEndings.Count < 2)
                return false;

            // Must have max awakening
            if (ocean.AwakeningLevel < 7)
                return false;

            // Must have full memory recovery (know you are Fragment of Manwe)
            if (!amnesia.TruthRevealed)
                return false;

            // Must have all wave fragments
            if (ocean.CollectedFragments.Count < 7)
                return false;

            // Must have story flag for being ready
            return story.HasStoryFlag("ready_for_dissolution");
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
