using UsurperRemake.Utils;
using UsurperRemake.Systems;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Talk to other patrons - now with interactive NPC selection
    /// </summary>
    private async Task TalkToPatrons()
    {
        terminal.ClearScreen();
        terminal.SetColor("cyan");
        terminal.WriteLine("Mingle with Patrons");
        terminal.WriteLine("===================");
        terminal.WriteLine("");

        // Get live NPCs at the Inn
        var npcsHere = GetLiveNPCsAtLocation();

        if (npcsHere.Count == 0)
        {
            terminal.SetColor("gray");
            terminal.WriteLine("The inn is quiet tonight. No interesting patrons to talk to.");
            await terminal.PressAnyKey();
            return;
        }

        // Show NPCs with interaction options
        terminal.SetColor("white");
        terminal.WriteLine("You see the following patrons here:");
        terminal.WriteLine("");

        for (int i = 0; i < Math.Min(npcsHere.Count, 8); i++)
        {
            var npc = npcsHere[i];
            var alignColor = npc.Darkness > npc.Chivalry ? "red" : (npc.Chivalry > 500 ? "bright_green" : "cyan");
            terminal.SetColor(alignColor);
            terminal.WriteLine($"  [{i + 1}] {npc.Name2} - Level {npc.Level} {npc.Class} ({GetAlignmentDisplay(npc)})");
        }

        terminal.WriteLine("");
        terminal.SetColor("yellow");
        terminal.WriteLine("[0] Return to inn menu");
        terminal.WriteLine("");

        var choice = await terminal.GetInput("Choose someone to approach (0-8): ");

        if (int.TryParse(choice, out int npcIndex) && npcIndex > 0 && npcIndex <= Math.Min(npcsHere.Count, 8))
        {
            await InteractWithNPC(npcsHere[npcIndex - 1]);
        }
    }

    /// <summary>
    /// Interactive menu for NPC interaction (Inn-specific override)
    /// </summary>
    protected override async Task InteractWithNPC(NPC npc)
    {
        bool continueInteraction = true;

        while (continueInteraction)
        {
            terminal.ClearScreen();
            terminal.SetColor("bright_cyan");
            terminal.WriteLine($"Interacting with {npc.Name2}");
            terminal.WriteLine(new string('─', 30 + npc.Name2.Length));
            terminal.WriteLine("");

            // Show NPC info
            terminal.SetColor("white");
            terminal.WriteLine($"  Level {npc.Level} {npc.Class}");
            terminal.WriteLine($"  {GetNPCMood(npc)}");
            terminal.WriteLine("");

            // Get relationship status
            var relationship = RelationshipSystem.GetRelationshipStatus(currentPlayer, npc);
            terminal.SetColor(GetRelationshipColor(relationship));
            terminal.WriteLine($"  Relationship: {GetRelationshipText(relationship)}");

            // Show alignment compatibility
            var reactionMod = AlignmentSystem.Instance.GetNPCReactionModifier(currentPlayer, npc);
            if (reactionMod >= 1.3f)
            {
                terminal.SetColor("bright_green");
                terminal.WriteLine($"  Alignment: Kindred spirits (excellent rapport)");
            }
            else if (reactionMod >= 1.0f)
            {
                terminal.SetColor("green");
                terminal.WriteLine($"  Alignment: Compatible (good rapport)");
            }
            else if (reactionMod >= 0.7f)
            {
                terminal.SetColor("yellow");
                terminal.WriteLine($"  Alignment: Neutral (standard rapport)");
            }
            else
            {
                terminal.SetColor("red");
                terminal.WriteLine($"  Alignment: Opposing (poor rapport)");
            }
            terminal.WriteLine("");

            // Show interaction options
            terminal.SetColor("yellow");
            terminal.WriteLine("What would you like to do?");
            terminal.WriteLine("");

            terminal.SetColor("white");
            terminal.WriteLine("[T] Talk - Make casual conversation");
            terminal.WriteLine("[F] Flirt - Show romantic interest");
            terminal.WriteLine("[C] Challenge - Challenge to a duel");
            terminal.WriteLine("[G] Gift - Give a gift (costs 50 gold)");

            // Marriage option if relationship is high enough
            if (relationship >= GameConfig.RelationLove)
            {
                terminal.SetColor("bright_magenta");
                terminal.WriteLine("[P] Propose - Ask for their hand in marriage!");
            }

            terminal.WriteLine("");
            terminal.SetColor("gray");
            terminal.WriteLine("[0] Return");
            terminal.WriteLine("");

            var choice = await terminal.GetInput("Your choice: ");

            switch (choice.ToUpper())
            {
                case "T":
                    await TalkToNPC(npc);
                    break;
                case "F":
                    await FlirtWithNPC(npc);
                    break;
                case "C":
                    await ChallengeNPC(npc);
                    continueInteraction = false; // Exit after combat
                    break;
                case "G":
                    await GiveGiftToNPC(npc);
                    break;
                case "P":
                    if (relationship >= GameConfig.RelationLove)
                    {
                        await ProposeToNPC(npc);
                        continueInteraction = false;
                    }
                    break;
                case "0":
                    continueInteraction = false;
                    break;
            }
        }
    }

    /// <summary>
    /// Have a conversation with an NPC
    /// </summary>
    private async Task TalkToNPC(NPC npc)
    {
        terminal.ClearScreen();
        terminal.SetColor("cyan");
        terminal.WriteLine($"Talking to {npc.Name2}");
        terminal.WriteLine("");

        // Get alignment-based reaction modifier
        var reactionModifier = AlignmentSystem.Instance.GetNPCReactionModifier(currentPlayer, npc);
        var random = new Random();
        var conversations = new List<string>();

        // Apply alignment reaction to conversation tone
        bool goodReaction = reactionModifier >= 1.0f;
        bool badReaction = reactionModifier < 0.8f;

        if (npc.Darkness > npc.Chivalry)
        {
            if (badReaction)
            {
                conversations.AddRange(new[] {
                    $"{npc.Name2} scowls at your virtuous presence. \"Get lost, do-gooder.\"",
                    $"{npc.Name2} spits, \"Your kind makes me sick.\"",
                    $"{npc.Name2} turns away. \"I don't deal with holy types.\""
                });
            }
            else
            {
                conversations.AddRange(new[] {
                    $"{npc.Name2} nods approvingly. \"I sense a kindred spirit.\"",
                    $"{npc.Name2} grins, \"You've got that look in your eyes. Good.\"",
                    $"{npc.Name2} sneers, \"Together we could do some real damage.\""
                });
            }
        }
        else if (npc.Chivalry > 500)
        {
            if (badReaction)
            {
                conversations.AddRange(new[] {
                    $"{npc.Name2} frowns at your dark aura. \"I sense evil in you.\"",
                    $"{npc.Name2} backs away. \"Stay away from me, dark one.\"",
                    $"{npc.Name2} warns, \"Change your ways or face judgment.\""
                });
            }
            else
            {
                conversations.AddRange(new[] {
                    $"{npc.Name2} greets you warmly. \"Well met, noble traveler!\"",
                    $"{npc.Name2} smiles, \"It's always good to meet fellow adventurers.\"",
                    $"{npc.Name2} says, \"May the gods watch over your journey.\""
                });
            }
        }
        else
        {
            conversations.AddRange(new[] {
                $"{npc.Name2} nods in greeting. \"How goes it, stranger?\"",
                $"{npc.Name2} remarks, \"The dungeon has been busy lately.\"",
                $"{npc.Name2} says, \"I'm just passing through, same as everyone.\"",
                $"{npc.Name2} mentions, \"The market had some interesting items today.\"",
            });
        }

        terminal.SetColor("white");
        terminal.WriteLine(conversations[random.Next(conversations.Count)]);
        terminal.WriteLine("");

        // Relationship boost modified by alignment compatibility
        int baseBoost = 1;
        int modifiedBoost = (int)(baseBoost * reactionModifier);
        modifiedBoost = Math.Max(-2, Math.Min(3, modifiedBoost)); // Clamp to -2 to +3

        RelationshipSystem.UpdateRelationship(currentPlayer, npc, modifiedBoost, modifiedBoost, false, false);

        if (modifiedBoost > 1)
        {
            terminal.SetColor("bright_green");
            terminal.WriteLine($"(Your alignment resonates with theirs! Relationship improves more.)");
        }
        else if (modifiedBoost <= 0)
        {
            terminal.SetColor("red");
            terminal.WriteLine($"(Your alignment clashes with theirs. Relationship worsens.)");
        }
        else
        {
            terminal.SetColor("green");
            terminal.WriteLine("(Your relationship with them improves slightly)");
        }

        await terminal.PressAnyKey();
    }

    /// <summary>
    /// Flirt with an NPC
    /// </summary>
    private async Task FlirtWithNPC(NPC npc)
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_magenta");
        terminal.WriteLine($"Flirting with {npc.Name2}");
        terminal.WriteLine("");

        var random = new Random();

        // Success based on Charisma
        int successChance = (int)Math.Min(80, 30 + currentPlayer.Charisma);
        bool success = random.Next(100) < successChance;

        if (success)
        {
            var responses = new[] {
                $"{npc.Name2} blushes and laughs. \"Well, aren't you charming!\"",
                $"{npc.Name2}'s eyes sparkle. \"You certainly know how to flatter someone.\"",
                $"{npc.Name2} smiles warmly. \"I like your style, adventurer.\"",
                $"{npc.Name2} leans closer. \"Perhaps we should talk more often...\""
            };

            terminal.SetColor("bright_green");
            terminal.WriteLine(responses[random.Next(responses.Length)]);
            terminal.WriteLine("");

            // Big relationship boost
            RelationshipSystem.UpdateRelationship(currentPlayer, npc, 1, 3, false, false);
            terminal.SetColor("green");
            terminal.WriteLine("(Your relationship improves significantly!)");

            // Small chance they become romantically interested
            if (random.Next(10) == 0)
            {
                terminal.SetColor("bright_magenta");
                terminal.WriteLine($"{npc.Name2} seems particularly interested in you...");
            }
        }
        else
        {
            var rejections = new[] {
                $"{npc.Name2} looks away awkwardly. \"That's... flattering, I suppose.\"",
                $"{npc.Name2} chuckles. \"Nice try, but I'm not that easy to impress.\"",
                $"{npc.Name2} raises an eyebrow. \"You might want to work on your approach.\"",
                $"{npc.Name2} just shakes their head and takes a drink."
            };

            terminal.SetColor("yellow");
            terminal.WriteLine(rejections[random.Next(rejections.Length)]);
            terminal.WriteLine("");
            terminal.SetColor("gray");
            terminal.WriteLine("(That didn't go as planned...)");
        }

        await terminal.PressAnyKey();
    }

    /// <summary>
    /// Challenge an NPC to a duel
    /// </summary>
    private async Task ChallengeNPC(NPC npc)
    {
        terminal.ClearScreen();
        terminal.SetColor("red");
        terminal.WriteLine($"Challenging {npc.Name2} to a Duel!");
        terminal.WriteLine("");

        // Check if they'll accept
        bool accepts = npc.Darkness > 300 || new Random().Next(100) < 50;

        if (!accepts)
        {
            terminal.SetColor("gray");
            terminal.WriteLine($"{npc.Name2} laughs and waves you off. \"I have better things to do.\"");
            await terminal.PressAnyKey();
            return;
        }

        terminal.SetColor("yellow");
        terminal.WriteLine($"{npc.Name2} accepts your challenge!");
        terminal.WriteLine("\"You'll regret this decision!\"");
        terminal.WriteLine("");

        var confirm = await terminal.GetInput("Fight now? (y/N): ");
        if (confirm.ToUpper() != "Y")
        {
            terminal.WriteLine($"{npc.Name2}: \"Changed your mind? Coward!\"", "gray");
            await Task.Delay(2000);
            return;
        }

        // Create monster from NPC for combat
        var npcMonster = Monster.CreateMonster(
            nr: 100,
            name: npc.Name2,
            hps: npc.HP,
            strength: npc.Strength,
            defence: npc.Defence,
            phrase: $"{npc.Name2} readies for battle!",
            grabweap: false,
            grabarm: false,
            weapon: "Weapon",
            armor: "Armor",
            poisoned: false,
            disease: false,
            punch: npc.Strength / 2,
            armpow: npc.ArmPow,
            weappow: npc.WeapPow
        );

        var combatEngine = new CombatEngine(terminal);
        var result = await combatEngine.PlayerVsMonster(currentPlayer, npcMonster);

        if (result.Outcome == CombatOutcome.Victory)
        {
            terminal.SetColor("bright_green");
            terminal.WriteLine("");
            terminal.WriteLine($"You have defeated {npc.Name2}!");
            terminal.WriteLine("Word of your victory spreads through the inn!");

            currentPlayer.Experience += npc.Level * 100;
            currentPlayer.PKills++;

            // Update relationship negatively
            RelationshipSystem.UpdateRelationship(currentPlayer, npc, -1, 5, false, false);

            // Generate news
            NewsSystem.Instance?.Newsy(true, $"{currentPlayer.Name} defeated {npc.Name2} in a tavern brawl!");
        }
        else if (result.Outcome == CombatOutcome.PlayerDied)
        {
            terminal.SetColor("red");
            terminal.WriteLine("");
            terminal.WriteLine($"{npc.Name2} knocks you unconscious!");
            currentPlayer.HP = 1; // Inn fights don't kill
            currentPlayer.PDefeats++;
        }

        await Task.Delay(3000);
    }

    /// <summary>
    /// Give a gift to an NPC
    /// </summary>
    private async Task GiveGiftToNPC(NPC npc)
    {
        if (currentPlayer.Gold < 50)
        {
            terminal.WriteLine("You don't have enough gold for a gift (50 gold needed).", "red");
            await Task.Delay(2000);
            return;
        }

        terminal.ClearScreen();
        terminal.SetColor("bright_yellow");
        terminal.WriteLine($"Giving a Gift to {npc.Name2}");
        terminal.WriteLine("");

        currentPlayer.Gold -= 50;

        var random = new Random();
        var responses = new[] {
            $"{npc.Name2}'s eyes light up. \"For me? How thoughtful!\"",
            $"{npc.Name2} accepts the gift graciously. \"You're too kind.\"",
            $"{npc.Name2} smiles broadly. \"I won't forget this kindness.\"",
        };

        terminal.SetColor("white");
        terminal.WriteLine(responses[random.Next(responses.Length)]);
        terminal.WriteLine("");

        // Big relationship boost
        RelationshipSystem.UpdateRelationship(currentPlayer, npc, 1, 5, false, false);
        terminal.SetColor("green");
        terminal.WriteLine("(Your relationship improves significantly!)");
        terminal.WriteLine("(-50 gold)");

        await terminal.PressAnyKey();
    }

    /// <summary>
    /// Propose marriage to an NPC
    /// </summary>
    private async Task ProposeToNPC(NPC npc)
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_magenta");
        terminal.WriteLine("═══════════════════════════════════════");
        terminal.WriteLine("          MARRIAGE PROPOSAL");
        terminal.WriteLine("═══════════════════════════════════════");
        terminal.WriteLine("");

        terminal.SetColor("white");
        terminal.WriteLine($"You kneel before {npc.Name2} and take their hand...");
        terminal.WriteLine("");
        terminal.WriteLine($"\"Will you marry me, {npc.Name2}?\"");
        terminal.WriteLine("");

        await Task.Delay(2000);

        // Try to perform marriage
        if (RelationshipSystem.PerformMarriage(currentPlayer, npc, out string message))
        {
            terminal.SetColor("bright_green");
            terminal.WriteLine($"{npc.Name2} beams with joy! \"Yes! A thousand times yes!\"");
            terminal.WriteLine("");
            terminal.WriteLine(message);

            // Generate news
            NewsSystem.Instance?.Newsy(true, $"{currentPlayer.Name} and {npc.Name2} got married at the Inn!");

            terminal.WriteLine("");
            terminal.SetColor("yellow");
            terminal.WriteLine("Congratulations on your marriage!");
        }
        else
        {
            terminal.SetColor("red");
            terminal.WriteLine(message);
            terminal.WriteLine("");
            terminal.WriteLine($"{npc.Name2} looks apologetic but declines.");
        }

        await terminal.PressAnyKey();
    }

    /// <summary>
    /// Get NPC mood description
    /// </summary>
    private string GetNPCMood(NPC npc)
    {
        if (npc.Darkness > npc.Chivalry + 200) return "They look aggressive and dangerous.";
        if (npc.Chivalry > npc.Darkness + 200) return "They seem friendly and approachable.";
        if (npc.HP < npc.MaxHP / 2) return "They look tired and worn from battle.";
        return "They seem relaxed and at ease.";
    }

    /// <summary>
    /// Get relationship status text
    /// </summary>
    private string GetRelationshipText(int relationship)
    {
        // Lower numbers are better relationships in Pascal system
        if (relationship <= GameConfig.RelationMarried) return "Married";
        if (relationship <= GameConfig.RelationLove) return "In Love";
        if (relationship <= GameConfig.RelationFriendship) return "Close Friend";
        if (relationship <= GameConfig.RelationNormal) return "Neutral";
        if (relationship <= GameConfig.RelationEnemy) return "Disliked";
        return "Hated Enemy";
    }

    /// <summary>
    /// Get relationship color
    /// </summary>
    private string GetRelationshipColor(int relationship)
    {
        // Lower numbers are better relationships in Pascal system
        if (relationship <= GameConfig.RelationLove) return "bright_magenta";
        if (relationship <= GameConfig.RelationFriendship) return "green";
        if (relationship <= GameConfig.RelationNormal) return "gray";
        if (relationship <= GameConfig.RelationEnemy) return "bright_red";
        return "red";
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
