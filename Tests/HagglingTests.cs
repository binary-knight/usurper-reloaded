using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using UsurperRemake;
using UsurperRemake.Systems;
using Xunit;

namespace UsurperReborn.Tests;

/// <summary>
/// Design item C: haggling gets an entry point. The Pascal acceptance rule is deterministic
/// (offer at least 80 percent of the price, discount within the Charisma tier), attempts
/// persist, and being thrown out is a day-stamped bar rather than "attempts == 0".
/// </summary>
[Collection("SharedGameSingletons")]
public class HagglingTests
{
    private static Character WithCharisma(int charisma) => new Character
    {
        Name1 = "hag", Name2 = "Hag", Class = CharacterClass.Warrior, Race = CharacterRace.Human, Charisma = charisma,
    };

    [Theory]
    [InlineData(100, 1000, 900, true)]   // 10% at the 76-125 tier (10%)
    [InlineData(100, 1000, 890, false)]  // 11% exceeds the tier
    [InlineData(100, 1000, 901, true)]   // 9.9%: the old truncation and the exact rule agree here
    [InlineData(100, 1000, 899, false)]  // 10.1%: the old (int) truncation passed this as 10%
    [InlineData(250, 1000, 800, true)]   // 20% ceiling at 201+
    [InlineData(250, 1000, 790, false)]  // below the 80% floor even at max Charisma
    [InlineData(10, 1000, 960, true)]    // 4% at the lowest tier
    [InlineData(10, 1000, 950, false)]
    [InlineData(100, 1000, 1000, false)] // not a discount
    public void AcceptanceRule_IsExactAndTiered(int charisma, long price, long offer, bool expected)
    {
        HagglingEngine.CalculateHagglingSuccess(WithCharisma(charisma), price, offer).Should().Be(expected);
    }

    [Fact]
    public void AcceptanceRule_DoesNotOverflowOnLargePrices()
    {
        long price = 5_000_000_000_000L;
        HagglingEngine.CalculateHagglingSuccess(WithCharisma(250), price, price - price / 5).Should().BeTrue();
        HagglingEngine.CalculateHagglingSuccess(WithCharisma(250), price, price - price / 5 - 1).Should().BeFalse();
    }

    [Fact]
    public void Attempts_GateHaggling_AndTheBarIsSeparate()
    {
        var p = WithCharisma(100);
        HagglingEngine.CanHaggle(p, HagglingEngine.ShopType.Weapon).Should().BeTrue();
        p.WeapHag = 0;
        HagglingEngine.CanHaggle(p, HagglingEngine.ShopType.Weapon).Should().BeFalse();
        p.IsBarredFromWeaponShop(currentDay: 5).Should().BeFalse("spending the attempts must not bar the shop");
        p.WeaponShopBarredUntilDay = 6;
        p.IsBarredFromWeaponShop(5).Should().BeTrue();
        p.IsBarredFromWeaponShop(6).Should().BeFalse("the bar expires with the day");
    }

    [Fact]
    public void DailyReset_RestoresAttempts_AndClearsTheBar()
    {
        var p = WithCharisma(100) ;
        p.WeapHag = 0; p.ArmHag = 1; p.WeaponShopBarredUntilDay = 9; p.ArmorShopBarredUntilDay = 9;
        HagglingEngine.ResetDailyHaggling(p);
        p.WeapHag.Should().Be(3); p.ArmHag.Should().Be(3);
        p.WeaponShopBarredUntilDay.Should().Be(0); p.ArmorShopBarredUntilDay.Should().Be(0);
    }

    [Fact]
    public void AttemptsAndBars_RoundTripThroughTheSave()
    {
        var p = WithCharisma(100);
        p.WeapHag = 1; p.ArmHag = 2; p.WeaponShopBarredUntilDay = 12; p.ArmorShopBarredUntilDay = 0;
        p.Level = 3; p.HP = 30; p.MaxHP = 30; p.BaseMaxHP = 30;

        var serialize = typeof(SaveSystem).GetMethod("SerializePlayer", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var data = (PlayerData)serialize.Invoke(SaveSystem.Instance, new object[] { p })!;
        var back = JsonSerializer.Deserialize<PlayerData>(JsonSerializer.Serialize(data))!;
        back.WeapHag.Should().Be(1); back.ArmHag.Should().Be(2); back.WeaponShopBarredUntilDay.Should().Be(12);

        var restore = typeof(GameEngine).GetMethod("RestorePlayerFromSaveData", BindingFlags.NonPublic | BindingFlags.Instance)!;
        Character restored;
        try { restored = (Character)restore.Invoke(GameEngine.Instance, new object[] { back })!; }
        catch (TargetInvocationException ex) when (ex.InnerException != null) { throw ex.InnerException; }
        restored.WeapHag.Should().Be(1); restored.ArmHag.Should().Be(2);
        restored.WeaponShopBarredUntilDay.Should().Be(12); restored.ArmorShopBarredUntilDay.Should().Be(0);
    }

    [Fact]
    public void LegacySave_WithoutTheFields_GetsThreeAttemptsAndNoBar()
    {
        var back = JsonSerializer.Deserialize<PlayerData>("{}")!;
        back.WeapHag.Should().Be(3); back.ArmHag.Should().Be(3); back.WeaponShopBarredUntilDay.Should().Be(0);
    }
}
