using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using Squ.Audio;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>焚城：消耗。分两轮给予所有敌人灼烧。</summary>
[RegisterCard(typeof(TokenCardPool), StableEntryStem = "burn_city")]
public sealed class BurnCity : ModCardTemplate
{
	public const int BaseBurning = 5;
	public const int BaseRepeatCount = 2;
	public const int UpgradedRepeatCount = 3;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<BurningPower>(BaseBurning),
	];

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		CardKeyword.Exhaust,
	];

	protected override HashSet<CardTag> CanonicalTags => [SquCardTags.Burning];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<BurningPower>(),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/BurnCity.png");

	public BurnCity()
		: base(1, CardType.Skill, CardRarity.Token, TargetType.AllEnemies)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ICombatState combatState = CombatState
			?? throw new InvalidOperationException("BurnCity requires an active combat.");
		decimal burning = DynamicVars[nameof(BurningPower)].BaseValue;
		SquSfx.Play(SquSfx.BurnCityEvent);

		int repeatCount = IsUpgraded ? UpgradedRepeatCount : BaseRepeatCount;
		for (int i = 0; i < repeatCount; i++)
		{
			foreach (Creature enemy in combatState.HittableEnemies)
			{
				if (!enemy.IsAlive)
				{
					continue;
				}

				await PowerCmd.Apply<BurningPower>(
					choiceContext,
					enemy,
					burning,
					Owner.Creature,
					this);
			}
		}
	}

	protected override void OnUpgrade()
	{
	}
}
