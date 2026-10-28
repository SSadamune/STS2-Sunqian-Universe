using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Relics;
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

		Assert.DoesNotContain(
			tips,
			tip => tip.CanonicalModel is BoxLunchRelic and not AbundantBoxLunchRelic);
		Assert.Equal(1, tips.Count(tip => tip.Id.Contains("ENERGY.title", StringComparison.Ordinal)));

		HoverTip abundant = Assert.Single(tips, tip => tip.CanonicalModel is AbundantBoxLunchRelic);
		Assert.Contains("[green]每回合开始时，你获得", abundant.Description);
		Assert.Contains("[green]你每回合可以从", abundant.Description);

		Assert.DoesNotContain("[green]", ModelDb.Relic<AbundantBoxLunchRelic>().HoverTip.Description);
	}
}
