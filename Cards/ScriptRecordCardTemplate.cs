using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using Squ.Character;
using Squ.Powers;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>剧本集展示并保存战斗内剧本能力的衍生牌。</summary>
public abstract class ScriptRecordCardTemplate : ModCardTemplate
{
	[SavedProperty]
	public int StoredAmount { get; set; }

	[SavedProperty]
	public bool SourceUpgraded { get; set; }

	public abstract Type PowerType { get; }

	public abstract string SourceCardTitle { get; }

	/// <summary>Material reuse permits repeated level upgrades when the source is played again.</summary>
	public virtual bool UsesMaterialReuse => false;
	protected virtual IEnumerable<IHoverTip> DirectOnlyHoverTips => [];
	internal IEnumerable<IHoverTip> NestedHoverTips => HoverTips.ExceptBy(
		DirectOnlyHoverTips.Select(tip => tip.Id), tip => tip.Id);
	protected override IEnumerable<IHoverTip> AdditionalHoverTips => UsesMaterialReuse
		? [..DirectOnlyHoverTips, SquKeywords.CreateMaterialReuseHoverTip(SourceCardTitle),
			HoverTipFactory.FromKeyword(SquKeywords.ScriptCollection)] : DirectOnlyHoverTips;
	public override int MaxUpgradeLevel => CanUpgradeScript ? (UsesMaterialReuse ? int.MaxValue : 1) : 0;

	public virtual bool CountsTowardActivationLimit => true;

	public virtual string? MutuallyExclusiveGroup => null;

	/// <summary>The record itself has an upgraded effect; source-card upgrades alone
	/// do not imply this (they may only change the source's immediate play effect).</summary>
	public virtual bool CanUpgradeScript => false;

	public sealed override string Title => CanUpgradeScript
		? base.Title
		: TitleLocString.GetFormattedText();

	public sealed override CardPoolModel VisualCardPool => ModelDb.CardPool<SunqianCardPool>();

	public override IEnumerable<CardKeyword> CanonicalKeywords => UsesMaterialReuse ? [SquKeywords.MaterialReuse] : [];

	protected override bool ShouldGlowGoldInternal =>
		Owner?.Creature.Powers
			.OfType<ScriptPowerTemplate>()
			.Any(power => power.GetType() == PowerType) == true;

	protected ScriptRecordCardTemplate()
		: base(0, CardType.Skill, CardRarity.Token, TargetType.Self, showInCardLibrary: false)
	{
	}

	internal void InitializeFrom(ScriptCardTemplate source, decimal amount)
	{
		StoredAmount = Math.Max(1, (int)amount);
		SourceUpgraded = source.IsUpgraded;
		ApplyStoredUpgrade();
		SyncDisplayVars();
		OnInitializedFrom(source);
	}

	internal void UpgradeFrom(ScriptCardTemplate source)
	{
		if (!CanUpgradeScript) return;
		if (UsesMaterialReuse)
		{
			for (int i = 0; i < (source.IsUpgraded ? 2 : 1) && IsUpgradable; i++)
			{
				UpgradeInternal();
				FinalizeUpgradeInternal();
			}
			SourceUpgraded |= source.IsUpgraded;
		}
		else
		{
			MergeSourceUpgrade(source);
		}
		SyncDisplayVars();
	}

	internal void PreviewFrom(ScriptCardTemplate source)
	{
		StoredAmount = Math.Max(1, source.GetScriptPowerAmountForPreview());
		SourceUpgraded = source.IsUpgraded;
		UpgradePreviewType = source.UpgradePreviewType;
		ApplyStoredUpgrade();
		SyncDisplayVars();
	}

	internal abstract Task ActivateAsync(PlayerChoiceContext choiceContext, Player player);

	protected virtual void OnInitializedFrom(ScriptCardTemplate source)
	{
	}

	protected virtual void SyncDisplayVars()
	{
	}

	protected void MergeSourceUpgrade(ScriptCardTemplate source)
	{
		SourceUpgraded |= source.IsUpgraded;
		ApplyStoredUpgrade();
	}

	protected override void AfterDeserialized()
	{
		base.AfterDeserialized();
		SyncDisplayVars();
	}

	private void ApplyStoredUpgrade()
	{
		if (!CanUpgradeScript || !SourceUpgraded || IsUpgraded || !IsUpgradable)
		{
			return;
		}

		UpgradeInternal();
		if (UpgradePreviewType == CardUpgradePreviewType.None)
		{
			FinalizeUpgradeInternal();
		}
	}

	protected sealed override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
		Task.CompletedTask;

	protected override void OnUpgrade()
	{
	}
}

public abstract class ScriptRecordCardTemplate<TPower, TSource> : ScriptRecordCardTemplate
	where TPower : ScriptPowerTemplate
	where TSource : ScriptCardTemplate
{
	private static readonly PileType[] SourceSearchPiles =
	[
		PileType.Play, PileType.Exhaust, PileType.Hand, PileType.Discard, PileType.Draw,
	];

	public sealed override Type PowerType => typeof(TPower);
	public sealed override string SourceCardTitle => ModelDb.Card<TSource>().Title;

	internal sealed override async Task ActivateAsync(PlayerChoiceContext choiceContext, Player player)
	{
		var power = (TPower)ModelDb.Power<TPower>().ToMutable();
		TSource source = ResolveSourceCard(player);
		ConfigurePower(power, source, player);
		power.SetActivationChoiceContext(choiceContext);
		await PowerCmd.Apply(
			choiceContext,
			power,
			player.Creature,
			StoredAmount,
			player.Creature,
			source);
	}

	protected virtual void ConfigurePower(TPower power, TSource source, Player player)
	{
	}

	private TSource ResolveSourceCard(Player player)
	{
		foreach (PileType pileType in SourceSearchPiles)
		{
			TSource? existing = pileType.GetPile(player).Cards
				.OfType<TSource>()
				.FirstOrDefault(card => card.IsUpgraded == SourceUpgraded);
			if (existing is not null)
			{
				return existing;
			}
		}

		ICombatState combatState = player.Creature.CombatState
			?? throw new InvalidOperationException("A Script can only be activated during combat.");
		var created = combatState.CreateCard<TSource>(player);
		if (SourceUpgraded)
		{
			created.UpgradeInternal();
			created.FinalizeUpgradeInternal();
		}

		return created;
	}
}

public abstract class MaterialReuseScriptRecordCardTemplate<TPower, TSource>
	: ScriptRecordCardTemplate<TPower, TSource>
	where TPower : ScriptPowerTemplate
	where TSource : ScriptCardTemplate
{
	public sealed override bool UsesMaterialReuse => true;
	public sealed override bool CanUpgradeScript => true;
	protected abstract int InitialAmount { get; }
	protected abstract int AmountPerUpgrade { get; }
	protected virtual string AmountVarName => "ScriptAmount";
	protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar(AmountVarName, InitialAmount)];
	protected override void SyncDisplayVars()
	{
		StoredAmount = InitialAmount + CurrentUpgradeLevel * AmountPerUpgrade;
		DynamicVars[AmountVarName].BaseValue = StoredAmount;
	}
	protected override void OnUpgrade()
	{
		DynamicVars[AmountVarName].UpgradeValueBy(AmountPerUpgrade);
		// Save restoration applies upgrades after AfterDeserialized.
		StoredAmount = InitialAmount + CurrentUpgradeLevel * AmountPerUpgrade;
	}
}
