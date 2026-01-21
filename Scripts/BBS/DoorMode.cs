using System;
using System.IO;
using System.Threading.Tasks;

namespace UsurperRemake.BBS
{
    /// <summary>
    /// BBS Door mode launcher - handles initialization when running as a door game
    /// </summary>
    public static class DoorMode
    {
        private static BBSSessionInfo? _sessionInfo;
        private static SocketTerminal? _socketTerminal;
        private static BBSTerminalAdapter? _terminalAdapter;
        private static bool _forceStdio = false;

        public static BBSSessionInfo? SessionInfo => _sessionInfo;
        public static BBSTerminalAdapter? TerminalAdapter => _terminalAdapter;
        public static bool IsInDoorMode => _sessionInfo != null && _sessionInfo.SourceType != DropFileType.None;

        /// <summary>
        /// Check command line args for door mode parameters
        /// Returns true if door mode should be used
        /// </summary>
        public static bool ParseCommandLineArgs(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i].ToLowerInvariant();

                // --door or -d followed by drop file path
                if ((arg == "--door" || arg == "-d") && i + 1 < args.Length)
                {
                    var dropFilePath = args[i + 1];
                    return InitializeFromDropFile(dropFilePath);
                }

                // --door32 followed by path (explicit DOOR32.SYS)
                if (arg == "--door32" && i + 1 < args.Length)
                {
                    var path = args[i + 1];
                    return InitializeFromDoor32Sys(path);
                }

                // --doorsys followed by path (explicit DOOR.SYS)
                if (arg == "--doorsys" && i + 1 < args.Length)
                {
                    var path = args[i + 1];
                    return InitializeFromDoorSys(path);
                }

                // --node followed by node directory (auto-detect drop file)
                if ((arg == "--node" || arg == "-n") && i + 1 < args.Length)
                {
                    var nodeDir = args[i + 1];
                    return InitializeFromNodeDirectory(nodeDir);
                }

                // --local for local testing mode
                if (arg == "--local" || arg == "-l")
                {
                    _sessionInfo = DropFileParser.CreateLocalSession();
                    return true;
                }

                // --stdio forces console I/O even when drop file has socket handle
                // Use with Standard I/O mode in Synchronet
                if (arg == "--stdio")
                {
                    _forceStdio = true;
                    continue; // Keep processing other args
                }

                // --help
                if (arg == "--help" || arg == "-h" || arg == "-?")
                {
                    PrintDoorHelp();
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Initialize from auto-detected drop file
        /// </summary>
        private static bool InitializeFromDropFile(string path)
        {
            try
            {
                _sessionInfo = DropFileParser.ParseDropFileAsync(path).GetAwaiter().GetResult();

                if (_sessionInfo == null)
                {
                    Console.Error.WriteLine($"Could not parse drop file: {path}");
                    return false;
                }

                Console.Error.WriteLine($"Loaded {_sessionInfo.SourceType} from: {_sessionInfo.SourcePath}");
                Console.Error.WriteLine($"User: {_sessionInfo.UserName} ({_sessionInfo.UserAlias})");
                Console.Error.WriteLine($"Connection: {_sessionInfo.CommType}, Handle: {_sessionInfo.SocketHandle}");

                return true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error loading drop file: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Initialize from explicit DOOR32.SYS path
        /// </summary>
        private static bool InitializeFromDoor32Sys(string path)
        {
            try
            {
                _sessionInfo = DropFileParser.ParseDoor32SysAsync(path).GetAwaiter().GetResult();
                Console.Error.WriteLine($"Loaded DOOR32.SYS: {path}");
                return true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error loading DOOR32.SYS: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Initialize from explicit DOOR.SYS path
        /// </summary>
        private static bool InitializeFromDoorSys(string path)
        {
            try
            {
                _sessionInfo = DropFileParser.ParseDoorSysAsync(path).GetAwaiter().GetResult();
                Console.Error.WriteLine($"Loaded DOOR.SYS: {path}");
                return true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error loading DOOR.SYS: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Initialize from a node directory (search for drop files)
        /// </summary>
        private static bool InitializeFromNodeDirectory(string nodeDir)
        {
            if (!Directory.Exists(nodeDir))
            {
                Console.Error.WriteLine($"Node directory not found: {nodeDir}");
                return false;
            }

            return InitializeFromDropFile(nodeDir);
        }

        /// <summary>
        /// Initialize the terminal for door mode
        /// Call this after ParseCommandLineArgs returns true
        /// </summary>
        public static BBSTerminalAdapter? InitializeTerminal()
        {
            if (_sessionInfo == null)
            {
                Console.Error.WriteLine("No session info - call ParseCommandLineArgs first");
                return null;
            }

            try
            {
                // If --stdio flag was used, force console I/O mode
                // This is for Synchronet's "Standard" I/O mode where stdin/stdout are redirected
                if (_forceStdio)
                {
                    Console.Error.WriteLine("Using Standard I/O mode (--stdio flag)");
                    _sessionInfo.CommType = ConnectionType.Local;
                }

                _socketTerminal = new SocketTerminal(_sessionInfo);

                if (!_socketTerminal.Initialize())
                {
                    Console.Error.WriteLine("Failed to initialize socket terminal");

                    // Fall back to local mode
                    if (_sessionInfo.CommType != ConnectionType.Local)
                    {
                        Console.Error.WriteLine("Falling back to local console mode");
                        _sessionInfo.CommType = ConnectionType.Local;
                    }
                }

                // Pass _forceStdio to tell adapter to use ANSI codes instead of Console.ForegroundColor
                _terminalAdapter = new BBSTerminalAdapter(_socketTerminal, _forceStdio);
                return _terminalAdapter;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Terminal initialization failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get the player name from the drop file for character lookup/creation
        /// </summary>
        public static string GetPlayerName()
        {
            if (_sessionInfo == null)
                return "Player";

            // Prefer alias, fall back to real name
            return !string.IsNullOrWhiteSpace(_sessionInfo.UserAlias)
                ? _sessionInfo.UserAlias
                : _sessionInfo.UserName;
        }

        /// <summary>
        /// Get a unique save namespace for this BBS to isolate saves from different BBSes.
        /// Uses the BBS name from the drop file, sanitized for use as a directory name.
        /// Returns null if not in door mode (use default saves directory).
        /// </summary>
        public static string? GetSaveNamespace()
        {
            if (_sessionInfo == null || !IsInDoorMode)
                return null;

            // Sanitize the BBS name for use as a directory
            var bbsName = _sessionInfo.BBSName;
            if (string.IsNullOrWhiteSpace(bbsName))
                bbsName = "BBS";

            // Remove invalid path characters
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = string.Join("_", bbsName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));

            // Limit length
            if (sanitized.Length > 32)
                sanitized = sanitized.Substring(0, 32);

            return sanitized;
        }

        /// <summary>
        /// Get the user record number from the drop file (unique ID per BBS user)
        /// </summary>
        public static int GetUserRecordNumber()
        {
            return _sessionInfo?.UserRecordNumber ?? 0;
        }

        /// <summary>
        /// Clean shutdown of door mode
        /// </summary>
        public static void Shutdown()
        {
            try
            {
                _socketTerminal?.Dispose();
            }
            catch { }

            _socketTerminal = null;
            _terminalAdapter = null;
            _sessionInfo = null;
        }

        /// <summary>
        /// Print help for door mode command line options
        /// </summary>
        private static void PrintDoorHelp()
        {
            Console.WriteLine("Usurper Reborn - BBS Door Mode");
            Console.WriteLine("");
            Console.WriteLine("Usage: UsurperReborn [options]");
            Console.WriteLine("");
            Console.WriteLine("Door Mode Options:");
            Console.WriteLine("  --door, -d <path>    Load drop file (auto-detect DOOR32.SYS or DOOR.SYS)");
            Console.WriteLine("  --door32 <path>      Load DOOR32.SYS explicitly");
            Console.WriteLine("  --doorsys <path>     Load DOOR.SYS explicitly");
            Console.WriteLine("  --node, -n <dir>     Search node directory for drop files");
            Console.WriteLine("  --local, -l          Run in local mode (no BBS connection)");
            Console.WriteLine("  --stdio              Use Standard I/O instead of socket (for Synchronet)");
            Console.WriteLine("");
            Console.WriteLine("Examples:");
            Console.WriteLine("  UsurperReborn --door /sbbs/node1/door32.sys");
            Console.WriteLine("  UsurperReborn --node /sbbs/node1");
            Console.WriteLine("  UsurperReborn -d C:\\SBBS\\NODE1\\");
            Console.WriteLine("");
            Console.WriteLine("Drop File Support:");
            Console.WriteLine("  DOOR32.SYS - Modern format with socket handle (recommended)");
            Console.WriteLine("  DOOR.SYS   - Legacy format (52 lines, no socket - uses console)");
            Console.WriteLine("");
            Console.WriteLine("For Synchronet BBS (Socket I/O mode):");
            Console.WriteLine("  Command: UsurperReborn --door %f");
            Console.WriteLine("  Drop File Type: Door32.sys");
            Console.WriteLine("  I/O Method: Socket");
            Console.WriteLine("");
            Console.WriteLine("For Synchronet BBS (Standard I/O mode - recommended):");
            Console.WriteLine("  Command: UsurperReborn --door32 %f --stdio");
            Console.WriteLine("  Drop File Type: Door32.sys");
            Console.WriteLine("  I/O Method: Standard");
            Console.WriteLine("  Native Executable: Yes");
            Console.WriteLine("");
        }

        /// <summary>
        /// Write a message to the BBS log (stderr)
        /// </summary>
        public static void Log(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            Console.Error.WriteLine($"[{timestamp}] USURPER: {message}");
        }
    }
}
