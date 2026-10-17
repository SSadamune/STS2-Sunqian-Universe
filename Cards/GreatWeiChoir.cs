using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Character;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "great_wei_choir")]
public sealed class GreatWeiChoir : ModCardTemplate
{
	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<BurningPower>(),
		HoverTipFactory.FromPower<VigorPower>(),
	];

	public GreatWeiChoir()
		: base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await PowerCmd.Apply<GreatWeiChoirPower>(
			choiceContext,
			Owner.Creature,
			1m,
			Owner.Creature,
			this);

		if (!IsUpgraded || CombatState is not { } combatState)
		{
			return;
		}

		List<BurningPower> enemyBurnings = combatState.Enemies
			.Where(static enemy => enemy.IsAlive)
			.Select(static enemy => enemy.GetPower<BurningPower>())
			.OfType<BurningPower>()
			.ToList();
		foreach (BurningPower burning in enemyBurnings)
		{
			if (burning.Owner.IsAlive && burning.Owner.GetPower<BurningPower>() == burning)
			{
				await burning.TriggerAsync(choiceContext, cardSource: this);
			}
		}
	}

	protected override void OnUpgrade()
	{
	}
}
