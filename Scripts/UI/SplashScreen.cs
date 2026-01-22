using System;
using System.IO;
using System.Threading.Tasks;

namespace UsurperRemake.UI
{
    /// <summary>
    /// Displays the USURPER REBORN ASCII art splash screen
    /// </summary>
    public static class SplashScreen
    {
        public static async Task Show(dynamic terminal)
        {
            terminal.ClearScreen();

            // Display the ASCII art title
            var lines = new[]
            {
                "████████████████████████████████████████████████████████████████████████████████",
                "████████████████████████████████████████████████████████████████████████████████",
                "████████████████████████████████████████████████████████████████████████████████",
                "███                                                                          ███",
                "███    ██    ██ ███████ ██    ██ ██████  ██████  ███████ ██████            ███",
                "███    ██    ██ ██      ██    ██ ██   ██ ██   ██ ██      ██   ██           ███",
                "███    ██    ██ ███████ ██    ██ ██████  ██████  █████   ██████            ███",
                "███    ██    ██      ██ ██    ██ ██   ██ ██      ██      ██   ██           ███",
                "███     ██████  ███████  ██████  ██   ██ ██      ███████ ██   ██           ███",
                "███                                                                          ███",
                "███           ██████  ███████ ██████   ██████  ██████  ███    ██           ███",
                "███           ██   ██ ██      ██   ██ ██    ██ ██   ██ ████   ██           ███",
                "███           ██████  █████   ██████  ██    ██ ██████  ██ ██  ██           ███",
                "███           ██   ██ ██      ██   ██ ██    ██ ██   ██ ██  ██ ██           ███",
                "███           ██   ██ ███████ ██████   ██████  ██   ██ ██   ████           ███",
                "███                                                                          ███",
                "████████████████████████████████████████████████████████████████████████████████",
                "███                                                                          ███",
                "███              A Classic BBS Door Game - Reimagined for 2025               ███",
                "███                                                                          ███",
                "███                    Based on the original by Jakob Dangarden             ███",
                "███                                                                          ███",
                "████████████████████████████████████████████████████████████████████████████████",
                "████████████████████████████████████████████████████████████████████████████████",
                "████████████████████████████████████████████████████████████████████████████████",
            };

            // Insert version line dynamically
            var versionLine = $"███                            Alpha v{GameConfig.Version.Replace("-alpha", "")}                             ███";
            var linesList = new System.Collections.Generic.List<string>(lines);
            linesList.Insert(linesList.Count - 3, versionLine);
            lines = linesList.ToArray();

            // Animated reveal with colors
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                // Color scheme based on line content
                if (line.Contains("USURPER") || line.Contains("REBORN"))
                {
                    terminal.SetColor("bright_red");
                }
                else if (line.Contains("BBS Door Game") || line.Contains("Jakob Dangarden"))
                {
                    terminal.SetColor("bright_yellow");
                }
                else if (line.Contains("Reimagined for 2025"))
                {
                    terminal.SetColor("bright_cyan");
                }
                else if (line.Contains("Alpha v"))
                {
                    terminal.SetColor("bright_green");
                }
                else if (line.Contains("█"))
                {
                    terminal.SetColor("red");
                }
                else
                {
                    terminal.SetColor("darkred");
                }

                terminal.WriteLine(line);

                // Small delay for dramatic effect on title lines
                if (i >= 4 && i <= 14)
                {
                    await Task.Delay(50);
                }
            }

            terminal.WriteLine("");
            terminal.WriteLine("");
            terminal.SetColor("bright_white");
            terminal.Write("                           Press Enter to begin...");
            terminal.SetColor("white");

            await terminal.WaitForKey("");
            terminal.ClearScreen();
        }
    }
}
