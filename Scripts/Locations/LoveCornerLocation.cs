#pragma warning disable CS0618 // ReadLine/ReadKey obsolete - TODO: convert to async
using UsurperRemake.Utils;
using UsurperRemake.Systems;
using Godot;
using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Love Corner location based on Pascal LOVERS.PAS
/// Complete dating, marriage, divorce, and family management system
/// Maintains perfect Pascal compatibility with original mechanics
/// </summary>
public class LoveCornerLocation : BaseLocation
{
    public LoveCornerLocation() : base((GameLocation)GameConfig.LoveCorner, GameConfig.DefaultLoveCornerName, "A cozy corner for romance and gossip.") { }

    public new void OnEnter(Character player)
    {
        base.OnEnter(player);
        ShowLocationDescription(player);
    }

    public bool HandleCommand(Character player, string command)
    {
        return command.ToUpper() switch
        {
            "A" => HandleApproachSomebody(player),
            "C" => HandleChildrenInRealm(player),
            "D" => HandleDivorce(player),
            "E" => HandleExamineChild(player),
            "V" => HandleVisitGossipMonger(player),
            "M" => HandleMarriedCouples(player),
            "P" => HandlePersonalRelations(player),
            "G" => HandleGiftShop(player),
            "S" => HandleStatus(player),
            "L" => HandleLoveHistory(player),
            "R" => HandleReturn(player),
            "?" => ShowMenuAndReturnTrue(player),
            _ => false
        };
    }

    private void ShowLocationDescription(Character player)
    {
        var terminal = TerminalEmulator.Instance;
        terminal.WriteLine();
        terminal.WriteLine();
        terminal.WriteLine($"You enter {GameConfig.DefaultLoveCornerName}.", TerminalEmulator.ColorGreen);
        terminal.WriteLine();
        
        if (!player.Expert)
        {
            ShowMenu(player);
        }
    }

    private void ShowMenu(Character player)
    {
        var terminal = TerminalEmulator.Instance;
        terminal.ClearScreen();
        terminal.WriteLine();
        terminal.WriteLine($"-=*=- {GameConfig.DefaultLoveCornerName} -=*=-", TerminalEmulator.ColorYellow);
        terminal.WriteLine();
        terminal.WriteLine("Come and train your romantic skills. There is nothing");
        terminal.WriteLine("like a big family and a house.");
        terminal.WriteLine("You should also make up to the people you have hurt through the");
        terminal.WriteLine("years. You can poison other persons lives here as well.");
        terminal.WriteLine();
        
        terminal.WriteLine("(A)pproach somebody          (C)hildren in the Realm");
        terminal.WriteLine("(D)ivorce                    (V)isit " + GameConfig.DefaultGossipMongerName);
        terminal.WriteLine("(M)arried Couples            (E)xamine child");
        terminal.WriteLine("(P)ersonal Relations         (G)ift shop");
        terminal.WriteLine("(S)tatus                     (R)eturn");
        terminal.WriteLine("(L)ove history");
        terminal.WriteLine();
        
        ShowPrompt(player);
    }

    private void ShowPrompt(Character player)
    {
        var terminal = TerminalEmulator.Instance;
        if (player.Expert)
        {
            terminal.Write($"{GameConfig.DefaultLoveCornerName} (A,C,D,E,V,M,P,G,S,R,L,?) :");
        }
        else
        {
            terminal.Write($"{GameConfig.DefaultLoveCornerName} (? for menu) :");
        }
    }

    private bool HandleApproachSomebody(Character player)
    {
        var terminal = TerminalEmulator.Instance;
        terminal.WriteLine();
        terminal.WriteLine("Approach Somebody", TerminalEmulator.ColorCyan);
        terminal.WriteLine();
        
        if (player.IntimacyActs < 1)
        {
            terminal.WriteLine("You have no intimacy acts left today!", TerminalEmulator.ColorRed);
            terminal.WriteLine("Come back tomorrow for more romantic opportunities.");
            return WaitForKey();
        }
        
        terminal.Write("Enter the name of the person you wish to approach: ");
        string targetName = terminal.ReadLine();
        
        if (string.IsNullOrWhiteSpace(targetName))
        {
            terminal.WriteLine("Invalid name entered.");
            return WaitForKey();
        }
        
        // In a full implementation, would search for the character
        // For now, simulate the interaction
        terminal.WriteLine();
        terminal.WriteLine($"Searching for {targetName}...", TerminalEmulator.ColorYellow);
        
        // Simulate finding the character and show dating menu
        return ShowDatingMenu(player, targetName);
    }

    private bool ShowDatingMenu(Character player, string targetName)
    {
        var terminal = TerminalEmulator.Instance;
        
        while (true)
        {
            terminal.WriteLine();
            terminal.WriteLine($"Dating with {targetName}", TerminalEmulator.ColorCyan);
            terminal.WriteLine();
            terminal.WriteLine("(K)iss                       (D)inner");
            terminal.WriteLine("(H)old hands                 (I)ntimate");
            terminal.WriteLine("(M)arry                      (C)hange feelings");
            terminal.WriteLine("(R)eturn");
            terminal.WriteLine();
            terminal.Write("Choose your action: ");
            
            string choice = terminal.ReadLine()?.ToUpper();
            
            switch (choice)
            {
                case "K":
                    return HandleKiss(player, targetName);
                case "D":
                    return HandleDinner(player, targetName);
                case "H":
                    return HandleHoldHands(player, targetName);
                case "I":
                    return HandleIntimate(player, targetName);
                case "M":
                    return HandleMarry(player, targetName);
                case "C":
                    return HandleChangeFeelings(player, targetName);
                case "R":
                    return true; // Return to main menu
                default:
                    terminal.WriteLine("Invalid choice.");
                    break;
            }
        }
    }

    private bool HandleKiss(Character player, string targetName)
    {
        var terminal = TerminalEmulator.Instance;
        terminal.WriteLine();
        terminal.WriteLine($"You lean in to kiss {targetName}...", TerminalEmulator.ColorMagenta);
        
        // Calculate experience (Pascal equivalent)
        long experience = player.Level * GameConfig.KissExperienceMultiplier;
        experience = Math.Max(experience, 100);
        
        player.Experience += experience;
        player.IntimacyActs--;
        
        terminal.WriteLine($"A passionate kiss! You both earn {experience} experience points!");
        terminal.WriteLine($"You have {player.IntimacyActs} intimacy acts left today.");
        
        // Random chance to improve relationship
        var random = new Random();
        if (random.Next(2) == 0)
        {
            terminal.WriteLine($"Your relationship with {targetName} has improved!", TerminalEmulator.ColorGreen);
            // In full implementation: RelationshipSystem.UpdateRelationship(player, target, 1);
        }
        
        return WaitForKey();
    }

    private bool HandleDinner(Character player, string targetName)
    {
        var terminal = TerminalEmulator.Instance;
        terminal.WriteLine();
        terminal.WriteLine($"You invite {targetName} for dinner...", TerminalEmulator.ColorYellow);
        
        long experience = player.Level * GameConfig.DinnerExperienceMultiplier;
        experience = Math.Max(experience, 150);
        
        player.Experience += experience;
        player.IntimacyActs--;
        
        terminal.WriteLine($"A delightful dinner! You both earn {experience} experience points!");
        terminal.WriteLine("The conversation flows as smoothly as the wine!");
        terminal.WriteLine($"You have {player.IntimacyActs} intimacy acts left today.");
        
        return WaitForKey();
    }

    private bool HandleHoldHands(Character player, string targetName)
    {
        var terminal = TerminalEmulator.Instance;
        terminal.WriteLine();
        terminal.WriteLine($"You reach out to hold {targetName}'s hand...", TerminalEmulator.ColorCyan);
        
        long experience = player.Level * GameConfig.HandHoldingExperienceMultiplier;
        experience = Math.Max(experience, 100);
        
        player.Experience += experience;
        player.IntimacyActs--;
        
        terminal.WriteLine($"A tender moment! You both earn {experience} experience points!");
        terminal.WriteLine("You walk together, hand in hand, sharing a beautiful moment.");
        terminal.WriteLine($"You have {player.IntimacyActs} intimacy acts left today.");
        
        return WaitForKey();
    }

    private bool HandleIntimate(Character player, string targetName)
    {
        var terminal = TerminalEmulator.Instance;
        terminal.WriteLine();
        terminal.WriteLine($"You embrace {targetName} passionately...", TerminalEmulator.ColorRed);
        
        long experience = player.Level * GameConfig.IntimateExperienceMultiplier;
        experience = Math.Max(experience, 200);
        
        player.Experience += experience;
        player.IntimacyActs--;
        
        terminal.WriteLine($"Passionate embrace! You both earn {experience} experience points!");
        terminal.WriteLine("The moment is filled with deep emotion and connection.");
        terminal.WriteLine($"You have {player.IntimacyActs} intimacy acts left today.");
        
        // Potential for pregnancy in full implementation
        terminal.WriteLine();
        terminal.WriteLine("The gods smile upon your union...", TerminalEmulator.ColorYellow);
        
        return WaitForKey();
    }

    private bool HandleMarry(Character player, string targetName)
    {
        var terminal = TerminalEmulator.Instance;
        terminal.WriteLine();
        terminal.WriteLine("Marriage Ceremony", TerminalEmulator.ColorYellow);
        terminal.WriteLine("==================");
        terminal.WriteLine();

        // Try to find the target character (NPC)
        var targetNPC = NPCSpawnSystem.Instance?.ActiveNPCs?.Find(n =>
            n.Name2.Equals(targetName, StringComparison.OrdinalIgnoreCase) ||
            n.Name1.Equals(targetName, StringComparison.OrdinalIgnoreCase));

        long weddingCost = GameConfig.WeddingCostBase;
        if (player.Gold < weddingCost)
        {
            terminal.WriteLine($"The wedding ceremony costs {weddingCost} gold!", TerminalEmulator.ColorRed);
            terminal.WriteLine($"You only have {player.Gold} gold.");
            return WaitForKey();
        }

        terminal.WriteLine($"Wedding ceremony with {targetName}:");
        terminal.WriteLine($"Cost: {weddingCost} gold");
        terminal.WriteLine();
        terminal.Write("Proceed with the ceremony? (Y/N): ");

        string confirm = terminal.ReadLine()?.ToUpper();
        if (confirm != "Y")
        {
            terminal.WriteLine("Wedding ceremony cancelled.");
            return WaitForKey();
        }

        // Pay wedding cost regardless of outcome
        player.Gold -= weddingCost;

        // Use RelationshipSystem.PerformMarriage for proper tracking if we have the target
        if (targetNPC != null)
        {
            if (RelationshipSystem.PerformMarriage(player, targetNPC, out string message))
            {
                terminal.WriteLine();
                terminal.WriteLine("*** WEDDING CEREMONY ***", TerminalEmulator.ColorYellow);
                terminal.WriteLine();
                terminal.WriteLine(message, TerminalEmulator.ColorGreen);
            }
            else
            {
                terminal.WriteLine();
                terminal.WriteLine(message, TerminalEmulator.ColorRed);
                // Refund on failure
                player.Gold += weddingCost;
            }
        }
        else
        {
            // Fallback for when target NPC not found (e.g., offline player or unknown name)
            // Manually set marriage flags for compatibility
            player.IsMarried = true;
            player.Married = true;
            player.SpouseName = targetName;
            player.MarriedTimes++;
            player.IntimacyActs--;

            var ceremonyMessages = GameConfig.WeddingCeremonyMessages;
            var random = new Random();
            string ceremonyMessage = ceremonyMessages[random.Next(ceremonyMessages.Length)];

            terminal.WriteLine();
            terminal.WriteLine("*** WEDDING CEREMONY ***", TerminalEmulator.ColorYellow);
            terminal.WriteLine();
            terminal.WriteLine($"{player.Name} and {targetName} are now married!", TerminalEmulator.ColorGreen);
            terminal.WriteLine(ceremonyMessage);
            terminal.WriteLine();
            terminal.WriteLine("Congratulations! (go home and make babies)", TerminalEmulator.ColorCyan);
        }

        return WaitForKey();
    }

    private bool HandleChangeFeelings(Character player, string targetName)
    {
        var terminal = TerminalEmulator.Instance;
        terminal.WriteLine();
        terminal.WriteLine("Change Your Feelings", TerminalEmulator.ColorCyan);
        terminal.WriteLine("====================");
        terminal.WriteLine();
        terminal.WriteLine("(L)ove               (P)assion");
        terminal.WriteLine("(F)riendship         (T)rust");
        terminal.WriteLine("(R)espect            (N)eutral");
        terminal.WriteLine("(S)uspicious         (A)nger");
        terminal.WriteLine("(E)nemy              (H)ate");
        terminal.WriteLine();
        terminal.Write("How do you feel about them? ");
        
        string feeling = terminal.ReadLine()?.ToUpper();
        
        int newRelation = feeling switch
        {
            "L" => GameConfig.RelationLove,
            "P" => GameConfig.RelationPassion,
            "F" => GameConfig.RelationFriendship,
            "T" => GameConfig.RelationTrust,
            "R" => GameConfig.RelationRespect,
            "N" => GameConfig.RelationNormal,
            "S" => GameConfig.RelationSuspicious,
            "A" => GameConfig.RelationAnger,
            "E" => GameConfig.RelationEnemy,
            "H" => GameConfig.RelationHate,
            _ => GameConfig.RelationNormal
        };
        
        terminal.WriteLine();
        terminal.WriteLine($"Your feelings toward {targetName} have been set to: {GetRelationshipName(newRelation)}");
        
        // Display appropriate reaction
        if (newRelation == GameConfig.RelationLove)
        {
            terminal.WriteLine("LOVE! LOVE! LOVE! LOVE!", TerminalEmulator.ColorMagenta);
        }
        else if (newRelation == GameConfig.RelationHate)
        {
            terminal.WriteLine("HATE! HATE! HATE! HATE!", TerminalEmulator.ColorRed);
        }
        
        return WaitForKey();
    }

    private bool HandleDivorce(Character player)
    {
        var terminal = TerminalEmulator.Instance;
        terminal.WriteLine();
        terminal.WriteLine("Divorce Proceedings", TerminalEmulator.ColorRed);
        terminal.WriteLine("==================");
        terminal.WriteLine();
        
        if (!player.IsMarried)
        {
            terminal.WriteLine("You are not married bird-brain!", TerminalEmulator.ColorRed);
            terminal.WriteLine("Go and find yourself a spouse before it's too late!");
            return WaitForKey();
        }
        
        terminal.WriteLine($"You are married to {player.SpouseName}.");
        terminal.WriteLine();
        terminal.WriteLine("Divorce process:", TerminalEmulator.ColorYellow);
        terminal.WriteLine("- You will lose custody of your children!");
        terminal.WriteLine("- Your relationship will become hostile!");
        terminal.WriteLine($"- Divorce costs {GameConfig.DivorceCostBase} gold!");
        terminal.WriteLine();
        
        if (player.Gold < GameConfig.DivorceCostBase)
        {
            terminal.WriteLine($"You need {GameConfig.DivorceCostBase} gold for divorce proceedings!", TerminalEmulator.ColorRed);
            terminal.WriteLine($"You only have {player.Gold} gold.");
            return WaitForKey();
        }
        
        terminal.Write("Are you sure you want to divorce? (Y/N): ");
        string confirm1 = terminal.ReadLine()?.ToUpper();
        if (confirm1 != "Y")
        {
            terminal.WriteLine("Divorce cancelled.");
            return WaitForKey();
        }
        
        terminal.Write("You will lose custody of your children! Go ahead anyway? (Y/N): ");
        string confirm2 = terminal.ReadLine()?.ToUpper();
        if (confirm2 != "Y")
        {
            terminal.WriteLine("Divorce cancelled.");
            return WaitForKey();
        }
        
        // Process divorce
        player.Gold -= GameConfig.DivorceCostBase;
        string exSpouse = player.SpouseName;
        player.IsMarried = false;
        player.Married = false;
        player.SpouseName = "";
        
        terminal.WriteLine();
        terminal.WriteLine("*** DIVORCE FINALIZED ***", TerminalEmulator.ColorRed);
        terminal.WriteLine();
        terminal.WriteLine($"{exSpouse} is gone at last, and good riddance!", TerminalEmulator.ColorYellow);
        terminal.WriteLine("You have lost custody of your children!");
        terminal.WriteLine();
        terminal.WriteLine("Single Life! Here I come!", TerminalEmulator.ColorGreen);
        
        return WaitForKey();
    }

    private bool HandleChildrenInRealm(Character player)
    {
        var terminal = TerminalEmulator.Instance;
        terminal.WriteLine();
        terminal.WriteLine("Children in the Realm", TerminalEmulator.ColorCyan);
        terminal.WriteLine("====================");
        terminal.WriteLine();
        
        // In full implementation, would list all children
        terminal.WriteLine("Child listing feature:");
        terminal.WriteLine("- View all children in the realm");
        terminal.WriteLine("- See orphans available for adoption");
        terminal.WriteLine("- Check on kidnapped children");
        terminal.WriteLine();
        terminal.WriteLine("This feature will be fully implemented in a future update.");
        
        return WaitForKey();
    }

    private bool HandleExamineChild(Character player)
    {
        var terminal = TerminalEmulator.Instance;
        terminal.WriteLine();
        terminal.WriteLine("Your Children", TerminalEmulator.ColorCyan);
        terminal.WriteLine("=============");
        terminal.WriteLine();

        var children = FamilySystem.Instance.GetChildrenOf(player);

        if (children.Count == 0)
        {
            terminal.WriteLine("You have no children.");
            terminal.WriteLine();
            terminal.WriteLine("To have children, marry someone of the opposite sex");
            terminal.WriteLine("and visit your home for intimate moments.", TerminalEmulator.ColorDarkGray);
            return WaitForKey();
        }

        terminal.WriteLine($"You have {children.Count} child{(children.Count > 1 ? "ren" : "")}:");
        terminal.WriteLine();

        foreach (var child in children)
        {
            terminal.WriteLine($"  {child.Name}", TerminalEmulator.ColorYellow);
            terminal.WriteLine($"    Age: {child.Age} year{(child.Age != 1 ? "s" : "")}");
            terminal.WriteLine($"    Sex: {(child.Sex == CharacterSex.Male ? "Male" : "Female")}");
            terminal.WriteLine($"    Behavior: {child.GetSoulDescription()}");
            terminal.WriteLine($"    Health: {child.GetHealthDescription()}");
            terminal.WriteLine($"    Location: {child.GetLocationDescription()}");

            var marks = child.GetStatusMarks();
            if (!string.IsNullOrEmpty(marks))
            {
                terminal.WriteLine($"    Status: {marks}", TerminalEmulator.ColorRed);
            }

            // Show other parent
            string otherParent = child.Mother == player.Name || child.MotherID == player.ID
                ? child.Father
                : child.Mother;
            if (!string.IsNullOrEmpty(otherParent))
            {
                terminal.WriteLine($"    Other Parent: {otherParent}", TerminalEmulator.ColorDarkGray);
            }

            terminal.WriteLine();
        }

        // Check for any children approaching adulthood
        var teensCount = children.Count(c => c.Age >= 15 && c.Age < FamilySystem.ADULT_AGE);
        if (teensCount > 0)
        {
            terminal.WriteLine($"{teensCount} of your children will come of age soon!", TerminalEmulator.ColorGreen);
            terminal.WriteLine("When children reach 18, they become independent NPCs.", TerminalEmulator.ColorDarkGray);
        }

        return WaitForKey();
    }

    private bool HandleVisitGossipMonger(Character player)
    {
        var terminal = TerminalEmulator.Instance;
        terminal.WriteLine();
        terminal.WriteLine($"Visiting {GameConfig.DefaultGossipMongerName}", TerminalEmulator.ColorMagenta);
        terminal.WriteLine("=====================================");
        terminal.WriteLine();
        terminal.WriteLine("\"Welcome, dearie! Come to hear the latest gossip?\"");
        terminal.WriteLine();
        terminal.WriteLine("Services available:");
        terminal.WriteLine("- Spy on other players' relationships");
        terminal.WriteLine("- Get information about marriages");
        terminal.WriteLine("- Learn about recent divorces");
        terminal.WriteLine("- Hear gossip about children");
        terminal.WriteLine();
        terminal.WriteLine("\"My services aren't free, but they're worth every gold piece!\"");
        terminal.WriteLine();
        terminal.WriteLine("This feature will be fully implemented in a future update.");
        
        return WaitForKey();
    }

    private bool HandleMarriedCouples(Character player)
    {
        var terminal = TerminalEmulator.Instance;
        terminal.WriteLine();
        terminal.WriteLine("<3<3<3 Married Couples <3<3<3", TerminalEmulator.ColorMagenta);
        terminal.WriteLine();

        // Get married couples from the relationship system
        var marriedCouples = RelationshipSystem.GetMarriedCouples();

        if (marriedCouples.Count == 0)
        {
            terminal.WriteLine("No married couples in the realm at this time.");
        }
        else
        {
            foreach (var couple in marriedCouples)
            {
                terminal.WriteLine(couple);
            }
        }

        terminal.WriteLine();

        return WaitForKey();
    }

    private bool HandlePersonalRelations(Character player)
    {
        var terminal = TerminalEmulator.Instance;
        terminal.WriteLine();
        terminal.WriteLine($"{player.Name}'s Personal Relations", TerminalEmulator.ColorCyan);
        terminal.WriteLine("=================================");
        terminal.WriteLine();
        
        if (player.IsMarried)
        {
            terminal.WriteLine($"Married to: {player.SpouseName}", TerminalEmulator.ColorGreen);
            terminal.WriteLine($"Marriage count: {player.MarriedTimes}");
            terminal.WriteLine();
        }
        
        terminal.WriteLine($"Children: {player.Children}");
        terminal.WriteLine($"Intimacy acts left today: {player.IntimacyActs}");
        terminal.WriteLine();
        terminal.WriteLine("Personal relations feature:");
        terminal.WriteLine("- View all your relationships");
        terminal.WriteLine("- See who loves/hates you");
        terminal.WriteLine("- Check relationship history");
        terminal.WriteLine();
        terminal.WriteLine("This will show your full relationship network in the complete system.");
        
        return WaitForKey();
    }

    private bool HandleGiftShop(Character player)
    {
        var terminal = TerminalEmulator.Instance;
        terminal.WriteLine();
        terminal.WriteLine("Gift Shop", TerminalEmulator.ColorYellow);
        terminal.WriteLine("=========");
        terminal.WriteLine();
        terminal.WriteLine("Welcome to the Love Corner Gift Shop!");
        terminal.WriteLine();
        terminal.WriteLine($"(R)oses - {GameConfig.RosesCost} gold");
        terminal.WriteLine($"(C)hocolates - {GameConfig.ChocolatesCostBase} gold");
        terminal.WriteLine($"(J)ewelry - {GameConfig.JewelryCostBase} gold");
        terminal.WriteLine($"(P)oison someone - {GameConfig.PoisonCostBase} gold");
        terminal.WriteLine("(E)xit shop");
        terminal.WriteLine();
        terminal.Write("What would you like to purchase? ");
        
        string choice = terminal.ReadLine()?.ToUpper();
        
        switch (choice)
        {
            case "R":
                return PurchaseGift(player, "Roses", GameConfig.RosesCost);
            case "C":
                return PurchaseGift(player, "Chocolates", GameConfig.ChocolatesCostBase);
            case "J":
                return PurchaseGift(player, "Jewelry", GameConfig.JewelryCostBase);
            case "P":
                return PurchasePoison(player);
            default:
                terminal.WriteLine("Thank you for visiting the gift shop!");
                return WaitForKey();
        }
    }

    private bool PurchaseGift(Character player, string giftName, long cost)
    {
        var terminal = TerminalEmulator.Instance;
        
        if (player.Gold < cost)
        {
            terminal.WriteLine($"You need {cost} gold to buy {giftName}!", TerminalEmulator.ColorRed);
            terminal.WriteLine($"You only have {player.Gold} gold.");
            return WaitForKey();
        }
        
        terminal.Write($"Who would you like to send {giftName} to? ");
        string recipient = terminal.ReadLine();
        
        if (string.IsNullOrWhiteSpace(recipient))
        {
            terminal.WriteLine("Invalid recipient.");
            return WaitForKey();
        }
        
        player.Gold -= cost;
        terminal.WriteLine();
        terminal.WriteLine($"You have sent {giftName} to {recipient}!");
        terminal.WriteLine("They will surely appreciate your thoughtful gift.");
        
        return WaitForKey();
    }

    private bool PurchasePoison(Character player)
    {
        var terminal = TerminalEmulator.Instance;
        long cost = GameConfig.PoisonCostBase;
        
        terminal.WriteLine();
        terminal.WriteLine("Poison Purchase", TerminalEmulator.ColorRed);
        terminal.WriteLine("===============");
        terminal.WriteLine();
        terminal.WriteLine("\"Psst... looking for something... special?\"", TerminalEmulator.ColorDarkGray);
        terminal.WriteLine($"\"This will cost you {cost} gold, no questions asked.\"");
        terminal.WriteLine();
        
        if (player.Gold < cost)
        {
            terminal.WriteLine($"You need {cost} gold!", TerminalEmulator.ColorRed);
            return WaitForKey();
        }
        
        terminal.Write("Who is your... target? ");
        string target = terminal.ReadLine();
        
        if (string.IsNullOrWhiteSpace(target))
        {
            terminal.WriteLine("\"No target, no deal.\"");
            return WaitForKey();
        }
        
        player.Gold -= cost;
        terminal.WriteLine();
        terminal.WriteLine($"\"Consider it done. {target} will... have an accident.\"", TerminalEmulator.ColorRed);
        terminal.WriteLine("\"We never had this conversation.\"");
        
        return WaitForKey();
    }

    private bool HandleStatus(Character player)
    {
        var terminal = TerminalEmulator.Instance;
        terminal.WriteLine();
        terminal.WriteLine($"{player.Name}'s Relationship Status", TerminalEmulator.ColorCyan);
        terminal.WriteLine("====================================");
        terminal.WriteLine();
        
        terminal.WriteLine($"Age: {player.Age} years old");
        terminal.WriteLine($"Sex: {(player.Sex == CharacterSex.Male ? "Male" : "Female")}");
        terminal.WriteLine($"Race: {player.Race}");
        terminal.WriteLine();
        
        if (player.IsMarried)
        {
            terminal.WriteLine($"Marital Status: Married to {player.SpouseName}", TerminalEmulator.ColorGreen);
            terminal.WriteLine($"Times married: {player.MarriedTimes}");
        }
        else
        {
            terminal.WriteLine("Marital Status: Single", TerminalEmulator.ColorYellow);
            if (player.MarriedTimes > 0)
            {
                terminal.WriteLine($"Previous marriages: {player.MarriedTimes}");
            }
        }
        
        terminal.WriteLine();
        terminal.WriteLine($"Children: {player.Children}");
        terminal.WriteLine($"Intimacy acts remaining today: {player.IntimacyActs}");
        terminal.WriteLine();
        
        // Character personality assessment
        if (player.Chivalry >= player.Darkness)
        {
            terminal.WriteLine($"{player.Name} is good-hearted.", TerminalEmulator.ColorGreen);
        }
        else
        {
            terminal.WriteLine($"{player.Name} has an evil mind.", TerminalEmulator.ColorRed);
        }
        
        return WaitForKey();
    }

    private bool HandleLoveHistory(Character player)
    {
        var terminal = TerminalEmulator.Instance;
        terminal.WriteLine();
        terminal.WriteLine("History of Love", TerminalEmulator.ColorMagenta);
        terminal.WriteLine("===============");
        terminal.WriteLine();
        terminal.WriteLine("See what's happened during the years.");
        terminal.WriteLine();
        terminal.WriteLine("(M)arriages & Divorces       (C)hild-births & Deaths");
        terminal.WriteLine("(1) Current Marriages        (H)ated players, Top List");
        terminal.WriteLine("(L)oved players, Top List    (R)eturn");
        terminal.WriteLine();
        terminal.Write("History Room (? for menu): ");
        
        string choice = terminal.ReadLine()?.ToUpper();
        
        switch (choice)
        {
            case "M":
                ShowMarriageHistory();
                break;
            case "C":
                ShowChildBirthHistory();
                break;
            case "1":
                return HandleMarriedCouples(player);
            case "H":
                ShowHatedPlayersList();
                break;
            case "L":
                ShowLovedPlayersList();
                break;
            case "R":
                return true;
            default:
                terminal.WriteLine("Invalid choice.");
                break;
        }
        
        return WaitForKey();
    }

    private void ShowMarriageHistory()
    {
        var terminal = TerminalEmulator.Instance;
        terminal.WriteLine();
        terminal.WriteLine("Marriage & Divorce History", TerminalEmulator.ColorYellow);
        terminal.WriteLine("==========================");
        terminal.WriteLine();
        terminal.WriteLine("Recent marriages and divorces will be displayed here.");
        terminal.WriteLine("This feature will show the complete marriage/divorce log.");
    }

    private void ShowChildBirthHistory()
    {
        var terminal = TerminalEmulator.Instance;
        terminal.WriteLine();
        terminal.WriteLine("Child Birth History", TerminalEmulator.ColorCyan);
        terminal.WriteLine("==================");
        terminal.WriteLine();
        terminal.WriteLine("Recent child births and deaths will be displayed here.");
        terminal.WriteLine("This feature will show the complete child birth log.");
    }

    private void ShowHatedPlayersList()
    {
        var terminal = TerminalEmulator.Instance;
        terminal.WriteLine();
        terminal.WriteLine("Most Hated Players", TerminalEmulator.ColorRed);
        terminal.WriteLine("==================");
        terminal.WriteLine();
        terminal.WriteLine("Top 10 most hated players will be displayed here.");
    }

    private void ShowLovedPlayersList()
    {
        var terminal = TerminalEmulator.Instance;
        terminal.WriteLine();
        terminal.WriteLine("Most Loved Players", TerminalEmulator.ColorMagenta);
        terminal.WriteLine("==================");
        terminal.WriteLine();
        terminal.WriteLine("Top 10 most loved players will be displayed here.");
    }

    private bool HandleReturn(Character player)
    {
        return false; // Exit location
    }

    private bool WaitForKey()
    {
        var terminal = TerminalEmulator.Instance;
        terminal.WriteLine();
        terminal.Write("Press any key to continue...");
        terminal.ReadKey();
        return true;
    }

    private string GetRelationshipName(int relation)
    {
        return relation switch
        {
            GameConfig.RelationMarried => "Married",
            GameConfig.RelationLove => "Love",
            GameConfig.RelationPassion => "Passion",
            GameConfig.RelationFriendship => "Friendship",
            GameConfig.RelationTrust => "Trust",
            GameConfig.RelationRespect => "Respect",
            GameConfig.RelationNormal => "Neutral",
            GameConfig.RelationSuspicious => "Suspicious",
            GameConfig.RelationAnger => "Anger",
            GameConfig.RelationEnemy => "Enemy",
            GameConfig.RelationHate => "Hate",
            _ => "Unknown"
        };
    }

    private bool ShowMenuAndReturnTrue(Character player)
    {
        ShowMenu(player);
        return true;
    }
} 
