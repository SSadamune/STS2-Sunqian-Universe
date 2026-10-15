using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Interop;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.JointMods;

public abstract class YjcmTinderJointModTestSuite : SunqianCombatTestSuite
{
    private static readonly ModelId HuoGongCardId =
        new("CARD", "YJCM_CARD_HUO_GONG");

    protected async Task AssertTinderHuoGongScenario(bool expectCombatUi)
    {
        Assert.True(YjcmPublicApiInterop.IsReady);
        Assert.False(YjcmPublicApiInterop.IsYjcmCharacter(Player.Character));
        const int tinderAmount = 4;
        await ApplyPower<TinderPower>(Player.Creature, tinderAmount);
        var tinder = Assert.IsType<TinderPower>(
            Player.Creature.GetPower<TinderPower>());
        IHoverTip huoGongTip = YjcmPublicApiInterop.CreateHuoGongHoverTip();
        string huoGongTitle = Assert.IsType<HoverTip>(huoGongTip).Title
            ?? throw new InvalidOperationException("Huo Gong hover title is missing.");
        string description = FormatPowerDescription(tinder);
        IHoverTip[] hoverTips = tinder.HoverTips.ToArray();

        if (expectCombatUi)
        {
            Assert.Contains(huoGongTitle, description);
            Assert.Contains(hoverTips, tip => tip.Id == huoGongTip.Id);
        }
        else
        {
            Assert.DoesNotContain(huoGongTitle, description);
            Assert.DoesNotContain(hoverTips, tip => tip.Id == huoGongTip.Id);
        }

        foreach (CardModel existing in PileType.Hand.GetPile(Player).Cards.ToArray())
        {
            await CardPileCmd.Add(existing, PileType.Discard);
        }

        var discarded = await AddToHand<Dazed>();
        CardModel huoGong = await AddToHand(HuoGongCardId);
        int baseHuoGong = huoGong.DynamicVars["Stacks"].IntValue;
        await Play(huoGong, EnemyAt(0));
        AssertInPile(discarded, PileType.Discard);
        PowerModel appliedHuoGong = Assert.Single(
            EnemyAt(0).Powers.Where(YjcmPublicApiInterop.IsHuoGongPower));
        // Huo Gong immediately discards Dazed after applying the power. That
        // discard triggers Fire Attack once and consumes one of its stacks.
        Assert.Equal(baseHuoGong + tinderAmount - 1, appliedHuoGong.Amount);
        Assert.Null(Player.Creature.GetPower<TinderPower>());
    }

    private static string FormatPowerDescription(PowerModel power)
    {
        var description = power.SmartDescription;
        description.Add("Amount", power.Amount);
        power.DynamicVars.AddTo(description);
        return description.GetFormattedText();
    }
}

public sealed class YjcmTinderPartyJointModTests : YjcmTinderJointModTestSuite
{
    private static readonly ModelId WuZhugeliangId =
        new("CHARACTER", "YJCM_CHARACTER_WU_ZHUGELIANG");

    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle
            .AddPlayer<Ironclad>(1)
            .AddPlayer(ModelDb.GetById<CharacterModel>(WuZhugeliangId), 2)
            .LocalNetId(1)
            .AddEnemy<BigDummy>()
            .WithSeed("yjcm-tinder-party-ui");

    [Fact]
    public Task Non_Yjcm_owner_gets_effect_and_ui_when_party_contains_Yjcm_character() =>
        AssertTinderHuoGongScenario(expectCombatUi: true);
}

public sealed class YjcmTinderNoPartyJointModTests : YjcmTinderJointModTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle
            .AddPlayer<Ironclad>(1)
            .AddPlayer<Silent>(2)
            .LocalNetId(1)
            .AddEnemy<BigDummy>()
            .WithSeed("yjcm-tinder-no-party-ui");

    [Fact]
    public Task Multiplayer_without_Yjcm_character_gets_effect_but_no_ui() =>
        AssertTinderHuoGongScenario(expectCombatUi: false);
}

public sealed class YjcmTinderSoloJointModTests : YjcmTinderJointModTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle
            .Player<Ironclad>()
            .AddEnemy<BigDummy>()
            .WithSeed("yjcm-tinder-solo-no-ui");

    [Fact]
    public Task Solo_non_Yjcm_character_gets_effect_but_no_ui() =>
        AssertTinderHuoGongScenario(expectCombatUi: false);
}
