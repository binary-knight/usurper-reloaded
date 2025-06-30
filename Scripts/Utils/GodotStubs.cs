using UsurperRemake.Utils;
// Godot stub implementations for standalone .NET compilation
using System;
using System.Collections.Generic;

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
    }

    public class RichTextLabel : Control
    {
        public string Text { get; set; } = "";
        public string BbcodeText { get; set; } = "";
        public bool BbcodeEnabled { get; set; }
        public void AppendText(string text) { }
        public void Clear() { }
    }

    public class LineEdit : Control
    {
        public string Text { get; set; } = "";
        public string PlaceholderText { get; set; } = "";
        public void Clear() { }
        public void SelectAll() { }
        public void GrabFocus() { }
    }

    public class Timer : Node
    {
        public float WaitTime { get; set; }
        public bool Autostart { get; set; }
        public bool OneShot { get; set; }
        public void Start() { }
        public void Stop() { }
        public event Action? Timeout;
    }

    public class Font : GodotObject
    {
        public int Size { get; set; }
        public string FontPath { get; set; } = "";
    }

    // Godot data types
    public struct Vector2
    {
        public float X { get; set; }
        public float Y { get; set; }
        
        public Vector2(float x, float y)
        {
            X = x;
            Y = y;
        }
        
        public static Vector2 Zero => new Vector2(0, 0);
    }

    public struct Color
    {
        public float R { get; set; }
        public float G { get; set; }
        public float B { get; set; }
        public float A { get; set; }
        
        public Color(float r, float g, float b, float a = 1.0f)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }
        
        public static Color White => new Color(1, 1, 1);
        public static Color Black => new Color(0, 0, 0);
        public static Color Red => new Color(1, 0, 0);
        public static Color Green => new Color(0, 1, 0);
        public static Color Blue => new Color(0, 0, 1);
        public static Color Yellow => new Color(1, 1, 0);
    }

    // Godot utility functions
    public static class GD
    {
        public static void Print(params object?[] what) 
        {
            Console.WriteLine(string.Join(" ", what ?? new object[0]));
        }
        
        public static void PrintErr(params object?[] what)
        {
            Console.Error.WriteLine(string.Join(" ", what ?? new object[0]));
        }
        
        public static T Load<T>(string path) where T : class => default(T)!;
        public static GodotObject Load(string path) => default(GodotObject)!;
        
        public static int Randi() => new Random().Next();
        public static float Randf() => (float)new Random().NextDouble();
        public static int RandiRange(int from, int to) => new Random().Next(from, to + 1);
        public static float RandfRange(float from, float to) => from + (float)new Random().NextDouble() * (to - from);
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
