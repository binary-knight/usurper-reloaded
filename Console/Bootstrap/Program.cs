using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UsurperRemake;
using UsurperRemake.Systems;
using Godot;

// NOTE: This bootstrapper lets you run the full Usurper remake from a plain
// command-line executable (without opening Godot). It relies on the
// console-safe fallbacks recently added to TerminalEmulator.

namespace UsurperConsole
{
    internal static class Program
    {
        // Windows console handler delegate
        private delegate bool ConsoleCtrlHandlerDelegate(int sig);
        private static ConsoleCtrlHandlerDelegate? _handler;

        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(ConsoleCtrlHandlerDelegate handler, bool add);

        // Console control signal types
        private const int CTRL_C_EVENT = 0;
        private const int CTRL_BREAK_EVENT = 1;
        private const int CTRL_CLOSE_EVENT = 2;
        private const int CTRL_LOGOFF_EVENT = 5;
        private const int CTRL_SHUTDOWN_EVENT = 6;

        private static bool _exitRequested = false;

        static async Task Main(string[] args)
        {
            // Set up console close handlers
            SetupConsoleCloseHandlers();

            // Ensure the Godot runtime is initialised enough for core types that
            // expect it (Node constructor, etc.). The simplest way is to call
            // GD.Load which forces the core to boot. If Godot is not available
            // this still compiles but returns default.
            GD.Print("Launching Usurper Reborn – Console Mode");

            // Spin up the full engine in console mode.
            await GameEngine.RunConsoleAsync();
        }

        private static void SetupConsoleCloseHandlers()
        {
            // Handle Ctrl+C gracefully
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true; // Prevent immediate termination
                HandleConsoleClose("Ctrl+C detected");
            };

            // Handle process exit (called when process is terminating)
            AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
            {
                if (!_exitRequested)
                {
                    HandleConsoleClose("Process exit detected");
                }
            };

            // Windows-specific: Handle console close button (X), shutdown, logoff
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    _handler = new ConsoleCtrlHandlerDelegate(ConsoleCtrlHandler);
                    SetConsoleCtrlHandler(_handler, true);
                }
                catch
                {
                    // Ignore if P/Invoke fails (e.g., running in non-console context)
                }
            }
        }

        private static bool ConsoleCtrlHandler(int sig)
        {
            switch (sig)
            {
                case CTRL_C_EVENT:
                case CTRL_BREAK_EVENT:
                    HandleConsoleClose("Ctrl+C/Break detected");
                    return true; // Handled - don't terminate immediately

                case CTRL_CLOSE_EVENT:
                    // User clicked the X button on the console window
                    HandleConsoleClose("Console window closed");
                    // Give time for save operation
                    System.Threading.Thread.Sleep(2000);
                    return false; // Allow termination after we've handled it

                case CTRL_LOGOFF_EVENT:
                case CTRL_SHUTDOWN_EVENT:
                    HandleConsoleClose("System shutdown/logoff");
                    System.Threading.Thread.Sleep(2000);
                    return false;

                default:
                    return false;
            }
        }

        private static void HandleConsoleClose(string reason)
        {
            if (_exitRequested) return;
            _exitRequested = true;

            // If this is an intentional exit from the game menu, don't show warning
            if (GameEngine.IsIntentionalExit) return;

            try
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine();
                Console.WriteLine("═══════════════════════════════════════════════════════════");
                Console.WriteLine("                    WARNING!");
                Console.WriteLine("═══════════════════════════════════════════════════════════");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"  {reason}");
                Console.WriteLine();
                Console.WriteLine("  Your progress since your last save may be lost!");
                Console.WriteLine("  Please use 'Quit to Main Menu' or go to sleep at the Inn");
                Console.WriteLine("  to save your game properly.");
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("  Attempting emergency save...");

                // Try to perform an emergency save
                var player = GameEngine.Instance?.CurrentPlayer;
                if (player != null)
                {
                    try
                    {
                        // Synchronous save for emergency
                        SaveSystem.Instance.SaveGame("emergency_autosave", player).Wait(TimeSpan.FromSeconds(3));
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("  Emergency save completed!");
                        Console.WriteLine("  Look for 'emergency_autosave' in the save menu.");
                    }
                    catch
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("  Emergency save failed - progress may be lost.");
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine("  No active game session to save.");
                }

                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("═══════════════════════════════════════════════════════════");
                Console.ResetColor();
            }
            catch
            {
                // Ignore any errors during shutdown message
            }
        }
    }
} 