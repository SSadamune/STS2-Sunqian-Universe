using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Character;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Cards;

/// <summary>
/// 回合结束时若在手牌中，自动打出并攻击随机敌人（参考 Stampede / I Am Invincible 的
/// <see cref="CardModel.AfterAutoPostPlayPhaseEntered" /> + <see cref="CardCmd.AutoPlay" /> 模式）。
/// </summary>
[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "starry_night_strike")]
public sealed class StarryNightStrike : ModCardTemplate
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(6m, ValueProp.Move),
	];

	protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/StarryNightStrike.png");

	public StarryNightStrike()
		: base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

		await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.FromCard(this)
			.Targeting(cardPlay.Target)
			.WithHitFx("vfx/vfx_attack_slash")
			.Execute(choiceContext);
	}

	public override async Task AfterAutoPostPlayPhaseEntered(PlayerChoiceContext choiceContext, Player player)
	{
		if (player != Owner || Pile?.Type != PileType.Hand || Keywords.Contains(CardKeyword.Unplayable))
		{
			return;
		}

		await CardCmd.AutoPlay(choiceContext, this, null);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(3m);
	}
}
