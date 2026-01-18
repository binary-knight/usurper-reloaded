using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UsurperRemake.Locations;

/// <summary>
/// Hall of Recruitment – hire or bribe NPC characters to join your team.
/// Ported from Pascal RECRUITE.PAS (simplified).
/// Keys:
///   L – List candidates
///   E – Examine a candidate
///   H – Hire / Bribe candidate (requires you be in a team)
///   S – Status
///   R – Return to Inn
/// </summary>
public class HallOfRecruitmentLocation : BaseLocation
{
    private readonly TeamSystem? teamSystem = UsurperRemake.Utils.GodotHelpers.GetNode<TeamSystem>("/root/TeamSystem");

    // Cache list each visit
    private List<NPC> candidatePool = new();

    public HallOfRecruitmentLocation() : base(GameLocation.Recruit,
        "Hall of Recruitment",
        "A bustling hall where adventurers seek employment and mercenaries barter their services.")
    {
    }

    protected override void SetupLocation()
    {
        PossibleExits.Add(GameLocation.TheInn);
        PossibleExits.Add(GameLocation.MainStreet);
    }

    protected override void DisplayLocation()
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_yellow");
        terminal.WriteLine("-*- Hall of Recruitment -*-");
        terminal.SetColor("white");
        terminal.WriteLine("Rows of rugged fighters and mysterious mages size you up, hoping for a lucrative contract.");
        terminal.WriteLine("");
        terminal.WriteLine("(L) List applicants    (E) Examine applicant    (H) Hire / Bribe");
        terminal.WriteLine("(S) Status             (R) Return to Inn");
        terminal.WriteLine("");
        ShowStatusLine();
    }

    protected override async Task<bool> ProcessChoice(string choice)
    {
        // Handle global quick commands first
        var (handled, shouldExit) = await TryProcessGlobalCommand(choice);
        if (handled) return shouldExit;

        if (string.IsNullOrWhiteSpace(choice)) return false;
        char ch = char.ToUpperInvariant(choice.Trim()[0]);

        switch (ch)
        {
            case 'L':
                await ListCandidates();
                return false;
            case 'E':
                await ExamineCandidate();
                return false;
            case 'H':
                await HireOrBribe();
                return false;
            case 'S':
                await ShowStatus();
                return false;
            case 'R':
                await NavigateToLocation(GameLocation.TheInn);
                return true;
            default:
                return false;
        }
    }

    #region Helper Methods

    private void RefreshCandidatePool()
    {
        var locMgr = LocationManager.Instance;
        candidatePool = locMgr.GetNPCsInLocation(GameLocation.Recruit)
                               .Where(n => n.IsAlive)
                               .ToList();

        // If pool thin (<6), randomly draw wanderers from other locations
        if (candidatePool.Count < 6)
        {
            var random = new Random();
            foreach (GameLocation loc in Enum.GetValues(typeof(GameLocation)))
            {
                if (loc == GameLocation.Recruit) continue;
                var locals = locMgr.GetNPCsInLocation(loc).Where(n => n.IsAlive && string.IsNullOrEmpty(n.Team));
                foreach (var npc in locals)
                {
                    if (candidatePool.Count >= 10) break;
                    if (random.NextDouble() < 0.1) // 10% chance to travel here
                    {
                        locMgr.RemoveNPCFromLocation(loc, npc);
                        locMgr.AddNPCToLocation(GameLocation.Recruit, npc);
                        npc.UpdateLocation("hall_recruit");
                        candidatePool.Add(npc);
                    }
                }
            }
        }
    }

    private async Task ListCandidates()
    {
        RefreshCandidatePool();

        if (candidatePool.Count == 0)
        {
            terminal.WriteLine("No adventurers are available for hire right now.", "gray");
            await terminal.WaitForKeyPress();
            return;
        }

        terminal.ClearScreen();
        terminal.SetColor("bright_cyan");
        terminal.WriteLine("Available Candidates");
        terminal.SetColor("cyan");
        terminal.WriteLine("Idx Name                 Class   Lvl  Cost  Loyalty");
        terminal.WriteLine("────────────────────────────────────────────────────");

        int idx = 1;
        foreach (var npc in candidatePool)
        {
            var cost = CalculateHireCost(npc);
            terminal.WriteLine($"{idx,3} {npc.Name2,-20} {npc.Class,-7} {npc.Level,3}  {cost,6}  {npc.Loyalty,3}");
            idx++;
        }
        terminal.WriteLine("");
        terminal.WriteLine("Press Enter to continue...");
        await terminal.WaitForKeyPress();
    }

    private async Task ExamineCandidate()
    {
        RefreshCandidatePool();
        if (candidatePool.Count == 0)
        {
            terminal.WriteLine("No one to examine.", "gray");
            await terminal.WaitForKeyPress();
            return;
        }

        var input = await terminal.GetInput("Enter candidate number or name: ");
        NPC? npc = null;
        if (int.TryParse(input, out int num) && num >= 1 && num <= candidatePool.Count)
            npc = candidatePool[num - 1];
        else
            npc = candidatePool.FirstOrDefault(n => n.Name2.Equals(input, StringComparison.OrdinalIgnoreCase));

        if (npc == null)
        {
            terminal.WriteLine("No matching candidate.", "red");
            await Task.Delay(1500);
            return;
        }

        terminal.ClearScreen();
        terminal.SetColor("bright_yellow");
        terminal.WriteLine(npc.Name2);
        terminal.SetColor("yellow");
        terminal.WriteLine(new string('═', npc.Name2.Length));
        terminal.SetColor("white");
        terminal.WriteLine(npc.GetDisplayInfo());
        terminal.WriteLine("");
        terminal.WriteLine("Press Enter...");
        await terminal.WaitForKeyPress();
    }

    private async Task HireOrBribe()
    {
        if (string.IsNullOrEmpty(currentPlayer.Team))
        {
            terminal.WriteLine("You must create or join a team before you can recruit followers.", "red");
            await terminal.WaitForKeyPress();
            return;
        }

        RefreshCandidatePool();
        if (candidatePool.Count == 0)
        {
            terminal.WriteLine("No one here to recruit.", "gray");
            await terminal.WaitForKeyPress();
            return;
        }

        var input = await terminal.GetInput("Who do you want to recruit (number)? ");
        if (!int.TryParse(input, out int num) || num < 1 || num > candidatePool.Count)
        {
            terminal.WriteLine("Invalid selection.", "red");
            await Task.Delay(1500);
            return;
        }

        var npc = candidatePool[num - 1];

        bool alreadyInOwnTeam = npc.Team == currentPlayer.Team;
        if (alreadyInOwnTeam)
        {
            terminal.WriteLine($"{npc.Name2} is already in your team!", "yellow");
            await Task.Delay(1500);
            return;
        }

        long cost = CalculateHireCost(npc);
        string action = string.IsNullOrEmpty(npc.Team) ? "hire" : "bribe";
        terminal.WriteLine($"It will cost {cost} gold to {action} {npc.Name2}. You have {currentPlayer.Gold} gold.");
        var confirm = await terminal.GetInput("Proceed? (y/N): ");
        if (!confirm.Equals("Y", StringComparison.OrdinalIgnoreCase))
            return;

        if (currentPlayer.Gold < cost)
        {
            terminal.WriteLine("You cannot afford this deal!", "red");
            await Task.Delay(1500);
            return;
        }

        // deduct gold
        currentPlayer.Gold -= cost;
        npc.Gold += cost; // give npc their fee

        // Join or switch team
        if (teamSystem != null)
        {
            // If NPC already has a team, leave it first by clearing fields
            npc.Team = "";
            npc.TeamPW = currentPlayer.TeamPW;
            teamSystem.JoinTeam(npc, currentPlayer.Team, currentPlayer.TeamPW);
        }
        else
        {
            npc.Team = currentPlayer.Team;
            npc.TeamPW = currentPlayer.TeamPW;
        }

        npc.Loyalty = Math.Max(10, npc.Loyalty / 2); // reset loyalty partially

        terminal.WriteLine($"{npc.Name2} bows and joins your cause!", "green");
        await Task.Delay(2000);
    }

    private static long CalculateHireCost(NPC npc)
    {
        // Simplified cost formula based on level & power
        long baseCost = npc.Level * 500;
        long statCost = (npc.Strength + npc.Defence + npc.Dexterity) * 20;
        return baseCost + statCost;
    }
    #endregion
} 