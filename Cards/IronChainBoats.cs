#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Squ.Audio;
using Squ.Character;
using Squ.Combat;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Cards;

/// <summary>铁索连舟：令目标后续受到的灼烧伤害扩散至其余敌人。</summary>
[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "iron_chain_boats")]
public sealed class IronChainBoats : SlightRevisionCardTemplate<FarmingGeneral>
{
	public const int ChainStacks = 1;

	protected override bool IsSlightRevisionTargetUpgraded => false;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<IronChainPower>(ChainStacks),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		..base.AdditionalHoverTips,
		HoverTipFactory.FromPower<BurningPower>(),
		HoverTipFactory.FromKeyword(SquKeywords.Environmental),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/IronChainBoats.png");

	public IronChainBoats()
		: base(1, CardType.Power, CardRarity.Rare, TargetType.AnyEnemy)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
		SquSfx.Play(SquSfx.IronChainBoatsPlayEvent);
		await PowerCmd.Apply<IronChainPower>(
			choiceContext,
			cardPlay.Target,
			DynamicVars[nameof(IronChainPower)].BaseValue,
			Owner.Creature,
			this);
	}

	protected override void OnUpgrade()
	{
		AddKeyword(CardKeyword.Innate);
	}
}
