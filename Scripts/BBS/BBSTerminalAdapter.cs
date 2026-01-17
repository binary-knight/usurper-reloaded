using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UsurperRemake.BBS
{
    /// <summary>
    /// Terminal implementation for BBS door mode that can work with socket or console I/O.
    /// This class provides the same interface as TerminalEmulator but routes through SocketTerminal.
    ///
    /// Note: Since TerminalEmulator extends Godot.Control, we can't directly inherit from it.
    /// Instead, this class provides the same public API and sets itself as TerminalEmulator.Instance
    /// using duck typing through the static instance pattern.
    /// </summary>
    public class BBSTerminalAdapter
    {
        private readonly SocketTerminal _socketTerminal;
        private readonly BBSSessionInfo _sessionInfo;
        private string _currentColor = "white";

        // Static instance for compatibility with code that uses TerminalEmulator.Instance
        public static BBSTerminalAdapter? Instance { get; private set; }

        public BBSTerminalAdapter(SocketTerminal socketTerminal)
        {
            _socketTerminal = socketTerminal;
            _sessionInfo = socketTerminal.SessionInfo;
            Instance = this;
        }

        public BBSSessionInfo SessionInfo => _sessionInfo;
        public bool IsConnected => _socketTerminal.IsConnected;

        #region Output Methods

        public void WriteLine(string text, string color = "white")
        {
            if (_sessionInfo.CommType == ConnectionType.Local)
            {
                // Use console for local mode
                var oldColor = Console.ForegroundColor;
                Console.ForegroundColor = ColorNameToConsole(color);

                // Handle inline color markup
                if (text.Contains("[") && text.Contains("[/]"))
                {
                    WriteMarkupToConsole(text);
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine(text);
                }
                Console.ForegroundColor = oldColor;
                return;
            }

            _socketTerminal.WriteLineAsync(text, color).GetAwaiter().GetResult();
        }

        public void WriteLine()
        {
            WriteLine("", "white");
        }

        public void Write(string text, string color = "white")
        {
            if (_sessionInfo.CommType == ConnectionType.Local)
            {
                var oldColor = Console.ForegroundColor;
                Console.ForegroundColor = ColorNameToConsole(color);

                if (text.Contains("[") && text.Contains("[/]"))
                {
                    WriteMarkupToConsole(text);
                }
                else
                {
                    Console.Write(text);
                }
                Console.ForegroundColor = oldColor;
                return;
            }

            _socketTerminal.WriteAsync(text, color).GetAwaiter().GetResult();
        }

        public void Write(string text)
        {
            Write(text, _currentColor);
        }

        public void SetColor(string color)
        {
            _currentColor = color ?? "white";

            if (_sessionInfo.CommType == ConnectionType.Local)
            {
                Console.ForegroundColor = ColorNameToConsole(_currentColor);
                return;
            }

            _socketTerminal.SetColorAsync(_currentColor).GetAwaiter().GetResult();
        }

        public void ClearScreen()
        {
            if (_sessionInfo.CommType == ConnectionType.Local)
            {
                Console.Clear();
                return;
            }

            _socketTerminal.ClearScreenAsync().GetAwaiter().GetResult();
        }

        #endregion

        #region Input Methods

        public async Task<string> GetInput(string prompt = "> ")
        {
            if (!string.IsNullOrEmpty(prompt))
            {
                Write(prompt, "bright_white");
            }

            if (_sessionInfo.CommType == ConnectionType.Local)
            {
                var result = Console.ReadLine() ?? "";
                return result;
            }

            return await _socketTerminal.GetInputAsync("");
        }

        public string GetInputSync(string prompt = "> ") => GetInput(prompt).GetAwaiter().GetResult();

        public async Task<string> GetKeyInput()
        {
            if (_sessionInfo.CommType == ConnectionType.Local)
            {
                var key = Console.ReadKey(true);
                return key.KeyChar.ToString();
            }

            return await _socketTerminal.GetKeyInputAsync("");
        }

        public async Task<int> GetMenuChoice(List<MenuOption> options)
        {
            WriteLine("");
            for (int i = 0; i < options.Count; i++)
            {
                var opt = options[i];
                SetColor("yellow");
                Write($"[{opt.Key}] ");
                SetColor(opt.Color ?? "white");
                WriteLine(opt.Text);
            }

            WriteLine("");

            while (true)
            {
                var input = await GetInput("> ");
                input = input.Trim().ToUpperInvariant();

                // Find matching option by key
                for (int i = 0; i < options.Count; i++)
                {
                    if (options[i].Key.ToUpperInvariant() == input)
                        return i;
                }

                // Try numeric input
                if (int.TryParse(input, out int num) && num >= 1 && num <= options.Count)
                    return num - 1;

                SetColor("red");
                WriteLine("Invalid choice. Please try again.");
            }
        }

        public async Task<bool> ConfirmAsync(string message, bool defaultValue = false)
        {
            string defaultHint = defaultValue ? " [Y/n]" : " [y/N]";
            SetColor("yellow");
            Write(message + defaultHint + " ");

            var input = await GetInput("");
            input = input.Trim().ToUpperInvariant();

            if (string.IsNullOrEmpty(input))
                return defaultValue;

            return input == "Y" || input == "YES";
        }

        public async Task<int> GetNumberInput(string prompt = "", int min = 0, int max = int.MaxValue)
        {
            while (true)
            {
                var input = await GetInput(prompt);

                if (int.TryParse(input.Trim(), out int num))
                {
                    if (num >= min && num <= max)
                        return num;

                    SetColor("red");
                    WriteLine($"Please enter a number between {min} and {max}.");
                }
                else
                {
                    SetColor("red");
                    WriteLine("Please enter a valid number.");
                }
            }
        }

        public async Task PressAnyKey(string message = "Press Enter to continue...")
        {
            SetColor("gray");
            WriteLine(message);
            await GetInput("");
        }

        public async Task WaitForKey(string message = "Press Enter to continue...")
        {
            await PressAnyKey(message);
        }

        #endregion

        #region Async Output Methods (for compatibility)

        public async Task WriteLineAsync(string text = "")
        {
            await _socketTerminal.WriteLineAsync(text);
        }

        public async Task WriteAsync(string text)
        {
            await _socketTerminal.WriteAsync(text);
        }

        public async Task ClearScreenAsync()
        {
            await _socketTerminal.ClearScreenAsync();
        }

        #endregion

        #region Markup and Color Helpers

        /// <summary>
        /// Write text with inline color markup to console
        /// </summary>
        private void WriteMarkupToConsole(string text)
        {
            var segments = ParseColorMarkup(text);
            var originalColor = Console.ForegroundColor;

            foreach (var (content, color) in segments)
            {
                if (!string.IsNullOrEmpty(color))
                    Console.ForegroundColor = ColorNameToConsole(color);
                else
                    Console.ForegroundColor = originalColor;

                Console.Write(content);
            }

            Console.ForegroundColor = originalColor;
        }

        /// <summary>
        /// Parse color markup like [red]text[/] into segments
        /// </summary>
        private List<(string content, string? color)> ParseColorMarkup(string text)
        {
            var result = new List<(string content, string? color)>();
            var currentContent = new System.Text.StringBuilder();
            string? currentColor = null;
            int i = 0;

            while (i < text.Length)
            {
                if (text[i] == '[')
                {
                    int end = text.IndexOf(']', i + 1);
                    if (end > i)
                    {
                        var tag = text.Substring(i + 1, end - i - 1).ToLowerInvariant();

                        if (tag == "/" || tag == "/color")
                        {
                            if (currentContent.Length > 0)
                            {
                                result.Add((currentContent.ToString(), currentColor));
                                currentContent.Clear();
                            }
                            currentColor = null;
                            i = end + 1;
                            continue;
                        }
                        else if (IsValidColor(tag))
                        {
                            if (currentContent.Length > 0)
                            {
                                result.Add((currentContent.ToString(), currentColor));
                                currentContent.Clear();
                            }
                            currentColor = tag;
                            i = end + 1;
                            continue;
                        }
                    }
                }

                currentContent.Append(text[i]);
                i++;
            }

            if (currentContent.Length > 0)
                result.Add((currentContent.ToString(), currentColor));

            return result;
        }

        private bool IsValidColor(string color)
        {
            return ColorNameToConsole(color) != ConsoleColor.White || color == "white" || color == "bright_white";
        }

        private ConsoleColor ColorNameToConsole(string colorName)
        {
            return colorName?.ToLower() switch
            {
                "black" => ConsoleColor.Black,
                "darkred" or "dark_red" => ConsoleColor.DarkRed,
                "darkgreen" or "dark_green" => ConsoleColor.DarkGreen,
                "darkyellow" or "dark_yellow" or "brown" => ConsoleColor.DarkYellow,
                "darkblue" or "dark_blue" => ConsoleColor.DarkBlue,
                "darkmagenta" or "dark_magenta" => ConsoleColor.DarkMagenta,
                "darkcyan" or "dark_cyan" => ConsoleColor.DarkCyan,
                "gray" or "grey" => ConsoleColor.Gray,
                "darkgray" or "dark_gray" => ConsoleColor.DarkGray,
                "red" or "bright_red" => ConsoleColor.Red,
                "green" or "bright_green" => ConsoleColor.Green,
                "yellow" or "bright_yellow" => ConsoleColor.Yellow,
                "blue" or "bright_blue" => ConsoleColor.Blue,
                "magenta" or "bright_magenta" => ConsoleColor.Magenta,
                "cyan" or "bright_cyan" => ConsoleColor.Cyan,
                "white" or "bright_white" => ConsoleColor.White,
                _ => ConsoleColor.White
            };
        }

        #endregion

        #region Display Methods (for compatibility)

        public void DisplayMessage(string message, string color = "white", bool newLine = true)
        {
            if (newLine)
                WriteLine(message, color);
            else
                Write(message, color);
        }

        public void ShowStatusBar(string playerName, int level, int hp, int maxHp, int gold, int turns)
        {
            SetColor("gray");
            WriteLine("────────────────────────────────────────────────────────");
            Write($" {playerName}", "bright_cyan");
            Write($" | Lv.{level}", "yellow");
            Write($" | HP: ", "gray");

            // Color HP based on percentage
            float hpPercent = maxHp > 0 ? (float)hp / maxHp : 0;
            string hpColor = hpPercent > 0.5f ? "green" : hpPercent > 0.25f ? "yellow" : "red";
            Write($"{hp}/{maxHp}", hpColor);

            Write($" | Gold: {gold}", "yellow");
            Write($" | Turns: {turns}", "cyan");
            WriteLine("");
            SetColor("gray");
            WriteLine("────────────────────────────────────────────────────────");
        }

        public void DrawBox(int x, int y, int width, int height, string color)
        {
            SetColor(color);

            // Top border
            Write("╔");
            for (int i = 0; i < width - 2; i++) Write("═");
            WriteLine("╗");

            // Sides
            for (int row = 0; row < height - 2; row++)
            {
                Write("║");
                for (int i = 0; i < width - 2; i++) Write(" ");
                WriteLine("║");
            }

            // Bottom border
            Write("╚");
            for (int i = 0; i < width - 2; i++) Write("═");
            WriteLine("╝");
        }

        #endregion
    }

    /// <summary>
    /// Menu option for GetMenuChoice
    /// </summary>
    public class MenuOption
    {
        public string Key { get; set; } = "";
        public string Text { get; set; } = "";
        public string? Color { get; set; }

        public MenuOption() { }

        public MenuOption(string key, string text, string? color = null)
        {
            Key = key;
            Text = text;
            Color = color;
        }
    }
}
