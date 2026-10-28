using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using Squ.Audio;
using Squ.Character;
using Squ.Interop;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(TokenCardPool), StableEntryStem = "shangfanggu_sigh")]
public sealed class ShangfangguSigh : ModCardTemplate
{
	private int _bonusForCurrentPlay;

	private sealed class PendingBonusVar() : PowerVar<GoodFirePower>(BaseBonusPercent)
	{
		public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode,
			Creature? target, bool runGlobalHooks)
		{
			PreviewValue = BaseValue + (card is ShangfangguSigh sigh ? sigh.GetPreviewBonus() : 0);
		}
	}

	internal int GetPendingBonus() => IsMutable && Owner is { } owner
		? owner.Creature.GetPower<PendingGoodFirePower>()?.Amount ?? 0
		: 0;

	private int GetPreviewBonus() => Pile?.Type switch
	{
		PileType.Hand => GetPendingBonus(),
		PileType.Play => _bonusForCurrentPlay > 0 ? _bonusForCurrentPlay : GetPendingBonus(),
		_ => 0,
	};
	public const int BaseBonusPercent = 100;

	public override CardPoolModel VisualCardPool => ModelDb.CardPool<SunqianCardPool>();
	public const int UpgradedBonusPercent = 150;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PendingBonusVar(),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<BurningPower>(),
		..YjcmGoodFireInterop.HuoGongHoverTips(this),
		..YjcmGoodFireInterop.LibraryHoverTips(),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/ShangfangguSigh.png");

	public ShangfangguSigh()
		: base(1, CardType.Power, CardRarity.Token, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		SquSfx.Play(SquSfx.YilingFineFireEvent);
		_bonusForCurrentPlay = GetPendingBonus();
		if (Owner.Creature.GetPower<PendingGoodFirePower>() is { } pending)
		{
			await PowerCmd.Remove(pending);
		}
		await PowerCmd.Apply<GoodFirePower>(
			choiceContext,
			Owner.Creature,
			DynamicVars[nameof(GoodFirePower)].BaseValue + _bonusForCurrentPlay,
			Owner.Creature,
			this);
	}

	protected override void AddExtraArgsToDescription(LocString description)
	{
		YjcmGoodFireInterop.AddCombatCondition(description, this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars[nameof(GoodFirePower)].UpgradeValueBy(UpgradedBonusPercent - BaseBonusPercent);
	}
}
