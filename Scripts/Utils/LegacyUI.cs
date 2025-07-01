using System.Threading.Tasks;

namespace UsurperRemake.Utils
{
    /// <summary>
    /// Tiny static wrapper that exposes the classic procedural UI helpers that the original
    /// Pascal-style code expects (DisplayMessage, GetInput, etc.). They all forward to the
    /// singleton <see cref="TerminalEmulator"/> instance so the rest of the engine can stay
    /// untouched for now.
    /// </summary>
    public static class LegacyUI
    {
        private static TerminalEmulator Terminal => TerminalEmulator.Instance ?? new TerminalEmulator();

        public static void DisplayMessage(string message) => Terminal.DisplayMessage(message);
        public static void DisplayMessage(string message, bool newLine) => Terminal.DisplayMessage(message, TerminalEmulator.ColorWhite, newLine);
        public static void DisplayMessage(string message, string color) => Terminal.DisplayMessage(message, color);
        public static void DisplayMessage(string message, System.ConsoleColor color)
            => Terminal.DisplayMessage(message, color);
        public static void DisplayMessage(string message, System.ConsoleColor color, bool newLine)
            => Terminal.DisplayMessage(message, color, newLine);

        public static string GetInput(string prompt = "> ") => Terminal.GetInput(prompt).GetAwaiter().GetResult();

        public static void ClearScreen() => Terminal.ClearScreen();

        public static Character? GetCurrentPlayer() => GameEngine.Instance?.CurrentPlayer;
    }
} 