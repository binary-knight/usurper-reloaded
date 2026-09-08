using System.Linq;
using FluentAssertions;
using UsurperRemake;
using Xunit;

namespace UsurperReborn.Tests;

/// <summary>
/// issue #112: a reforge must touch the player's own copy of a weapon, never the shop's shared
/// template, and a reforged weapon must survive unequip and re-equip with its rarity and stats.
/// </summary>
[Collection("SharedGameSingletons")]
public class ReforgeOwnCopyTests
{
    private static Character Hero() => new Character
    {
        Name1 = "smith", Name2 = "Smith", Class = CharacterClass.Warrior, Race = CharacterRace.Human, Level = 20,
        HP = 500, MaxHP = 500, Strength = 50, Defence = 30,
    };

    [Fact]
    public void AShopWeaponEquippedAtTheCounter_GetsItsOwnCopyBeforeAReforge_AndTheTemplateIsUntouched()
    {
        var template = EquipmentDatabase.GetOneHandedWeapons().First(w => w.MinLevel <= 20 && !w.IsCursed);
        int templateId = template.Id;
        int templatePower = template.WeaponPower;
        var rarity = template.Rarity;

        var hero = Hero();
        hero.EquipItem(template, EquipmentSlot.MainHand, out _).Should().BeTrue();
        hero.EquippedItems[EquipmentSlot.MainHand].Should().Be(templateId, "the shop equips the template itself");
        EquipmentDatabase.IsDynamic(templateId).Should().BeFalse();

        var own = hero.EnsureOwnEquipmentCopy(EquipmentSlot.MainHand)!;
        own.Id.Should().NotBe(templateId);
        EquipmentDatabase.IsDynamic(own.Id).Should().BeTrue();
        hero.EquippedItems[EquipmentSlot.MainHand].Should().Be(own.Id, "the slot points at the copy");
        own.WeaponPower.Should().Be(templatePower);

        // the reforge rewrites the copy; the template every other player shares does not move
        own.WeaponPower = templatePower * 3;
        own.Rarity = EquipmentRarity.Epic;
        EquipmentDatabase.GetById(templateId)!.WeaponPower.Should().Be(templatePower);
        EquipmentDatabase.GetById(templateId)!.Rarity.Should().Be(rarity);

        hero.EnsureOwnEquipmentCopy(EquipmentSlot.MainHand)!.Id.Should().Be(own.Id, "a second call is a no-op on a copy");
        hero.RecalculateStats();
        hero.WeapPow.Should().Be(hero.BaseWeapPow + templatePower * 3, "the character wears the copy");
    }

    [Fact]
    public void AReforgedWeapon_KeepsItsRarityAndStats_ThroughUnequipAndReequip()
    {
        var template = EquipmentDatabase.GetOneHandedWeapons().First(w => w.MinLevel <= 20 && !w.IsCursed);
        var hero = Hero();
        hero.EquipItem(template, EquipmentSlot.MainHand, out _).Should().BeTrue();
        var own = hero.EnsureOwnEquipmentCopy(EquipmentSlot.MainHand)!;
        own.WeaponPower = 77; own.StrengthBonus = 9; own.AgilityBonus = 4; own.ConstitutionBonus = 6;
        own.BlockChance = 3; own.LifeSteal = 5; own.Rarity = EquipmentRarity.Rare;

        var unequipped = hero.UnequipSlot(EquipmentSlot.MainHand)!;
        var item = hero.ConvertEquipmentToLegacyItem(unequipped);
        item.Rarity.Should().Be(EquipmentRarity.Rare, "the quality does not reset in the bag");
        item.Attack.Should().Be(77);
        item.Strength.Should().Be(9);
        item.Agility.Should().Be(4);
        item.BlockChance.Should().Be(3);
        item.LootEffects.Should().Contain(((int)LootGenerator.SpecialEffect.Constitution, 6));
        item.LootEffects.Should().Contain(((int)LootGenerator.SpecialEffect.LifeSteal, 5));

        var back = Character.BuildEquipmentFromItem(item, EquipmentSlot.MainHand, WeaponHandedness.OneHanded, own.WeaponType);
        back.Rarity.Should().Be(EquipmentRarity.Rare);
        back.WeaponPower.Should().Be(77);
        back.StrengthBonus.Should().Be(9);
        back.AgilityBonus.Should().Be(4);
        back.ConstitutionBonus.Should().Be(6);
        back.BlockChance.Should().Be(3);
        back.LifeSteal.Should().Be(5);
    }

    [Fact]
    public void AnEmptySlot_HasNoCopyToMake()
    {
        Hero().EnsureOwnEquipmentCopy(EquipmentSlot.MainHand).Should().BeNull();
    }
}
