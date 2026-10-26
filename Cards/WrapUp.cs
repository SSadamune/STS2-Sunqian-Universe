using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ;
using Squ.Audio;
using Squ.Character;
using Squ.Script;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "wrap_up")]
[RegisterCharacterStarterCard(typeof(SunqianCharacter), 1)]
public sealed class WrapUp : ModCardTemplate
{
	public const int BaseVigor = 3;
	public const int UpgradedVigor = 4;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<VigorPower>(BaseVigor),
	];

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		SquKeywords.Wrap,
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<VigorPower>(),
		HoverTipFactory.FromKeyword(SquKeywords.Script),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/WrapUp.png");

	protected override bool ShouldGlowGoldInternal =>
		SquKeywords.ShouldGlowForWrap(this);

	public WrapUp()
		: base(0, CardType.Skill, CardRarity.Basic, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		SquSfx.PlayRandom(RunState, SquSfx.WrapUpEvents);
		bool wrap = await ScriptSystem.TryConsumeWrapAsync(Owner.Creature);
		await ApplyVigor(choiceContext, DynamicVars[nameof(VigorPower)].BaseValue);
		if (wrap)
		{
			await PlayerCmd.GainEnergy(1m, Owner);
		}
	}

	protected override void OnUpgrade()
	{
		DynamicVars[nameof(VigorPower)].UpgradeValueBy(UpgradedVigor - BaseVigor);
	}

	private Task ApplyVigor(PlayerChoiceContext choiceContext, decimal amount) =>
		PowerCmd.Apply<VigorPower>(
			choiceContext,
			Owner.Creature,
			amount,
			Owner.Creature,
			this);
}
