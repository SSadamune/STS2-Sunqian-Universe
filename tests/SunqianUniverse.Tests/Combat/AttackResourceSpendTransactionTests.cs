using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Combat;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.Combat;

public sealed class AttackResourceSpendTransactionTests : SunqianCombatTestSuite
{
	protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
		battle.Player<Ironclad>().AddEnemy<BigDummy>().WithSeed("attack-resource-transaction");

	[Fact]
	public async Task Records_only_registered_negative_debits_and_clamps_them_to_the_opening_budget()
	{
		await ApplyPower<VigorPower>(Player.Creature, 4);
		var root = await AddToHand<StrikeIronclad>();
		var child = await AddToHand<StrikeIronclad>();
		VigorPower vigor = Assert.IsType<VigorPower>(Player.Creature.GetPower<VigorPower>());
		var transaction = new AttackResourceSpendTransaction(Player.Creature, root);

		transaction.Record(vigor, -2m, child);
		transaction.Record(vigor, -2m, null);
		transaction.Record(vigor, 3m, root);
		Assert.Equal(0m, transaction.VigorSpent);

		transaction.Record(vigor, -3m, root);
		transaction.Record(vigor, -3m, root);
		Assert.Equal(4m, transaction.VigorSpent);
		Assert.Equal(0m, transaction.GetRestorableVigor());
	}

	[Fact]
	public async Task Nested_attack_is_counted_only_after_the_parent_explicitly_registers_it()
	{
		await ApplyPower<VigorPower>(Player.Creature, 4);
		var root = await AddToHand<StrikeIronclad>();
		var child = await AddToHand<StrikeIronclad>();
		VigorPower vigor = Assert.IsType<VigorPower>(Player.Creature.GetPower<VigorPower>());
		var transaction = new AttackResourceSpendTransaction(Player.Creature, root);

		transaction.Record(vigor, -2m, child);
		Assert.False(transaction.SpentAny);

		transaction.RegisterSource(child);
		transaction.Record(vigor, -2m, child);
		Assert.Equal(2m, transaction.VigorSpent);
	}
}
