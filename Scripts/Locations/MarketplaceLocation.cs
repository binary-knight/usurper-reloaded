using Godot;
using GlobalItem = global::Item;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UsurperRemake.Locations;

/// <summary>
/// Marketplace – player‐driven item trading (simplified port of PLMARKET.PAS).
/// Players may list items for sale and purchase items other players have listed.
/// </summary>
public class MarketplaceLocation : BaseLocation
{
    /// <summary>
    /// Represents a market posting.
    /// </summary>
    private class MarketItem
    {
        public GlobalItem Item { get; init; } = null!;  // The item being sold
        public string Seller { get; init; } = string.Empty; // Seller Name2
        public long Price { get; set; } // Asking price
        public DateTime Posted { get; init; } = DateTime.Now;
        public bool Sold { get; set; }
        public string Buyer { get; set; } = string.Empty;

        public string GetDisplayRow(int index)
        {
            var age = (DateTime.Now - Posted).Days;
            return $"[{index}] {Item.GetDisplayName()} — {Price:N0} {GameConfig.MoneyType} (by {Seller}, {age}d)";
        }
    }

    // Shared across game session
    private static readonly List<MarketItem> Listings = new();
    private const int MaxListingAgeDays = 30; // Auto‐expire after 30 days

    public MarketplaceLocation() : base(GameLocation.Marketplace, "Marketplace",
        "Wooden stalls creak under the weight of exotic wares. A bulletin board stands nearby, covered in notes.")
    {
    }

    protected override void SetupLocation()
    {
        PossibleExits.Add(GameLocation.MainStreet);
    }

    protected override void DisplayLocation()
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_yellow");
        terminal.WriteLine("<< PLAYERS' MARKET >>\n");
        terminal.SetColor("green");
        terminal.WriteLine("(C)heck bulletin board  (B)uy item  (A)dd item  (S)tatus  (R)eturn\n");
        ShowStatusLine();
    }

    protected override async Task<bool> ProcessChoice(string choice)
    {
        switch (choice.ToUpperInvariant())
        {
            case "C":
                await ShowBoard();
                return false;
            case "B":
                await BuyItem();
                return false;
            case "A":
                await ListItem();
                return false;
            case "S":
                await ShowStatus();
                return false;
            case "R":
            case "Q":
                await NavigateToLocation(GameLocation.MainStreet);
                return true;
            default:
                return await base.ProcessChoice(choice);
        }
    }

    private async Task ShowBoard()
    {
        CleanupExpired();
        terminal.ClearScreen();
        terminal.SetColor("bright_cyan");
        terminal.WriteLine("BULLETIN BOARD\n");
        if (Listings.All(l => l.Sold))
        {
            terminal.WriteLine("No items for sale right now.", "yellow");
        }
        else
        {
            int idx = 1;
            foreach (var listing in Listings.Where(l => !l.Sold))
            {
                terminal.WriteLine(listing.GetDisplayRow(idx++));
            }
        }
        terminal.WriteLine("");
        await terminal.PressAnyKey();
    }

    private async Task BuyItem()
    {
        CleanupExpired();
        var unsold = Listings.Where(l => !l.Sold).ToList();
        if (unsold.Count == 0)
        {
            terminal.WriteLine("Nothing available for purchase.", "yellow");
            await Task.Delay(1500);
            return;
        }

        await ShowBoard();
        var input = await terminal.GetInput("Enter number of item to buy (or Q to cancel): ");
        if (input.Trim().Equals("Q", StringComparison.OrdinalIgnoreCase)) return;
        if (!int.TryParse(input, out int choice) || choice < 1 || choice > unsold.Count)
        {
            terminal.WriteLine("Invalid selection.", "red");
            await Task.Delay(1500);
            return;
        }

        var listing = unsold[choice - 1];
        if (currentPlayer.Gold < listing.Price)
        {
            terminal.WriteLine("You cannot afford that item!", "red");
            await Task.Delay(1500);
            return;
        }

        // Transfer gold and item
        currentPlayer.Gold -= listing.Price;
        currentPlayer.Inventory.Add(listing.Item.Clone());
        listing.Sold = true;
        listing.Buyer = currentPlayer.DisplayName;
        terminal.WriteLine("Transaction complete! The item is now yours.", "bright_green");
        await Task.Delay(2000);
    }

    private async Task ListItem()
    {
        // Show player inventory with index numbers
        var sellable = currentPlayer.Inventory.Where(it => it != null).ToList();
        if (sellable.Count == 0)
        {
            terminal.WriteLine("You have nothing to sell.", "yellow");
            await Task.Delay(1500);
            return;
        }

        terminal.ClearScreen();
        terminal.SetColor("cyan");
        terminal.WriteLine("Your Inventory:");
        for (int i = 0; i < sellable.Count; i++)
        {
            terminal.WriteLine($"{i + 1}. {sellable[i].GetDisplayName()}");
        }

        var input = await terminal.GetInput("Enter number of item to sell (or Q to cancel): ");
        if (input.Trim().Equals("Q", StringComparison.OrdinalIgnoreCase)) return;
        if (!int.TryParse(input, out int choice) || choice < 1 || choice > sellable.Count)
        {
            terminal.WriteLine("Invalid selection.", "red");
            await Task.Delay(1500);
            return;
        }

        var item = sellable[choice - 1];
        terminal.WriteLine("Enter the price for the item:");
        var priceInput = await terminal.GetInput();
        if (!long.TryParse(priceInput, out long price) || price < 0)
        {
            terminal.WriteLine("Invalid price format.", "red");
            await Task.Delay(1500);
            return;
        }

        // Add item to marketplace
        Listings.Add(new MarketItem { Item = item.Clone(), Price = price, Seller = currentPlayer.DisplayName });
        currentPlayer.Inventory.Remove(item);
        terminal.WriteLine("Item listed successfully!", "bright_green");
        await Task.Delay(2000);
    }

    private void CleanupExpired()
    {
        Listings.RemoveAll(l => (DateTime.Now - l.Posted).Days > MaxListingAgeDays || l.Sold);
    }

    private async Task ShowStatus()
    {
        terminal.ClearScreen();
        terminal.SetColor("cyan");
        terminal.WriteLine("Your Market Activity:\n");

        var myListings = Listings.Where(l => l.Seller == currentPlayer.DisplayName && !l.Sold).ToList();
        var sold = Listings.Where(l => l.Seller == currentPlayer.DisplayName && l.Sold).ToList();
        var purchases = Listings.Where(l => l.Buyer == currentPlayer.DisplayName).ToList();

        terminal.WriteLine($"Active listings: {myListings.Count}");
        terminal.WriteLine($"Items sold: {sold.Count}");
        terminal.WriteLine($"Items purchased: {purchases.Count}\n");

        await terminal.PressAnyKey();
    }
} 