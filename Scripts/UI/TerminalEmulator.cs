using UsurperRemake.Utils;
using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using System.Linq;

public partial class TerminalEmulator : Control
{
    private const int COLUMNS = 80;
    private const int ROWS = 25;

    private RichTextLabel display;
    private LineEdit inputLine;
    private Font dosFont;

    private Queue<string> outputBuffer = new Queue<string>();
    private TaskCompletionSource<string> inputAwaiter;
    private int cursorX = 0, cursorY = 0;
    private string currentColor = "white";
    
    // Static instance property for compatibility
    public static TerminalEmulator Instance { get; private set; }
    
    // ANSI color mappings
    private readonly Dictionary<string, Color> ansiColors = new Dictionary<string, Color>
    {
        { "black", Color.Black },
        { "darkred", Color.FromHtml("#800000") },
        { "darkgreen", Color.FromHtml("#008000") },
        { "darkyellow", Color.FromHtml("#808000") },
        { "darkblue", Color.FromHtml("#000080") },
        { "darkmagenta", Color.FromHtml("#800080") },
        { "darkcyan", Color.FromHtml("#008080") },
        { "gray", Color.FromHtml("#C0C0C0") },
        { "darkgray", Color.FromHtml("#808080") },
        { "red", Color.Red },
        { "green", Color.Green },
        { "yellow", Color.Yellow },
        { "blue", Color.Blue },
        { "magenta", Color.Magenta },
        { "cyan", Color.Cyan },
        { "white", Color.White },
        { "bright_white", Color.White },
        { "bright_red", Color.FromHtml("#FF6060") },
        { "bright_green", Color.FromHtml("#60FF60") },
        { "bright_yellow", Color.FromHtml("#FFFF60") },
        { "bright_blue", Color.FromHtml("#6060FF") },
        { "bright_magenta", Color.FromHtml("#FF60FF") },
        { "bright_cyan", Color.FromHtml("#60FFFF") }
    };
    
    public override void _Ready()
    {
        Instance = this; // Set static instance for compatibility
        SetupDisplay();
        SetupInput();
    }
    
    private void SetupDisplay()
    {
        display = new RichTextLabel();
        display.SetAnchorsAndOffsetsPreset(Control.Preset.FullRect);
        display.BbcodeEnabled = true;
        display.ScrollFollowing = true;
        display.FitContent = true;
        
        // Try to load DOS font, fall back to monospace
        if (Godot.FileAccess.FileExists("res://Assets/Fonts/perfect_dos_vga_437.ttf"))
        {
            dosFont = GD.Load<Font>("res://Assets/Fonts/perfect_dos_vga_437.ttf");
        }
        else
        {
            // Use system monospace font as fallback
            dosFont = ThemeDB.FallbackFont;
        }
        
        display.AddThemeFontOverride("normal_font", dosFont);
        display.AddThemeFontSizeOverride("normal_font_size", 16);
        
        // Set background to black
        var styleBox = new StyleBoxFlat();
        styleBox.BgColor = Color.Black;
        display.AddThemeStyleboxOverride("normal", styleBox);
        
        // Set default text color
        display.AddThemeColorOverride("default_color", ansiColors["bright_white"]);
        
        AddChild(display);
    }
    
    private void SetupInput()
    {
        inputLine = new LineEdit();
        inputLine.Visible = false;
        inputLine.TextSubmitted += OnInputSubmitted;
        inputLine.AddThemeFontOverride("font", dosFont);
        inputLine.AddThemeFontSizeOverride("font_size", 16);
        AddChild(inputLine);
    }
    
    public void WriteLine(string text, string color = "white")
    {
        if (display != null)
        {
            // Convert simplified [colorname]text[/] format to BBCode [color=colorname]text[/color]
            string processedText = ConvertSimplifiedColorToBBCode(text);
            string formattedText = $"[color={color}]{processedText}[/color]";
            display.Text += formattedText + "\n";
        }
        else
        {
            Console.WriteLine(text);
        }
    }

    /// <summary>
    /// Convert simplified [colorname]text[/] format to Godot BBCode [color=colorname]text[/color]
    /// This allows combat messages to use simple color codes that work in both Godot and console modes
    /// </summary>
    private string ConvertSimplifiedColorToBBCode(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        // Replace [colorname] with [color=colorname]
        text = System.Text.RegularExpressions.Regex.Replace(
            text,
            @"\[([a-z_]+)\]",
            "[color=$1]"
        );

        // Replace [/] with [/color]
        text = text.Replace("[/]", "[/color]");

        return text;
    }
    
    // Overload for cases with no text parameter
    public void WriteLine()
    {
        WriteLine("", "white");
    }
    
    // Overload for single string parameter  
    public void WriteLine(string text)
    {
        WriteLine(text, "white");
    }
    
    public void Write(string text, string color = null)
    {
        // Use current color if no color specified
        string effectiveColor = color ?? currentColor;

        // If the Godot RichTextLabel is available use it, otherwise fall back to plain Console output
        if (display != null)
        {
        var colorCode = ansiColors.ContainsKey(effectiveColor) ?
            ansiColors[effectiveColor].ToHtml() : ansiColors["white"].ToHtml();

        display.AppendText($"[color=#{colorCode}]{text}[/color]");
        }
        else
        {
            // Console fallback – approximate ANSI colour mapping
            ConsoleColor oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ColorNameToConsole(effectiveColor);
            Console.Write(text);
            Console.ForegroundColor = oldColor;
        }

        cursorX += text.Length;
    }
    
    private ConsoleColor ColorNameToConsole(string colorName)
    {
        // Basic map – expand as needed
        return colorName.ToLower() switch
        {
            "black" => ConsoleColor.Black,
            "darkred" or "red" or "bright_red" => ConsoleColor.Red,
            "darkgreen" or "green" or "bright_green" => ConsoleColor.Green,
            "darkyellow" or "yellow" or "bright_yellow" => ConsoleColor.Yellow,
            "darkblue" or "blue" or "bright_blue" => ConsoleColor.Blue,
            "darkmagenta" or "magenta" or "bright_magenta" => ConsoleColor.Magenta,
            "darkcyan" or "cyan" or "bright_cyan" => ConsoleColor.Cyan,
            "gray" or "bright_white" or "white" => ConsoleColor.White,
            "darkgray" => ConsoleColor.DarkGray,
            _ => ConsoleColor.White
        };
    }
    
    public void ClearScreen()
    {
        if (display != null)
    {
        display.Clear();
        }
        else
        {
            Console.Clear();
        }
        cursorX = 0;
        cursorY = 0;
    }
    
    public void SetCursorPosition(int x, int y)
    {
        cursorX = x;
        cursorY = y;
        // Note: In a full ANSI implementation, this would move cursor
        // For now, we'll just track position for box drawing
    }
    
    public async Task<string> GetInput(string prompt = "> ")
    {
        Write(prompt, "bright_white");
        
        // If running inside Godot (display created) use the LineEdit, otherwise Console.ReadLine
        if (inputLine != null && display != null)
        {
        inputLine.Clear();
        inputLine.Visible = true;
        inputLine.GrabFocus();
        
        inputAwaiter = new TaskCompletionSource<string>();
        var result = await inputAwaiter.Task;
        
        inputLine.Visible = false;
        WriteLine(result, "cyan");
            return result;
        }
        else
        {
            var result = Console.ReadLine() ?? string.Empty;
            WriteLine(result, "cyan");
        return result;
        }
    }
    
    private void OnInputSubmitted(string text)
    {
        inputAwaiter?.SetResult(text);
    }
    
    public async Task<int> GetMenuChoice(List<MenuOption> options)
    {
        WriteLine("");
        for (int i = 0; i < options.Count; i++)
        {
            WriteLine($"{i + 1}. {options[i].Text}", "yellow");
        }
        WriteLine("0. Go back", "gray");
        WriteLine("");
        
        while (true)
        {
            var input = await GetInput("Your choice: ");
            if (int.TryParse(input, out int choice))
            {
                if (choice == 0) return -1;
                if (choice > 0 && choice <= options.Count)
                    return choice - 1;
            }
            
            WriteLine("Invalid choice!", "red");
        }
    }
    
    public void ShowASCIIArt(string artName)
    {
        var artPath = $"res://Assets/ASCII/{artName}.ans";
        if (Godot.FileAccess.FileExists(artPath))
        {
            var file = Godot.FileAccess.Open(artPath, Godot.FileAccess.ModeFlags.Read);
            var content = file.GetAsText();
            file.Close();
            
            // Parse ANSI art and display
            DisplayANSI(content);
        }
        else
        {
            // Show ASCII title as fallback
            ShowUsurperTitle();
        }
    }
    
    private void ShowUsurperTitle()
    {
        WriteLine("", "white");
        WriteLine("╔══════════════════════════════════════════════════════════════════════════════╗", "bright_blue");
        WriteLine("║                                                                              ║", "bright_blue");
        WriteLine("║  ██╗   ██╗███████╗██╗   ██╗██████╗ ██████╗ ███████╗██████╗                ║", "bright_red");
        WriteLine("║  ██║   ██║██╔════╝██║   ██║██╔══██╗██╔══██╗██╔════╝██╔══██╗               ║", "bright_red");
        WriteLine("║  ██║   ██║███████╗██║   ██║██████╔╝██████╔╝█████╗  ██████╔╝               ║", "bright_red");
        WriteLine("║  ██║   ██║╚════██║██║   ██║██╔══██╗██╔═══╝ ██╔══╝  ██╔══██╗               ║", "bright_red");
        WriteLine("║  ╚██████╔╝███████║╚██████╔╝██║  ██║██║     ███████╗██║  ██║               ║", "bright_red");
        WriteLine("║   ╚═════╝ ╚══════╝ ╚═════╝ ╚═╝  ╚═╝╚═╝     ╚══════╝╚═╝  ╚═╝               ║", "bright_red");
        WriteLine("║                                                                              ║", "bright_blue");
        WriteLine("║                         ██████╗ ███████╗██████╗  ██████╗ ██████╗ ███╗   ██╗║", "bright_yellow");
        WriteLine("║                         ██╔══██╗██╔════╝██╔══██╗██╔═══██╗██╔══██╗████╗  ██║║", "bright_yellow");
        WriteLine("║                         ██████╔╝█████╗  ██████╔╝██║   ██║██████╔╝██╔██╗ ██║║", "bright_yellow");
        WriteLine("║                         ██╔══██╗██╔══╝  ██╔══██╗██║   ██║██╔══██╗██║╚██╗██║║", "bright_yellow");
        WriteLine("║                         ██║  ██║███████╗██████╔╝╚██████╔╝██║  ██║██║ ╚████║║", "bright_yellow");
        WriteLine("║                         ╚═╝  ╚═╝╚══════╝╚═════╝  ╚═════╝ ╚═╝  ╚═╝╚═╝  ╚═══╝║", "bright_yellow");
        WriteLine("║                                                                              ║", "bright_blue");
        WriteLine("║                          A Classic BBS Door Game Remake                     ║", "bright_cyan");
        WriteLine("║                              With Advanced NPC AI                           ║", "bright_cyan");
        WriteLine("║                                                                              ║", "bright_blue");
        WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝", "bright_blue");
        WriteLine("", "white");
    }
    
    private void DisplayANSI(string ansiContent)
    {
        // Simple ANSI parser - in a full implementation this would be much more complex
        var lines = ansiContent.Split('\n');
        foreach (var line in lines)
        {
            WriteLine(line.Replace("\r", ""), "white");
        }
    }
    
    public void DrawBox(int x, int y, int width, int height, string color = "white")
    {
        // Box drawing characters
        const string TL = "╔"; const string TR = "╗";
        const string BL = "╚"; const string BR = "╝";
        const string H = "═"; const string V = "║";
        
        var colorCode = ansiColors.ContainsKey(color) ? 
            ansiColors[color].ToHtml() : ansiColors["white"].ToHtml();
        
        // Draw top
        var topLine = TL + new string('═', width - 2) + TR;
        display.AppendText($"[color=#{colorCode}]{topLine}[/color]\n");
        
        // Draw sides
        for (int i = 1; i < height - 1; i++)
        {
            var middleLine = V + new string(' ', width - 2) + V;
            display.AppendText($"[color=#{colorCode}]{middleLine}[/color]\n");
        }
        
        // Draw bottom
        var bottomLine = BL + new string('═', width - 2) + BR;
        display.AppendText($"[color=#{colorCode}]{bottomLine}[/color]\n");
    }
    
    public void ShowStatusBar(string playerName, int level, int hp, int maxHp, int gold, int turns)
    {
        var statusText = $"Player: {playerName} | Level: {level} | HP: {hp}/{maxHp} | Gold: {gold} | Turns: {turns}";
        SetStatusLine(statusText);
    }
    
    /// <summary>
    /// Set status line for compatibility with GameEngine
    /// </summary>
    public void SetStatusLine(string statusText)
    {
        // For now, just display it as a regular line
        // In a full implementation, this would set a persistent status bar
        WriteLine($"[Status] {statusText}", "bright_cyan");
    }
    
    public async Task PressAnyKey(string message = "Press any key to continue...")
    {
        await GetInput(message);
    }
    
    // Missing API methods for compatibility
    public void SetColor(string color)
    {
        currentColor = color;
    }
    
    public async Task<string> GetKeyInput()
    {
        return await GetInput("");
    }
    
    public async Task<string> GetStringInput(string prompt = "")
    {
        return await GetInput(prompt);
    }
    
    public async Task WaitForKeyPress(string message = "Press any key to continue...")
    {
        await GetInput(message);
    }
    
    // Additional compatibility methods
    public void Clear() => ClearScreen();
    
    // Additional missing async methods
    public async Task<string> GetInputAsync(string prompt = "")
    {
        return await GetInput(prompt);
    }
    
    public async Task<string> ReadLineAsync()
    {
        return await GetInput("");
    }
    
    public async Task<string> ReadKeyAsync()
    {
        return await GetInput("");
    }
    
    // Additional missing async methods for API compatibility
    public async Task WriteLineAsync(string text = "")
    {
        WriteLine(text);
        await Task.CompletedTask;
    }
    
    public async Task WriteColorLineAsync(string text, string color)
    {
        WriteLine(text, color);
        await Task.CompletedTask;
    }
    
    public async Task WriteAsync(string text)
    {
        Write(text);
        await Task.CompletedTask;
    }
    
    public async Task WriteColorAsync(string text, string color)
    {
        Write(text, color);
        await Task.CompletedTask;
    }
    
    /// <summary>
    /// Get a single character of input from the user (first character typed).
    /// </summary>
    /// <remarks>
    /// Older Pascal code frequently worked with single-character commands.  Returning the first
    /// character of the line that the user enters gives the same behaviour while still allowing
    /// users to press <Enter> as usual.  If the user simply presses <Enter> we return a null-char
    /// (\0) so the caller can treat it as a cancel/no-input event.
    /// </remarks>
    public async Task<char> GetCharAsync()
    {
        var input = await GetKeyInput();
        return string.IsNullOrEmpty(input) ? '\0' : input[0];
    }
    
    /// <summary>
    /// Convenience alias – behaves exactly the same as <see cref="GetCharAsync"/> but has a more
    /// descriptive name when reading yes/no style keystrokes.
    /// </summary>
    public async Task<char> GetKeyCharAsync() => await GetCharAsync();
    
    // Missing methods that are being called throughout the codebase
    public async Task<bool> ConfirmAsync(string message = "Are you sure? (Y/N): ")
    {
        while (true)
        {
            WriteLine(message, "yellow");
            var input = await GetInput();
            var response = input.ToUpper().Trim();
            
            if (response == "Y" || response == "YES")
                return true;
            if (response == "N" || response == "NO")
                return false;
                
            WriteLine("Please answer Y or N.", "red");
        }
    }
    
    // Overload for ConfirmAsync that takes a boolean parameter
    public async Task<bool> ConfirmAsync(string message, bool defaultValue)
    {
        while (true)
        {
            string prompt = defaultValue ? $"{message} (Y/n): " : $"{message} (y/N): ";
            WriteLine(prompt, "yellow");
            var input = await GetInput();
            var response = input.ToUpper().Trim();
            
            if (string.IsNullOrEmpty(response))
                return defaultValue;
            
            if (response == "Y" || response == "YES")
                return true;
            if (response == "N" || response == "NO")
                return false;
                
            WriteLine("Please answer Y or N.", "red");
        }
    }
    
    public async Task<string> GetStringAsync(string prompt = "")
    {
        return await GetInput(prompt);
    }
    
    public async Task<int> GetNumberInput(string prompt = "", int min = 0, int max = int.MaxValue)
    {
        while (true)
        {
            var input = await GetInput(prompt);
            if (int.TryParse(input, out int result))
            {
                if (result >= min && result <= max)
                    return result;
                WriteLine($"Please enter a number between {min} and {max}.", "red");
            }
            else
            {
                WriteLine("Please enter a valid number.", "red");
            }
        }
    }
    
    /// <summary>
    /// Overload that omits the prompt string – maintains backwards-compatibility with legacy code that
    /// expected the Pascal signature GetNumberInput(min, max).
    /// </summary>
    public async Task<int> GetNumberInput(int min, int max)
    {
        return await GetNumberInput("", min, max);
    }
    
    // Add DisplayMessage method to handle ConsoleColor parameters
    public void DisplayMessage(string message, ConsoleColor color, bool newLine = true)
    {
        string colorName = color switch
        {
            ConsoleColor.Red => "red",
            ConsoleColor.Green => "green",
            ConsoleColor.Blue => "blue",
            ConsoleColor.Yellow => "yellow",
            ConsoleColor.Cyan => "cyan",
            ConsoleColor.Magenta => "magenta",
            ConsoleColor.White => "white",
            ConsoleColor.Gray => "gray",
            ConsoleColor.DarkGray => "darkgray",
            ConsoleColor.DarkRed => "darkred",
            ConsoleColor.DarkGreen => "darkgreen",
            ConsoleColor.DarkBlue => "darkblue",
            ConsoleColor.DarkYellow => "darkyellow",
            ConsoleColor.DarkCyan => "darkcyan",
            ConsoleColor.DarkMagenta => "darkmagenta",
            ConsoleColor.Black => "black",
            _ => "white"
        };
        
        if (newLine)
            WriteLine(message, colorName);
        else
            Write(message, colorName);
    }
    
    public void DisplayMessage(string message, string color, bool newLine = true)
    {
        if (newLine)
            WriteLine(message, color);
        else
            Write(message, color);
    }
    
    // Overload for DisplayMessage that takes 3 arguments with ConsoleColor
    public void DisplayMessage(string message, string color, ConsoleColor backgroundColor)
    {
        // For now, ignore the background color and just display the message
        WriteLine(message, color);
    }
    
    public void DisplayMessage(string message)
    {
        WriteLine(message, "white");
    }

    // Additional missing API methods for compatibility
    public string ReadLine()
    {
        // Synchronous version - not ideal but needed for compatibility
        return GetInput().Result;
    }

    public string ReadKey()
    {
        // Synchronous version - not ideal but needed for compatibility
        return GetKeyInput().Result;
    }

    public async Task ClearScreenAsync()
    {
        ClearScreen();
        await Task.CompletedTask;
    }

    public static string ColorWhite = "white";
    public static string ColorCyan = "cyan";
    public static string ColorGreen = "green";
    public static string ColorRed = "red";
    public static string ColorYellow = "yellow";
    public static string ColorBlue = "blue";
    public static string ColorMagenta = "magenta";
    public static string ColorDarkGray = "darkgray";
    
    // Missing methods causing CS1061 errors
    public void ShowANSIArt(string artName)
    {
        ShowASCIIArt(artName); // Delegate to existing method
    }
    
    public async Task WaitForKey()
    {
        await GetKeyInput();
    }
    
    public async Task WaitForKey(string message)
    {
        await PressAnyKey(message);
    }

    // Added overloads to accept ConsoleColor for legacy compatibility
    public void WriteLine(string text, ConsoleColor color, bool newLine = true)
    {
        var colorName = color.ToString().ToLower();
        if (newLine)
            WriteLine(text, colorName);
        else
            Write(text, colorName);
    }

    public void WriteLine(string text, ConsoleColor color)
    {
        WriteLine(text, color.ToString().ToLower());
    }

    public void Write(string text, ConsoleColor color)
    {
        Write(text, color.ToString().ToLower());
    }

    public void SetColor(ConsoleColor color)
    {
        SetColor(color.ToString().ToLower());
    }

    // Synchronous helper for legacy code paths
    public string GetInputSync(string prompt = "> ") => GetInput(prompt).GetAwaiter().GetResult();
} 
