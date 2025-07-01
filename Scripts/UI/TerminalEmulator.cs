using UsurperRemake.Utils;
using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;

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
    
    // ANSI color mappings
    private readonly Dictionary<string, Color> ansiColors = new Dictionary<string, Color>
    {
        ["black"] = new Color(0, 0, 0),
        ["red"] = new Color(0.67f, 0, 0),
        ["green"] = new Color(0, 0.67f, 0),
        ["yellow"] = new Color(0.67f, 0.67f, 0),
        ["blue"] = new Color(0, 0, 0.67f),
        ["magenta"] = new Color(0.67f, 0, 0.67f),
        ["cyan"] = new Color(0, 0.67f, 0.67f),
        ["white"] = new Color(0.67f, 0.67f, 0.67f),
        ["gray"] = new Color(0.33f, 0.33f, 0.33f),
        ["bright_red"] = new Color(1, 0, 0),
        ["bright_green"] = new Color(0, 1, 0),
        ["bright_yellow"] = new Color(1, 1, 0),
        ["bright_blue"] = new Color(0, 0, 1),
        ["bright_magenta"] = new Color(1, 0, 1),
        ["bright_cyan"] = new Color(0, 1, 1),
        ["bright_white"] = new Color(1, 1, 1)
    };
    
    public override void _Ready()
    {
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
        if (FileAccess.FileExists("res://Assets/Fonts/perfect_dos_vga_437.ttf"))
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
        var colorCode = ansiColors.ContainsKey(color) ? 
            ansiColors[color].ToHtml() : ansiColors["white"].ToHtml();
            
        display.AppendText($"[color=#{colorCode}]{text}[/color]\n");
        cursorY++;
        cursorX = 0;
    }
    
    public void Write(string text, string color = "white")
    {
        var colorCode = ansiColors.ContainsKey(color) ? 
            ansiColors[color].ToHtml() : ansiColors["white"].ToHtml();
            
        display.AppendText($"[color=#{colorCode}]{text}[/color]");
        cursorX += text.Length;
    }
    
    public void ClearScreen()
    {
        display.Clear();
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
        
        inputLine.Clear();
        inputLine.Visible = true;
        inputLine.GrabFocus();
        
        inputAwaiter = new TaskCompletionSource<string>();
        var result = await inputAwaiter.Task;
        
        inputLine.Visible = false;
        WriteLine(result, "cyan");
        
        return result;
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
        if (FileAccess.FileExists(artPath))
        {
            var file = FileAccess.Open(artPath, FileAccess.ModeFlags.Read);
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
        WriteLine("".PadRight(80, '═'), "blue");
        var statusText = $" {playerName} | Level {level} | HP: {hp}/{maxHp} | Gold: {gold} | Turns: {turns} ";
        var padding = (80 - statusText.Length) / 2;
        WriteLine(statusText.PadLeft(statusText.Length + padding).PadRight(80), "bright_white");
        WriteLine("".PadRight(80, '═'), "blue");
    }
    
    public async Task PressAnyKey(string message = "Press any key to continue...")
    {
        await GetInput(message);
    }
    
    // Missing API methods for compatibility
    public void SetColor(string color)
    {
        // For console output, we don't need to set a persistent color
        // Individual WriteLine/Write calls already handle color
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
} 
