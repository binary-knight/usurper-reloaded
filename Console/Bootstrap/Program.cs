using System.Threading.Tasks;
using UsurperRemake;
using Godot;

// NOTE: This bootstrapper lets you run the full Usurper remake from a plain
// command-line executable (without opening Godot). It relies on the
// console-safe fallbacks recently added to TerminalEmulator.

namespace UsurperConsole
{
    internal static class Program
    {
        static async Task Main(string[] args)
        {
            // Ensure the Godot runtime is initialised enough for core types that
            // expect it (Node constructor, etc.). The simplest way is to call
            // GD.Load which forces the core to boot. If Godot is not available
            // this still compiles but returns default.
            GD.Print("Launching Usurper Reborn – Console Mode");

            // Spin up the full engine in console mode.
            await GameEngine.RunConsoleAsync();
        }
    }
} 