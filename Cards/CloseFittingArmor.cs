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
using Squ.Audio;
using Squ.Character;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "close_fitting_armor")]
public sealed class CloseFittingArmor : ModCardTemplate
{
	private const decimal BasePlating = 3m;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DexterityPlatingVar(BasePlating),
	];

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		CardKeyword.Retain,
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<PlatingPower>(),
		HoverTipFactory.FromPower<DexterityPower>(),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/CloseFittingArmor.png");

	public CloseFittingArmor()
		: base(0, CardType.Power, CardRarity.Rare, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		SquSfx.Play(SquSfx.CloseFittingArmorEvent);
		decimal plating = GetPlatingAmount(Owner.Creature, DynamicVars[nameof(PlatingPower)].BaseValue);
		await PowerCmd.Apply<PlatingPower>(
			choiceContext,
			Owner.Creature,
			plating,
			Owner.Creature,
			this);
	}

	protected override void OnUpgrade()
	{
		AddKeyword(CardKeyword.Innate);
	}

	private static decimal GetPlatingAmount(Creature owner, decimal baseAmount)
	{
		decimal dexterity = owner.GetPower<DexterityPower>()?.Amount ?? 0m;
		return Math.Max(0m, baseAmount + dexterity);
	}

	/// <summary>
	/// 卡面显示当前敏捷加成后的覆甲；实际打出时会重新读取敏捷并将结果作为固定层数施加。
	/// </summary>
	private sealed class DexterityPlatingVar : PowerVar<PlatingPower>
	{
		public DexterityPlatingVar(decimal baseValue)
			: base(baseValue)
		{
		}

		public override void UpdateCardPreview(
			CardModel card,
			CardPreviewMode previewMode,
			Creature? target,
			bool runGlobalHooks)
		{
			decimal amount = GetPlatingAmount(card.Owner.Creature, BaseValue);
			if (!runGlobalHooks || card.CombatState is not { } combatState)
			{
				PreviewValue = amount;
				return;
			}

			PreviewValue = Hook.ModifyPowerAmountGiven(
				combatState,
				ModelDb.Power<PlatingPower>(),
				card.Owner.Creature,
				amount,
				target,
				card,
				out IEnumerable<AbstractModel> _);
		}
	}
}
