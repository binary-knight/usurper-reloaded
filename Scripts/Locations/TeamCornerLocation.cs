using UsurperRemake.Utils;
using UsurperRemake.Systems;
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
    // Pascal constants from TCORNER.PAS
    private const int LocalMaxY = 200; // max number of teams the routines will handle
    private const int MaxTeamSize = 5; // Maximum members per team

    public TeamCornerLocation() : base(
        GameLocation.TeamCorner,
        "Adventurers Team Corner",
        "The place where gangs gather to plan their strategies and make their decisions."
    ) { }

    protected override void SetupLocation()
    {
        // Pascal-compatible exits
        PossibleExits = new List<GameLocation>
        {
            GameLocation.TheInn  // Can return to the Inn
        };

        // Team Corner actions
        LocationActions = new List<string>
        {
            "Team Rankings",
            "Info on Teams",
            "Your Team Status",
            "Create Team",
            "Join Team",
            "Quit Team",
            "Recruit NPC",
            "Examine Member",
            "Password Change",
            "Send Team Message"
        };
    }

    protected override void DisplayLocation()
    {
        terminal.ClearScreen();

        // Header
        terminal.SetColor("bright_magenta");
        terminal.WriteLine("╔══════════════════════════════════════════════════════════════════════════════╗");
        terminal.WriteLine("║                         ADVENTURERS TEAM CORNER                             ║");
        terminal.WriteLine("║                    'Where gangs forge their destiny'                        ║");
        terminal.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝");
        terminal.WriteLine("");

        // Atmospheric description
        terminal.SetColor("white");
        terminal.WriteLine("A smoky back room filled with rough-hewn tables. Team banners hang from the");
        terminal.WriteLine("rafters, and the walls are covered with bounties, challenges, and team records.");
        terminal.WriteLine("");

        // Show player's team status
        if (!string.IsNullOrEmpty(currentPlayer.Team))
        {
            terminal.SetColor("bright_cyan");
            terminal.WriteLine($"Your Team: {currentPlayer.Team}");
            terminal.WriteLine($"Turf Control: {(currentPlayer.CTurf ? "YES - You own this town!" : "No")}");
            terminal.WriteLine("");
        }
        else
        {
            terminal.SetColor("yellow");
            terminal.WriteLine("You are not a member of any team. Create or join one to gain power!");
            terminal.WriteLine("");
        }

        // Menu options
        terminal.SetColor("cyan");
        terminal.WriteLine("Team Information:");
        terminal.SetColor("white");
        WriteMenuOption("T", "Team Rankings", "P", "Password Change");
        WriteMenuOption("I", "Info on Teams", "E", "Examine Member");
        WriteMenuOption("Y", "Your Team Status", "", "");
        terminal.WriteLine("");

        terminal.SetColor("cyan");
        terminal.WriteLine("Team Actions:");
        terminal.SetColor("white");
        WriteMenuOption("C", "Create Team", "J", "Join Team");
        WriteMenuOption("Q", "Quit Team", "A", "Apply for Membership");
        WriteMenuOption("N", "Recruit NPC", "2", "Sack Member");
        terminal.WriteLine("");

        terminal.SetColor("cyan");
        terminal.WriteLine("Communication:");
        terminal.SetColor("white");
        WriteMenuOption("M", "Message Teammates", "!", "Resurrect Teammate");
        terminal.WriteLine("");

        terminal.SetColor("yellow");
        terminal.WriteLine("Navigation:");
        terminal.SetColor("white");
        WriteMenuOption("R", "Return to Inn", "S", "Status");
        terminal.WriteLine("");
    }

    private void WriteMenuOption(string key1, string label1, string key2, string label2)
    {
        if (!string.IsNullOrEmpty(key1))
        {
            terminal.SetColor("darkgray");
            terminal.Write("[");
            terminal.SetColor("bright_cyan");
            terminal.Write(key1);
            terminal.SetColor("darkgray");
            terminal.Write("] ");
            terminal.SetColor("white");
            terminal.Write(label1.PadRight(25));
        }

        if (!string.IsNullOrEmpty(key2))
        {
            terminal.SetColor("darkgray");
            terminal.Write("[");
            terminal.SetColor("bright_cyan");
            terminal.Write(key2);
            terminal.SetColor("darkgray");
            terminal.Write("] ");
            terminal.SetColor("white");
            terminal.Write(label2);
        }
        terminal.WriteLine("");
    }

    protected override async Task<bool> ProcessChoice(string choice)
    {
        // Handle global quick commands first
        var (handled, shouldExit) = await TryProcessGlobalCommand(choice);
        if (handled) return shouldExit;

        if (string.IsNullOrWhiteSpace(choice))
            return false;

        var upperChoice = choice.ToUpper().Trim();

        switch (upperChoice)
        {
            case "T":
                await ShowTeamRankings();
                return false;

            case "I":
                await ShowTeamInfo();
                return false;

            case "Y":
                await ShowYourTeamStatus();
                return false;

            case "C":
                await CreateTeam();
                return false;

            case "J":
                await JoinTeam();
                return false;

            case "A":
                await JoinTeam(); // Apply is same as join for now
                return false;

            case "Q":
                await QuitTeam();
                return false;

            case "N":
                await RecruitNPCToTeam();
                return false;

            case "E":
                await ExamineMember();
                return false;

            case "P":
                await ChangeTeamPassword();
                return false;

            case "M":
                await SendTeamMessage();
                return false;

            case "2":
                await SackMember();
                return false;

            case "!":
                await ResurrectTeammate();
                return false;

            case "R":
                await NavigateToLocation(GameLocation.TheInn);
                return true;

            case "S":
                await ShowStatus();
                return false;

            case "?":
                // Menu is already displayed
                return false;

            default:
                terminal.WriteLine("Invalid choice! The gang leader shakes his head.", "red");
                await Task.Delay(1500);
                return false;
        }
    }

    #region Team Management Functions

    /// <summary>
    /// Show team rankings - all teams sorted by power
    /// </summary>
    private async Task ShowTeamRankings()
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_magenta");
        terminal.WriteLine("╔══════════════════════════════════════════════════════════════════════════════╗");
        terminal.WriteLine("║                             TEAM RANKINGS                                   ║");
        terminal.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝");
        terminal.WriteLine("");

        // Get all teams from NPCs
        var allNPCs = NPCSpawnSystem.Instance.ActiveNPCs;
        var teamGroups = allNPCs
            .Where(n => !string.IsNullOrEmpty(n.Team) && n.IsAlive)
            .GroupBy(n => n.Team)
            .Select(g => new
            {
                TeamName = g.Key,
                MemberCount = g.Count(),
                TotalPower = g.Sum(m => m.Level + (int)m.Strength + (int)m.Defence),
                AverageLevel = (int)g.Average(m => m.Level),
                ControlsTurf = g.Any(m => m.CTurf)
            })
            .OrderByDescending(t => t.TotalPower)
            .ToList();

        // Add player's team if exists and has no NPC members yet
        if (!string.IsNullOrEmpty(currentPlayer.Team))
        {
            var playerTeamExists = teamGroups.Any(t => t.TeamName == currentPlayer.Team);
            if (!playerTeamExists)
            {
                terminal.SetColor("bright_cyan");
                terminal.WriteLine($"Your Team: {currentPlayer.Team}");
                var playerPower = currentPlayer.Level + (int)currentPlayer.Strength + (int)currentPlayer.Defence;
                terminal.WriteLine($"  Members: 1 (just you), Power: {playerPower}, Turf: {(currentPlayer.CTurf ? "Yes" : "No")}");
                terminal.WriteLine("");
            }
        }

        if (teamGroups.Count == 0 && string.IsNullOrEmpty(currentPlayer.Team))
        {
            terminal.SetColor("yellow");
            terminal.WriteLine("No teams have been formed yet.");
            terminal.WriteLine("Be the first to create a team!");
        }
        else
        {
            terminal.SetColor("white");
            terminal.WriteLine($"{"Rank",-5} {"Team Name",-24} {"Mbrs",-6} {"Power",-8} {"Avg Lvl",-8} {"Turf",-5}");
            terminal.SetColor("darkgray");
            terminal.WriteLine(new string('─', 60));

            int rank = 1;
            foreach (var team in teamGroups)
            {
                if (team.ControlsTurf)
                    terminal.SetColor("bright_yellow");
                else if (team.TeamName == currentPlayer.Team)
                    terminal.SetColor("bright_cyan");
                else
                    terminal.SetColor("white");

                string turfMark = team.ControlsTurf ? "*" : "-";
                terminal.WriteLine($"{rank,-5} {team.TeamName,-24} {team.MemberCount,-6} {team.TotalPower,-8} {team.AverageLevel,-8} {turfMark,-5}");
                rank++;
            }

            terminal.WriteLine("");
            terminal.SetColor("bright_yellow");
            terminal.WriteLine("* = Controls the town turf");
        }

        terminal.WriteLine("");
        terminal.SetColor("darkgray");
        terminal.WriteLine("Press any key to continue...");
        await terminal.ReadKeyAsync();
    }

    /// <summary>
    /// Show info on a specific team
    /// </summary>
    private async Task ShowTeamInfo()
    {
        terminal.WriteLine("");
        terminal.SetColor("cyan");
        terminal.Write("Which team do you want info on? ");
        terminal.SetColor("white");
        string teamName = await terminal.ReadLineAsync();

        if (string.IsNullOrEmpty(teamName))
            return;

        terminal.ClearScreen();
        terminal.SetColor("bright_cyan");
        terminal.WriteLine($"Team Information: {teamName}");
        terminal.SetColor("darkgray");
        terminal.WriteLine(new string('═', 50));
        terminal.WriteLine("");

        await ShowTeamMembers(teamName, false);

        terminal.WriteLine("");
        terminal.SetColor("darkgray");
        terminal.WriteLine("Press any key to continue...");
        await terminal.ReadKeyAsync();
    }

    /// <summary>
    /// Show your team's status
    /// </summary>
    private async Task ShowYourTeamStatus()
    {
        if (string.IsNullOrEmpty(currentPlayer.Team))
        {
            terminal.WriteLine("");
            terminal.SetColor("red");
            terminal.WriteLine("You don't belong to a team.");
            terminal.WriteLine("");
            await Task.Delay(2000);
            return;
        }

        terminal.ClearScreen();
        terminal.SetColor("bright_cyan");
        terminal.WriteLine($"Team Status: {currentPlayer.Team}");
        terminal.SetColor("darkgray");
        terminal.WriteLine(new string('═', 50));
        terminal.WriteLine("");

        terminal.SetColor("white");
        terminal.WriteLine($"Team Name: {currentPlayer.Team}");

        if (currentPlayer.CTurf)
        {
            terminal.SetColor("bright_yellow");
            terminal.WriteLine("Town Control: YES - Your team owns this town!");
        }
        else
        {
            terminal.SetColor("red");
            terminal.WriteLine("Town Control: NO");
        }

        terminal.SetColor("white");
        terminal.WriteLine($"Team Record: {currentPlayer.TeamRec} days");
        terminal.WriteLine("");

        await ShowTeamMembers(currentPlayer.Team, true);

        terminal.WriteLine("");
        terminal.SetColor("darkgray");
        terminal.WriteLine("Press any key to continue...");
        await terminal.ReadKeyAsync();
    }

    /// <summary>
    /// Show members of a team
    /// </summary>
    private async Task ShowTeamMembers(string teamName, bool detailed)
    {
        terminal.SetColor("cyan");
        terminal.WriteLine("Team Members:");
        terminal.SetColor("darkgray");
        terminal.WriteLine("─────────────");

        // Get NPCs in this team
        var allNPCs = NPCSpawnSystem.Instance.ActiveNPCs;
        var teamMembers = allNPCs
            .Where(n => n.Team == teamName)
            .OrderByDescending(n => n.Level)
            .ToList();

        if (teamMembers.Count == 0)
        {
            terminal.SetColor("yellow");
            terminal.WriteLine($"No NPC members found in team '{teamName}'.");
            if (currentPlayer.Team == teamName)
            {
                terminal.WriteLine("(You are the only member!)");
            }
            return;
        }

        if (detailed)
        {
            terminal.SetColor("white");
            terminal.WriteLine($"{"Name",-20} {"Class",-12} {"Lvl",-5} {"HP",-12} {"Location",-15} {"Status",-8}");
            terminal.SetColor("darkgray");
            terminal.WriteLine(new string('─', 75));

            foreach (var member in teamMembers)
            {
                string hpDisplay = $"{member.HP}/{member.MaxHP}";
                string location = member.CurrentLocation ?? "Unknown";
                if (location.Length > 14) location = location.Substring(0, 14);

                if (member.IsAlive)
                    terminal.SetColor("white");
                else
                    terminal.SetColor("red");

                string status = member.IsAlive ? "Alive" : "Dead";
                terminal.WriteLine($"{member.DisplayName,-20} {member.Class,-12} {member.Level,-5} {hpDisplay,-12} {location,-15} {status,-8}");
            }

            terminal.WriteLine("");
            terminal.SetColor("cyan");
            terminal.WriteLine($"Total: {teamMembers.Count} NPC members, {teamMembers.Count(m => m.IsAlive)} alive");
        }
        else
        {
            foreach (var member in teamMembers)
            {
                string status = member.IsAlive ? "" : " (Dead)";
                terminal.SetColor("white");
                terminal.WriteLine($"  {member.DisplayName} - Level {member.Level} {member.Class}{status}");
            }
        }
    }

    /// <summary>
    /// Create a new team
    /// </summary>
    private async Task CreateTeam()
    {
        if (!string.IsNullOrEmpty(currentPlayer.Team))
        {
            terminal.WriteLine("");
            terminal.SetColor("red");
            terminal.WriteLine($"You are already a member of {currentPlayer.Team}!");
            terminal.WriteLine("You must quit your current team first.");
            terminal.WriteLine("");
            await Task.Delay(2000);
            return;
        }

        terminal.WriteLine("");
        terminal.SetColor("cyan");
        terminal.WriteLine("Creating a new gang...");
        terminal.WriteLine("");

        // Get team name
        terminal.SetColor("white");
        terminal.Write("Enter gang name (max 40 chars): ");
        string teamName = await terminal.ReadLineAsync();

        if (string.IsNullOrEmpty(teamName) || teamName.Length > 40)
        {
            terminal.SetColor("red");
            terminal.WriteLine("Invalid team name!");
            await Task.Delay(2000);
            return;
        }

        // Check if team name already exists
        var allNPCs = NPCSpawnSystem.Instance.ActiveNPCs;
        if (allNPCs.Any(n => n.Team == teamName))
        {
            terminal.SetColor("red");
            terminal.WriteLine("A team with that name already exists!");
            await Task.Delay(2000);
            return;
        }

        // Get password
        terminal.Write("Enter gang password (max 20 chars): ");
        string password = await terminal.ReadLineAsync();

        if (string.IsNullOrEmpty(password) || password.Length > 20)
        {
            terminal.SetColor("red");
            terminal.WriteLine("Invalid password!");
            await Task.Delay(2000);
            return;
        }

        // Create team
        currentPlayer.Team = teamName;
        currentPlayer.TeamPW = password;
        currentPlayer.CTurf = false;
        currentPlayer.TeamRec = 0;

        terminal.WriteLine("");
        terminal.SetColor("bright_green");
        terminal.WriteLine($"Gang '{teamName}' created successfully!");
        terminal.WriteLine($"You are now the leader of {teamName}!");
        terminal.WriteLine("");

        // Generate news
        NewsSystem.Instance.Newsy(true, $"{currentPlayer.DisplayName} formed a new team: '{teamName}'!");

        terminal.SetColor("darkgray");
        terminal.WriteLine("Press any key to continue...");
        await terminal.ReadKeyAsync();
    }

    /// <summary>
    /// Join an existing team
    /// </summary>
    private async Task JoinTeam()
    {
        if (!string.IsNullOrEmpty(currentPlayer.Team))
        {
            terminal.WriteLine("");
            terminal.SetColor("red");
            terminal.WriteLine($"You are already a member of {currentPlayer.Team}!");
            terminal.WriteLine("");
            await Task.Delay(2000);
            return;
        }

        terminal.WriteLine("");
        terminal.SetColor("cyan");
        terminal.Write("Which gang would you like to join? ");
        terminal.SetColor("white");
        string teamName = await terminal.ReadLineAsync();

        if (string.IsNullOrEmpty(teamName))
            return;

        // Find a team member to get the password from
        var allNPCs = NPCSpawnSystem.Instance.ActiveNPCs;
        var teamMember = allNPCs.FirstOrDefault(n => n.Team == teamName && n.IsAlive);

        if (teamMember == null)
        {
            terminal.SetColor("red");
            terminal.WriteLine("No active team found with that name!");
            await Task.Delay(2000);
            return;
        }

        terminal.SetColor("cyan");
        terminal.Write("Enter password: ");
        terminal.SetColor("white");
        string password = await terminal.ReadLineAsync();

        if (password == teamMember.TeamPW)
        {
            currentPlayer.Team = teamName;
            currentPlayer.TeamPW = password;
            currentPlayer.CTurf = teamMember.CTurf;

            terminal.WriteLine("");
            terminal.SetColor("bright_green");
            terminal.WriteLine($"Correct! You are now a member of {teamName}!");
            terminal.WriteLine("");

            NewsSystem.Instance.Newsy(true, $"{currentPlayer.DisplayName} joined the team '{teamName}'!");

            terminal.SetColor("darkgray");
            terminal.WriteLine("Press any key to continue...");
            await terminal.ReadKeyAsync();
        }
        else
        {
            terminal.WriteLine("");
            terminal.SetColor("red");
            terminal.WriteLine("Wrong password! Access denied.");
            terminal.WriteLine("");
            await Task.Delay(2000);
        }
    }

    /// <summary>
    /// Quit your current team
    /// </summary>
    private async Task QuitTeam()
    {
        if (string.IsNullOrEmpty(currentPlayer.Team))
        {
            terminal.WriteLine("");
            terminal.SetColor("red");
            terminal.WriteLine("You don't belong to a team!");
            terminal.WriteLine("");
            await Task.Delay(2000);
            return;
        }

        terminal.WriteLine("");
        terminal.SetColor("yellow");
        terminal.Write($"Really quit {currentPlayer.Team}? (Y/N): ");
        string response = await terminal.ReadLineAsync();

        if (response?.ToUpper().StartsWith("Y") == true)
        {
            string oldTeam = currentPlayer.Team;
            currentPlayer.Team = "";
            currentPlayer.TeamPW = "";
            currentPlayer.CTurf = false;
            currentPlayer.TeamRec = 0;

            terminal.WriteLine("");
            terminal.SetColor("bright_green");
            terminal.WriteLine("You have left the team!");
            terminal.WriteLine("");

            NewsSystem.Instance.Newsy(true, $"{currentPlayer.DisplayName} left the team '{oldTeam}'!");

            terminal.SetColor("darkgray");
            terminal.WriteLine("Press any key to continue...");
            await terminal.ReadKeyAsync();
        }
    }

    /// <summary>
    /// Recruit an NPC to join your team
    /// </summary>
    private async Task RecruitNPCToTeam()
    {
        if (string.IsNullOrEmpty(currentPlayer.Team))
        {
            terminal.WriteLine("");
            terminal.SetColor("red");
            terminal.WriteLine("You must be in a team to recruit members!");
            terminal.WriteLine("Create a team first with the (C)reate team option.");
            terminal.WriteLine("");
            await Task.Delay(2000);
            return;
        }

        // Count current team size
        var allNPCs = NPCSpawnSystem.Instance.ActiveNPCs;
        var currentTeamSize = allNPCs.Count(n => n.Team == currentPlayer.Team && n.IsAlive) + 1; // +1 for player

        if (currentTeamSize >= MaxTeamSize)
        {
            terminal.WriteLine("");
            terminal.SetColor("red");
            terminal.WriteLine($"Your team is full! (Max {MaxTeamSize} members)");
            terminal.WriteLine("");
            await Task.Delay(2000);
            return;
        }

        terminal.ClearScreen();
        terminal.SetColor("bright_magenta");
        terminal.WriteLine("╔══════════════════════════════════════════════════════════════════════════════╗");
        terminal.WriteLine("║                             NPC RECRUITMENT                                 ║");
        terminal.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝");
        terminal.WriteLine("");

        terminal.SetColor("white");
        terminal.WriteLine($"Team: {currentPlayer.Team}");
        terminal.WriteLine($"Current Size: {currentTeamSize}/{MaxTeamSize}");
        terminal.WriteLine("");

        // Find NPCs that are not in any team and are in town locations
        var townLocations = new[] { "Main Street", "Market", "Inn", "Temple", "Church", "Weapon Shop", "Armor Shop", "Castle", "Bank", "Team Corner" };
        var availableNPCs = allNPCs
            .Where(n => n.IsAlive &&
                   string.IsNullOrEmpty(n.Team) &&
                   townLocations.Contains(n.CurrentLocation))
            .OrderByDescending(n => n.Level)
            .Take(10)
            .ToList();

        if (availableNPCs.Count == 0)
        {
            terminal.SetColor("yellow");
            terminal.WriteLine("No NPCs available for recruitment right now.");
            terminal.WriteLine("Try again later - NPCs move around the world!");
            terminal.WriteLine("");
            terminal.SetColor("darkgray");
            terminal.WriteLine("Press any key to continue...");
            await terminal.ReadKeyAsync();
            return;
        }

        terminal.SetColor("cyan");
        terminal.WriteLine("Available Recruits:");
        terminal.SetColor("white");
        terminal.WriteLine($"{"#",-3} {"Name",-18} {"Class",-12} {"Lvl",-5} {"Location",-14} {"Cost",-10}");
        terminal.SetColor("darkgray");
        terminal.WriteLine(new string('─', 65));

        terminal.SetColor("white");
        for (int i = 0; i < availableNPCs.Count; i++)
        {
            var npc = availableNPCs[i];
            long recruitCost = CalculateRecruitmentCost(npc, currentPlayer);
            string location = npc.CurrentLocation ?? "Unknown";
            if (location.Length > 13) location = location.Substring(0, 13);

            terminal.WriteLine($"{i + 1,-3} {npc.DisplayName,-18} {npc.Class,-12} {npc.Level,-5} {location,-14} {recruitCost:N0}g");
        }

        terminal.WriteLine("");
        terminal.SetColor("bright_yellow");
        terminal.WriteLine($"Your Gold: {currentPlayer.Gold:N0}");
        terminal.WriteLine("");
        terminal.SetColor("cyan");
        terminal.Write("Enter number to recruit (0 to cancel): ");
        terminal.SetColor("white");
        string input = await terminal.ReadLineAsync();

        if (int.TryParse(input, out int choice) && choice >= 1 && choice <= availableNPCs.Count)
        {
            var recruit = availableNPCs[choice - 1];
            long cost = CalculateRecruitmentCost(recruit, currentPlayer);

            if (currentPlayer.Gold < cost)
            {
                terminal.WriteLine("");
                terminal.SetColor("red");
                terminal.WriteLine($"You don't have enough gold to recruit {recruit.DisplayName}!");
                terminal.WriteLine($"You need {cost:N0} gold, but only have {currentPlayer.Gold:N0}.");
            }
            else
            {
                // Recruitment success!
                currentPlayer.Gold -= cost;
                recruit.Team = currentPlayer.Team;
                recruit.TeamPW = currentPlayer.TeamPW;
                recruit.CTurf = currentPlayer.CTurf;

                terminal.WriteLine("");
                terminal.SetColor("bright_green");
                terminal.WriteLine($"{recruit.DisplayName} has joined your team!");
                terminal.WriteLine($"You paid {cost:N0} gold for recruitment.");
                terminal.WriteLine("");
                terminal.SetColor("bright_cyan");
                terminal.WriteLine($"\"{recruit.DisplayName} says: 'I'll fight alongside you, boss!'\"");

                NewsSystem.Instance.Newsy(true, $"{currentPlayer.DisplayName} recruited {recruit.DisplayName} into team '{currentPlayer.Team}'!");
            }
        }
        else if (choice != 0 && !string.IsNullOrEmpty(input))
        {
            terminal.SetColor("red");
            terminal.WriteLine("Invalid choice.");
        }

        terminal.WriteLine("");
        terminal.SetColor("darkgray");
        terminal.WriteLine("Press any key to continue...");
        await terminal.ReadKeyAsync();
    }

    /// <summary>
    /// Calculate the cost to recruit an NPC
    /// </summary>
    private long CalculateRecruitmentCost(NPC npc, Character recruiter)
    {
        long baseCost = npc.Level * 500;
        baseCost += ((long)npc.Strength + (long)npc.Defence + (long)npc.Agility) * 20;

        if (npc.Level > recruiter.Level)
            baseCost = (long)(baseCost * 1.5);

        if (npc.Level < recruiter.Level - 5)
            baseCost = (long)(baseCost * 0.7);

        return Math.Max(100, baseCost);
    }

    /// <summary>
    /// Examine a team member in detail
    /// </summary>
    private async Task ExamineMember()
    {
        if (string.IsNullOrEmpty(currentPlayer.Team))
        {
            terminal.WriteLine("");
            terminal.SetColor("red");
            terminal.WriteLine("You don't belong to a team.");
            terminal.WriteLine("");
            await Task.Delay(2000);
            return;
        }

        terminal.WriteLine("");
        terminal.SetColor("cyan");
        terminal.WriteLine("Examine which team member? (enter ? to see your team)");
        terminal.Write(": ");
        terminal.SetColor("white");
        string memberName = await terminal.ReadLineAsync();

        if (memberName == "?")
        {
            await ShowTeamMembers(currentPlayer.Team, true);
            terminal.SetColor("darkgray");
            terminal.WriteLine("Press any key to continue...");
            await terminal.ReadKeyAsync();
            return;
        }

        // Find the member
        var allNPCs = NPCSpawnSystem.Instance.ActiveNPCs;
        var member = allNPCs.FirstOrDefault(n =>
            n.Team == currentPlayer.Team &&
            n.DisplayName.Equals(memberName, StringComparison.OrdinalIgnoreCase));

        if (member == null)
        {
            terminal.SetColor("red");
            terminal.WriteLine($"No team member named '{memberName}' found.");
            await Task.Delay(2000);
            return;
        }

        // Show detailed stats
        terminal.ClearScreen();
        terminal.SetColor("bright_cyan");
        terminal.WriteLine($"═══════════════════════════════════════");
        terminal.WriteLine($"        {member.DisplayName.ToUpper()}");
        terminal.WriteLine($"═══════════════════════════════════════");
        terminal.WriteLine("");

        terminal.SetColor("white");
        terminal.WriteLine($"Class: {member.Class}");
        terminal.WriteLine($"Race: {member.Race}");
        terminal.WriteLine($"Level: {member.Level}");

        if (member.IsAlive)
        {
            terminal.SetColor("bright_green");
            terminal.WriteLine("Status: Alive");
        }
        else
        {
            terminal.SetColor("red");
            terminal.WriteLine("Status: Dead");
        }
        terminal.WriteLine("");

        terminal.SetColor("white");
        terminal.WriteLine($"HP: {member.HP}/{member.MaxHP}");
        terminal.WriteLine($"Mana: {member.Mana}/{member.MaxMana}");
        terminal.WriteLine($"Gold: {member.Gold:N0}");
        terminal.WriteLine("");

        terminal.WriteLine($"Strength: {member.Strength,-6} Defence: {member.Defence,-6}");
        terminal.WriteLine($"Agility:  {member.Agility,-6} Stamina: {member.Stamina,-6}");
        terminal.WriteLine($"Weapon Power: {member.WeapPow,-6} Armor Power: {member.ArmPow,-6}");
        terminal.WriteLine("");

        terminal.WriteLine($"Location: {member.CurrentLocation ?? "Unknown"}");
        terminal.WriteLine("");

        terminal.SetColor("darkgray");
        terminal.WriteLine("Press any key to continue...");
        await terminal.ReadKeyAsync();
    }

    /// <summary>
    /// Change team password
    /// </summary>
    private async Task ChangeTeamPassword()
    {
        if (string.IsNullOrEmpty(currentPlayer.Team))
        {
            terminal.WriteLine("");
            terminal.SetColor("red");
            terminal.WriteLine("You don't belong to a team.");
            terminal.WriteLine("");
            await Task.Delay(2000);
            return;
        }

        terminal.WriteLine("");
        terminal.SetColor("cyan");
        terminal.Write("Enter current password: ");
        terminal.SetColor("white");
        string currentPassword = await terminal.ReadLineAsync();

        if (currentPassword != currentPlayer.TeamPW)
        {
            terminal.WriteLine("");
            terminal.SetColor("red");
            terminal.WriteLine("Wrong password!");
            terminal.WriteLine("");
            await Task.Delay(2000);
            return;
        }

        terminal.SetColor("cyan");
        terminal.Write("Enter new password: ");
        terminal.SetColor("white");
        string newPassword = await terminal.ReadLineAsync();

        if (!string.IsNullOrEmpty(newPassword) && newPassword.Length <= 20)
        {
            string oldPassword = currentPlayer.TeamPW;
            currentPlayer.TeamPW = newPassword;

            // Update all team members' passwords
            var allNPCs = NPCSpawnSystem.Instance.ActiveNPCs;
            foreach (var npc in allNPCs.Where(n => n.Team == currentPlayer.Team))
            {
                npc.TeamPW = newPassword;
            }

            terminal.WriteLine("");
            terminal.SetColor("bright_green");
            terminal.WriteLine("Password changed successfully!");
            terminal.WriteLine("");
        }
        else
        {
            terminal.SetColor("red");
            terminal.WriteLine("Invalid password!");
        }

        await Task.Delay(2000);
    }

    /// <summary>
    /// Send message to team members
    /// </summary>
    private async Task SendTeamMessage()
    {
        if (string.IsNullOrEmpty(currentPlayer.Team))
        {
            terminal.WriteLine("");
            terminal.SetColor("red");
            terminal.WriteLine("You don't belong to a team.");
            terminal.WriteLine("");
            await Task.Delay(2000);
            return;
        }

        terminal.WriteLine("");
        terminal.SetColor("cyan");
        terminal.WriteLine("Message to team members:");
        terminal.Write(": ");
        terminal.SetColor("white");
        string message = await terminal.ReadLineAsync();

        if (!string.IsNullOrEmpty(message))
        {
            terminal.WriteLine("");
            terminal.SetColor("bright_green");
            terminal.WriteLine("Message sent to team!");
            terminal.SetColor("white");
            terminal.WriteLine($"Your message: \"{message}\"");
            terminal.WriteLine("");

            // Could integrate with mail system here
        }

        await Task.Delay(2000);
    }

    /// <summary>
    /// Sack a team member
    /// </summary>
    private async Task SackMember()
    {
        if (string.IsNullOrEmpty(currentPlayer.Team))
        {
            terminal.WriteLine("");
            terminal.SetColor("red");
            terminal.WriteLine("You don't belong to a team.");
            terminal.WriteLine("");
            await Task.Delay(2000);
            return;
        }

        terminal.WriteLine("");
        terminal.SetColor("cyan");
        terminal.WriteLine("Who must be SACKED? (enter ? to see your team)");
        terminal.Write(": ");
        terminal.SetColor("white");
        string memberName = await terminal.ReadLineAsync();

        if (memberName == "?")
        {
            await ShowTeamMembers(currentPlayer.Team, true);
            terminal.SetColor("darkgray");
            terminal.WriteLine("Press any key to continue...");
            await terminal.ReadKeyAsync();
            return;
        }

        if (!string.IsNullOrEmpty(memberName))
        {
            var allNPCs = NPCSpawnSystem.Instance.ActiveNPCs;
            var member = allNPCs.FirstOrDefault(n =>
                n.Team == currentPlayer.Team &&
                n.DisplayName.Equals(memberName, StringComparison.OrdinalIgnoreCase));

            if (member == null)
            {
                terminal.SetColor("red");
                terminal.WriteLine($"No team member named '{memberName}' found.");
                await Task.Delay(2000);
                return;
            }

            terminal.SetColor("yellow");
            terminal.Write($"Really sack {member.DisplayName}? (Y/N): ");
            string response = await terminal.ReadLineAsync();

            if (response?.ToUpper().StartsWith("Y") == true)
            {
                member.Team = "";
                member.TeamPW = "";
                member.CTurf = false;

                terminal.WriteLine("");
                terminal.SetColor("bright_green");
                terminal.WriteLine($"{member.DisplayName} has been sacked from the team!");
                terminal.WriteLine("");

                NewsSystem.Instance.Newsy(true, $"{member.DisplayName} was kicked out of team '{currentPlayer.Team}'!");

                terminal.SetColor("darkgray");
                terminal.WriteLine("Press any key to continue...");
                await terminal.ReadKeyAsync();
            }
        }
    }

    /// <summary>
    /// Resurrect a dead teammate
    /// </summary>
    private async Task ResurrectTeammate()
    {
        if (string.IsNullOrEmpty(currentPlayer.Team))
        {
            terminal.WriteLine("");
            terminal.SetColor("red");
            terminal.WriteLine("You don't belong to a team.");
            terminal.WriteLine("");
            await Task.Delay(2000);
            return;
        }

        // Find dead team members
        var allNPCs = NPCSpawnSystem.Instance.ActiveNPCs;
        var deadMembers = allNPCs
            .Where(n => n.Team == currentPlayer.Team && (n.IsDead || !n.IsAlive))
            .ToList();

        if (deadMembers.Count == 0)
        {
            terminal.WriteLine("");
            terminal.SetColor("bright_green");
            terminal.WriteLine("All your team members are alive!");
            terminal.WriteLine("");
            await Task.Delay(2000);
            return;
        }

        terminal.WriteLine("");
        terminal.SetColor("cyan");
        terminal.WriteLine("Dead Team Members:");
        for (int i = 0; i < deadMembers.Count; i++)
        {
            var dead = deadMembers[i];
            long cost = dead.Level * 1000; // Resurrection cost
            terminal.SetColor("white");
            terminal.WriteLine($"{i + 1}. {dead.DisplayName} (Level {dead.Level}) - Cost: {cost:N0} gold");
        }

        terminal.WriteLine("");
        terminal.SetColor("cyan");
        terminal.Write("Enter number to resurrect (0 to cancel): ");
        terminal.SetColor("white");
        string input = await terminal.ReadLineAsync();

        if (int.TryParse(input, out int choice) && choice >= 1 && choice <= deadMembers.Count)
        {
            var toResurrect = deadMembers[choice - 1];
            long cost = toResurrect.Level * 1000;

            if (currentPlayer.Gold < cost)
            {
                terminal.SetColor("red");
                terminal.WriteLine($"You need {cost:N0} gold to resurrect {toResurrect.DisplayName}!");
            }
            else
            {
                currentPlayer.Gold -= cost;
                toResurrect.HP = toResurrect.MaxHP / 2; // Resurrect at half HP
                toResurrect.IsDead = false; // Clear permanent death flag - IsAlive is computed from HP > 0

                terminal.WriteLine("");
                terminal.SetColor("bright_green");
                terminal.WriteLine($"{toResurrect.DisplayName} has been resurrected!");
                terminal.WriteLine($"Cost: {cost:N0} gold");

                NewsSystem.Instance.Newsy(true, $"{toResurrect.DisplayName} was resurrected by their team '{currentPlayer.Team}'!");
            }
        }

        terminal.WriteLine("");
        terminal.SetColor("darkgray");
        terminal.WriteLine("Press any key to continue...");
        await terminal.ReadKeyAsync();
    }

    #endregion
}
