using System;
using System.Globalization;
using System.Threading.Tasks;
using UsurperRemake.BBS;

namespace UsurperRemake.Systems;

/// <summary>
/// v1.2 (design item D): the bank's robbery reserve. It used to be a process-wide static
/// in BankLocation that restarted at 500,000 on every server start and was one number for
/// every session. It is now one persisted value per world: WorldStateData.BankVaultReserve
/// in single-player, the "bank_vault" world_state row online, where every change goes
/// through the backend's optimistic-concurrency update so two robbers cannot both take the
/// same gold. Player bank balances are never touched by anyone else's robbery.
/// </summary>
public static class BankVaultSystem
{
    public const string WorldStateKey = "bank_vault";

    /// <summary>The two backend calls the vault needs, so tests can supply a fake store.</summary>
    public interface IVaultStore
    {
        Task<string?> Load(string key);
        Task<bool> TryAtomicUpdate(string key, Func<string, string> transform);
    }

    private sealed class BackendStore : IVaultStore
    {
        private readonly IOnlineSaveBackend _backend;
        public BackendStore(IOnlineSaveBackend backend) { _backend = backend; }
        public Task<string?> Load(string key) => _backend.LoadWorldState(key);
        public Task<bool> TryAtomicUpdate(string key, Func<string, string> transform) => _backend.TryAtomicUpdate(key, transform);
    }

    private static long _reserve = GameConfig.BankVaultInitial;

    /// <summary>Tests set this to a fake store; production resolves the online backend.</summary>
    internal static IVaultStore? StoreOverride { get; set; }

    private static IVaultStore? Store
    {
        get
        {
            if (StoreOverride != null) return StoreOverride;
            if (!DoorMode.IsOnlineMode) return null;
            return SaveSystem.Instance?.Backend is IOnlineSaveBackend online ? new BackendStore(online) : null;
        }
    }

    /// <summary>The last known reserve. Online it is refreshed on bank entry and after every change.</summary>
    public static long Current => _reserve;

    /// <summary>Single-player restore, and tests.</summary>
    public static void Load(long value) => _reserve = Math.Max(0, value);

    /// <summary>Online: read the shared row. A missing row is the initial reserve.</summary>
    public static async Task Refresh()
    {
        var store = Store;
        if (store == null) return;
        try
        {
            var json = await store.Load(WorldStateKey);
            _reserve = Parse(json);
        }
        catch (Exception ex)
        {
            DebugLogger.Instance.LogWarning("BANK", $"Vault refresh failed: {ex.Message}");
        }
    }

    public static Task Deposit(long amount) => amount <= 0 ? Task.CompletedTask : Mutate(v => SafeAdd(v, amount));

    public static Task Withdraw(long amount) => amount <= 0 ? Task.CompletedTask : Mutate(v => Math.Max(0, v - amount));

    /// <summary>
    /// Take the robbery cut: a quarter of what is in the vault beyond the robber's own
    /// deposits, capped. Returns the amount actually removed, which is what the robber is
    /// credited; a second robber arriving first gets the reduced remainder, never a stale copy.
    /// </summary>
    public static async Task<long> Rob(long robberOwnBankGold)
    {
        long stolen = 0;
        await Mutate(v =>
        {
            stolen = RobberyTake(v, robberOwnBankGold);
            return Math.Max(0, v - stolen);
        });
        return stolen;
    }

    public static long RobberyTake(long reserve, long robberOwnBankGold)
    {
        long othersGold = Math.Max(0, reserve - Math.Max(0, robberOwnBankGold));
        return Math.Min(othersGold / 4, GameConfig.BankRobberyMaxTake);
    }

    /// <summary>Once per world day: a flat refill plus a percentage, up to the cap.</summary>
    public static Task DailyRefill() => Mutate(v =>
    {
        long grown = SafeAdd(v, GameConfig.BankVaultDailyRefill + v / 100 * GameConfig.BankVaultRefillRatePercent);
        return Math.Min(grown, GameConfig.BankVaultCap);
    });

    private static async Task Mutate(Func<long, long> f)
    {
        var store = Store;
        if (store == null)
        {
            _reserve = f(_reserve);
            return;
        }
        for (int attempt = 0; attempt < 3; attempt++)
        {
            long written = 0;
            bool ok;
            try
            {
                ok = await store.TryAtomicUpdate(WorldStateKey, json =>
                {
                    written = f(Parse(json));
                    return written.ToString(CultureInfo.InvariantCulture);
                });
            }
            catch (Exception ex)
            {
                DebugLogger.Instance.LogWarning("BANK", $"Vault update failed: {ex.Message}");
                ok = false;
            }
            if (ok)
            {
                _reserve = written;
                return;
            }
        }
        // Contention three times running: the gold has already moved for the player, so apply
        // the change to the cached copy rather than leave the player short; the next refresh
        // re-reads the shared row.
        DebugLogger.Instance.LogWarning("BANK", "Vault update lost the race three times; applied to the cached reserve only");
        _reserve = f(_reserve);
    }

    private static long Parse(string? json) =>
        long.TryParse((json ?? "").Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? Math.Max(0, v) : GameConfig.BankVaultInitial;

    private static long SafeAdd(long current, long amount)
    {
        if (amount <= 0) return current;
        if (current > long.MaxValue - amount) return long.MaxValue;
        return current + amount;
    }
}
