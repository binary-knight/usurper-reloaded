using Godot;
using System;
using System.Threading.Tasks;
using UsurperRemake.Utils;

namespace UsurperRemake.Locations
{
    /// <summary>
    /// Dark Alley – the shady district featuring black-market style services.
    /// Inspired by SHADY.PAS from the original Usurper.
    /// </summary>
    public class DarkAlleyLocation : BaseLocation
    {
        public DarkAlleyLocation() : base(GameLocation.DarkAlley, "Dark Alley",
            "You stumble into a dimly-lit back street where questionable vendors ply their trade.")
        {
        }

        protected override void SetupLocation()
        {
            PossibleExits.Add(GameLocation.MainStreet);
        }

        protected override async Task<bool> ProcessChoice(string choice)
        {
            switch (choice.ToUpperInvariant())
            {
                case "D":
                    await VisitDrugPalace();
                    return false;
                case "S":
                    await VisitSteroidShop();
                    return false;
                case "O":
                    await VisitOrbsHealthClub();
                    return false;
                case "G":
                    await VisitGroggoMagic();
                    return false;
                case "B":
                    await VisitBeerHut();
                    return false;
                case "A":
                    await VisitAlchemistHeaven();
                    return false;
                case "Q":
                case "R":
                    await NavigateToLocation(GameLocation.MainStreet);
                    return true;
                default:
                    return await base.ProcessChoice(choice);
            }
        }

        protected override void DisplayLocation()
        {
            terminal.ClearScreen();

            terminal.SetColor("bright_magenta");
            terminal.WriteLine("╔══════════════════════════════════════════════╗");
            terminal.WriteLine("║                THE DARK ALLEY               ║");
            terminal.WriteLine("╚══════════════════════════════════════════════╝");
            terminal.WriteLine("");

            terminal.SetColor("gray");
            terminal.WriteLine("Torches sputter in the moist air and the smell of trash " +
                                "mixes with exotic spices.  Whispers of illicit trade echo " +
                                "between crooked doorways.");
            terminal.WriteLine("");

            terminal.SetColor("yellow");
            terminal.WriteLine("Shady establishments:");
            terminal.SetColor("green");
            terminal.WriteLine("(D) Drug Palace – questionable stimulants");
            terminal.WriteLine("(S) Steroid Shop – quick bulk for a price");
            terminal.WriteLine("(O) Orbs Health Club – mysterious healing orbs");
            terminal.WriteLine("(G) Groggo's Magic Services – black-market scrolls");
            terminal.WriteLine("(B) Bob's Beer Hut – cheap booze and rumors");
            terminal.WriteLine("(A) Alchemist's Heaven – experimental potions");
            terminal.WriteLine("(Q) Return to Main Street");
            terminal.WriteLine("");

            ShowStatusLine();
        }

        #region Individual shop handlers

        private async Task VisitDrugPalace()
        {
            terminal.WriteLine("");
            terminal.WriteLine("You enter a smoky den lined with velvet curtains.", "white");
            long price = GD.RandRange(250, 750);
            terminal.WriteLine($"A shady dealer offers a glittering packet for {price:N0} {GameConfig.MoneyType}.", "cyan");
            var ans = await terminal.GetInput("Buy it? (Y/N): ");
            if (ans.ToUpper() != "Y") return;

            if (currentPlayer.Gold < price)
            {
                terminal.WriteLine("You don't have that kind of cash!", "red");
                await Task.Delay(1500);
                return;
            }

            currentPlayer.Gold -= price;
            currentPlayer.Addict += 1;
            currentPlayer.Darkness += 5;
            currentPlayer.Stamina = Math.Max(1, currentPlayer.Stamina - 2);
            currentPlayer.Strength += 2;

            terminal.WriteLine("You feel an intense rush coursing through your veins…", "bright_green");
            await Task.Delay(2000);
        }

        private async Task VisitSteroidShop()
        {
            terminal.WriteLine("");
            terminal.WriteLine("A muscular dwarf guards crates of suspicious vials.", "white");
            const long price = 1000;
            terminal.WriteLine($"Bulk-up serum costs {price:N0} {GameConfig.MoneyType}.", "cyan");
            var ans = await terminal.GetInput("Inject? (Y/N): ");
            if (ans.ToUpper() != "Y") return;

            if (currentPlayer.Gold < price)
            {
                terminal.WriteLine("You can't afford that!", "red");
                await Task.Delay(1500);
                return;
            }

            currentPlayer.Gold -= price;
            currentPlayer.Strength += 5;
            currentPlayer.Stamina += 3;
            currentPlayer.Darkness += 3;

            terminal.WriteLine("Your muscles swell unnaturally!", "bright_green");
            await Task.Delay(2000);
        }

        private async Task VisitOrbsHealthClub()
        {
            terminal.WriteLine("");
            terminal.WriteLine("A hooded cleric guides you to glowing orbs floating in a pool.", "white");
            long price = currentPlayer.Level * 50 + 100;
            terminal.WriteLine($"Restoring vitality costs {price:N0} {GameConfig.MoneyType}.", "cyan");
            var ans = await terminal.GetInput("Pay? (Y/N): ");
            if (ans.ToUpper() != "Y") return;

            if (currentPlayer.Gold < price)
            {
                terminal.WriteLine("Insufficient gold.", "red");
                await Task.Delay(1500);
                return;
            }

            currentPlayer.Gold -= price;
            currentPlayer.HP = currentPlayer.MaxHP;
            terminal.WriteLine("Warm light knits your wounds together – you are fully healed!", "bright_green");
            await Task.Delay(2000);
        }

        private async Task VisitGroggoMagic()
        {
            terminal.WriteLine("");
            terminal.WriteLine("The infamous gnome Groggo grins widely behind a cluttered desk.", "white");
            terminal.WriteLine("\"I sell scrolls, charms, secrets – but nothing is cheap!\"", "yellow");
            long price = 750;
            terminal.WriteLine($"A basic identification scroll costs {price:N0} {GameConfig.MoneyType}.");
            var ans = await terminal.GetInput("Purchase? (Y/N): ");
            if (ans.ToUpper() != "Y") return;

            if (currentPlayer.Gold < price)
            {
                terminal.WriteLine("Groggo scoffs. \"Come back with real money!\"", "red");
                await Task.Delay(1500);
                return;
            }
            currentPlayer.Gold -= price;
            terminal.WriteLine("You receive a crumpled parchment covered in runes.", "bright_green");
            await Task.Delay(2000);
        }

        private async Task VisitBeerHut()
        {
            terminal.WriteLine("");
            terminal.WriteLine("Bob hands you a frothy mug that smells vaguely of goblin sweat.", "white");
            const long price = 25;
            if (currentPlayer.Gold < price)
            {
                terminal.WriteLine("Bob laughs, \"Pay first, friend!\"", "red");
                await Task.Delay(1500);
                return;
            }
            currentPlayer.Gold -= price;
            currentPlayer.Stamina = Math.Max(1, currentPlayer.Stamina - 1);
            terminal.WriteLine("It burns going down, but courage fills your heart!", "bright_green");
            await Task.Delay(1500);
        }

        private async Task VisitAlchemistHeaven()
        {
            terminal.WriteLine("");
            terminal.WriteLine("Shelves of bubbling concoctions line the walls.", "white");
            long price = 300;
            terminal.WriteLine($"A random experimental potion costs {price:N0} {GameConfig.MoneyType}.", "cyan");
            var ans = await terminal.GetInput("Buy? (Y/N): ");
            if (ans.ToUpper() != "Y") return;

            if (currentPlayer.Gold < price)
            {
                terminal.WriteLine("The alchemist shakes his head – no credit.", "red");
                await Task.Delay(1500);
                return;
            }

            currentPlayer.Gold -= price;
            int effect = GD.RandRange(1, 3);
            switch (effect)
            {
                case 1:
                    currentPlayer.Intelligence += 2;
                    terminal.WriteLine("Your mind feels sharper! (+2 INT)", "bright_green");
                    break;
                case 2:
                    currentPlayer.HP = Math.Min(currentPlayer.MaxHP, currentPlayer.HP + 20);
                    terminal.WriteLine("A warm glow mends some wounds. (+20 HP)", "bright_green");
                    break;
                default:
                    currentPlayer.Darkness += 2;
                    terminal.WriteLine("The potion fizzles nastily… you feel uneasy. (+2 Darkness)", "yellow");
                    break;
            }
            await Task.Delay(2000);
        }

        #endregion
    }
} 