using UsurperRemake.Utils;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Weapon Shop Location - Pascal-compatible trading system
/// Based on WEAPSHOP.PAS with Tully the troll, haggling, and race bonuses
/// </summary>
public class WeaponShopLocation : BaseLocation
{
    private string shopkeeperName = "Tully";
    private bool isKicked = false;
    private Character? currentPlayer => GameEngine.Instance?.CurrentPlayer;
    
    public WeaponShopLocation() : base(
        GameLocation.WeaponShop,
        "Weapon Shop",
        "You enter the dusty old weaponstore filled with all kinds of different weapons."
    ) { }
    
    protected override void SetupLocation()
    {
        base.SetupLocation();
        
        // Load shopkeeper name from config (Pascal cfg_string(15))
        shopkeeperName = "Tully"; // TODO: Load from game config
        
        if (currentPlayer == null) return;
        
        isKicked = currentPlayer.WeapHag == 0; // Kicked if no haggling attempts left
    }
    
    protected override void DisplayLocation()
    {
        terminal.ClearScreen();
        
        // ASCII art header
        terminal.SetColor("brown");
        terminal.WriteLine("╔══════════════════════════════════════════════════════╗");
        terminal.WriteLine("║                    WEAPON SHOP                      ║");
        terminal.WriteLine("╚══════════════════════════════════════════════════════╝");
        terminal.WriteLine("");
        
        // Pascal shop description
        terminal.SetColor("yellow");
        terminal.WriteLine($"Weaponstore, run by {shopkeeperName} the troll");
        terminal.WriteLine(new string('=', $"Weaponstore, run by {shopkeeperName} the troll".Length));
        
        terminal.SetColor("white");
        terminal.WriteLine("You enter the dusty old weaponstore and notice that the shelves");
        terminal.WriteLine("are filled with all kinds of different weapons. Yet you know that the");
        terminal.WriteLine("real powerful items dwells somewhere deeper within this mysterious");
        terminal.WriteLine("building.");
        terminal.WriteLine("");
        
        terminal.WriteLine("A fat troll stumbles out from a room nearby and greets you.");
        terminal.Write("You realize that this dirty old swine must be ");
        terminal.SetColor("cyan");
        terminal.Write(shopkeeperName);
        terminal.SetColor("white");
        terminal.WriteLine(" the owner.");
        terminal.WriteLine("After a brief inspection of his customer he asks what you want.");
        terminal.WriteLine("");
        
        // Display player gold
        if (currentPlayer == null) return;
        
        terminal.Write("(You have ");
        terminal.SetColor("yellow");
        terminal.Write($"{currentPlayer.Gold:N0}");
        terminal.SetColor("white");
        terminal.WriteLine(" gold pieces)");
        terminal.WriteLine("");
        
        ShowWeaponShopMenu();
    }
    
    /// <summary>
    /// Show weapon shop menu
    /// </summary>
    private void ShowWeaponShopMenu()
    {
        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("bright_yellow");
        terminal.Write("B");
        terminal.SetColor("darkgray");
        terminal.Write("] ");
        terminal.SetColor("white");
        terminal.WriteLine("uy weapon");

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("bright_yellow");
        terminal.Write("T");
        terminal.SetColor("darkgray");
        terminal.Write("] ");
        terminal.SetColor("white");
        terminal.WriteLine("he best weapon for your gold");

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("bright_green");
        terminal.Write("S");
        terminal.SetColor("darkgray");
        terminal.Write("] ");
        terminal.SetColor("white");
        terminal.WriteLine("ell weapon");

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("bright_cyan");
        terminal.Write("L");
        terminal.SetColor("darkgray");
        terminal.Write("] ");
        terminal.SetColor("white");
        terminal.WriteLine("ist all weapons");

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
        if (string.IsNullOrWhiteSpace(choice))
            return false;
            
        var upperChoice = choice.ToUpper().Trim();
        if (currentPlayer == null) return false;
        
        // Check if player was kicked out
        if (isKicked && upperChoice != "R")
        {
            terminal.SetColor("red");
            terminal.WriteLine("The strong desk-clerks throw you out!");
            terminal.WriteLine("You realize that you went a little bit too far in");
            terminal.WriteLine("your attempts to get a good deal.");
            await Task.Delay(3000);
            return true; // Force return to street
        }
        
        switch (upperChoice)
        {
            case "R":
                await NavigateToLocation(GameLocation.MainStreet);
                return true;
                
            case "B":
                await BuyWeapon();
                return false;
                
            case "T":
                await BuyBestWeapon();
                return false;
                
            case "S":
                await SellWeapon();
                return false;
                
            case "L":
                await ListWeapons();
                return false;
                
            case "?":
                // Menu is always shown
                return false;
                
            default:
                terminal.WriteLine("Invalid choice! Try again.", "red");
                await Task.Delay(1500);
                return false;
        }
    }
    
    /// <summary>
    /// Buy a specific weapon
    /// </summary>
    private async Task BuyWeapon()
    {
        if (currentPlayer.RHand != 0)
        {
            terminal.SetColor("red");
            terminal.WriteLine("Get rid of your old weapon first!");
            await Task.Delay(2000);
            return;
        }
        
        terminal.SetColor("yellow");
        terminal.Write("Which one?");
        terminal.SetColor("white");
        terminal.WriteLine(", the troll mutters:");
        
        var weaponChoice = await terminal.GetInput("Enter weapon number: ");
        
        if (!int.TryParse(weaponChoice, out int weaponId) || weaponId <= 0)
        {
            terminal.WriteLine("Invalid weapon number!", "red");
            await Task.Delay(1500);
            return;
        }
        
        // Get weapon from item manager
        var weapon = ItemManager.GetWeapon(weaponId);
        if (weapon == null)
        {
            terminal.WriteLine("That weapon doesn't exist!", "red");
            await Task.Delay(1500);
            return;
        }
        
        terminal.SetColor("white");
        terminal.Write("So you want a ");
        terminal.SetColor("cyan");
        terminal.WriteLine(weapon.Name);
        terminal.WriteLine("");

        // Show equipment comparison if player has current weapon
        if (currentPlayer.RHand > 0)
        {
            var currentWeapon = ItemManager.GetWeapon(currentPlayer.RHand);
            if (currentWeapon != null)
            {
                terminal.SetColor("gray");
                terminal.Write("Current: ");
                terminal.SetColor("white");
                terminal.Write($"{currentWeapon.Name}");
                terminal.SetColor("yellow");
                terminal.WriteLine($" (Power: {currentWeapon.Power})");

                terminal.SetColor("gray");
                terminal.Write("New:     ");
                terminal.SetColor("bright_cyan");
                terminal.Write($"{weapon.Name}");
                terminal.SetColor("bright_yellow");
                terminal.WriteLine($" (Power: {weapon.Power})");

                long powerChange = weapon.Power - currentWeapon.Power;
                terminal.SetColor("gray");
                terminal.Write("Change:  ");
                if (powerChange > 0)
                {
                    terminal.SetColor("bright_green");
                    terminal.WriteLine($"+{powerChange} Power");
                }
                else if (powerChange < 0)
                {
                    terminal.SetColor("bright_red");
                    terminal.WriteLine($"{powerChange} Power (DOWNGRADE!)");
                }
                else
                {
                    terminal.SetColor("yellow");
                    terminal.WriteLine("No change");
                }
                terminal.WriteLine("");
            }
        }

        long finalPrice = weapon.Value;
        
        // Apply race discount for trolls
        if (currentPlayer.Race == CharacterRace.Troll)
        {
            finalPrice = HagglingEngine.ApplyRaceDiscount(currentPlayer, weapon.Value);
            
            terminal.SetColor("green");
            terminal.WriteLine($"{shopkeeperName} blinks at you");
            terminal.WriteLine("Hey, we trolls gotta stick together!");
            terminal.WriteLine("Therefore I will give ya a discount!");
            terminal.WriteLine("");
        }
        
        terminal.SetColor("white");
        terminal.Write("It will cost you ");
        terminal.SetColor("yellow");
        terminal.Write($"{finalPrice:N0}");
        terminal.SetColor("white");
        terminal.WriteLine(" in gold.");
        terminal.WriteLine("");
        
        // Check if player can afford it
        if (currentPlayer.Gold < finalPrice)
        {
            terminal.WriteLine("You don't have enough gold!", "red");
            await Task.Delay(2000);
            return;
        }
        
        // Purchase options
        terminal.WriteLine("Pay? (Y)es, [N]o, (H)aggle");
        var choice = await terminal.GetInput(": ");
        
        switch (choice.ToUpper())
        {
            case "Y":
                await CompleteWeaponPurchase(currentPlayer, weapon, finalPrice);
                break;
                
            case "H":
                var hagglePrice = await HagglingEngine.Haggle(currentPlayer, HagglingEngine.ShopType.Weapon, 
                                                             finalPrice, shopkeeperName, terminal);
                if (hagglePrice < finalPrice)
                {
                    await CompleteWeaponPurchase(currentPlayer, weapon, hagglePrice);
                }
                else
                {
                    terminal.WriteLine("No deal reached.", "yellow");
                    await Task.Delay(1500);
                }
                break;
                
            case "N":
            case "":
                terminal.WriteLine("Maybe next time.", "gray");
                await Task.Delay(1000);
                break;
                
            default:
                terminal.WriteLine("Invalid choice!", "red");
                await Task.Delay(1000);
                break;
        }
    }
    
    /// <summary>
    /// Buy the best weapon player can afford
    /// </summary>
    private async Task BuyBestWeapon()
    {
        if (currentPlayer.RHand != 0)
        {
            terminal.SetColor("red");
            terminal.WriteLine("Get rid of your old weapon first!");
            await Task.Delay(2000);
            return;
        }
        
        // Find best affordable weapon
        var bestWeapon = ItemManager.GetBestAffordableWeapon(currentPlayer.Gold, currentPlayer);
        
        if (bestWeapon == null)
        {
            terminal.SetColor("red");
            terminal.WriteLine("Sorry friend! I don't have any weapon you can afford.");
            await Task.Delay(2000);
            return;
        }
        
        terminal.SetColor("green");
        terminal.WriteLine("I have exactly what you are looking for!");
        terminal.Write(", ");
        terminal.SetColor("cyan");
        terminal.Write(shopkeeperName);
        terminal.SetColor("white");
        terminal.WriteLine(" says.");
        
        terminal.Write("Buy ");
        terminal.SetColor("cyan");
        terminal.Write(bestWeapon.Name);
        terminal.SetColor("white");
        terminal.Write(" for ");
        terminal.SetColor("yellow");
        terminal.Write($"{bestWeapon.Value:N0}");
        terminal.SetColor("white");
        terminal.Write(" gold pieces?");
        
        var confirm = await terminal.GetInput(" (Y/N): ");
        
        if (confirm.ToUpper() == "Y")
        {
            terminal.SetColor("green");
            terminal.WriteLine("Ok, says the troll and hands you the weapon.");
            terminal.Write("You give ");
            terminal.SetColor("cyan");
            terminal.Write(shopkeeperName);
            terminal.SetColor("white");
            terminal.WriteLine(" the gold.");
            
            await CompleteWeaponPurchase(currentPlayer, bestWeapon, bestWeapon.Value);
        }
        else
        {
            terminal.WriteLine("Maybe another time.", "gray");
            await Task.Delay(1000);
        }
    }
    
    /// <summary>
    /// Sell current weapon
    /// </summary>
    private async Task SellWeapon()
    {
        if (currentPlayer.RHand == 0)
        {
            terminal.SetColor("red");
            terminal.WriteLine("Sell what?");
            await Task.Delay(1500);
            return;
        }
        
        var weapon = ItemManager.GetWeapon(currentPlayer.RHand);
        if (weapon == null)
        {
            terminal.WriteLine("Your weapon seems to have vanished!", "red");
            await Task.Delay(1500);
            return;
        }
        
        long sellPrice = weapon.Value / 2; // Sell for half value

        terminal.SetColor("white");
        terminal.WriteLine("The troll declares that he will pay you");
        terminal.SetColor("yellow");
        terminal.Write($"{sellPrice:N0}");
        terminal.SetColor("white");
        terminal.Write(" gold pieces for your ");
        terminal.SetColor("cyan");
        terminal.WriteLine(weapon.Name);
        terminal.WriteLine("");

        // Warning for valuable items
        if (weapon.Value > 1000)
        {
            terminal.SetColor("bright_yellow");
            terminal.WriteLine("⚠ WARNING: This is a valuable weapon!");
            terminal.SetColor("yellow");
            terminal.WriteLine($"   You'll only get {sellPrice:N0} gold (half its value).");
            terminal.WriteLine("");
        }

        var confirm = await terminal.GetInput("Will you sell it? (Y/N): ");

        if (confirm.ToUpper() == "Y")
        {
            terminal.WriteLine("You give the troll your weapon, and receive the gold.", "green");
            
            // Complete the sale
            currentPlayer.Gold += sellPrice;
            currentPlayer.RHand = 0;
            currentPlayer.WeapPow = 0;
            
            // Clear poison if weapon was poisoned
            if (currentPlayer.Poison > 0)
            {
                terminal.WriteLine("You feel a bit sad, now when your poisoned weapon is gone.", "gray");
                currentPlayer.Poison = 0;
            }
            
            await Task.Delay(2000);
        }
        else
        {
            terminal.WriteLine("Keep it then.", "gray");
            await Task.Delay(1000);
        }
    }
    
    /// <summary>
    /// List available weapons
    /// </summary>
    private async Task ListWeapons()
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_white");
        terminal.WriteLine("Ancient Weapons                    Price");
        terminal.WriteLine("=====================================");
        
        var weapons = ItemManager.GetShopWeapons();
        int count = 1;
        
        foreach (var weapon in weapons.Take(20)) // Show first 20 weapons
        {
            terminal.SetColor("cyan");
            terminal.Write($"{count,3}. ");
            
            // Format weapon name with dots
            string nameWithDots = weapon.Name;
            while (nameWithDots.Length < 25)
            {
                nameWithDots += ".";
            }
            
            terminal.SetColor("white");
            terminal.Write(nameWithDots);
            
            // Format price with dots
            string priceStr = $"{weapon.Value:N0}";
            while (priceStr.Length < 10)
            {
                priceStr = "." + priceStr;
            }
            
            terminal.SetColor("yellow");
            terminal.WriteLine(priceStr);
            
            count++;
            
            // Pagination
            if (count % 15 == 0)
            {
                terminal.WriteLine("");
                var continueChoice = await terminal.GetInput("Continue? (Y/N): ");
                if (continueChoice.ToUpper() != "Y")
                {
                    break;
                }
                terminal.WriteLine("");
            }
        }
        
        terminal.WriteLine("");
        await terminal.PressAnyKey();
    }
    
    /// <summary>
    /// Complete weapon purchase
    /// </summary>
    private async Task CompleteWeaponPurchase(Character player, ClassicWeapon weapon, long price)
    {
        player.Gold -= price;
        player.RHand = GetWeaponId(weapon);
        player.WeapPow = (int)weapon.Power;
        
        terminal.SetColor("green");
        terminal.WriteLine("Transaction completed!");
        terminal.WriteLine($"You are now wielding {weapon.Name}");
        
        // Create news entry (placeholder)
        terminal.WriteLine($"[NEWS] {player.DisplayName} bought a {weapon.Name}.", "cyan");
        
        await Task.Delay(2000);
    }
    
    /// <summary>
    /// Get weapon ID from ClassicWeapon (helper method)
    /// </summary>
    private int GetWeaponId(ClassicWeapon weapon)
    {
        // Find the weapon ID in the classic weapons collection
        var weapons = ItemManager.GetShopWeapons();
        for (int i = 0; i < weapons.Count; i++)
        {
            if (weapons[i].Name == weapon.Name)
                return i + 1; // 1-based indexing
        }
        return 0;
    }
    
    /// <summary>
    /// Check if player can use weapon (basic checks for classic weapons)
    /// </summary>
    private bool CanPlayerUseWeapon(Character player, ClassicWeapon weapon)
    {
        // Classic weapons have minimal restrictions
        // Most restrictions are handled at purchase time
        return true;
    }
} 
