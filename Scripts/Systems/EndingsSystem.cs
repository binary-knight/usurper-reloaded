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

            // Check for True Ending first (requires specific conditions)
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
                    await PlayTrueEnding(player, terminal);
                    break;
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
