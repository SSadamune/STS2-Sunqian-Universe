using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Character;
using Squ.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// 继续享受：获得格挡，并像攻击牌一样消耗活力来增加格挡。
/// 活力减少走 <see cref="PowerCmd.ModifyAmount"/>，因此平湖惊雷等监听仍会触发。
/// </summary>
[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "keep_enjoying")]
public sealed class KeepEnjoying : ModCardTemplate
{
	public const decimal CanonicalBlock = 8m;
	public const decimal UpgradedBlock = 11m;

	private static readonly ValueProp BlockProps = ValueProp.Move;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new VigorBlockVar(CanonicalBlock),
	];

	public override bool GainsBlock => true;

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<VigorPower>(),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/KeepEnjoying.png");

	protected override bool ShouldGlowGoldInternal =>
		Owner?.Creature != null && SquVigorSnapshot.GetAmount(Owner.Creature) > 0;

	public KeepEnjoying()
		: base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		int spentVigor = await SquVigorSnapshot.SpendAll(choiceContext, Owner.Creature, this);
		decimal block = DynamicVars.Block.BaseValue + spentVigor;
		await CreatureCmd.GainBlock(Owner.Creature, block, BlockProps, cardPlay);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Block.UpgradeValueBy(UpgradedBlock - CanonicalBlock);
	}

	/// <summary>
	/// 卡面格挡预览包含当前活力；<see cref="DynamicVar.BaseValue"/> 保持印面数值，
	/// 这样 <c>{Block:diff()}</c> 才能把加成后的数字标绿。
	/// </summary>
	private sealed class VigorBlockVar : BlockVar
	{
		public VigorBlockVar(decimal baseValue)
			: base(baseValue, BlockProps)
		{
		}

		public override void UpdateCardPreview(
			CardModel card,
			CardPreviewMode previewMode,
			Creature? target,
			bool runGlobalHooks)
		{
			if (card.Owner?.Creature is not Creature owner)
			{
				PreviewValue = BaseValue;
				return;
			}

			decimal amount = BaseValue + SquVigorSnapshot.GetAmount(owner);
			if (!runGlobalHooks || card.CombatState is not { } combatState)
			{
				PreviewValue = amount;
				return;
			}

			PreviewValue = Math.Max(
				0m,
				Hook.ModifyBlock(
					combatState,
					owner,
					amount,
					BlockProps,
					card,
					null,
					out _));
		}
	}
}
