using System;
using System.Threading.Tasks;

namespace UsurperConsole
{
    /// <summary>
    /// Console implementation of terminal functionality
    /// Replaces Godot UI components for command-line gaming
    /// </summary>
    public class ConsoleTerminal
    {
        public void WriteLine(string text)
        {
            Console.WriteLine(text);
        }
        
        public void WriteLine(string text, ConsoleColor color)
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ForegroundColor = oldColor;
        }
        
        public void Write(string text)
        {
            Console.Write(text);
        }
        
        public void Write(string text, ConsoleColor color)
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(text);
            Console.ForegroundColor = oldColor;
        }
        
        public void SetColor(ConsoleColor color)
        {
            Console.ForegroundColor = color;
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