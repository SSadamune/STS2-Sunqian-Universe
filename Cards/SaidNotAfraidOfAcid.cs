#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Audio;
using Squ.Script;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Cards;

/// <summary>由《咱家不怕酸》的「稍作修改」产生：攻击后消耗不能被打出的牌，并获得等量的酒。</summary>
[RegisterCard(typeof(TokenCardPool), StableEntryStem = "said_not_afraid_of_acid")]
public sealed class SaidNotAfraidOfAcid : ModCardTemplate
{
	public const int BaseDamage = 8;
	public const int UpgradedDamage = 11;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(BaseDamage, ValueProp.Move),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
		HoverTipFactory.FromKeyword(CardKeyword.Unplayable),
		HoverTipFactory.FromCard<Wine>(IsUpgraded),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/SaidNotAfraidOfAcid.png");

	public SaidNotAfraidOfAcid()
		: base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
		SquSfx.Play(SquSfx.SaidNotAfraidOfAcidEvent);

		await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.FromCard(this, cardPlay)
			.Targeting(cardPlay.Target)
			.WithHitFx("vfx/vfx_attack_slash")
			.Execute(choiceContext);

		ICombatState combatState = CombatState
			?? throw new InvalidOperationException("SaidNotAfraidOfAcid requires an active combat.");
		List<CardModel> unplayableCards = PileType.Hand.GetPile(Owner).Cards
			.Where(card => card.Keywords.Contains(CardKeyword.Unplayable))
			.ToList();

		foreach (CardModel unplayableCard in unplayableCards)
		{
			await CardCmd.Exhaust(choiceContext, unplayableCard);
		}

		for (int i = 0; i < unplayableCards.Count; i++)
		{
			await GeneratedCombatCards.AddToHandInCombat<Wine>(
				combatState,
				Owner,
				IsUpgraded,
				Owner);
		}
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(UpgradedDamage - BaseDamage);
	}
}
