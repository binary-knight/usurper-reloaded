using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// This file collects ultra-lightweight stub implementations that legacy code still expects.
// They are NOT feature-complete – just enough to make the code compile.

#region GameConfig color & misc constants extensions
public static partial class GameConfig
{
    public const string LevelColor = "`6";
    public const string ClassColor = "`5";
    public const string RaceColor  = "`3";
    public const string RoyalColor = "`E";

    public static class GameLocation
    {
        public const int Bank = (int)global::GameLocation.Bank;
        public const int MainStreet = (int)global::GameLocation.MainStreet;
        public const int MagicShop = (int)global::GameLocation.MagicShop;
        public const int TheInn = (int)global::GameLocation.TheInn;
        public const int Temple = (int)global::GameLocation.Temple;
        // Add more mappings as needed by legacy code
    }
}
#endregion

#region NewsSystem overloads & helpers
public partial class NewsSystem
{
    // Overload with arbitrary extra message parts (header + any tail parts)
    public void Newsy(bool newsToAnsi, string header, params string[] messages)
    {
        if (messages == null || messages.Length == 0)
        {
            Newsy(newsToAnsi, header);
            return;
        }

        foreach (var part in messages)
        {
            Newsy(newsToAnsi, $"{header} {part}");
        }
    }
}
#endregion

#region MailSystem quest helpers
public static partial class MailSystem
{
    public static void SendQuestClaimedMail(string player, string questName){}
    public static void SendQuestOfferMail(string player, string questName, long reward){}
    public static void SendQuestFailureMail(string player, string questName){}
    public static void SendQuestFailureNotificationMail(string player, string questName){}
    public static void SendQuestCompletionMail(string player, string questName, long reward){}
}

// Lower-case alias used by Pascal-style code
public static class mailSystem
{
    public static void SendMail(string receiver, string subject, string line1, string line2 = "", string line3 = "", string line4 = "")
        => MailSystem.SendMail(receiver, subject, line1, line2, line3, line4);
}
#endregion

#region RelationshipSystem helpers
public partial class RelationshipSystem
{
    public int GetRelation(Character a, Character b) => GetRelationshipStatus(a, b);

    // Placeholder that simply logs kill counts – full logic later
    public void UpdateKillStats(Character killer, Character victim) {}

    public List<string> GetAllRelationships() => new List<string>();
}
#endregion 