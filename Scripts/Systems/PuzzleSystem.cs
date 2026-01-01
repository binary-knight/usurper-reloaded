using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using UsurperRemake.Utils;

namespace UsurperRemake.Systems
{
    /// <summary>
    /// Comprehensive Puzzle System for dungeon challenges
    /// Handles logic puzzles, environmental puzzles, and combat puzzles
    /// </summary>
    public class PuzzleSystem
    {
        private static PuzzleSystem? _instance;
        public static PuzzleSystem Instance => _instance ??= new PuzzleSystem();

        private Random random = new Random();

        // Track solved puzzles per floor
        private Dictionary<int, HashSet<string>> solvedPuzzles = new();

        public event Action<PuzzleType, bool>? OnPuzzleCompleted;

        public PuzzleSystem()
        {
            _instance = this;
        }

        /// <summary>
        /// Generate a puzzle for a room based on type and difficulty
        /// </summary>
        public PuzzleInstance GeneratePuzzle(PuzzleType type, int difficulty, DungeonTheme theme)
        {
            return type switch
            {
                PuzzleType.LeverSequence => GenerateLeverPuzzle(difficulty, theme),
                PuzzleType.SymbolAlignment => GenerateSymbolPuzzle(difficulty, theme),
                PuzzleType.PressurePlates => GeneratePressurePuzzle(difficulty, theme),
                PuzzleType.NumberGrid => GenerateNumberPuzzle(difficulty),
                PuzzleType.MemoryMatch => GenerateMemoryPuzzle(difficulty, theme),
                PuzzleType.LightDarkness => GenerateLightPuzzle(difficulty, theme),
                PuzzleType.ItemCombination => GenerateItemPuzzle(difficulty, theme),
                PuzzleType.EnvironmentChange => GenerateEnvironmentPuzzle(difficulty, theme),
                PuzzleType.ReflectionPuzzle => GenerateReflectionPuzzle(difficulty, theme),
                _ => GenerateLeverPuzzle(difficulty, theme)
            };
        }

        /// <summary>
        /// Get a random puzzle type appropriate for the floor level
        /// </summary>
        public PuzzleType GetRandomPuzzleType(int floorLevel)
        {
            var availableTypes = new List<PuzzleType>
            {
                PuzzleType.LeverSequence,
                PuzzleType.SymbolAlignment,
                PuzzleType.NumberGrid
            };

            // Add more complex puzzles at deeper floors
            if (floorLevel >= 15)
            {
                availableTypes.Add(PuzzleType.PressurePlates);
                availableTypes.Add(PuzzleType.MemoryMatch);
            }

            if (floorLevel >= 30)
            {
                availableTypes.Add(PuzzleType.LightDarkness);
                availableTypes.Add(PuzzleType.ItemCombination);
            }

            if (floorLevel >= 50)
            {
                availableTypes.Add(PuzzleType.EnvironmentChange);
                availableTypes.Add(PuzzleType.ReflectionPuzzle);
            }

            return availableTypes[random.Next(availableTypes.Count)];
        }

        #region Puzzle Generation

        private PuzzleInstance GenerateLeverPuzzle(int difficulty, DungeonTheme theme)
        {
            int leverCount = 3 + difficulty;
            var solution = Enumerable.Range(0, leverCount).OrderBy(_ => random.Next()).ToList();

            var hints = new List<string>();
            if (difficulty < 3)
            {
                // Give partial hints for easier puzzles
                hints.Add($"The {GetOrdinal(solution[0] + 1)} lever must be first.");
                if (leverCount > 4)
                    hints.Add($"The {GetOrdinal(solution[leverCount - 1] + 1)} lever is last.");
            }

            return new PuzzleInstance
            {
                Type = PuzzleType.LeverSequence,
                Difficulty = difficulty,
                Theme = theme,
                Title = GetLeverPuzzleTitle(theme),
                Description = $"Before you stand {leverCount} levers, each marked with a different symbol. " +
                             "They must be pulled in the correct sequence to proceed.",
                Solution = solution.Select(i => i.ToString()).ToList(),
                CurrentState = new List<string>(),
                MaxAttempts = 3 + difficulty,
                AttemptsRemaining = 3 + difficulty,
                Hints = hints,
                FailureDamagePercent = 10 + (difficulty * 5),
                SuccessXP = 50 * difficulty
            };
        }

        private PuzzleInstance GenerateSymbolPuzzle(int difficulty, DungeonTheme theme)
        {
            var symbols = GetThemedSymbols(theme);
            int panelCount = 3 + (difficulty / 2);
            var solution = new List<string>();

            for (int i = 0; i < panelCount; i++)
            {
                solution.Add(symbols[random.Next(symbols.Length)]);
            }

            // Generate a cryptic hint
            string hint = GenerateSymbolHint(solution, symbols);

            return new PuzzleInstance
            {
                Type = PuzzleType.SymbolAlignment,
                Difficulty = difficulty,
                Theme = theme,
                Title = "Symbol Alignment",
                Description = $"A circular mechanism with {panelCount} rotating panels. " +
                             "Each panel displays various symbols. Align them correctly to unlock the way.",
                Solution = solution,
                CurrentState = Enumerable.Repeat(symbols[0], panelCount).ToList(),
                AvailableChoices = symbols.ToList(),
                MaxAttempts = 5 + difficulty,
                AttemptsRemaining = 5 + difficulty,
                Hints = new List<string> { hint },
                FailureDamagePercent = 5 + (difficulty * 3),
                SuccessXP = 40 * difficulty
            };
        }

        private PuzzleInstance GeneratePressurePuzzle(int difficulty, DungeonTheme theme)
        {
            int plateCount = 4 + difficulty;
            var solution = Enumerable.Range(0, plateCount).OrderBy(_ => random.Next()).ToList();

            return new PuzzleInstance
            {
                Type = PuzzleType.PressurePlates,
                Difficulty = difficulty,
                Theme = theme,
                Title = "Pressure Plates",
                Description = $"The floor is divided into {plateCount} distinct pressure plates. " +
                             "Step on them in the wrong order, and the traps will activate.",
                Solution = solution.Select(i => i.ToString()).ToList(),
                CurrentState = new List<string>(),
                MaxAttempts = 2 + difficulty,
                AttemptsRemaining = 2 + difficulty,
                Hints = new List<string> { "Study the wear patterns on the plates..." },
                FailureDamagePercent = 15 + (difficulty * 5),
                SuccessXP = 60 * difficulty,
                RequiresMovement = true
            };
        }

        private PuzzleInstance GenerateNumberPuzzle(int difficulty)
        {
            // Generate a simple math puzzle
            int target = 10 + (difficulty * 5) + random.Next(20);
            var numbers = new List<int>();
            int remaining = target;

            while (remaining > 0)
            {
                int n = random.Next(1, Math.Min(remaining + 1, 10));
                numbers.Add(n);
                remaining -= n;
            }

            // Add some red herrings
            for (int i = 0; i < difficulty; i++)
            {
                numbers.Add(random.Next(1, 15));
            }

            numbers = numbers.OrderBy(_ => random.Next()).ToList();

            return new PuzzleInstance
            {
                Type = PuzzleType.NumberGrid,
                Difficulty = difficulty,
                Theme = DungeonTheme.AncientRuins,
                Title = "The Number Grid",
                Description = $"Ancient numerals are carved into stone tiles. " +
                             $"Select tiles that sum to exactly {target}.",
                Solution = new List<string> { target.ToString() },
                CurrentState = new List<string>(),
                AvailableChoices = numbers.Select(n => n.ToString()).ToList(),
                MaxAttempts = 3 + difficulty,
                AttemptsRemaining = 3 + difficulty,
                Hints = new List<string> { $"The answer is {target}. Not all numbers are needed." },
                FailureDamagePercent = 10,
                SuccessXP = 45 * difficulty,
                CustomData = new Dictionary<string, object> { ["target"] = target }
            };
        }

        private PuzzleInstance GenerateMemoryPuzzle(int difficulty, DungeonTheme theme)
        {
            var symbols = GetThemedSymbols(theme);
            int sequenceLength = 3 + difficulty;
            var solution = new List<string>();

            for (int i = 0; i < sequenceLength; i++)
            {
                solution.Add(symbols[random.Next(symbols.Length)]);
            }

            return new PuzzleInstance
            {
                Type = PuzzleType.MemoryMatch,
                Difficulty = difficulty,
                Theme = theme,
                Title = "Memory of the Ancients",
                Description = "Glowing symbols flash before you in sequence. " +
                             "Remember and repeat the pattern exactly.",
                Solution = solution,
                CurrentState = new List<string>(),
                AvailableChoices = symbols.ToList(),
                MaxAttempts = 2 + (difficulty / 2),
                AttemptsRemaining = 2 + (difficulty / 2),
                Hints = new List<string>(),
                FailureDamagePercent = 8 + (difficulty * 3),
                SuccessXP = 55 * difficulty,
                RequiresSequence = true,
                ShowSolutionFirst = true
            };
        }

        private PuzzleInstance GenerateLightPuzzle(int difficulty, DungeonTheme theme)
        {
            int torchCount = 4 + difficulty;
            var solution = new List<string>();

            // Generate pattern (some on, some off)
            for (int i = 0; i < torchCount; i++)
            {
                solution.Add(random.NextDouble() < 0.5 ? "lit" : "unlit");
            }

            return new PuzzleInstance
            {
                Type = PuzzleType.LightDarkness,
                Difficulty = difficulty,
                Theme = theme,
                Title = "Dance of Light and Shadow",
                Description = $"Ancient torches line the walls. Some must burn, some must be dark. " +
                             "Find the pattern that pleases the spirits.",
                Solution = solution,
                CurrentState = Enumerable.Repeat("unlit", torchCount).ToList(),
                AvailableChoices = new List<string> { "toggle" },
                MaxAttempts = torchCount + difficulty,
                AttemptsRemaining = torchCount + difficulty,
                Hints = new List<string> { GenerateLightHint(solution) },
                FailureDamagePercent = 5,
                SuccessXP = 50 * difficulty
            };
        }

        private PuzzleInstance GenerateItemPuzzle(int difficulty, DungeonTheme theme)
        {
            var (item1, item2, result) = GetItemCombination(theme, difficulty);

            return new PuzzleInstance
            {
                Type = PuzzleType.ItemCombination,
                Difficulty = difficulty,
                Theme = theme,
                Title = "The Alchemist's Lock",
                Description = "A mechanism requires a specific substance to activate. " +
                             "Combine items from your surroundings to create it.",
                Solution = new List<string> { item1, item2 },
                CurrentState = new List<string>(),
                AvailableChoices = GenerateItemChoices(item1, item2, difficulty),
                MaxAttempts = 3 + difficulty,
                AttemptsRemaining = 3 + difficulty,
                Hints = new List<string> { $"You need to create {result}..." },
                FailureDamagePercent = 15 + (difficulty * 3),
                SuccessXP = 65 * difficulty,
                CustomData = new Dictionary<string, object> { ["result"] = result }
            };
        }

        private PuzzleInstance GenerateEnvironmentPuzzle(int difficulty, DungeonTheme theme)
        {
            var (description, solution, hint) = GetEnvironmentPuzzle(theme, difficulty);

            return new PuzzleInstance
            {
                Type = PuzzleType.EnvironmentChange,
                Difficulty = difficulty,
                Theme = theme,
                Title = "Elemental Challenge",
                Description = description,
                Solution = solution,
                CurrentState = new List<string>(),
                MaxAttempts = 4 + difficulty,
                AttemptsRemaining = 4 + difficulty,
                Hints = new List<string> { hint },
                FailureDamagePercent = 20 + (difficulty * 5),
                SuccessXP = 70 * difficulty,
                RequiresEnvironmentInteraction = true
            };
        }

        private PuzzleInstance GenerateReflectionPuzzle(int difficulty, DungeonTheme theme)
        {
            int mirrorCount = 3 + (difficulty / 2);
            var solution = new List<string>();

            var angles = new[] { "0", "45", "90", "135" };
            for (int i = 0; i < mirrorCount; i++)
            {
                solution.Add(angles[random.Next(angles.Length)]);
            }

            return new PuzzleInstance
            {
                Type = PuzzleType.ReflectionPuzzle,
                Difficulty = difficulty,
                Theme = theme,
                Title = "Hall of Mirrors",
                Description = $"A beam of light must be directed to a crystal. " +
                             $"Rotate the {mirrorCount} mirrors to guide it correctly.",
                Solution = solution,
                CurrentState = Enumerable.Repeat("0", mirrorCount).ToList(),
                AvailableChoices = angles.ToList(),
                MaxAttempts = mirrorCount * 2 + difficulty,
                AttemptsRemaining = mirrorCount * 2 + difficulty,
                Hints = new List<string> { "The light must reach the crystal without interruption..." },
                FailureDamagePercent = 5,
                SuccessXP = 60 * difficulty
            };
        }

        #endregion

        #region Puzzle Interaction

        /// <summary>
        /// Present a puzzle to the player and handle interaction
        /// </summary>
        public async Task<PuzzleResult> PresentPuzzle(PuzzleInstance puzzle, Character player, TerminalEmulator terminal)
        {
            terminal.Clear();
            DisplayPuzzleHeader(puzzle, terminal);

            bool solved = false;
            int totalAttempts = 0;

            while (!solved && puzzle.AttemptsRemaining > 0)
            {
                totalAttempts++;

                // Show current state
                DisplayPuzzleState(puzzle, terminal);

                // Show hints if available and player asks
                if (puzzle.Hints.Count > 0 && totalAttempts > 1)
                {
                    terminal.WriteLine($"[Type 'hint' for a clue, or 'quit' to give up]", "dark_cyan");
                }

                // Get player input based on puzzle type
                var result = await GetPuzzleInput(puzzle, terminal);

                if (result.Action == PuzzleAction.Quit)
                {
                    terminal.WriteLine("You step back from the puzzle, unable to solve it.", "yellow");
                    return new PuzzleResult { Solved = false, Fled = true };
                }

                if (result.Action == PuzzleAction.Hint)
                {
                    ShowHint(puzzle, terminal);
                    continue;
                }

                // Check the answer
                if (CheckPuzzleSolution(puzzle, result.Input))
                {
                    solved = true;
                    await DisplayPuzzleSuccess(puzzle, player, terminal);
                }
                else
                {
                    puzzle.AttemptsRemaining--;
                    await DisplayPuzzleFailure(puzzle, player, terminal);
                }
            }

            OnPuzzleCompleted?.Invoke(puzzle.Type, solved);

            return new PuzzleResult
            {
                Solved = solved,
                Attempts = totalAttempts,
                XPEarned = solved ? puzzle.SuccessXP : 0,
                DamageTaken = solved ? 0 : CalculateFailureDamage(puzzle, player)
            };
        }

        private void DisplayPuzzleHeader(PuzzleInstance puzzle, TerminalEmulator terminal)
        {
            string diffText = puzzle.Difficulty switch
            {
                1 => "Simple",
                2 => "Moderate",
                3 => "Challenging",
                4 => "Difficult",
                _ => "Legendary"
            };

            terminal.WriteLine("╔══════════════════════════════════════════════════════════════════╗", "bright_cyan");
            terminal.WriteLine($"║  {puzzle.Title.PadRight(62)}║", "bright_cyan");
            terminal.WriteLine($"║  Difficulty: {diffText.PadRight(51)}║", "cyan");
            terminal.WriteLine("╚══════════════════════════════════════════════════════════════════╝", "bright_cyan");
            terminal.WriteLine("");
            terminal.WriteLine(puzzle.Description, "white");
            terminal.WriteLine("");
        }

        private void DisplayPuzzleState(PuzzleInstance puzzle, TerminalEmulator terminal)
        {
            terminal.WriteLine($"Attempts remaining: {puzzle.AttemptsRemaining}",
                puzzle.AttemptsRemaining > 2 ? "green" : "yellow");
            terminal.WriteLine("");

            switch (puzzle.Type)
            {
                case PuzzleType.LeverSequence:
                    DisplayLeverState(puzzle, terminal);
                    break;
                case PuzzleType.SymbolAlignment:
                    DisplaySymbolState(puzzle, terminal);
                    break;
                case PuzzleType.LightDarkness:
                    DisplayLightState(puzzle, terminal);
                    break;
                case PuzzleType.NumberGrid:
                    DisplayNumberState(puzzle, terminal);
                    break;
                case PuzzleType.MemoryMatch:
                    if (puzzle.ShowSolutionFirst && puzzle.CurrentState.Count == 0)
                    {
                        DisplayMemorySequence(puzzle, terminal);
                    }
                    break;
                default:
                    DisplayGenericState(puzzle, terminal);
                    break;
            }
        }

        private void DisplayLeverState(PuzzleInstance puzzle, TerminalEmulator terminal)
        {
            int leverCount = puzzle.Solution.Count;
            terminal.WriteLine("  Levers:", "white");
            for (int i = 0; i < leverCount; i++)
            {
                bool pulled = puzzle.CurrentState.Contains(i.ToString());
                string status = pulled ? "[PULLED]" : "[------]";
                string color = pulled ? "green" : "gray";
                terminal.WriteLine($"    {i + 1}. {status}", color);
            }
            terminal.WriteLine("");
            terminal.WriteLine("Enter lever number to pull (1-" + leverCount + "):", "cyan");
        }

        private void DisplaySymbolState(PuzzleInstance puzzle, TerminalEmulator terminal)
        {
            terminal.WriteLine("  Current alignment:", "white");
            for (int i = 0; i < puzzle.CurrentState.Count; i++)
            {
                terminal.WriteLine($"    Panel {i + 1}: {puzzle.CurrentState[i]}", "gray");
            }
            terminal.WriteLine("");
            terminal.WriteLine("  Available symbols: " + string.Join(", ", puzzle.AvailableChoices), "cyan");
            terminal.WriteLine("");
            terminal.WriteLine("Enter: [panel number] [symbol] (e.g., '1 sun'):", "cyan");
        }

        private void DisplayLightState(PuzzleInstance puzzle, TerminalEmulator terminal)
        {
            terminal.WriteLine("  Torches:", "white");
            for (int i = 0; i < puzzle.CurrentState.Count; i++)
            {
                bool lit = puzzle.CurrentState[i] == "lit";
                string display = lit ? "🔥 LIT   " : "💨 UNLIT ";
                string color = lit ? "bright_yellow" : "dark_gray";
                terminal.Write($"    Torch {i + 1}: ");
                terminal.WriteLine(display, color);
            }
            terminal.WriteLine("");
            terminal.WriteLine("Enter torch number to toggle (1-" + puzzle.CurrentState.Count + "):", "cyan");
        }

        private void DisplayNumberState(PuzzleInstance puzzle, TerminalEmulator terminal)
        {
            terminal.WriteLine("  Available numbers:", "white");
            terminal.WriteLine("    " + string.Join("  ", puzzle.AvailableChoices), "bright_cyan");
            terminal.WriteLine("");

            if (puzzle.CurrentState.Count > 0)
            {
                int sum = puzzle.CurrentState.Sum(s => int.Parse(s));
                terminal.WriteLine($"  Selected: {string.Join(" + ", puzzle.CurrentState)} = {sum}", "yellow");
            }

            int target = (int)puzzle.CustomData["target"];
            terminal.WriteLine($"  Target sum: {target}", "bright_green");
            terminal.WriteLine("");
            terminal.WriteLine("Enter a number to add/remove, or 'submit' when ready:", "cyan");
        }

        private void DisplayMemorySequence(PuzzleInstance puzzle, TerminalEmulator terminal)
        {
            terminal.WriteLine("  Watch the sequence carefully!", "bright_yellow");
            terminal.WriteLine("");
            terminal.WriteLine("  " + string.Join(" -> ", puzzle.Solution), "bright_magenta");
            terminal.WriteLine("");
            terminal.WriteLine("  (The sequence will be hidden after you begin)", "gray");
            terminal.WriteLine("");
            terminal.WriteLine("Press Enter when ready to begin...", "cyan");
        }

        private void DisplayGenericState(PuzzleInstance puzzle, TerminalEmulator terminal)
        {
            if (puzzle.CurrentState.Count > 0)
            {
                terminal.WriteLine("  Current state: " + string.Join(", ", puzzle.CurrentState), "yellow");
            }
            if (puzzle.AvailableChoices.Count > 0)
            {
                terminal.WriteLine("  Options: " + string.Join(", ", puzzle.AvailableChoices), "cyan");
            }
            terminal.WriteLine("");
        }

        private async Task<PuzzleInputResult> GetPuzzleInput(PuzzleInstance puzzle, TerminalEmulator terminal)
        {
            string input = await terminal.GetInputAsync("> ");
            input = input.Trim().ToLower();

            if (input == "quit" || input == "q" || input == "leave")
                return new PuzzleInputResult { Action = PuzzleAction.Quit };

            if (input == "hint" || input == "h")
                return new PuzzleInputResult { Action = PuzzleAction.Hint };

            return new PuzzleInputResult { Action = PuzzleAction.Attempt, Input = input };
        }

        private bool CheckPuzzleSolution(PuzzleInstance puzzle, string input)
        {
            switch (puzzle.Type)
            {
                case PuzzleType.LeverSequence:
                case PuzzleType.PressurePlates:
                    return CheckSequenceSolution(puzzle, input);

                case PuzzleType.SymbolAlignment:
                    return CheckSymbolSolution(puzzle, input);

                case PuzzleType.LightDarkness:
                    return CheckLightSolution(puzzle, input);

                case PuzzleType.NumberGrid:
                    return CheckNumberSolution(puzzle, input);

                case PuzzleType.MemoryMatch:
                    return CheckMemorySolution(puzzle, input);

                case PuzzleType.ReflectionPuzzle:
                    return CheckMirrorSolution(puzzle, input);

                default:
                    return puzzle.Solution.Contains(input);
            }
        }

        private bool CheckSequenceSolution(PuzzleInstance puzzle, string input)
        {
            // Add to current sequence
            if (int.TryParse(input, out int leverNum))
            {
                leverNum--; // Convert to 0-indexed
                if (leverNum >= 0 && leverNum < puzzle.Solution.Count)
                {
                    puzzle.CurrentState.Add(leverNum.ToString());

                    // Check if sequence matches so far
                    for (int i = 0; i < puzzle.CurrentState.Count; i++)
                    {
                        if (puzzle.CurrentState[i] != puzzle.Solution[i])
                        {
                            puzzle.CurrentState.Clear(); // Reset on wrong order
                            return false;
                        }
                    }

                    // Check if complete
                    return puzzle.CurrentState.Count == puzzle.Solution.Count;
                }
            }
            return false;
        }

        private bool CheckSymbolSolution(PuzzleInstance puzzle, string input)
        {
            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 && int.TryParse(parts[0], out int panel))
            {
                panel--; // Convert to 0-indexed
                string symbol = parts[1];

                if (panel >= 0 && panel < puzzle.CurrentState.Count &&
                    puzzle.AvailableChoices.Contains(symbol))
                {
                    puzzle.CurrentState[panel] = symbol;

                    // Check if all panels match solution
                    return puzzle.CurrentState.SequenceEqual(puzzle.Solution);
                }
            }
            return false;
        }

        private bool CheckLightSolution(PuzzleInstance puzzle, string input)
        {
            if (int.TryParse(input, out int torch))
            {
                torch--; // Convert to 0-indexed
                if (torch >= 0 && torch < puzzle.CurrentState.Count)
                {
                    // Toggle
                    puzzle.CurrentState[torch] = puzzle.CurrentState[torch] == "lit" ? "unlit" : "lit";

                    // Check if matches solution
                    return puzzle.CurrentState.SequenceEqual(puzzle.Solution);
                }
            }
            return false;
        }

        private bool CheckNumberSolution(PuzzleInstance puzzle, string input)
        {
            if (input == "submit")
            {
                int sum = puzzle.CurrentState.Sum(s => int.Parse(s));
                int target = (int)puzzle.CustomData["target"];
                return sum == target;
            }

            if (int.TryParse(input, out int num) && puzzle.AvailableChoices.Contains(input))
            {
                if (puzzle.CurrentState.Contains(input))
                    puzzle.CurrentState.Remove(input);
                else
                    puzzle.CurrentState.Add(input);
            }
            return false;
        }

        private bool CheckMemorySolution(PuzzleInstance puzzle, string input)
        {
            puzzle.CurrentState.Add(input);

            // Check if matches so far
            for (int i = 0; i < puzzle.CurrentState.Count; i++)
            {
                if (!puzzle.Solution[i].Equals(puzzle.CurrentState[i], StringComparison.OrdinalIgnoreCase))
                {
                    puzzle.CurrentState.Clear();
                    return false;
                }
            }

            return puzzle.CurrentState.Count == puzzle.Solution.Count;
        }

        private bool CheckMirrorSolution(PuzzleInstance puzzle, string input)
        {
            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 && int.TryParse(parts[0], out int mirror))
            {
                mirror--;
                if (mirror >= 0 && mirror < puzzle.CurrentState.Count &&
                    puzzle.AvailableChoices.Contains(parts[1]))
                {
                    puzzle.CurrentState[mirror] = parts[1];
                    return puzzle.CurrentState.SequenceEqual(puzzle.Solution);
                }
            }
            return false;
        }

        private void ShowHint(PuzzleInstance puzzle, TerminalEmulator terminal)
        {
            if (puzzle.Hints.Count > 0)
            {
                string hint = puzzle.Hints[Math.Min(puzzle.HintsUsed, puzzle.Hints.Count - 1)];
                puzzle.HintsUsed++;
                terminal.WriteLine("");
                terminal.WriteLine("═══ HINT ═══", "bright_yellow");
                terminal.WriteLine(hint, "yellow");
                terminal.WriteLine("═════════════", "bright_yellow");
                terminal.WriteLine("");
            }
            else
            {
                terminal.WriteLine("No hints available for this puzzle.", "gray");
            }
        }

        private async Task DisplayPuzzleSuccess(PuzzleInstance puzzle, Character player, TerminalEmulator terminal)
        {
            terminal.WriteLine("");
            terminal.WriteLine("╔══════════════════════════════════════════════════════════════════╗", "bright_green");
            terminal.WriteLine("║                    P U Z Z L E   S O L V E D !                   ║", "bright_green");
            terminal.WriteLine("╚══════════════════════════════════════════════════════════════════╝", "bright_green");
            terminal.WriteLine("");

            terminal.WriteLine($"You gain {puzzle.SuccessXP} experience!", "cyan");
            player.Experience += puzzle.SuccessXP;

            // Ocean philosophy tie-in for certain puzzles
            if (puzzle.Difficulty >= 4)
            {
                terminal.WriteLine("");
                terminal.WriteLine("As the mechanism unlocks, a whisper echoes:", "bright_magenta");
                terminal.WriteLine("'The wave that stops struggling finds its way home...'", "magenta");
                OceanPhilosophySystem.Instance.CollectFragment(WaveFragment.TheForgetting);
            }

            await terminal.GetInputAsync("Press Enter to continue...");
        }

        private async Task DisplayPuzzleFailure(PuzzleInstance puzzle, Character player, TerminalEmulator terminal)
        {
            terminal.WriteLine("");
            terminal.WriteLine("The puzzle resets with a grinding sound.", "yellow");

            if (puzzle.FailureDamagePercent > 0 && puzzle.AttemptsRemaining == 0)
            {
                int damage = CalculateFailureDamage(puzzle, player);
                player.HP = Math.Max(1, player.HP - damage);
                terminal.WriteLine($"A trap triggers! You take {damage} damage!", "red");
            }

            if (puzzle.AttemptsRemaining > 0)
            {
                terminal.WriteLine($"{puzzle.AttemptsRemaining} attempts remaining.", "yellow");
            }
            else
            {
                terminal.WriteLine("You have exhausted all attempts. The puzzle remains unsolved.", "dark_red");
            }

            await Task.Delay(500);
        }

        private int CalculateFailureDamage(PuzzleInstance puzzle, Character player)
        {
            return (int)(player.MaxHP * (puzzle.FailureDamagePercent / 100.0));
        }

        #endregion

        #region Helper Methods

        private string[] GetThemedSymbols(DungeonTheme theme)
        {
            return theme switch
            {
                DungeonTheme.Catacombs => new[] { "skull", "bone", "tomb", "cross", "candle", "ghost" },
                DungeonTheme.Sewers => new[] { "rat", "water", "pipe", "grate", "slime", "drain" },
                DungeonTheme.Caverns => new[] { "crystal", "stalactite", "bat", "gem", "pool", "mushroom" },
                DungeonTheme.AncientRuins => new[] { "sun", "moon", "star", "eye", "serpent", "crown" },
                DungeonTheme.DemonLair => new[] { "pentagram", "flame", "horn", "blood", "chain", "claw" },
                DungeonTheme.FrozenDepths => new[] { "snowflake", "icicle", "frost", "wind", "glacier", "aurora" },
                DungeonTheme.VolcanicPit => new[] { "fire", "lava", "ash", "smoke", "ember", "obsidian" },
                DungeonTheme.AbyssalVoid => new[] { "void", "eye", "spiral", "tear", "wave", "infinity" },
                _ => new[] { "circle", "square", "triangle", "diamond", "star", "cross" }
            };
        }

        private string GenerateSymbolHint(List<string> solution, string[] symbols)
        {
            if (solution.Count == 0) return "Study the symbols carefully.";

            // Give hint about first or last symbol
            return random.NextDouble() < 0.5
                ? $"The sequence begins with '{solution[0]}'..."
                : $"The final symbol is '{solution[solution.Count - 1]}'...";
        }

        private string GenerateLightHint(List<string> solution)
        {
            int litCount = solution.Count(s => s == "lit");
            return $"Exactly {litCount} torches must burn.";
        }

        private (string item1, string item2, string result) GetItemCombination(DungeonTheme theme, int difficulty)
        {
            var combinations = new List<(string, string, string)>
            {
                ("water", "fire_salt", "steam"),
                ("bone_dust", "blood", "awakening_paste"),
                ("crystal_shard", "moonlight", "glowing_crystal"),
                ("sulfur", "charcoal", "flash_powder"),
                ("silver_dust", "holy_water", "blessed_silver"),
                ("shadow_essence", "light_fragment", "twilight_orb"),
                ("dragon_scale", "phoenix_ash", "eternal_flame"),
                ("void_shard", "soul_fragment", "null_essence")
            };

            int maxIndex = Math.Min(combinations.Count, 2 + difficulty);
            return combinations[random.Next(maxIndex)];
        }

        private List<string> GenerateItemChoices(string item1, string item2, int difficulty)
        {
            var choices = new List<string> { item1, item2 };
            var redHerrings = new[] { "moss", "stone", "feather", "iron_dust", "spider_silk",
                                       "mushroom_cap", "rat_tail", "candle_wax" };

            for (int i = 0; i < 2 + difficulty; i++)
            {
                var herring = redHerrings[random.Next(redHerrings.Length)];
                if (!choices.Contains(herring))
                    choices.Add(herring);
            }

            return choices.OrderBy(_ => random.Next()).ToList();
        }

        private (string desc, List<string> solution, string hint) GetEnvironmentPuzzle(DungeonTheme theme, int difficulty)
        {
            return theme switch
            {
                DungeonTheme.Caverns => (
                    "Water flows from multiple channels into a central basin. " +
                    "Redirect the flow to fill the basin without flooding the room.",
                    new List<string> { "left", "center", "right" },
                    "Only three channels lead to safety..."
                ),
                DungeonTheme.VolcanicPit => (
                    "Lava flows threaten to consume the only path forward. " +
                    "Cool the streams in the right order to create a safe crossing.",
                    new List<string> { "2", "1", "3" },
                    "Cool the closest flow last, or it will reheat..."
                ),
                DungeonTheme.FrozenDepths => (
                    "Ice blocks the only exit. You must melt precisely the right amount - " +
                    "too much and the ceiling collapses, too little and you cannot pass.",
                    new List<string> { "torch", "wall", "floor" },
                    "Heat rises... use this to your advantage."
                ),
                _ => (
                    "The environment itself seems to be a puzzle. " +
                    "Interact with the elements in the correct sequence.",
                    new List<string> { "1", "2", "3" },
                    "Observe before you act."
                )
            };
        }

        private string GetLeverPuzzleTitle(DungeonTheme theme)
        {
            return theme switch
            {
                DungeonTheme.Catacombs => "The Bone Levers",
                DungeonTheme.AncientRuins => "Mechanism of the Ancients",
                DungeonTheme.DemonLair => "Chains of Torment",
                _ => "The Lever Sequence"
            };
        }

        private string GetOrdinal(int number)
        {
            return number switch
            {
                1 => "first",
                2 => "second",
                3 => "third",
                4 => "fourth",
                5 => "fifth",
                6 => "sixth",
                7 => "seventh",
                _ => $"{number}th"
            };
        }

        #endregion
    }

    #region Puzzle Data Classes

    public class PuzzleInstance
    {
        public PuzzleType Type { get; set; }
        public int Difficulty { get; set; }
        public DungeonTheme Theme { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public List<string> Solution { get; set; } = new();
        public List<string> CurrentState { get; set; } = new();
        public List<string> AvailableChoices { get; set; } = new();
        public int MaxAttempts { get; set; }
        public int AttemptsRemaining { get; set; }
        public List<string> Hints { get; set; } = new();
        public int HintsUsed { get; set; } = 0;
        public int FailureDamagePercent { get; set; }
        public int SuccessXP { get; set; }
        public bool RequiresMovement { get; set; }
        public bool RequiresSequence { get; set; }
        public bool ShowSolutionFirst { get; set; }
        public bool RequiresEnvironmentInteraction { get; set; }
        public Dictionary<string, object> CustomData { get; set; } = new();
    }

    public class PuzzleResult
    {
        public bool Solved { get; set; }
        public bool Fled { get; set; }
        public int Attempts { get; set; }
        public int XPEarned { get; set; }
        public int DamageTaken { get; set; }
    }

    public class PuzzleInputResult
    {
        public PuzzleAction Action { get; set; }
        public string Input { get; set; } = "";
    }

    public enum PuzzleAction
    {
        Attempt,
        Hint,
        Quit
    }

    #endregion
}
