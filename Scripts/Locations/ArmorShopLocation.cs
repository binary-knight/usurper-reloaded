using UsurperRemake.Utils;
using UsurperRemake.Systems;
using UsurperRemake;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Armor Shop Location - Modern RPG slot-based armor system
/// Sells armor pieces for each body slot (Head, Body, Arms, Hands, Legs, Feet, Waist, Face, Cloak)
/// </summary>
public class ArmorShopLocation : BaseLocation
{
    private string shopkeeperName = "Reese";
    private EquipmentSlot? currentSlotCategory = null;
    private int currentPage = 0;
    private const int ItemsPerPage = 15;

    // Armor slots sold in this shop
    private static readonly EquipmentSlot[] ArmorSlots = new[]
    {
        EquipmentSlot.Head,
        EquipmentSlot.Body,
        EquipmentSlot.Arms,
        EquipmentSlot.Hands,
        EquipmentSlot.Legs,
        EquipmentSlot.Feet,
        EquipmentSlot.Waist,
        EquipmentSlot.Face,
        EquipmentSlot.Cloak
    };

    public ArmorShopLocation() : base(
        GameLocation.ArmorShop,
        "Armor Shop",
        "You enter the armor shop and notice a strange but appealing smell."
    ) { }

    protected override void SetupLocation()
    {
        base.SetupLocation();
        shopkeeperName = "Reese";
    }

    protected override void DisplayLocation()
    {
        terminal.ClearScreen();

        if (currentPlayer == null) return;

        // Check if player has been kicked out for bad haggling
        if (currentPlayer.ArmHag < 1)
        {
            terminal.SetColor("bright_red");
            terminal.WriteLine("The strong desk-clerks throw you out!");
            terminal.WriteLine("You realize that you went a little bit too far in");
            terminal.WriteLine("your attempts to get a good deal.");
            terminal.WriteLine("");
            terminal.WriteLine("Press any key to return to street...", "yellow");
            return;
        }

        terminal.SetColor("bright_blue");
        terminal.WriteLine("════════════════════════════════════════════════════════");
        terminal.WriteLine($"    Armorshop, run by {shopkeeperName} the elf");
        terminal.WriteLine("════════════════════════════════════════════════════════");
        terminal.WriteLine("");

        if (currentSlotCategory.HasValue)
        {
            // Show items for the selected slot
            ShowSlotItems(currentSlotCategory.Value);
        }
        else
        {
            // Show main menu with slot categories
            ShowMainMenu();
        }
    }

    private void ShowMainMenu()
    {
        terminal.SetColor("white");
        terminal.WriteLine("As you enter the store you notice a strange but appealing smell.");
        terminal.SetColor("bright_green");
        terminal.Write(shopkeeperName);
        terminal.SetColor("white");
        terminal.WriteLine(" appears with a smile: \"What armor piece interests you today?\"");
        terminal.WriteLine("");

        terminal.SetColor("white");
        terminal.Write("You have ");
        terminal.SetColor("yellow");
        terminal.Write(FormatNumber(currentPlayer.Gold));
        terminal.SetColor("white");
        terminal.WriteLine(" gold crowns.");

        // Show alignment price modifier
        var alignmentModifier = AlignmentSystem.Instance.GetPriceModifier(currentPlayer, isShadyShop: false);
        if (alignmentModifier != 1.0f)
        {
            var (alignText, alignColor) = AlignmentSystem.Instance.GetAlignmentDisplay(currentPlayer);
            terminal.SetColor(alignColor);
            if (alignmentModifier < 1.0f)
                terminal.WriteLine($"  Your {alignText} alignment grants you a {(int)((1.0f - alignmentModifier) * 100)}% discount!");
            else
                terminal.WriteLine($"  Your {alignText} alignment causes a {(int)((alignmentModifier - 1.0f) * 100)}% markup.");
        }

        // Show world event price modifier
        var worldEventModifier = WorldEventSystem.Instance.GlobalPriceModifier;
        if (Math.Abs(worldEventModifier - 1.0f) > 0.01f)
        {
            if (worldEventModifier < 1.0f)
            {
                terminal.SetColor("bright_green");
                terminal.WriteLine($"  World Events: {(int)((1.0f - worldEventModifier) * 100)}% discount active!");
            }
            else
            {
                terminal.SetColor("red");
                terminal.WriteLine($"  World Events: {(int)((worldEventModifier - 1.0f) * 100)}% price increase!");
            }
        }
        terminal.WriteLine("");

        // Show current equipment summary
        ShowEquippedArmor();
        terminal.WriteLine("");

        terminal.SetColor("cyan");
        terminal.WriteLine("Select an armor category:");
        terminal.WriteLine("");

        int num = 1;
        foreach (var slot in ArmorSlots)
        {
            var currentItem = currentPlayer.GetEquipment(slot);
            terminal.SetColor("darkgray");
            terminal.Write("[");
            terminal.SetColor("bright_yellow");
            terminal.Write($"{num}");
            terminal.SetColor("darkgray");
            terminal.Write("] ");
            terminal.SetColor("white");
            terminal.Write($"{slot.GetDisplayName().PadRight(12)}");

            if (currentItem != null)
            {
                terminal.SetColor("gray");
                terminal.Write(" - ");
                terminal.SetColor("bright_cyan");
                terminal.Write($"{currentItem.Name}");
                terminal.SetColor("gray");
                terminal.Write($" (AC:{currentItem.ArmorClass})");
            }
            else
            {
                terminal.SetColor("darkgray");
                terminal.Write(" - Empty");
            }
            terminal.WriteLine("");
            num++;
        }

        terminal.WriteLine("");

        // Sell option
        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("bright_green");
        terminal.Write("S");
        terminal.SetColor("darkgray");
        terminal.Write("] ");
        terminal.SetColor("white");
        terminal.WriteLine("ell equipped armor");

        // Auto-equip option
        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("bright_magenta");
        terminal.Write("A");
        terminal.SetColor("darkgray");
        terminal.Write("] ");
        terminal.SetColor("magenta");
        terminal.WriteLine($"uto-buy best affordable armor for all slots");

        terminal.WriteLine("");
        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("bright_red");
        terminal.Write("R");
        terminal.SetColor("darkgray");
        terminal.Write("] ");
        terminal.SetColor("red");
        terminal.WriteLine("eturn to street");
        terminal.WriteLine("");
    }

    private void ShowEquippedArmor()
    {
        terminal.SetColor("cyan");
        terminal.WriteLine("Current Armor:");

        long totalAC = 0;
        foreach (var slot in ArmorSlots)
        {
            var item = currentPlayer.GetEquipment(slot);
            if (item != null)
            {
                totalAC += item.ArmorClass;
            }
        }

        terminal.SetColor("white");
        terminal.Write("Total Armor Class: ");
        terminal.SetColor("bright_cyan");
        terminal.WriteLine($"{totalAC}");
    }

    private void ShowSlotItems(EquipmentSlot slot)
    {
        var items = EquipmentDatabase.GetBySlot(slot)
            .OrderBy(i => i.Value)
            .ToList();

        var currentItem = currentPlayer.GetEquipment(slot);

        terminal.SetColor("bright_yellow");
        terminal.WriteLine($"═══ {slot.GetDisplayName()} Armor ═══");
        terminal.WriteLine("");

        if (currentItem != null)
        {
            terminal.SetColor("cyan");
            terminal.Write("Currently Equipped: ");
            terminal.SetColor("bright_white");
            terminal.Write($"{currentItem.Name}");
            terminal.SetColor("gray");
            terminal.WriteLine($" (AC: {currentItem.ArmorClass}, Value: {FormatNumber(currentItem.Value)})");
            terminal.WriteLine("");
        }

        // Paginate items
        int startIndex = currentPage * ItemsPerPage;
        var pageItems = items.Skip(startIndex).Take(ItemsPerPage).ToList();
        int totalPages = (items.Count + ItemsPerPage - 1) / ItemsPerPage;

        terminal.SetColor("gray");
        terminal.WriteLine($"Page {currentPage + 1}/{totalPages} - {items.Count} items total");
        terminal.WriteLine("");

        terminal.SetColor("bright_blue");
        terminal.WriteLine("  #   Name                          AC   Price       Bonus");
        terminal.SetColor("darkgray");
        terminal.WriteLine("─────────────────────────────────────────────────────────────");

        int num = 1;
        foreach (var item in pageItems)
        {
            bool canAfford = currentPlayer.Gold >= item.Value;
            bool isUpgrade = currentItem == null || item.ArmorClass > currentItem.ArmorClass;

            terminal.SetColor(canAfford ? "bright_cyan" : "darkgray");
            terminal.Write($"{num,3}. ");

            terminal.SetColor(canAfford ? "white" : "darkgray");
            terminal.Write($"{item.Name,-28}");

            terminal.SetColor(canAfford ? "bright_cyan" : "darkgray");
            terminal.Write($"{item.ArmorClass,4}  ");

            terminal.SetColor(canAfford ? "yellow" : "darkgray");
            terminal.Write($"{FormatNumber(item.Value),10}  ");

            // Show bonus stats
            var bonuses = GetBonusDescription(item);
            if (!string.IsNullOrEmpty(bonuses))
            {
                terminal.SetColor(canAfford ? "green" : "darkgray");
                terminal.Write(bonuses);
            }

            // Show upgrade indicator
            if (isUpgrade && canAfford)
            {
                terminal.SetColor("bright_green");
                terminal.Write(" ↑");
            }
            else if (!isUpgrade && currentItem != null)
            {
                terminal.SetColor("red");
                terminal.Write(" ↓");
            }

            terminal.WriteLine("");
            num++;
        }

        terminal.WriteLine("");

        // Navigation
        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("bright_yellow");
        terminal.Write("#");
        terminal.SetColor("darkgray");
        terminal.Write("] ");
        terminal.SetColor("white");
        terminal.Write("Buy item   ");

        if (currentPage > 0)
        {
            terminal.SetColor("darkgray");
            terminal.Write("[");
            terminal.SetColor("bright_cyan");
            terminal.Write("P");
            terminal.SetColor("darkgray");
            terminal.Write("] Previous   ");
        }

        if (currentPage < totalPages - 1)
        {
            terminal.SetColor("darkgray");
            terminal.Write("[");
            terminal.SetColor("bright_cyan");
            terminal.Write("N");
            terminal.SetColor("darkgray");
            terminal.Write("] Next   ");
        }

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("bright_red");
        terminal.Write("B");
        terminal.SetColor("darkgray");
        terminal.Write("] ");
        terminal.SetColor("red");
        terminal.WriteLine("Back");
        terminal.WriteLine("");
    }

    private string GetBonusDescription(Equipment item)
    {
        var bonuses = new List<string>();

        if (item.StrengthBonus != 0) bonuses.Add($"Str{(item.StrengthBonus > 0 ? "+" : "")}{item.StrengthBonus}");
        if (item.DexterityBonus != 0) bonuses.Add($"Dex{(item.DexterityBonus > 0 ? "+" : "")}{item.DexterityBonus}");
        if (item.ConstitutionBonus != 0) bonuses.Add($"Con{(item.ConstitutionBonus > 0 ? "+" : "")}{item.ConstitutionBonus}");
        if (item.IntelligenceBonus != 0) bonuses.Add($"Int{(item.IntelligenceBonus > 0 ? "+" : "")}{item.IntelligenceBonus}");
        if (item.WisdomBonus != 0) bonuses.Add($"Wis{(item.WisdomBonus > 0 ? "+" : "")}{item.WisdomBonus}");
        if (item.MaxHPBonus != 0) bonuses.Add($"HP+{item.MaxHPBonus}");
        if (item.MaxManaBonus != 0) bonuses.Add($"MP+{item.MaxManaBonus}");
        if (item.MagicResistance != 0) bonuses.Add($"MR{item.MagicResistance}%");

        return string.Join(" ", bonuses.Take(3)); // Limit to 3 to fit
    }

    protected override async Task<bool> ProcessChoice(string choice)
    {
        if (currentPlayer == null) return true;

        if (currentPlayer.ArmHag < 1)
        {
            await NavigateToLocation(GameLocation.MainStreet);
            return true;
        }

        var upperChoice = choice.ToUpper().Trim();

        // In slot view
        if (currentSlotCategory.HasValue)
        {
            return await ProcessSlotChoice(upperChoice);
        }

        // In main menu
        switch (upperChoice)
        {
            case "R":
                await NavigateToLocation(GameLocation.MainStreet);
                return true;

            case "S":
                await SellArmor();
                return false;

            case "A":
                await AutoBuyBestArmor();
                return false;

            case "?":
                DisplayLocation();
                return false;

            default:
                // Try to parse as slot number
                if (int.TryParse(upperChoice, out int slotNum) && slotNum >= 1 && slotNum <= ArmorSlots.Length)
                {
                    currentSlotCategory = ArmorSlots[slotNum - 1];
                    currentPage = 0;
                    return false;
                }

                terminal.WriteLine("Invalid choice!", "red");
                await Task.Delay(1000);
                return false;
        }
    }

    private async Task<bool> ProcessSlotChoice(string choice)
    {
        switch (choice)
        {
            case "B":
                currentSlotCategory = null;
                currentPage = 0;
                return false;

            case "P":
                if (currentPage > 0) currentPage--;
                return false;

            case "N":
                var items = EquipmentDatabase.GetBySlot(currentSlotCategory.Value);
                int totalPages = (items.Count + ItemsPerPage - 1) / ItemsPerPage;
                if (currentPage < totalPages - 1) currentPage++;
                return false;

            default:
                // Try to parse as item number
                if (int.TryParse(choice, out int itemNum) && itemNum >= 1)
                {
                    await BuyItem(currentSlotCategory.Value, itemNum);
                }
                return false;
        }
    }

    private async Task BuyItem(EquipmentSlot slot, int itemIndex)
    {
        var items = EquipmentDatabase.GetBySlot(slot)
            .OrderBy(i => i.Value)
            .ToList();

        int actualIndex = currentPage * ItemsPerPage + itemIndex - 1;
        if (actualIndex < 0 || actualIndex >= items.Count)
        {
            terminal.WriteLine("Invalid item number!", "red");
            await Task.Delay(1000);
            return;
        }

        var item = items[actualIndex];

        // Apply alignment and world event price modifiers
        var alignmentModifier = AlignmentSystem.Instance.GetPriceModifier(currentPlayer, isShadyShop: false);
        var worldEventModifier = WorldEventSystem.Instance.GlobalPriceModifier;
        var totalModifier = alignmentModifier * worldEventModifier;
        long adjustedPrice = (long)(item.Value * totalModifier);

        if (currentPlayer.Gold < adjustedPrice)
        {
            terminal.WriteLine("");
            terminal.SetColor("red");
            terminal.WriteLine($"You need {FormatNumber(adjustedPrice)} gold but only have {FormatNumber(currentPlayer.Gold)}!");
            await Pause();
            return;
        }

        // Check alignment requirements
        if (item.RequiresGood && currentPlayer.Chivalry <= currentPlayer.Darkness)
        {
            terminal.WriteLine("");
            terminal.SetColor("red");
            terminal.WriteLine($"{item.Name} requires a good alignment!");
            await Pause();
            return;
        }

        if (item.RequiresEvil && currentPlayer.Darkness <= currentPlayer.Chivalry)
        {
            terminal.WriteLine("");
            terminal.SetColor("red");
            terminal.WriteLine($"{item.Name} requires an evil alignment!");
            await Pause();
            return;
        }

        terminal.WriteLine("");
        terminal.SetColor("white");
        terminal.Write($"Buy {item.Name} for ");
        terminal.SetColor("yellow");
        terminal.Write(FormatNumber(adjustedPrice));
        terminal.SetColor("white");
        if (Math.Abs(totalModifier - 1.0f) > 0.01f)
        {
            terminal.SetColor("gray");
            terminal.Write($" (was {FormatNumber(item.Value)})");
            terminal.SetColor("white");
        }
        terminal.Write(" gold? (Y/N): ");

        var confirm = await terminal.GetInput("");
        if (confirm.ToUpper() != "Y")
        {
            return;
        }

        // Process purchase
        currentPlayer.Gold -= adjustedPrice;

        // Equip the item (will auto-unequip old item)
        if (currentPlayer.EquipItem(item, out string message))
        {
            terminal.SetColor("bright_green");
            terminal.WriteLine("");
            terminal.WriteLine($"You purchased and equipped {item.Name}!");
            terminal.SetColor("gray");
            terminal.WriteLine(message);

            // Recalculate combat stats
            currentPlayer.RecalculateStats();
        }
        else
        {
            terminal.SetColor("red");
            terminal.WriteLine($"Failed to equip: {message}");
            // Refund
            currentPlayer.Gold += adjustedPrice;
        }

        await Pause();
    }

    private async Task SellArmor()
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_yellow");
        terminal.WriteLine("═══ Sell Equipped Armor ═══");
        terminal.WriteLine("");

        // List all equipped armor pieces
        var equippedArmor = new List<(EquipmentSlot slot, Equipment item)>();
        int num = 1;

        foreach (var slot in ArmorSlots)
        {
            var item = currentPlayer.GetEquipment(slot);
            if (item != null)
            {
                equippedArmor.Add((slot, item));
                long sellPrice = item.Value / 2;

                terminal.SetColor("bright_cyan");
                terminal.Write($"{num}. ");
                terminal.SetColor("white");
                terminal.Write($"{slot.GetDisplayName()}: {item.Name}");
                terminal.SetColor("yellow");
                terminal.WriteLine($" - Sell for {FormatNumber(sellPrice)} gold");
                num++;
            }
        }

        if (equippedArmor.Count == 0)
        {
            terminal.SetColor("gray");
            terminal.WriteLine("You have no armor equipped to sell.");
            await Pause();
            return;
        }

        terminal.WriteLine("");
        terminal.SetColor("white");
        terminal.Write("Sell which piece? (0 to cancel): ");

        var input = await terminal.GetInput("");
        if (!int.TryParse(input, out int sellChoice) || sellChoice < 1 || sellChoice > equippedArmor.Count)
        {
            return;
        }

        var (sellSlot, sellItem) = equippedArmor[sellChoice - 1];
        long price = sellItem.Value / 2;

        // Check if cursed
        if (sellItem.IsCursed)
        {
            terminal.SetColor("red");
            terminal.WriteLine("");
            terminal.WriteLine($"The {sellItem.Name} is CURSED and cannot be removed!");
            await Pause();
            return;
        }

        terminal.SetColor("white");
        terminal.Write($"Sell {sellItem.Name} for ");
        terminal.SetColor("yellow");
        terminal.Write(FormatNumber(price));
        terminal.SetColor("white");
        terminal.Write(" gold? (Y/N): ");

        var confirm = await terminal.GetInput("");
        if (confirm.ToUpper() == "Y")
        {
            currentPlayer.UnequipSlot(sellSlot);
            currentPlayer.Gold += price;
            currentPlayer.RecalculateStats();

            terminal.SetColor("bright_green");
            terminal.WriteLine("");
            terminal.WriteLine($"Sold {sellItem.Name} for {FormatNumber(price)} gold!");
        }

        await Pause();
    }

    private async Task AutoBuyBestArmor()
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_magenta");
        terminal.WriteLine("═══ Auto-Buy Best Affordable Armor ═══");
        terminal.WriteLine("");

        int purchased = 0;
        long totalSpent = 0;

        foreach (var slot in ArmorSlots)
        {
            var currentItem = currentPlayer.GetEquipment(slot);
            int currentAC = currentItem?.ArmorClass ?? 0;

            // Find best affordable upgrade for this slot
            var bestItem = EquipmentDatabase.GetBySlot(slot)
                .Where(i => i.ArmorClass > currentAC)
                .Where(i => i.Value <= currentPlayer.Gold)
                .Where(i => !i.RequiresGood || currentPlayer.Chivalry > currentPlayer.Darkness)
                .Where(i => !i.RequiresEvil || currentPlayer.Darkness > currentPlayer.Chivalry)
                .OrderByDescending(i => i.ArmorClass)
                .ThenBy(i => i.Value)
                .FirstOrDefault();

            if (bestItem != null)
            {
                currentPlayer.Gold -= bestItem.Value;
                totalSpent += bestItem.Value;

                if (currentPlayer.EquipItem(bestItem, out _))
                {
                    purchased++;
                    terminal.SetColor("bright_green");
                    terminal.WriteLine($"✓ {slot.GetDisplayName()}: Bought {bestItem.Name} (AC:{bestItem.ArmorClass}) for {FormatNumber(bestItem.Value)}");
                }
            }
            else
            {
                if (currentItem != null)
                {
                    terminal.SetColor("gray");
                    terminal.WriteLine($"  {slot.GetDisplayName()}: {currentItem.Name} is already best affordable");
                }
                else
                {
                    terminal.SetColor("darkgray");
                    terminal.WriteLine($"  {slot.GetDisplayName()}: No affordable armor found");
                }
            }
        }

        terminal.WriteLine("");
        terminal.SetColor("bright_cyan");
        terminal.WriteLine($"Purchased {purchased} armor pieces for {FormatNumber(totalSpent)} gold.");

        if (purchased > 0)
        {
            currentPlayer.RecalculateStats();
        }

        await Pause();
    }

    private async Task Pause()
    {
        terminal.SetColor("gray");
        terminal.Write("Press ENTER to continue...");
        await terminal.GetInput("");
    }

    private static string FormatNumber(long value)
    {
        return value.ToString("N0");
    }
}
