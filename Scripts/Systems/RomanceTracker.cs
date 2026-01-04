using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace UsurperRemake.Systems
{
    /// <summary>
    /// Romance Tracker System - Tracks all romantic relationships, lovers, spouses,
    /// and intimate encounter history for the player and NPCs.
    /// </summary>
    public class RomanceTracker
    {
        private static RomanceTracker? _instance;
        public static RomanceTracker Instance => _instance ??= new RomanceTracker();

        // Current relationships
        public List<Lover> CurrentLovers { get; set; } = new();
        public List<Spouse> Spouses { get; set; } = new();  // Supports multiple if poly
        public List<string> FriendsWithBenefits { get; set; } = new();

        // History
        public List<string> Exes { get; set; } = new();
        public List<IntimateEncounter> EncounterHistory { get; set; } = new();

        // Tracking
        public Dictionary<string, int> JealousyLevels { get; set; } = new();
        public Dictionary<string, RelationshipStructure> AgreedStructures { get; set; } = new();

        // Complex arrangements
        public List<CuckoldArrangement> CuckArrangements { get; set; } = new();
        public List<PolyNetwork> PolyNetworks { get; set; } = new();

        public RomanceTracker()
        {
            _instance = this;
        }

        /// <summary>
        /// Reset all romance data - call when starting new game or loading different player
        /// </summary>
        public void Reset()
        {
            CurrentLovers.Clear();
            Spouses.Clear();
            FriendsWithBenefits.Clear();
            Exes.Clear();
            EncounterHistory.Clear();
            JealousyLevels.Clear();
            AgreedStructures.Clear();
            CuckArrangements.Clear();
            PolyNetworks.Clear();
            GD.Print("[Romance] Romance tracker reset");
        }

        /// <summary>
        /// Add a new lover to the tracker
        /// </summary>
        public void AddLover(string npcId, int initialLoveLevel = 30, bool isExclusive = false)
        {
            if (CurrentLovers.Any(l => l.NPCId == npcId))
            {
                GD.Print($"[Romance] {npcId} is already a lover");
                return;
            }

            var lover = new Lover
            {
                NPCId = npcId,
                LoveLevel = initialLoveLevel,
                IsExclusive = isExclusive,
                KnowsAboutOthers = !isExclusive,
                RelationshipStart = DateTime.Now
            };

            CurrentLovers.Add(lover);
            GD.Print($"[Romance] New lover added: {npcId}");

            // Check jealousy of other lovers
            CheckJealousyTriggers(npcId);
        }

        /// <summary>
        /// Add a spouse (marriage)
        /// </summary>
        public void AddSpouse(string npcId, bool isPolyMarriage = false)
        {
            if (Spouses.Any(s => s.NPCId == npcId))
            {
                GD.Print($"[Romance] Already married to {npcId}");
                return;
            }

            // Check if this is allowed
            if (Spouses.Count > 0 && !isPolyMarriage)
            {
                // Check if current spouse consents to polyamory
                var currentSpouse = Spouses.FirstOrDefault();
                if (currentSpouse != null && !currentSpouse.AcceptsPolyamory)
                {
                    GD.Print($"[Romance] Cannot marry {npcId} - current spouse does not accept polyamory");
                    return;
                }
            }

            var spouse = new Spouse
            {
                NPCId = npcId,
                MarriedDate = DateTime.Now,
                LoveLevel = 20, // Start at Love level
                AcceptsPolyamory = isPolyMarriage,
                Children = 0
            };

            Spouses.Add(spouse);

            // Remove from lovers list if present
            CurrentLovers.RemoveAll(l => l.NPCId == npcId);

            GD.Print($"[Romance] Married to {npcId}!");
        }

        /// <summary>
        /// Add a friends-with-benefits relationship
        /// </summary>
        public void AddFWB(string npcId)
        {
            if (!FriendsWithBenefits.Contains(npcId))
            {
                FriendsWithBenefits.Add(npcId);
                GD.Print($"[Romance] FWB relationship with {npcId}");
            }
        }

        /// <summary>
        /// End a relationship and move to exes
        /// </summary>
        public void EndRelationship(string npcId, string reason = "mutual")
        {
            bool wasLover = CurrentLovers.RemoveAll(l => l.NPCId == npcId) > 0;
            bool wasFWB = FriendsWithBenefits.Remove(npcId);

            if (wasLover || wasFWB)
            {
                if (!Exes.Contains(npcId))
                    Exes.Add(npcId);

                GD.Print($"[Romance] Relationship ended with {npcId}: {reason}");
            }
        }

        /// <summary>
        /// Divorce a spouse
        /// </summary>
        public void Divorce(string npcId)
        {
            var spouse = Spouses.FirstOrDefault(s => s.NPCId == npcId);
            if (spouse != null)
            {
                Spouses.Remove(spouse);

                if (!Exes.Contains(npcId))
                    Exes.Add(npcId);

                GD.Print($"[Romance] Divorced from {npcId}");
            }
        }

        /// <summary>
        /// Record an intimate encounter
        /// </summary>
        public void RecordEncounter(IntimateEncounter encounter)
        {
            EncounterHistory.Add(encounter);

            // Update love levels for participants
            foreach (var partnerId in encounter.PartnerIds)
            {
                var lover = CurrentLovers.FirstOrDefault(l => l.NPCId == partnerId);
                if (lover != null)
                {
                    lover.LoveLevel = Math.Max(10, lover.LoveLevel - 2); // Increase love (lower number = better)
                    lover.LastIntimateDate = encounter.Date;
                }

                var spouse = Spouses.FirstOrDefault(s => s.NPCId == partnerId);
                if (spouse != null)
                {
                    spouse.LastIntimateDate = encounter.Date;
                }
            }

            GD.Print($"[Romance] Intimate encounter recorded: {encounter.Type} with {string.Join(", ", encounter.PartnerIds)}");
        }

        /// <summary>
        /// Check if player is in a relationship with this NPC
        /// </summary>
        public bool IsInRelationshipWith(string npcId)
        {
            return CurrentLovers.Any(l => l.NPCId == npcId) ||
                   Spouses.Any(s => s.NPCId == npcId) ||
                   FriendsWithBenefits.Contains(npcId);
        }

        /// <summary>
        /// Get the relationship type with an NPC
        /// </summary>
        public RomanceRelationType GetRelationType(string npcId)
        {
            if (Spouses.Any(s => s.NPCId == npcId))
                return RomanceRelationType.Spouse;
            if (CurrentLovers.Any(l => l.NPCId == npcId))
                return RomanceRelationType.Lover;
            if (FriendsWithBenefits.Contains(npcId))
                return RomanceRelationType.FWB;
            if (Exes.Contains(npcId))
                return RomanceRelationType.Ex;
            return RomanceRelationType.None;
        }

        /// <summary>
        /// Check jealousy triggers when starting new relationship
        /// </summary>
        private void CheckJealousyTriggers(string newLoverId)
        {
            // Check spouses
            foreach (var spouse in Spouses)
            {
                if (!spouse.AcceptsPolyamory && !spouse.KnowsAboutOthers)
                {
                    JealousyLevels[spouse.NPCId] = Math.Min(100, (JealousyLevels.GetValueOrDefault(spouse.NPCId, 0)) + 30);
                    GD.Print($"[Romance] {spouse.NPCId} is getting jealous!");
                }
            }

            // Check exclusive lovers
            foreach (var lover in CurrentLovers.Where(l => l.IsExclusive && l.NPCId != newLoverId))
            {
                if (!lover.KnowsAboutOthers)
                {
                    JealousyLevels[lover.NPCId] = Math.Min(100, (JealousyLevels.GetValueOrDefault(lover.NPCId, 0)) + 20);
                    GD.Print($"[Romance] {lover.NPCId} is getting jealous!");
                }
            }
        }

        /// <summary>
        /// Process jealousy consequences - called during daily maintenance or game tick.
        /// High jealousy can lead to relationship damage, confrontations, or breakups.
        /// </summary>
        /// <returns>List of consequence messages to display to the player</returns>
        public List<string> ProcessJealousyConsequences()
        {
            var messages = new List<string>();
            var random = new Random();

            // Process each jealous partner
            foreach (var kvp in JealousyLevels.ToList())
            {
                string npcId = kvp.Key;
                int jealousy = kvp.Value;

                if (jealousy <= 0) continue;

                // Find the NPC
                var npc = NPCSpawnSystem.Instance?.ActiveNPCs?.FirstOrDefault(n => n.ID == npcId);
                string npcName = npc?.Name ?? npcId;

                // Check if they're a spouse or lover
                var spouse = Spouses.FirstOrDefault(s => s.NPCId == npcId);
                var lover = CurrentLovers.FirstOrDefault(l => l.NPCId == npcId);

                if (jealousy >= 90)
                {
                    // CRITICAL: Breakup or divorce!
                    if (spouse != null)
                    {
                        messages.Add($"{npcName} has had enough of your infidelity!");
                        messages.Add($"{npcName} demands a divorce!");

                        // Remove spouse
                        Spouses.Remove(spouse);
                        Exes.Add(npcId);

                        // Severe relationship damage
                        if (npc != null)
                        {
                            RelationshipSystem.UpdateRelationship(null!, npc, -1, 30, true);
                        }

                        NewsSystem.Instance.Newsy(true, $"{npcName} has divorced their partner due to infidelity!");
                        JealousyLevels.Remove(npcId);
                    }
                    else if (lover != null)
                    {
                        messages.Add($"{npcName} is heartbroken and ends things with you!");

                        CurrentLovers.Remove(lover);
                        Exes.Add(npcId);

                        if (npc != null)
                        {
                            RelationshipSystem.UpdateRelationship(null!, npc, -1, 20, true);
                        }

                        JealousyLevels.Remove(npcId);
                    }
                }
                else if (jealousy >= 70)
                {
                    // High jealousy: Confrontation and major relationship damage
                    if (random.NextDouble() < 0.3)
                    {
                        messages.Add($"{npcName} confronts you about your unfaithfulness!");
                        messages.Add($"\"How could you do this to me?!\" they cry.");

                        if (npc != null)
                        {
                            RelationshipSystem.UpdateRelationship(null!, npc, -1, 10, true);
                        }

                        // Small chance to calm down if they confront
                        JealousyLevels[npcId] = Math.Max(0, jealousy - 10);
                    }
                }
                else if (jealousy >= 50)
                {
                    // Medium jealousy: Suspicion and coldness
                    if (random.NextDouble() < 0.2)
                    {
                        messages.Add($"{npcName} seems distant and suspicious of you...");

                        if (npc != null)
                        {
                            RelationshipSystem.UpdateRelationship(null!, npc, -1, 3, true);
                        }
                    }
                }

                // Jealousy slowly fades over time (if no new triggers)
                if (jealousy > 0 && jealousy < 90)
                {
                    JealousyLevels[npcId] = Math.Max(0, jealousy - 2);
                }
            }

            return messages;
        }

        /// <summary>
        /// Get the jealousy level for a specific NPC
        /// </summary>
        public int GetJealousyLevel(string npcId)
        {
            return JealousyLevels.GetValueOrDefault(npcId, 0);
        }

        /// <summary>
        /// Set up a cuckold/cuckquean arrangement
        /// </summary>
        public void SetupCuckoldArrangement(string primaryPartnerId, string thirdPartyId, bool playerIsWatching)
        {
            var arrangement = new CuckoldArrangement
            {
                PrimaryPartnerId = primaryPartnerId,
                ThirdPartyId = thirdPartyId,
                PlayerIsWatching = playerIsWatching,
                ConsentedDate = DateTime.Now,
                EncounterCount = 0
            };

            CuckArrangements.Add(arrangement);
            GD.Print($"[Romance] Cuckold arrangement set up with {primaryPartnerId} and {thirdPartyId}");
        }

        /// <summary>
        /// Set up a poly network
        /// </summary>
        public void SetupPolyNetwork(string networkName, List<string> members)
        {
            var network = new PolyNetwork
            {
                NetworkName = networkName,
                MemberIds = new List<string>(members),
                EstablishedDate = DateTime.Now
            };

            PolyNetworks.Add(network);
            GD.Print($"[Romance] Poly network '{networkName}' established with {members.Count} members");
        }

        /// <summary>
        /// Get total number of romantic partners (current)
        /// </summary>
        public int TotalPartnerCount => CurrentLovers.Count + Spouses.Count + FriendsWithBenefits.Count;

        /// <summary>
        /// Check if player is married
        /// </summary>
        public bool IsMarried => Spouses.Count > 0;

        /// <summary>
        /// Get primary spouse (first married)
        /// </summary>
        public Spouse? PrimarySpouse => Spouses.FirstOrDefault();

        /// <summary>
        /// Check if player has consented polyamory with a specific partner
        /// </summary>
        public bool HasPolyConsent(string npcId)
        {
            return AgreedStructures.TryGetValue(npcId, out var structure) &&
                   (structure == RelationshipStructure.Polyamorous ||
                    structure == RelationshipStructure.OpenRelationship);
        }

        /// <summary>
        /// Convert to save data format
        /// </summary>
        public UsurperRemake.Systems.RomanceTrackerData ToSaveData()
        {
            var data = new UsurperRemake.Systems.RomanceTrackerData();

            // Convert lovers
            foreach (var lover in CurrentLovers)
            {
                data.CurrentLovers.Add(new UsurperRemake.Systems.LoverData
                {
                    NPCId = lover.NPCId,
                    NPCName = GetNPCName(lover.NPCId),
                    LoveLevel = lover.LoveLevel,
                    IsExclusive = lover.IsExclusive,
                    KnowsAboutOthers = lover.KnowsAboutOthers,
                    MetamorsList = new List<string>(lover.MetamorsList),
                    RelationshipStart = lover.RelationshipStart,
                    LastIntimateDate = lover.LastIntimateDate
                });
            }

            // Convert spouses
            foreach (var spouse in Spouses)
            {
                data.Spouses.Add(new UsurperRemake.Systems.SpouseData
                {
                    NPCId = spouse.NPCId,
                    NPCName = GetNPCName(spouse.NPCId),
                    MarriedDate = spouse.MarriedDate,
                    LoveLevel = spouse.LoveLevel,
                    AcceptsPolyamory = spouse.AcceptsPolyamory,
                    KnowsAboutOthers = spouse.KnowsAboutOthers,
                    Children = spouse.Children,
                    LastIntimateDate = spouse.LastIntimateDate
                });
            }

            // Copy simple lists
            data.FriendsWithBenefits = new List<string>(FriendsWithBenefits);
            data.Exes = new List<string>(Exes);

            // Convert encounter history
            foreach (var encounter in EncounterHistory)
            {
                data.EncounterHistory.Add(new UsurperRemake.Systems.IntimateEncounterData
                {
                    Date = encounter.Date,
                    Location = encounter.Location,
                    PartnerIds = new List<string>(encounter.PartnerIds),
                    PartnerNames = encounter.PartnerIds.Select(id => GetNPCName(id)).ToList(),
                    Type = (int)encounter.Type,
                    Mood = (int)encounter.Mood,
                    IsFirstTime = encounter.IsFirstTime,
                    WatcherIds = new List<string>(encounter.WatcherIds),
                    Notes = encounter.Notes
                });
            }

            // Copy jealousy and structures
            data.JealousyLevels = new Dictionary<string, int>(JealousyLevels);
            foreach (var kvp in AgreedStructures)
            {
                data.AgreedStructures[kvp.Key] = (int)kvp.Value;
            }

            // Convert cuck arrangements
            foreach (var arr in CuckArrangements)
            {
                data.CuckArrangements.Add(new UsurperRemake.Systems.CuckoldArrangementData
                {
                    PrimaryPartnerId = arr.PrimaryPartnerId,
                    PrimaryPartnerName = GetNPCName(arr.PrimaryPartnerId),
                    ThirdPartyId = arr.ThirdPartyId,
                    ThirdPartyName = GetNPCName(arr.ThirdPartyId),
                    PlayerIsWatching = arr.PlayerIsWatching,
                    ConsentedDate = arr.ConsentedDate,
                    EncounterCount = arr.EncounterCount
                });
            }

            // Convert poly networks
            foreach (var network in PolyNetworks)
            {
                data.PolyNetworks.Add(new UsurperRemake.Systems.PolyNetworkData
                {
                    NetworkName = network.NetworkName,
                    MemberIds = new List<string>(network.MemberIds),
                    MemberNames = network.MemberIds.Select(id => GetNPCName(id)).ToList(),
                    EstablishedDate = network.EstablishedDate
                });
            }

            // Save conversation states (flirt progress)
            data.ConversationStates = VisualNovelDialogueSystem.Instance.GetConversationStatesForSave();

            return data;
        }

        /// <summary>
        /// Load from save data format
        /// </summary>
        public void LoadFromSaveData(UsurperRemake.Systems.RomanceTrackerData data)
        {
            if (data == null) return;

            // Clear existing data
            CurrentLovers.Clear();
            Spouses.Clear();
            FriendsWithBenefits.Clear();
            Exes.Clear();
            EncounterHistory.Clear();
            JealousyLevels.Clear();
            AgreedStructures.Clear();
            CuckArrangements.Clear();
            PolyNetworks.Clear();

            // Load lovers
            foreach (var loverData in data.CurrentLovers)
            {
                CurrentLovers.Add(new Lover
                {
                    NPCId = loverData.NPCId,
                    LoveLevel = loverData.LoveLevel,
                    IsExclusive = loverData.IsExclusive,
                    KnowsAboutOthers = loverData.KnowsAboutOthers,
                    MetamorsList = new List<string>(loverData.MetamorsList),
                    RelationshipStart = loverData.RelationshipStart,
                    LastIntimateDate = loverData.LastIntimateDate
                });
            }

            // Load spouses
            foreach (var spouseData in data.Spouses)
            {
                Spouses.Add(new Spouse
                {
                    NPCId = spouseData.NPCId,
                    MarriedDate = spouseData.MarriedDate,
                    LoveLevel = spouseData.LoveLevel,
                    AcceptsPolyamory = spouseData.AcceptsPolyamory,
                    KnowsAboutOthers = spouseData.KnowsAboutOthers,
                    Children = spouseData.Children,
                    LastIntimateDate = spouseData.LastIntimateDate
                });
            }

            // Load simple lists
            FriendsWithBenefits.AddRange(data.FriendsWithBenefits);
            Exes.AddRange(data.Exes);

            // Load encounter history
            foreach (var encData in data.EncounterHistory)
            {
                EncounterHistory.Add(new IntimateEncounter
                {
                    Date = encData.Date,
                    Location = encData.Location,
                    PartnerIds = new List<string>(encData.PartnerIds),
                    Type = (EncounterType)encData.Type,
                    Mood = (IntimacyMood)encData.Mood,
                    IsFirstTime = encData.IsFirstTime,
                    WatcherIds = new List<string>(encData.WatcherIds),
                    Notes = encData.Notes
                });
            }

            // Load jealousy and structures
            foreach (var kvp in data.JealousyLevels)
            {
                JealousyLevels[kvp.Key] = kvp.Value;
            }
            foreach (var kvp in data.AgreedStructures)
            {
                AgreedStructures[kvp.Key] = (RelationshipStructure)kvp.Value;
            }

            // Load cuck arrangements
            foreach (var arrData in data.CuckArrangements)
            {
                CuckArrangements.Add(new CuckoldArrangement
                {
                    PrimaryPartnerId = arrData.PrimaryPartnerId,
                    ThirdPartyId = arrData.ThirdPartyId,
                    PlayerIsWatching = arrData.PlayerIsWatching,
                    ConsentedDate = arrData.ConsentedDate,
                    EncounterCount = arrData.EncounterCount
                });
            }

            // Load poly networks
            foreach (var netData in data.PolyNetworks)
            {
                PolyNetworks.Add(new PolyNetwork
                {
                    NetworkName = netData.NetworkName,
                    MemberIds = new List<string>(netData.MemberIds),
                    EstablishedDate = netData.EstablishedDate
                });
            }

            // Load conversation states (flirt progress)
            if (data.ConversationStates != null && data.ConversationStates.Count > 0)
            {
                VisualNovelDialogueSystem.Instance.LoadConversationStates(data.ConversationStates);
            }

            GD.Print($"[Romance] Loaded romance data: {Spouses.Count} spouses, {CurrentLovers.Count} lovers, {EncounterHistory.Count} encounters");
        }

        /// <summary>
        /// Helper to get NPC name from ID
        /// </summary>
        private string GetNPCName(string npcId)
        {
            var npc = UsurperRemake.Systems.NPCSpawnSystem.Instance?.ActiveNPCs?.FirstOrDefault(n => n.ID == npcId);
            return npc?.Name ?? npcId;
        }

        /// <summary>
        /// Get spouse by NPC ID
        /// </summary>
        public Spouse? GetSpouse(string npcId)
        {
            return Spouses.FirstOrDefault(s => s.NPCId == npcId);
        }

        /// <summary>
        /// Get lover by NPC ID
        /// </summary>
        public Lover? GetLover(string npcId)
        {
            return CurrentLovers.FirstOrDefault(l => l.NPCId == npcId);
        }

        /// <summary>
        /// Add a child to a spouse
        /// </summary>
        public void AddChildToSpouse(string npcId)
        {
            var spouse = GetSpouse(npcId);
            if (spouse != null)
            {
                spouse.Children++;
                GD.Print($"[Romance] Child added to marriage with {npcId}. Total children: {spouse.Children}");
            }
        }
    }

    /// <summary>
    /// Represents a lover relationship
    /// </summary>
    public class Lover
    {
        public string NPCId { get; set; } = "";
        public int LoveLevel { get; set; }
        public bool IsExclusive { get; set; }
        public bool KnowsAboutOthers { get; set; }
        public List<string> MetamorsList { get; set; } = new();  // Other partners they know about
        public DateTime RelationshipStart { get; set; }
        public DateTime? LastIntimateDate { get; set; }
    }

    /// <summary>
    /// Represents a spouse (married partner)
    /// </summary>
    public class Spouse
    {
        public string NPCId { get; set; } = "";
        public DateTime MarriedDate { get; set; }
        public int LoveLevel { get; set; }
        public bool AcceptsPolyamory { get; set; }
        public bool KnowsAboutOthers { get; set; }
        public int Children { get; set; }
        public DateTime? LastIntimateDate { get; set; }
    }

    /// <summary>
    /// Represents an intimate encounter
    /// </summary>
    public class IntimateEncounter
    {
        public DateTime Date { get; set; }
        public string Location { get; set; } = "";
        public List<string> PartnerIds { get; set; } = new();
        public EncounterType Type { get; set; }
        public IntimacyMood Mood { get; set; }
        public bool IsFirstTime { get; set; }
        public List<string> WatcherIds { get; set; } = new();  // For voyeur scenes
        public string Notes { get; set; } = "";
    }

    /// <summary>
    /// Types of intimate encounters
    /// </summary>
    public enum EncounterType
    {
        Solo,           // One-on-one
        Threesome,      // Three participants
        Group,          // More than three
        Voyeur,         // Watching others
        BeingWatched    // Being watched
    }

    /// <summary>
    /// Mood of the intimate encounter
    /// </summary>
    public enum IntimacyMood
    {
        Tender,
        Passionate,
        Rough,
        Playful,
        Kinky,
        Romantic,
        Quick
    }

    /// <summary>
    /// Cuckold/Cuckquean arrangement
    /// </summary>
    public class CuckoldArrangement
    {
        public string PrimaryPartnerId { get; set; } = "";
        public string ThirdPartyId { get; set; } = "";
        public bool PlayerIsWatching { get; set; }  // true = player watches, false = player participates
        public DateTime ConsentedDate { get; set; }
        public int EncounterCount { get; set; }
    }

    /// <summary>
    /// Polyamorous network
    /// </summary>
    public class PolyNetwork
    {
        public string NetworkName { get; set; } = "";
        public List<string> MemberIds { get; set; } = new();
        public DateTime EstablishedDate { get; set; }
    }

    /// <summary>
    /// Relationship structure agreed with partner
    /// </summary>
    public enum RelationshipStructure
    {
        Monogamous,          // Traditional exclusive
        OpenRelationship,    // Primary partner + others allowed
        Polyamorous,         // Multiple committed relationships
        CasualOnly,          // No commitment, just fun
        FriendsWithBenefits, // Physical without romance
        DontAsk              // Don't ask, don't tell
    }

    /// <summary>
    /// Romance relationship type
    /// </summary>
    public enum RomanceRelationType
    {
        None,
        Spouse,
        Lover,
        FWB,
        Ex
    }
}
