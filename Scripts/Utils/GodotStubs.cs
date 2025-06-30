using UsurperRemake.Utils;
// Godot stub implementations for standalone .NET compilation
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Godot
{
    // Base Godot classes
    public abstract class GodotObject
    {
        public virtual void _Ready() { }
        public virtual void QueueFree() { }
    }

    public abstract class Node : GodotObject
    {
        public string Name { get; set; } = "";
        public virtual void AddChild(Node child) { }
        public virtual T GetNode<T>(string path) where T : Node => default(T)!;
        public virtual Node GetNode(string path) => default(Node)!;
        public virtual void RemoveChild(Node child) { }
        public virtual Node GetParent() => default(Node)!;
    }

    public abstract class Control : Node
    {
        public Vector2 Size { get; set; }
        public virtual void Show() { }
        public virtual void Hide() { }
        
        public static class Preset
        {
            public const int FULL_RECT = 15;
        }
    }

    public class RichTextLabel : Control
    {
        public string Text { get; set; } = "";
        public bool BbcodeEnabled { get; set; } = true;
        
        public void AppendText(string text) => Text += text;
        public void Clear() => Text = "";
        public void SetAnchorsAndOffsetsPreset(int preset) { }
        public bool ScrollFollowing { get; set; } = true;
        public bool FitContent { get; set; } = true;
        public void AddThemeFontOverride(string name, Font font) { }
        public void AddThemeFontSizeOverride(string name, int size) { }
        public void AddThemeStyleboxOverride(string name, StyleBox style) { }
        public void AddThemeColorOverride(string name, Color color) { }
    }

    public class LineEdit : Control
    {
        public string Text { get; set; } = "";
        public bool Visible { get; set; } = true;
        public event Action<string>? TextSubmitted;
        
        public void AddThemeFontOverride(string name, Font font) { }
        public void AddThemeFontSizeOverride(string name, int size) { }
    }

    public class Timer : Node
    {
        public double WaitTime { get; set; } = 1.0;
        public bool Autostart { get; set; } = false;
        public event Action? Timeout;
        
        public void Start() => Task.Delay(TimeSpan.FromSeconds(WaitTime)).ContinueWith(_ => Timeout?.Invoke());
        public void Stop() { }
    }

    public class Font : GodotObject
    {
        public int Size { get; set; }
        public string FontPath { get; set; } = "";
    }

    // Godot data types
    public struct Vector2
    {
        public float X, Y;
        public Vector2(float x, float y) { X = x; Y = y; }
        
        public static Vector2 Zero => new Vector2(0, 0);
    }

    public struct Color
    {
        public float R, G, B, A;
        public Color(float r, float g, float b, float a = 1f) { R = r; G = g; B = b; A = a; }
        
        public static Color White => new(1, 1, 1);
        public static Color Black => new(0, 0, 0);
        public static Color Red => new(1, 0, 0);
        public static Color Green => new(0, 1, 0);
        public static Color Blue => new(0, 0, 1);
        public static Color Yellow => new(1, 1, 0);
        public static Color Cyan => new(0, 1, 1);
        public static Color Magenta => new(1, 0, 1);
        
        public string ToHtml(bool withAlpha = true) => 
            withAlpha ? $"#{(int)(R*255):X2}{(int)(G*255):X2}{(int)(B*255):X2}{(int)(A*255):X2}"
                      : $"#{(int)(R*255):X2}{(int)(G*255):X2}{(int)(B*255):X2}";
    }

    // Godot utility functions
    public static class GD
    {
        private static Random random = new();
        
        public static void Print(params object[] args)
        {
            Console.WriteLine(string.Join(" ", args));
        }
        
        public static int RandRange(int min, int max)
        {
            return random.Next(min, max + 1);
        }
        
        public static float RandRange(float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }
        
        public static T Load<T>(string path) where T : class => default(T)!;
        public static GodotObject Load(string path) => default(GodotObject)!;
        
        public static int Randi() => random.Next();
        public static float Randf() => (float)random.NextDouble();
        public static int RandiRange(int from, int to) => random.Next(from, to + 1);
        public static float RandfRange(float from, float to) => from + (float)random.NextDouble() * (to - from);
    }

    // Resource classes
    public abstract class Resource : GodotObject { }
    public abstract class StyleBox : Resource { }
    public class StyleBoxFlat : StyleBox { }

    // File system
    public class FileAccess
    {
        public static bool FileExists(string path) => File.Exists(path);
        public static FileAccess? Open(string path, int mode) => new FileAccess();
        
        public static class ModeFlags
        {
            public const int READ = 1;
            public const int WRITE = 2;
        }
        
        public string GetAsText() => "";
        public void Close() { }
    }

    // Theme system
    public static class ThemeDB
    {
        public static Font? FallbackFont => new Font();
    }
}

// Additional stub classes for specific project needs
namespace UsurperRemake.Utils
{
    public interface ITerminal
    {
        void WriteLine(string text);
        void Write(string text);
        string ReadLine();
        void Clear();
    }

    public class TerminalUI : ITerminal
    {
        public void WriteLine(string text) => Console.WriteLine(text);
        public void Write(string text) => Console.Write(text);
        public string ReadLine() => Console.ReadLine() ?? "";
        public void Clear() => Console.Clear();
    }

    public class SaveManager
    {
        public static void SaveGame(object data, string filename) { }
        public static T LoadGame<T>(string filename) where T : new() => new T();
        public static bool SaveExists(string filename) => false;
    }

    public class GodWorldLocation
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
    }
}
