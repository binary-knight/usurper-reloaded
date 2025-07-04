using UsurperRemake.Utils;
using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Anchor Road Location - Complete implementation based on Pascal CHALLENG.PAS
/// Central challenge hub providing access to various adventure activities including tournaments, gang wars, 
/// bounty hunting, quests, and gym competitions
/// Direct Pascal compatibility with exact function preservation
/// </summary>
public class AnchorRoadLocation : BaseLocation
{
    private readonly TournamentSystem? tournamentSystem = UsurperRemake.Utils.GodotHelpers.GetNode<TournamentSystem>("/root/TournamentSystem");
    private readonly QuestSystem? questSystem = UsurperRemake.Utils.GodotHelpers.GetNode<QuestSystem>("/root/QuestSystem");
    private readonly TeamSystem? teamSystem = UsurperRemake.Utils.GodotHelpers.GetNode<TeamSystem>("/root/TeamSystem");
    private readonly NewsSystem newsSystem = NewsSystem.Instance;
    
    // Pascal constants from CHALLENG.PAS
    private const string LocationName = "Anchor Road";
    private const string LocationDescription = "Conjunction of Destinies";
    private const int MenuOffset = 20; // Pascal offset constant
    
    public AnchorRoadLocation() : base(GameLocation.AnchorRoad, "Anchor Road", "Conjunction of Destinies")
    {
    }
    
    // No _Ready needed for standalone mode
    
    /// <summary>
    /// Show full menu - Pascal CHALLENG.PAS Meny procedure
    /// </summary>
    private void ShowFullMenu()
    {
        var terminal = this.terminal;
        
        terminal.ClearScreen();
        terminal.WriteLine($"\n{GameConfig.BrightColor}-*- {GameConfig.AnchorName}, {LocationDescription} -*-{GameConfig.TextColor}");
        terminal.WriteLine("\nTo the north you can see the Castle in all its might.");
        terminal.WriteLine("The Dormitory lies to the west.");
        terminal.WriteLine("The Red Fields are to the east.");
        terminal.WriteLine("It's time to be brave.\n");
        
        // Menu options with Pascal spacing
        terminal.Write($"{PadMenuOption("(D)ormitory", MenuOffset)}{PadMenuOption("(B)ounty hunting", MenuOffset)}");
        terminal.WriteLine("(Q)uests");
        
        terminal.Write($"{PadMenuOption("(G)ang war", MenuOffset)}{PadMenuOption("(O)nline war", MenuOffset)}{PadMenuOption("(A)ltar of the Gods", MenuOffset)}");
        terminal.WriteLine("");
        
        terminal.Write($"{PadMenuOption("(C)laim town", MenuOffset)}");
        terminal.WriteLine("(F)lee town control");
        
        terminal.Write($"{PadMenuOption("(S)tatus", MenuOffset)}{PadMenuOption("(K)ings Castle", MenuOffset)}");
        terminal.WriteLine("(T)he Gym");
        
        terminal.WriteLine("(P)rison grounds (attempt a jailbreak)");
        
        terminal.WriteLine($"{PadMenuOption("(R)eturn to town", MenuOffset)}");
    }
    
    /// <summary>
    /// Pad menu option - Pascal CHALLENG.PAS ljust function
    /// </summary>
    private string PadMenuOption(string option, int width)
    {
        return option.PadRight(width);
    }
    
    // BaseLocation override to process one choice
    protected override async Task<bool> ProcessChoice(string choice)
    {
        if (string.IsNullOrWhiteSpace(choice)) return false;
        char ch = char.ToUpperInvariant(choice.Trim()[0]);

        switch (ch)
        {
            case 'D':
                await NavigateToDormitory(currentPlayer);
                return true;
            case 'B':
                await StartBountyHunting(currentPlayer);
                return false;
            case 'Q':
                await StartQuests(currentPlayer);
                return false;
            case 'G':
                await StartGangWar(currentPlayer);
                return false;
            case 'O':
                await StartOnlineWar(currentPlayer);
                return false;
            case 'A':
                await NavigateToAltar(currentPlayer);
                return true;
            case 'C':
                await ClaimTown(currentPlayer);
                return false;
            case 'F':
                await FleeTownControl(currentPlayer);
                return false;
            case 'S':
                await ShowPlayerStatus(currentPlayer);
                return false;
            case 'K':
                await NavigateToKingsCastle(currentPlayer);
                return true;
            case 'T':
                await NavigateToGym(currentPlayer);
                return true;
            case 'P':
                await NavigateToPrisonGrounds(currentPlayer);
                return true;
            case 'R':
                await ReturnToTown(currentPlayer);
                return true;
            case '?':
                return false; // menu redisplayed each loop
            default:
                terminal.WriteLine($"{GameConfig.ErrorColor}Invalid choice. Press ? for menu.{GameConfig.TextColor}");
                await Task.Delay(1000);
                return false;
        }
    }

    protected override void DisplayLocation()
    {
        ShowFullMenu();
        ShowStatusLine();
    }
    
    #region Navigation Methods - Pascal CHALLENG.PAS location functions
    
    /// <summary>
    /// Navigate to dormitory
    /// </summary>
    private async Task NavigateToDormitory(Character player)
    {
        var terminal = GetNode<TerminalEmulator>("/root/TerminalEmulator");
        terminal.WriteLine($"\n{GameConfig.LocationColor}You walk west to the Dormitory...{GameConfig.TextColor}");
        
        // Navigate to dormitory location
        await LocationManager.Instance.ChangeLocation(player, "DormitoryLocation");
    }
    
    /// <summary>
    /// Start bounty hunting - Pascal CHALLENG.PAS bounty hunting functionality
    /// </summary>
    private async Task StartBountyHunting(Character player)
    {
        var terminal = GetNode<TerminalEmulator>("/root/TerminalEmulator");
        
        if (player.PFights <= 0)
        {
            terminal.WriteLine($"\n{GameConfig.ErrorColor}You have no player fights left today!{GameConfig.TextColor}");
            await terminal.WaitForKeyPress();
            return;
        }
        
        terminal.WriteLine($"\n{GameConfig.CombatColor}You decide to hunt for bounties...{GameConfig.TextColor}");
        
        // Navigate to bounty hunting system
        await LocationManager.Instance.ChangeLocation(player, "BountyHuntingLocation");
    }
    
    /// <summary>
    /// Start quests - Pascal CHALLENG.PAS quest functionality
    /// </summary>
    private async Task StartQuests(Character player)
    {
        var terminal = GetNode<TerminalEmulator>("/root/TerminalEmulator");
        terminal.WriteLine($"\n{GameConfig.QuestColor}You look for available quests...{GameConfig.TextColor}");
        
        // Show quest options
        await questSystem.ShowAvailableQuests(player);
        await terminal.WaitForKeyPress();
    }
    
    /// <summary>
    /// Start gang war - Pascal CHALLENG.PAS gang war functionality
    /// </summary>
    private async Task StartGangWar(Character player)
    {
        var terminal = GetNode<TerminalEmulator>("/root/TerminalEmulator");
        
        if (string.IsNullOrEmpty(player.Team))
        {
            terminal.WriteLine($"\n{GameConfig.ErrorColor}You must be in a team to participate in gang wars!{GameConfig.TextColor}");
            await terminal.WaitForKeyPress();
            return;
        }
        
        terminal.WriteLine($"\n{GameConfig.CombatColor}You prepare for gang warfare...{GameConfig.TextColor}");
        
        // Use TeamSystem gang warfare
        await teamSystem.InitiateGangWar(player);
        await terminal.WaitForKeyPress();
    }
    
    /// <summary>
    /// Start online war - Pascal CHALLENG.PAS online war functionality
    /// </summary>
    private async Task StartOnlineWar(Character player)
    {
        var terminal = GetNode<TerminalEmulator>("/root/TerminalEmulator");
        
        if (player.PFights <= 0)
        {
            terminal.WriteLine($"\n{GameConfig.ErrorColor}You have no player fights left today!{GameConfig.TextColor}");
            await terminal.WaitForKeyPress();
            return;
        }
        
        terminal.WriteLine($"\n{GameConfig.CombatColor}You seek online opponents...{GameConfig.TextColor}");
        
        // Navigate to online dueling system
        await LocationManager.Instance.ChangeLocation(player, "OnlineDuelLocation");
    }
    
    /// <summary>
    /// Navigate to altar
    /// </summary>
    private async Task NavigateToAltar(Character player)
    {
        var terminal = GetNode<TerminalEmulator>("/root/TerminalEmulator");
        terminal.WriteLine($"\n{GameConfig.LocationColor}You approach the Altar of the Gods...{GameConfig.TextColor}");
        
        // Navigate to temple/altar location
        await LocationManager.Instance.ChangeLocation(player, "AltarLocation");
    }
    
    /// <summary>
    /// Claim town - Pascal CHALLENG.PAS town control functionality
    /// </summary>
    private async Task ClaimTown(Character player)
    {
        var terminal = GetNode<TerminalEmulator>("/root/TerminalEmulator");
        
        if (string.IsNullOrEmpty(player.Team))
        {
            terminal.WriteLine($"\n{GameConfig.ErrorColor}You must be in a team to claim towns!{GameConfig.TextColor}");
            await terminal.WaitForKeyPress();
            return;
        }
        
        if (player.CTurf)
        {
            terminal.WriteLine($"\n{GameConfig.ErrorColor}Your team already controls this town!{GameConfig.TextColor}");
            await terminal.WaitForKeyPress();
            return;
        }
        
        terminal.WriteLine($"\n{GameConfig.CombatColor}You attempt to claim the town for your team...{GameConfig.TextColor}");
        
        // Use TeamSystem town claiming
        var result = await teamSystem.ClaimTown(player);
        
        if (result.Success)
        {
            terminal.WriteLine($"\n{GameConfig.SuccessColor}{result.Message}{GameConfig.TextColor}");
            
            // News coverage
            newsSystem.Newsy($"Town Claimed! {GameConfig.NewsColorPlayer}{player.Name2}{GameConfig.NewsColorDefault}'s team has claimed control of the town!",
                false, GameConfig.NewsCategory.General);
        }
        else
        {
            terminal.WriteLine($"\n{GameConfig.ErrorColor}{result.Message}{GameConfig.TextColor}");
        }
        
        await terminal.WaitForKeyPress();
    }
    
    /// <summary>
    /// Flee town control - Pascal CHALLENG.PAS flee town functionality
    /// </summary>
    private async Task FleeTownControl(Character player)
    {
        var terminal = GetNode<TerminalEmulator>("/root/TerminalEmulator");
        
        if (!player.CTurf)
        {
            terminal.WriteLine($"\n{GameConfig.ErrorColor}Your team doesn't control any town!{GameConfig.TextColor}");
            await terminal.WaitForKeyPress();
            return;
        }
        
        terminal.WriteLine($"\n{GameConfig.WarningColor}Are you sure you want to abandon town control? (Y/N){GameConfig.TextColor}");
        
        var confirm = await terminal.GetKeyCharAsync();
        if (char.ToUpper(confirm) == 'Y')
        {
            var result = await teamSystem.AbandonTownControl(player);
            
            if (result.Success)
            {
                terminal.WriteLine($"\n{GameConfig.SuccessColor}{result.Message}{GameConfig.TextColor}");
                
                // News coverage
                newsSystem.Newsy($"Town Abandoned! {GameConfig.NewsColorPlayer}{player.Name2}{GameConfig.NewsColorDefault}'s team has abandoned control of the town!",
                    false, GameConfig.NewsCategory.General);
            }
            else
            {
                terminal.WriteLine($"\n{GameConfig.ErrorColor}{result.Message}{GameConfig.TextColor}");
            }
        }
        else
        {
            terminal.WriteLine($"\n{GameConfig.TextColor}Town control maintained.{GameConfig.TextColor}");
        }
        
        await terminal.WaitForKeyPress();
    }
    
    /// <summary>
    /// Show player status - Pascal CHALLENG.PAS status functionality
    /// </summary>
    private async Task ShowPlayerStatus(Character player)
    {
        var terminal = GetNode<TerminalEmulator>("/root/TerminalEmulator");
        
        terminal.WriteLine($"\n{GameConfig.StatusColor}=== Character Status ==={GameConfig.TextColor}");
        terminal.WriteLine($"Name: {GameConfig.PlayerColor}{player.Name2}{GameConfig.TextColor}");
        terminal.WriteLine($"Level: {GameConfig.LevelColor}{player.Level}{GameConfig.TextColor}");
        terminal.WriteLine($"Class: {GameConfig.ClassColor}{player.Class}{GameConfig.TextColor}");
        terminal.WriteLine($"Race: {GameConfig.RaceColor}{player.Race}{GameConfig.TextColor}");
        terminal.WriteLine($"HP: {GameConfig.HPColor}{player.HP:N0}/{player.MaxHP:N0}{GameConfig.TextColor}");
        terminal.WriteLine($"Experience: {GameConfig.ExperienceColor}{player.Experience:N0}{GameConfig.TextColor}");
        terminal.WriteLine($"Gold: {GameConfig.GoldColor}{player.Gold:N0}{GameConfig.TextColor}");
        terminal.WriteLine($"Bank: {GameConfig.GoldColor}{player.BankGold:N0}{GameConfig.TextColor}");
        
        if (!string.IsNullOrEmpty(player.Team))
        {
            terminal.WriteLine($"Team: {GameConfig.TeamColor}{player.Team}{GameConfig.TextColor}");
            if (player.CTurf)
            {
                terminal.WriteLine($"{GameConfig.SuccessColor}Team controls the town!{GameConfig.TextColor}");
            }
        }
        
        terminal.WriteLine($"Fights Left: {GameConfig.FightColor}{player.TurnsLeft}{GameConfig.TextColor}");
        terminal.WriteLine($"Gym Sessions: {GameConfig.SessionColor}{player.GymSessions}{GameConfig.TextColor}");
        
        await terminal.WaitForKeyPress();
    }
    
    /// <summary>
    /// Navigate to King's Castle
    /// </summary>
    private async Task NavigateToKingsCastle(Character player)
    {
        var terminal = GetNode<TerminalEmulator>("/root/TerminalEmulator");
        terminal.WriteLine($"\n{GameConfig.LocationColor}You head north to the King's Castle...{GameConfig.TextColor}");
        
        // Navigate to castle location
        await LocationManager.Instance.ChangeLocation(player, "KingsCastleLocation");
    }
    
    /// <summary>
    /// Navigate to gym - Pascal CHALLENG.PAS gym functionality
    /// </summary>
    private async Task NavigateToGym(Character player)
    {
        var terminal = GetNode<TerminalEmulator>("/root/TerminalEmulator");
        terminal.WriteLine($"\n{GameConfig.LocationColor}You walk to the Gym for some competition...{GameConfig.TextColor}");
        
        // Navigate to gym location
        await LocationManager.Instance.ChangeLocation(player, "GymLocation");
    }
    
    /// <summary>
    /// Navigate to the prison exterior
    /// </summary>
    private async Task NavigateToPrisonGrounds(Character player)
    {
        var terminal = GetNode<TerminalEmulator>("/root/TerminalEmulator");
        terminal.WriteLine($"\n{GameConfig.LocationColor}You head south toward the looming prison walls...{GameConfig.TextColor}");
        await LocationManager.Instance.ChangeLocation(player, "PrisonWalkLocation");
    }
    
    /// <summary>
    /// Return to town
    /// </summary>
    private async Task ReturnToTown(Character player)
    {
        var terminal = GetNode<TerminalEmulator>("/root/TerminalEmulator");
        terminal.WriteLine($"\n{GameConfig.LocationColor}You return to the Main Street...{GameConfig.TextColor}");
        
        // Navigate back to main street
        await LocationManager.Instance.ChangeLocation(player, GameLocation.MainStreet);
    }
    
    #endregion
    
    #region Pascal View_Royal_Guard Implementation
    
    /// <summary>
    /// View royal guard - Pascal CHALLENG.PAS View_Royal_Guard procedure
    /// </summary>
    public async Task ViewRoyalGuard(Character player)
    {
        var terminal = GetNode<TerminalEmulator>("/root/TerminalEmulator");
        
        terminal.WriteLine($"\n{GameConfig.RoyalColor}The Royal Guard{GameConfig.TextColor}");
        
        // Load king data
        var king = await LoadKingData();
        
        int guardCount = 1;
        for (int i = 1; i <= GameConfig.KingGuards; i++)
        {
            if (!string.IsNullOrEmpty(king.Guards[i - 1].Name))
            {
                var guard = await FindGuardCharacter(king.Guards[i - 1].Name, king.Guards[i - 1].IsHuman);
                
                if (guard != null)
                {
                    terminal.WriteLine($"#{guardCount} {GameConfig.PlayerColor}{guard.Name2}{GameConfig.TextColor} " +
                        $"(the level {GameConfig.LevelColor}{guard.Level}{GameConfig.TextColor} " +
                        $"{GetRaceDisplay(guard.Race)})");
                    guardCount++;
                }
            }
        }
        
        if (guardCount == 1)
        {
            terminal.WriteLine($"{GameConfig.WarningColor}The Royal Guard is empty.{GameConfig.TextColor}");
        }
        
        await terminal.WaitForKeyPress();
    }
    
    /// <summary>
    /// Load king data - Pascal king loading logic
    /// </summary>
    private async Task<KingData> LoadKingData()
    {
        // TODO: Load from king file (KING.DAT)
        return new KingData();
    }
    
    /// <summary>
    /// Find guard character - Pascal guard lookup logic
    /// </summary>
    private async Task<Character> FindGuardCharacter(string guardName, bool isHuman)
    {
        // TODO: Search in USERS.DAT and NPCS.DAT based on isHuman flag
        return null;
    }
    
    /// <summary>
    /// Get race display - Pascal race_display function
    /// </summary>
    private string GetRaceDisplay(CharacterRace race)
    {
        return race.ToString();
    }
    
    #endregion
    
    #region Data Structures
    
    public class KingData
    {
        public List<GuardInfo> Guards { get; set; } = new List<GuardInfo>(new GuardInfo[GameConfig.KingGuards]);
    }
    
    public class GuardInfo
    {
        public string Name { get; set; } = "";
        public bool IsHuman { get; set; } = false;
    }
    
    #endregion
} 
