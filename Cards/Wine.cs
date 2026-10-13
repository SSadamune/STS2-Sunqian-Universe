using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Audio;
using Squ.Character;
using Squ.Interop;
using Squ.Script;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "wine")]
public sealed class Wine : ModCardTemplate
{
	public const int BaseVigor = 3;
	public const int BaseEnergy = 1;
	public const int UpgradedEnergy = 2;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new EnergyVar(BaseEnergy),
		new PowerVar<VigorPower>(BaseVigor),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.ForEnergy(this),
		..NewsanguoVigorSwap.GrantedPowerHoverTips(this),
		HoverTipFactory.FromCard<Dazed>(),
		..NewsanguoVigorSwap.LibraryHoverTips(),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/Wine.png");

	public Wine()
		: base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		SquSfx.PlayRandom(RunState, SquSfx.WineEvents);

		ICombatState combatState = CombatState
			?? throw new InvalidOperationException("Wine requires an active combat.");

		await PlayerCmd.GainEnergy((int)DynamicVars.Energy.BaseValue, Owner);
		await NewsanguoVigorSwap.ApplyVigorOrDrunkenMight(
			choiceContext,
			Owner.Creature,
			DynamicVars[nameof(VigorPower)].BaseValue,
			Owner.Creature,
			this);

		await GeneratedCombatCards.AddToDrawPileInCombat<Dazed>(
			combatState,
			Owner,
			1,
			upgraded: false,
			Owner);
	}

	protected override void AddExtraArgsToDescription(LocString description)
	{
		NewsanguoVigorSwap.AddCombatNote(description, this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Energy.UpgradeValueBy(UpgradedEnergy - BaseEnergy);
	}
}
