using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace UsurperConsole
{
    /// <summary>
    /// Console implementation of terminal functionality
    /// Replaces Godot UI components for command-line gaming
    /// </summary>
    public class ConsoleTerminal
    {
        private ConsoleColor currentColor = ConsoleColor.White;

        /// <summary>
        /// Convert string color names to ConsoleColor enum
        /// </summary>
        private ConsoleColor ColorNameToConsole(string colorName)
        {
            return colorName?.ToLower() switch
            {
                "black" => ConsoleColor.Black,
                "darkred" => ConsoleColor.DarkRed,
                "darkgreen" => ConsoleColor.DarkGreen,
                "darkyellow" => ConsoleColor.DarkYellow,
                "darkblue" => ConsoleColor.DarkBlue,
                "darkmagenta" => ConsoleColor.DarkMagenta,
                "darkcyan" => ConsoleColor.DarkCyan,
                "gray" => ConsoleColor.Gray,
                "darkgray" => ConsoleColor.DarkGray,
                "red" => ConsoleColor.Red,
                "green" => ConsoleColor.Green,
                "yellow" => ConsoleColor.Yellow,
                "blue" => ConsoleColor.Blue,
                "magenta" => ConsoleColor.Magenta,
                "cyan" => ConsoleColor.Cyan,
                "white" => ConsoleColor.White,
                "bright_white" => ConsoleColor.White,
                "bright_red" => ConsoleColor.Red,
                "bright_green" => ConsoleColor.Green,
                "bright_yellow" => ConsoleColor.Yellow,
                "bright_blue" => ConsoleColor.Blue,
                "bright_magenta" => ConsoleColor.Magenta,
                "bright_cyan" => ConsoleColor.Cyan,
                "brown" => ConsoleColor.DarkYellow,
                _ => ConsoleColor.White
            };
        }

        public void WriteLine(string text)
        {
            // Check for inline color markup tags like [red]text[/]
            if (text != null && text.Contains("[") && text.Contains("[/]"))
            {
                WriteMarkup(text);
                Console.WriteLine();
            }
            else
            {
                Console.WriteLine(text);
            }
        }

        /// <summary>
        /// Parse and render text with inline color markup tags like [red]text[/]
        /// </summary>
        public void WriteMarkup(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            var oldColor = Console.ForegroundColor;

            // Pattern matches [color]content[/] or plain text
            // Supports nested: [red]some [yellow]yellow[/] text[/]
            var pattern = @"\[([a-z_]+)\](.*?)\[/\]";
            var lastIndex = 0;

            // Use a simple state-based parser for proper nesting support
            int i = 0;
            while (i < text.Length)
            {
                // Look for opening tag
                if (text[i] == '[' && i + 1 < text.Length && text[i + 1] != '/')
                {
                    // Find the closing bracket
                    var closeIndex = text.IndexOf(']', i);
                    if (closeIndex > i)
                    {
                        var colorName = text.Substring(i + 1, closeIndex - i - 1);
                        var color = ColorNameToConsole(colorName);

                        // Find matching [/]
                        var endTagIndex = FindMatchingEndTag(text, closeIndex + 1);
                        if (endTagIndex > closeIndex)
                        {
                            // Extract content between tags
                            var content = text.Substring(closeIndex + 1, endTagIndex - closeIndex - 1);

                            // Recursively process content (for nested tags)
                            Console.ForegroundColor = color;
                            WriteMarkup(content);
                            Console.ForegroundColor = oldColor;

                            // Move past the [/]
                            i = endTagIndex + 3;
                            continue;
                        }
                    }
                }

                // Regular character, just print it
                Console.Write(text[i]);
                i++;
            }

            Console.ForegroundColor = oldColor;
        }

        /// <summary>
        /// Find the matching [/] end tag, handling nesting
        /// </summary>
        private int FindMatchingEndTag(string text, int startIndex)
        {
            int depth = 1;
            int i = startIndex;

            while (i < text.Length && depth > 0)
            {
                if (text[i] == '[')
                {
                    // Check for end tag [/]
                    if (i + 2 < text.Length && text[i + 1] == '/' && text[i + 2] == ']')
                    {
                        depth--;
                        if (depth == 0)
                        {
                            return i;
                        }
                        i += 3;
                        continue;
                    }
                    // Check for opening tag [something]
                    var closeIndex = text.IndexOf(']', i);
                    if (closeIndex > i && text[i + 1] != '/')
                    {
                        depth++;
                        i = closeIndex + 1;
                        continue;
                    }
                }
                i++;
            }

            return -1;
        }

        public void WriteLine(string text, ConsoleColor color)
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ForegroundColor = oldColor;
        }

        public void WriteLine(string text, string color)
        {
            WriteLine(text, ColorNameToConsole(color));
        }

        public void Write(string text)
        {
            // Check for inline color markup tags like [red]text[/]
            if (text != null && text.Contains("[") && text.Contains("[/]"))
            {
                WriteMarkup(text);
            }
            else
            {
                Console.Write(text);
            }
        }

        public void Write(string text, ConsoleColor color)
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(text);
            Console.ForegroundColor = oldColor;
        }

        public void Write(string text, string color)
        {
            Write(text, ColorNameToConsole(color));
        }

        public void SetColor(ConsoleColor color)
        {
            Console.ForegroundColor = color;
            currentColor = color;
        }

        public void SetColor(string color)
        {
            SetColor(ColorNameToConsole(color));
        }
        
        public async Task<string> GetInput(string prompt = "")
        {
            if (!string.IsNullOrEmpty(prompt))
            {
                Console.Write(prompt);
            }
            return Console.ReadLine() ?? "";
        }
        
        public async Task<string> GetStringInput(string prompt = "")
        {
            return await GetInput(prompt);
        }
        
        public async Task<string> GetKeyInput(string prompt = "")
        {
            if (!string.IsNullOrEmpty(prompt))
            {
                Console.Write(prompt);
            }
            return Console.ReadKey().KeyChar.ToString();
        }
        
        public async Task WaitForKeyPress(string message = "Press any key to continue...")
        {
            Console.WriteLine(message);
            Console.ReadKey();
        }
        
        public async Task PressAnyKey(string message = "Press any key to continue...")
        {
            await WaitForKeyPress(message);
        }
        
        public void Clear()
        {
            Console.Clear();
        }

        public void ClearScreen()
        {
            Console.Clear();
        }

        public async Task WaitForKey()
        {
            Console.ReadKey(true);
            await Task.CompletedTask;
        }

        public async Task WaitForKey(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                Console.WriteLine(message);
            }
            Console.ReadKey(true);
            await Task.CompletedTask;
        }

        public void DisplayFile(string filename)
        {
            try
            {
                if (File.Exists(filename))
                {
                    var content = File.ReadAllText(filename);
                    Console.WriteLine(content);
                }
                else
                {
                    Console.WriteLine($"File not found: {filename}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading file {filename}: {ex.Message}");
            }
        }
    }
} 