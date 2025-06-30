using UsurperRemake.Utils;
using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Dungeon Location - Pascal-compatible dungeon system
/// Based on DUNGEONC.PAS, DUNGEVC.PAS, DUNGEV2.PAS, and DMAZE.PAS
/// </summary>
public class DungeonLocation : BaseLocation
{
    private List<Character> teammates = new();
    private int currentDungeonLevel = 1;
    private int maxDungeonLevel = 50;
    private DungeonTerrain currentTerrain = DungeonTerrain.Underground;
    private Random dungeonRandom = new Random();
    
    // Pascal-compatible encounter chances
    private const float MonsterEncounterChance = 0.4f;
    private const float TreasureEncounterChance = 0.15f;
    private const float EventEncounterChance = 0.25f;
    private const float EmptyRoomChance = 0.2f;
    
    public DungeonLocation() : base(
        GameLocation.Dungeons,
        "The Dungeons",
        "You stand before the entrance to the ancient dungeons. Dark passages lead deep into the earth."
    ) { }
    
    protected override void SetupLocation()
    {
        base.SetupLocation();
        
        // Initialize dungeon level based on player level
        var playerLevel = GetCurrentPlayer()?.Level ?? 1;
        currentDungeonLevel = Math.Max(1, playerLevel / 3);
        
        if (currentDungeonLevel > maxDungeonLevel)
            currentDungeonLevel = maxDungeonLevel;
    }
    
    protected override void DisplayLocation()
    {
        terminal.ClearScreen();
        
        // ASCII art header
        terminal.SetColor("red");
        terminal.WriteLine("╔═══════════════════════════════════════════════════╗");
        terminal.WriteLine("║                  THE DUNGEONS                    ║");
        terminal.WriteLine("╚═══════════════════════════════════════════════════╝");
        terminal.WriteLine("");
        
        // Dungeon status
        terminal.SetColor("white");
        terminal.WriteLine($"Current Level: {currentDungeonLevel}");
        terminal.WriteLine($"Terrain: {GetTerrainDescription(currentTerrain)}");
        terminal.WriteLine($"Team Size: {teammates.Count + 1} members");
        terminal.WriteLine("");
        
        // Atmospheric description
        terminal.SetColor("gray");
        var descriptions = new[]
        {
            "The air grows thick and cold as you venture deeper...",
            "Strange echoes drift from the dark passages ahead.",
            "Ancient stone walls bear the scars of countless battles.",
            "Flickering torchlight reveals shadowy alcoves and doorways.",
            "The scent of danger and treasure fills the stale air."
        };
        
        terminal.WriteLine(descriptions[dungeonRandom.Next(descriptions.Length)]);
        terminal.WriteLine("");
        
        ShowDungeonMenu();
    }
    
    private void ShowDungeonMenu()
    {
        terminal.SetColor("white");
        terminal.WriteLine("(E)xplore Level     (D)escend Deeper    (A)scend to Surface");
        terminal.WriteLine("(T)eam Management   (S)tatus            (R)est");
        terminal.WriteLine("(M)ap of Area       (Q)uit to Town      (?) Help");
        terminal.WriteLine("");
        
        // Show team members
        if (teammates.Count > 0)
        {
            terminal.SetColor("cyan");
            terminal.WriteLine("Your Team:");
            for (int i = 0; i < teammates.Count; i++)
            {
                var member = teammates[i];
                var status = member.IsAlive ? "Ready" : "Injured";
                terminal.WriteLine($"  {i + 1}. {member.DisplayName} - Level {member.Level} - {status}");
            }
            terminal.WriteLine("");
        }
        
        // Level warning
        if (currentDungeonLevel > GetCurrentPlayer().Level + 5)
        {
            terminal.SetColor("red");
            terminal.WriteLine("⚠ WARNING: This level is extremely dangerous for your level!");
            terminal.WriteLine("");
        }
    }
    
    protected override async Task<bool> ProcessChoice(string choice)
    {
        if (string.IsNullOrWhiteSpace(choice))
            return false;
            
        var upperChoice = choice.ToUpper().Trim();
        
        switch (upperChoice)
        {
            case "E":
                await ExploreLevel();
                return false;
                
            case "D":
                await DescendDeeper();
                return false;
                
            case "A":
                await AscendToSurface();
                return false;
                
            case "T":
                await ManageTeam();
                return false;
                
            case "S":
                await ShowDungeonStatus();
                return false;
                
            case "R":
                await RestInDungeon();
                return false;
                
            case "M":
                await ShowDungeonMap();
                return false;
                
            case "Q":
                await QuitToDungeon();
                return true;
                
            case "?":
                await ShowDungeonHelp();
                return false;
                
            default:
                terminal.WriteLine("Invalid choice! Type ? for help.", "red");
                await Task.Delay(1500);
                return false;
        }
    }
    
    /// <summary>
    /// Main exploration mechanic - Pascal encounter system
    /// </summary>
    private async Task ExploreLevel()
    {
        var currentPlayer = GetCurrentPlayer();
        
        // Check if player has fights left
        if (currentPlayer.Fights <= 0)
        {
            terminal.WriteLine("You have no dungeon fights left today!", "red");
            await Task.Delay(2000);
            return;
        }
        
        terminal.ClearScreen();
        terminal.SetColor("yellow");
        terminal.WriteLine("═══ EXPLORING ═══");
        terminal.WriteLine("");
        
        // Atmospheric exploration text
        await ShowExplorationText();
        
        // Determine encounter type (Pascal-compatible)
        var encounterRoll = dungeonRandom.NextDouble();
        
        if (encounterRoll < MonsterEncounterChance)
        {
            await MonsterEncounter();
        }
        else if (encounterRoll < MonsterEncounterChance + TreasureEncounterChance)
        {
            await TreasureEncounter();
        }
        else if (encounterRoll < MonsterEncounterChance + TreasureEncounterChance + EventEncounterChance)
        {
            await SpecialEventEncounter();
        }
        else
        {
            await EmptyRoomEncounter();
        }
        
        // Use up one fight
        currentPlayer.Fights--;
        
        await Task.Delay(1000);
        await terminal.PressAnyKey();
    }
    
    /// <summary>
    /// Monster encounter - Pascal DUNGEVC.PAS mechanics
    /// </summary>
    private async Task MonsterEncounter()
    {
        terminal.SetColor("red");
        terminal.WriteLine("▼ MONSTER ENCOUNTER ▼");
        terminal.WriteLine("");
        
        // Create monster based on terrain and level (Pascal-compatible)
        var monster = CreateDungeonMonster(true); // Leader monster
        
        terminal.WriteLine($"A {monster.Name} appears!");
        await Task.Delay(2000);
        
        // Combat using our combat engine
        var combatEngine = new CombatEngine(terminal);
        var result = await combatEngine.PlayerVsMonster(GetCurrentPlayer(), monster, teammates);
    }
    
    /// <summary>
    /// Treasure encounter - Pascal treasure mechanics
    /// </summary>
    private async Task TreasureEncounter()
    {
        terminal.SetColor("yellow");
        terminal.WriteLine("★ TREASURE FOUND ★");
        terminal.WriteLine("");
        
        var treasureType = dungeonRandom.Next(3);
        var currentPlayer = GetCurrentPlayer();
        
        switch (treasureType)
        {
            case 0: // Gold treasure (Pascal formula)
                {
                    terminal.WriteLine("You have found a treasure chest!");
                    await Task.Delay(1500);
                    
                    long goldFound = currentDungeonLevel * 1500 + dungeonRandom.Next(5000);
                    long expGained = currentDungeonLevel * 200 + dungeonRandom.Next(5000);
                    
                    terminal.SetColor("green");
                    terminal.WriteLine($"You find {goldFound} gold pieces!");
                    terminal.WriteLine($"You gain {expGained} experience points!");
                    
                    currentPlayer.Gold += goldFound;
                    currentPlayer.Experience += expGained;
                    
                    // Share with team
                    if (teammates.Count > 0)
                    {
                        long teamShare = goldFound / (teammates.Count + 1);
                        foreach (var member in teammates)
                        {
                            if (member.IsAlive)
                            {
                                member.Gold += teamShare;
                                member.Experience += expGained / (teammates.Count + 1);
                            }
                        }
                        terminal.WriteLine($"Your team shares the wealth!");
                    }
                }
                break;
                
            case 1: // Healing potions
                {
                    terminal.WriteLine("The chest is filled with healing potions!");
                    
                    int potionsFound = dungeonRandom.Next(currentPlayer.Level) + 1;
                    potionsFound *= 2; // Pascal formula
                    
                    terminal.SetColor("green");
                    terminal.WriteLine($"You took {potionsFound} potions.");
                    
                    currentPlayer.Healing += potionsFound;
                }
                break;
                
            case 2: // Magic scroll
                {
                    await HandleMagicScroll();
                }
                break;
        }
    }
    
    /// <summary>
    /// Magic scroll encounter - Pascal scroll mechanics
    /// </summary>
    private async Task HandleMagicScroll()
    {
        terminal.WriteLine("You have found a scroll! It reads:");
        terminal.WriteLine("");
        
        var scrollType = dungeonRandom.Next(3);
        var currentPlayer = GetCurrentPlayer();
        
        switch (scrollType)
        {
            case 0: // Blessing scroll
                terminal.SetColor("bright_white");
                terminal.WriteLine("Utter: 'XAVARANTHE JHUSULMAX VASWIUN'");
                terminal.WriteLine("And you will receive a blessing.");
                break;
                
            case 1: // Undead summon scroll  
                terminal.SetColor("red");
                terminal.WriteLine("Utter: 'ZASHNIVANTHE ULIPMAN NO SEE'");
                terminal.WriteLine("And you will see ancient power rise again.");
                break;
                
            case 2: // Secret cave scroll
                terminal.SetColor("cyan");
                terminal.WriteLine("Utter: 'RANTVANTHI SHGELUUIM VARTHMIOPLXH'");
                terminal.WriteLine("And you will be given opportunities.");
                break;
        }
        
        terminal.WriteLine("");
        var recite = await terminal.GetInput("Recite the scroll? (Y/N): ");
        
        if (recite.ToUpper() == "Y")
        {
            await ExecuteScrollMagic(scrollType, currentPlayer);
        }
        else
        {
            terminal.WriteLine("You carefully store the scroll for later.", "gray");
        }
    }
    
    /// <summary>
    /// Execute scroll magic effects
    /// </summary>
    private async Task ExecuteScrollMagic(int scrollType, Character player)
    {
        terminal.WriteLine("");
        terminal.WriteLine("The ancient words resonate with power...", "bright_white");
        await Task.Delay(2000);
        
        switch (scrollType)
        {
            case 0: // Blessing
                {
                    long chivalryGain = dungeonRandom.Next(500) + 50;
                    player.Chivalry += chivalryGain;
                    
                    terminal.SetColor("bright_yellow");
                    terminal.WriteLine("Divine light surrounds you!");
                    terminal.WriteLine($"Your chivalry increases by {chivalryGain}!");
                }
                break;
                
            case 1: // Undead summon (triggers combat)
                {
                    terminal.SetColor("red");
                    terminal.WriteLine("The ground trembles as ancient evil awakens!");
                    await Task.Delay(2000);
                    
                    // Create undead monster
                    var undead = CreateUndeadMonster();
                    terminal.WriteLine($"You have summoned a {undead.Name}!");
                    
                    // Fight the undead
                    var combatEngine = new CombatEngine(terminal);
                    await combatEngine.PlayerVsMonster(player, undead, teammates);
                }
                break;
                
            case 2: // Secret opportunity
                {
                    terminal.SetColor("bright_cyan");
                    terminal.WriteLine("A hidden passage opens before you!");
                    
                    long bonusGold = currentDungeonLevel * 2000;
                    player.Gold += bonusGold;
                    
                    terminal.WriteLine($"You discover {bonusGold} gold in the secret chamber!");
                }
                break;
        }
    }
    
    /// <summary>
    /// Special event encounters - Pascal DUNGEV2.PAS
    /// </summary>
    private async Task SpecialEventEncounter()
    {
        var eventType = dungeonRandom.Next(3);
        
        switch (eventType)
        {
            case 0:
                await MerchantEncounter();
                break;
            case 1:
                await WitchDoctorEncounter();
                break;
            case 2:
                await BeggarEncounter();
                break;
        }
    }
    
    /// <summary>
    /// Merchant encounter - Pascal DUNGEV2.PAS
    /// </summary>
    private async Task MerchantEncounter()
    {
        terminal.SetColor("green");
        terminal.WriteLine("♦ MERCHANT ENCOUNTER ♦");
        terminal.WriteLine("");
        terminal.WriteLine("A traveling merchant appears from the shadows!");
        terminal.WriteLine("'Greetings, brave adventurer! Care to trade?'");
        terminal.WriteLine("");
        
        var choice = await terminal.GetInput("(T)rade with merchant or (A)ttack for goods? ");
        
        if (choice.ToUpper() == "T")
        {
            // Peaceful trading
            terminal.WriteLine("The merchant offers you healing supplies at a fair price.", "green");
            // TODO: Implement trading system
        }
        else if (choice.ToUpper() == "A")
        {
            terminal.WriteLine("You decide to rob the poor merchant!", "red");
            
            // Create merchant monster for combat
            var merchant = CreateMerchantMonster();
            var combatEngine = new CombatEngine(terminal);
            await combatEngine.PlayerVsMonster(GetCurrentPlayer(), merchant, teammates);
            
            // Evil deed
            GetCurrentPlayer().Darkness += 10;
        }
    }
    
    /// <summary>
    /// Witch Doctor encounter - Pascal DUNGEV2.PAS
    /// </summary>
    private async Task WitchDoctorEncounter()
    {
        terminal.SetColor("magenta");
        terminal.WriteLine("🔮 WITCH DOCTOR ENCOUNTER 🔮");
        terminal.WriteLine("");
        
        var currentPlayer = GetCurrentPlayer();
        long cost = currentPlayer.Level * 12500;
        
        if (currentPlayer.Gold >= cost)
        {
            terminal.WriteLine("You meet the evil Witch-Doctor Mbluta!");
            terminal.WriteLine($"He demands {cost} gold or he will curse you!");
            terminal.WriteLine("");
            
            var choice = await terminal.GetInput("(P)ay the witch doctor or (R)un away? ");
            
            if (choice.ToUpper() == "P")
            {
                currentPlayer.Gold -= cost;
                terminal.WriteLine("You reluctantly pay the witch doctor.", "yellow");
                terminal.WriteLine("He vanishes into the darkness...");
            }
            else
            {
                // 50% chance to escape
                if (dungeonRandom.Next(2) == 0)
                {
                    terminal.WriteLine("You manage to flee the evil witch doctor!", "green");
                }
                else
                {
                    terminal.WriteLine("You fail to escape and are cursed!", "red");
                    
                    // Random curse effect
                    var curseType = dungeonRandom.Next(3);
                    switch (curseType)
                    {
                        case 0:
                            var expLoss = currentPlayer.Level * 1500;
                            currentPlayer.Experience = Math.Max(0, currentPlayer.Experience - expLoss);
                            terminal.WriteLine($"You lose {expLoss} experience points!");
                            break;
                        case 1:
                            var fightLoss = dungeonRandom.Next(5) + 1;
                            currentPlayer.Fights = Math.Max(0, currentPlayer.Fights - fightLoss);
                            terminal.WriteLine($"You lose {fightLoss} dungeon fights!");
                            break;
                        case 2:
                            var pfightLoss = dungeonRandom.Next(3) + 1;
                            currentPlayer.PFights = Math.Max(0, currentPlayer.PFights - pfightLoss);
                            terminal.WriteLine($"You lose {pfightLoss} player fights!");
                            break;
                    }
                }
            }
        }
        else
        {
            terminal.WriteLine("A witch doctor appears but sees you have no gold and leaves.", "gray");
        }
        
        await Task.Delay(3000);
    }
    
    /// <summary>
    /// Empty room encounter
    /// </summary>
    private async Task EmptyRoomEncounter()
    {
        terminal.SetColor("gray");
        terminal.WriteLine("○ EMPTY CHAMBER ○");
        terminal.WriteLine("");
        
        var descriptions = new[]
        {
            "You enter an empty stone chamber. Dust motes dance in the torchlight.",
            "This room appears abandoned. Old bones litter the floor.",
            "Ancient murals cover the walls of this empty hall.",
            "You find nothing of interest in this forgotten room.",
            "The chamber echoes with the sounds of your footsteps."
        };
        
        terminal.WriteLine(descriptions[dungeonRandom.Next(descriptions.Length)]);
        terminal.WriteLine("");
        
        // Small chance of hidden treasure in empty rooms
        if (dungeonRandom.NextDouble() < 0.1)
        {
            terminal.WriteLine("Wait... you notice something hidden!");
            long hiddenGold = dungeonRandom.Next(100 * currentDungeonLevel);
            GetCurrentPlayer().Gold += hiddenGold;
            terminal.WriteLine($"You find {hiddenGold} gold hidden in a crack!", "yellow");
        }
        
        await Task.Delay(2000);
    }
    
    /// <summary>
    /// Create dungeon monster based on level and terrain
    /// </summary>
    private Monster CreateDungeonMonster(bool isLeader = false)
    {
        var monsterNames = GetMonsterNamesForTerrain(currentTerrain);
        var weaponArmor = GetWeaponArmorForTerrain(currentTerrain);
        
        var name = monsterNames[dungeonRandom.Next(monsterNames.Length)];
        var weapon = weaponArmor.weapons[dungeonRandom.Next(weaponArmor.weapons.Length)];
        var armor = weaponArmor.armor[dungeonRandom.Next(weaponArmor.armor.Length)];
        
        if (isLeader)
        {
            name = GetLeaderName(name);
        }
        
        // Pascal-compatible monster stats
        long hp = currentDungeonLevel * 5;
        if (isLeader) hp *= 2;
        
        var monster = Monster.CreateMonster(
            nr: currentDungeonLevel,
            name: name,
            hps: hp,
            strength: currentDungeonLevel * 2,
            defence: 0,
            phrase: GetMonsterPhrase(currentTerrain),
            grabweap: dungeonRandom.NextDouble() < 0.3,
            grabarm: false,
            weapon: weapon,
            armor: armor,
            poisoned: false,
            disease: false,
            punch: currentDungeonLevel * 3,
            armpow: currentDungeonLevel * 2,
            weappow: currentDungeonLevel * 2
        );
        
        if (isLeader)
        {
            monster.IsBoss = true;
        }
        
        return monster;
    }
    
    // Helper methods for monster creation
    private string[] GetMonsterNamesForTerrain(DungeonTerrain terrain)
    {
        return terrain switch
        {
            DungeonTerrain.Underground => new[] { "Orc", "Half-Orc", "Goblin", "Troll", "Skeleton" },
            DungeonTerrain.Mountains => new[] { "Mountain Bandit", "Hill Giant", "Stone Golem", "Dwarf Warrior" },
            DungeonTerrain.Desert => new[] { "Robber Knight", "Robber Squire", "Desert Nomad", "Sand Troll" },
            DungeonTerrain.Forest => new[] { "Tree Hunter", "Green Threat", "Forest Bandit", "Wild Beast" },
            DungeonTerrain.Caves => new[] { "Cave Troll", "Underground Drake", "Deep Dweller", "Rock Monster" },
            _ => new[] { "Monster", "Creature", "Beast", "Fiend" }
        };
    }
    
    private (string[] weapons, string[] armor) GetWeaponArmorForTerrain(DungeonTerrain terrain)
    {
        return terrain switch
        {
            DungeonTerrain.Underground => (
                new[] { "Sword", "Spear", "Axe", "Club" },
                new[] { "Leather", "Chain-mail", "Cloth" }
            ),
            DungeonTerrain.Mountains => (
                new[] { "War Hammer", "Battle Axe", "Mace" },
                new[] { "Chain-mail", "Scale Mail", "Plate" }
            ),
            DungeonTerrain.Desert => (
                new[] { "Lance", "Scimitar", "Javelin" },
                new[] { "Chain-Mail", "Leather", "Robes" }
            ),
            DungeonTerrain.Forest => (
                new[] { "Silver Dagger", "Sling", "Sharp Stick", "Bow" },
                new[] { "Cloth", "Leather", "Bark Armor" }
            ),
            _ => (
                new[] { "Rusty Sword", "Broken Spear", "Old Club" },
                new[] { "Torn Clothes", "Rags", "Nothing" }
            )
        };
    }
    
    private string GetLeaderName(string baseName)
    {
        return baseName + " Leader";
    }
    
    private string GetMonsterPhrase(DungeonTerrain terrain)
    {
        var phrases = terrain switch
        {
            DungeonTerrain.Underground => new[] { "Trespasser!", "Attack!", "Kill them!", "No mercy!" },
            DungeonTerrain.Mountains => new[] { "Give yourself up!", "Take no prisoners!", "For the clan!" },
            DungeonTerrain.Desert => new[] { "No prisoners!", "Your gold or your life!", "Die, infidel!" },
            DungeonTerrain.Forest => new[] { "Wrong way, lads!", "Protect the trees!", "Nature's revenge!" },
            _ => new[] { "Grrargh!", "Attack!", "Die!", "No escape!" }
        };
        
        return phrases[dungeonRandom.Next(phrases.Length)];
    }
    
    // Additional helper methods
    private async Task ShowExplorationText()
    {
        var explorationTexts = new[]
        {
            "You cautiously advance through the shadowy corridors...",
            "Your footsteps echo in the ancient stone passages...",
            "Flickering torchlight reveals mysterious doorways ahead...",
            "The air grows colder as you venture deeper into the dungeon...",
            "Strange sounds echo from the darkness beyond..."
        };
        
        terminal.WriteLine(explorationTexts[dungeonRandom.Next(explorationTexts.Length)], "gray");
        await Task.Delay(2000);
    }
    
    private string GetTerrainDescription(DungeonTerrain terrain)
    {
        return terrain switch
        {
            DungeonTerrain.Underground => "Ancient Underground Tunnels",
            DungeonTerrain.Mountains => "Rocky Mountain Passages",
            DungeonTerrain.Desert => "Desert Ruins and Tombs",
            DungeonTerrain.Forest => "Overgrown Forest Caves",
            DungeonTerrain.Caves => "Deep Natural Caverns",
            _ => "Unknown Territory"
        };
    }
    
    // Placeholder methods for features to implement
    private async Task DescendDeeper()
    {
        if (currentDungeonLevel < maxDungeonLevel)
        {
            currentDungeonLevel++;
            terminal.WriteLine($"You descend to dungeon level {currentDungeonLevel}.", "yellow");
        }
        else
        {
            terminal.WriteLine("You cannot go any deeper!", "red");
        }
        await Task.Delay(1500);
    }
    
    private async Task AscendToSurface()
    {
        if (currentDungeonLevel > 1)
        {
            currentDungeonLevel--;
            terminal.WriteLine($"You ascend to dungeon level {currentDungeonLevel}.", "green");
        }
        else
        {
            await NavigateToLocation(GameLocation.MainStreet);
        }
        await Task.Delay(1500);
    }
    
    private async Task ManageTeam()
    {
        terminal.WriteLine("Team management not yet implemented.", "gray");
        await Task.Delay(1500);
    }
    
    private async Task ShowDungeonStatus()
    {
        await ShowStatus();
    }
    
    private async Task RestInDungeon()
    {
        terminal.WriteLine("You rest in a safe alcove and recover some health.", "green");
        var player = GetCurrentPlayer();
        player.HP = Math.Min(player.MaxHP, player.HP + 10);
        await Task.Delay(2000);
    }
    
    private async Task ShowDungeonMap()
    {
        terminal.WriteLine("Dungeon mapping not yet implemented.", "gray");
        await Task.Delay(1500);
    }
    
    private async Task QuitToDungeon()
    {
        await NavigateToLocation(GameLocation.MainStreet);
    }
    
    private async Task ShowDungeonHelp()
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_cyan");
        terminal.WriteLine("Dungeon Help");
        terminal.WriteLine("============");
        terminal.WriteLine("");
        terminal.SetColor("white");
        terminal.WriteLine("E - Explore the current level (uses 1 dungeon fight)");
        terminal.WriteLine("D - Descend to a deeper, more dangerous level");
        terminal.WriteLine("A - Ascend to a safer level or return to town");
        terminal.WriteLine("T - Manage your team members");
        terminal.WriteLine("S - View your character status");
        terminal.WriteLine("R - Rest to recover health");
        terminal.WriteLine("M - View the dungeon map");
        terminal.WriteLine("Q - Quit and return to town");
        terminal.WriteLine("");
        await terminal.PressAnyKey();
    }
    
    // Additional encounter methods
    private async Task BeggarEncounter()
    {
        terminal.SetColor("cyan");
        terminal.WriteLine("☂ BEGGAR ENCOUNTER ☂");
        terminal.WriteLine("");
        terminal.WriteLine("A poor beggar approaches you with outstretched hands.");
        terminal.WriteLine("'Please, kind sir/madam, spare some gold for a poor soul?'");
        terminal.WriteLine("");
        
        var choice = await terminal.GetInput("(G)ive gold to beggar or (I)gnore them? ");
        
        if (choice.ToUpper() == "G")
        {
            var currentPlayer = GetCurrentPlayer();
            if (currentPlayer.Gold >= 10)
            {
                currentPlayer.Gold -= 10;
                currentPlayer.Chivalry += 5;
                terminal.WriteLine("The beggar thanks you profusely for your kindness!", "green");
                terminal.WriteLine("Your chivalry increases!");
            }
            else
            {
                terminal.WriteLine("You don't have enough gold to spare.", "red");
            }
        }
        else
        {
            terminal.WriteLine("You ignore the beggar and continue on your way.", "gray");
        }
        
        await Task.Delay(2000);
    }
    
    private Monster CreateMerchantMonster()
    {
        return Monster.CreateMonster(1, "Frightened Merchant", 30, 10, 0,
            "Help me!", false, false, "Walking Stick", "Robes", 
            false, false, 5, 1, 3);
    }
    
    private Monster CreateUndeadMonster()
    {
        var names = new[] { "Undead", "Zombie", "Skeleton Warrior" };
        var name = names[dungeonRandom.Next(names.Length)];
        
        return Monster.CreateMonster(currentDungeonLevel, name, 
            currentDungeonLevel * 5, currentDungeonLevel * 2, 0,
            "...", false, false, "Rusty Sword", "Tattered Armor",
            false, false, currentDungeonLevel * 70, 0, currentDungeonLevel * 2);
    }
}

/// <summary>
/// Dungeon terrain types affecting encounters
/// </summary>
public enum DungeonTerrain
{
    Underground,
    Mountains, 
    Desert,
    Forest,
    Caves
} 
