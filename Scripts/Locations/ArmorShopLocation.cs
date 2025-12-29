using UsurperRemake.Utils;
using UsurperRemake;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Armor Shop Location - Complete Pascal-compatible armor system
/// Based on ARMSHOP.PAS with Reese and multi-slot equipment
/// </summary>
public class ArmorShopLocation : BaseLocation
{
    private string shopkeeperName = "Reese";
    private bool isKicked = false;

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

        terminal.SetColor("white");
        terminal.WriteLine("As you enter the store you notice a strange but appealing smell.");
        terminal.WriteLine("You recall that some merchants use magic elixirs to make their selling easier...");
        terminal.SetColor("bright_green");
        terminal.Write(shopkeeperName);
        terminal.SetColor("white");
        terminal.WriteLine(" suddenly appears out of nowhere, with a smile on his face.");
        terminal.WriteLine("He is known as a respectable citizen, although evil tongues speak of");
        terminal.WriteLine("meetings with dark and mysterious creatures from the deep dungeons.");
        terminal.WriteLine("");

        terminal.SetColor("white");
        terminal.Write("You have ");
        terminal.SetColor("yellow");
        terminal.Write(FormatNumber(currentPlayer.Gold));
        terminal.SetColor("white");
        terminal.WriteLine(" gold crowns.");
        terminal.WriteLine("");

        ShowArmorShopMenu();
    }

    private void ShowArmorShopMenu()
    {
        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("bright_yellow");
        terminal.Write("B");
        terminal.SetColor("darkgray");
        terminal.Write("] ");
        terminal.SetColor("white");
        terminal.WriteLine("uy armor");

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("bright_green");
        terminal.Write("S");
        terminal.SetColor("darkgray");
        terminal.Write("] ");
        terminal.SetColor("white");
        terminal.WriteLine("ell armor");

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("bright_cyan");
        terminal.Write("L");
        terminal.SetColor("darkgray");
        terminal.Write("] ");
        terminal.SetColor("white");
        terminal.WriteLine("ist armor");

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("bright_magenta");
        terminal.Write("1");
        terminal.SetColor("darkgray");
        terminal.Write("] ");
        terminal.SetColor("magenta");
        terminal.Write("Ask ");
        terminal.SetColor("bright_cyan");
        terminal.Write(shopkeeperName);
        terminal.SetColor("magenta");
        terminal.WriteLine(" for help with your equipment!");

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

        // Show quick command bar
        ShowQuickCommandBar();
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

        switch (upperChoice)
        {
            case "R":
                await NavigateToLocation(GameLocation.MainStreet);
                return true;

            case "L":
                await ListArmors();
                return false;

            case "B":
                await BuyArmor();
                return false;

            case "S":
                await SellArmor();
                return false;

            case "1":
                await BuyBestEquipment();
                return false;

            case "?":
                DisplayLocation();
                return false;

            default:
                terminal.WriteLine("Invalid choice!", "red");
                await Task.Delay(1000);
                return false;
        }
    }

    /// <summary>
    /// List all available armors in the shop (Classic mode)
    /// Based on ARMSHOP.PAS lines 774-815
    /// </summary>
    private async Task ListArmors()
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_blue");
        terminal.WriteLine("═══════════════════════════════════════════════════");
        terminal.WriteLine("             Ancient Armors                 Price");
        terminal.WriteLine("═══════════════════════════════════════════════════");

        var armors = ItemManager.GetShopArmors();
        int count = 0;

        foreach (var armor in armors)
        {
            terminal.SetColor("bright_cyan");
            terminal.Write($"{count + 1,3}. ");

            terminal.SetColor("bright_white");
            var nameField = armor.Name.PadRight(30, '.');
            terminal.Write(nameField);

            terminal.SetColor("yellow");
            var priceField = FormatNumber(armor.Value).PadLeft(15, '.');
            terminal.WriteLine(priceField);

            count++;

            if (count % 20 == 0 && count < armors.Count)
            {
                terminal.WriteLine("");
                terminal.SetColor("yellow");
                terminal.Write("Continue? (Y/N): ");
                var response = await terminal.GetInput("");
                if (response.ToUpper() != "Y" && response.ToUpper() != "")
                    break;
                terminal.ClearScreen();
            }
        }

        terminal.WriteLine("");
        await Pause();
    }

    /// <summary>
    /// Buy armor - Classic mode from ARMSHOP.PAS lines 987-1093
    /// </summary>
    private async Task BuyArmor()
    {
        if (currentPlayer == null) return;

        terminal.ClearScreen();

        // Classic mode check - player can only have one armor at a time
        if (currentPlayer.Armor != 0)
        {
            terminal.SetColor("bright_magenta");
            terminal.WriteLine("Get rid of your old armor first!");
            await Pause();
            return;
        }

        terminal.SetColor("bright_magenta");
        terminal.Write("Which one? (number or 0 to cancel): ");
        var input = await terminal.GetInput("");

        if (!int.TryParse(input, out int armorChoice) || armorChoice <= 0)
        {
            return;
        }

        var armors = ItemManager.GetShopArmors();
        if (armorChoice > armors.Count)
        {
            terminal.WriteLine("Invalid selection!", "red");
            await Pause();
            return;
        }

        var selectedArmor = armors[armorChoice - 1];

        terminal.WriteLine("");
        terminal.SetColor("white");
        terminal.Write("So you want a ");
        terminal.SetColor("bright_cyan");
        terminal.WriteLine(selectedArmor.Name);
        terminal.WriteLine("");

        // Show equipment comparison if player has current armor
        if (currentPlayer.Armor > 0)
        {
            var currentArmor = ItemManager.GetClassicArmor(currentPlayer.Armor);
            if (currentArmor != null)
            {
                terminal.SetColor("gray");
                terminal.Write("Current: ");
                terminal.SetColor("white");
                terminal.Write($"{currentArmor.Name}");
                terminal.SetColor("cyan");
                terminal.WriteLine($" (AC: {currentArmor.Power})");

                terminal.SetColor("gray");
                terminal.Write("New:     ");
                terminal.SetColor("bright_cyan");
                terminal.Write($"{selectedArmor.Name}");
                terminal.SetColor("bright_cyan");
                terminal.WriteLine($" (AC: {selectedArmor.Power})");

                long acChange = selectedArmor.Power - currentArmor.Power;
                terminal.SetColor("gray");
                terminal.Write("Change:  ");
                if (acChange > 0)
                {
                    terminal.SetColor("bright_green");
                    terminal.WriteLine($"+{acChange} AC");
                }
                else if (acChange < 0)
                {
                    terminal.SetColor("bright_red");
                    terminal.WriteLine($"{acChange} AC (DOWNGRADE!)");
                }
                else
                {
                    terminal.SetColor("yellow");
                    terminal.WriteLine("No change");
                }
                terminal.WriteLine("");
            }
        }

        terminal.SetColor("white");
        terminal.Write("It will cost you ");
        terminal.SetColor("yellow");
        terminal.Write(FormatNumber(selectedArmor.Value));
        terminal.SetColor("white");
        terminal.WriteLine(" in gold.");
        terminal.WriteLine("");

        terminal.SetColor("white");
        terminal.WriteLine("Pay? (Y)es, [N]o: ");
        var response = await terminal.GetInput("");

        if (response.ToUpper() != "Y")
        {
            terminal.WriteLine("No", "bright_red");
            return;
        }

        terminal.WriteLine("Yes", "bright_green");
        terminal.WriteLine("");

        if (currentPlayer.Gold < selectedArmor.Value)
        {
            terminal.SetColor("bright_magenta");
            terminal.Write("No gold, no armor!");
            terminal.SetColor("white");
            terminal.Write(", ");
            terminal.SetColor("bright_green");
            terminal.Write(shopkeeperName);
            terminal.SetColor("white");
            terminal.WriteLine(" yells.");
            await Pause();
            return;
        }

        // Complete the purchase
        terminal.SetColor("bright_magenta");
        terminal.Write("Deal!");
        terminal.SetColor("white");
        terminal.Write(", says ");
        terminal.SetColor("bright_green");
        terminal.Write(shopkeeperName);
        terminal.SetColor("white");
        terminal.WriteLine(" and gives you the armor.");
        terminal.WriteLine("");

        terminal.WriteLine("You hand over the gold.");

        currentPlayer.Gold -= selectedArmor.Value;
        currentPlayer.Armor = armorChoice;
        currentPlayer.APow = (int)selectedArmor.Power;

        terminal.WriteLine("");
        terminal.SetColor("bright_green");
        terminal.WriteLine($"You are now equipped with {selectedArmor.Name} (AC: {selectedArmor.Power})!");

        await Pause();
    }

    /// <summary>
    /// Sell armor - Based on ARMSHOP.PAS lines 817-844
    /// </summary>
    private async Task SellArmor()
    {
        if (currentPlayer == null) return;

        terminal.ClearScreen();

        if (currentPlayer.Armor == 0)
        {
            terminal.SetColor("bright_magenta");
            terminal.WriteLine("You don't have anything to sell!");
            await Pause();
            return;
        }

        var armor = ItemManager.GetClassicArmor(currentPlayer.Armor);
        if (armor == null)
        {
            terminal.WriteLine("Error loading armor data!", "red");
            await Pause();
            return;
        }

        long sellPrice = armor.Value / 2;

        terminal.SetColor("bright_green");
        terminal.Write(shopkeeperName);
        terminal.SetColor("white");
        terminal.WriteLine(" declares that he will pay you ");
        terminal.SetColor("yellow");
        terminal.Write(FormatNumber(sellPrice));
        terminal.SetColor("white");
        terminal.Write(" gold crowns for your ");
        terminal.SetColor("bright_cyan");
        terminal.WriteLine(armor.Name);
        terminal.WriteLine("");

        // Warning for valuable items
        if (armor.Value > 1000)
        {
            terminal.SetColor("bright_yellow");
            terminal.WriteLine("⚠ WARNING: This is valuable armor!");
            terminal.SetColor("yellow");
            terminal.WriteLine($"   You'll only get {FormatNumber(sellPrice)} gold (half its value).");
            terminal.WriteLine("");
        }

        terminal.SetColor("white");
        terminal.Write("Will you sell it? (Y/[N]): ");
        var response = await terminal.GetInput("");

        if (response.ToUpper() != "Y")
        {
            return;
        }

        terminal.WriteLine("");
        terminal.SetColor("white");
        terminal.Write("You give ");
        terminal.SetColor("bright_green");
        terminal.Write(shopkeeperName);
        terminal.SetColor("white");
        terminal.WriteLine(" your armor, and receive the gold.");

        currentPlayer.Gold += sellPrice;
        currentPlayer.Armor = 0;
        currentPlayer.APow = 0;

        await Pause();
    }

    /// <summary>
    /// Ask Reese to help equip player with best affordable equipment
    /// Based on ARMSHOP.PAS lines 602-773
    /// </summary>
    private async Task BuyBestEquipment()
    {
        if (currentPlayer == null) return;

        terminal.ClearScreen();

        terminal.SetColor("bright_magenta");
        terminal.Write("Hey! I need some help over here!");
        terminal.SetColor("white");
        terminal.Write(", you shout to ");
        terminal.SetColor("bright_green");
        terminal.Write(shopkeeperName);
        terminal.SetColor("white");
        terminal.WriteLine(".");
        terminal.WriteLine("");

        terminal.SetColor("bright_magenta");
        terminal.Write("Ok. Let's see how much gold you got");
        terminal.SetColor("white");
        terminal.Write(", ");
        terminal.SetColor("bright_green");
        terminal.Write(shopkeeperName);
        terminal.SetColor("white");
        terminal.WriteLine(" says.");

        await Pause();
        terminal.WriteLine("");

        if (currentPlayer.Gold == 0)
        {
            terminal.SetColor("white");
            terminal.Write("You show ");
            terminal.SetColor("bright_green");
            terminal.Write(shopkeeperName);
            terminal.SetColor("white");
            terminal.WriteLine(" your empty purse.");
            terminal.WriteLine("");

            terminal.SetColor("bright_magenta");
            terminal.Write("Is this supposed to be funny?");
            terminal.SetColor("white");
            terminal.Write(", ");
            terminal.SetColor("bright_green");
            terminal.Write(shopkeeperName);
            terminal.SetColor("white");
            terminal.WriteLine(" says with a strange voice.");
            await Pause();
            return;
        }

        if (currentPlayer.Gold < 50)
        {
            terminal.SetColor("white");
            terminal.WriteLine($"You show {shopkeeperName} your gold crowns.");
            terminal.WriteLine("");

            terminal.SetColor("bright_magenta");
            terminal.Write("You won't get anything for that!");
            terminal.SetColor("white");
            terminal.Write(", ");
            terminal.SetColor("bright_green");
            terminal.Write(shopkeeperName);
            terminal.SetColor("white");
            terminal.WriteLine(" says in a mocking tone.");
            await Pause();
            return;
        }

        // Check if player already has armor
        if (currentPlayer.Armor != 0)
        {
            terminal.SetColor("bright_magenta");
            terminal.Write("You are already fully equipped!");
            terminal.SetColor("white");
            terminal.Write(", ");
            terminal.SetColor("bright_green");
            terminal.Write(shopkeeperName);
            terminal.SetColor("white");
            terminal.WriteLine(" says.");
            await Pause();
            return;
        }

        // Find best armor player can afford (using Body as the armor type for classic mode)
        var bestArmor = ItemManager.GetBestAffordableArmor(ObjType.Body, currentPlayer.Gold, currentPlayer);

        if (bestArmor == null)
        {
            terminal.SetColor("bright_magenta");
            terminal.Write("Too bad we couldn't find anything suitable.");
            terminal.SetColor("white");
            terminal.Write(", ");
            terminal.SetColor("bright_green");
            terminal.Write(shopkeeperName);
            terminal.SetColor("white");
            terminal.WriteLine(" says.");
            await Pause();
            return;
        }

        terminal.WriteLine("");
        terminal.SetColor("bright_magenta");
        terminal.Write("I have a nice ");
        terminal.SetColor("bright_cyan");
        terminal.Write(bestArmor.Name);
        terminal.SetColor("bright_magenta");
        terminal.Write(" here");
        terminal.SetColor("white");
        terminal.Write(", ");
        terminal.SetColor("bright_green");
        terminal.Write(shopkeeperName);
        terminal.SetColor("white");
        terminal.WriteLine(" says.");

        terminal.SetColor("white");
        terminal.Write("You can get it for ");
        terminal.SetColor("yellow");
        terminal.Write(FormatNumber(bestArmor.Value));
        terminal.SetColor("white");
        terminal.WriteLine(" gold crowns.");
        terminal.WriteLine("");

        terminal.SetColor("white");
        terminal.Write("Buy it? (Y/N): ");
        var response = await terminal.GetInput("");

        if (response.ToUpper() != "Y")
        {
            return;
        }

        // Complete purchase
        currentPlayer.Gold -= bestArmor.Value;
        var armorIndex = ItemManager.GetShopArmors().IndexOf(bestArmor) + 1;
        currentPlayer.Armor = armorIndex;
        currentPlayer.APow = (int)bestArmor.Power;

        terminal.WriteLine("");
        terminal.SetColor("bright_magenta");
        terminal.Write("A pleasure doing business with you!");
        terminal.SetColor("white");
        terminal.Write(", ");
        terminal.SetColor("bright_green");
        terminal.Write(shopkeeperName);
        terminal.SetColor("white");
        terminal.WriteLine(" smiles.");
        terminal.WriteLine("");

        terminal.SetColor("bright_green");
        terminal.WriteLine($"You equipped {bestArmor.Name} (AC: {bestArmor.Power})!");

        await Pause();
    }

    private string FormatNumber(long number)
    {
        return number.ToString("N0");
    }

    private async Task Pause()
    {
        terminal.WriteLine("");
        terminal.SetColor("yellow");
        terminal.Write("Press any key to continue...");
        await terminal.GetInput("");
    }
} 
