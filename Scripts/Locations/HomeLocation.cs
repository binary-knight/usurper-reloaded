using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ModelItem = global::Item;

namespace UsurperRemake.Locations;

/// <summary>
/// Player home – allows resting, item storage, viewing trophies and family.
/// Simplified port of Pascal HOME.PAS but supports core mechanics needed now.
/// </summary>
public class HomeLocation : BaseLocation
{
    // Static chest storage per player id (real name is unique key)
    private static readonly Dictionary<string, List<ModelItem>> PlayerChests = new();
    private List<ModelItem> Chest => PlayerChests[playerKey];
    private string playerKey;

    public HomeLocation() : base(GameLocation.Home, "Your Home", "Your humble abode – a safe haven to rest and prepare for adventures.")
    {
    }

    protected override void SetupLocation()
    {
        PossibleExits = new()
        {
            GameLocation.AnchorRoad
        };

        LocationActions = new()
        {
            "Rest and recover (R)",
            "Deposit item to chest (D)",
            "Withdraw item from chest (W)",
            "View stored items (L)",
            "View trophies & stats (T)",
            "View family (F)",
            "Status (S)",
            "Return (E)"
        };
    }

    public override async Task EnterLocation(Character player, TerminalEmulator term)
    {
        playerKey = (player is Player p ? p.RealName : player.Name2) ?? player.Name2;
        if (!PlayerChests.ContainsKey(playerKey))
            PlayerChests[playerKey] = new List<ModelItem>();
        await base.EnterLocation(player, term);
    }

    protected override async Task<bool> ProcessChoice(string choice)
    {
        var c = choice.Trim().ToUpperInvariant();
        switch (c)
        {
            case "R":
                await DoRest();
                return false;
            case "D":
                await DepositItem();
                return false;
            case "W":
                await WithdrawItem();
                return false;
            case "L":
                ShowChestContents();
                await terminal.WaitForKey();
                return false;
            case "T":
                ShowTrophies();
                await terminal.WaitForKey();
                return false;
            case "F":
                ShowFamily();
                await terminal.WaitForKey();
                return false;
            case "S":
                await ShowStatus();
                return false;
            case "E":
                await NavigateToLocation(GameLocation.AnchorRoad);
                return true;
            default:
                return await base.ProcessChoice(choice);
        }
    }

    private async Task DoRest()
    {
        terminal.WriteLine("You relax in your comfortable bed...", "gray");
        await Task.Delay(1500);
        currentPlayer.HP = currentPlayer.MaxHP;
        currentPlayer.Mana = currentPlayer.MaxMana;
        terminal.WriteLine("You feel completely rejuvenated!", "bright_green");
        await terminal.WaitForKey();
    }

    private async Task DepositItem()
    {
        if (!currentPlayer.Inventory.Any())
        {
            terminal.WriteLine("You have no items to store.", "yellow");
            await terminal.WaitForKey();
            return;
        }
        terminal.WriteLine("Select item number to deposit (or 0 to cancel):", "cyan");
        for (int i = 0; i < currentPlayer.Inventory.Count; i++)
        {
            terminal.WriteLine($"  {i + 1}. {currentPlayer.Inventory[i].GetDisplayName()}");
        }
        var input = await terminal.GetInput("Choice: ");
        if (int.TryParse(input, out int idx) && idx > 0 && idx <= currentPlayer.Inventory.Count)
        {
            var item = currentPlayer.Inventory[idx - 1];
            currentPlayer.Inventory.RemoveAt(idx - 1);
            Chest.Add(item);
            terminal.WriteLine($"Stored {item.GetDisplayName()} in your chest.", "green");
        }
        else
        {
            terminal.WriteLine("Cancelled.", "gray");
        }
        await terminal.WaitForKey();
    }

    private async Task WithdrawItem()
    {
        if (!Chest.Any())
        {
            terminal.WriteLine("Your chest is empty.", "yellow");
            await terminal.WaitForKey();
            return;
        }
        terminal.WriteLine("Select item number to withdraw (or 0 to cancel):", "cyan");
        for (int i = 0; i < Chest.Count; i++)
        {
            terminal.WriteLine($"  {i + 1}. {Chest[i].GetDisplayName()}");
        }
        var input = await terminal.GetInput("Choice: ");
        if (int.TryParse(input, out int idx) && idx > 0 && idx <= Chest.Count)
        {
            var item = Chest[idx - 1];
            Chest.RemoveAt(idx - 1);
            currentPlayer.Inventory.Add(item);
            terminal.WriteLine($"Retrieved {item.GetDisplayName()} from your chest.", "green");
        }
        else
        {
            terminal.WriteLine("Cancelled.", "gray");
        }
        await terminal.WaitForKey();
    }

    private void ShowChestContents()
    {
        terminal.WriteLine("\nItems in your chest:", "bright_cyan");
        if (!Chest.Any())
        {
            terminal.WriteLine("  (empty)", "gray");
        }
        else
        {
            for (int i = 0; i < Chest.Count; i++)
            {
                terminal.WriteLine($"  {i + 1}. {Chest[i].GetDisplayName()}");
            }
        }
    }

    private void ShowTrophies()
    {
        terminal.WriteLine("\nTrophies & Achievements", "bright_cyan");
        if (currentPlayer is Player p && p.Achievements.Any())
        {
            foreach (var kv in p.Achievements)
            {
                var status = kv.Value ? "✅" : "❌";
                terminal.WriteLine($" {status} {kv.Key}");
            }
        }
        else
        {
            terminal.WriteLine("  No achievements yet.");
        }
    }

    private void ShowFamily()
    {
        terminal.WriteLine("\nFamily", "bright_cyan");
        // Placeholder – until marriage/children systems done
        terminal.WriteLine("You live alone... for now.", "gray");
    }
} 