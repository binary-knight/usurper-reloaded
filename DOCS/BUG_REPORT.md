# Comprehensive Bug Report - Usurper Reborn

Generated: 2026-01-21

This report contains bugs identified through comprehensive code review of all game systems.

---

## CRITICAL BUGS (Must Fix)

### 1. Quest Completion Impossible - Missing Occupier Field
**File:** QuestSystem.cs:79-81, 112
**Impact:** Progress Blocker

`ClaimQuest()` sets Occupier field, but `CompleteQuest()` checks `quest.Occupier == player.Name2`. If quest is auto-claimed (bounties), Occupier remains empty and quest cannot complete, permanently blocking bounty quests.

### 2. NPC/Player Relationships Never Saved
**File:** SaveSystem.cs:1012-1016, 1172-1176
**Impact:** Data Loss

Both `SerializeRelationships()` and `SerializeNPCRelationships()` return empty dictionaries with TODO comments. All relationship data is lost on save/load.

### 3. NPC Memories/Goals/Emotions Never Restored
**File:** SaveSystem.cs:891-892, GameEngine.cs:1714-1960
**Impact:** Data Loss

`SerializeGoals()`, `SerializeMemories()`, and `SerializeEmotionalState()` save NPC state, but `RestoreNPCs()` never restores them. NPCs lose all memory of player interactions on load.

### 4. AllSpecialFloors Missing Old God Floors
**File:** DungeonLocation.cs:2820
**Impact:** Progression Bypass

`AllSpecialFloors` is missing floors 40, 55, 70, 85, 95, 100 from OldGodFloors. `GetMaxAccessibleFloor()` doesn't block Old God floors, allowing players to skip prerequisites.

### 5. PerformMarriage Allows Marrying Dead NPCs
**File:** RelationshipSystem.cs:150-240
**Impact:** Broken State

No `IsDead` check before marriage. Players can marry permanently dead NPCs, creating invalid relationship records.

---

## HIGH SEVERITY BUGS

### Combat Systems

#### 6. Phase Resistance Calculation Inverted
**File:** OldGodBossSystem.cs:793, 856
**Impact:** Boss fights unbalanced

Division by `BossDefenseMultiplier` instead of multiplication inverts the effect. Bosses with defense > 1.0 become WEAKER instead of stronger.

#### 7. King Defense Divided by 4 in Throne Challenges
**File:** ChallengeSystem.cs:288
**Impact:** Throne too easy to take

`kingStr / 4` makes king defense 4x weaker than guards. Throne challenges are significantly easier than intended.

### Save/Load System

#### 8. Player ID Never Saved/Restored
**File:** SaveDataStructures.cs, GameEngine.cs:1200-1582
**Impact:** ID-based lookups fail

`PlayerData` has no ID property. Systems using ID-based lookups (RomanceTracker) break after load.

#### 9. NPC Personality Overwritten on Load
**File:** GameEngine.cs:1803-1846
**Impact:** Personality reset

`EnsureSystemsInitialized()` at line 1846 may regenerate random personality, overwriting restored personality data.

### NPC/Relationship Systems

#### 10. GetSpouseName Returns Dead Spouse
**File:** RelationshipSystem.cs:119-144
**Impact:** Dead spouse interactions

No `IsDead` check. Players can interact with (including intimate scenes) permanently dead spouses.

#### 11. GetChildrenOfCouple Logic Error
**File:** FamilySystem.cs:67-78
**Impact:** Wrong children returned

Operator precedence error in OR clause causes method to return children from unrelated couples.

#### 12. GetNPCsAtLocation Returns Dead NPCs
**File:** NPCSpawnSystem.cs:556-559
**Impact:** Dead NPC interactions

No `IsDead` filter. Dead NPCs appear in location queries.

### Story/Progression Systems

#### 13. GetPlayerLevel() Returns Hardcoded 1
**File:** CompanionSystem.cs:1665-1669
**Impact:** Wrong level tracking

Stub method always returns 1. Companion death records and grief calculations use wrong player level.

#### 14. SevenSeals Array Bounds Risk
**File:** SevenSealsSystem.cs:444-446
**Impact:** Potential crash

`nextSealFloors[collected - 1]` has no bounds check. If collected >= 8, causes IndexOutOfRangeException.

### Economy/Equipment Systems

#### 15. Double HP Application on Equip
**File:** Items.cs:109
**Impact:** Stat inflation

`character.HP = Math.Min(character.HP + HP, character.MaxHP)` adds HP bonus twice (once to MaxHP, once to current HP).

#### 16. Gold Overflow in Dungeon Rewards
**File:** DungeonLocation.cs:2296, 2460, 3216, 3835, 4177, 4591
**Impact:** Gold corruption

Direct `player.Gold +=` without overflow check. Large dungeon bonuses (2000x level) can overflow `long` type.

#### 17. EquipmentDatabase Not Force-Initialized
**File:** EquipmentData.cs:33-55
**Impact:** Null equipment

Static `Initialize()` must be called explicitly. Shops can return null items if accessed before init.

### Daily/World Simulation

#### 18. King.CalculateDailyIncome() Returns Zero
**File:** King.cs:136-140
**Impact:** Treasury always empty

Returns `TaxRate * 10` but TaxRate is never set (defaults to 0). Royal treasury income is always zero.

#### 19. GD.RandRange Bounds Error
**File:** DailySystemManager.cs:523, 543
**Impact:** Events never trigger

`GD.RandRange(0, 2)` returns 0-1 (not 0-2). Case 2 (HarvestFestival) and Case 3 (AncientRelicFound) are unreachable.

#### 20. ProcessNPCsDuringAbsence Not Implemented
**File:** DailySystemManager.cs:354-369
**Impact:** World doesn't advance

Methods only print messages, no actual simulation. World state doesn't advance during player absence.

### Dungeon/Monster Systems

#### 21. DescendDeeper Missing Floor Generation
**File:** DungeonLocation.cs:6406-6409
**Impact:** Dungeon broken

`DescendDeeper()` increments level but never calls `GenerateOrRestoreFloor()`. Floor data remains from previous level.

#### 22. Monster Level 2 Gap
**File:** MonsterFamilies.cs
**Impact:** No enemies at level 2

Lowest MinLevel is 3 (Ooze). Level 2 players have no suitable monsters to fight.

---

## MEDIUM SEVERITY BUGS

### Statistics Tracking (Multiple Locations)

#### 23. Poison Cure Gold Not Tracked
**File:** HealerLocation.cs:573

#### 24. Disease Cure Gold Not Tracked (2 instances)
**File:** HealerLocation.cs:673, 704

#### 25. Cursed Item Removal Gold Not Tracked
**File:** HealerLocation.cs:843

#### 26. Inn Expenses Not Tracked (3 instances)
**File:** InnLocation.cs:588, 1108, 1226
Drinks (-5g), Drinking Game (-20g), Food (-10g) have no `RecordGoldSpent()` calls.

#### 27. Inn Gift Gold Not Tracked
**File:** InnLocation.cs:1030

### Combat/Damage

#### 28. Integer Division in Dodge Calculation
**File:** OldGodBossSystem.cs:1018
`actualDamage / 3` loses precision due to integer division.

### Story Systems

#### 29. FullReset() Doesn't Clear OceanPhilosophy
**File:** StoryProgressionSystem.cs:459-479
Wave fragments and philosophy progress persist across new games.

#### 30. Achievement "completionist" Logic Error
**File:** AchievementSystem.cs:869-872
Completionist is marked Secret but check expects non-secret. Can never unlock.

#### 31. Vex Death Check Division Risk
**File:** CompanionSystem.cs:920-926
`RecruitedDay` may be 0, causing negative days calculation.

#### 32. GriefSystem Missing NPC Grief on Deserialize
**File:** GriefSystem.cs:699-706
Only companion grief restored, NPC grief (spouse/team death) lost on load.

### NPC Systems

#### 33. Spouse Death Not Synced to Relationship
**File:** RelationshipSystem.cs + NPCSpawnSystem.cs
When spouse dies (IsDead=true), relationship stays "married" forever.

#### 34. ImprisonNPC Doesn't Clear Death State
**File:** NPCSpawnSystem.cs:603-611
Imprisoned NPCs that die and respawn have orphaned prison timers.

#### 35. SaveRelationship() is No-Op
**File:** RelationshipSystem.cs:514-519
Only updates timestamp, no actual persistence mechanism.

#### 36. ConvertChildToNPC Missing Name1
**File:** FamilySystem.cs:205-226
Only Name2 set, Name1 remains null. Brain/Personality may not initialize.

### Equipment

#### 37. Equipment Stat Conversion Incomplete
**File:** Character.cs:509-520
`ConvertEquipmentToItem()` drops: ConstitutionBonus, IntelligenceBonus, CharismaBonus, CriticalChance, LifeSteal, PoisonDamage.

#### 38. Cursed Item Unequip Logic Broken
**File:** Character.cs:536-537
`UnequipSlot()` returns null for cursed items but callers ignore return value.

### World Events

#### 39. PlagueEnds Event Never Generated
**File:** WorldEventSystem.cs:389-395
EventType.PlagueEnds defined but never added to possible events. Plagues can become permanent.

---

## LOW SEVERITY BUGS

### 40. Quest Reward Byte Truncation
**File:** QuestSystem.cs:488-495, 1325
Reward stored as byte (0-255), high-level rewards truncated.

### 41. Achievement Rewards Not Tracked in Statistics
**File:** AchievementSystem.cs:776-779
Gold/XP rewards not recorded with RecordGoldEarned().

### 42. PersonalQuestStarted Never Reset on Failure
**File:** CompanionSystem.cs:964-982
Failed quests keep PersonalQuestStarted=true, blocking retry.

### 43. RealTime24Hour Reset Uses > Instead of >=
**File:** DailySystemManager.cs:76
Edge case: reset delayed if login exactly at midnight.

### 44. Finance News Shows Pre-Calculation Values
**File:** DailySystemManager.cs:472-488
Net change calculated before ProcessDailyActivities but reported after.

### 45. Bank Robbery Overflow Risk
**File:** BankLocation.cs:1267-1281
`playerPower += currentDungeonLevel * 50` can overflow int at high levels.

### 46. SevenSeals CollectSeal No Floor Validation
**File:** SevenSealsSystem.cs:316-368
Seal collection doesn't verify player is on correct dungeon floor.

### 47. Aquatic Monsters Only Available Level 12+
**File:** MonsterFamilies.cs:262
Aquatic family starts at MinLevel=12, missing from early game.

### 48. Celestial Monsters Only Available Level 20+
**File:** MonsterFamilies.cs:279
Celestial family locked until mid-game (MinLevel=20).

### 49. Player Spell/Skill Lists May Be Null
**File:** GameEngine.cs:1468-1476
Lists only assigned if save has content, may remain null.

### 50. MarketInventory Null Risk on Load
**File:** GameEngine.cs:1780-1797
No null check before `npc.MarketInventory.Add()`.

---

## SUMMARY

| Severity | Count |
|----------|-------|
| Critical | 5 |
| High | 17 |
| Medium | 17 |
| Low | 11 |
| **TOTAL** | **50** |

### Priority Fix Order:
1. Save/Load data loss bugs (relationships, NPC state)
2. Quest completion blocker
3. Combat calculation errors (phase resistance, king defense)
4. Statistics tracking gaps
5. Dungeon progression issues
6. NPC death state handling
7. Gold overflow protection
8. Event generation fixes
