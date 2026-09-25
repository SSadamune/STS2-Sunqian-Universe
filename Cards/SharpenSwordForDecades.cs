#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using Squ.Character;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Cards;

/// <summary>
/// 磨剑十几年：被保留时在本场战斗增加铸造值；未被打出清零的增量会在战斗结束时
/// 写回牌组原卡，并通过存档属性永久保存。
/// </summary>
[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "sharpen_sword_for_decades")]
public sealed class SharpenSwordForDecades : ChargeCardTemplate
{
	public const int BaseForge = 0;
	public const int BaseChargeForge = 3;
	public const int UpgradedChargeForge = 4;
	public const string ChargeForgeVarName = "ChargeForge";

	private int _retainedForgeBonus;

	[SavedProperty]
	public int RetainedForgeBonus
	{
		get => _retainedForgeBonus;
		set
		{
			AssertMutable();
			_retainedForgeBonus = value;
			DynamicVars.Forge.BaseValue = BaseForge + value;
		}
	}

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new ChargedForgeVar(BaseForge + RetainedForgeBonus),
		new DynamicVar(ChargeForgeVarName, BaseChargeForge),
	];

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		CardKeyword.Retain,
		.. base.CanonicalKeywords,
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
		HoverTipFactory.FromForge();

	protected override string ChargeEffectLocKey => Id.Entry + ".chargeEffect";

	protected override ChargeHooks Charge => new(
		OnRetained: IncreaseForgeUntilPlayed,
		OnPowerAmountChanged: null,
		Clear: ResetUnretainedForge);

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/SharpenSwordForDecades.png");

	protected override bool ShouldGlowGoldInternal =>
		DynamicVars.Forge.BaseValue > BaseForge + RetainedForgeBonus;

	public SharpenSwordForDecades()
		: base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await ForgeCmd.Forge(DynamicVars.Forge.BaseValue, Owner, this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars[ChargeForgeVarName]
			.UpgradeValueBy(UpgradedChargeForge - BaseChargeForge);
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		PersistUnplayedCharge();
		return Task.CompletedTask;
	}

	private Task IncreaseForgeUntilPlayed(PlayerChoiceContext choiceContext)
	{
		DynamicVars.Forge.BaseValue += DynamicVars[ChargeForgeVarName].BaseValue;
		RefreshCardVisuals();
		return Task.CompletedTask;
	}

	private void ResetUnretainedForge()
	{
		DynamicVars.Forge.BaseValue = BaseForge + RetainedForgeBonus;
		RefreshCardVisuals();
	}

	private void PersistUnplayedCharge()
	{
		if (DeckVersion is not SharpenSwordForDecades deckVersion
			|| deckVersion.Pile?.Type != PileType.Deck)
		{
			return;
		}

		int unplayedCharge = DynamicVars.Forge.IntValue - BaseForge - RetainedForgeBonus;
		if (unplayedCharge <= 0)
		{
			return;
		}

		deckVersion.RetainedForgeBonus += unplayedCharge;
		RetainedForgeBonus += unplayedCharge;
	}

	private sealed class ChargedForgeVar : ForgeVar
	{
		public ChargedForgeVar(int forge)
			: base(forge)
		{
		}

		public override void UpdateCardPreview(
			CardModel card,
			CardPreviewMode previewMode,
			Creature? target,
			bool runGlobalHooks)
		{
			if (card is not SharpenSwordForDecades sharpenSword)
			{
				return;
			}

			EnchantedValue = BaseForge + sharpenSword.RetainedForgeBonus;
			PreviewValue = BaseValue;
		}
	}
}
