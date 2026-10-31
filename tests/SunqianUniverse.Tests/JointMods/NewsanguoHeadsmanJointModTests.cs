using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using Squ.Interop;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.JointMods;

/// <summary>
/// 击杀不能结束整场战斗，否则打出后的补回赶在战斗结束前执行不到。
/// </summary>
public sealed class NewsanguoHeadsmanJointModTests : SunqianCombatTestSuite
{
	private static readonly ModelId CaoWeiCharacterId =
		new("CHARACTER", "NEWSANGUO_CHARACTER_CAO_WEI_CHARACTER");

	protected override void ConfigureBattle(CombatTestBattleBuilder battle)
	{
		CharacterModel caoWei = ModelDb.GetById<CharacterModel>(CaoWeiCharacterId);
		battle
			.Player(caoWei)
			.AddEnemy<BigDummy>()
			.AddEnemy<BigDummy>()
			.WithSeed("joint-newsanguo-headsman");
	}

	[Fact]
	public async Task Headsman_restores_spent_vigor_and_drunken_might_after_a_kill()
	{
		await Play(await AddToHand<HeadsmanScript>(), EnemyAt(0));
		await ApplyDrunkenMight(4);
		await CreatureCmd.SetCurrentHp(EnemyAt(0), 1);

		await Play(await AddToHand<StrikeIronclad>(), EnemyAt(0));

		Assert.Equal(HeadsmanScript.BaseVigor, (int)(Player.Creature.GetPower<VigorPower>()?.Amount ?? 0));
		Assert.Equal(4, NewsanguoPublicApiInterop.GetDrunkenMight(Player));
	}

	[Fact]
	public async Task Headsman_restores_at_most_the_opening_resources_after_a_nested_attack_kills()
	{
		await Play(await AddToHand<HeadsmanScript>(), EnemyAt(1));
		await PlayerCmd.GainEnergy(1, Player);
		await CardPileCmd.Add(PileType.Hand.GetPile(Player).Cards.ToArray(), PileType.Exhaust);
		await CardPileCmd.Add(PileType.Draw.GetPile(Player).Cards.ToArray(), PileType.Exhaust);
		await AddCardToPile<StrikeIronclad>(PileType.Hand);
		await ApplyDrunkenMight(4);
		await CreatureCmd.SetCurrentHp(EnemyAt(0), 20);

		await Play(await AddToHand<Blitzkrieg>(), EnemyAt(0));

		Assert.Equal(HeadsmanScript.BaseVigor, (int)(Player.Creature.GetPower<VigorPower>()?.Amount ?? 0));
		Assert.Equal(4, NewsanguoPublicApiInterop.GetDrunkenMight(Player));
	}

	private Task ApplyDrunkenMight(int amount) =>
		NewsanguoPublicApiInterop.ApplyDrunkenMight(
			new BlockingPlayerChoiceContext(),
			Player.Creature,
			amount,
			Player.Creature,
			null);
}
