using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Cards;
using Squ.Character;
using Squ.Combat;
using Squ.Monsters;
using Squ.Patches;
using Squ.Powers;
using TestTheSpire;
using Xunit;
namespace Squ.Tests.Cards;

public sealed class SummonBrotherTests : SunqianCombatTestSuite
{
    private MegaCrit.Sts2.Core.GameActions.Multiplayer.BlockingPlayerChoiceContext ChoiceContext() => new();
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<LiubeiCharacter>().AddEnemy<BigDummy>().WithSeed("summon-brothers");

    [Theory]
    [InlineData(false, 8)]
    [InlineData(true, 11)]
    public async Task Slight_revision_transforms_into_matching_third_brother_card(bool upgraded, int amount)
    {
        var card = await AddToHand<SummonSecondBrother>();
        Assert.DoesNotContain("\n\n", card.GetDescriptionForPile(PileType.Hand));
        if (upgraded) Upgrade(card);
        Assert.Equal("召唤二弟" + (upgraded ? "+" : ""), card.Title);
        Assert.Contains("召唤三弟", card.GetDescriptionForPile(PileType.Hand));
        Assert.True(SlightRevisionSystem.TryGetRevision(card, out var target, out bool targetUpgraded));
        Assert.IsType<SummonThirdBrother>(target);
        Assert.Equal(upgraded, targetUpgraded);
        await SlightRevisionSystem.TransformAsync(card, target, targetUpgraded);
        var thirdCard = Assert.Single(Player.PlayerCombatState!.Hand.Cards.OfType<SummonThirdBrother>());
        Assert.Equal(upgraded, thirdCard.IsUpgraded);
        Assert.Equal(2, thirdCard.EnergyCost.GetWithModifiers(CostModifiers.None));
        Assert.Contains("三弟", thirdCard.GetDescriptionForPile(PileType.Hand));
        Assert.DoesNotContain("二弟", thirdCard.GetDescriptionForPile(PileType.Hand));
        await PlayerCmd.SetEnergy(0, Player);
        int hp = Player.Creature.CurrentHp;
        int enemyHp = EnemyAt(0).CurrentHp;
        await Play(thirdCard, EnemyAt(0));
        var pet = CompanionSummons.GetCompanion(Player, CompanionSummons.ThirdBrotherId)!;
        Assert.IsType<ThirdBrother>(pet.Monster);
        Assert.Equal(amount, pet.CurrentHp);
        Assert.Equal(hp - amount, Player.Creature.CurrentHp);
        Assert.Equal(enemyHp - amount, EnemyAt(0).CurrentHp);
        Assert.Equal(amount, LifeContractSystem.GetTotal(Player, CompanionSummons.ThirdBrotherId));
        Assert.Equal(0, LifeContractSystem.GetTotal(Player));
        Assert.Null(CompanionSummons.GetCompanion(Player, CompanionSummons.SecondBrotherId));
        Assert.Equal(CompanionSummons.SecondBrotherId, CompanionSummons.GetDefaultId(Player));
        Assert.Contains(CompanionSummons.ThirdBrotherId, CompanionSummons.GetAvailableIds(Player));
        await OstyCmd.Summon(ChoiceContext(), Player, 3, null);
        Assert.Equal(3, CompanionSummons.GetCompanion(Player, CompanionSummons.SecondBrotherId)!.CurrentHp);
        Assert.Equal(amount, pet.CurrentHp);
    }

    [Fact]
    public async Task Third_brother_takes_damage_and_reuses_native_model_and_status_widget()
    {
        var pet = (await CompanionSummons.SummonSpecific(ChoiceContext(), Player, CompanionSummons.ThirdBrotherId, 10, null)).Creature!;
        int hp = Player.Creature.CurrentHp;
        await CreatureCmd.Damage(ChoiceContext(), Player.Creature, 3, ValueProp.Move, null, null);
        Assert.Equal(hp, Player.Creature.CurrentHp);
        Assert.Equal(7, pet.CurrentHp);
        Assert.Contains("[gold]三弟[/gold]", pet.GetPower<DieForYouPower>()!.Description.GetFormattedText());
        Assert.Contains("[gold]三弟[/gold]", pet.GetPower<LifeContractPower>()!.Description.GetFormattedText());
        using var visuals = pet.Monster!.CreateVisuals();
        Assert.EndsWith("/osty.tscn", visuals.SceneFilePath);
        var node = PreloadManager.Cache.GetScene("res://scenes/combat/creature.tscn").Instantiate<NCreature>();
        Traverse.Create(node).Property(nameof(NCreature.Entity)).SetValue(pet);
        Traverse.Create(node).Property(nameof(NCreature.Visuals)).SetValue(pet.Monster.CreateVisuals());
        try
        {
            ((SceneTree)Engine.GetMainLoop()).Root.AddChild(node);
            var display = Traverse.Create(node).Field<NCreatureStateDisplay>("_stateDisplay").Value;
            Assert.True(display.Visible);
            Assert.True(node.IsInteractable);
            var powers = Traverse.Create(display).Field<NPowerContainer>("_powerContainer").Value;
            Assert.Contains(powers.GetChildren().OfType<NPower>(), power => power.Model is DieForYouPower);
            Assert.Contains(powers.GetChildren().OfType<NPower>(), power => power.Model is LifeContractPower);
            Assert.Same(pet.Monster, SecondBrotherPresentationPatches.AsOstyPresentation(pet.Monster));
        }
        finally { node.GetParent()?.RemoveChild(node); node.QueueFree(); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Brothers_keep_separate_contracts_and_only_surviving_brother_pays_his_own(bool killSecond)
    {
        int hp = Player.Creature.CurrentHp;
        await Play(await AddToHand<SummonSecondBrother>(), EnemyAt(0));
        var thirdCard = await AddToHand<SummonThirdBrother>();
        Upgrade(thirdCard);
        await Play(thirdCard, EnemyAt(0));
        var second = CompanionSummons.GetCompanion(Player, CompanionSummons.SecondBrotherId)!;
        var third = CompanionSummons.GetCompanion(Player, CompanionSummons.ThirdBrotherId)!;
        Assert.Equal(8, second.GetPower<LifeContractPower>()!.DisplayAmount);
        Assert.Equal(11, third.GetPower<LifeContractPower>()!.DisplayAmount);
        if (killSecond) await CreatureCmd.Kill(second);
        await second.GetPower<LifeContractPower>()!.AfterCombatEnd(null!);
        await third.GetPower<LifeContractPower>()!.AfterCombatEnd(null!);
        Assert.Equal(hp - (killSecond ? 8 : 0), Player.Creature.CurrentHp);
        int paidHp = Player.Creature.CurrentHp;
        await third.GetPower<LifeContractPower>()!.AfterCombatEnd(null!);
        Assert.Equal(paidHp, Player.Creature.CurrentHp);
    }

    [Fact]
    public void Renamed_starter_is_registered_and_third_is_not_a_reward_card()
    {
        Assert.Single(Player.Character.StartingDeck.OfType<SummonSecondBrother>());
        var pool = ModelDb.CardPool<LiubeiCardPool>().GetUnlockedCards(Player.UnlockState, Player.RunState.CardMultiplayerConstraint);
        Assert.DoesNotContain(pool, card => card is SummonThirdBrother);
        Assert.DoesNotContain(CompanionSummons.ThirdBrotherId, CompanionSummons.GetAvailableIds(Player));
    }
}
