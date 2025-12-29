using UsurperRemake.Utils;
using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// The Inn location - social hub with Seth Able, drinking, and team activities
/// Based on Pascal INN.PAS and INNC.PAS
/// </summary>
public class InnLocation : BaseLocation
{
    private NPC sethAble;
    private bool sethAbleAvailable = true;
    
    public InnLocation() : base(
        GameLocation.TheInn,
        "The Inn",
        "You enter the smoky tavern. The air is thick with the smell of ale and the sound of rowdy conversation."
    ) { }
    
    protected override void SetupLocation()
    {
        // Pascal-compatible exits from ONLINE.PAS onloc_theinn case
        PossibleExits = new List<GameLocation>
        {
            GameLocation.MainStreet,    // loc1 - back to main street
            GameLocation.TeamCorner,    // loc2 - team corner
            GameLocation.Recruit        // loc3 - hall of recruitment
        };
        
        // Inn-specific actions
        LocationActions = new List<string>
        {
            "Buy a drink (5 gold)",         // Drinking system
            "Challenge Seth Able",          // Fight Seth Able
            "Talk to patrons",              // Social interaction  
            "Play drinking game",           // Drinking competition
            "Listen to rumors",             // Information gathering
            "Check bulletin board",         // News and messages
            "Rest at table",                // Minor healing
            "Order food (10 gold)"          // Stamina boost
        };
        
        // Create Seth Able NPC
        CreateSethAble();
    }
    
    /// <summary>
    /// Create the famous Seth Able NPC
    /// </summary>
    private void CreateSethAble()
    {
        sethAble = new NPC("Seth Able", "drunk_fighter", CharacterClass.Warrior, 15)
        {
            IsSpecialNPC = true,
            SpecialScript = "drunk_fighter",
            IsHostile = false,
            CurrentLocation = "inn"
        };
        
        // Set Seth Able's stats (he's tough!)
        sethAble.Strength = 45;
        sethAble.Defence = 35;
        sethAble.HP = 200;
        sethAble.MaxHP = 200;
        sethAble.Level = 15;
        sethAble.Experience = 50000;
        sethAble.Gold = 1000;
        
        // Seth is usually drunk
        sethAble.Mental = 30; // Poor mental state from drinking
        
        AddNPC(sethAble);
    }
    
    protected override void DisplayLocation()
    {
        terminal.ClearScreen();
        
        // Inn header with ASCII art
        terminal.SetColor("bright_yellow");
        terminal.WriteLine("╔══════════════════════════════════════════════════════════════════════════════╗");
        terminal.WriteLine("║                                THE INN                                      ║");
        terminal.WriteLine("║                          'The Drunken Dragon'                               ║");
        terminal.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝");
        terminal.WriteLine("");
        
        // Atmospheric description
        terminal.SetColor("white");
        terminal.WriteLine("The inn is dimly lit by flickering candles. Rough wooden tables are occupied");
        terminal.WriteLine("by travelers, merchants, and local toughs. The bartender eyes you suspiciously.");
        terminal.WriteLine("");
        
        // Special Seth Able description
        if (sethAbleAvailable)
        {
            terminal.SetColor("red");
            terminal.WriteLine("Seth Able, the notorious drunk fighter, sits hunched over a tankard in");
            terminal.WriteLine("the corner. His bloodshot eyes survey the room, looking for trouble.");
            terminal.WriteLine("");
        }
        
        // Show other NPCs
        ShowNPCsInLocation();
        
        // Show inn-specific menu
        ShowInnMenu();
        
        // Status line
        ShowStatusLine();
    }
    
    /// <summary>
    /// Show Inn-specific menu options
    /// </summary>
    private void ShowInnMenu()
    {
        terminal.SetColor("yellow");
        terminal.WriteLine("Inn Activities:");
        terminal.WriteLine("");

        // Row 1
        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("bright_yellow");
        terminal.Write("D");
        terminal.SetColor("darkgray");
        terminal.Write("] ");
        terminal.SetColor("white");
        terminal.Write("Buy a drink (5 gold)      ");

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("bright_cyan");
        terminal.Write("T");
        terminal.SetColor("darkgray");
        terminal.Write("] ");
        terminal.SetColor("white");
        terminal.WriteLine("Talk to patrons");

        // Row 2
        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("bright_red");
        terminal.Write("F");
        terminal.SetColor("darkgray");
        terminal.Write("] ");
        terminal.SetColor("white");
        terminal.Write("Challenge Seth Able       ");

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("bright_magenta");
        terminal.Write("G");
        terminal.SetColor("darkgray");
        terminal.Write("] ");
        terminal.SetColor("white");
        terminal.WriteLine("Play drinking game");

        // Row 3
        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("cyan");
        terminal.Write("R");
        terminal.SetColor("darkgray");
        terminal.Write("] ");
        terminal.SetColor("white");
        terminal.Write("Listen to rumors          ");

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("yellow");
        terminal.Write("B");
        terminal.SetColor("darkgray");
        terminal.Write("] ");
        terminal.SetColor("white");
        terminal.WriteLine("Check bulletin board");

        // Row 4
        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("green");
        terminal.Write("E");
        terminal.SetColor("darkgray");
        terminal.Write("] ");
        terminal.SetColor("white");
        terminal.Write("Rest at table             ");

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("bright_green");
        terminal.Write("O");
        terminal.SetColor("darkgray");
        terminal.Write("] ");
        terminal.SetColor("white");
        terminal.WriteLine("Order food (10 gold)");
        terminal.WriteLine("");

        terminal.SetColor("cyan");
        terminal.WriteLine("Special Areas:");

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("bright_cyan");
        terminal.Write("C");
        terminal.SetColor("darkgray");
        terminal.Write("] ");
        terminal.SetColor("white");
        terminal.Write("Team Corner              ");

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("magenta");
        terminal.Write("H");
        terminal.SetColor("darkgray");
        terminal.Write("] ");
        terminal.SetColor("white");
        terminal.WriteLine("Hall of Recruitment");
        terminal.WriteLine("");

        terminal.SetColor("yellow");
        terminal.WriteLine("Navigation:");

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("bright_red");
        terminal.Write("M");
        terminal.SetColor("darkgray");
        terminal.Write("] ");
        terminal.SetColor("red");
        terminal.Write("Return to Main Street    ");

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("cyan");
        terminal.Write("S");
        terminal.SetColor("darkgray");
        terminal.Write("] ");
        terminal.SetColor("white");
        terminal.Write("Status    ");

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("yellow");
        terminal.Write("?");
        terminal.SetColor("darkgray");
        terminal.Write("] ");
        terminal.SetColor("white");
        terminal.WriteLine("Help");
        terminal.WriteLine("");
    }
    
    protected override async Task<bool> ProcessChoice(string choice)
    {
        if (string.IsNullOrWhiteSpace(choice))
            return false;
            
        var upperChoice = choice.ToUpper().Trim();
        
        switch (upperChoice)
        {
            case "D":
                await BuyDrink();
                return false;
                
            case "F":
                await ChallengeSethAble();
                return false;
                
            case "T":
                await TalkToPatrons();
                return false;
                
            case "G":
                await PlayDrinkingGame();
                return false;
                
            case "R":
                await ListenToRumors();
                return false;
                
            case "B":
                await CheckBulletinBoard();
                return false;
                
            case "E":
                await RestAtTable();
                return false;
                
            case "O":
                await OrderFood();
                return false;
                
            case "C":
                await NavigateToLocation(GameLocation.TeamCorner);
                return true;
                
            case "H":
                await NavigateToLocation(GameLocation.Recruit);
                return true;
                
            case "M":
                await NavigateToLocation(GameLocation.MainStreet);
                return true;
                
            case "S":
                await ShowStatus();
                return false;
                
            case "?":
                // Menu already shown
                return false;
                
            default:
                terminal.WriteLine("Invalid choice! The bartender shakes his head.", "red");
                await Task.Delay(1500);
                return false;
        }
    }
    
    /// <summary>
    /// Buy a drink at the inn
    /// </summary>
    private async Task BuyDrink()
    {
        if (currentPlayer.Gold < 5)
        {
            terminal.WriteLine("You don't have enough gold for a drink!", "red");
            await Task.Delay(2000);
            return;
        }
        
        currentPlayer.Gold -= 5;
        currentPlayer.DrinksLeft--;
        
        terminal.SetColor("green");
        terminal.WriteLine("You order a tankard of ale from the bartender.");
        terminal.WriteLine("The bitter brew slides down your throat...");
        
        // Random drink effects
        var effect = GD.RandRange(1, 4);
        switch (effect)
        {
            case 1:
                terminal.WriteLine("The ale boosts your confidence! (+2 Charisma temporarily)");
                currentPlayer.Charisma += 2;
                break;
            case 2:
                terminal.WriteLine("You feel slightly dizzy but stronger! (+1 Strength temporarily)");
                currentPlayer.Strength += 1;
                break;
            case 3:
                terminal.WriteLine("The alcohol makes you reckless! (-1 Wisdom temporarily)");
                currentPlayer.Wisdom = Math.Max(1, currentPlayer.Wisdom - 1);
                break;
            case 4:
                terminal.WriteLine("You feel relaxed and restored. (+5 HP)");
                currentPlayer.HP = Math.Min(currentPlayer.MaxHP, currentPlayer.HP + 5);
                break;
        }
        
        await Task.Delay(2500);
    }
    
    /// <summary>
    /// Challenge Seth Able to a fight
    /// </summary>
    private async Task ChallengeSethAble()
    {
        if (!sethAbleAvailable)
        {
            terminal.WriteLine("Seth Able is not here right now.", "gray");
            await Task.Delay(1500);
            return;
        }
        
        terminal.ClearScreen();
        terminal.SetColor("red");
        terminal.WriteLine("CHALLENGING SETH ABLE");
        terminal.WriteLine("====================");
        terminal.WriteLine("");
        
        // Seth's drunken response
        var responses = new[]
        {
            "*hiccup* You want a piece of me?!",
            "You lookin' at me funny, stranger?",
            "*burp* Think you can take the great Seth Able?",
            "I'll show you what a REAL fighter can do!",
            "*sways* Come on then, if you think you're hard enough!"
        };
        
        terminal.SetColor("yellow");
        terminal.WriteLine($"Seth Able: \"{responses[GD.RandRange(0, responses.Length - 1)]}\"");
        terminal.WriteLine("");
        
        terminal.SetColor("white");
        terminal.WriteLine("WARNING: Seth Able is a dangerous opponent!");
        terminal.WriteLine($"Seth Able - Level {sethAble.Level} - HP: {sethAble.HP}");
        terminal.WriteLine($"You - Level {currentPlayer.Level} - HP: {currentPlayer.HP}");
        terminal.WriteLine("");
        
        var confirm = await terminal.GetInput("Are you sure you want to fight? (y/N): ");
        
        if (confirm.ToUpper() == "Y")
        {
            await FightSethAble();
        }
        else
        {
            terminal.WriteLine("Seth Able: \"Hah! Smart choice, coward!\"", "yellow");
            await Task.Delay(2000);
        }
    }
    
    /// <summary>
    /// Fight Seth Able using full combat engine
    /// </summary>
    private async Task FightSethAble()
    {
        terminal.WriteLine("The inn falls silent as you approach Seth Able...", "red");
        await Task.Delay(2000);
        
        // Create Seth as a monster for combat (Pascal-compatible)
        var sethMonster = Monster.CreateMonster(
            nr: 99,                     // Special monster number for Seth
            name: "Seth Able",
            hps: 150,                   // High HP for a tough fighter
            strength: 35,               // Strong fighter
            defence: 20,                // Good natural defense
            phrase: "You lookin' at me funny?!",
            grabweap: false,            // Can't take Seth's stuff
            grabarm: false,
            weapon: "Massive Fists",
            armor: "Thick Skin",
            poisoned: false,
            disease: false,
            punch: 40,                  // High punch power
            armpow: 15,                 // Natural armor
            weappow: 25                 // Weapon power of fists
        );
        
        // Seth is a special unique NPC
        sethMonster.IsUnique = true;
        sethMonster.IsBoss = false;
        sethMonster.CanSpeak = true;
        
        // Initialize combat engine
        var combatEngine = new CombatEngine(terminal);
        
        // Execute combat
        var result = await combatEngine.PlayerVsMonster(currentPlayer, sethMonster);
        
        // Handle combat outcome
        switch (result.Outcome)
        {
            case CombatOutcome.Victory:
                // Player wins (rare!)
                terminal.SetColor("bright_green");
                terminal.WriteLine("");
                terminal.WriteLine("INCREDIBLE! You have defeated Seth Able!");
                terminal.WriteLine("The entire inn erupts in shocked silence...");
                terminal.WriteLine("Even the bartender drops his glass in amazement!");
                terminal.WriteLine("");
                terminal.WriteLine("You are now a legend in this tavern!");
                
                // Pascal-compatible rewards for defeating Seth
                currentPlayer.Experience += 1000;
                currentPlayer.Gold += 500;
                currentPlayer.PKills++;
                currentPlayer.Fame += 10;          // Fame for defeating Seth
                currentPlayer.Chivalry += 5;       // Chivalrous victory
                
                // Seth becomes unavailable for a while (Pascal: NPCs can be "knocked out")
                sethAbleAvailable = false;
                sethAble.SetState(NPCState.Unconscious);
                
                terminal.WriteLine("You gain legendary status among the patrons!");
                break;
                
            case CombatOutcome.PlayerDied:
                // Player died (should be rare vs Seth, he's more of a brawler)
                terminal.SetColor("red");
                terminal.WriteLine("");
                terminal.WriteLine("Seth Able's powerful blow knocks you unconscious!");
                terminal.WriteLine("You wake up later with a massive headache...");
                
                // In Pascal, inn fights rarely kill, more like knockout
                currentPlayer.HP = 1;  // Leave player at 1 HP instead of dead
                currentPlayer.PDefeats++;
                break;
                
            case CombatOutcome.PlayerEscaped:
                terminal.SetColor("yellow");
                terminal.WriteLine("");
                terminal.WriteLine("You manage to back away from Seth Able!");
                terminal.WriteLine("'That's right, walk away!' Seth calls after you.");
                terminal.WriteLine("The other patrons chuckle at your retreat.");
                break;
                
            default:
                // Seth wins (usual outcome)
                terminal.SetColor("red");
                terminal.WriteLine("");
                terminal.WriteLine("Seth Able's massive fist connects with your jaw!");
                terminal.WriteLine("You crash into a table and slide to the floor...");
                terminal.WriteLine("The patrons laugh as Seth returns to his drink.");
                terminal.WriteLine("");
                terminal.WriteLine("'Maybe next time, kid!' Seth gruffs.");
                
                currentPlayer.PDefeats++;
                break;
        }
        
        await Task.Delay(3000);
    }
    
    /// <summary>
    /// Talk to other patrons
    /// </summary>
    private async Task TalkToPatrons()
    {
        terminal.ClearScreen();
        terminal.SetColor("cyan");
        terminal.WriteLine("Talking to Patrons");
        terminal.WriteLine("==================");
        terminal.WriteLine("");
        
        var conversations = new[]
        {
            "A merchant tells you about rare items in the marketplace.",
            "A guard mentions increased monster activity in the dungeons.",
            "A traveler warns you about bandits on the roads.",
            "A local farmer complains about the King's new taxes.",
            "A mysterious woman hints about secrets in the Dark Alley.",
            "An old man tells tales of the legendary Death Knight."
        };
        
        terminal.SetColor("white");
        terminal.WriteLine(conversations[GD.RandRange(0, conversations.Length - 1)]);
        terminal.WriteLine("");
        
        if (GD.Randf() < 0.3f)
        {
            terminal.SetColor("green");
            terminal.WriteLine("You make a good impression and gain +1 Charisma!");
            currentPlayer.Charisma++;
        }
        
        await terminal.PressAnyKey();
    }
    
    /// <summary>
    /// Play drinking game
    /// </summary>
    private async Task PlayDrinkingGame()
    {
        if (currentPlayer.Gold < 20)
        {
            terminal.WriteLine("You need at least 20 gold to enter the drinking contest!", "red");
            await Task.Delay(2000);
            return;
        }
        
        terminal.ClearScreen();
        terminal.SetColor("bright_yellow");
        terminal.WriteLine("DRINKING CONTEST");
        terminal.WriteLine("================");
        terminal.WriteLine("");
        
        currentPlayer.Gold -= 20;
        
        var rounds = 0;
        var maxRounds = currentPlayer.Stamina / 10;
        
        while (rounds < maxRounds && GD.Randf() < 0.7f)
        {
            rounds++;
            terminal.WriteLine($"Round {rounds}: You down another drink!", "yellow");
            await Task.Delay(1000);
        }
        
        terminal.WriteLine("");
        if (rounds >= 5)
        {
            terminal.SetColor("green");
            terminal.WriteLine($"You won the contest after {rounds} rounds!");
            terminal.WriteLine("You win 100 gold and gain reputation!");
            currentPlayer.Gold += 100;
            currentPlayer.Charisma += 2;
        }
        else
        {
            terminal.SetColor("red");
            terminal.WriteLine($"You lasted {rounds} rounds before passing out.");
            terminal.WriteLine("You wake up with a headache but no permanent damage.");
        }
        
        await Task.Delay(3000);
    }
    
    /// <summary>
    /// Listen to rumors
    /// </summary>
    private async Task ListenToRumors()
    {
        terminal.ClearScreen();
        terminal.SetColor("magenta");
        terminal.WriteLine("Tavern Rumors");
        terminal.WriteLine("=============");
        terminal.WriteLine("");
        
        var rumors = new[]
        {
            "They say the King is planning to increase the royal guard...",
            "Word is that someone found a magical sword in the dungeons last week.",
            "The priests at the temple are worried about strange omens.",
            "A new monster has been spotted in the lower dungeon levels.",
            "The weapon shop is expecting a shipment of rare items soon."
        };
        
        terminal.SetColor("white");
        for (int i = 0; i < 3; i++)
        {
            terminal.WriteLine($"• {rumors[GD.RandRange(0, rumors.Length - 1)]}");
        }
        
        terminal.WriteLine("");
        await terminal.PressAnyKey();
    }
    
    /// <summary>
    /// Check bulletin board
    /// </summary>
    private async Task CheckBulletinBoard()
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_cyan");
        terminal.WriteLine("Inn Bulletin Board");
        terminal.WriteLine("==================");
        terminal.WriteLine("");
        
        terminal.SetColor("white");
        terminal.WriteLine("NOTICES:");
        terminal.WriteLine("• WANTED: Brave adventurers for dungeon exploration");
        terminal.WriteLine("• REWARD: 500 gold for information on the missing merchant");
        terminal.WriteLine("• WARNING: Increased bandit activity on eastern roads");
        terminal.WriteLine("• FOR SALE: Enchanted leather armor, contact Gareth");
        terminal.WriteLine("• TEAM RECRUITMENT: The Iron Wolves are seeking members");
        terminal.WriteLine("");
        
        await terminal.PressAnyKey();
    }
    
    /// <summary>
    /// Rest at table for minor healing
    /// </summary>
    private async Task RestAtTable()
    {
        terminal.WriteLine("You find a quiet corner and rest for a while...", "green");
        await Task.Delay(2000);
        
        var healing = Math.Min(10, currentPlayer.MaxHP - currentPlayer.HP);
        if (healing > 0)
        {
            currentPlayer.HP += healing;
            terminal.WriteLine($"You feel refreshed and recover {healing} HP.", "green");
        }
        else
        {
            terminal.WriteLine("You are already at full health.", "white");
        }
        
        await Task.Delay(2000);
    }
    
    /// <summary>
    /// Order food for stamina boost
    /// </summary>
    private async Task OrderFood()
    {
        if (currentPlayer.Gold < 10)
        {
            terminal.WriteLine("You don't have enough gold for a meal!", "red");
            await Task.Delay(2000);
            return;
        }
        
        currentPlayer.Gold -= 10;
        
        terminal.WriteLine("You order a hearty meal of roasted meat and bread.", "green");
        terminal.WriteLine("The food fills your belly and boosts your stamina!");
        
        currentPlayer.Stamina += 5;
        var healing = Math.Min(15, currentPlayer.MaxHP - currentPlayer.HP);
        if (healing > 0)
        {
            currentPlayer.HP += healing;
            terminal.WriteLine($"You also recover {healing} HP from the nourishing meal.", "green");
        }
        
        await Task.Delay(2500);
    }
} 
