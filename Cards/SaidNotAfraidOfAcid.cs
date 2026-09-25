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
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Cards;

/// <summary>由《咱家不怕酸》的「稍作修改」产生：攻击后将一张不能被打出的牌变化为酒。</summary>
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
			.Where(IsTransformableUnplayable)
			.ToList();
		List<CardTransformation> transformations = [];

		foreach (CardModel unplayableCard in unplayableCards)
		{
			CardModel wine = combatState.CreateCard<Wine>(Owner);
			if (IsUpgraded)
			{
				CardCmd.Upgrade(wine);
			}
			transformations.Add(new CardTransformation(unplayableCard, wine));
		}

		await CardCmd.Transform(transformations, null);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(UpgradedDamage - BaseDamage);
	}

	private static bool IsTransformableUnplayable(CardModel card) =>
		card.IsTransformable && card.Keywords.Contains(CardKeyword.Unplayable);
}
