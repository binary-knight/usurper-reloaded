using UsurperRemake.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// Magic Shop - Complete Pascal-compatible magical services
/// Features: Magic Items, Item Identification, Healing Potions, Trading
/// Based on Pascal MAGIC.PAS with full compatibility
/// </summary>
public partial class MagicShopLocation : BaseLocation
{
    private const string ShopTitle = "Magic Shop";
    private static string _ownerName = GameConfig.DefaultMagicShopOwner;
    private static int _identificationCost = GameConfig.DefaultIdentificationCost;
    
    // Available magic items for sale
    private static List<Item> _magicInventory = new List<Item>();
    
    public MagicShopLocation()
    {
        LocationName = "Magic Shop";
        LocationId = GameConfig.GameLocation.MagicShop;
        
        InitializeMagicInventory();
        
        // Add shop owner NPC
        var shopOwner = CreateShopOwner();
        npcs.Add(shopOwner);
    }
    
    private NPC CreateShopOwner()
    {
        var owner = new NPC(_ownerName, _ownerName, CharacterAI.Civilian, CharacterRace.Gnome);
        owner.Name1 = _ownerName;
        owner.Name2 = _ownerName;
        owner.Level = 30;
        owner.Gold = 1000000L;
        owner.HP = owner.MaxHP = 150;
        owner.Strength = 12;
        owner.Defence = 15;
        owner.Agility = 20;
        owner.Charisma = 25;
        owner.Wisdom = 35;
        owner.Dexterity = 22;
        owner.Mana = owner.MaxMana = 200;
        
        // Magic shop owner personality - mystical and knowledgeable
        owner.Brain.Personality.Intelligence = 0.9f;
        owner.Brain.Personality.Mysticism = 1.0f;
        owner.Brain.Personality.Greed = 0.7f;
        owner.Brain.Personality.Patience = 0.8f;
        
        return owner;
    }
    
    private void InitializeMagicInventory()
    {
        _magicInventory.Clear();
        
        // Neck items (Amulets)
        AddMagicItem("Amulet of Wisdom", MagicItemType.Neck, 2500, wisdom: 3, mana: 10);
        AddMagicItem("Pendant of Protection", MagicItemType.Neck, 3500, defense: 5, magicRes: 15);
        AddMagicItem("Holy Symbol", MagicItemType.Neck, 4000, goodOnly: true, cureType: CureType.All);
        AddMagicItem("Dark Medallion", MagicItemType.Neck, 4500, evilOnly: true, mana: 25, cursed: true);
        AddMagicItem("Crystal Necklace", MagicItemType.Neck, 6000, wisdom: 5, mana: 20, dexterity: 2);
        
        // Ring items  
        AddMagicItem("Ring of Dexterity", MagicItemType.Fingers, 1800, dexterity: 4);
        AddMagicItem("Mana Ring", MagicItemType.Fingers, 2200, mana: 15, wisdom: 2);
        AddMagicItem("Ring of Protection", MagicItemType.Fingers, 2800, defense: 3, magicRes: 10);
        AddMagicItem("Sage's Ring", MagicItemType.Fingers, 3500, wisdom: 4, mana: 18);
        AddMagicItem("Ring of Shadows", MagicItemType.Fingers, 4200, evilOnly: true, dexterity: 6, cursed: true);
        AddMagicItem("Master's Ring", MagicItemType.Fingers, 8000, wisdom: 8, mana: 35, dexterity: 4);
        
        // Waist items (Belts)
        AddMagicItem("Belt of Strength", MagicItemType.Waist, 2000, strength: 3, attack: 2);
        AddMagicItem("Girdle of Dexterity", MagicItemType.Waist, 2300, dexterity: 5, defense: 2);
        AddMagicItem("Mage's Belt", MagicItemType.Waist, 3200, mana: 20, wisdom: 3);
        AddMagicItem("Belt of Health", MagicItemType.Waist, 3800, cureType: CureType.All, defense: 3);
        AddMagicItem("Cursed Girdle", MagicItemType.Waist, 5000, strength: -2, dexterity: 8, cursed: true);
    }
    
    private void AddMagicItem(string name, MagicItemType type, int value, 
        int strength = 0, int defense = 0, int attack = 0, int dexterity = 0, 
        int wisdom = 0, int mana = 0, int magicRes = 0, CureType cureType = CureType.None,
        bool goodOnly = false, bool evilOnly = false, bool cursed = false)
    {
        var item = new Item();
        item.Name = name;
        item.Value = value;
        item.Type = ObjType.Magic;
        item.MagicType = type;
        item.IsShopItem = true;
        
        // Set base stats
        item.Strength = strength;
        item.Defence = defense;
        item.Attack = attack;
        item.Dexterity = dexterity;
        item.Wisdom = wisdom;
        
        // Set magic properties
        item.MagicProperties.Mana = mana;
        item.MagicProperties.Wisdom = wisdom;
        item.MagicProperties.Dexterity = dexterity;
        item.MagicProperties.MagicResistance = magicRes;
        item.MagicProperties.DiseaseImmunity = cureType;
        
        // Set restrictions
        item.OnlyForGood = goodOnly;
        item.OnlyForEvil = evilOnly;
        item.IsCursed = cursed;
        
        _magicInventory.Add(item);
    }
    
    public override void Enter(Character player)
    {
        base.Enter(player);
        DisplayMagicShopMenu(player);
    }
    
    private void DisplayMagicShopMenu(Character player)
    {
        ClearScreen();
        
        // Shop header
        string title = $"{ShopTitle}, run by {_ownerName} the gnome";
        DisplayMessage($"┌─ {title} ─┐", ConsoleColor.Magenta);
        DisplayMessage("├─────────────────────────────────────────────────────────┤", ConsoleColor.Magenta);
        DisplayMessage("");
        
        // Shop description
        DisplayMessage("You enter the dark and dusty boutique, filled with all sorts", ConsoleColor.Gray);
        DisplayMessage("of strange objects. As you examine the place you notice a", ConsoleColor.Gray);
        DisplayMessage("few druids and wizards searching for orbs and other mysterious items.", ConsoleColor.Gray);
        DisplayMessage("When you reach the counter you try to remember what you were looking for.", ConsoleColor.Gray);
        DisplayMessage("");
        
        // Greeting
        string raceGreeting = GetRaceGreeting(player.Race);
        DisplayMessage($"What shall it be {raceGreeting}?", ConsoleColor.Cyan);
        DisplayMessage("");
        
        // Player gold display
        DisplayMessage($"(You have {player.Gold:N0} gold coins)", ConsoleColor.Gray);
        DisplayMessage("");
        
        // Menu options
        DisplayMessage("(R)eturn to street          (L)ist Items", ConsoleColor.Gray);
        DisplayMessage("(I)dentify item             (B)uy Item", ConsoleColor.Gray);
        DisplayMessage("(H)ealing Potions           (S)ell Item", ConsoleColor.Gray);
        DisplayMessage($"                            (T)alk to {_ownerName}", ConsoleColor.Gray);
        DisplayMessage("");
        
        ProcessMagicShopMenu(player);
    }
    
    private string GetRaceGreeting(CharacterRace race)
    {
        return race switch
        {
            CharacterRace.Human => "human",
            CharacterRace.Elf => "elf",
            CharacterRace.Dwarf => "dwarf", 
            CharacterRace.Hobbit => "hobbit",
            CharacterRace.Gnome => "fellow gnome",
            _ => "traveler"
        };
    }
    
    private void ProcessMagicShopMenu(Character player)
    {
        while (true)
        {
            DisplayMessage("Magic Shop (? for menu): ", ConsoleColor.Yellow, false);
            var choice = Console.ReadKey().KeyChar.ToString().ToUpper();
            DisplayMessage(""); // New line
            
            switch (choice)
            {
                case "L":
                    ListMagicItems(player);
                    break;
                case "B":
                    BuyMagicItem(player);
                    break;
                case "S":
                    SellItem(player);
                    break;
                case "I":
                    IdentifyItem(player);
                    break;
                case "H":
                    BuyHealingPotions(player);
                    break;
                case "T":
                    TalkToOwner(player);
                    break;
                case "R":
                    ExitLocation(player, GameConfig.GameLocation.MainStreet);
                    return;
                case "?":
                    DisplayMagicShopMenu(player);
                    return;
                default:
                    DisplayMessage("Invalid choice. Press '?' for menu.", ConsoleColor.Red);
                    break;
            }
            
            DisplayMessage("");
            DisplayMessage("Press any key to continue...", ConsoleColor.Yellow);
            Console.ReadKey();
            DisplayMagicShopMenu(player);
            return;
        }
    }
    
    private void ListMagicItems(Character player)
    {
        DisplayMessage("");
        DisplayMessage("═══ Magic Items for Sale ═══", ConsoleColor.Cyan);
        DisplayMessage("");
        
        int itemNumber = 1;
        foreach (var item in _magicInventory)
        {
            DisplayMessage($"{itemNumber}. {item.Name} - {item.Value:N0} gold", ConsoleColor.White);
            
            // Show item properties
            var properties = new List<string>();
            if (item.Strength != 0) properties.Add($"Str {(item.Strength > 0 ? "+" : "")}{item.Strength}");
            if (item.Defence != 0) properties.Add($"Def {(item.Defence > 0 ? "+" : "")}{item.Defence}");
            if (item.Attack != 0) properties.Add($"Att {(item.Attack > 0 ? "+" : "")}{item.Attack}");
            if (item.Dexterity != 0) properties.Add($"Dex {(item.Dexterity > 0 ? "+" : "")}{item.Dexterity}");
            if (item.Wisdom != 0) properties.Add($"Wis {(item.Wisdom > 0 ? "+" : "")}{item.Wisdom}");
            if (item.MagicProperties.Mana != 0) properties.Add($"Mana {(item.MagicProperties.Mana > 0 ? "+" : "")}{item.MagicProperties.Mana}");
            
            if (properties.Count > 0)
            {
                DisplayMessage($"   ({string.Join(", ", properties)})", ConsoleColor.Green);
            }
            
            // Show restrictions
            if (item.OnlyForGood) DisplayMessage("   (Good characters only)", ConsoleColor.Blue);
            if (item.OnlyForEvil) DisplayMessage("   (Evil characters only)", ConsoleColor.Red);
            if (item.IsCursed) DisplayMessage("   (CURSED!)", ConsoleColor.DarkRed);
            if (item.MagicProperties.DiseaseImmunity != CureType.None)
            {
                DisplayMessage($"   (Cures {item.MagicProperties.DiseaseImmunity})", ConsoleColor.Green);
            }
            
            DisplayMessage("");
            itemNumber++;
        }
    }
    
    private void BuyMagicItem(Character player)
    {
        DisplayMessage("");
        DisplayMessage("Enter Item # to buy: ", ConsoleColor.Yellow, false);
        string input = Console.ReadLine();
        
        if (int.TryParse(input, out int itemNumber) && itemNumber > 0 && itemNumber <= _magicInventory.Count)
        {
            var item = _magicInventory[itemNumber - 1];
            
            // Check restrictions
            if (item.OnlyForGood && player.Chivalry < 1 && player.Darkness > 0)
            {
                DisplayMessage("This item is charmed for good characters.", ConsoleColor.Red);
                DisplayMessage("You can buy it, but you cannot use it!", ConsoleColor.Red);
            }
            else if (item.OnlyForEvil && player.Chivalry > 0 && player.Darkness < 1)
            {
                DisplayMessage("This item is enchanted and can be used by evil characters only.", ConsoleColor.Red);
                DisplayMessage("You can buy it, but not use it!", ConsoleColor.Red);
            }
            
            if (item.StrengthRequired > player.Strength)
            {
                DisplayMessage("This item is too heavy for you to use!", ConsoleColor.Red);
            }
            
            // Check class restrictions (if any)
            // TODO: Implement class restrictions when needed
            
            DisplayMessage($"Buy the {item.Name} for {item.Value:N0} gold? (Y/N): ", ConsoleColor.Yellow, false);
            var confirm = Console.ReadKey().KeyChar.ToString().ToUpper();
            DisplayMessage("");
            
            if (confirm == "Y")
            {
                if (player.Gold < item.Value)
                {
                    DisplayMessage("You don't have enough gold!", ConsoleColor.Red);
                }
                else if (player.Inventory.Count >= GameConfig.MaxInventoryItems)
                {
                    DisplayMessage("Your inventory is full!", ConsoleColor.Red);
                }
                else
                {
                    player.Gold -= item.Value;
                    player.Inventory.Add(item.Clone());
                    
                    DisplayMessage("Done!", ConsoleColor.Green);
                    DisplayMessage($"You purchased the {item.Name}.", ConsoleColor.Gray);
                    
                    // Ask if they want to equip it immediately
                    DisplayMessage($"Start to use the {item.Name} immediately? (Y/N): ", ConsoleColor.Yellow, false);
                    var useNow = Console.ReadKey().KeyChar.ToString().ToUpper();
                    DisplayMessage("");
                    
                    if (useNow == "Y")
                    {
                        // TODO: Implement item equipping system
                        DisplayMessage($"You equip the {item.Name}.", ConsoleColor.Green);
                    }
                    else
                    {
                        DisplayMessage($"You put the {item.Name} in your backpack.", ConsoleColor.Gray);
                    }
                }
            }
        }
        else
        {
            DisplayMessage("Invalid item number.", ConsoleColor.Red);
        }
    }
    
    private void SellItem(Character player)
    {
        DisplayMessage("");
        
        if (player.Inventory.Count == 0)
        {
            DisplayMessage("You have nothing to sell.", ConsoleColor.Gray);
            return;
        }
        
        DisplayMessage("Your inventory:", ConsoleColor.Cyan);
        for (int i = 0; i < player.Inventory.Count; i++)
        {
            var item = player.Inventory[i];
            DisplayMessage($"{i + 1}. {item.Name} (worth {item.Value / 2:N0} gold)", ConsoleColor.White);
        }
        
        DisplayMessage("");
        DisplayMessage("Enter item # to sell (0 to cancel): ", ConsoleColor.Yellow, false);
        string input = Console.ReadLine();
        
        if (int.TryParse(input, out int itemIndex) && itemIndex > 0 && itemIndex <= player.Inventory.Count)
        {
            var item = player.Inventory[itemIndex - 1];
            int sellPrice = item.Value / 2; // Magic shop buys at 50% value
            
            // Check if shop wants this item type
            if (item.Type == ObjType.Magic || item.MagicType != MagicItemType.None)
            {
                DisplayMessage($"Sell {item.Name} for {sellPrice:N0} gold? (Y/N): ", ConsoleColor.Yellow, false);
                var confirm = Console.ReadKey().KeyChar.ToString().ToUpper();
                DisplayMessage("");
                
                if (confirm == "Y")
                {
                    player.Inventory.RemoveAt(itemIndex - 1);
                    player.Gold += sellPrice;
                    DisplayMessage("Deal!", ConsoleColor.Green);
                    DisplayMessage($"You sold the {item.Name} for {sellPrice:N0} gold.", ConsoleColor.Gray);
                }
            }
            else
            {
                // Random grumpy response
                var responses = new[]
                {
                    "You are not worth dealing with!",
                    "Hahaha...!",
                    "NO HAGGLING IN MY STORE!",
                    "Pay or get lost!"
                };
                var response = responses[new Random().Next(responses.Length)];
                DisplayMessage($"I don't buy that kind of items, {_ownerName} says.", ConsoleColor.Red);
                DisplayMessage($"{response}, {_ownerName} adds.", ConsoleColor.Red);
            }
        }
    }
    
    private void IdentifyItem(Character player)
    {
        DisplayMessage("");
        
        var unidentifiedItems = player.Inventory.Where(item => !item.IsIdentified).ToList();
        if (unidentifiedItems.Count == 0)
        {
            DisplayMessage("You have no unidentified items.", ConsoleColor.Gray);
            return;
        }
        
        DisplayMessage("Unidentified items:", ConsoleColor.Cyan);
        for (int i = 0; i < unidentifiedItems.Count; i++)
        {
            DisplayMessage($"{i + 1}. {unidentifiedItems[i].Name}", ConsoleColor.White);
        }
        
        DisplayMessage("");
        DisplayMessage($"Identification costs {_identificationCost:N0} gold per item.", ConsoleColor.Gray);
        DisplayMessage("Enter item # to identify (0 to cancel): ", ConsoleColor.Yellow, false);
        string input = Console.ReadLine();
        
        if (int.TryParse(input, out int itemIndex) && itemIndex > 0 && itemIndex <= unidentifiedItems.Count)
        {
            if (player.Gold < _identificationCost)
            {
                DisplayMessage("You don't have enough gold for identification!", ConsoleColor.Red);
                return;
            }
            
            var item = unidentifiedItems[itemIndex - 1];
            DisplayMessage($"Identify {item.Name} for {_identificationCost:N0} gold? (Y/N): ", ConsoleColor.Yellow, false);
            var confirm = Console.ReadKey().KeyChar.ToString().ToUpper();
            DisplayMessage("");
            
            if (confirm == "Y")
            {
                player.Gold -= _identificationCost;
                item.IsIdentified = true;
                
                DisplayMessage($"{_ownerName} examines the {item.Name} carefully...", ConsoleColor.Gray);
                DisplayMessage("");
                DisplayMessage($"The {item.Name} is now identified!", ConsoleColor.Green);
                
                // Show full item details
                DisplayItemDetails(item);
            }
        }
    }
    
    private void DisplayItemDetails(Item item)
    {
        DisplayMessage("═══ Item Properties ═══", ConsoleColor.Cyan);
        DisplayMessage($"Name: {item.Name}", ConsoleColor.White);
        DisplayMessage($"Value: {item.Value:N0} gold", ConsoleColor.Yellow);
        
        if (item.Strength != 0) DisplayMessage($"Strength: {(item.Strength > 0 ? "+" : "")}{item.Strength}", ConsoleColor.Green);
        if (item.Defence != 0) DisplayMessage($"Defence: {(item.Defence > 0 ? "+" : "")}{item.Defence}", ConsoleColor.Green);
        if (item.Attack != 0) DisplayMessage($"Attack: {(item.Attack > 0 ? "+" : "")}{item.Attack}", ConsoleColor.Green);
        if (item.Dexterity != 0) DisplayMessage($"Dexterity: {(item.Dexterity > 0 ? "+" : "")}{item.Dexterity}", ConsoleColor.Green);
        if (item.Wisdom != 0) DisplayMessage($"Wisdom: {(item.Wisdom > 0 ? "+" : "")}{item.Wisdom}", ConsoleColor.Green);
        if (item.MagicProperties.Mana != 0) DisplayMessage($"Mana: {(item.MagicProperties.Mana > 0 ? "+" : "")}{item.MagicProperties.Mana}", ConsoleColor.Blue);
        
        if (item.StrengthRequired > 0) DisplayMessage($"Strength Required: {item.StrengthRequired}", ConsoleColor.Red);
        
        // Disease curing
        if (item.MagicProperties.DiseaseImmunity != CureType.None)
        {
            string cureText = item.MagicProperties.DiseaseImmunity switch
            {
                CureType.All => "It cures Every known disease!",
                CureType.Blindness => "It cures Blindness!",
                CureType.Plague => "It cures the Plague!",
                CureType.Smallpox => "It cures Smallpox!",
                CureType.Measles => "It cures Measles!",
                CureType.Leprosy => "It cures Leprosy!",
                _ => ""
            };
            DisplayMessage(cureText, ConsoleColor.Green);
        }
        
        // Restrictions
        if (item.OnlyForGood) DisplayMessage("This item can only be used by good characters.", ConsoleColor.Blue);
        if (item.OnlyForEvil) DisplayMessage("This item can only be used by evil characters.", ConsoleColor.Red);
        if (item.IsCursed) DisplayMessage($"The {item.Name} is CURSED!", ConsoleColor.DarkRed);
    }
    
    private void BuyHealingPotions(Character player)
    {
        DisplayMessage("");
        
        // Calculate potion price: level × 5
        int potionPrice = player.Level * GameConfig.HealingPotionLevelMultiplier;
        int maxPotionsCanBuy = player.Gold / potionPrice;
        int maxPotionsCanCarry = GameConfig.MaxHealingPotions - player.Healing;
        int maxPotions = Math.Min(maxPotionsCanBuy, maxPotionsCanCarry);
        
        if (player.Gold < potionPrice)
        {
            DisplayMessage("You don't have enough gold!", ConsoleColor.Red);
            return;
        }
        
        if (player.Healing >= GameConfig.MaxHealingPotions)
        {
            DisplayMessage("You already have the maximum number of healing potions!", ConsoleColor.Red);
            return;
        }
        
        if (maxPotions <= 0)
        {
            DisplayMessage("You can't afford any potions!", ConsoleColor.Red);
            return;
        }
        
        DisplayMessage($"Current price is {potionPrice:N0} gold per potion.", ConsoleColor.Gray);
        DisplayMessage($"You have {player.Gold:N0} gold.", ConsoleColor.Gray);
        DisplayMessage($"You have {player.Healing} potions.", ConsoleColor.Gray);
        DisplayMessage("");
        
        DisplayMessage($"How many? (max {maxPotions} potions): ", ConsoleColor.Yellow, false);
        string input = Console.ReadLine();
        
        if (int.TryParse(input, out int quantity) && quantity > 0 && quantity <= maxPotions)
        {
            int totalCost = quantity * potionPrice;
            
            if (player.Gold >= totalCost)
            {
                player.Gold -= totalCost;
                player.Healing += quantity;
                
                DisplayMessage($"Ok, it's a deal. You buy {quantity} potions.", ConsoleColor.Green);
                DisplayMessage($"Total cost: {totalCost:N0} gold.", ConsoleColor.Gray);
            }
            else
            {
                DisplayMessage($"{_ownerName} looks at you and laughs...Who are you trying to fool?", ConsoleColor.Red);
            }
        }
        else
        {
            DisplayMessage("Aborted.", ConsoleColor.Red);
        }
    }
    
    private void TalkToOwner(Character player)
    {
        DisplayMessage("");
        
        var responses = new[]
        {
            $"Welcome to my shop, {GetRaceGreeting(player.Race)}!",
            "These items hold great power, use them wisely.",
            "I've been collecting magical artifacts for centuries.",
            "Beware of cursed items - they can be as dangerous as they are powerful.",
            "The art of identification is not to be taken lightly.",
            "Healing potions? A bargain at these prices!",
            "Magic flows through everything if you know how to look.",
            "Good and evil items choose their wielders carefully."
        };
        
        var response = responses[new Random().Next(responses.Length)];
        DisplayMessage($"{_ownerName} says:", ConsoleColor.Cyan);
        DisplayMessage($"'{response}'", ConsoleColor.White);
        
        // Special responses based on player status
        if (player.Class == CharacterClass.Magician || player.Class == CharacterClass.Sage)
        {
            DisplayMessage("", ConsoleColor.Gray);
            DisplayMessage("I sense magical potential in you. Choose your items carefully.", ConsoleColor.Magenta);
        }
        else if (player.Darkness > 50)
        {
            DisplayMessage("", ConsoleColor.Gray);
            DisplayMessage("Your aura is... interesting. Perhaps you'd be interested in some darker artifacts?", ConsoleColor.DarkRed);
        }
        else if (player.Chivalry > 50)
        {
            DisplayMessage("", ConsoleColor.Gray);
            DisplayMessage("A noble spirit! I have some blessed items that might serve you well.", ConsoleColor.Blue);
        }
    }
    
    /// <summary>
    /// Get current magic shop owner name
    /// </summary>
    public static string GetOwnerName()
    {
        return _ownerName;
    }
    
    /// <summary>
    /// Set magic shop owner name (from configuration)
    /// </summary>
    public static void SetOwnerName(string name)
    {
        if (!string.IsNullOrEmpty(name))
        {
            _ownerName = name;
        }
    }
    
    /// <summary>
    /// Set identification cost (from configuration)
    /// </summary>
    public static void SetIdentificationCost(int cost)
    {
        if (cost > 0)
        {
            _identificationCost = cost;
        }
    }
    
    /// <summary>
    /// Get available magic items for external systems
    /// </summary>
    public static List<Item> GetMagicInventory()
    {
        return new List<Item>(_magicInventory);
    }
} 
