using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ModelItem = global::Item;
using UsurperRemake.Systems;

namespace UsurperRemake.Locations;

/// <summary>
/// Player home – allows resting, item storage, viewing trophies and family.
/// Simplified port of Pascal HOME.PAS but supports core mechanics needed now.
/// Now includes romance/family features.
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
            "Spend time with spouse (P)",
            "Visit bedroom (B)",
            "Status (S)",
            "Return to town (Q)"
        };
    }

    public override async Task EnterLocation(Character player, TerminalEmulator term)
    {
        playerKey = (player is Player p ? p.RealName : player.Name2) ?? player.Name2;
        if (!PlayerChests.ContainsKey(playerKey))
            PlayerChests[playerKey] = new List<ModelItem>();
        await base.EnterLocation(player, term);
    }

    protected override void DisplayLocation()
    {
        terminal.ClearScreen();

        // Header
        terminal.SetColor("bright_cyan");
        terminal.WriteLine("+==========================================================+");
        terminal.WriteLine("|                      YOUR HOME                           |");
        terminal.WriteLine("+==========================================================+");
        terminal.WriteLine("");

        // Description
        terminal.SetColor("white");
        terminal.WriteLine("You stand in the warm comfort of your home. A crackling fire");
        terminal.WriteLine("illuminates the cozy interior, and your belongings are arranged");
        terminal.WriteLine("just how you like them.");
        terminal.WriteLine("");

        // Show family info if applicable
        var romance = RomanceTracker.Instance;
        if (romance.Spouses.Count > 0 || romance.CurrentLovers.Count > 0)
        {
            terminal.SetColor("bright_magenta");
            terminal.WriteLine("Your loved ones are here with you.");
            terminal.WriteLine("");
        }

        // Menu
        ShowHomeMenu();

        // Status line
        ShowStatusLine();
    }

    private void ShowHomeMenu()
    {
        terminal.SetColor("bright_yellow");
        terminal.WriteLine("--- Home Activities ---");
        terminal.WriteLine("");

        // Row 1 - Rest & Storage
        terminal.SetColor("darkgray");
        terminal.Write(" [");
        terminal.SetColor("bright_green");
        terminal.Write("R");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.Write("est & Recover   ");

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("bright_cyan");
        terminal.Write("D");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.Write("eposit Item    ");

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("bright_cyan");
        terminal.Write("W");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.WriteLine("ithdraw Item");

        // Row 2 - View
        terminal.SetColor("darkgray");
        terminal.Write(" [");
        terminal.SetColor("yellow");
        terminal.Write("L");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.Write("ist Chest       ");

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("yellow");
        terminal.Write("T");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.Write("rophies        ");

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("yellow");
        terminal.Write("F");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.WriteLine("amily");

        // Row 3 - Romance
        terminal.SetColor("darkgray");
        terminal.Write(" [");
        terminal.SetColor("bright_magenta");
        terminal.Write("P");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.Write("artner Time     ");

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("bright_magenta");
        terminal.Write("B");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.WriteLine("edroom");

        terminal.WriteLine("");

        // Navigation
        terminal.SetColor("gray");
        terminal.WriteLine("--- Navigation ---");

        terminal.SetColor("darkgray");
        terminal.Write(" [");
        terminal.SetColor("cyan");
        terminal.Write("S");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.Write("tatus          ");

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("bright_red");
        terminal.Write("Q");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.WriteLine("uit to Main Street");

        terminal.WriteLine("");
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
                await ShowFamily();
                return false;
            case "P":
                await SpendTimeWithSpouse();
                return false;
            case "B":
                await VisitBedroom();
                return false;
            case "S":
                await ShowStatus();
                return false;
            case "Q":
            case "M": // Also allow M for Main Street
                await NavigateToLocation(GameLocation.MainStreet);
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

    private async Task ShowFamily()
    {
        terminal.WriteLine("\n", "white");
        terminal.WriteLine("╔══════════════════════════════════════╗", "bright_cyan");
        terminal.WriteLine("║            FAMILY & LOVED ONES       ║", "bright_cyan");
        terminal.WriteLine("╚══════════════════════════════════════╝", "bright_cyan");
        terminal.WriteLine();

        var romance = RomanceTracker.Instance;
        bool hasFamily = false;

        // Show spouse(s)
        if (romance.Spouses.Count > 0)
        {
            hasFamily = true;
            terminal.SetColor("bright_magenta");
            terminal.WriteLine($"  <3 SPOUSE{(romance.Spouses.Count > 1 ? "S" : "")} <3");
            terminal.SetColor("white");

            foreach (var spouse in romance.Spouses)
            {
                var npc = NPCSpawnSystem.Instance?.ActiveNPCs?.FirstOrDefault(n => n.ID == spouse.NPCId);
                var name = npc?.Name ?? spouse.NPCId;
                var marriedDays = (int)(DateTime.Now - spouse.MarriedDate).TotalDays;

                terminal.Write($"    ");
                terminal.SetColor("bright_red");
                terminal.Write("<3 ");
                terminal.SetColor("bright_white");
                terminal.Write(name);
                terminal.SetColor("gray");
                terminal.WriteLine($" - Married {marriedDays} day{(marriedDays != 1 ? "s" : "")}");

                if (spouse.Children > 0)
                {
                    terminal.SetColor("bright_yellow");
                    terminal.WriteLine($"      Children together: {spouse.Children}");
                }

                if (spouse.AcceptsPolyamory)
                {
                    terminal.SetColor("magenta");
                    terminal.WriteLine("      (Open to polyamory)");
                }
            }
            terminal.WriteLine();
        }

        // Show lovers
        if (romance.CurrentLovers.Count > 0)
        {
            hasFamily = true;
            terminal.SetColor("magenta");
            terminal.WriteLine("  LOVERS");
            terminal.SetColor("white");

            foreach (var lover in romance.CurrentLovers)
            {
                var npc = NPCSpawnSystem.Instance?.ActiveNPCs?.FirstOrDefault(n => n.ID == lover.NPCId);
                var name = npc?.Name ?? lover.NPCId;
                var daysTogether = (int)(DateTime.Now - lover.RelationshipStart).TotalDays;

                terminal.Write($"    ");
                terminal.SetColor("bright_magenta");
                terminal.Write("<3 ");
                terminal.SetColor("white");
                terminal.Write(name);
                terminal.SetColor("gray");
                terminal.Write($" - Together {daysTogether} day{(daysTogether != 1 ? "s" : "")}");

                if (lover.IsExclusive)
                {
                    terminal.SetColor("bright_cyan");
                    terminal.Write(" [Exclusive]");
                }
                terminal.WriteLine();
            }
            terminal.WriteLine();
        }

        // Show friends with benefits
        if (romance.FriendsWithBenefits.Count > 0)
        {
            hasFamily = true;
            terminal.SetColor("cyan");
            terminal.WriteLine("  FRIENDS WITH BENEFITS");
            terminal.SetColor("white");

            foreach (var fwbId in romance.FriendsWithBenefits)
            {
                var npc = NPCSpawnSystem.Instance?.ActiveNPCs?.FirstOrDefault(n => n.ID == fwbId);
                var name = npc?.Name ?? fwbId;
                terminal.WriteLine($"    ~ {name}");
            }
            terminal.WriteLine();
        }

        // Show children (from all spouses)
        int totalChildren = romance.Spouses.Sum(s => s.Children);
        if (totalChildren > 0 || currentPlayer.Children > 0)
        {
            hasFamily = true;
            int childCount = Math.Max(totalChildren, currentPlayer.Children);
            terminal.SetColor("bright_yellow");
            terminal.WriteLine($"  CHILDREN: {childCount}");
            terminal.SetColor("gray");
            terminal.WriteLine("    (Visit Love Corner to see detailed child information)");
            terminal.WriteLine();
        }

        // Show exes
        if (romance.Exes.Count > 0)
        {
            terminal.SetColor("dark_gray");
            terminal.WriteLine($"  PAST RELATIONSHIPS: {romance.Exes.Count}");
            terminal.SetColor("gray");
            foreach (var exId in romance.Exes.Take(5)) // Show max 5
            {
                var npc = NPCSpawnSystem.Instance?.ActiveNPCs?.FirstOrDefault(n => n.ID == exId);
                var name = npc?.Name ?? exId;
                terminal.WriteLine($"    - {name}");
            }
            if (romance.Exes.Count > 5)
            {
                terminal.WriteLine($"    ... and {romance.Exes.Count - 5} more");
            }
            terminal.WriteLine();
        }

        if (!hasFamily)
        {
            terminal.SetColor("gray");
            terminal.WriteLine("  You live alone... for now.");
            terminal.WriteLine();
            terminal.SetColor("bright_cyan");
            terminal.WriteLine("  Tip: Meet someone special at Main Street or Love Corner!");
        }

        terminal.SetColor("white");
        await terminal.WaitForKey();
    }

    private async Task SpendTimeWithSpouse()
    {
        var romance = RomanceTracker.Instance;

        if (romance.Spouses.Count == 0 && romance.CurrentLovers.Count == 0)
        {
            terminal.WriteLine("\nYou have no spouse or lover to spend time with.", "yellow");
            terminal.WriteLine("Perhaps you should get out there and meet someone?", "gray");
            await terminal.WaitForKey();
            return;
        }

        terminal.WriteLine("\n", "white");
        terminal.SetColor("bright_magenta");
        terminal.WriteLine("Who would you like to spend time with?");
        terminal.WriteLine();

        var options = new List<(string id, string name, string type)>();

        foreach (var spouse in romance.Spouses)
        {
            var npc = NPCSpawnSystem.Instance?.ActiveNPCs?.FirstOrDefault(n => n.ID == spouse.NPCId);
            var name = npc?.Name ?? spouse.NPCId;
            options.Add((spouse.NPCId, name, "spouse"));
        }

        foreach (var lover in romance.CurrentLovers)
        {
            var npc = NPCSpawnSystem.Instance?.ActiveNPCs?.FirstOrDefault(n => n.ID == lover.NPCId);
            var name = npc?.Name ?? lover.NPCId;
            options.Add((lover.NPCId, name, "lover"));
        }

        terminal.SetColor("white");
        for (int i = 0; i < options.Count; i++)
        {
            var opt = options[i];
            terminal.Write($"  [{i + 1}] ");
            terminal.SetColor(opt.type == "spouse" ? "bright_red" : "bright_magenta");
            terminal.Write($"<3 {opt.name}");
            terminal.SetColor("gray");
            terminal.WriteLine($" ({opt.type})");
        }
        terminal.SetColor("gray");
        terminal.WriteLine("  [0] Cancel");
        terminal.WriteLine();

        var input = await terminal.GetInput("Choice: ");
        if (!int.TryParse(input, out int choice) || choice < 1 || choice > options.Count)
        {
            terminal.WriteLine("Cancelled.", "gray");
            await terminal.WaitForKey();
            return;
        }

        var selected = options[choice - 1];
        var selectedNpc = NPCSpawnSystem.Instance?.ActiveNPCs?.FirstOrDefault(n => n.ID == selected.id);

        if (selectedNpc == null)
        {
            terminal.WriteLine($"{selected.name} is not available right now.", "yellow");
            await terminal.WaitForKey();
            return;
        }

        await SpendQualityTime(selectedNpc, selected.type);
    }

    private async Task SpendQualityTime(NPC partner, string relationType)
    {
        terminal.WriteLine("\n", "white");
        terminal.SetColor("bright_magenta");
        terminal.WriteLine($"You spend quality time with {partner.Name}...");
        terminal.WriteLine();

        terminal.SetColor("white");
        terminal.WriteLine("  [1] Have a romantic dinner together");
        terminal.WriteLine("  [2] Take a walk and hold hands");
        terminal.WriteLine("  [3] Cuddle by the fire");
        terminal.WriteLine("  [4] Have a deep conversation");
        if (relationType == "spouse")
        {
            terminal.SetColor("bright_red");
            terminal.WriteLine("  [5] Retire to the bedroom...");
        }
        terminal.SetColor("gray");
        terminal.WriteLine("  [0] Cancel");
        terminal.WriteLine();

        var input = await terminal.GetInput("Choice: ");
        if (!int.TryParse(input, out int choice) || choice < 1)
        {
            terminal.WriteLine("You decide to spend time alone.", "gray");
            await terminal.WaitForKey();
            return;
        }

        terminal.WriteLine();

        switch (choice)
        {
            case 1: // Romantic dinner
                terminal.SetColor("bright_yellow");
                terminal.WriteLine($"You prepare a lovely dinner for {partner.Name}.");
                terminal.SetColor("white");
                terminal.WriteLine("The candlelight flickers as you share stories and laughter.");
                terminal.WriteLine($"{partner.Name} gazes at you with affection.");

                // XP bonus for married couples
                if (relationType == "spouse")
                {
                    long xpBonus = currentPlayer.Level * 50;
                    currentPlayer.Experience += xpBonus;
                    terminal.SetColor("bright_green");
                    terminal.WriteLine($"Your bond strengthens! (+{xpBonus} XP)");
                }
                break;

            case 2: // Walk and hold hands
                terminal.SetColor("cyan");
                terminal.WriteLine($"You and {partner.Name} walk hand in hand through the garden.");
                terminal.SetColor("white");
                terminal.WriteLine("The evening air is cool and refreshing.");
                terminal.WriteLine($"{partner.Name} rests their head on your shoulder.");

                // Small HP recovery from relaxation
                currentPlayer.HP = Math.Min(currentPlayer.HP + currentPlayer.MaxHP / 20, currentPlayer.MaxHP);
                terminal.SetColor("bright_green");
                terminal.WriteLine("The peaceful moment restores you slightly.");
                break;

            case 3: // Cuddle by fire
                terminal.SetColor("bright_red");
                terminal.WriteLine("You settle by the crackling fire together.");
                terminal.SetColor("white");
                terminal.WriteLine($"{partner.Name} nestles close to you for warmth.");
                terminal.WriteLine("You feel utterly at peace in this moment.");

                // Mana recovery from emotional connection
                currentPlayer.Mana = Math.Min(currentPlayer.Mana + currentPlayer.MaxMana / 10, currentPlayer.MaxMana);
                terminal.SetColor("bright_blue");
                terminal.WriteLine("Your spiritual connection is renewed.");
                break;

            case 4: // Deep conversation
                await VisualNovelDialogueSystem.Instance.StartConversation(currentPlayer, partner, terminal);
                return; // Already handled

            case 5: // Bedroom (spouse only)
                if (relationType == "spouse")
                {
                    await IntimacySystem.Instance.InitiateIntimateScene(currentPlayer, partner, terminal);
                    return;
                }
                terminal.WriteLine("Invalid choice.", "gray");
                break;

            default:
                terminal.WriteLine("Invalid choice.", "gray");
                break;
        }

        await terminal.WaitForKey();
    }

    private async Task VisitBedroom()
    {
        var romance = RomanceTracker.Instance;

        terminal.WriteLine("\n", "white");
        terminal.SetColor("bright_magenta");
        terminal.WriteLine("═══════════════════════════════════════");
        terminal.WriteLine("         THE MASTER BEDROOM");
        terminal.WriteLine("═══════════════════════════════════════");
        terminal.WriteLine();

        if (romance.Spouses.Count == 0 && romance.CurrentLovers.Count == 0)
        {
            terminal.SetColor("gray");
            terminal.WriteLine("Your bed seems cold and empty...");
            terminal.WriteLine("Perhaps you should find someone special to share it with.");
            await terminal.WaitForKey();
            return;
        }

        // Check if spouse is home
        var availablePartners = new List<NPC>();

        foreach (var spouse in romance.Spouses)
        {
            var npc = NPCSpawnSystem.Instance?.ActiveNPCs?.FirstOrDefault(n => n.ID == spouse.NPCId);
            if (npc != null && npc.CurrentLocation == "Your Home")
            {
                availablePartners.Add(npc);
            }
        }

        foreach (var lover in romance.CurrentLovers)
        {
            var npc = NPCSpawnSystem.Instance?.ActiveNPCs?.FirstOrDefault(n => n.ID == lover.NPCId);
            if (npc != null && npc.CurrentLocation == "Your Home")
            {
                availablePartners.Add(npc);
            }
        }

        if (availablePartners.Count == 0)
        {
            terminal.SetColor("yellow");
            terminal.WriteLine("Your partner isn't home right now.");
            terminal.SetColor("gray");
            terminal.WriteLine("They might be at Main Street or elsewhere in town.");
            await terminal.WaitForKey();
            return;
        }

        if (availablePartners.Count == 1)
        {
            var partner = availablePartners[0];
            terminal.SetColor("bright_magenta");
            terminal.WriteLine($"{partner.Name} is here, looking inviting...");
            terminal.WriteLine();
            terminal.SetColor("white");
            terminal.WriteLine($"  [1] Join {partner.Name} in bed");
            terminal.WriteLine("  [0] Leave the bedroom");

            var input = await terminal.GetInput("Choice: ");
            if (input == "1")
            {
                await IntimacySystem.Instance.InitiateIntimateScene(currentPlayer, partner, terminal);
            }
            else
            {
                terminal.WriteLine("You quietly leave the bedroom.", "gray");
                await terminal.WaitForKey();
            }
        }
        else
        {
            // Multiple partners available
            terminal.SetColor("bright_magenta");
            terminal.WriteLine("Multiple partners are here waiting for you...");
            terminal.WriteLine();

            for (int i = 0; i < availablePartners.Count; i++)
            {
                terminal.SetColor("white");
                terminal.WriteLine($"  [{i + 1}] {availablePartners[i].Name}");
            }
            terminal.SetColor("gray");
            terminal.WriteLine("  [0] Leave the bedroom");

            var input = await terminal.GetInput("Choice: ");
            if (int.TryParse(input, out int choice) && choice >= 1 && choice <= availablePartners.Count)
            {
                await IntimacySystem.Instance.InitiateIntimateScene(currentPlayer, availablePartners[choice - 1], terminal);
            }
            else
            {
                terminal.WriteLine("You quietly leave the bedroom.", "gray");
                await terminal.WaitForKey();
            }
        }
    }
} 