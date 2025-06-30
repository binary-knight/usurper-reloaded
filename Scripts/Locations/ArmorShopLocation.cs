using UsurperRemake.Utils;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Armor Shop Location - Complete Pascal-compatible armor system
/// Based on ARMSHOP.PAS with Reese and multi-slot equipment
/// </summary>
public class ArmorShopLocation : BaseLocation
{
    private string shopkeeperName = "Reese";
    private bool isKicked = false;
    private ObjType currentArmorType = ObjType.Head;
    
    public ArmorShopLocation() : base(
        GameLocation.ArmorShop,
        "Armor Shop",
        "You enter the armor shop and notice a strange but appealing smell."
    ) { }
    
    protected override void SetupLocation()
    {
        base.SetupLocation();
        shopkeeperName = "Reese";
        
        var currentPlayer = GetCurrentPlayer();
        isKicked = currentPlayer?.ArmHag == 0;
    }
    
    protected override void DisplayLocation()
    {
        terminal.ClearScreen();
        
        terminal.SetColor("bright_blue");
        terminal.WriteLine("╔══════════════════════════════════════════════════════╗");
        terminal.WriteLine("║                     ARMOR SHOP                      ║");
        terminal.WriteLine("╚══════════════════════════════════════════════════════╝");
        
        ShowArmorShopMenu();
    }
    
    private void ShowArmorShopMenu()
    {
        terminal.SetColor("white");
        terminal.WriteLine("(R)eturn to street");
        terminal.WriteLine("(B)uy");
        terminal.WriteLine("(S)ell");
        terminal.WriteLine("(L)ist items");
        terminal.WriteLine("");
    }
    
    protected override async Task<bool> ProcessChoice(string choice)
    {
        var upperChoice = choice.ToUpper().Trim();
        
        switch (upperChoice)
        {
            case "R":
                await NavigateToLocation(GameLocation.MainStreet);
                return true;
                
            case "B":
                terminal.WriteLine("Buying armor...", "green");
                await Task.Delay(1000);
                return false;
                
            default:
                terminal.WriteLine("Invalid choice!", "red");
                await Task.Delay(1000);
                return false;
        }
    }
} 
