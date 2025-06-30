using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

/// <summary>
/// Quest Hall Location - Based on Pascal PLYQUEST.PAS
/// Where players can claim and complete quests
/// </summary>
public class QuestHallLocation : BaseLocation
{
    public QuestHallLocation(TerminalEmulator terminal) : base(terminal)
    {
    }
    
    public override async Task Enter(Character player)
    {
        terminal.WriteLine("You enter the Quest Hall.", "white");
        terminal.WriteLine("The royal clerk Pingon is here to distribute assignments.", "white");
        terminal.WriteLine("", "white");
        
        bool leaving = false;
        while (!leaving)
        {
            ShowMenu(player.Expert);
            var choice = await terminal.GetInput("Your choice: ");
            
            switch (choice.ToUpper())
            {
                case "C":
                    await ClaimQuest(player);
                    break;
                case "F":
                    await FinishQuest(player);
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
        
        terminal.WriteLine("You leave the Quest Hall.", "white");
    }
    
    private void ShowMenu(bool expert)
    {
        if (!expert)
        {
            terminal.WriteLine("-*- Quest Hall -*-", "bright_blue");
            terminal.WriteLine("(C)laim Quest  (F)inish Quest", "cyan");
            terminal.WriteLine("(S)tatus       (R)eturn", "cyan");
        }
        else
        {
            terminal.WriteLine("Quest Hall (C,F,S,R) :", "white");
        }
    }
    
    private async Task ClaimQuest(Character player)
    {
        var quests = QuestSystem.GetAvailableQuests(player);
        if (quests.Count == 0)
        {
            terminal.WriteLine("No quests available.", "yellow");
            return;
        }
        
        terminal.WriteLine("Available Quests:", "cyan");
        for (int i = 0; i < quests.Count; i++)
        {
            var quest = quests[i];
            terminal.WriteLine($"[{i + 1}] {quest.GetTargetDescription()} - {quest.GetDifficultyString()}", "white");
        }
        
        var input = await terminal.GetInput($"Select quest (1-{quests.Count}): ");
        if (int.TryParse(input, out int selection) && selection > 0 && selection <= quests.Count)
        {
            var quest = quests[selection - 1];
            if (QuestSystem.ClaimQuest(player, quest.Id))
            {
                terminal.WriteLine("Quest claimed successfully!", "green");
            }
            else
            {
                terminal.WriteLine("Failed to claim quest.", "red");
            }
        }
    }
    
    private async Task FinishQuest(Character player)
    {
        var quests = QuestSystem.GetPlayerQuests(player.Name2);
        if (quests.Count == 0)
        {
            terminal.WriteLine("You have no active quests.", "yellow");
            return;
        }
        
        terminal.WriteLine("Your Active Quests:", "cyan");
        for (int i = 0; i < quests.Count; i++)
        {
            var quest = quests[i];
            terminal.WriteLine($"[{i + 1}] {quest.GetTargetDescription()} - {quest.DaysRemaining} days left", "white");
        }
        
        var input = await terminal.GetInput($"Select quest to complete (1-{quests.Count}): ");
        if (int.TryParse(input, out int selection) && selection > 0 && selection <= quests.Count)
        {
            var quest = quests[selection - 1];
            terminal.WriteLine("You embark on your journey...", "white");
            await Task.Delay(1000);
            
            if (QuestSystem.CompleteQuest(player, quest.Id, terminal))
            {
                terminal.WriteLine("Quest completed successfully!", "green");
            }
            else
            {
                terminal.WriteLine("Failed to complete quest.", "red");
            }
        }
    }
    
    private async Task ShowStatus(Character player)
    {
        terminal.WriteLine("Quest Status:", "cyan");
        terminal.WriteLine($"Quests completed: {player.RoyQuests}", "white");
        terminal.WriteLine($"Quests today: {player.RoyQuestsToday}", "white");
        
        var active = QuestSystem.GetPlayerQuests(player.Name2);
        terminal.WriteLine($"Active quests: {active.Count}", "white");
        
        await terminal.PressAnyKey();
    }
} 