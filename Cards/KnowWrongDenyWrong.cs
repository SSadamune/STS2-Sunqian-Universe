using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using Squ;
using Squ.Audio;
using Squ.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(ColorlessCardPool), StableEntryStem = "know_wrong_deny_wrong")]
public sealed class KnowWrongDenyWrong : ModCardTemplate
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new ReloadCountDamageVar(),
	];

	public override CardMultiplayerConstraint MultiplayerConstraint =>
		CardMultiplayerConstraint.SingleplayerOnly;

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		CardKeyword.Ethereal,
		CardKeyword.Innate,
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/KnowWrongDenyWrong.png");

	public KnowWrongDenyWrong()
		: base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
		SquSfx.Play(SquSfx.KnowWrongDenyWrongEvent);
		SyncDamageFromCache();

		await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.FromCard(this, cardPlay)
			.Targeting(cardPlay.Target)
			.WithHitFx("vfx/vfx_attack_slash")
			.Execute(choiceContext);
	}

	protected override void AddExtraArgsToDescription(LocString description)
	{
		if (!ShouldShowCurrentDamageLine())
		{
			description.Add("CurrentLine", string.Empty);
			return;
		}

		if (!IsInCombat)
		{
			SyncDamageFromCache();
		}

		SquKeywords.AddNestedLoc(
			description,
			"CurrentLine",
			new LocString("cards", Id.Entry + ".currentDamage"));
	}

	private bool ShouldShowCurrentDamageLine() =>
		IsMutable && RunState != null;

	private void SyncDamageFromCache()
	{
		DynamicVars.Damage.BaseValue = RunReloadCount.Current * (IsUpgraded ? 2 : 1);
	}

	private sealed class ReloadCountDamageVar : DamageVar
	{
		public ReloadCountDamageVar()
			: base(0m, ValueProp.Move)
		{
		}

		public override void UpdateCardPreview(
			CardModel card,
			CardPreviewMode previewMode,
			Creature? target,
			bool runGlobalHooks)
		{
			if (card.IsMutable)
			{
				BaseValue = RunReloadCount.Current * (card.IsUpgraded ? 2 : 1);
			}

			base.UpdateCardPreview(card, previewMode, target, runGlobalHooks && card.IsInCombat);
		}
	}
}
