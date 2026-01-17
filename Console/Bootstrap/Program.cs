using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UsurperRemake;
using UsurperRemake.Systems;
using UsurperRemake.BBS;

// Console bootstrapper for Usurper Reborn
//
// BBS DOOR MODE: Use command-line arguments to run as a BBS door:
//   --door <path>     Load drop file (auto-detect DOOR32.SYS or DOOR.SYS)
//   --door32 <path>   Load DOOR32.SYS explicitly
//   --node <dir>      Search node directory for drop files
//   --local           Run in local mode (no BBS connection)

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
            // Check for BBS door mode arguments
            if (args.Length > 0 && DoorMode.ParseCommandLineArgs(args))
            {
                // BBS Door Mode - initialize door terminal
                await RunDoorModeAsync();
                return;
            }

            // Standard console mode
            // Set up console close handlers
            SetupConsoleCloseHandlers();

            Console.WriteLine("Launching Usurper Reborn – Console Mode");

            // Spin up the full engine in console mode.
            await GameEngine.RunConsoleAsync();
        }

        /// <summary>
        /// Run the game in BBS door mode
        /// </summary>
        private static async Task RunDoorModeAsync()
        {
            try
            {
                DoorMode.Log("Initializing BBS door mode...");

                // Initialize the terminal adapter
                var terminal = DoorMode.InitializeTerminal();
                if (terminal == null)
                {
                    DoorMode.Log("Failed to initialize terminal - aborting");
                    return;
                }

                var sessionInfo = DoorMode.SessionInfo;
                if (sessionInfo != null)
                {
                    DoorMode.Log($"Session: {sessionInfo.UserName} from {sessionInfo.BBSName}");
                    DoorMode.Log($"Connection: {sessionInfo.CommType}, Node: {sessionInfo.NodeNumber}");
                }

                // Set up console close handlers (for local mode fallback)
                SetupConsoleCloseHandlers();

                // Run the game engine in door mode
                // The terminal adapter will handle all I/O
                await GameEngine.RunConsoleAsync();
            }
            catch (Exception ex)
            {
                DoorMode.Log($"Door mode error: {ex.Message}");
                Console.Error.WriteLine(ex.ToString());
            }
            finally
            {
                DoorMode.Log("Shutting down door mode...");
                DoorMode.Shutdown();
            }
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