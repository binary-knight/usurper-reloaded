using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UsurperRemake.Utils;

// This file purposefully groups small compatibility shims that let the modern C# port compile
// even though the full feature-complete implementation is still pending.

namespace UsurperRemake
{
    #region Missing Simple Enums
    // Simplified activity/state/relationship enums used by the AI & simulator layers
    public enum Activity { Idle, Working, Hunting, Resting, Socialising }

    public enum RelationshipType
    {
        Enemy = -1,
        Neutral = 0,
        Friend = 1,
        CloseFriend = 2,
        Lover = 3
    }

    public enum NPCState { Idle, Patrol, Engage, Flee, Dead }
    #endregion
}

namespace UsurperRemake.Utils
{
    public partial class TerminalUI
    {
        public static implicit operator TerminalUI(TerminalEmulator emulator) => new TerminalUI();
        public static implicit operator TerminalEmulator(TerminalUI ui) => TerminalEmulator.Instance ?? new TerminalEmulator();
    }
}

#region Partial class shims (global namespace like originals)

public partial class MailSystem
{
    /// <summary>Legacy helper expected by TeamSystem – simply forwards to <see cref="SendSystemMail"/> for now.</summary>
    public static void SendMail(string receiver, string subject, string line1, string line2 = "", string line3 = "", string line4 = "")
    {
        SendSystemMail(receiver, subject, line1, line2, line3);
        if (!string.IsNullOrWhiteSpace(line4))
        {
            // Append extra line when provided
            SendSystemMail(receiver, subject, line4);
        }
    }
}

public partial class NewsSystem
{
    // Simple three-parameter overload used by TournamentSystem and others
    public void Newsy(bool newsToAnsi, string header, string message)
        => Newsy(newsToAnsi, $"{header} – {message}");

    // Team-specific wrapper expected by TeamSystem when category is supplied
    public void WriteTeamNews(string headline, string message, GameConfig.NewsCategory category)
        => GenericNews(category, GameConfig.Ansi, $"{headline}: {message}");
}

public partial class CombatEngine
{
    /// <summary>Extremely stripped-down stub that lets the code compile. A full implementation will be added later.</summary>
    public class PvPCombatResult
    {
        public Character? Winner { get; set; }
        public int Rounds { get; set; }
    }

    public Task<PvPCombatResult> ConductPlayerVsPlayerCombat(Character attacker, Character defender, bool fastGame)
        => Task.FromResult(new PvPCombatResult { Winner = attacker, Rounds = 1 });
}

public partial class NPC
{
    // Overload that converts enum to the original int-based relationship scale
    public void AddRelationship(string characterId, UsurperRemake.RelationshipType relation)
        => AddRelationship(characterId, (int)relation);
}

#endregion 