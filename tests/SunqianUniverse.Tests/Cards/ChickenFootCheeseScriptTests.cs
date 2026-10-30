using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using Squ.Combat;
using Squ.Powers;
using STS2RitsuLib.Models.Capabilities;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class ChickenFootCheeseScriptTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Ironclad>()
            .AddEnemy<BigDummy>()
            .AddEnemy<BigDummy>()
            .WithSeed("sunqian-chicken-foot-cheese-script");
    }

    [Fact]
    public async Task Basic_strike_has_no_presentation_capability_until_script_is_active()
    {
        var strike = await AddToHand<StrikeIronclad>();

        Assert.Null(strike.Capability<ChickenFootCheeseStrikeCapability>());
    }

    [Fact]
    public async Task Base_card_deals_six_then_three_and_both_hits_receive_vigor()
    {
        await ApplyPower<VigorPower>(Player.Creature, 2);
        var first = EnemyAt(0);
        var second = EnemyAt(1);
        int firstHpBefore = first.CurrentHp;
        int secondHpBefore = second.CurrentHp;
        var card = await AddToHand<ChickenFootCheeseScript>();

        card.DynamicVars.Damage.UpdateCardPreview(card, CardPreviewMode.Normal, first, true);
        card.DynamicVars[ChickenFootCheeseScript.FollowUpDamageVarName]
            .UpdateCardPreview(card, CardPreviewMode.Normal, first, true);
        card.DynamicVars[ChickenFootCheeseScript.ScriptFollowUpDamageVarName]
            .UpdateCardPreview(card, CardPreviewMode.Normal, first, true);

        Assert.Equal(8, card.DynamicVars.Damage.PreviewValue);
        Assert.Equal(5, card.DynamicVars[ChickenFootCheeseScript.FollowUpDamageVarName].PreviewValue);
        string cardDescription = card.GetDescriptionForPile(PileType.Hand);
        Assert.Contains("5", cardDescription);

        await PlayAndActivateScript(card);

        const int expectedDamage = (ChickenFootCheeseScript.BaseDamage + 2)
            + (ChickenFootCheeseScript.BaseFollowUpDamage + 2);
        Assert.Equal(firstHpBefore - expectedDamage, first.CurrentHp);
        Assert.Equal(secondHpBefore - expectedDamage, second.CurrentHp);
        Assert.Equal(
            ScriptChickenFootCheesePower.BaseFollowUpDamage,
            Assert.IsType<ScriptChickenFootCheesePower>(
                Player.Creature.GetPower<ScriptChickenFootCheesePower>()).FollowUpDamage);
    }

    [Fact]
    public async Task Upgraded_card_deals_six_then_nine_and_stores_nine_in_script()
    {
        var first = EnemyAt(0);
        var second = EnemyAt(1);
        int firstHpBefore = first.CurrentHp;
        int secondHpBefore = second.CurrentHp;
        var card = Upgrade(await AddToHand<ChickenFootCheeseScript>());

        Assert.Equal(
            ChickenFootCheeseScript.UpgradedFollowUpDamage,
            card.DynamicVars[ChickenFootCheeseScript.FollowUpDamageVarName].BaseValue);
        Assert.Equal(
            ChickenFootCheeseScript.UpgradedFollowUpDamage,
            card.DynamicVars[ChickenFootCheeseScript.ScriptFollowUpDamageVarName].BaseValue);
        var strikePreview = Upgrade(Combat.CreateCard<ChickenFootCheeseScriptStrikePreview>(Player));
        Assert.Equal(ChickenFootCheeseScript.BaseDamage, strikePreview.DynamicVars.Damage.BaseValue);
        Assert.Equal(
            ChickenFootCheeseScript.UpgradedFollowUpDamage,
            strikePreview.DynamicVars[ChickenFootCheeseScriptStrikePreview.FollowUpDamageVarName].BaseValue);

        await PlayAndActivateScript(card);

        const int expectedDamage = ChickenFootCheeseScript.BaseDamage
            + ChickenFootCheeseScript.UpgradedFollowUpDamage;
        Assert.Equal(firstHpBefore - expectedDamage, first.CurrentHp);
        Assert.Equal(secondHpBefore - expectedDamage, second.CurrentHp);
        Assert.Equal(
            ScriptChickenFootCheesePower.UpgradedFollowUpDamage,
            Assert.IsType<ScriptChickenFootCheesePower>(
                Player.Creature.GetPower<ScriptChickenFootCheesePower>()).FollowUpDamage);
    }

    [Fact]
    public async Task Redirected_strike_deals_normal_damage_then_vigor_scaled_follow_up()
    {
        await PlayAndActivateScript(await AddToHand<ChickenFootCheeseScript>());
        await ApplyPower<VigorPower>(Player.Creature, 2);

        var first = EnemyAt(0);
        var second = EnemyAt(1);
        int firstHpBefore = first.CurrentHp;
        int secondHpBefore = second.CurrentHp;
        var strike = await AddToHand<StrikeIronclad>();
        var capability = Assert.IsType<ChickenFootCheeseStrikeCapability>(
            strike.Capability<ChickenFootCheeseStrikeCapability>());

        strike.DynamicVars.Damage.UpdateCardPreview(
            strike,
            CardPreviewMode.Normal,
            first,
            runGlobalHooks: true);
        capability.DynamicVars[ChickenFootCheeseStrikeCapability.FollowUpDamageVarName]
            .UpdateCardPreview(strike, CardPreviewMode.Normal, first, true);

        Assert.Equal(8, strike.DynamicVars.Damage.PreviewValue);
        Assert.Equal(
            5,
            capability.DynamicVars[ChickenFootCheeseStrikeCapability.FollowUpDamageVarName].PreviewValue);
        Assert.Equal(
            "对随机两名敌人先造成[green]8[/green]点伤害，再造成[green]5[/green]点伤害。",
            strike.GetDescriptionForPile(PileType.Hand));

        await Play(strike);

        const int expectedDamage = (6 + 2)
            + (ScriptChickenFootCheesePower.BaseFollowUpDamage + 2);
        Assert.Equal(firstHpBefore - expectedDamage, first.CurrentHp);
        Assert.Equal(secondHpBefore - expectedDamage, second.CurrentHp);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Longtao_strike_nested_body_is_replaced_in_hand_and_during_play(bool upgraded)
    {
        await PlayAndActivateScript(await AddToHand<ChickenFootCheeseScript>());
        var strike = await AddToHand<StrikeLongtao>();
        if (upgraded) Upgrade(strike);
        await ApplyPower<VigorPower>(Player.Creature, 1);
        var capability = strike.Capability<ChickenFootCheeseStrikeCapability>()!;
        strike.DynamicVars.Damage.UpdateCardPreview(strike, CardPreviewMode.Normal, EnemyAt(0), true);
        capability.DynamicVars[ChickenFootCheeseStrikeCapability.FollowUpDamageVarName]
            .UpdateCardPreview(strike, CardPreviewMode.Normal, EnemyAt(0), true);
        string damage = upgraded ? "10" : "7";
        string expected = $"对随机两名敌人先造成[green]{damage}[/green]点伤害，再造成[green]4[/green]点伤害。";

        foreach (PileType pile in new[] { PileType.Hand, PileType.Play })
        {
            await CardPileCmd.Add(strike, pile);
            string text = strike.GetDescriptionForPile(pile);
            Assert.Equal(expected, text);
            Assert.DoesNotContain("BodyText", text);
        }
    }

    [Fact]
    public async Task Upgraded_script_grants_nine_follow_up_regardless_of_strike_upgrade()
    {
        await PlayAndActivateScript(Upgrade(await AddToHand<ChickenFootCheeseScript>()));
        var normalStrike = await AddToHand<StrikeIronclad>();
        var upgradedStrike = Upgrade(await AddToHand<StrikeIronclad>());
        var normalCapability = Assert.IsType<ChickenFootCheeseStrikeCapability>(
            normalStrike.Capability<ChickenFootCheeseStrikeCapability>());
        var upgradedCapability = Assert.IsType<ChickenFootCheeseStrikeCapability>(
            upgradedStrike.Capability<ChickenFootCheeseStrikeCapability>());

        normalCapability.DynamicVars[ChickenFootCheeseStrikeCapability.FollowUpDamageVarName]
            .UpdateCardPreview(normalStrike, CardPreviewMode.Normal, EnemyAt(0), true);
        upgradedCapability.DynamicVars[ChickenFootCheeseStrikeCapability.FollowUpDamageVarName]
            .UpdateCardPreview(upgradedStrike, CardPreviewMode.Normal, EnemyAt(0), true);
        Assert.Equal(
            ScriptChickenFootCheesePower.UpgradedFollowUpDamage,
            normalCapability.DynamicVars[ChickenFootCheeseStrikeCapability.FollowUpDamageVarName].PreviewValue);
        Assert.Equal(
            ScriptChickenFootCheesePower.UpgradedFollowUpDamage,
            upgradedCapability.DynamicVars[ChickenFootCheeseStrikeCapability.FollowUpDamageVarName].PreviewValue);
        Assert.Contains("9", normalStrike.GetDescriptionForPile(PileType.Hand));

        await ApplyPower<VigorPower>(Player.Creature, 2);
        var first = EnemyAt(0);
        var second = EnemyAt(1);
        int firstHpBefore = first.CurrentHp;
        int secondHpBefore = second.CurrentHp;
        await Play(upgradedStrike);

        const int expectedDamage = (9 + 2)
            + (ScriptChickenFootCheesePower.UpgradedFollowUpDamage + 2);
        Assert.Equal(firstHpBefore - expectedDamage, first.CurrentHp);
        Assert.Equal(secondHpBefore - expectedDamage, second.CurrentHp);
    }
    [Theory]
    [InlineData(PileType.Hand)]
    [InlineData(PileType.Play)]
    [InlineData(PileType.Draw)]
    [InlineData(PileType.Discard)]
    [InlineData(PileType.Exhaust)]
    public async Task Local_preview_does_not_change_serialized_state_and_only_displays_in_hand_or_play(PileType pile)
    {
        await PlayAndActivateScript(Upgrade(await AddToHand<ChickenFootCheeseScript>()));
        var strike = await AddToHand<StrikeIronclad>();
        await CardPileCmd.Add(strike, pile);
        await ApplyPower<VigorPower>(Player.Creature, 2);
        var capability = strike.Capability<ChickenFootCheeseStrikeCapability>()!;
        var damage = capability.DynamicVars[ChickenFootCheeseStrikeCapability.FollowUpDamageVarName];
        byte[] before = Serialize(strike);
        string? capabilityState = capability.SaveState()?.ToJsonString();

        foreach (bool globalHooks in new[] { false, true })
        {
            damage.UpdateCardPreview(strike, CardPreviewMode.Normal, EnemyAt(0), globalHooks);
            string text = strike.GetDescriptionForPile(pile);
            bool onTable = pile is PileType.Hand or PileType.Play;
            Assert.Equal(onTable ? (globalHooks ? 11 : 9) : 3, damage.PreviewValue);
            Assert.Equal(3, damage.BaseValue);
            Assert.Equal(onTable, text.Contains("再造成"));
            Assert.Equal(before, Serialize(strike));
            Assert.Equal(capabilityState, capability.SaveState()?.ToJsonString());
        }

        if (pile is PileType.Hand or PileType.Play)
        {
            Assert.DoesNotContain("再造成", strike.GetDescriptionForPile(PileType.Deck));
            await CardPileCmd.Add(strike, PileType.Draw);
            damage.UpdateCardPreview(strike, CardPreviewMode.Normal, EnemyAt(0), true);
            Assert.Equal(3, damage.PreviewValue);
            Assert.DoesNotContain("再造成", strike.GetDescriptionForPile(PileType.Draw));
            Assert.Equal(before, Serialize(strike));
        }

        static byte[] Serialize(CardModel card)
        {
            var writer = new PacketWriter();
            card.ToSerializable().Serialize(writer);
            return writer.Buffer.Take(writer.BytePosition).ToArray();
        }
    }

}
