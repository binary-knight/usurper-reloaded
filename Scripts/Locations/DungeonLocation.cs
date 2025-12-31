using UsurperRemake.Utils;
using UsurperRemake.Systems;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Dungeon Location - Pascal-compatible dungeon system
/// Based on DUNGEONC.PAS, DUNGEVC.PAS, DUNGEV2.PAS, and DMAZE.PAS
/// </summary>
public class DungeonLocation : BaseLocation
{
    private List<Character> teammates = new();
    private int currentDungeonLevel = 1;
    private int maxDungeonLevel = 100;
    private DungeonTerrain currentTerrain = DungeonTerrain.Underground;
    private Random dungeonRandom = new Random();
    
    // Encounter chances: 90% monsters, 10% special events (no empty rooms - always something happens!)
    private const float MonsterEncounterChance = 0.90f;
    private const float SpecialEventChance = 0.10f;
    
    public DungeonLocation() : base(
        GameLocation.Dungeons,
        "The Dungeons",
        "You stand before the entrance to the ancient dungeons. Dark passages lead deep into the earth."
    ) { }
    
    protected override void SetupLocation()
    {
        base.SetupLocation();
        
        // Initialize dungeon level exactly at the player's level (faithful to original — the
        // player could then choose to go deeper up to +10 levels).
        var playerLevel = GetCurrentPlayer()?.Level ?? 1;
        currentDungeonLevel = Math.Max(1, playerLevel);
        
        if (currentDungeonLevel > maxDungeonLevel)
            currentDungeonLevel = maxDungeonLevel;
    }
    
    protected override void DisplayLocation()
    {
        terminal.ClearScreen();

        // Breadcrumb navigation
        ShowBreadcrumb();

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

    protected override string GetBreadcrumbPath()
    {
        return $"Main Street → Dungeons → Level {currentDungeonLevel}";
    }

    private void ShowDungeonMenu()
    {
        // Row 1 - Exploration actions
        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("bright_yellow");
        terminal.Write("E");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.Write("xplore Level     ");

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("bright_magenta");
        terminal.Write("D");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.Write("escend Deeper    ");

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("bright_green");
        terminal.Write("A");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.WriteLine("scend to Surface");

        // Row 2 - Difficulty increase
        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("bright_red");
        terminal.Write("I");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.WriteLine("ncrease Difficulty (+10)");

        // Row 3 - Team and status
        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("bright_cyan");
        terminal.Write("T");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.Write("eam Management   ");

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("cyan");
        terminal.Write("S");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.Write("tatus            ");

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("green");
        terminal.Write("P");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.WriteLine("otions (Monk)");

        // Row 4 - Map and exit
        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("bright_cyan");
        terminal.Write("M");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.Write("ap of Area       ");

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("bright_red");
        terminal.Write("Q");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("red");
        terminal.Write("uit to Town      ");

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("yellow");
        terminal.Write("?");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.WriteLine(" Help");
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

        // Show quick command bar
        ShowQuickCommandBar();
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

            case "P":
                await VisitMonk();
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
                
            case "I":
                await IncreaseDifficulty();
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

        // No turn/fight limits in the new persistent system - explore freely!

        terminal.ClearScreen();
        terminal.SetColor("yellow");
        terminal.WriteLine("═══ EXPLORING ═══");
        terminal.WriteLine("");

        // Atmospheric exploration text
        await ShowExplorationText();

        // Determine encounter type: 90% monsters, 10% special events
        var encounterRoll = dungeonRandom.NextDouble();

        if (encounterRoll < MonsterEncounterChance)
        {
            await MonsterEncounter();
        }
        else
        {
            await SpecialEventEncounter();
        }

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

        // Use new MonsterGenerator to create level-appropriate monsters
        var monsters = MonsterGenerator.GenerateMonsterGroup(currentDungeonLevel, dungeonRandom);

        var combatEngine = new CombatEngine(terminal);

        // Display encounter message with color
        if (monsters.Count == 1)
        {
            var monster = monsters[0];
            if (monster.IsBoss)
            {
                terminal.SetColor("bright_red");
                terminal.WriteLine($"⚠ A powerful [{monster.MonsterColor}]{monster.Name}[/] blocks your path! ⚠");
            }
            else
            {
                terminal.SetColor(monster.MonsterColor);
                terminal.WriteLine($"A [{monster.MonsterColor}]{monster.Name}[/] appears!");
            }
        }
        else
        {
            terminal.SetColor("yellow");
            terminal.Write($"You encounter a group of [{monsters[0].MonsterColor}]{monsters.Count} {monsters[0].Name}");
            if (monsters[0].FamilyName != "")
            {
                terminal.Write($"[/] from the {monsters[0].FamilyName} family!");
            }
            else
            {
                terminal.Write("[/]!");
            }
            terminal.WriteLine("");
        }

        terminal.WriteLine("");
        await Task.Delay(2000);

        // Use new PlayerVsMonsters method - ALL monsters fight at once!
        // Monk will appear after ALL monsters are defeated
        await combatEngine.PlayerVsMonsters(GetCurrentPlayer(), monsters, teammates, offerMonkEncounter: true);
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
    /// Special event encounters - Based on Pascal DUNGEVC.PAS and DUNGEV2.PAS
    /// Includes positive, negative, and neutral events for variety
    /// </summary>
    private async Task SpecialEventEncounter()
    {
        // 12 different event types based on original Pascal
        var eventType = dungeonRandom.Next(12);

        switch (eventType)
        {
            case 0:
                await TreasureChestEncounter();
                break;
            case 1:
                await PotionCacheEncounter();
                break;
            case 2:
                await MerchantEncounter();
                break;
            case 3:
                await WitchDoctorEncounter();
                break;
            case 4:
                await BeggarEncounter();
                break;
            case 5:
                await StrangersEncounter();
                break;
            case 6:
                await HarassedWomanEncounter();
                break;
            case 7:
                await WoundedManEncounter();
                break;
            case 8:
                await MysteriousShrine();
                break;
            case 9:
                await TrapEncounter();
                break;
            case 10:
                await AncientScrollEncounter();
                break;
            case 11:
                await GamblingGhostEncounter();
                break;
        }
    }

    /// <summary>
    /// Treasure chest encounter - Classic Pascal treasure mechanics
    /// </summary>
    private async Task TreasureChestEncounter()
    {
        terminal.SetColor("yellow");
        terminal.WriteLine("★ TREASURE CHEST ★");
        terminal.WriteLine("");

        terminal.WriteLine("You discover an ancient chest hidden in the shadows!", "cyan");
        terminal.WriteLine("");

        var choice = await terminal.GetInput("(O)pen the chest or (L)eave it alone? ");

        var currentPlayer = GetCurrentPlayer();

        if (choice.ToUpper() == "O")
        {
            // 70% good, 20% trap, 10% mimic
            var chestRoll = dungeonRandom.Next(10);

            if (chestRoll < 7)
            {
                // Good treasure!
                long goldFound = currentDungeonLevel * 1500 + dungeonRandom.Next(5000);
                long expGained = currentDungeonLevel * 200 + dungeonRandom.Next(3000);

                terminal.SetColor("green");
                terminal.WriteLine("The chest opens to reveal glittering treasure!");
                terminal.WriteLine($"You find {goldFound} gold pieces!");
                terminal.WriteLine($"You gain {expGained} experience!");

                currentPlayer.Gold += goldFound;
                currentPlayer.Experience += expGained;
            }
            else if (chestRoll < 9)
            {
                // Trap!
                terminal.SetColor("red");
                terminal.WriteLine("CLICK! It's a trap!");

                var trapType = dungeonRandom.Next(3);
                switch (trapType)
                {
                    case 0:
                        var poisonDmg = currentDungeonLevel * 5;
                        currentPlayer.HP -= poisonDmg;
                        terminal.WriteLine($"Poison gas! You take {poisonDmg} damage!");
                        currentPlayer.Poison = Math.Max(currentPlayer.Poison, 1);
                        terminal.WriteLine("You have been poisoned!", "magenta");
                        break;
                    case 1:
                        var spikeDmg = currentDungeonLevel * 8;
                        currentPlayer.HP -= spikeDmg;
                        terminal.WriteLine($"Spikes shoot out! You take {spikeDmg} damage!");
                        break;
                    case 2:
                        var goldLost = currentPlayer.Gold / 10;
                        currentPlayer.Gold -= goldLost;
                        terminal.WriteLine($"Acid sprays your coin pouch! You lose {goldLost} gold!");
                        break;
                }
            }
            else
            {
                // Mimic! (triggers combat)
                terminal.SetColor("bright_red");
                terminal.WriteLine("The chest MOVES! It's a MIMIC!");
                await Task.Delay(1500);

                var mimic = Monster.CreateMonster(
                    currentDungeonLevel, "Mimic",
                    currentDungeonLevel * 15, currentDungeonLevel * 4, 0,
                    "Fooled you!", false, false, "Teeth", "Wooden Shell",
                    false, false, currentDungeonLevel * 5, currentDungeonLevel * 3, currentDungeonLevel * 3
                );
                mimic.IsBoss = true;
                mimic.Level = currentDungeonLevel;

                var combatEngine = new CombatEngine(terminal);
                await combatEngine.PlayerVsMonster(currentPlayer, mimic, teammates);
            }
        }
        else
        {
            terminal.WriteLine("You wisely leave the chest alone and continue on.", "gray");
        }

        await Task.Delay(2000);
    }

    /// <summary>
    /// Strangers encounter - Band of rogues/orcs (from Pascal DUNGEV2.PAS)
    /// </summary>
    private async Task StrangersEncounter()
    {
        terminal.SetColor("red");
        terminal.WriteLine("⚔ STRANGERS APPROACH ⚔");
        terminal.WriteLine("");

        var currentPlayer = GetCurrentPlayer();
        var groupType = dungeonRandom.Next(4);
        string groupName;
        string[] memberTypes;

        switch (groupType)
        {
            case 0:
                groupName = "orcs";
                memberTypes = new[] { "Orc", "Half-Orc", "Orc Raider" };
                terminal.WriteLine("A group of orcs emerges from the shadows!", "gray");
                terminal.WriteLine("They are poorly armed with sticks and clubs.", "gray");
                break;
            case 1:
                groupName = "trolls";
                memberTypes = new[] { "Troll", "Half-Troll", "Lumber-Troll" };
                terminal.WriteLine("A band of trolls blocks your path!", "green");
                terminal.WriteLine("They carry clubs and spears.", "gray");
                break;
            case 2:
                groupName = "rogues";
                memberTypes = new[] { "Rogue", "Thief", "Pirate" };
                terminal.WriteLine("A gang of rogues surrounds you!", "cyan");
                terminal.WriteLine("They brandish knives and rapiers.", "gray");
                break;
            default:
                groupName = "dwarves";
                memberTypes = new[] { "Dwarf", "Dwarf Warrior", "Dwarf Scout" };
                terminal.WriteLine("A group of armed dwarves approaches!", "yellow");
                terminal.WriteLine("They carry swords and axes.", "gray");
                break;
        }

        terminal.WriteLine("");
        terminal.WriteLine("Their leader demands your gold!", "white");
        terminal.WriteLine("");

        var choice = await terminal.GetInput("(F)ight them, (P)ay them off, or try to (E)scape? ");

        if (choice.ToUpper() == "F")
        {
            terminal.WriteLine("You draw your weapon and prepare for battle!", "yellow");
            await Task.Delay(1500);

            // Create the group
            int groupSize = dungeonRandom.Next(3, 6);
            var monsters = new List<Monster>();

            for (int i = 0; i < groupSize; i++)
            {
                var name = memberTypes[dungeonRandom.Next(memberTypes.Length)];
                if (i == 0) name = name + " Leader";

                var monster = Monster.CreateMonster(
                    currentDungeonLevel, name,
                    currentDungeonLevel * (i == 0 ? 8 : 4),
                    currentDungeonLevel * 2, 0,
                    "Attack!", false, false, "Weapon", "Armor",
                    false, false, currentDungeonLevel * 2, currentDungeonLevel, currentDungeonLevel * 2
                );
                monster.Level = currentDungeonLevel;
                if (i == 0) monster.IsBoss = true;
                monsters.Add(monster);
            }

            var combatEngine = new CombatEngine(terminal);
            await combatEngine.PlayerVsMonsters(currentPlayer, monsters, teammates, offerMonkEncounter: false);
        }
        else if (choice.ToUpper() == "P")
        {
            long bribe = currentDungeonLevel * 500 + dungeonRandom.Next(1000);
            if (currentPlayer.Gold >= bribe)
            {
                currentPlayer.Gold -= bribe;
                terminal.WriteLine($"You reluctantly hand over {bribe} gold.", "yellow");
                terminal.WriteLine($"The {groupName} leave you in peace.", "gray");
            }
            else
            {
                terminal.WriteLine("You don't have enough gold!", "red");
                terminal.WriteLine("They attack anyway!", "red");
                await Task.Delay(1500);
                // Trigger simplified combat
                var monster = Monster.CreateMonster(
                    currentDungeonLevel, $"{groupName.Substring(0, 1).ToUpper()}{groupName.Substring(1)} Leader",
                    currentDungeonLevel * 10, currentDungeonLevel * 3, 0,
                    "No gold means death!", false, false, "Weapon", "Armor",
                    false, false, currentDungeonLevel * 3, currentDungeonLevel * 2, currentDungeonLevel * 2
                );
                var combatEngine = new CombatEngine(terminal);
                await combatEngine.PlayerVsMonster(currentPlayer, monster, teammates);
            }
        }
        else
        {
            // Escape attempt - 60% chance
            if (dungeonRandom.NextDouble() < 0.6)
            {
                terminal.WriteLine("You manage to slip away into the shadows!", "green");
            }
            else
            {
                terminal.WriteLine("They catch you trying to escape!", "red");
                long stolen = currentPlayer.Gold / 5;
                currentPlayer.Gold -= stolen;
                terminal.WriteLine($"They beat you and steal {stolen} gold!", "red");
            }
        }

        await Task.Delay(2000);
    }

    /// <summary>
    /// Harassed woman encounter - Moral choice event (from Pascal DUNGEVC.PAS)
    /// </summary>
    private async Task HarassedWomanEncounter()
    {
        terminal.SetColor("magenta");
        terminal.WriteLine("♀ DAMSEL IN DISTRESS ♀");
        terminal.WriteLine("");

        terminal.WriteLine("You hear screams echoing through the corridor!", "white");
        terminal.WriteLine("A woman is being harassed by a band of ruffians.", "gray");
        terminal.WriteLine("");

        var choice = await terminal.GetInput("(H)elp her, (I)gnore the situation, or (J)oin the ruffians? ");

        var currentPlayer = GetCurrentPlayer();

        if (choice.ToUpper() == "H")
        {
            terminal.WriteLine("You rush to her defense!", "green");
            terminal.WriteLine("\"Unhand her, villains!\"", "yellow");
            await Task.Delay(1500);

            // Fight ruffians
            var monsters = new List<Monster>();
            int count = dungeonRandom.Next(2, 4);
            for (int i = 0; i < count; i++)
            {
                var name = i == 0 ? "Ruffian Leader" : "Ruffian";
                var monster = Monster.CreateMonster(
                    currentDungeonLevel, name,
                    currentDungeonLevel * (i == 0 ? 6 : 3), currentDungeonLevel * 2, 0,
                    "Mind your own business!", false, false, "Knife", "Rags",
                    false, false, currentDungeonLevel * 2, currentDungeonLevel, currentDungeonLevel
                );
                monster.Level = currentDungeonLevel;
                monsters.Add(monster);
            }

            var combatEngine = new CombatEngine(terminal);
            await combatEngine.PlayerVsMonsters(currentPlayer, monsters, teammates, offerMonkEncounter: false);

            if (currentPlayer.HP > 0)
            {
                terminal.WriteLine("");
                terminal.WriteLine("The woman thanks you profusely!", "cyan");
                long reward = currentDungeonLevel * 300 + dungeonRandom.Next(500);
                long chivGain = dungeonRandom.Next(50) + 30;

                terminal.WriteLine($"She rewards you with {reward} gold!", "yellow");
                terminal.WriteLine($"Your chivalry increases by {chivGain}!", "white");

                currentPlayer.Gold += reward;
                currentPlayer.Chivalry += chivGain;
            }
        }
        else if (choice.ToUpper() == "J")
        {
            terminal.SetColor("red");
            terminal.WriteLine("You join the ruffians in their villainy!");
            terminal.WriteLine("This is a shameful act!");

            long stolen = dungeonRandom.Next(200) + 50;
            long darkGain = dungeonRandom.Next(75) + 50;

            currentPlayer.Gold += stolen;
            currentPlayer.Darkness += darkGain;

            terminal.WriteLine($"You steal {stolen} gold from the woman.", "yellow");
            terminal.WriteLine($"Your darkness increases by {darkGain}!", "magenta");
        }
        else
        {
            terminal.WriteLine("You turn away and pretend not to notice.", "gray");
            terminal.WriteLine("Her cries fade as you continue your journey...", "gray");
        }

        await Task.Delay(2500);
    }

    /// <summary>
    /// Wounded man encounter - Healing quest (from Pascal DUNGEVC.PAS)
    /// </summary>
    private async Task WoundedManEncounter()
    {
        terminal.SetColor("cyan");
        terminal.WriteLine("✚ WOUNDED STRANGER ✚");
        terminal.WriteLine("");

        terminal.WriteLine("You find a wounded man lying against the wall.", "white");
        terminal.WriteLine("He is bleeding heavily and begs for help.", "gray");
        terminal.WriteLine("\"Please... I need healing... I can pay...\"", "yellow");
        terminal.WriteLine("");

        var choice = await terminal.GetInput("(H)elp him, (R)ob him, or (L)eave him? ");

        var currentPlayer = GetCurrentPlayer();

        if (choice.ToUpper() == "H")
        {
            if (currentPlayer.Healing > 0)
            {
                currentPlayer.Healing--;
                terminal.WriteLine("You use a healing potion on the wounded stranger.", "green");
                terminal.WriteLine("He recovers enough to stand.", "white");

                long reward = currentDungeonLevel * 500 + dungeonRandom.Next(1000);
                long chivGain = dungeonRandom.Next(40) + 20;

                terminal.WriteLine($"\"Thank you, hero! Take this reward: {reward} gold!\"", "yellow");
                terminal.WriteLine($"Your chivalry increases by {chivGain}!", "white");

                currentPlayer.Gold += reward;
                currentPlayer.Chivalry += chivGain;
            }
            else
            {
                terminal.WriteLine("You have no healing potions to spare!", "red");
                terminal.WriteLine("You try to bandage his wounds with cloth...", "gray");

                if (dungeonRandom.NextDouble() < 0.5)
                {
                    terminal.WriteLine("It seems to help a little.", "green");
                    currentPlayer.Chivalry += 10;
                }
                else
                {
                    terminal.WriteLine("Unfortunately, he dies from his wounds.", "red");
                }
            }
        }
        else if (choice.ToUpper() == "R")
        {
            terminal.SetColor("red");
            terminal.WriteLine("You search the dying man's belongings...");

            long stolen = dungeonRandom.Next(500) + 100;
            long darkGain = dungeonRandom.Next(80) + 60;

            terminal.WriteLine($"You find {stolen} gold in his purse.", "yellow");
            terminal.WriteLine($"Your darkness increases by {darkGain}!", "magenta");

            currentPlayer.Gold += stolen;
            currentPlayer.Darkness += darkGain;

            terminal.WriteLine("He dies cursing your name...", "gray");
        }
        else
        {
            terminal.WriteLine("You step over the dying man and continue on.", "gray");
            terminal.WriteLine("His moans fade behind you...", "gray");
        }

        await Task.Delay(2500);
    }

    /// <summary>
    /// Mysterious shrine - Random buff or debuff
    /// </summary>
    private async Task MysteriousShrine()
    {
        terminal.SetColor("bright_cyan");
        terminal.WriteLine("✦ MYSTERIOUS SHRINE ✦");
        terminal.WriteLine("");

        terminal.WriteLine("You discover an ancient shrine glowing with strange light.", "white");
        terminal.WriteLine("Offerings of gold and bones surround a stone altar.", "gray");
        terminal.WriteLine("");

        var choice = await terminal.GetInput("(P)ray at the shrine, (D)esecrate it, or (L)eave? ");

        var currentPlayer = GetCurrentPlayer();

        if (choice.ToUpper() == "P")
        {
            terminal.WriteLine("You kneel before the ancient shrine...", "cyan");
            await Task.Delay(1500);

            // Random blessing or curse
            var outcome = dungeonRandom.Next(6);
            switch (outcome)
            {
                case 0:
                    terminal.WriteLine("Divine light fills you!", "bright_yellow");
                    currentPlayer.HP = currentPlayer.MaxHP;
                    terminal.WriteLine("You are fully healed!", "green");
                    break;
                case 1:
                    var strBonus = dungeonRandom.Next(5) + 1;
                    currentPlayer.Strength += strBonus;
                    terminal.WriteLine($"You feel stronger! +{strBonus} Strength!", "green");
                    break;
                case 2:
                    var expBonus = currentDungeonLevel * 500;
                    currentPlayer.Experience += expBonus;
                    terminal.WriteLine($"Ancient wisdom flows into you! +{expBonus} EXP!", "yellow");
                    break;
                case 3:
                    terminal.WriteLine("The shrine is silent...", "gray");
                    terminal.WriteLine("Nothing happens.", "gray");
                    break;
                case 4:
                    var hpLoss = currentPlayer.HP / 4;
                    currentPlayer.HP -= hpLoss;
                    terminal.WriteLine("The shrine drains your life force!", "red");
                    terminal.WriteLine($"You lose {hpLoss} HP!", "red");
                    break;
                case 5:
                    var goldLoss = currentPlayer.Gold / 5;
                    currentPlayer.Gold -= goldLoss;
                    terminal.WriteLine("Your gold dissolves into the altar!", "red");
                    terminal.WriteLine($"You lose {goldLoss} gold!", "red");
                    break;
            }
        }
        else if (choice.ToUpper() == "D")
        {
            terminal.SetColor("red");
            terminal.WriteLine("You smash the shrine and steal the offerings!");

            long stolen = currentDungeonLevel * 200 + dungeonRandom.Next(500);
            long darkGain = dungeonRandom.Next(50) + 30;

            terminal.WriteLine($"You find {stolen} gold among the offerings!", "yellow");
            terminal.WriteLine($"Your darkness increases by {darkGain}!", "magenta");

            currentPlayer.Gold += stolen;
            currentPlayer.Darkness += darkGain;

            // Chance of angering spirits
            if (dungeonRandom.NextDouble() < 0.3)
            {
                terminal.WriteLine("");
                terminal.WriteLine("An angry spirit emerges from the ruined shrine!", "bright_red");
                await Task.Delay(1500);

                var spirit = Monster.CreateMonster(
                    currentDungeonLevel + 5, "Vengeful Spirit",
                    currentDungeonLevel * 12, currentDungeonLevel * 4, 0,
                    "You will pay for your sacrilege!", false, false, "Spectral Claws", "Ethereal Form",
                    false, false, currentDungeonLevel * 4, currentDungeonLevel * 3, currentDungeonLevel * 3
                );
                spirit.IsBoss = true;

                var combatEngine = new CombatEngine(terminal);
                await combatEngine.PlayerVsMonster(currentPlayer, spirit, teammates);
            }
        }
        else
        {
            terminal.WriteLine("You wisely leave the mysterious shrine alone.", "gray");
        }

        await Task.Delay(2000);
    }

    /// <summary>
    /// Trap encounter - Various dungeon hazards
    /// </summary>
    private async Task TrapEncounter()
    {
        terminal.SetColor("red");
        terminal.WriteLine("⚠ TRAP! ⚠");
        terminal.WriteLine("");

        var currentPlayer = GetCurrentPlayer();
        var trapType = dungeonRandom.Next(5);

        switch (trapType)
        {
            case 0:
                terminal.WriteLine("The floor gives way beneath you!", "white");
                var fallDmg = currentDungeonLevel * 3 + dungeonRandom.Next(10);
                currentPlayer.HP -= fallDmg;
                terminal.WriteLine($"You fall into a pit and take {fallDmg} damage!", "red");
                break;
            case 1:
                terminal.WriteLine("Poison darts shoot from the walls!", "white");
                var dartDmg = currentDungeonLevel * 2 + dungeonRandom.Next(8);
                currentPlayer.HP -= dartDmg;
                currentPlayer.Poison = Math.Max(currentPlayer.Poison, 1);
                terminal.WriteLine($"You take {dartDmg} damage and are poisoned!", "magenta");
                break;
            case 2:
                terminal.WriteLine("A magical rune explodes beneath your feet!", "bright_magenta");
                var runeDmg = currentDungeonLevel * 5 + dungeonRandom.Next(15);
                currentPlayer.HP -= runeDmg;
                terminal.WriteLine($"You take {runeDmg} magical damage!", "red");
                break;
            case 3:
                terminal.WriteLine("A net falls from above, trapping you!", "white");
                terminal.WriteLine("You struggle free, but lose time...", "gray");
                // Could implement time/turn penalty here
                break;
            case 4:
                terminal.WriteLine("You trigger a tripwire!", "white");
                terminal.WriteLine("But nothing happens... the trap is broken.", "green");
                terminal.WriteLine("You find some gold hidden near the mechanism.", "yellow");
                currentPlayer.Gold += currentDungeonLevel * 50;
                break;
        }

        await Task.Delay(2500);
    }

    /// <summary>
    /// Ancient scroll encounter - Magic scroll discovery
    /// </summary>
    private async Task AncientScrollEncounter()
    {
        terminal.SetColor("bright_cyan");
        terminal.WriteLine("📜 ANCIENT SCROLL 📜");
        terminal.WriteLine("");

        terminal.WriteLine("You discover an ancient scroll tucked into a wall crevice.", "white");
        terminal.WriteLine("Strange symbols glow faintly on the parchment.", "gray");

        await HandleMagicScroll();
    }

    /// <summary>
    /// Gambling ghost encounter - Risk/reward minigame
    /// </summary>
    private async Task GamblingGhostEncounter()
    {
        terminal.SetColor("bright_white");
        terminal.WriteLine("👻 GAMBLING GHOST 👻");
        terminal.WriteLine("");

        terminal.WriteLine("A spectral figure materializes before you!", "cyan");
        terminal.WriteLine("\"Greetings, mortal! Care for a game of chance?\"", "yellow");
        terminal.WriteLine("The ghost produces a pair of ethereal dice.", "gray");
        terminal.WriteLine("");

        var currentPlayer = GetCurrentPlayer();
        long minBet = 100;
        long maxBet = currentPlayer.Gold / 2;

        if (currentPlayer.Gold < minBet)
        {
            terminal.WriteLine("\"Bah! You have no gold worth gambling for!\"", "yellow");
            terminal.WriteLine("The ghost fades away in disappointment.", "gray");
            await Task.Delay(2000);
            return;
        }

        terminal.WriteLine($"\"Place your bet! (Minimum {minBet}, Maximum {maxBet})\"", "yellow");
        var betStr = await terminal.GetInput("Your bet (or 0 to decline): ");

        if (!long.TryParse(betStr, out long bet) || bet < minBet || bet > maxBet)
        {
            terminal.WriteLine("\"Coward! Perhaps next time...\"", "yellow");
            terminal.WriteLine("The ghost fades away.", "gray");
            await Task.Delay(2000);
            return;
        }

        terminal.WriteLine($"You bet {bet} gold!", "white");
        terminal.WriteLine("The ghost rolls the dice...", "gray");
        await Task.Delay(1500);

        var ghostRoll = dungeonRandom.Next(1, 7) + dungeonRandom.Next(1, 7);
        terminal.WriteLine($"Ghost rolls: {ghostRoll}", "cyan");

        terminal.WriteLine("Your turn to roll...", "gray");
        await Task.Delay(1000);

        var playerRoll = dungeonRandom.Next(1, 7) + dungeonRandom.Next(1, 7);
        terminal.WriteLine($"You roll: {playerRoll}", "yellow");

        if (playerRoll > ghostRoll)
        {
            terminal.SetColor("green");
            terminal.WriteLine("YOU WIN!");
            terminal.WriteLine($"The ghost begrudgingly pays you {bet} gold!", "yellow");
            currentPlayer.Gold += bet;
        }
        else if (playerRoll < ghostRoll)
        {
            terminal.SetColor("red");
            terminal.WriteLine("YOU LOSE!");
            terminal.WriteLine($"The ghost cackles as your gold vanishes!", "yellow");
            currentPlayer.Gold -= bet;
        }
        else
        {
            terminal.SetColor("yellow");
            terminal.WriteLine("TIE!");
            terminal.WriteLine("\"Interesting... keep your gold, mortal. Until next time!\"", "yellow");
        }

        terminal.WriteLine("The ghost fades into the shadows...", "gray");
        await Task.Delay(2500);
    }

    /// <summary>
    /// Potion cache encounter - find random potions
    /// </summary>
    private async Task PotionCacheEncounter()
    {
        terminal.SetColor("bright_green");
        terminal.WriteLine("✚ POTION CACHE ✚");
        terminal.WriteLine("");

        var currentPlayer = GetCurrentPlayer();

        // Random potion messages
        string[] messages = new[]
        {
            "You discover an abandoned healer's satchel!",
            "A fallen adventurer's pack contains healing supplies!",
            "You find a monk's abandoned cache of potions!",
            "A hidden alcove reveals a stash of healing elixirs!",
            "The corpse of a cleric clutches a bag of potions!"
        };

        terminal.WriteLine(messages[dungeonRandom.Next(messages.Length)], "cyan");
        terminal.WriteLine("");

        // Give 1-5 potions, but don't exceed max
        int potionsFound = dungeonRandom.Next(1, 6);
        int currentPotions = (int)currentPlayer.Healing;
        int maxPotions = currentPlayer.MaxPotions;
        int roomAvailable = maxPotions - currentPotions;

        if (roomAvailable <= 0)
        {
            terminal.WriteLine("You already have the maximum number of potions!", "yellow");
            terminal.WriteLine("You leave the potions for another adventurer.", "gray");
        }
        else
        {
            int actualGained = Math.Min(potionsFound, roomAvailable);
            currentPlayer.Healing += actualGained;

            terminal.SetColor("green");
            terminal.WriteLine($"You collect {actualGained} healing potion{(actualGained > 1 ? "s" : "")}!");
            terminal.WriteLine($"Potions: {currentPlayer.Healing}/{currentPlayer.MaxPotions}", "cyan");

            if (actualGained < potionsFound)
            {
                terminal.WriteLine($"(You had to leave {potionsFound - actualGained} potion{(potionsFound - actualGained > 1 ? "s" : "")} behind - at maximum capacity)", "gray");
            }
        }

        await Task.Delay(2500);
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
        
        // Smooth scaling factors – tuned for balanced difficulty curve
        float scaleFactor = 1f + (currentDungeonLevel / 20f); // every 20 levels → +100 %

        // Regular monsters are weaker, bosses are tougher (like the original game)
        float monsterMultiplier = isLeader ? 1.8f : 0.6f; // Regular monsters are 60% strength, bosses are 180%

        long hp = (long)(currentDungeonLevel * 4 * scaleFactor * monsterMultiplier); // survivability

        int strength = (int)(currentDungeonLevel * 1.5f * scaleFactor * monsterMultiplier); // base damage
        int punch    = (int)(currentDungeonLevel * 1.2f * scaleFactor * monsterMultiplier); // natural attacks
        int weapPow  = (int)(currentDungeonLevel * 0.9f * scaleFactor * monsterMultiplier); // weapon bonus
        int armPow   = (int)(currentDungeonLevel * 0.9f * scaleFactor * monsterMultiplier); // defense bonus

        var monster = Monster.CreateMonster(
            nr: currentDungeonLevel,
            name: name,
            hps: hp,
            strength: strength,
            defence: 0,
            phrase: GetMonsterPhrase(currentTerrain),
            grabweap: dungeonRandom.NextDouble() < 0.3,
            grabarm: false,
            weapon: weapon,
            armor: armor,
            poisoned: false,
            disease: false,
            punch: punch,
            armpow: armPow,
            weappow: weapPow
        );
        
        if (isLeader)
        {
            monster.IsBoss = true;
        }
        
        // Store level for other systems (initiative scaling etc.)
        monster.Level = currentDungeonLevel;
        
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
        var playerLevel = GetCurrentPlayer()?.Level ?? 1;
        int deepestAllowed = Math.Min(maxDungeonLevel, playerLevel + 10);

        if (currentDungeonLevel < deepestAllowed)
        {
            currentDungeonLevel++;
            terminal.WriteLine($"You descend to dungeon level {currentDungeonLevel}.", "yellow");
        }
        else
        {
            terminal.WriteLine("A mysterious force bars your way – it seems too dangerous to venture deeper right now.", "red");
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
        var player = GetCurrentPlayer();

        terminal.ClearScreen();
        terminal.SetColor("bright_cyan");
        terminal.WriteLine("╔═══════════════════════════════════════════════════╗");
        terminal.WriteLine("║               TEAM MANAGEMENT                     ║");
        terminal.WriteLine("╚═══════════════════════════════════════════════════╝");
        terminal.WriteLine("");

        // Check if player is in a team
        if (string.IsNullOrEmpty(player.Team))
        {
            terminal.SetColor("yellow");
            terminal.WriteLine("You are not in a team.");
            terminal.WriteLine("Visit the Team Corner in town to create or join a team!");
            terminal.WriteLine("");
            await terminal.PressAnyKey();
            return;
        }

        terminal.SetColor("white");
        terminal.WriteLine($"Team: {player.Team}");
        terminal.WriteLine($"Team controls turf: {(player.CTurf ? "Yes" : "No")}");
        terminal.WriteLine("");

        // Show current dungeon party
        terminal.SetColor("cyan");
        terminal.WriteLine("Current Dungeon Party:");
        terminal.WriteLine($"  1. {player.DisplayName} (You) - Level {player.Level} {player.Class}");
        for (int i = 0; i < teammates.Count; i++)
        {
            var tm = teammates[i];
            string status = tm.IsAlive ? $"HP: {tm.HP}/{tm.MaxHP}" : "[INJURED]";
            terminal.WriteLine($"  {i + 2}. {tm.DisplayName} - Level {tm.Level} {tm.Class} - {status}");
        }
        terminal.WriteLine("");

        // Get available NPC teammates from same team
        var npcTeammates = UsurperRemake.Systems.NPCSpawnSystem.Instance.ActiveNPCs
            .Where(n => n.Team == player.Team && n.IsAlive && !teammates.Contains(n))
            .ToList();

        if (npcTeammates.Count > 0)
        {
            terminal.SetColor("green");
            terminal.WriteLine("Available Team Members (not in dungeon party):");
            for (int i = 0; i < npcTeammates.Count; i++)
            {
                var npc = npcTeammates[i];
                terminal.WriteLine($"  [{i + 1}] {npc.DisplayName} - Level {npc.Level} {npc.Class} - HP: {npc.HP}/{npc.MaxHP}");
            }
            terminal.WriteLine("");
        }
        else if (teammates.Count == 0)
        {
            terminal.SetColor("gray");
            terminal.WriteLine("No team members available to join your dungeon party.");
            terminal.WriteLine("Recruit NPCs at the Team Corner or Hall of Recruitment!");
            terminal.WriteLine("");
        }

        // Show options
        terminal.SetColor("white");
        terminal.WriteLine("Options:");
        if (npcTeammates.Count > 0 && teammates.Count < 4) // Max 4 teammates + player = 5
        {
            terminal.WriteLine("  [A]dd a team member to dungeon party");
        }
        if (teammates.Count > 0)
        {
            terminal.WriteLine("  [R]emove a team member from party");
        }
        terminal.WriteLine("  [B]ack to dungeon menu");
        terminal.WriteLine("");

        var choice = await terminal.GetInput("Choice: ");
        choice = choice.ToUpper().Trim();

        switch (choice)
        {
            case "A":
                if (npcTeammates.Count > 0 && teammates.Count < 4)
                {
                    await AddTeammateToParty(npcTeammates);
                }
                else if (teammates.Count >= 4)
                {
                    terminal.WriteLine("Your dungeon party is full (max 4 teammates)!", "yellow");
                    await Task.Delay(1500);
                }
                else
                {
                    terminal.WriteLine("No team members available to add.", "gray");
                    await Task.Delay(1500);
                }
                break;

            case "R":
                if (teammates.Count > 0)
                {
                    await RemoveTeammateFromParty();
                }
                else
                {
                    terminal.WriteLine("No teammates to remove.", "gray");
                    await Task.Delay(1500);
                }
                break;

            case "B":
            default:
                break;
        }
    }

    private async Task AddTeammateToParty(List<NPC> available)
    {
        terminal.WriteLine("");
        terminal.SetColor("white");
        terminal.Write("Enter number of team member to add (1-");
        terminal.Write($"{available.Count}");
        terminal.Write("): ");
        var input = await terminal.GetInput("");

        if (int.TryParse(input, out int index) && index >= 1 && index <= available.Count)
        {
            var npc = available[index - 1];
            teammates.Add(npc);

            // Move NPC to dungeon
            npc.UpdateLocation("Dungeon");

            terminal.SetColor("green");
            terminal.WriteLine($"{npc.DisplayName} joins your dungeon party!");
            terminal.WriteLine("They will fight alongside you against monsters.");

            // 15% team XP/gold bonus for having teammates
            terminal.SetColor("cyan");
            terminal.WriteLine("Team bonus: +15% XP and gold from battles!");
        }
        else
        {
            terminal.WriteLine("Invalid selection.", "red");
        }
        await Task.Delay(2000);
    }

    private async Task RemoveTeammateFromParty()
    {
        terminal.WriteLine("");
        terminal.SetColor("white");
        terminal.Write("Enter number of teammate to remove (1-");
        terminal.Write($"{teammates.Count}");
        terminal.Write("): ");
        var input = await terminal.GetInput("");

        if (int.TryParse(input, out int index) && index >= 1 && index <= teammates.Count)
        {
            var member = teammates[index - 1];
            teammates.RemoveAt(index - 1);

            // Move NPC back to town (cast to NPC if applicable)
            if (member is NPC npc)
            {
                npc.UpdateLocation("Main Street");
            }

            terminal.SetColor("yellow");
            terminal.WriteLine($"{member.DisplayName} leaves the dungeon party and returns to town.");
        }
        else
        {
            terminal.WriteLine("Invalid selection.", "red");
        }
        await Task.Delay(1500);
    }
    
    private async Task ShowDungeonStatus()
    {
        await ShowStatus();
    }
    
    private async Task VisitMonk()
    {
        var player = GetCurrentPlayer();
        var combatEngine = new CombatEngine(terminal);
        await combatEngine.OfferMonkPotionPurchase(player);
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
        terminal.WriteLine("E - Explore the current level");
        terminal.WriteLine("D - Descend to a deeper, more dangerous level");
        terminal.WriteLine("A - Ascend to a safer level or return to town");
        terminal.WriteLine("T - Manage your team members");
        terminal.WriteLine("S - View your character status");
        terminal.WriteLine("P - Buy potions from the wandering monk");
        terminal.WriteLine("M - View the dungeon map");
        terminal.WriteLine("Q - Quit and return to town");
        terminal.WriteLine("");
        await terminal.PressAnyKey();
    }
    
    /// <summary>
    /// Increase dungeon level directly up to +10 levels above the player (original mechanic).
    /// </summary>
    private async Task IncreaseDifficulty()
    {
        var playerLevel = GetCurrentPlayer()?.Level ?? 1;
        int targetLevel = currentDungeonLevel + 10;
        int maxAllowed = Math.Min(maxDungeonLevel, playerLevel + 10);

        if (targetLevel > maxAllowed)
            targetLevel = maxAllowed;

        if (targetLevel == currentDungeonLevel)
        {
            terminal.WriteLine("You cannot raise the difficulty any higher right now.", "yellow");
        }
        else
        {
            currentDungeonLevel = targetLevel;
            terminal.WriteLine($"You steel your nerves. The dungeon now feels like level {currentDungeonLevel}!", "magenta");
        }

        await Task.Delay(1500);
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
