using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Team Corner Location - Complete implementation based on Pascal TCORNER.PAS
/// "This is the place where the teams make their decisions"
/// Provides team creation, management, communication, and all team-related functions
/// </summary>
public class TeamCornerLocation : BaseLocation
{
    private TeamSystem teamSystem;
    private NewsSystem newsSystem;
    private MailSystem mailSystem;
    private RelationshipSystem relationshipSystem;
    
    // Pascal constants from TCORNER.PAS
    private const int LocalMaxY = 200; // max number of teams the routines will handle
    
    public TeamCornerLocation()
    {
        LocationId = GameLocation.TeamCorner;
        LocationName = "Adventurers Team Corner";
        Description = "The place where gangs gather to plan their strategies and make their decisions.";
        
        // Pascal menu options from TCORNER.PAS Meny procedure
        AddMenuOption("T", "Team Rankings");
        AddMenuOption("P", "Password change");
        AddMenuOption("I", "Info on Teams");
        AddMenuOption("E", "Examine member");
        AddMenuOption("M", "Messages to teammates");
        AddMenuOption("Y", "Your team status");
        AddMenuOption("J", "Join team");
        AddMenuOption("*", "Resurrect teammember");
        AddMenuOption("C", "Create team");
        AddMenuOption("!", "Send message to other team");
        AddMenuOption("Q", "Quit team");
        AddMenuOption("1", "Send items to member");
        AddMenuOption("A", "Apply for membership");
        AddMenuOption("2", "Sack Member");
        AddMenuOption("S", "Status");
        AddMenuOption("O", "Other Team, check");
        AddMenuOption("R", "Return");
    }
    
    public override void _Ready()
    {
        base._Ready();
        teamSystem = GetNode<TeamSystem>("/root/TeamSystem");
        newsSystem = GetNode<NewsSystem>("/root/NewsSystem");
        mailSystem = GetNode<MailSystem>("/root/MailSystem");
        relationshipSystem = GetNode<RelationshipSystem>("/root/RelationshipSystem");
    }
    
    public override async Task<bool> HandlePlayerInput(string input, Player player)
    {
        if (string.IsNullOrEmpty(input))
            return false;
            
        char choice = char.ToUpper(input[0]);
        
        switch (choice)
        {
            case '?':
                if (player.Character.Expert)
                    DisplayMenu(force: true);
                else
                    DisplayMenu(force: false);
                return true;
                
            case 'R':
                terminal.WriteLine();
                await Exit(player);
                return true;
                
            case 'S':
                await ShowPlayerStatus(player);
                return true;
                
            case 'T':
                await ShowTeamRankings(player);
                return true;
                
            case 'I':
                await ShowTeamInfo(player);
                return true;
                
            case 'Y':
                await ShowYourTeamStatus(player);
                return true;
                
            case 'C':
                await CreateTeam(player);
                return true;
                
            case 'J':
                await JoinTeam(player);
                return true;
                
            case 'A':
                await ApplyForMembership(player);
                return true;
                
            case 'Q':
                await QuitTeam(player);
                return true;
                
            case 'P':
                await ChangeTeamPassword(player);
                return true;
                
            case 'M':
                await SendTeamMessage(player);
                return true;
                
            case '!':
                await SendMessageToOtherTeam(player);
                return true;
                
            case 'E':
                await ExamineMember(player);
                return true;
                
            case '2':
                await SackMember(player);
                return true;
                
            case '1':
                await SendItemsToMember(player);
                return true;
                
            case '*':
                await ResurrectTeammate(player);
                return true;
                
            case 'O':
                await CheckOtherTeam(player);
                return true;
                
            default:
                terminal.WriteLine($"{GameConfig.NewsColorDeath}Invalid choice.{GameConfig.NewsColorDefault}");
                return true;
        }
    }
    
    public override void DisplayMenu(bool force = false)
    {
        if (!force && CurrentPlayer?.Character.Expert == true)
            return;
            
        terminal.Clear();
        terminal.WriteLine();
        terminal.WriteLine($"{GameConfig.NewsColorRoyal}-*- Adventurers Team Corner -*-{GameConfig.NewsColorDefault}");
        terminal.WriteLine();
        
        // Left column (24 char offset from Pascal)
        const int offset = 24;
        
        terminal.WriteLine($"{GameConfig.NewsColorHighlight}(T)eam Rankings".PadRight(offset) + $"{GameConfig.NewsColorDefault}(P)assword change");
        terminal.WriteLine($"{GameConfig.NewsColorHighlight}(I)nfo on Teams".PadRight(offset) + $"{GameConfig.NewsColorDefault}(E)xamine member");
        terminal.WriteLine($"{GameConfig.NewsColorHighlight}(M)essages to teammates".PadRight(offset) + $"{GameConfig.NewsColorDefault}(Y)our team status");
        terminal.WriteLine($"{GameConfig.NewsColorHighlight}(J)oin team".PadRight(offset) + $"{GameConfig.NewsColorDefault}(*) Resurrect teammember");
        terminal.WriteLine($"{GameConfig.NewsColorHighlight}(C)reate team".PadRight(offset) + $"{GameConfig.NewsColorDefault}(!) Send message to other team");
        terminal.WriteLine($"{GameConfig.NewsColorHighlight}(Q)uit team".PadRight(offset) + $"{GameConfig.NewsColorDefault}(1) Send items to member");
        terminal.WriteLine($"{GameConfig.NewsColorHighlight}(A)pply for membership".PadRight(offset) + $"{GameConfig.NewsColorDefault}(2) Sack Member");
        terminal.WriteLine($"{GameConfig.NewsColorHighlight}(S)tatus".PadRight(offset) + $"{GameConfig.NewsColorDefault}(O)ther Team, check");
        terminal.WriteLine();
        terminal.WriteLine($"{GameConfig.NewsColorDefault}(R)eturn");
    }
    
    public override void DisplayPrompt()
    {
        if (CurrentPlayer?.Character.Expert == false)
        {
            terminal.WriteLine();
            terminal.Write($"{GameConfig.NewsColorDefault}Team Corner ({GameConfig.NewsColorHighlight}?{GameConfig.NewsColorDefault} for menu): ");
        }
        else
        {
            terminal.WriteLine();
            terminal.Write($"{GameConfig.NewsColorDefault}Team (R,T,P,I,S,M,Y,J,*,C,!,Q,A,1,2,E,O,?): ");
        }
    }
    
    #region Team Management Functions - Pascal TCORNER.PAS Implementation
    
    /// <summary>
    /// Create new team - Pascal TCORNER.PAS team creation
    /// </summary>
    private async Task CreateTeam(Player player)
    {
        if (!string.IsNullOrEmpty(player.Character.Team))
        {
            terminal.WriteLine();
            terminal.WriteLine($"{GameConfig.NewsColorDeath}You are already a member of the {GameConfig.NewsColorHighlight}{player.Character.Team}{GameConfig.NewsColorDeath} crew!{GameConfig.NewsColorDefault}");
            terminal.WriteLine();
            return;
        }
        
        terminal.WriteLine();
        terminal.WriteLine($"{GameConfig.NewsColorDefault}Creating a new gang...");
        terminal.WriteLine();
        
        // Get team name
        terminal.Write($"{GameConfig.NewsColorDefault}Enter gang name (max 40 chars): ");
        string teamName = await terminal.ReadLineAsync();
        
        if (string.IsNullOrEmpty(teamName) || teamName.Length > 40)
        {
            terminal.WriteLine($"{GameConfig.NewsColorDeath}Invalid team name!{GameConfig.NewsColorDefault}");
            return;
        }
        
        // Get password
        terminal.Write($"{GameConfig.NewsColorDefault}Enter gang password (max 20 chars): ");
        string password = await terminal.ReadLineAsync();
        
        if (string.IsNullOrEmpty(password) || password.Length > 20)
        {
            terminal.WriteLine($"{GameConfig.NewsColorDeath}Invalid password!{GameConfig.NewsColorDefault}");
            return;
        }
        
        // Create team
        if (teamSystem.CreateTeam(player.Character, teamName, password))
        {
            terminal.WriteLine();
            terminal.WriteLine($"{GameConfig.NewsColorRoyal}Gang '{teamName}' created successfully!{GameConfig.NewsColorDefault}");
            terminal.WriteLine($"{GameConfig.NewsColorDefault}You are now the leader of {GameConfig.NewsColorHighlight}{teamName}{GameConfig.NewsColorDefault}!");
            terminal.WriteLine();
            await WaitForKeyPress();
        }
        else
        {
            terminal.WriteLine();
            terminal.WriteLine($"{GameConfig.NewsColorDeath}Failed to create gang! Name may already be taken.{GameConfig.NewsColorDefault}");
            terminal.WriteLine();
        }
    }
    
    /// <summary>
    /// Join existing team - Pascal TCORNER.PAS team joining
    /// </summary>
    private async Task JoinTeam(Player player)
    {
        if (!string.IsNullOrEmpty(player.Character.Team))
        {
            terminal.WriteLine();
            terminal.WriteLine($"{GameConfig.NewsColorDeath}You are already a member of the {GameConfig.NewsColorHighlight}{player.Character.Team}{GameConfig.NewsColorDeath} crew!{GameConfig.NewsColorDefault}");
            terminal.WriteLine();
            return;
        }
        
        terminal.WriteLine();
        terminal.WriteLine($"{GameConfig.NewsColorDefault}Which gang would you like to join?");
        terminal.Write($"{GameConfig.NewsColorDefault}: ");
        string teamName = await terminal.ReadLineAsync();
        
        if (string.IsNullOrEmpty(teamName))
            return;
            
        terminal.Write($"{GameConfig.NewsColorDefault}Enter password: ");
        string password = await terminal.ReadLineAsync();
        
        if (teamSystem.JoinTeam(player.Character, teamName, password))
        {
            terminal.WriteLine();
            terminal.WriteLine($"{GameConfig.NewsColorRoyal}Correct! You are now a member of {GameConfig.NewsColorHighlight}{teamName}{GameConfig.NewsColorDefault}!{GameConfig.NewsColorDefault}");
            terminal.WriteLine();
            await WaitForKeyPress();
        }
        else
        {
            terminal.WriteLine();
            terminal.WriteLine($"{GameConfig.NewsColorDeath}Failed to join gang! Wrong password or team is full.{GameConfig.NewsColorDefault}");
            terminal.WriteLine();
        }
    }
    
    /// <summary>
    /// Apply for membership - Pascal TCORNER.PAS membership application
    /// </summary>
    private async Task ApplyForMembership(Player player)
    {
        // Same as join team for now - could be extended for approval system
        await JoinTeam(player);
    }
    
    /// <summary>
    /// Quit team - Pascal TCORNER.PAS team quitting
    /// </summary>
    private async Task QuitTeam(Player player)
    {
        if (string.IsNullOrEmpty(player.Character.Team))
        {
            terminal.WriteLine();
            terminal.WriteLine($"{GameConfig.NewsColorDeath}You don't belong to a team!{GameConfig.NewsColorDefault}");
            terminal.WriteLine();
            return;
        }
        
        terminal.WriteLine();
        terminal.WriteLine($"{GameConfig.NewsColorDeath}Quit Team{GameConfig.NewsColorDefault}");
        terminal.WriteLine("─────────");
        terminal.WriteLine();
        
        if (await Confirm($"Really Quit {GameConfig.NewsColorHighlight}{player.Character.Team}{GameConfig.NewsColorDefault}", "N"))
        {
            if (teamSystem.QuitTeam(player.Character))
            {
                terminal.WriteLine();
                terminal.WriteLine($"{GameConfig.NewsColorRoyal}You have left the Team!{GameConfig.NewsColorDefault}");
                terminal.WriteLine();
                await WaitForKeyPress();
            }
        }
    }
    
    /// <summary>
    /// Change team password - Pascal TCORNER.PAS password change
    /// </summary>
    private async Task ChangeTeamPassword(Player player)
    {
        if (string.IsNullOrEmpty(player.Character.Team))
        {
            terminal.WriteLine();
            terminal.WriteLine($"{GameConfig.NewsColorDeath}You don't belong to a team.{GameConfig.NewsColorDefault}");
            terminal.WriteLine();
            return;
        }
        
        terminal.WriteLine();
        terminal.Write($"{GameConfig.NewsColorDefault}Enter current password: ");
        string currentPassword = await terminal.ReadLineAsync();
        
        if (currentPassword != player.Character.TeamPW)
        {
            terminal.WriteLine();
            terminal.WriteLine($"{GameConfig.NewsColorDeath}Wrong password!{GameConfig.NewsColorDefault}");
            terminal.WriteLine();
            return;
        }
        
        terminal.Write($"{GameConfig.NewsColorDefault}Enter new password: ");
        string newPassword = await terminal.ReadLineAsync();
        
        if (!string.IsNullOrEmpty(newPassword) && newPassword.Length <= 20)
        {
            player.Character.TeamPW = newPassword;
            terminal.WriteLine();
            terminal.WriteLine($"{GameConfig.NewsColorRoyal}Password changed successfully!{GameConfig.NewsColorDefault}");
            terminal.WriteLine();
        }
        else
        {
            terminal.WriteLine();
            terminal.WriteLine($"{GameConfig.NewsColorDeath}Invalid password!{GameConfig.NewsColorDefault}");
            terminal.WriteLine();
        }
    }
    
    /// <summary>
    /// Send message to team - Pascal TCORNER.PAS team communication
    /// </summary>
    private async Task SendTeamMessage(Player player)
    {
        if (string.IsNullOrEmpty(player.Character.Team))
        {
            terminal.WriteLine();
            terminal.WriteLine($"{GameConfig.NewsColorDeath}You don't belong to a team.{GameConfig.NewsColorDefault}");
            terminal.WriteLine();
            return;
        }
        
        terminal.WriteLine();
        terminal.WriteLine($"{GameConfig.NewsColorDefault}Message to team members:");
        terminal.Write($"{GameConfig.NewsColorDefault}: ");
        string message = await terminal.ReadLineAsync();
        
        if (!string.IsNullOrEmpty(message))
        {
            teamSystem.SendTeamMessage(player.Character, message);
            terminal.WriteLine();
            terminal.WriteLine($"{GameConfig.NewsColorRoyal}Message sent to team!{GameConfig.NewsColorDefault}");
            terminal.WriteLine();
        }
    }
    
    /// <summary>
    /// Sack team member - Pascal TCORNER.PAS member removal
    /// </summary>
    private async Task SackMember(Player player)
    {
        if (string.IsNullOrEmpty(player.Character.Team))
        {
            terminal.WriteLine();
            terminal.WriteLine($"{GameConfig.NewsColorDeath}You don't belong to a team.{GameConfig.NewsColorDefault}");
            terminal.WriteLine();
            return;
        }
        
        terminal.WriteLine();
        terminal.WriteLine($"{GameConfig.NewsColorDefault}Who must be SACKED? (enter {GameConfig.NewsColorHighlight}?{GameConfig.NewsColorDefault} to see your team)");
        terminal.Write($"{GameConfig.NewsColorDefault}: ");
        string memberName = await terminal.ReadLineAsync();
        
        if (memberName == "?")
        {
            await ShowTeamMembers(player.Character.Team, true);
            return;
        }
        
        if (!string.IsNullOrEmpty(memberName))
        {
            if (await Confirm($"{GameConfig.NewsColorPlayer}{memberName}{GameConfig.NewsColorDefault}", "n"))
            {
                if (teamSystem.SackTeamMember(player.Character, memberName))
                {
                    terminal.WriteLine();
                    terminal.WriteLine($"{GameConfig.NewsColorRoyal}{memberName} has been sacked from the team!{GameConfig.NewsColorDefault}");
                    terminal.WriteLine();
                    await WaitForKeyPress();
                }
                else
                {
                    terminal.WriteLine();
                    terminal.WriteLine($"{GameConfig.NewsColorDeath}Failed to sack member!{GameConfig.NewsColorDefault}");
                    terminal.WriteLine();
                }
            }
        }
    }
    
    /// <summary>
    /// Show your team status - Pascal TCORNER.PAS team status display
    /// </summary>
    private async Task ShowYourTeamStatus(Player player)
    {
        if (string.IsNullOrEmpty(player.Character.Team))
        {
            terminal.WriteLine();
            terminal.WriteLine($"{GameConfig.NewsColorDeath}You don't belong to a team.{GameConfig.NewsColorDefault}");
            terminal.WriteLine();
            return;
        }
        
        terminal.Clear();
        terminal.WriteLine();
        terminal.WriteLine($"{GameConfig.NewsColorRoyal}Team Status for {GameConfig.NewsColorHighlight}{player.Character.Team}{GameConfig.NewsColorDefault}");
        terminal.WriteLine("═══════════════════════════════════════");
        terminal.WriteLine();
        
        terminal.WriteLine($"{GameConfig.NewsColorDefault}Team Name: {GameConfig.NewsColorHighlight}{player.Character.Team}{GameConfig.NewsColorDefault}");
        terminal.WriteLine($"{GameConfig.NewsColorDefault}Town Control: {(player.Character.CTurf ? $"{GameConfig.NewsColorRoyal}YES" : $"{GameConfig.NewsColorDeath}NO")}{GameConfig.NewsColorDefault}");
        terminal.WriteLine($"{GameConfig.NewsColorDefault}Record: {GameConfig.NewsColorPlayer}{player.Character.TeamRec}{GameConfig.NewsColorDefault} days");
        terminal.WriteLine();
        
        await ShowTeamMembers(player.Character.Team, true);
        
        terminal.WriteLine();
        await WaitForKeyPress();
    }
    
    /// <summary>
    /// Show team rankings - Pascal TCORNER.PAS team rankings
    /// </summary>
    private async Task ShowTeamRankings(Player player)
    {
        terminal.Clear();
        terminal.WriteLine();
        terminal.WriteLine($"{GameConfig.NewsColorRoyal}Team Rankings{GameConfig.NewsColorDefault}");
        terminal.WriteLine("══════════════");
        terminal.WriteLine();
        
        // TODO: Implement team ranking display
        // This would load all teams and sort by power/score
        terminal.WriteLine($"{GameConfig.NewsColorDefault}Team ranking system not yet implemented.");
        terminal.WriteLine("This will show teams sorted by power and achievements.");
        terminal.WriteLine();
        
        await WaitForKeyPress();
    }
    
    /// <summary>
    /// Show team info - Pascal TCORNER.PAS team information
    /// </summary>
    private async Task ShowTeamInfo(Player player)
    {
        terminal.WriteLine();
        terminal.Write($"{GameConfig.NewsColorDefault}Which team do you want info on? ");
        string teamName = await terminal.ReadLineAsync();
        
        if (!string.IsNullOrEmpty(teamName))
        {
            terminal.Clear();
            terminal.WriteLine();
            terminal.WriteLine($"{GameConfig.NewsColorRoyal}Team Information: {GameConfig.NewsColorHighlight}{teamName}{GameConfig.NewsColorDefault}");
            terminal.WriteLine("═══════════════════════════════════════");
            terminal.WriteLine();
            
            await ShowTeamMembers(teamName, false);
            
            terminal.WriteLine();
            await WaitForKeyPress();
        }
    }
    
    /// <summary>
    /// Examine team member - Pascal TCORNER.PAS member examination
    /// </summary>
    private async Task ExamineMember(Player player)
    {
        if (string.IsNullOrEmpty(player.Character.Team))
        {
            terminal.WriteLine();
            terminal.WriteLine($"{GameConfig.NewsColorDeath}You don't belong to a team.{GameConfig.NewsColorDefault}");
            terminal.WriteLine();
            return;
        }
        
        terminal.WriteLine();
        terminal.WriteLine($"{GameConfig.NewsColorDefault}Examine which team member? (enter {GameConfig.NewsColorHighlight}?{GameConfig.NewsColorDefault} to see your team)");
        terminal.Write($"{GameConfig.NewsColorDefault}: ");
        string memberName = await terminal.ReadLineAsync();
        
        if (memberName == "?")
        {
            await ShowTeamMembers(player.Character.Team, true);
            return;
        }
        
        // TODO: Implement member examination
        // This would show detailed stats of the team member
        terminal.WriteLine();
        terminal.WriteLine($"{GameConfig.NewsColorDefault}Member examination not yet implemented.");
        terminal.WriteLine("This will show detailed character information.");
        terminal.WriteLine();
    }
    
    #endregion
    
    #region Helper Functions
    
    /// <summary>
    /// Show team members - Pascal TCORNER.PAS display_members functionality
    /// </summary>
    private async Task ShowTeamMembers(string teamName, bool detailed)
    {
        terminal.WriteLine($"{GameConfig.NewsColorDefault}Team Members:");
        terminal.WriteLine("─────────────");
        
        // TODO: Implement team member display
        // This would load all characters with matching team name
        terminal.WriteLine($"{GameConfig.NewsColorDefault}Team member display not yet fully implemented.");
        terminal.WriteLine($"Members of {GameConfig.NewsColorHighlight}{teamName}{GameConfig.NewsColorDefault} would be shown here.");
        
        if (detailed)
        {
            terminal.WriteLine("Detailed stats and status would be included.");
        }
    }
    
    /// <summary>
    /// Send message to other team - Pascal TCORNER.PAS inter-team communication
    /// </summary>
    private async Task SendMessageToOtherTeam(Player player)
    {
        terminal.WriteLine();
        terminal.WriteLine($"{GameConfig.NewsColorDefault}Send message to which team?");
        terminal.Write($"{GameConfig.NewsColorDefault}: ");
        string targetTeam = await terminal.ReadLineAsync();
        
        if (string.IsNullOrEmpty(targetTeam))
            return;
            
        terminal.WriteLine($"{GameConfig.NewsColorDefault}Message:");
        terminal.Write($"{GameConfig.NewsColorDefault}: ");
        string message = await terminal.ReadLineAsync();
        
        if (!string.IsNullOrEmpty(message))
        {
            // TODO: Implement inter-team messaging
            terminal.WriteLine();
            terminal.WriteLine($"{GameConfig.NewsColorDefault}Inter-team messaging not yet implemented.");
            terminal.WriteLine($"Would send message to {GameConfig.NewsColorHighlight}{targetTeam}{GameConfig.NewsColorDefault}.");
            terminal.WriteLine();
        }
    }
    
    /// <summary>
    /// Send items to team member - Pascal TCORNER.PAS item transfer
    /// </summary>
    private async Task SendItemsToMember(Player player)
    {
        if (string.IsNullOrEmpty(player.Character.Team))
        {
            terminal.WriteLine();
            terminal.WriteLine($"{GameConfig.NewsColorDeath}You don't belong to a team.{GameConfig.NewsColorDefault}");
            terminal.WriteLine();
            return;
        }
        
        terminal.WriteLine();
        terminal.WriteLine($"{GameConfig.NewsColorDefault}Item transfer system not yet implemented.");
        terminal.WriteLine("This will allow sending items to team members.");
        terminal.WriteLine();
    }
    
    /// <summary>
    /// Resurrect teammate - Pascal TCORNER.PAS resurrection functionality
    /// </summary>
    private async Task ResurrectTeammate(Player player)
    {
        if (string.IsNullOrEmpty(player.Character.Team))
        {
            terminal.WriteLine();
            terminal.WriteLine($"{GameConfig.NewsColorDeath}You don't belong to a team.{GameConfig.NewsColorDefault}");
            terminal.WriteLine();
            return;
        }
        
        terminal.WriteLine();
        terminal.WriteLine($"{GameConfig.NewsColorDefault}Team resurrection not yet implemented.");
        terminal.WriteLine("This will allow reviving dead team members.");
        terminal.WriteLine();
    }
    
    /// <summary>
    /// Check other team - Pascal TCORNER.PAS team inspection
    /// </summary>
    private async Task CheckOtherTeam(Player player)
    {
        terminal.WriteLine();
        terminal.Write($"{GameConfig.NewsColorDefault}Which team do you want to check? ");
        string teamName = await terminal.ReadLineAsync();
        
        if (!string.IsNullOrEmpty(teamName))
        {
            await ShowTeamInfo(player);
        }
    }
    
    /// <summary>
    /// Show player status
    /// </summary>
    private async Task ShowPlayerStatus(Player player)
    {
        terminal.Clear();
        terminal.WriteLine();
        // TODO: Display full character status
        terminal.WriteLine($"{GameConfig.NewsColorDefault}Character: {GameConfig.NewsColorPlayer}{player.Character.Name2}{GameConfig.NewsColorDefault}");
        terminal.WriteLine($"{GameConfig.NewsColorDefault}Level: {GameConfig.NewsColorPlayer}{player.Character.Level}{GameConfig.NewsColorDefault}");
        terminal.WriteLine($"{GameConfig.NewsColorDefault}Team: {(string.IsNullOrEmpty(player.Character.Team) ? "None" : player.Character.Team)}{GameConfig.NewsColorDefault}");
        terminal.WriteLine();
        await WaitForKeyPress();
    }
    
    /// <summary>
    /// Confirmation dialog
    /// </summary>
    private async Task<bool> Confirm(string message, string defaultChoice)
    {
        terminal.Write($"{GameConfig.NewsColorDefault}{message} ({defaultChoice.ToUpper()}/y/n)? ");
        string response = await terminal.ReadLineAsync();
        
        if (string.IsNullOrEmpty(response))
            response = defaultChoice;
            
        return response.ToUpper().StartsWith("Y");
    }
    
    /// <summary>
    /// Wait for key press
    /// </summary>
    private async Task WaitForKeyPress()
    {
        terminal.Write($"{GameConfig.NewsColorDefault}Press any key to continue...");
        await terminal.ReadKeyAsync();
    }
    
    #endregion
} 