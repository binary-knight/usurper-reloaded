using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace UsurperRemake.Systems
{
    /// <summary>
    /// v1.1.4: the world boss rows. Every shared field is written by one guarded UPDATE that
    /// carries its own precondition and reports its row count; nothing here read-edit-writes
    /// boss_data_json. See DOCS/WORLD_BOSS_PLAN.md, "State" and "Invariants".
    /// </summary>
    public partial class SqlSaveBackend
    {
        private static readonly string[] WorldBossColumns =
        {
            "def_id TEXT DEFAULT ''", "scheduled_at TEXT", "window_hours INTEGER DEFAULT 3", "nights INTEGER DEFAULT 1",
            "median_level INTEGER DEFAULT 0", "online_at_spawn INTEGER DEFAULT 0", "first_hit_at TEXT", "last_damaged_at TEXT",
            "killed_at TEXT", "ended_at TEXT", "hp_at_end INTEGER DEFAULT 0", "peak_engaged INTEGER DEFAULT 0",
            "regen_total INTEGER DEFAULT 0", "phase INTEGER DEFAULT 1", "settled INTEGER DEFAULT 0",
            // milestone B, shipped in the same migration so the schema changes once
            "telegraph_id TEXT", "telegraph_seq INTEGER DEFAULT 0", "telegraph_lands_at TEXT",
            "interrupts_needed INTEGER DEFAULT 0", "interrupts_done INTEGER DEFAULT 0", "stagger_until TEXT",
            "focus_player TEXT", "focus_until TEXT", "window_started_at TEXT",
            // v1.1.5
            "last_resolved_seq INTEGER DEFAULT 0", "last_resolved_outcome TEXT DEFAULT ''",
        };

        private static readonly string[] WorldBossDamageColumns =
        {
            "player_level INTEGER DEFAULT 0", "sessions INTEGER DEFAULT 0", "rounds INTEGER DEFAULT 0", "deaths INTEGER DEFAULT 0",
            "night_damage INTEGER DEFAULT 0", "window_damage INTEGER DEFAULT 0", "paid_nights INTEGER DEFAULT 0",
            "cooldown_until TEXT", "engaged_since_seq INTEGER DEFAULT 0", "last_resolved_seq INTEGER DEFAULT 0",
            "answers INTEGER DEFAULT 0", "is_npc INTEGER DEFAULT 0", "display_name TEXT DEFAULT ''",
            // v1.1.5
            "answered_seq INTEGER DEFAULT 0", "answer_kind TEXT DEFAULT ''", "engaged_until_seq INTEGER DEFAULT 0",
        };

        private static void MigrateWorldBossTables(SqliteConnection connection)
        {
            foreach (var col in WorldBossColumns)
            {
                try { using var c = connection.CreateCommand(); c.CommandText = $"ALTER TABLE world_bosses ADD COLUMN {col};"; c.ExecuteNonQuery(); }
                catch { /* column already exists */ }
            }
            foreach (var col in WorldBossDamageColumns)
            {
                try { using var c = connection.CreateCommand(); c.CommandText = $"ALTER TABLE world_boss_damage ADD COLUMN {col};"; c.ExecuteNonQuery(); }
                catch { /* column already exists */ }
            }
            using var t = connection.CreateCommand();
            t.CommandText = @"
                CREATE TABLE IF NOT EXISTS world_boss_rewards (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    boss_id INTEGER NOT NULL,
                    boss_name TEXT DEFAULT '',
                    player_name TEXT NOT NULL,
                    night INTEGER NOT NULL DEFAULT 1,
                    kind TEXT NOT NULL DEFAULT 'kill',
                    xp INTEGER DEFAULT 0,
                    gold INTEGER DEFAULT 0,
                    fame INTEGER DEFAULT 0,
                    item_json TEXT DEFAULT '',
                    rarity INTEGER DEFAULT 0,
                    marks INTEGER DEFAULT 0,
                    score REAL DEFAULT 0,
                    mvp INTEGER DEFAULT 0,
                    damage_dealt INTEGER DEFAULT 0,
                    settled_at TEXT DEFAULT (datetime('now')),
                    delivered INTEGER DEFAULT 0,
                    delivered_at TEXT,
                    UNIQUE(boss_id, player_name, kind, night)
                );
                CREATE INDEX IF NOT EXISTS idx_world_boss_rewards_player ON world_boss_rewards(player_name, delivered);
                CREATE TABLE IF NOT EXISTS world_boss_events (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    boss_id INTEGER NOT NULL,
                    seq INTEGER DEFAULT 0,
                    kind TEXT NOT NULL,
                    player_name TEXT DEFAULT '',
                    detail TEXT DEFAULT '',
                    at TEXT DEFAULT (datetime('now'))
                );
                CREATE INDEX IF NOT EXISTS idx_world_boss_events_boss ON world_boss_events(boss_id, kind);";
            t.ExecuteNonQuery();
        }

        private const string WorldBossSelect = @"SELECT id, boss_name, boss_level, max_hp, current_hp, started_at, expires_at, boss_data_json,
                   status, COALESCE(phase, 1), COALESCE(nights, 1), COALESCE(def_id, ''), last_damaged_at, COALESCE(settled, 0), COALESCE(median_level, 0),
                   COALESCE(telegraph_id, ''), COALESCE(telegraph_seq, 0), telegraph_lands_at, COALESCE(interrupts_needed, 0), COALESCE(interrupts_done, 0),
                   stagger_until, COALESCE(focus_player, ''), focus_until, COALESCE(last_resolved_seq, 0), COALESCE(last_resolved_outcome, '')
            FROM world_bosses ";

        private static DateTime? ReadUtc(SqliteDataReader reader, int i) =>
            reader.IsDBNull(i) ? null
            : DateTime.TryParse(reader.GetString(i), null, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out var d) ? d : null;

        private static WorldBossInfo ReadWorldBoss(SqliteDataReader reader) => new WorldBossInfo
        {
            Id = reader.GetInt32(0),
            BossName = reader.GetString(1),
            BossLevel = reader.GetInt32(2),
            MaxHP = reader.GetInt64(3),
            CurrentHP = reader.GetInt64(4),
            StartedAt = DateTime.TryParse(reader.GetString(5), null, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out var st) ? st : DateTime.UtcNow,
            ExpiresAt = DateTime.TryParse(reader.GetString(6), null, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out var et) ? et : DateTime.UtcNow,
            BossDataJson = reader.IsDBNull(7) ? "{}" : reader.GetString(7),
            Status = reader.IsDBNull(8) ? "active" : reader.GetString(8),
            Phase = reader.GetInt32(9),
            Nights = reader.GetInt32(10),
            DefinitionId = reader.GetString(11),
            LastDamagedAt = reader.IsDBNull(12) ? null : (DateTime.TryParse(reader.GetString(12), null, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out var ld) ? ld : null),
            Settled = reader.GetInt32(13) != 0,
            MedianLevel = reader.GetInt32(14),
            TelegraphId = reader.GetString(15),
            TelegraphSeq = reader.GetInt64(16),
            TelegraphLandsAt = ReadUtc(reader, 17),
            InterruptsNeeded = reader.GetInt32(18),
            InterruptsDone = reader.GetInt32(19),
            StaggerUntil = ReadUtc(reader, 20),
            FocusPlayer = reader.GetString(21),
            FocusUntil = ReadUtc(reader, 22),
            LastResolvedSeq = reader.GetInt64(23),
            LastResolvedOutcome = reader.GetString(24),
        };

        private async Task<WorldBossInfo?> QueryOneWorldBoss(string where, params (string name, object value)[] args)
        {
            try
            {
                using var connection = OpenConnection();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = WorldBossSelect + where + " LIMIT 1;";
                foreach (var (name, value) in args) cmd.Parameters.AddWithValue(name, value);
                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync()) return ReadWorldBoss((SqliteDataReader)reader);
            }
            catch (Exception ex) { DebugLogger.Instance.LogError("SQL", $"World boss query failed: {ex.Message}"); }
            return null;
        }

        /// <summary>The active row whether or not its window has passed; the tick decides what a passed window means.</summary>
        public Task<WorldBossInfo?> GetActiveWorldBossAnyTime() =>
            QueryOneWorldBoss("WHERE status = 'active' ORDER BY started_at DESC");

        public Task<WorldBossInfo?> GetWithdrawnWorldBoss() =>
            QueryOneWorldBoss("WHERE status = 'withdrawn' ORDER BY started_at DESC");

        public Task<WorldBossInfo?> GetWorldBossById(int bossId) =>
            QueryOneWorldBoss("WHERE id = @id", ("@id", bossId));

        /// <summary>Spawn with the v1.1.4 columns filled. Window and expiry are hours from now.</summary>
        public async Task<int> SpawnScheduledWorldBoss(string bossName, int bossLevel, long maxHp, int windowHours,
            string bossDataJson, string defId, DateTime scheduledAtUtc, int medianLevel, int onlineAtSpawn)
        {
            try
            {
                using var connection = OpenConnection();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"INSERT INTO world_bosses (boss_name, boss_level, max_hp, current_hp, boss_data_json, expires_at,
                                        def_id, scheduled_at, window_hours, nights, median_level, online_at_spawn, phase, window_started_at)
                                    VALUES (@name, @level, @hp, @hp, @json, datetime('now', '+' || @hours || ' hours'),
                                        @def, @sched, @hours, 1, @median, @online, 1, datetime('now'))
                                    RETURNING id;";
                cmd.Parameters.AddWithValue("@name", bossName);
                cmd.Parameters.AddWithValue("@level", bossLevel);
                cmd.Parameters.AddWithValue("@hp", maxHp);
                cmd.Parameters.AddWithValue("@json", bossDataJson);
                cmd.Parameters.AddWithValue("@hours", windowHours);
                cmd.Parameters.AddWithValue("@def", defId);
                cmd.Parameters.AddWithValue("@sched", scheduledAtUtc.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@median", medianLevel);
                cmd.Parameters.AddWithValue("@online", onlineAtSpawn);
                return Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }
            catch (Exception ex) { DebugLogger.Instance.LogError("SQL", $"Failed to spawn scheduled world boss: {ex.Message}"); return -1; }
        }

        /// <summary>
        /// Apply damage to the shared pool. Credited only when the boss row is active and inside its
        /// window; clamped to the HP that was actually there. Returns the remaining HP, whether this
        /// call won the kill claim (the status flip), and the damage actually applied (0 when not credited).
        /// </summary>
        public async Task<(long remainingHp, bool wasKillingBlow, long applied)> RecordWorldBossDamage(int bossId, string playerName, long damage, int playerLevel, string displayName = "")
        {
            long remainingHp = 0; bool wasKillingBlow = false; long applied = 0;
            if (damage <= 0) return (remainingHp, wasKillingBlow, applied);
            try
            {
                using var connection = OpenConnection();
                using var transaction = connection.BeginTransaction();

                long before;
                using (var readCmd = connection.CreateCommand())
                {
                    readCmd.Transaction = transaction;
                    readCmd.CommandText = "SELECT current_hp FROM world_bosses WHERE id = @id AND status = 'active' AND expires_at > datetime('now');";
                    readCmd.Parameters.AddWithValue("@id", bossId);
                    var r = readCmd.ExecuteScalar();
                    if (r == null || r == DBNull.Value) { transaction.Commit(); return (0, false, 0); }
                    before = Convert.ToInt64(r);
                }
                applied = Math.Min(damage, before);
                if (applied <= 0) { transaction.Commit(); return (0, false, 0); }

                int updated;
                using (var updateCmd = connection.CreateCommand())
                {
                    updateCmd.Transaction = transaction;
                    updateCmd.CommandText = @"UPDATE world_bosses SET current_hp = MAX(0, current_hp - @damage),
                                                  last_damaged_at = datetime('now'),
                                                  first_hit_at = COALESCE(first_hit_at, datetime('now'))
                                              WHERE id = @id AND status = 'active' AND expires_at > datetime('now');";
                    updateCmd.Parameters.AddWithValue("@id", bossId);
                    updateCmd.Parameters.AddWithValue("@damage", applied);
                    updated = await updateCmd.ExecuteNonQueryAsync();
                }
                if (updated != 1) { transaction.Commit(); return (0, false, 0); }

                using (var dmgCmd = connection.CreateCommand())
                {
                    dmgCmd.Transaction = transaction;
                    dmgCmd.CommandText = @"INSERT INTO world_boss_damage (boss_id, player_name, damage_dealt, hits, player_level, night_damage, window_damage, display_name)
                                          VALUES (@bossId, LOWER(@player), @damage, 1, @level, @damage, @damage, @display)
                                          ON CONFLICT(boss_id, player_name) DO UPDATE SET
                                              damage_dealt = damage_dealt + @damage,
                                              display_name = CASE WHEN @display <> '' THEN @display ELSE display_name END,
                                              night_damage = COALESCE(night_damage, 0) + @damage,
                                              window_damage = COALESCE(window_damage, 0) + @damage,
                                              hits = hits + 1,
                                              player_level = CASE WHEN @level > 0 THEN @level ELSE player_level END,
                                              last_hit_at = datetime('now');";
                    dmgCmd.Parameters.AddWithValue("@bossId", bossId);
                    dmgCmd.Parameters.AddWithValue("@player", playerName);
                    dmgCmd.Parameters.AddWithValue("@damage", applied);
                    dmgCmd.Parameters.AddWithValue("@level", playerLevel);
                    dmgCmd.Parameters.AddWithValue("@display", displayName ?? "");
                    await dmgCmd.ExecuteNonQueryAsync();
                }

                using (var hpCmd = connection.CreateCommand())
                {
                    hpCmd.Transaction = transaction;
                    hpCmd.CommandText = "SELECT current_hp FROM world_bosses WHERE id = @id;";
                    hpCmd.Parameters.AddWithValue("@id", bossId);
                    remainingHp = Convert.ToInt64(hpCmd.ExecuteScalar() ?? 0);
                }

                if (remainingHp <= 0)
                {
                    using var defeatCmd = connection.CreateCommand();
                    defeatCmd.Transaction = transaction;
                    defeatCmd.CommandText = @"UPDATE world_bosses SET status = 'defeated', killed_at = datetime('now'), ended_at = datetime('now'), hp_at_end = 0
                                              WHERE id = @id AND status = 'active';";
                    defeatCmd.Parameters.AddWithValue("@id", bossId);
                    wasKillingBlow = await defeatCmd.ExecuteNonQueryAsync() == 1;
                }
                transaction.Commit();
            }
            catch (Exception ex) { DebugLogger.Instance.LogError("SQL", $"Failed to record world boss damage: {ex.Message}"); }
            return (remainingHp, wasKillingBlow, applied);
        }

        /// <summary>One session ended: counts and the re-entry cooldown on the player's own row.</summary>
        public async Task RecordWorldBossSession(int bossId, string playerName, int playerLevel, int rounds, bool fell, int cooldownSeconds, string displayName = "", long exitSeq = 0)
        {
            try
            {
                using var connection = OpenConnection();
                using var cmd = connection.CreateCommand();
                // last_hit_at is NULL here on purpose: a session without a hit is not "engaged"
                cmd.CommandText = @"INSERT INTO world_boss_damage (boss_id, player_name, damage_dealt, hits, player_level, sessions, rounds, deaths, cooldown_until, last_hit_at, display_name, engaged_until_seq)
                                    VALUES (@bossId, LOWER(@player), 0, 0, @level, 1, @rounds, @deaths, datetime('now', '+' || @secs || ' seconds'), NULL, @display, @exit)
                                    ON CONFLICT(boss_id, player_name) DO UPDATE SET
                                        sessions = COALESCE(sessions, 0) + 1,
                                        engaged_until_seq = @exit,
                                        last_hit_at = NULL,
                                        display_name = CASE WHEN @display <> '' THEN @display ELSE display_name END,
                                        rounds = COALESCE(rounds, 0) + @rounds,
                                        deaths = COALESCE(deaths, 0) + @deaths,
                                        player_level = CASE WHEN @level > 0 THEN @level ELSE player_level END,
                                        cooldown_until = datetime('now', '+' || @secs || ' seconds');";
                cmd.Parameters.AddWithValue("@bossId", bossId);
                cmd.Parameters.AddWithValue("@player", playerName);
                cmd.Parameters.AddWithValue("@level", playerLevel);
                cmd.Parameters.AddWithValue("@rounds", rounds);
                cmd.Parameters.AddWithValue("@deaths", fell ? 1 : 0);
                cmd.Parameters.AddWithValue("@secs", cooldownSeconds);
                cmd.Parameters.AddWithValue("@exit", exitSeq);
                cmd.Parameters.AddWithValue("@display", displayName ?? "");
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex) { DebugLogger.Instance.LogError("SQL", $"Failed to record world boss session: {ex.Message}"); }
        }

        /// <summary>Seconds left on the player's re-entry cooldown, 0 when none.</summary>
        public int GetWorldBossCooldownSeconds(int bossId, string playerName)
        {
            try
            {
                using var connection = OpenConnection();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"SELECT CAST(MAX(0, (julianday(cooldown_until) - julianday('now')) * 86400) AS INTEGER)
                                    FROM world_boss_damage WHERE boss_id = @id AND player_name = LOWER(@player) AND cooldown_until IS NOT NULL;";
                cmd.Parameters.AddWithValue("@id", bossId);
                cmd.Parameters.AddWithValue("@player", playerName);
                var r = cmd.ExecuteScalar();
                return r == null || r == DBNull.Value ? 0 : Convert.ToInt32(r);
            }
            catch { return 0; }
        }

        /// <summary>Humans with a hit inside the last N minutes: the "engaged" count.</summary>
        public int GetWorldBossEngagedCount(int bossId, int minutes)
        {
            try
            {
                using var connection = OpenConnection();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"SELECT COUNT(*) FROM world_boss_damage WHERE boss_id = @id AND COALESCE(is_npc, 0) = 0
                                    AND last_hit_at > datetime('now', '-' || @m || ' minutes');";
                cmd.Parameters.AddWithValue("@id", bossId);
                cmd.Parameters.AddWithValue("@m", minutes);
                return Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
            }
            catch { return 0; }
        }

        private async Task<bool> GuardedWorldBossUpdate(string sql, params (string name, object value)[] args)
        {
            try
            {
                using var connection = OpenConnection();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;
                foreach (var (name, value) in args) cmd.Parameters.AddWithValue(name, value);
                return await cmd.ExecuteNonQueryAsync() == 1;
            }
            catch (Exception ex) { DebugLogger.Instance.LogError("SQL", $"World boss update failed: {ex.Message}"); return false; }
        }

        /// <summary>Window over, boss alive: it withdraws with its HP. True once.</summary>
        public Task<bool> WithdrawWorldBoss(int bossId) => GuardedWorldBossUpdate(
            @"UPDATE world_bosses SET status = 'withdrawn', ended_at = datetime('now'), hp_at_end = current_hp,
                  last_resolved_seq = COALESCE(telegraph_seq, 0), telegraph_lands_at = NULL, interrupts_done = 0, stagger_until = NULL, focus_player = '', focus_until = NULL
              WHERE id = @id AND status = 'active';", ("@id", bossId));

        /// <summary>A withdrawn boss returns for another night, regenerated; night damage starts over. True once per night.</summary>
        public async Task<bool> ReactivateWorldBoss(int bossId, double regenFraction, int windowHours)
        {
            bool ok = await GuardedWorldBossUpdate(
                @"UPDATE world_bosses SET status = 'active', nights = COALESCE(nights, 1) + 1,
                      current_hp = MIN(max_hp, current_hp + CAST(max_hp * @regen AS INTEGER)),
                      regen_total = COALESCE(regen_total, 0) + (MIN(max_hp, current_hp + CAST(max_hp * @regen AS INTEGER)) - current_hp),
                      expires_at = datetime('now', '+' || @hours || ' hours'), window_hours = @hours,
                      ended_at = NULL, window_started_at = datetime('now'), started_at = datetime('now')
                  WHERE id = @id AND status = 'withdrawn';",
                ("@id", bossId), ("@regen", regenFraction), ("@hours", windowHours));
            if (ok)
                await GuardedWorldBossUpdateMany("UPDATE world_boss_damage SET night_damage = 0, window_damage = 0 WHERE boss_id = @id;", ("@id", bossId));
            return ok;
        }

        private async Task GuardedWorldBossUpdateMany(string sql, params (string name, object value)[] args)
        {
            try
            {
                using var connection = OpenConnection();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;
                foreach (var (name, value) in args) cmd.Parameters.AddWithValue(name, value);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex) { DebugLogger.Instance.LogError("SQL", $"World boss update failed: {ex.Message}"); }
        }

        /// <summary>After the last night, the boss leaves. True once.</summary>
        public Task<bool> MarkWorldBossLeft(int bossId) => GuardedWorldBossUpdate(
            @"UPDATE world_bosses SET status = 'left', ended_at = datetime('now') WHERE id = @id AND status = 'withdrawn';", ("@id", bossId));

        /// <summary>
        /// Rally: regenerate only while active, after a hit inside the current window, and idle for the
        /// given minutes. Idle time from a previous night never counts, so a returned boss keeps its
        /// wounds until someone hits it (the nightly 20 percent is the only regeneration between windows).
        /// </summary>
        public Task<bool> RallyRegenWorldBoss(int bossId, long amount, int idleMinutes) => GuardedWorldBossUpdate(
            @"UPDATE world_bosses SET current_hp = MIN(max_hp, current_hp + @n), regen_total = COALESCE(regen_total, 0) + (MIN(max_hp, current_hp + @n) - current_hp)
              WHERE id = @id AND status = 'active' AND current_hp < max_hp AND current_hp > 0
                AND last_damaged_at IS NOT NULL
                AND last_damaged_at >= COALESCE(window_started_at, started_at)
                AND last_damaged_at < datetime('now', '-' || @m || ' minutes');",
            ("@id", bossId), ("@n", amount), ("@m", idleMinutes));

        /// <summary>Phase only ever rises. True when it rose.</summary>
        public Task<bool> RaiseWorldBossPhase(int bossId, int phase) => GuardedWorldBossUpdate(
            @"UPDATE world_bosses SET phase = @p WHERE id = @id AND COALESCE(phase, 1) < @p;", ("@id", bossId), ("@p", phase));

        /// <summary>A boss ability that heals: bounded by max HP, active rows only.</summary>
        public Task<bool> HealWorldBoss(int bossId, long amount) => GuardedWorldBossUpdate(
            @"UPDATE world_bosses SET current_hp = MIN(max_hp, current_hp + @n) WHERE id = @id AND status = 'active' AND current_hp > 0;",
            ("@id", bossId), ("@n", amount));

        public Task<bool> UpdateWorldBossPeakEngaged(int bossId, int engaged) => GuardedWorldBossUpdate(
            @"UPDATE world_bosses SET peak_engaged = @n WHERE id = @id AND COALESCE(peak_engaged, 0) < @n;", ("@id", bossId), ("@n", engaged));

        /// <summary>One guarded write per (player, night): the bit flips once, and the row count is the pay signal.</summary>
        public Task<bool> ClaimWorldBossNightPay(int bossId, string playerName, int night) => GuardedWorldBossUpdate(
            @"UPDATE world_boss_damage SET paid_nights = COALESCE(paid_nights, 0) | @bit
              WHERE boss_id = @id AND player_name = LOWER(@player) AND (COALESCE(paid_nights, 0) & @bit) = 0;",
            ("@id", bossId), ("@player", playerName), ("@bit", 1 << Math.Clamp(night - 1, 0, 30)));

        /// <summary>Settle flips once; the reward rows are written first and are unique per (boss, player, kind, night).</summary>
        public Task<bool> MarkWorldBossSettled(int bossId) => GuardedWorldBossUpdate(
            @"UPDATE world_bosses SET settled = 1 WHERE id = @id AND COALESCE(settled, 0) = 0;", ("@id", bossId));

        /// <summary>Insert-or-ignore: a second settle of the same boss writes nothing new.</summary>
        public async Task<bool> InsertWorldBossReward(WorldBossReward r)
        {
            try
            {
                using var connection = OpenConnection();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"INSERT OR IGNORE INTO world_boss_rewards
                                        (boss_id, boss_name, player_name, night, kind, xp, gold, fame, item_json, rarity, marks, score, mvp, damage_dealt)
                                    VALUES (@boss, @bossName, LOWER(@player), @night, @kind, @xp, @gold, @fame, @item, @rarity, @marks, @score, @mvp, @dmg);";
                cmd.Parameters.AddWithValue("@boss", r.BossId);
                cmd.Parameters.AddWithValue("@bossName", r.BossName ?? "");
                cmd.Parameters.AddWithValue("@player", r.PlayerName);
                cmd.Parameters.AddWithValue("@night", r.Night);
                cmd.Parameters.AddWithValue("@kind", r.Kind);
                cmd.Parameters.AddWithValue("@xp", r.Xp);
                cmd.Parameters.AddWithValue("@gold", r.Gold);
                cmd.Parameters.AddWithValue("@fame", r.Fame);
                cmd.Parameters.AddWithValue("@item", r.ItemJson ?? "");
                cmd.Parameters.AddWithValue("@rarity", r.Rarity);
                cmd.Parameters.AddWithValue("@marks", r.Marks);
                cmd.Parameters.AddWithValue("@score", r.Score);
                cmd.Parameters.AddWithValue("@mvp", r.Mvp ? 1 : 0);
                cmd.Parameters.AddWithValue("@dmg", r.DamageDealt);
                return await cmd.ExecuteNonQueryAsync() == 1;
            }
            catch (Exception ex) { DebugLogger.Instance.LogError("SQL", $"Failed to insert world boss reward: {ex.Message}"); return false; }
        }

        public List<WorldBossReward> GetUndeliveredWorldBossRewards(string playerName)
        {
            var list = new List<WorldBossReward>();
            try
            {
                using var connection = OpenConnection();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"SELECT id, boss_id, boss_name, player_name, night, kind, xp, gold, fame, item_json, rarity, marks, score, mvp, damage_dealt
                                    FROM world_boss_rewards WHERE player_name = LOWER(@player) AND delivered = 0 ORDER BY id;";
                cmd.Parameters.AddWithValue("@player", playerName);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new WorldBossReward
                    {
                        Id = reader.GetInt64(0), BossId = reader.GetInt32(1), BossName = reader.GetString(2), PlayerName = reader.GetString(3),
                        Night = reader.GetInt32(4), Kind = reader.GetString(5), Xp = reader.GetInt64(6), Gold = reader.GetInt64(7), Fame = reader.GetInt32(8),
                        ItemJson = reader.IsDBNull(9) ? "" : reader.GetString(9), Rarity = reader.GetInt32(10), Marks = reader.GetInt32(11),
                        Score = reader.GetDouble(12), Mvp = reader.GetInt32(13) != 0, DamageDealt = reader.GetInt64(14),
                    });
                }
            }
            catch (Exception ex) { DebugLogger.Instance.LogError("SQL", $"Failed to read world boss rewards: {ex.Message}"); }
            return list;
        }

        /// <summary>Delivery flips once; the row count says whether this caller owns the delivery.</summary>
        public Task<bool> MarkWorldBossRewardDelivered(long rewardId) => GuardedWorldBossUpdate(
            @"UPDATE world_boss_rewards SET delivered = 1, delivered_at = datetime('now') WHERE id = @id AND delivered = 0;", ("@id", rewardId));

        public List<int> GetUnsettledWorldBossIds()
        {
            var ids = new List<int>();
            try
            {
                using var connection = OpenConnection();
                using var cmd = connection.CreateCommand();
                // A withdrawn boss is paid inside EndWindow before it withdraws; only a kill or a leaving settles.
                cmd.CommandText = @"SELECT id FROM world_bosses WHERE status IN ('defeated', 'left') AND COALESCE(settled, 0) = 0;";
                using var reader = cmd.ExecuteReader();
                while (reader.Read()) ids.Add(reader.GetInt32(0));
            }
            catch (Exception ex) { DebugLogger.Instance.LogError("SQL", $"Failed to list unsettled world bosses: {ex.Message}"); }
            return ids;
        }

        public void LogWorldBossEvent(int bossId, string kind, string playerName = "", string detail = "", long seq = 0)
        {
            try
            {
                using var connection = OpenConnection();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"INSERT INTO world_boss_events (boss_id, seq, kind, player_name, detail) VALUES (@id, @seq, @kind, LOWER(@player), @detail);";
                cmd.Parameters.AddWithValue("@id", bossId);
                cmd.Parameters.AddWithValue("@seq", seq);
                cmd.Parameters.AddWithValue("@kind", kind);
                cmd.Parameters.AddWithValue("@player", playerName ?? "");
                cmd.Parameters.AddWithValue("@detail", detail ?? "");
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex) { DebugLogger.Instance.LogError("SQL", $"Failed to log world boss event: {ex.Message}"); }
        }

        // ───────────────────────────── v1.1.5 (milestone B): telegraphs, interrupts, focus ─────────────────────────────

        /// <summary>The tick issues the next telegraph; only from the previous seq, only on an active row.</summary>
        public Task<bool> IssueWorldBossTelegraph(int bossId, string telegraphId, long seq, int landSeconds, int interruptsNeeded) => GuardedWorldBossUpdate(
            @"UPDATE world_bosses SET telegraph_id = @tid, telegraph_seq = @seq, telegraph_lands_at = datetime('now', '+' || @land || ' seconds'),
                  interrupts_needed = @needed, interrupts_done = 0
              WHERE id = @id AND status = 'active' AND COALESCE(telegraph_seq, 0) = @seq - 1 AND COALESCE(last_resolved_seq, 0) = COALESCE(telegraph_seq, 0);",
            ("@id", bossId), ("@tid", telegraphId), ("@seq", seq), ("@land", landSeconds), ("@needed", interruptsNeeded));

        /// <summary>The tick resolves a landed telegraph once; a broken channel staggers the boss.</summary>
        public Task<bool> ResolveWorldBossTelegraph(int bossId, long seq, string outcome, int staggerSeconds) => GuardedWorldBossUpdate(
            @"UPDATE world_bosses SET last_resolved_seq = @seq, last_resolved_outcome = @outcome,
                  stagger_until = CASE WHEN @outcome = 'broken' THEN datetime('now', '+' || @stagger || ' seconds') ELSE stagger_until END
              WHERE id = @id AND telegraph_seq = @seq AND COALESCE(last_resolved_seq, 0) < @seq;",
            ("@id", bossId), ("@seq", seq), ("@outcome", outcome), ("@stagger", staggerSeconds));

        /// <summary>A player's interrupt: counts only while the channel is live and short of its need.</summary>
        public Task<bool> TryInterruptWorldBoss(int bossId, long seq) => GuardedWorldBossUpdate(
            @"UPDATE world_bosses SET interrupts_done = COALESCE(interrupts_done, 0) + 1
              WHERE id = @id AND status = 'active' AND telegraph_seq = @seq AND COALESCE(last_resolved_seq, 0) < @seq
                AND COALESCE(interrupts_done, 0) < COALESCE(interrupts_needed, 0) AND telegraph_lands_at > datetime('now');",
            ("@id", bossId), ("@seq", seq));

        /// <summary>The player's answer on their own row, once per seq; survives a retreat.</summary>
        public Task<bool> RecordWorldBossAnswer(int bossId, string playerName, long seq, string kind) => GuardedWorldBossUpdate(
            @"UPDATE world_boss_damage SET answered_seq = @seq, answer_kind = @kind, answers = COALESCE(answers, 0) + 1, last_hit_at = datetime('now')
              WHERE boss_id = @id AND player_name = LOWER(@player) AND COALESCE(answered_seq, 0) < @seq;",
            ("@id", bossId), ("@player", playerName), ("@seq", seq), ("@kind", kind));

        /// <summary>
        /// Entry: the row exists, and the player is in from this seq. Telegraphs issued while they
        /// were away are not theirs (the loop carries the one that was live when they left, read
        /// before this write, into its first round).
        /// </summary>
        public async Task EnsureWorldBossPlayerRow(int bossId, string playerName, int playerLevel, string displayName, long engagedSinceSeq)
        {
            try
            {
                using var connection = OpenConnection();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"INSERT INTO world_boss_damage (boss_id, player_name, damage_dealt, hits, player_level, display_name, last_hit_at, engaged_since_seq, last_resolved_seq)
                                    VALUES (@id, LOWER(@player), 0, 0, @level, @display, datetime('now'), @seq, @seq - 1)
                                    ON CONFLICT(boss_id, player_name) DO UPDATE SET
                                        last_hit_at = datetime('now'),
                                        engaged_since_seq = @seq,
                                        last_resolved_seq = MAX(COALESCE(last_resolved_seq, 0), @seq - 1),
                                        player_level = CASE WHEN @level > 0 THEN @level ELSE player_level END,
                                        display_name = CASE WHEN @display <> '' THEN @display ELSE display_name END;";
                cmd.Parameters.AddWithValue("@id", bossId);
                cmd.Parameters.AddWithValue("@player", playerName);
                cmd.Parameters.AddWithValue("@level", playerLevel);
                cmd.Parameters.AddWithValue("@display", displayName ?? "");
                cmd.Parameters.AddWithValue("@seq", engagedSinceSeq);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex) { DebugLogger.Instance.LogError("SQL", $"Failed to ensure world boss row: {ex.Message}"); }
        }

        /// <summary>The loop applied a landed telegraph to this player; once per seq.</summary>
        public Task<bool> AdvanceWorldBossPlayerResolved(int bossId, string playerName, long seq) => GuardedWorldBossUpdate(
            @"UPDATE world_boss_damage SET last_resolved_seq = @seq WHERE boss_id = @id AND player_name = LOWER(@player) AND COALESCE(last_resolved_seq, 0) < @seq;",
            ("@id", bossId), ("@player", playerName), ("@seq", seq));

        public WorldBossPlayerTelegraphState GetWorldBossPlayerTelegraphState(int bossId, string playerName)
        {
            try
            {
                using var connection = OpenConnection();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"SELECT COALESCE(engaged_since_seq, 0), COALESCE(last_resolved_seq, 0), COALESCE(answered_seq, 0), COALESCE(answer_kind, ''), COALESCE(engaged_until_seq, 0)
                                    FROM world_boss_damage WHERE boss_id = @id AND player_name = LOWER(@player);";
                cmd.Parameters.AddWithValue("@id", bossId);
                cmd.Parameters.AddWithValue("@player", playerName);
                using var reader = cmd.ExecuteReader();
                if (reader.Read()) return new WorldBossPlayerTelegraphState(reader.GetInt64(0), reader.GetInt64(1), reader.GetInt64(2), reader.GetString(3), reader.GetInt64(4));
            }
            catch (Exception ex) { DebugLogger.Instance.LogError("SQL", $"Failed to read player telegraph state: {ex.Message}"); }
            return new WorldBossPlayerTelegraphState(0, 0, 0, "", 0);
        }

        /// <summary>Resolved telegraphs after a seq, from the events table (kind telegraph_resolved, detail id|kind|outcome).</summary>
        public List<WorldBossTelegraphOutcome> GetResolvedWorldBossTelegraphs(int bossId, long afterSeq, int limit = 20)
        {
            var list = new List<WorldBossTelegraphOutcome>();
            try
            {
                using var connection = OpenConnection();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"SELECT seq, detail FROM world_boss_events WHERE boss_id = @id AND kind = 'telegraph_resolved' AND seq > @after ORDER BY seq LIMIT @limit;";
                cmd.Parameters.AddWithValue("@id", bossId);
                cmd.Parameters.AddWithValue("@after", afterSeq);
                cmd.Parameters.AddWithValue("@limit", limit);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var parts = (reader.IsDBNull(1) ? "" : reader.GetString(1)).Split('|');
                    if (parts.Length >= 3) list.Add(new WorldBossTelegraphOutcome(reader.GetInt64(0), parts[0], parts[1], parts[2]));
                }
            }
            catch (Exception ex) { DebugLogger.Instance.LogError("SQL", $"Failed to read resolved telegraphs: {ex.Message}"); }
            return list;
        }

        /// <summary>Challenge: take the boss's focus for a while, if nobody holds it.</summary>
        public Task<bool> TryChallengeWorldBoss(int bossId, string playerName, int holdSeconds) => GuardedWorldBossUpdate(
            @"UPDATE world_bosses SET focus_player = LOWER(@player), focus_until = datetime('now', '+' || @hold || ' seconds')
              WHERE id = @id AND status = 'active' AND (focus_until IS NULL OR focus_until < datetime('now'));",
            ("@id", bossId), ("@player", playerName), ("@hold", holdSeconds));

        /// <summary>
        /// The tick's focus window: when the window is older than N seconds, the top window damage among
        /// engaged humans takes focus unless a Challenge holds it; then the window starts over. Focus is
        /// last-writer-wins by design (the tick and Challenge both write it under their own guards).
        /// Returns the focus player after the refresh, or null when the window had not closed.
        /// </summary>
        public async Task<string?> RefreshWorldBossFocus(int bossId, int windowSeconds, int engagedMinutes)
        {
            try
            {
                using var connection = OpenConnection();
                using var transaction = connection.BeginTransaction();
                using (var check = connection.CreateCommand())
                {
                    check.Transaction = transaction;
                    check.CommandText = @"SELECT COUNT(*) FROM world_bosses WHERE id = @id AND status = 'active'
                                          AND (window_started_at IS NULL OR window_started_at <= datetime('now', '-' || @w || ' seconds'));";
                    check.Parameters.AddWithValue("@id", bossId);
                    check.Parameters.AddWithValue("@w", windowSeconds);
                    if (Convert.ToInt32(check.ExecuteScalar()) == 0) { transaction.Commit(); return null; }
                }
                string? top = null;
                using (var pick = connection.CreateCommand())
                {
                    pick.Transaction = transaction;
                    pick.CommandText = @"SELECT player_name FROM world_boss_damage WHERE boss_id = @id AND COALESCE(is_npc, 0) = 0
                                         AND last_hit_at > datetime('now', '-' || @m || ' minutes') ORDER BY COALESCE(window_damage, 0) DESC, last_hit_at DESC LIMIT 1;";
                    pick.Parameters.AddWithValue("@id", bossId);
                    pick.Parameters.AddWithValue("@m", engagedMinutes);
                    top = pick.ExecuteScalar() as string;
                }
                using (var set = connection.CreateCommand())
                {
                    set.Transaction = transaction;
                    set.CommandText = @"UPDATE world_bosses SET focus_player = CASE WHEN (focus_until IS NULL OR focus_until < datetime('now')) THEN @top ELSE focus_player END,
                                            window_started_at = datetime('now') WHERE id = @id;";
                    set.Parameters.AddWithValue("@id", bossId);
                    set.Parameters.AddWithValue("@top", (object?)top ?? DBNull.Value);
                    set.ExecuteNonQuery();
                }
                using (var zero = connection.CreateCommand())
                {
                    zero.Transaction = transaction;
                    zero.CommandText = "UPDATE world_boss_damage SET window_damage = 0 WHERE boss_id = @id;";
                    zero.Parameters.AddWithValue("@id", bossId);
                    zero.ExecuteNonQuery();
                }
                string? focus;
                using (var read = connection.CreateCommand())
                {
                    read.Transaction = transaction;
                    read.CommandText = "SELECT COALESCE(focus_player, '') FROM world_bosses WHERE id = @id;";
                    read.Parameters.AddWithValue("@id", bossId);
                    focus = read.ExecuteScalar() as string;
                }
                transaction.Commit();
                return focus ?? "";
            }
            catch (Exception ex) { DebugLogger.Instance.LogError("SQL", $"Focus refresh failed: {ex.Message}"); return null; }
        }

        /// <summary>Engaged humans, most recent hit first: key and display name.</summary>
        public List<(string key, string display)> GetWorldBossEngagedNames(int bossId, int minutes, int limit = 50)
        {
            var list = new List<(string, string)>();
            try
            {
                using var connection = OpenConnection();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"SELECT player_name, COALESCE(display_name, '') FROM world_boss_damage WHERE boss_id = @id AND COALESCE(is_npc, 0) = 0
                                    AND last_hit_at > datetime('now', '-' || @m || ' minutes') ORDER BY last_hit_at DESC LIMIT @limit;";
                cmd.Parameters.AddWithValue("@id", bossId);
                cmd.Parameters.AddWithValue("@m", minutes);
                cmd.Parameters.AddWithValue("@limit", limit);
                using var reader = cmd.ExecuteReader();
                while (reader.Read()) list.Add((reader.GetString(0), reader.GetString(1)));
            }
            catch (Exception ex) { DebugLogger.Instance.LogError("SQL", $"Failed to list engaged players: {ex.Message}"); }
            return list;
        }

        /// <summary>Median level of players who logged in during the last N days; the fallback when nobody has.</summary>
        public int GetActivePlayersMedianLevel(int days, int fallback = 20)
        {
            try
            {
                var levels = new List<int>();
                using var connection = OpenConnection();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"SELECT json_extract(player_data, '$.player.level') FROM players
                                    WHERE is_banned = 0 AND last_login > datetime('now', '-' || @d || ' days')
                                      AND json_extract(player_data, '$.player.level') IS NOT NULL
                                      AND username NOT LIKE 'emergency_%';";
                cmd.Parameters.AddWithValue("@d", days);
                using var reader = cmd.ExecuteReader();
                while (reader.Read()) { if (!reader.IsDBNull(0)) levels.Add(Convert.ToInt32(reader.GetValue(0))); }
                if (levels.Count == 0) return fallback;
                levels.Sort();
                return levels[levels.Count / 2];
            }
            catch { return fallback; }
        }

        /// <summary>Usernames and languages of players who logged in during the last N days, for notice mail.</summary>
        public List<(string username, string language)> GetRecentActivePlayers(int days, int limit = 500)
        {
            var list = new List<(string, string)>();
            try
            {
                using var connection = OpenConnection();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"SELECT username, COALESCE(language, 'en') FROM players
                                    WHERE is_banned = 0 AND last_login > datetime('now', '-' || @d || ' days') AND username NOT LIKE 'emergency_%'
                                    ORDER BY last_login DESC LIMIT @limit;";
                cmd.Parameters.AddWithValue("@d", days);
                cmd.Parameters.AddWithValue("@limit", limit);
                using var reader = cmd.ExecuteReader();
                while (reader.Read()) list.Add((reader.GetString(0), reader.GetString(1)));
            }
            catch (Exception ex) { DebugLogger.Instance.LogError("SQL", $"Failed to list recent players: {ex.Message}"); }
            return list;
        }
    }
}
