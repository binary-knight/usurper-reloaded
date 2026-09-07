using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using UsurperRemake;
using UsurperRemake.Systems;
using Xunit;

namespace UsurperReborn.Tests;

/// <summary>
/// Design item D: the bank's robbery reserve is one persisted value per world, changed
/// through an atomic update online so two robbers cannot both take the same gold.
/// </summary>
[Collection("SharedGameSingletons")]
public class BankVaultTests : IDisposable
{
    /// <summary>An in-memory world_state row: the transform runs against the stored value, and
    /// the first <see cref="FailFirst"/> updates report a lost race without writing.</summary>
    private sealed class FakeStore : BankVaultSystem.IVaultStore
    {
        public string? Value;
        public int FailFirst;
        public bool FailAlways;
        public int Attempts;
        public Task<string?> Load(string key) => Task.FromResult(Value);
        public Task<bool> TryAtomicUpdate(string key, Func<string, string> transform)
        {
            Attempts++;
            var next = transform(Value ?? "");
            if (FailAlways || FailFirst-- > 0) return Task.FromResult(false);
            Value = next;
            return Task.FromResult(true);
        }
    }

    public BankVaultTests() { BankVaultSystem.StoreOverride = null; BankVaultSystem.Load(GameConfig.BankVaultInitial); }
    public void Dispose() { BankVaultSystem.StoreOverride = null; BankVaultSystem.Load(GameConfig.BankVaultInitial); }

    [Fact]
    public async Task SinglePlayer_DepositWithdrawAndFloor()
    {
        await BankVaultSystem.Deposit(100_000);
        BankVaultSystem.Current.Should().Be(600_000);
        await BankVaultSystem.Withdraw(700_000);
        BankVaultSystem.Current.Should().Be(0, "the reserve floors at zero");
    }

    [Theory]
    [InlineData(1_000_000, 200_000, 200_000)]   // a quarter of what is not the robber's own
    [InlineData(3_000_000, 0, 250_000)]          // capped
    [InlineData(100_000, 100_000, 0)]            // only the robber's own gold in there
    [InlineData(0, 0, 0)]
    public void RobberyTake_IsAQuarterOfOthersGold_Capped(long reserve, long own, long expected)
    {
        BankVaultSystem.RobberyTake(reserve, own).Should().Be(expected);
    }

    [Fact]
    public async Task DailyRefill_AddsFlatPlusPercent_UpToTheCap()
    {
        BankVaultSystem.Load(0);
        await BankVaultSystem.DailyRefill();
        BankVaultSystem.Current.Should().Be(25_000);
        BankVaultSystem.Load(4_990_000);
        await BankVaultSystem.DailyRefill();
        BankVaultSystem.Current.Should().Be(5_000_000, "the cap holds");
        BankVaultSystem.Load(1_000_000);
        await BankVaultSystem.DailyRefill();
        BankVaultSystem.Current.Should().Be(1_035_000, "25,000 flat plus one percent");
    }

    [Fact]
    public async Task Online_TwoRobbers_NeverTakeTheSameGold()
    {
        var store = new FakeStore { Value = "150000" };
        BankVaultSystem.StoreOverride = store;
        var (first, firstLanded) = await BankVaultSystem.Rob(0);
        var (second, secondLanded) = await BankVaultSystem.Rob(0);
        firstLanded.Should().BeTrue(); secondLanded.Should().BeTrue();
        first.Should().Be(37_500);
        second.Should().Be(28_125, "the second robber sees the reduced reserve, not a stale copy");
        store.Value.Should().Be("84375");
        BankVaultSystem.Current.Should().Be(84_375);
    }

    [Fact]
    public async Task Online_LostRace_IsRetriedAgainstTheFreshValue()
    {
        var store = new FakeStore { Value = "500000", FailFirst = 1 };
        BankVaultSystem.StoreOverride = store;
        await BankVaultSystem.Deposit(1);
        store.Attempts.Should().Be(2);
        store.Value.Should().Be("500001", "one deposit lands once");
    }

    [Fact]
    public async Task Online_RobberyThatNeverLands_PaysNothing()
    {
        var store = new FakeStore { Value = "1000000", FailAlways = true };
        BankVaultSystem.StoreOverride = store;
        var (stolen, landed) = await BankVaultSystem.Rob(0);
        landed.Should().BeFalse();
        stolen.Should().Be(0, "a robber who was not paid is not short, and the vault paid nobody");
        store.Value.Should().Be("1000000", "the row is untouched");
        store.Attempts.Should().Be(3);
    }

    [Fact]
    public async Task Online_DepositThatNeverLands_AppliesToTheCache()
    {
        var store = new FakeStore { Value = "500000", FailAlways = true };
        BankVaultSystem.StoreOverride = store;
        await BankVaultSystem.Refresh();
        await BankVaultSystem.Deposit(10);
        store.Value.Should().Be("500000", "the row could not be written");
        BankVaultSystem.Current.Should().Be(500_010, "the player's gold already moved, so the cached aggregate follows");
    }

    [Fact]
    public async Task Online_MissingRow_IsTheInitialReserve()
    {
        var store = new FakeStore { Value = null };
        BankVaultSystem.StoreOverride = store;
        await BankVaultSystem.Refresh();
        BankVaultSystem.Current.Should().Be(GameConfig.BankVaultInitial);
        await BankVaultSystem.Withdraw(1);
        store.Value.Should().Be("499999");
        store.Value = "0";
        await BankVaultSystem.Refresh();
        BankVaultSystem.Current.Should().Be(0, "a stored zero is a real value, not a missing row");
    }

    [Fact]
    public void WorldState_CarriesTheReserve_WithTheInitialAsLegacyDefault()
    {
        var legacy = System.Text.Json.JsonSerializer.Deserialize<WorldStateData>("{}")!;
        legacy.BankVaultReserve.Should().Be(GameConfig.BankVaultInitial);
        var saved = System.Text.Json.JsonSerializer.Deserialize<WorldStateData>("{\"BankVaultReserve\":0}")!;
        saved.BankVaultReserve.Should().Be(0);
    }
}
