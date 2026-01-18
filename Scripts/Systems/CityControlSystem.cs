using UsurperRemake.Systems;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// City Control System - Manages team control of the city
/// The team that controls the city receives:
/// - Discounts at shops (percentage off purchases)
/// - Share of sales tax (set by the King)
/// - Prestige and influence
///
/// IMPORTANT RULES:
/// - The King cannot be on a team (must leave team to become King)
/// - City controller and throne controller must be DIFFERENT entities
/// </summary>
public class CityControlSystem
{
    private static CityControlSystem? instance;
    public static CityControlSystem Instance => instance ??= new CityControlSystem();

    private Random random = new();

    // City control bonuses
    public const float ShopDiscountPercent = 10f;     // 10% discount at all shops
    public const int MinTaxSharePercent = 1;          // Minimum tax share (1%)
    public const int MaxTaxSharePercent = 10;         // Maximum tax share (10%)

    /// <summary>
    /// Get the team currently controlling the city (CTurf = true)
    /// </summary>
    public string? GetControllingTeam()
    {
        var npcs = NPCSpawnSystem.Instance?.ActiveNPCs;
        if (npcs == null) return null;

        var controller = npcs.FirstOrDefault(n => n.CTurf && !string.IsNullOrEmpty(n.Team));
        return controller?.Team;
    }

    /// <summary>
    /// Get all members of the city-controlling team
    /// </summary>
    public List<NPC> GetCityControllers()
    {
        var controllingTeam = GetControllingTeam();
        if (string.IsNullOrEmpty(controllingTeam)) return new List<NPC>();

        return NPCSpawnSystem.Instance?.ActiveNPCs?
            .Where(n => n.Team == controllingTeam && n.IsAlive)
            .ToList() ?? new List<NPC>();
    }

    /// <summary>
    /// Check if a character is a member of the city-controlling team
    /// </summary>
    public bool IsCharacterCityController(Character character)
    {
        if (character == null || string.IsNullOrEmpty(character.Team))
            return false;

        return character.CTurf;
    }

    /// <summary>
    /// Calculate shop discount for a character
    /// </summary>
    public float GetShopDiscount(Character buyer)
    {
        if (IsCharacterCityController(buyer))
        {
            return ShopDiscountPercent / 100f; // 10% = 0.10
        }

        return 0f; // No discount
    }

    /// <summary>
    /// Apply discount to a price for city controllers
    /// </summary>
    public long ApplyDiscount(long originalPrice, Character buyer)
    {
        if (!IsCharacterCityController(buyer))
            return originalPrice;

        float discount = GetShopDiscount(buyer);
        long discountAmount = (long)(originalPrice * discount);

        return Math.Max(1, originalPrice - discountAmount);
    }

    /// <summary>
    /// Calculate and distribute city tax share from a sale
    /// Called when any shop makes a sale
    /// </summary>
    public void ProcessSaleTax(long saleAmount)
    {
        var king = CastleLocation.GetCurrentKing();
        if (king == null) return;

        // Calculate the city's share based on King's setting
        int taxPercent = king.CityTaxPercent;
        if (taxPercent <= 0) return;

        long cityShare = (saleAmount * taxPercent) / 100;
        if (cityShare <= 0) return;

        // Distribute among city controllers
        var controllers = GetCityControllers();
        if (controllers.Count == 0) return;

        // Split evenly among all controllers
        long sharePerMember = cityShare / controllers.Count;
        if (sharePerMember <= 0) return;

        foreach (var controller in controllers)
        {
            controller.GainGold(sharePerMember);
        }

        // GD.Print($"[CityControl] Distributed {cityShare} gold tax share to {controllers.Count} city controllers");
    }

    /// <summary>
    /// Attempt to challenge for city control
    /// Returns true if the challenge was successful
    /// </summary>
    public bool ChallengeForCityControl(Character challenger, string challengerTeam)
    {
        if (string.IsNullOrEmpty(challengerTeam))
        {
            // GD.Print("[CityControl] Challenge failed: Challenger must be in a team");
            return false;
        }

        // RULE: King cannot be on a team / control the city
        var king = CastleLocation.GetCurrentKing();
        if (king != null && king.Name == challenger.Name2)
        {
            // GD.Print("[CityControl] Challenge failed: King cannot control the city");
            return false;
        }

        var currentControllers = GetCityControllers();
        var controllingTeam = GetControllingTeam();

        // If no one controls the city, easy takeover
        if (string.IsNullOrEmpty(controllingTeam) || currentControllers.Count == 0)
        {
            TransferCityControl(challengerTeam);
            NewsSystem.Instance?.Newsy(true, $"'{challengerTeam}' has taken control of the city!");
            return true;
        }

        // Get challenger's team members
        var challengingTeam = NPCSpawnSystem.Instance?.ActiveNPCs?
            .Where(n => n.Team == challengerTeam && n.IsAlive)
            .ToList() ?? new List<NPC>();

        if (challengingTeam.Count == 0)
        {
            // GD.Print("[CityControl] Challenge failed: No team members available");
            return false;
        }

        // Calculate team power
        long challengerPower = challengingTeam.Sum(m => m.Level + m.Strength + m.Defence);
        long defenderPower = currentControllers.Sum(m => m.Level + m.Strength + m.Defence);

        // Add randomness (+-20%)
        challengerPower += random.Next((int)(-challengerPower * 0.2), (int)(challengerPower * 0.2));
        defenderPower += random.Next((int)(-defenderPower * 0.2), (int)(defenderPower * 0.2));

        if (challengerPower > defenderPower)
        {
            // Challengers win!
            TransferCityControl(challengerTeam);
            NewsSystem.Instance?.Newsy(true,
                $"'{challengerTeam}' defeated '{controllingTeam}' and now controls the city!");
            return true;
        }
        else
        {
            // Defenders hold
            NewsSystem.Instance?.Newsy(true,
                $"'{controllingTeam}' successfully defended the city against '{challengerTeam}'!");
            return false;
        }
    }

    /// <summary>
    /// Transfer city control to a new team
    /// </summary>
    private void TransferCityControl(string newControllingTeam)
    {
        var npcs = NPCSpawnSystem.Instance?.ActiveNPCs;
        if (npcs == null) return;

        // Remove control from old team
        foreach (var npc in npcs.Where(n => n.CTurf))
        {
            npc.CTurf = false;
        }

        // Give control to new team
        foreach (var npc in npcs.Where(n => n.Team == newControllingTeam && n.IsAlive))
        {
            npc.CTurf = true;
            npc.TeamRec = 0;
        }

        // GD.Print($"[CityControl] City control transferred to '{newControllingTeam}'");
    }

    /// <summary>
    /// Remove city control from a team (e.g., when team disbands)
    /// </summary>
    public void RemoveCityControl(string teamName)
    {
        var npcs = NPCSpawnSystem.Instance?.ActiveNPCs;
        if (npcs == null) return;

        foreach (var npc in npcs.Where(n => n.Team == teamName))
        {
            npc.CTurf = false;
        }

        NewsSystem.Instance?.Newsy(true, $"The city is no longer under team control!");
        // GD.Print($"[CityControl] Removed city control from '{teamName}'");
    }

    /// <summary>
    /// Check if a King challenge conflicts with city control rules
    /// King cannot be on a team - return true if they need to leave their team first
    /// </summary>
    public bool WouldViolateKingTeamRule(Character potentialKing)
    {
        return !string.IsNullOrEmpty(potentialKing.Team);
    }

    /// <summary>
    /// Force a character to leave their team (for becoming King)
    /// </summary>
    public void ForceLeaveTeam(Character character)
    {
        if (string.IsNullOrEmpty(character.Team)) return;

        string oldTeam = character.Team;

        character.Team = "";
        character.TeamPW = "";
        character.CTurf = false;
        character.TeamRec = 0;

        NewsSystem.Instance?.Newsy(true, $"{character.Name2} has left '{oldTeam}'.");
        // GD.Print($"[CityControl] {character.Name2} forced to leave team '{oldTeam}'");
    }

    /// <summary>
    /// Get city control status message for display
    /// </summary>
    public string GetCityStatusMessage()
    {
        var controllingTeam = GetControllingTeam();
        var controllers = GetCityControllers();

        if (string.IsNullOrEmpty(controllingTeam))
        {
            return "The city is not under any team's control.";
        }

        return $"'{controllingTeam}' controls the city ({controllers.Count} members)";
    }

    /// <summary>
    /// Get detailed city control info
    /// </summary>
    public (string TeamName, int MemberCount, long TotalPower) GetCityControlInfo()
    {
        var controllingTeam = GetControllingTeam();
        if (string.IsNullOrEmpty(controllingTeam))
            return ("None", 0, 0);

        var controllers = GetCityControllers();
        long power = controllers.Sum(m => m.Level + m.Strength + m.Defence);

        return (controllingTeam, controllers.Count, power);
    }
}
