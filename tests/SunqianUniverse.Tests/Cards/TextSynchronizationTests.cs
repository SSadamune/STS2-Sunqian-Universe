using System.Text.Json;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using Squ.Cards;
using Squ.Powers;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Cards;

public sealed class TextSynchronizationTests : SunqianCombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("text-synchronization");

    [Theory]
    [InlineData(false, 3)]
    [InlineData(true, 4)]
    public async Task Defend_block_bonus_wording_tracks_base_and_upgraded_values_in_card_and_power(
        bool upgraded, int amount)
    {
        var card = await AddToHand<TooKindToBeTrue>();
        if (upgraded)
        {
            Upgrade(card);
        }
        Assert.Contains($"给予的[gold]格挡[/gold]+{amount}。",
            card.GetDescriptionForPile(PileType.Hand));
        await ApplyPower<TooKindToBeTruePower>(Player.Creature, amount);
        var power = Player.Creature.GetPower<TooKindToBeTruePower>()!;
        var description = power.SmartDescription;
        description.Add("Amount", power.Amount);
        Assert.Contains($"给予的[gold]格挡[/gold]+[blue]{amount}[/blue]。",
            description.GetFormattedText());
    }

    [Theory]
    [InlineData("zhs")]
    [InlineData("eng")]
    public void Both_languages_preserve_the_updated_text_and_dynamic_variables(string language)
    {
        using var cards = ReadTable(language, "cards");
        using var powers = ReadTable(language, "powers");
        using var keywords = ReadTable(language, "card_keywords");
        using var relics = ReadTable(language, "relics");
        string boxLunch = relics.RootElement.GetProperty("SUNQIAN_UNIVERSE_RELIC_BOX_LUNCH.description").GetString()!;
        string abundantBoxLunch = relics.RootElement.GetProperty("SUNQIAN_UNIVERSE_RELIC_ABUNDANT_BOX_LUNCH.description").GetString()!;
        Assert.Contains("[blue]2[/blue]", boxLunch);
        Assert.Contains(language == "zhs" ? "休眠" : "falls asleep", boxLunch);
        Assert.Contains(language == "zhs" ? "每回合限一次" : "Once per turn", abundantBoxLunch);
        Assert.Equal(2, abundantBoxLunch.Split("{energyPrefix:energyIcons(1)}").Length - 1);
        string wrap = keywords.RootElement.GetProperty("SUNQIAN_UNIVERSE_KEYWORD_WRAP.description").GetString()!;
        Assert.Contains(language == "zhs" ? "选择其中一个" : "choose one", wrap);
        Assert.Contains(language == "zhs" ? "[gold]剧本[/gold]能力" : "[gold]Script[/gold] Power", wrap);
        Assert.Contains(language == "zhs" ? "令一个剧本失效" : "end one Script", wrap);
        foreach (var table in new[] { cards, powers, keywords })
        {
            foreach (var entry in table.RootElement.EnumerateObject())
            {
                string value = entry.Value.GetString()!.Replace("[gold]", "").Replace("[/gold]", "");
                Assert.DoesNotContain("剧本状态", value);
            }
        }
        string Card(string suffix) => cards.RootElement.GetProperty(
            "SUNQIAN_UNIVERSE_CARD_" + suffix).GetString()!;
        string Power(string suffix) => powers.RootElement.GetProperty(
            "SUNQIAN_UNIVERSE_POWER_TOO_KIND_TO_BE_TRUE_POWER." + suffix).GetString()!;

        Assert.Contains(language == "zhs" ? "[gold]可能有用[/gold]" : "[gold]potentially useful[/gold]",
            Card("METAMORPHOSIS.description"));
        Assert.Equal(language == "zhs"
                ? "取决于资源和战况，会出现不同的备选牌。"
                : "Different card choices appear depending on your resources and the combat situation.",
            Card("METAMORPHOSIS.potentiallyUsefulDescription"));
        Assert.Equal(language == "zhs"
                ? "激活此[gold]剧本[/gold]，若激活过则还会将数值叠加。"
                : "Activate this [gold]Script[/gold]; if it has been activated before, also stack its values.",
            Card("SCRIPT_ACTIVATE_REINFORCE"));
        Assert.Contains(language == "zhs" ? "你的牌被[gold]消耗[/gold]后" : "After one of your cards is [gold]Exhausted[/gold]",
            Card("RUTHLESS_STRIKE.chargeEffect"));
        Assert.Contains("{ChargeForge:diff()}", Card("SHARPEN_SWORD_FOR_DECADES.chargeEffect"));
        Assert.DoesNotContain("[blue]", Card("FINGER_SNAP_STRIKE.chargeEffect"));
        Assert.Contains("{IfUpgraded:show:", Card("DISTRIBUTED_SCREENWRITER.description"));
        Assert.False(powers.RootElement.TryGetProperty(
            "SUNQIAN_UNIVERSE_POWER_DISTRIBUTED_SCREENWRITER_POWER.smartDescriptionWithDraw", out _));
        if (language == "zhs")
        {
            Assert.Equal("你打出的攻击牌会保留[gold]活力[/gold]加成。",
                Card("SCRIPT_RECORD_XILIANG_SAVAGE.description"));
            Assert.Equal("博望悖论・进军", Card("SCRIPT_RECORD_BOWANG_PARADOX_ADVANCE.title"));
            Assert.Equal("博望悖论・放火", Card("SCRIPT_RECORD_BOWANG_PARADOX_ARSON.title"));
            Assert.DoesNotContain("你", Card("SEND_HORSE_WITHOUT_HAY.chargeEffect"));
            Assert.DoesNotContain("当", Card("STARGAZING.description"));
            Assert.Contains("少于", Card("TRANSPARENT_HOLE.description"));
            foreach (var entry in cards.RootElement.EnumerateObject().Where(entry =>
                entry.Name.StartsWith("SUNQIAN_UNIVERSE_CARD_FLAME_STRIKE.")))
            {
                Assert.DoesNotContain("如果", entry.Value.GetString()!);
                Assert.DoesNotContain("两", entry.Value.GetString()!);
            }
            Assert.Contains("回合结束时[gold]铸造[/gold]", Card("SHARPEN_SWORD_FOR_DECADES.chargeEffect"));
        }
        Assert.Equal(2, Card("BOMBARD_CHIBI_SCRIPT.description").Count(c => c == '\n'));
        Assert.Contains("+{ExtraBlock:diff()}", Card("TOO_KIND_TO_BE_TRUE.description"));
        Assert.Equal(Power("description"), Power("smartDescription"));
        Assert.Contains("+[blue]{Amount}[/blue]", Power("description"));
        foreach (var entry in cards.RootElement.EnumerateObject().Where(entry =>
            entry.Name.StartsWith("SUNQIAN_UNIVERSE_CARD_SCRIPT_RECORD_") && entry.Name.EndsWith(".title")))
        {
            Assert.DoesNotContain(language == "zhs" ? "剧本：" : "Script: ", entry.Value.GetString()!);
        }
    }

    private static JsonDocument ReadTable(string language, string table) =>
        JsonDocument.Parse(Godot.FileAccess.GetFileAsString(
            $"res://sunqian-universe/localization/{language}/{table}.json"));
}
