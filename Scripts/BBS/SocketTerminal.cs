using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace UsurperRemake.BBS
{
    /// <summary>
    /// Terminal I/O implementation that reads/writes to an inherited socket handle
    /// Used when running as a BBS door game
    /// </summary>
    public class SocketTerminal : IDisposable
    {
        private Socket? _socket;
        private NetworkStream? _stream;
        private StreamReader? _reader;
        private StreamWriter? _writer;
        private readonly BBSSessionInfo _sessionInfo;
        private bool _disposed = false;
        private string _currentColor = "white";

        // ANSI escape codes
        private const string ESC = "\x1b";
        private const string CSI = "\x1b[";

        // ANSI color codes
        private static readonly Dictionary<string, string> AnsiColors = new()
        {
            // Standard colors (30-37)
            { "black", "30" },
            { "red", "31" },
            { "green", "32" },
            { "yellow", "33" },
            { "blue", "34" },
            { "magenta", "35" },
            { "cyan", "36" },
            { "white", "37" },
            { "gray", "90" },
            { "grey", "90" },

            // Dark variants (use dim attribute)
            { "darkred", "31" },
            { "dark_red", "31" },
            { "darkgreen", "32" },
            { "dark_green", "32" },
            { "darkyellow", "33" },
            { "dark_yellow", "33" },
            { "brown", "33" },
            { "darkblue", "34" },
            { "dark_blue", "34" },
            { "darkmagenta", "35" },
            { "dark_magenta", "35" },
            { "darkcyan", "36" },
            { "dark_cyan", "36" },
            { "darkgray", "90" },
            { "dark_gray", "90" },
            { "darkgrey", "90" },

            // Bright variants (90-97)
            { "bright_black", "90" },
            { "bright_red", "91" },
            { "bright_green", "92" },
            { "bright_yellow", "93" },
            { "bright_blue", "94" },
            { "bright_magenta", "95" },
            { "bright_cyan", "96" },
            { "bright_white", "97" }
        };

        // Unicode to CP437 character mapping for BBS compatibility
        // Maps Unicode box-drawing and special characters to their CP437 byte equivalents
        private static readonly Dictionary<char, byte> UnicodeToCp437 = new()
        {
            // Box drawing - single line
            { '─', 196 }, // Horizontal line
            { '│', 179 }, // Vertical line
            { '┌', 218 }, // Top-left corner
            { '┐', 191 }, // Top-right corner
            { '└', 192 }, // Bottom-left corner
            { '┘', 217 }, // Bottom-right corner
            { '├', 195 }, // Left T
            { '┤', 180 }, // Right T
            { '┬', 194 }, // Top T
            { '┴', 193 }, // Bottom T
            { '┼', 197 }, // Cross

            // Box drawing - double line
            { '═', 205 }, // Double horizontal
            { '║', 186 }, // Double vertical
            { '╔', 201 }, // Double top-left
            { '╗', 187 }, // Double top-right
            { '╚', 200 }, // Double bottom-left
            { '╝', 188 }, // Double bottom-right
            { '╠', 204 }, // Double left T
            { '╣', 185 }, // Double right T
            { '╦', 203 }, // Double top T
            { '╩', 202 }, // Double bottom T
            { '╬', 206 }, // Double cross

            // Box drawing - mixed single/double
            { '╒', 213 }, // Down single, right double
            { '╓', 214 }, // Down double, right single
            { '╕', 184 }, // Down single, left double
            { '╖', 183 }, // Down double, left single
            { '╘', 212 }, // Up single, right double
            { '╙', 211 }, // Up double, right single
            { '╛', 190 }, // Up single, left double
            { '╜', 189 }, // Up double, left single
            { '╞', 198 }, // Vertical single, right double
            { '╟', 199 }, // Vertical double, right single
            { '╡', 181 }, // Vertical single, left double
            { '╢', 182 }, // Vertical double, left single
            { '╤', 209 }, // Down single, horizontal double
            { '╥', 210 }, // Down double, horizontal single
            { '╧', 207 }, // Up single, horizontal double
            { '╨', 208 }, // Up double, horizontal single
            { '╪', 216 }, // Vertical single, horizontal double
            { '╫', 215 }, // Vertical double, horizontal single

            // Block elements
            { '█', 219 }, // Full block
            { '▄', 220 }, // Lower half block
            { '▀', 223 }, // Upper half block
            { '▌', 221 }, // Left half block
            { '▐', 222 }, // Right half block
            { '░', 176 }, // Light shade
            { '▒', 177 }, // Medium shade
            { '▓', 178 }, // Dark shade

            // Arrows
            { '↑', 24 },
            { '↓', 25 },
            { '→', 26 },
            { '←', 27 },
            { '↔', 29 },
            { '↕', 18 },

            // Math and symbols
            { '≡', 240 }, // Identical to
            { '±', 241 }, // Plus-minus
            { '≥', 242 }, // Greater than or equal
            { '≤', 243 }, // Less than or equal
            { '÷', 246 }, // Division
            { '≈', 247 }, // Almost equal
            { '°', 248 }, // Degree
            { '•', 249 }, // Bullet
            { '·', 250 }, // Middle dot
            { '√', 251 }, // Square root
            { '²', 253 }, // Superscript 2
            { '■', 254 }, // Black square

            // Currency and misc
            { '¢', 155 },
            { '£', 156 },
            { '¥', 157 },
            { '₧', 158 }, // Peseta
            { 'ƒ', 159 }, // Florin

            // Greek letters (commonly used)
            { 'α', 224 },
            { 'β', 225 },
            { 'Γ', 226 },
            { 'π', 227 },
            { 'Σ', 228 },
            { 'σ', 229 },
            { 'µ', 230 },
            { 'τ', 231 },
            { 'Φ', 232 },
            { 'Θ', 233 },
            { 'Ω', 234 },
            { 'δ', 235 },
            { '∞', 236 },
            { 'φ', 237 },
            { 'ε', 238 },
            { '∩', 239 },

            // Special characters
            { '♠', 6 },
            { '♣', 5 },
            { '♥', 3 },
            { '♦', 4 },
            { '☺', 1 },
            { '☻', 2 },
            { '☼', 15 },
            { '♪', 13 },
            { '♫', 14 },

            // Accented vowels (common ones)
            { 'á', 160 },
            { 'í', 161 },
            { 'ó', 162 },
            { 'ú', 163 },
            { 'ñ', 164 },
            { 'Ñ', 165 },
            { 'ª', 166 },
            { 'º', 167 },
            { '¿', 168 },
            { '¡', 173 },
            { 'ä', 132 },
            { 'Ä', 142 },
            { 'ö', 148 },
            { 'Ö', 153 },
            { 'ü', 129 },
            { 'Ü', 154 },
            { 'é', 130 },
            { 'è', 138 },
            { 'ê', 136 },
            { 'ë', 137 },
            { 'â', 131 },
            { 'à', 133 },
            { 'ç', 135 },
            { 'Ç', 128 },
        };

        public bool IsConnected => _socket?.Connected ?? false;
        public BBSSessionInfo SessionInfo => _sessionInfo;

        public SocketTerminal(BBSSessionInfo sessionInfo)
        {
            _sessionInfo = sessionInfo;
        }

        /// <summary>
        /// Initialize the socket from the inherited handle in the session info
        /// </summary>
        public bool Initialize()
        {
            if (_sessionInfo.CommType == ConnectionType.Local)
            {
                // Local mode - no socket needed
                return true;
            }

            if (_sessionInfo.SocketHandle <= 0)
            {
                Console.Error.WriteLine($"Invalid socket handle: {_sessionInfo.SocketHandle}");
                return false;
            }

            try
            {
                // Create socket from inherited handle
                _socket = CreateSocketFromHandle(_sessionInfo.SocketHandle);

                if (_socket == null || !_socket.Connected)
                {
                    Console.Error.WriteLine("Failed to create socket from handle or socket not connected");
                    return false;
                }

                _stream = new NetworkStream(_socket, ownsSocket: false);
                _reader = new StreamReader(_stream, Encoding.ASCII);
                _writer = new StreamWriter(_stream, Encoding.ASCII) { AutoFlush = true };

                return true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to initialize socket: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Create a Socket object from an inherited handle
        /// Works on both Windows (socket handle) and Linux (file descriptor)
        /// </summary>
        private Socket? CreateSocketFromHandle(int handle)
        {
            try
            {
                // Create SafeSocketHandle from the inherited handle
                // On Windows: this is a SOCKET handle
                // On Linux: this is a file descriptor (int)
                // Both work with SafeSocketHandle in .NET 6+
                var safeHandle = new SafeSocketHandle(new IntPtr(handle), ownsHandle: false);
                var socket = new Socket(safeHandle);

                // Verify the socket is valid
                if (!socket.Connected)
                {
                    Console.Error.WriteLine($"Socket from handle {handle} is not connected");
                    // On some systems, Connected may return false even for valid sockets
                    // Try to use it anyway if no exception was thrown
                }

                return socket;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"CreateSocketFromHandle failed: {ex.Message}");
                Console.Error.WriteLine("This may be normal on some Linux BBSes - falling back to console I/O");
                return null;
            }
        }

        #region Output Methods

        public async Task WriteAsync(string text)
        {
            if (_sessionInfo.CommType == ConnectionType.Local || _stream == null)
            {
                Console.Write(text);
                return;
            }

            try
            {
                // Convert text to CP437 bytes for BBS compatibility
                var bytes = ConvertToCp437(text);
                await _stream.WriteAsync(bytes, 0, bytes.Length);
                await _stream.FlushAsync();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Socket write error: {ex.Message}");
            }
        }

        /// <summary>
        /// Convert a Unicode string to CP437 bytes for BBS terminal compatibility.
        /// Characters with known CP437 mappings are converted, others are passed through as ASCII.
        /// </summary>
        private byte[] ConvertToCp437(string text)
        {
            var result = new byte[text.Length];
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                // Check if we have a CP437 mapping for this character
                if (UnicodeToCp437.TryGetValue(c, out byte cp437Byte))
                {
                    result[i] = cp437Byte;
                }
                else if (c <= 127)
                {
                    // Standard ASCII - pass through directly
                    result[i] = (byte)c;
                }
                else
                {
                    // Unknown character - use '?' as fallback
                    result[i] = (byte)'?';
                }
            }
            return result;
        }

        public async Task WriteLineAsync(string text = "")
        {
            await WriteAsync(text + "\r\n");
        }

        public async Task WriteAsync(string text, string color)
        {
            await SetColorAsync(color);
            await WriteAsync(text);
        }

        public async Task WriteLineAsync(string text, string color)
        {
            await SetColorAsync(color);
            await WriteLineAsync(text);
        }

        public async Task SetColorAsync(string color)
        {
            if (string.IsNullOrEmpty(color) || color == _currentColor)
                return;

            _currentColor = color.ToLowerInvariant();

            if (_sessionInfo.Emulation == TerminalEmulation.ASCII)
                return; // No colors in ASCII mode

            string ansiCode = GetAnsiColorCode(_currentColor);
            await WriteRawAsync($"{CSI}{ansiCode}m");
        }

        public async Task ClearScreenAsync()
        {
            if (_sessionInfo.Emulation >= TerminalEmulation.ANSI)
            {
                await WriteRawAsync($"{CSI}2J{CSI}H"); // Clear screen and move to home
            }
            else
            {
                // ASCII mode - send form feed or many newlines
                await WriteAsync("\f");
            }
        }

        public async Task MoveCursorAsync(int row, int col)
        {
            if (_sessionInfo.Emulation >= TerminalEmulation.ANSI)
            {
                await WriteRawAsync($"{CSI}{row};{col}H");
            }
        }

        public async Task ResetAttributesAsync()
        {
            if (_sessionInfo.Emulation >= TerminalEmulation.ANSI)
            {
                await WriteRawAsync($"{CSI}0m");
                _currentColor = "white";
            }
        }

        /// <summary>
        /// Write raw bytes/text without any CP437 processing (used for ANSI escape codes)
        /// </summary>
        private async Task WriteRawAsync(string data)
        {
            if (_sessionInfo.CommType == ConnectionType.Local || _stream == null)
            {
                Console.Write(data);
                return;
            }

            try
            {
                // Write raw ASCII bytes without CP437 conversion (for ANSI escape codes)
                var bytes = Encoding.ASCII.GetBytes(data);
                await _stream.WriteAsync(bytes, 0, bytes.Length);
                await _stream.FlushAsync();
            }
            catch { }
        }

        /// <summary>
        /// Write text with inline color markup [colorname]text[/]
        /// </summary>
        public async Task WriteMarkupAsync(string text)
        {
            var segments = ParseColorMarkup(text);
            foreach (var (content, color) in segments)
            {
                if (!string.IsNullOrEmpty(color))
                    await SetColorAsync(color);
                await WriteAsync(content);
            }
        }

        public async Task WriteMarkupLineAsync(string text)
        {
            await WriteMarkupAsync(text);
            await WriteLineAsync();
        }

        #endregion

        #region Input Methods

        public async Task<string> GetInputAsync(string prompt = "")
        {
            if (!string.IsNullOrEmpty(prompt))
                await WriteAsync(prompt);

            if (_sessionInfo.CommType == ConnectionType.Local || _reader == null)
            {
                return Console.ReadLine() ?? "";
            }

            try
            {
                var line = await ReadLineFromSocketAsync();
                return line ?? "";
            }
            catch
            {
                return "";
            }
        }

        public async Task<string> GetKeyInputAsync(string prompt = "")
        {
            if (!string.IsNullOrEmpty(prompt))
                await WriteAsync(prompt);

            if (_sessionInfo.CommType == ConnectionType.Local || _stream == null)
            {
                var key = Console.ReadKey(true);
                return key.KeyChar.ToString();
            }

            try
            {
                // Read a single character from socket
                var buffer = new byte[1];
                var bytesRead = await _stream.ReadAsync(buffer, 0, 1);

                if (bytesRead == 0)
                    return "";

                return Encoding.ASCII.GetString(buffer, 0, 1);
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Read a line from the socket, handling telnet negotiation and line endings
        /// </summary>
        private async Task<string?> ReadLineFromSocketAsync()
        {
            if (_stream == null) return null;

            var buffer = new StringBuilder();
            var charBuffer = new byte[1];

            while (true)
            {
                var bytesRead = await _stream.ReadAsync(charBuffer, 0, 1);
                if (bytesRead == 0)
                    return buffer.Length > 0 ? buffer.ToString() : null;

                byte b = charBuffer[0];

                // Handle telnet IAC commands (255 = IAC)
                if (b == 255)
                {
                    await HandleTelnetCommandAsync();
                    continue;
                }

                // CR or LF ends the line
                if (b == '\r' || b == '\n')
                {
                    // Consume any following LF after CR
                    if (b == '\r' && _stream.DataAvailable)
                    {
                        var peek = new byte[1];
                        await _stream.ReadAsync(peek, 0, 1);
                        // If it's not LF, we'd need to push it back, but for simplicity we accept it
                    }
                    return buffer.ToString();
                }

                // Backspace handling
                if (b == 8 || b == 127) // BS or DEL
                {
                    if (buffer.Length > 0)
                    {
                        buffer.Remove(buffer.Length - 1, 1);
                        // Echo backspace-space-backspace to erase character
                        await WriteRawAsync("\b \b");
                    }
                    continue;
                }

                // Ignore control characters except printable ASCII
                if (b < 32)
                    continue;

                buffer.Append((char)b);

                // Echo the character back (most telnet clients expect echo)
                await WriteRawAsync(((char)b).ToString());
            }
        }

        /// <summary>
        /// Handle telnet IAC (Interpret As Command) sequences
        /// </summary>
        private async Task HandleTelnetCommandAsync()
        {
            if (_stream == null) return;

            var cmdBuffer = new byte[2];
            var bytesRead = await _stream.ReadAsync(cmdBuffer, 0, 2);

            if (bytesRead < 2) return;

            byte cmd = cmdBuffer[0];
            byte option = cmdBuffer[1];

            // Respond to common telnet negotiations
            // 251 = WILL, 252 = WON'T, 253 = DO, 254 = DON'T

            switch (cmd)
            {
                case 251: // WILL - respond with DON'T
                    await WriteRawAsync($"\xff\xfe{(char)option}"); // IAC DON'T option
                    break;
                case 253: // DO - respond with WON'T
                    await WriteRawAsync($"\xff\xfc{(char)option}"); // IAC WON'T option
                    break;
            }
        }

        #endregion

        #region Helper Methods

        private string GetAnsiColorCode(string color)
        {
            color = color.ToLowerInvariant().Replace("_", "");

            if (AnsiColors.TryGetValue(color, out var code))
                return code;

            // Also check with underscores
            if (AnsiColors.TryGetValue(color.Replace("_", ""), out code))
                return code;

            return "37"; // Default to white
        }

        /// <summary>
        /// Parse color markup like [red]text[/] into segments
        /// </summary>
        private List<(string content, string? color)> ParseColorMarkup(string text)
        {
            var result = new List<(string content, string? color)>();
            var current = new StringBuilder();
            string? currentColor = null;
            int i = 0;

            while (i < text.Length)
            {
                if (text[i] == '[')
                {
                    // Look for closing bracket
                    int end = text.IndexOf(']', i + 1);
                    if (end > i)
                    {
                        var tag = text.Substring(i + 1, end - i - 1).ToLowerInvariant();

                        if (tag == "/" || tag == "/color")
                        {
                            // End tag - save current segment
                            if (current.Length > 0)
                            {
                                result.Add((current.ToString(), currentColor));
                                current.Clear();
                            }
                            currentColor = null;
                            i = end + 1;
                            continue;
                        }
                        else if (IsValidColor(tag))
                        {
                            // Color tag - save current segment and start new
                            if (current.Length > 0)
                            {
                                result.Add((current.ToString(), currentColor));
                                current.Clear();
                            }
                            currentColor = tag;
                            i = end + 1;
                            continue;
                        }
                    }
                }

                current.Append(text[i]);
                i++;
            }

            if (current.Length > 0)
                result.Add((current.ToString(), currentColor));

            return result;
        }

        private bool IsValidColor(string color)
        {
            return AnsiColors.ContainsKey(color) ||
                   AnsiColors.ContainsKey(color.Replace("_", ""));
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            _writer?.Dispose();
            _reader?.Dispose();
            _stream?.Dispose();
            // Don't dispose the socket - we don't own it (BBS does)
        }

        #endregion
    }
}
