using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UsurperRemake.Locations;

/// <summary>
/// Dormitory – rest & recovery hub (Pascal DORM.PAS minimal port).
/// Offers:
///   L – List sleepers   E – Examine sleeper   G – Go to sleep (daily reset)
///   W – Wake the guests (dangerous fist-fight light version)
///   S – Status          R – Return to Anchor Road
/// </summary>
public class DormitoryLocation : BaseLocation
{
    private List<NPC> sleepers = new();
    private readonly Random rng = new();

    public DormitoryLocation() : base(GameLocation.Dormitory,
        "Dormitory",
        "Rows of squeaky wooden bunks line the walls; weary adventurers snore under thin blankets.")
    {
    }

    protected override void SetupLocation()
    {
        PossibleExits.Add(GameLocation.AnchorRoad);
        PossibleExits.Add(GameLocation.MainStreet);
    }

    protected override void DisplayLocation()
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_yellow");
        terminal.WriteLine("-*- Dormitory -*-");
        terminal.SetColor("white");
        terminal.WriteLine("The warm, stale air is thick with the smell of sweat and cheap ale.");
        terminal.WriteLine("");
        terminal.WriteLine("(L) List sleepers    (E) Examine    (G) Go to sleep");
        terminal.WriteLine("(W) Wake the guests  (S) Status     (R) Return");
        terminal.WriteLine("");
        ShowStatusLine();
    }

    protected override async Task<bool> ProcessChoice(string choice)
    {
        // Handle global quick commands first
        var (handled, shouldExit) = await TryProcessGlobalCommand(choice);
        if (handled) return shouldExit;

        if (string.IsNullOrWhiteSpace(choice)) return false;
        char ch = char.ToUpperInvariant(choice.Trim()[0]);

        switch (ch)
        {
            case 'L':
                await ListSleepers();
                return false;
            case 'E':
                await ExamineSleeper();
                return false;
            case 'G':
                await GoToSleep();
                return true; // exits via navigation
            case 'W':
                await WakeGuests();
                return false;
            case 'S':
                await ShowStatus();
                return false;
            case 'R':
                await NavigateToLocation(GameLocation.AnchorRoad);
                return true;
            default:
                return false;
        }
    }

    #region Helper Methods

    private void RefreshSleepers()
    {
        sleepers = LocationManager.Instance.GetNPCsInLocation(GameLocation.Dormitory)
                    .Where(n => n.IsAlive)
                    .ToList();

        // Populate with wanderers if empty
        if (sleepers.Count < 4)
        {
            foreach (var npc in GameEngine.Instance.GetNPCsInLocation(GameLocation.MainStreet))
            {
                if (sleepers.Count >= 8) break;
                if (rng.NextDouble() < 0.05)
                {
                    LocationManager.Instance.RemoveNPCFromLocation(GameLocation.MainStreet, npc);
                    LocationManager.Instance.AddNPCToLocation(GameLocation.Dormitory, npc);
                    npc.UpdateLocation("dormitory");
                    sleepers.Add(npc);
                }
            }
        }
    }

    private async Task ListSleepers()
    {
        RefreshSleepers();
        terminal.ClearScreen();
        terminal.SetColor("bright_cyan");
        terminal.WriteLine("Sleeping Guests");
        terminal.SetColor("cyan");
        if (sleepers.Count == 0)
        {
            terminal.WriteLine("No one is asleep right now.");
        }
        else
        {
            int idx = 1;
            foreach (var npc in sleepers)
            {
                terminal.WriteLine($"{idx,3}. {npc.Name2} (Lvl {npc.Level})");
                idx++;
            }
        }
        terminal.WriteLine("\nPress Enter...");
        await terminal.WaitForKeyPress();
    }

    private async Task ExamineSleeper()
    {
        RefreshSleepers();
        if (sleepers.Count == 0)
        {
            terminal.WriteLine("Nobody to examine.", "gray");
            await Task.Delay(1500);
            return;
        }
        var input = await terminal.GetInput("Enter sleeper number or name: ");
        NPC? npc = null;
        if (int.TryParse(input, out int num) && num >= 1 && num <= sleepers.Count)
            npc = sleepers[num - 1];
        else
            npc = sleepers.FirstOrDefault(n => n.Name2.Equals(input, StringComparison.OrdinalIgnoreCase));

        if (npc == null)
        {
            terminal.WriteLine("No such sleeper.", "red");
            await Task.Delay(1500);
            return;
        }

        terminal.ClearScreen();
        terminal.SetColor("bright_yellow");
        terminal.WriteLine(npc.Name2);
        terminal.SetColor("yellow");
        terminal.WriteLine(new string('═', npc.Name2.Length));
        terminal.SetColor("white");
        terminal.WriteLine(npc.GetDisplayInfo());
        terminal.WriteLine("\nPress Enter...");
        await terminal.WaitForKeyPress();
    }

    private async Task GoToSleep()
    {
        var confirm = await terminal.GetInput("Stay here for the night? (y/N): ");
        if (!confirm.Equals("Y", StringComparison.OrdinalIgnoreCase))
            return;

        terminal.ClearScreen();
        terminal.SetColor("white");
        terminal.WriteLine("You claim a free bunk and drift into uneasy sleep...");
        await Task.Delay(1500);

        // Restore HP/Mana/Stamina
        currentPlayer.HP = currentPlayer.MaxHP;
        currentPlayer.Mana = currentPlayer.MaxMana;
        currentPlayer.Stamina = Math.Max(currentPlayer.Stamina, currentPlayer.Constitution * 2);

        // Forced daily reset
        await DailySystemManager.Instance.ForceDailyReset();

        terminal.WriteLine("You awaken refreshed, ready for a new day of adventure.", "green");
        await Task.Delay(1500);

        // After rest, return to Anchor Road (classic)
        await NavigateToLocation(GameLocation.AnchorRoad);
    }

    private async Task WakeGuests()
    {
        if (currentPlayer.DarkNr <= 0)
        {
            terminal.WriteLine("You feel too righteous to cause such mischief today.", "yellow");
            await Task.Delay(1500);
            return;
        }

        RefreshSleepers();
        if (sleepers.Count == 0)
        {
            terminal.WriteLine("There is no one to disturb.", "gray");
            await Task.Delay(1500);
            return;
        }

        terminal.WriteLine("You let out a thunderous shout!", "yellow");
        await Task.Delay(1000);
        currentPlayer.Darkness += 10; // a bit darker
        currentPlayer.DarkNr -= 1;

        // Select 1-3 random sleepers to fight
        var angry = sleepers.OrderBy(_ => rng.Next()).Take(rng.Next(1, Math.Min(3, sleepers.Count))).ToList();
        var combatEngine = new CombatEngine(terminal);

        foreach (var npc in angry)
        {
            if (!currentPlayer.IsAlive) break;
            terminal.WriteLine($"{npc.Name2} wakes up furious and attacks you!", "red");
            await Task.Delay(1000);

            var result = await combatEngine.PlayerVsPlayer(currentPlayer, npc);
            if (!currentPlayer.IsAlive)
            {
                terminal.WriteLine("You were knocked out!", "red");
                break;
            }
            else
            {
                terminal.WriteLine($"You subdued {npc.Name2}.", "green");
                npc.HP = Math.Max(1, npc.HP - 10);
            }
        }
        await terminal.WaitForKeyPress();
    }
    #endregion
} 