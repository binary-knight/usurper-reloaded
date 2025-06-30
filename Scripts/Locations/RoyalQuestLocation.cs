using UsurperRemake.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

/// <summary>
/// Royal Quest Location - Based on Pascal RQUESTS.PAS
/// Where kings can create and manage quests
/// </summary>
public class RoyalQuestLocation : BaseLocation
{
    public RoyalQuestLocation(TerminalEmulator terminal) : base(terminal)
    {
    }
    
    public new async Task Enter(Character player)
    {
        if (!player.King)
        {
            terminal.WriteLine("Only royalty may enter the Quest Master's chambers.", "red");
            await terminal.PressAnyKey();
            return;
        }
        
        terminal.WriteLine("You enter the Chambers of Quest Master Pingon", "white");
        terminal.WriteLine("", "white");
        
        bool leaving = false;
        while (!leaving)
        {
            ShowMenu(player.Expert);
            var choice = await terminal.GetInput("Your choice: ");
            
            switch (choice.ToUpper())
            {
                case "I":
                    await InitiateQuest(player);
                    break;
                case "L":
                    await ListQuests();
                    break;
                case "S":
                    await ShowStatus(player);
                    break;
                case "R":
                    leaving = true;
                    break;
                default:
                    terminal.WriteLine("Invalid choice.", "red");
                    break;
            }
        }
        
        terminal.WriteLine("You take the winding staircase down to the Great Hall.", "white");
    }
    
    private void ShowMenu(bool expert)
    {
        if (!expert)
        {
            terminal.WriteLine("-*- Quest Master -*-", "bright_blue");
            terminal.WriteLine("(I)nitiate Quest  (L)ist Quests", "cyan");
            terminal.WriteLine("(S)tatus          (R)eturn", "cyan");
        }
        else
        {
            terminal.WriteLine("Quest Master (I,L,S,R) :", "white");
        }
    }
    
    private async Task InitiateQuest(Character king)
    {
        if (king.QuestsLeft < 1)
        {
            terminal.WriteLine("You have no quests left today!", "red");
            await terminal.PressAnyKey();
            return;
        }
        
        terminal.WriteLine("Quest Creation", "bright_yellow");
        terminal.WriteLine($"Quests remaining: {king.QuestsLeft}", "cyan");
        terminal.WriteLine("", "white");
        
        // Quest type selection
        terminal.WriteLine("Quest Types:", "white");
        terminal.WriteLine("(1) Monster Hunt", "cyan");
        terminal.WriteLine("(2) Assassination", "cyan");
        terminal.WriteLine("(3) Seduction", "cyan");
        terminal.WriteLine("(4) Territory", "cyan");
        terminal.WriteLine("(5) Gang War", "cyan");
        
        var typeInput = await terminal.GetInput("Select type (1-5): ");
        if (!int.TryParse(typeInput, out int type) || type < 1 || type > 5)
        {
            terminal.WriteLine("Invalid type.", "red");
            return;
        }
        
        // Difficulty selection
        terminal.WriteLine("", "white");
        terminal.WriteLine("Difficulty:", "white");
        terminal.WriteLine("(1) Easy (2) Medium (3) Hard (4) Extreme", "cyan");
        
        var diffInput = await terminal.GetInput("Select difficulty (1-4): ");
        if (!byte.TryParse(diffInput, out byte difficulty) || difficulty < 1 || difficulty > 4)
        {
            terminal.WriteLine("Invalid difficulty.", "red");
            return;
        }
        
        // Quest description
        terminal.WriteLine("", "white");
        var comment = await terminal.GetInput("Quest description (optional): ");
        if (string.IsNullOrEmpty(comment))
        {
            comment = $"A quest of {(QuestTarget)(type - 1)} type.";
        }
        
        // Confirm creation
        terminal.WriteLine("", "white");
        terminal.WriteLine("Create this quest? (Y/N)", "white");
        var confirm = await terminal.GetInput("> ");
        
        if (confirm.ToUpper().StartsWith("Y"))
        {
            try
            {
                var quest = QuestSystem.CreateQuest(king, (QuestTarget)(type - 1), difficulty, comment);
                terminal.WriteLine("", "white");
                terminal.WriteLine("Quest created successfully!", "green");
                terminal.WriteLine("Players can now claim this quest.", "white");
                
                await terminal.PressAnyKey();
            }
            catch (Exception ex)
            {
                terminal.WriteLine($"Failed to create quest: {ex.Message}", "red");
                await terminal.PressAnyKey();
            }
        }
    }
    
    private async Task ListQuests()
    {
        var quests = QuestSystem.GetAllQuests();
        
        terminal.WriteLine("All Quests:", "bright_cyan");
        terminal.WriteLine("", "white");
        
        if (quests.Count == 0)
        {
            terminal.WriteLine("No quests exist.", "yellow");
        }
        else
        {
            foreach (var quest in quests)
            {
                var status = string.IsNullOrEmpty(quest.Occupier) ? "Available" : $"Claimed by {quest.Occupier}";
                terminal.WriteLine($"{quest.GetTargetDescription()} - {quest.GetDifficultyString()}", "white");
                terminal.WriteLine($"  Created by: {quest.Initiator} | Status: {status}", "gray");
                terminal.WriteLine("", "white");
            }
        }
        
        await terminal.PressAnyKey();
    }
    
    private async Task ShowStatus(Character player)
    {
        var allQuests = QuestSystem.GetAllQuests();
        var myQuests = allQuests.Where(q => q.Initiator == player.Name2).ToList();
        
        terminal.WriteLine("Royal Quest Status:", "cyan");
        terminal.WriteLine($"Quests remaining today: {player.QuestsLeft}", "white");
        terminal.WriteLine($"Total quests created: {myQuests.Count}", "white");
        
        var active = myQuests.Where(q => !string.IsNullOrEmpty(q.Occupier)).Count();
        terminal.WriteLine($"Currently active: {active}", "white");
        
        await terminal.PressAnyKey();
    }
} 
