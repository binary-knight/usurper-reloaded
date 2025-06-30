using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

/// <summary>
/// Temple of the Gods - Complete Pascal-compatible temple system
/// Based on TEMPLE.PAS with worship, sacrifice, and divine services
/// Integrated with Phase 13 God System
/// </summary>
public partial class TempleLocation : BaseLocation
{
    private readonly TerminalEmulator terminal;
    private readonly LocationManager locationManager;
    private readonly GodSystem godSystem;
    private bool refreshMenu = true;
    private Character currentPlayer;
    
    public TempleLocation(TerminalEmulator terminal, LocationManager locationManager, GodSystem godSystem)
    {
        this.terminal = terminal;
        this.locationManager = locationManager;
        this.godSystem = godSystem;
        
        LocationName = "Temple of the Gods";
        LocationId = "temple";
        Description = "The Temple area is crowded with monks, preachers and processions of priests on their way to the altars. The doomsday prophets are trying to get your attention.";
    }
    
    /// <summary>
    /// Main temple processing loop based on Pascal TEMPLE.PAS
    /// </summary>
    public override async Task<string> ProcessLocation(Character player)
    {
        currentPlayer = player;
        terminal.Clear();
        
        await DisplayWelcomeMessage();
        await VerifyPlayerGodExists();
        
        bool exitLocation = false;
        refreshMenu = true;
        
        while (!exitLocation)
        {
            try
            {
                await DisplayMenu(refreshMenu);
                refreshMenu = false;
                
                var choice = await terminal.GetInputAsync("Temple area (? for menu) :");
                
                switch (choice.ToUpper())
                {
                    case "?":
                        refreshMenu = true;
                        continue;
                        
                    case GameConfig.TempleMenuWorship: // "W"
                        await ProcessWorship();
                        break;
                        
                    case GameConfig.TempleMenuDesecrate: // "D"
                        await ProcessDesecrateAltar();
                        break;
                        
                    case GameConfig.TempleMenuAltars: // "A"
                        await DisplayAltars();
                        break;
                        
                    case GameConfig.TempleMenuContribute: // "C"
                        await ProcessContribute();
                        break;
                        
                    case GameConfig.TempleMenuStatus: // "S"
                        await DisplayPlayerStatus();
                        break;
                        
                    case GameConfig.TempleMenuGodRanking: // "G"
                        await DisplayGodRanking();
                        break;
                        
                    case GameConfig.TempleMenuHolyNews: // "H"
                        await DisplayHolyNews();
                        break;
                        
                    case GameConfig.TempleMenuReturn: // "R"
                        exitLocation = true;
                        break;
                        
                    default:
                        terminal.WriteLine("Invalid choice. Press ? for menu.", "red");
                        await Task.Delay(1000);
                        break;
                }
            }
            catch (LocationChangeException ex)
            {
                return ex.NewLocation;
            }
        }
        
        return "main_street";
    }
    
    /// <summary>
    /// Display temple welcome message (Pascal TEMPLE.PAS)
    /// </summary>
    private async Task DisplayWelcomeMessage()
    {
        terminal.WriteLine("");
        terminal.WriteLine("You enter the Temple Area", "yellow");
        terminal.WriteLine("");
        
        string playerGod = godSystem.GetPlayerGod(currentPlayer.Name2);
        if (!string.IsNullOrEmpty(playerGod))
        {
            terminal.WriteLine($"You worship {playerGod}.", "cyan");
        }
        else
        {
            terminal.WriteLine("You are not a believer.", "gray");
        }
        
        await Task.Delay(1500);
    }
    
    /// <summary>
    /// Display temple menu (Pascal TEMPLE.PAS Meny procedure)
    /// </summary>
    private async Task DisplayMenu(bool forceDisplay)
    {
        if (!forceDisplay && currentPlayer.Expert) return;
        
        terminal.Clear();
        terminal.WriteLine("");
        terminal.WriteLine("═══ Temple of the Gods ═══", "magenta");
        terminal.WriteLine("");
        terminal.WriteLine("The Temple area is crowded with monks, preachers and", "white");
        terminal.WriteLine("processions of priests on their way to the altars.", "white");
        terminal.WriteLine("The doomsday prophets are trying to get your attention.", "white");
        terminal.WriteLine("");
        
        string playerGod = godSystem.GetPlayerGod(currentPlayer.Name2);
        if (!string.IsNullOrEmpty(playerGod))
        {
            terminal.WriteLine($"You worship {playerGod}.", "cyan");
        }
        else
        {
            terminal.WriteLine("You are not a believer.", "gray");
        }
        terminal.WriteLine("");
        
        // Main menu options (Pascal TEMPLE.PAS format)
        terminal.WriteLine("(W)orship              (D)esecrate altar       (H)oly News", "yellow");
        terminal.WriteLine("(A)ltars               (C)ontribute", "yellow");
        terminal.WriteLine("(S)tatus               (G)od ranking", "yellow");
        terminal.WriteLine("(R)eturn", "yellow");
        terminal.WriteLine("");
    }
    
    /// <summary>
    /// Process worship selection and god faith (Pascal TEMPLE.PAS)
    /// </summary>
    private async Task ProcessWorship()
    {
        terminal.WriteLine("");
        terminal.WriteLine("");
        
        string currentGod = godSystem.GetPlayerGod(currentPlayer.Name2);
        bool goAhead = true;
        
        if (!string.IsNullOrEmpty(currentGod))
        {
            terminal.WriteLine($"You currently worship {currentGod}.", "white");
            
            var choice = await terminal.GetInputAsync($"Have you lost your faith in {currentGod}? (Y/N) ");
            if (choice.ToUpper() == "Y")
            {
                // Abandon faith
                terminal.WriteLine("");
                terminal.WriteLine($"You don't believe in {currentGod} anymore.", "white");
                terminal.WriteLine($"{currentGod}'s powers diminish...", "yellow");
                
                var noteChoice = await terminal.GetInputAsync($"Send a note to {currentGod}? (Y/N) ");
                string note = "";
                if (noteChoice.ToUpper() == "Y")
                {
                    note = await terminal.GetInputAsync("Note: ");
                    terminal.WriteLine("Done!", "green");
                }
                
                if (string.IsNullOrEmpty(note))
                {
                    var randomNotes = new[] 
                    {
                        "You are not my God!",
                        "farewell..",
                        "never again will I follow you!"
                    };
                    note = randomNotes[new Random().Next(randomNotes.Length)];
                }
                
                // Remove from god system
                godSystem.SetPlayerGod(currentPlayer.Name2, "");
                
                // In Pascal, this would send mail to the god and news
                terminal.WriteLine("");
                terminal.WriteLine("You are no longer a believer.", "yellow");
            }
            else
            {
                terminal.WriteLine("Good for you. The gods don't take too kindly on apostates.", "green");
                goAhead = false;
            }
        }
        
        if (goAhead)
        {
            terminal.WriteLine("Select a God to worship", "white");
            var selectedGod = await SelectGod();
            
            if (selectedGod != null)
            {
                terminal.WriteLine("Ok.", "green");
                terminal.WriteLine($"You raise your hands and pray to the almighty {selectedGod.Name}", "white");
                terminal.Write("for forgiveness", "white");
                
                // Delay dots animation (Pascal Make_Delay_Dots)
                for (int i = 0; i < 15; i++)
                {
                    terminal.Write(".", "white");
                    await Task.Delay(300);
                }
                terminal.WriteLine("");
                
                terminal.WriteLine($"You are now a believer in {selectedGod.Name}!", "yellow");
                
                // Set in god system
                godSystem.SetPlayerGod(currentPlayer.Name2, selectedGod.Name);
                
                // In Pascal, this would send mail to god and news
                terminal.WriteLine("");
                terminal.WriteLine("The gods smile upon your faith!", "cyan");
            }
        }
        
        await Task.Delay(2000);
    }
    
    /// <summary>
    /// Process altar desecration (Pascal TEMPLE.PAS)
    /// </summary>
    private async Task ProcessDesecrateAltar()
    {
        terminal.WriteLine("");
        terminal.WriteLine("");
        
        if (currentPlayer.DarkNr < 1)
        {
            terminal.WriteLine("You don't have any evil deeds left!", "red");
            await Task.Delay(2000);
            return;
        }
        
        var choice = await terminal.GetInputAsync("Do you really want to upset the gods? (Y/N) ");
        if (choice.ToUpper() != "Y")
        {
            terminal.WriteLine("Good for you!", "green");
            await Task.Delay(1000);
            return;
        }
        
        var selectedGod = await SelectGod("Select god to desecrate altar");
        if (selectedGod == null) return;
        
        string playerGod = godSystem.GetPlayerGod(currentPlayer.Name2);
        if (!string.IsNullOrEmpty(playerGod) && playerGod == selectedGod.Name)
        {
            terminal.WriteLine("");
            terminal.WriteLine("You are not allowed to abuse your own God!", "red");
            await Task.Delay(2000);
            return;
        }
        
        var confirmChoice = await terminal.GetInputAsync($"Desecrate {selectedGod.Name}'s altar? (Y/N) ");
        if (confirmChoice.ToUpper() != "Y") return;
        
        await PerformDesecration(selectedGod);
    }
    
    /// <summary>
    /// Process contribution/sacrifice to gods (Pascal TEMPLE.PAS contribute_to_god)
    /// </summary>
    private async Task ProcessContribute()
    {
        terminal.WriteLine("");
        terminal.WriteLine("");
        
        if (currentPlayer.ChivNr < 1)
        {
            terminal.WriteLine("You don't have any good deeds left!", "red");
            await Task.Delay(2000);
            return;
        }
        
        terminal.WriteLine("═══ Who shall receive your gift ═══", "cyan");
        var selectedGod = await SelectGod();
        if (selectedGod == null) return;
        
        string playerGod = godSystem.GetPlayerGod(currentPlayer.Name2);
        bool wrongGod = false;
        bool goAhead = true;
        
        if (!string.IsNullOrEmpty(playerGod) && playerGod != selectedGod.Name)
        {
            terminal.WriteLine("");
            terminal.WriteLine($"{selectedGod.Name} is not your God! Are you sure about this?", "red");
            terminal.WriteLine($"The mighty {playerGod} is not going to be happy.", "red");
            
            var choice = await terminal.GetInputAsync("Continue? (Y/N) ");
            if (choice.ToUpper() == "Y")
            {
                wrongGod = true;
            }
            else
            {
                terminal.WriteLine("Good for you!", "green");
                goAhead = false;
            }
        }
        
        if (goAhead)
        {
            await ProcessSacrificeMenu(selectedGod, wrongGod);
        }
        
        await Task.Delay(1000);
    }
    
    /// <summary>
    /// Display all altars (Pascal TEMPLE.PAS)
    /// </summary>
    private async Task DisplayAltars()
    {
        terminal.WriteLine("");
        terminal.WriteLine("");
        terminal.WriteLine("═══ Altars of the Gods ═══", "magenta");
        terminal.WriteLine("");
        
        var activeGods = godSystem.GetActiveGods();
        if (activeGods.Count == 0)
        {
            terminal.WriteLine("No gods exist in this realm.", "gray");
        }
        else
        {
            foreach (var god in activeGods.OrderByDescending(g => g.Experience))
            {
                terminal.WriteLine($"Altar of {god.Name} the {god.GetTitle()}", "yellow");
                terminal.WriteLine($"  Believers: {god.Believers}", "white");
                terminal.WriteLine($"  Power: {god.Experience}", "cyan");
                terminal.WriteLine("");
            }
        }
        
        await terminal.GetInputAsync("Press Enter to continue...");
    }
    
    /// <summary>
    /// Display god ranking (Pascal TEMPLE.PAS)
    /// </summary>
    private async Task DisplayGodRanking()
    {
        terminal.WriteLine("");
        terminal.WriteLine("");
        
        var rankedGods = godSystem.ListGods(true);
        
        if (rankedGods.Count == 0)
        {
            terminal.WriteLine("No gods exist in this realm.", "gray");
        }
        else
        {
            terminal.WriteLine("   Immortals                Rank                Followers", "white");
            terminal.WriteLine("═══════════════════════════════════════════════════════════", "magenta");
            
            for (int i = 0; i < rankedGods.Count; i++)
            {
                var god = rankedGods[i];
                string line = $"{(i + 1).ToString().PadLeft(3)}. {god.Name.PadRight(25)} {god.GetTitle().PadRight(20)} {god.Believers.ToString().PadLeft(10)}";
                terminal.WriteLine(line, "yellow");
            }
        }
        
        await terminal.GetInputAsync("Press Enter to continue...");
    }
    
    /// <summary>
    /// Display holy news (Pascal TEMPLE.PAS)
    /// </summary>
    private async Task DisplayHolyNews()
    {
        terminal.WriteLine("");
        terminal.WriteLine("");
        terminal.WriteLine("═══ Holy News ═══", "cyan");
        terminal.WriteLine("");
        terminal.WriteLine("The gods watch over the realm...", "white");
        terminal.WriteLine("Divine interventions shape the fate of mortals...", "white");
        terminal.WriteLine("Prayers and sacrifices reach the heavens...", "white");
        terminal.WriteLine("");
        
        var stats = godSystem.GetGodStatistics();
        terminal.WriteLine($"Total Active Gods: {stats["TotalGods"]}", "yellow");
        terminal.WriteLine($"Total Believers: {stats["TotalBelievers"]}", "yellow");
        terminal.WriteLine($"Most Powerful: {stats["MostPowerfulGod"]}", "yellow");
        terminal.WriteLine($"Most Popular: {stats["MostPopularGod"]}", "yellow");
        
        await terminal.GetInputAsync("Press Enter to continue...");
    }
    
    /// <summary>
    /// Select a god from available options (Pascal Select_A_God function)
    /// </summary>
    private async Task<God> SelectGod(string prompt = "Select a god")
    {
        var activeGods = godSystem.GetActiveGods();
        if (activeGods.Count == 0)
        {
            terminal.WriteLine("No gods are available.", "red");
            await Task.Delay(1000);
            return null;
        }
        
        while (true)
        {
            terminal.WriteLine($"{prompt} (press ? for list):", "white");
            var input = await terminal.GetInputAsync("");
            
            if (input == "?")
            {
                await DisplayGodList();
                continue;
            }
            
            if (string.IsNullOrEmpty(input))
            {
                return null;
            }
            
            var selectedGod = activeGods.FirstOrDefault(g => 
                g.Name.Equals(input, StringComparison.OrdinalIgnoreCase));
                
            if (selectedGod != null)
            {
                return selectedGod;
            }
            
            terminal.WriteLine("God not found. Try again or press ? for list.", "red");
        }
    }
    
    /// <summary>
    /// Display list of available gods
    /// </summary>
    private async Task DisplayGodList()
    {
        terminal.WriteLine("");
        terminal.WriteLine("Available Gods:", "cyan");
        terminal.WriteLine("");
        
        var activeGods = godSystem.GetActiveGods().OrderBy(g => g.Name);
        foreach (var god in activeGods)
        {
            terminal.WriteLine($"  {god.Name} - {god.GetTitle()}", "yellow");
        }
        terminal.WriteLine("");
    }
    
    /// <summary>
    /// Perform altar desecration (Pascal TEMPLE.PAS)
    /// </summary>
    private async Task PerformDesecration(God god)
    {
        terminal.WriteLine("");
        terminal.WriteLine("");
        
        var random = new Random();
        switch (random.Next(2))
        {
            case 0:
                terminal.WriteLine("When nobody is around You start to", "white");
                terminal.WriteLine("pound away at the altar with a pickaxe.", "white");
                terminal.Write("Hack", "red");
                for (int i = 0; i < 4; i++)
                {
                    await Task.Delay(500);
                    terminal.Write(".", "red");
                }
                terminal.Write("hack", "red");
                for (int i = 0; i < 4; i++)
                {
                    await Task.Delay(500);
                    terminal.Write(".", "red");
                }
                terminal.WriteLine("hack..!", "red");
                break;
                
            case 1:
                terminal.WriteLine("You find some unholy substances and", "white");
                terminal.WriteLine("pour them all over the altar!", "white");
                terminal.WriteLine("The altar is severely damaged!", "red");
                break;
        }
        
        terminal.WriteLine("");
        terminal.WriteLine($"You have desecrated {god.Name}'s altar!", "red");
        terminal.WriteLine("The gods will remember this blasphemy!", "red");
        
        // Process desecration in god system
        godSystem.ProcessAltarDesecration(god.Name, currentPlayer.Name2);
        
        // Use evil deed
        currentPlayer.DarkNr--;
        
        await Task.Delay(3000);
    }
    
    /// <summary>
    /// Process sacrifice menu (Pascal TEMPLE.PAS)
    /// </summary>
    private async Task ProcessSacrificeMenu(God god, bool wrongGod)
    {
        bool done = false;
        
        while (!done)
        {
            terminal.WriteLine("");
            terminal.WriteLine($"═══ Sacrifice to {god.Name} ═══", "cyan");
            terminal.WriteLine("");
            terminal.WriteLine("(G)old", "yellow");
            terminal.WriteLine("(S)tatus", "yellow");
            terminal.WriteLine("(R)eturn", "yellow");
            
            var choice = await terminal.GetInputAsync("Sacrifice (? for menu): ");
            
            switch (choice.ToUpper())
            {
                case "G":
                    await ProcessGoldSacrifice(god, wrongGod);
                    break;
                    
                case "S":
                    await DisplayPlayerStatus();
                    break;
                    
                case "R":
                    done = true;
                    break;
                    
                case "?":
                    // Menu already displayed
                    break;
                    
                default:
                    terminal.WriteLine("Invalid choice.", "red");
                    await Task.Delay(1000);
                    break;
            }
        }
    }
    
    /// <summary>
    /// Process gold sacrifice (Pascal TEMPLE.PAS)
    /// </summary>
    private async Task ProcessGoldSacrifice(God god, bool wrongGod)
    {
        terminal.WriteLine("");
        var goldStr = await terminal.GetInputAsync("Amount of gold to sacrifice: ");
        
        if (!long.TryParse(goldStr, out long goldAmount) || goldAmount <= 0)
        {
            terminal.WriteLine("Invalid amount.", "red");
            await Task.Delay(1000);
            return;
        }
        
        if (goldAmount > currentPlayer.Gold)
        {
            terminal.WriteLine("You don't have that much gold!", "red");
            await Task.Delay(1000);
            return;
        }
        
        var choice = await terminal.GetInputAsync($"Sacrifice {goldAmount} gold to {god.Name}? (Y/N) ");
        if (choice.ToUpper() != "Y") return;
        
        // Process sacrifice
        currentPlayer.Gold -= goldAmount;
        var powerGained = godSystem.ProcessGoldSacrifice(god.Name, goldAmount, currentPlayer.Name2);
        
        terminal.WriteLine("");
        terminal.WriteLine($"{god.Name}'s power is growing!", "yellow");
        terminal.WriteLine("You can feel it...Your reward will come.", "white");
        terminal.WriteLine($"Power increased by {powerGained} points!", "cyan");
        
        // Use good deed
        currentPlayer.ChivNr--;
        
        await Task.Delay(3000);
    }
    
    /// <summary>
    /// Verify player's god still exists (Pascal TEMPLE.PAS)
    /// </summary>
    private async Task VerifyPlayerGodExists()
    {
        string playerGod = godSystem.GetPlayerGod(currentPlayer.Name2);
        if (!string.IsNullOrEmpty(playerGod))
        {
            if (!godSystem.VerifyGodExists(playerGod))
            {
                terminal.WriteLine($"Your god {playerGod} no longer exists!", "red");
                terminal.WriteLine("Your faith has been shaken...", "gray");
                godSystem.SetPlayerGod(currentPlayer.Name2, "");
                await Task.Delay(2000);
            }
        }
    }
    
    /// <summary>
    /// Display player status
    /// </summary>
    private async Task DisplayPlayerStatus()
    {
        terminal.WriteLine("");
        terminal.WriteLine("═══ Your Status ═══", "cyan");
        terminal.WriteLine("");
        terminal.WriteLine($"Name: {currentPlayer.Name2}", "yellow");
        terminal.WriteLine($"Level: {currentPlayer.Level}", "yellow");
        terminal.WriteLine($"Gold: {currentPlayer.Gold:N0}", "yellow");
        terminal.WriteLine($"Good Deeds: {currentPlayer.ChivNr}", "green");
        terminal.WriteLine($"Evil Deeds: {currentPlayer.DarkNr}", "red");
        
        string playerGod = godSystem.GetPlayerGod(currentPlayer.Name2);
        if (!string.IsNullOrEmpty(playerGod))
        {
            terminal.WriteLine($"God: {playerGod}", "cyan");
        }
        else
        {
            terminal.WriteLine("God: None (Pagan)", "gray");
        }
        
        await terminal.GetInputAsync("Press Enter to continue...");
    }
} 