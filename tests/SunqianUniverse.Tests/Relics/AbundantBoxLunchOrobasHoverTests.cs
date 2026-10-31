using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Relics;
using Squ.Powers;
using Squ.Relics;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Relics;

public sealed class AbundantBoxLunchOrobasHoverTests : SunqianCombatTestSuite
{
	protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
		battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("abundant-box-lunch-orobas");

	[Fact]
	public void Orobas_preview_omits_box_lunch_and_greens_only_the_upgraded_phrases()
	{
		TouchOfOrobas orobas = (TouchOfOrobas)ModelDb.Relic<TouchOfOrobas>().ToMutable();
		orobas.SetupForTests(
			ModelDb.Relic<BoxLunchRelic>().Id,
			ModelDb.Relic<AbundantBoxLunchRelic>().Id);

		HoverTip[] tips = orobas.HoverTipsExcludingRelic.OfType<HoverTip>().ToArray();
		string sentryId = ModelDb.Power<ScriptSentryPower>().Id.ToString();
		string viceGeneralId = ModelDb.Power<ScriptViceGeneralPower>().Id.ToString();

		Assert.DoesNotContain(tips, tip => tip.CanonicalModel is BoxLunchRelic);
		Assert.DoesNotContain(tips, tip => tip.Id == sentryId);
		Assert.Equal(1, tips.Count(tip => tip.Id.Contains("ENERGY.title", StringComparison.Ordinal)));

		HoverTip abundant = Assert.Single(tips, tip => tip.CanonicalModel is AbundantBoxLunchRelic);
		Assert.Contains("[green]前三次[/green]", abundant.Description);

		HoverTip viceGeneral = Assert.Single(tips, tip => tip.Id == viceGeneralId);
		Assert.Contains("[green]3点额外伤害[/green]", viceGeneral.Description);
		Assert.Contains("[green]1点额外格挡[/green]", viceGeneral.Description);

		Assert.DoesNotContain("[green]", ModelDb.Relic<AbundantBoxLunchRelic>().HoverTip.Description);
		string powerDescription = ModelDb.Power<ScriptViceGeneralPower>().GetDumbHoverTip().Description;
		Assert.Contains("[blue]3[/blue]", powerDescription);
		Assert.Contains("[blue]1[/blue]", powerDescription);
		Assert.DoesNotContain("[green]", powerDescription);
	}

	[Fact]
	public async Task Vice_general_adds_three_strike_damage_and_one_defend_block()
	{
		Creature enemy = EnemyAt(0);
		await ApplyPower<ScriptViceGeneralPower>(Player.Creature, 1);
		int hpBefore = enemy.CurrentHp;

		await Play(await AddToHand<StrikeIronclad>(), enemy);
		Assert.Equal(hpBefore - 6 - (int)ScriptViceGeneralPower.StrikeBonusDamage, enemy.CurrentHp);

		await Play(await AddToHand<DefendIronclad>());
		Assert.Equal(5 + (int)ScriptViceGeneralPower.DefendBonusBlock, Player.Creature.Block);
	}
}
