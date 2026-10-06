using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>关帝形态的文关羽选项。只出现在二选一界面，不会进入牌组。</summary>
[RegisterCard(typeof(TokenCardPool), StableEntryStem = "guan_yu_civil_ver")]
public sealed class GuanYuCivilVer : ModCardTemplate
{
	public const int BlockPerEnergy = 2;

	private const string UpgradeHighlightVarName = "UpgradeHighlight";

	private sealed class DexterityOnlyBlockVar(decimal baseValue)
		: BlockVar(baseValue, ValueProp.Unpowered)
	{
		public override void UpdateCardPreview(
			CardModel card,
			CardPreviewMode previewMode,
			MegaCrit.Sts2.Core.Entities.Creatures.Creature? target,
			bool runGlobalHooks)
		{
			int dexterity =
				card.Owner?.Creature.GetPower<DexterityPower>()?.Amount ?? 0;
			PreviewValue = System.Math.Max(0, BaseValue + dexterity);
		}
	}

	public override bool CanBeGeneratedInCombat => false;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DexterityOnlyBlockVar(BlockPerEnergy),
		new DynamicVar(UpgradeHighlightVarName, 0),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromCard<SunqianScript>(),
		HoverTipFactory.FromPower<DexterityPower>(),
		HoverTipFactory.Static(StaticHoverTip.Block),
		CreateEnergyTimingHoverTip(),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/GuanDiForm.png");

	public GuanYuCivilVer()
		: base(-1, CardType.Power, CardRarity.Token, TargetType.Self)
	{
	}

	public void SetBlockPerEnergy(int blockPerEnergy) =>
		DynamicVars.Block.BaseValue = blockPerEnergy;

	private IHoverTip CreateEnergyTimingHoverTip()
	{
		LocString description = new("cards", Id.Entry + ".energyTimingDescription");
		description.Add("energyPrefix", EnergyIconHelper.GetPrefix(this));
		return new HoverTip(SquCommonL10n.AnnotationTitle(), description);
	}

	protected override void OnUpgrade() =>
		DynamicVars[UpgradeHighlightVarName].UpgradeValueBy(0);

	protected override void AddExtraArgsToDescription(LocString description)
	{
		if (!IsUpgraded)
		{
			description.Add("FreeUntilPlayedText", "");
			return;
		}

		string suffix = DynamicVars[UpgradeHighlightVarName].WasJustUpgraded
			? ".freeUntilPlayedPreview"
			: ".freeUntilPlayed";
		description.Add("FreeUntilPlayedText", new LocString("cards", Id.Entry + suffix));
	}

	protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
		Task.CompletedTask;
}
