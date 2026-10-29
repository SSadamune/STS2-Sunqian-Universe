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
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Character;
using Squ.Combat;
using Squ.Interop;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>剧本集中展示并保存战斗内剧本能力的衍生牌。</summary>
public abstract class ScriptRecordCardTemplate : ModCardTemplate
{
	[SavedProperty]
	public int StoredAmount { get; set; }

	[SavedProperty]
	public bool SourceUpgraded { get; set; }

	public abstract Type PowerType { get; }

	public virtual bool CanBeReinforced => false;
	public virtual bool UsesMaterialReuse => false;
	public override int MaxUpgradeLevel => UsesMaterialReuse ? int.MaxValue : base.MaxUpgradeLevel;

	public virtual bool CountsTowardActivationLimit => true;

	public virtual string? MutuallyExclusiveGroup => null;

	internal virtual bool HasUpgradedVersion => false;

	public sealed override string Title => HasUpgradedVersion
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

	internal void ReinforceFrom(ScriptCardTemplate source, decimal amount)
	{
		if (UsesMaterialReuse)
		{
			for (int i = 0; i < (source.IsUpgraded ? 2 : 1); i++)
			{
				UpgradeInternal();
				FinalizeUpgradeInternal();
			}
			SourceUpgraded |= source.IsUpgraded;
			SyncDisplayVars();
			return;
		}
		if (!CanBeReinforced)
		{
			return;
		}

		StoredAmount += Math.Max(1, (int)amount);
		MergeSourceUpgrade(source);
		SyncDisplayVars();
		OnReinforcedFrom(source);
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

	protected virtual void OnReinforcedFrom(ScriptCardTemplate source)
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
		if (!HasUpgradedVersion || !SourceUpgraded || IsUpgraded || !IsUpgradable)
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
	public sealed override Type PowerType => typeof(TPower);

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
		foreach (PileType pileType in new[]
			{
				PileType.Play,
				PileType.Exhaust,
				PileType.Hand,
				PileType.Discard,
				PileType.Draw,
			})
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

public abstract class ReinforceableScriptRecordCardTemplate<TPower, TSource>
	: ScriptRecordCardTemplate<TPower, TSource>
	where TPower : ScriptPowerTemplate
	where TSource : ScriptCardTemplate
{
	public sealed override bool CanBeReinforced => true;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("ScriptAmount", 1),
	];

	protected override void SyncDisplayVars() =>
		DynamicVars["ScriptAmount"].BaseValue = StoredAmount;
}

public abstract class MaterialReuseScriptRecordCardTemplate<TPower, TSource>
	: ReinforceableScriptRecordCardTemplate<TPower, TSource>
	where TPower : ScriptPowerTemplate
	where TSource : ScriptCardTemplate
{
	public sealed override bool UsesMaterialReuse => true;
	internal sealed override bool HasUpgradedVersion => true;
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

[RegisterCard(typeof(TokenCardPool), StableEntryStem = "script_record_grunt")]
public sealed class GruntScriptRecord
	: ScriptRecordCardTemplate<ScriptGruntPower, GruntScript>
{
	internal override bool HasUpgradedVersion => true;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("BonusDamage", ScriptGruntPower.BaseBonusDamage),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/GruntScript.png");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromKeyword(SquKeywords.VariousStrikeCards),
	];

	protected override void SyncDisplayVars() =>
		DynamicVars["BonusDamage"].BaseValue = SourceUpgraded
			? ScriptGruntPower.UpgradedBonusDamage
			: ScriptGruntPower.BaseBonusDamage;

	protected override void OnUpgrade() =>
		DynamicVars["BonusDamage"].UpgradeValueBy(
			ScriptGruntPower.UpgradedBonusDamage
			- ScriptGruntPower.BaseBonusDamage);
}

[RegisterCard(typeof(TokenCardPool), StableEntryStem = "script_record_neutral_ambush")]
public sealed class NeutralAmbushScriptRecord : ScriptRecordCardTemplate<ScriptNeutralAmbushPower, NeutralAmbushScript>
{
	public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/NeutralAmbushScript.png");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		..SquCardHoverTipFactory.FromCardWithCardHoverTips<GiantRock>(this, SourceUpgraded),
		..SquCardHoverTipFactory.FromCardWithCardHoverTips<SalvoStrike>(this, SourceUpgraded),
	];
}

[RegisterCard(typeof(TokenCardPool), StableEntryStem = "script_record_servant")]
public sealed class ServantScriptRecord : ScriptRecordCardTemplate<ScriptServantPower, ServantScript>
{
	internal override bool HasUpgradedVersion => true;

	public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/ServantScript.png");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		SquCardHoverTipFactory.FromCard<ThrowHimOut>(this, SourceUpgraded),
	];
}

[RegisterCard(typeof(TokenCardPool), StableEntryStem = "script_record_two_word_poet")]
public sealed class TwoWordPoetScriptRecord : ScriptRecordCardTemplate<ScriptTwoWordPoetPower, TwoWordPoetScript>
{
	internal override bool HasUpgradedVersion => true;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(
			ScriptTwoWordPoetPower.ScriptDamageVarName,
			TwoWordPoetScript.CanonicalDamage,
			ValueProp.Move),
		new RepeatVar(TwoWordPoetScript.CanonicalHits),
	];

	public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/TwoWordPoetScript.png");

	internal void AddConsumedVigor(TwoWordPoetScript source, int amount)
	{
		StoredAmount += Math.Max(0, amount);
		MergeSourceUpgrade(source);
		SyncDisplayVars();
	}

	protected override void SyncDisplayVars()
	{
		DynamicVars[ScriptTwoWordPoetPower.ScriptDamageVarName].BaseValue = StoredAmount;
		DynamicVars.Repeat.BaseValue = TwoWordPoetScript.CanonicalHits;
	}
}

[RegisterCard(typeof(TokenCardPool), StableEntryStem = "script_record_sunqian")]
public sealed class SunqianScriptRecord : ScriptRecordCardTemplate<ScriptSunqianPower, SunqianScript>
{
	internal override bool HasUpgradedVersion => true;
	internal void UpdateFrom(SunqianScript source)
	{
		MergeSourceUpgrade(source);
		SyncDisplayVars();
	}

	protected override IEnumerable<DynamicVar> CanonicalVars =>
		[new DynamicVar("Dexterity", SunqianScript.BaseDexterity), new CardsVar(SunqianScript.BaseDraw)];
	public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/SunqianScript.png");
	protected override void SyncDisplayVars() => DynamicVars.Cards.BaseValue =
		SourceUpgraded ? SunqianScript.UpgradedDraw : SunqianScript.BaseDraw;
	protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1);

}

[RegisterCard(typeof(TokenCardPool), StableEntryStem = "script_record_burning_linked_camps")]
public sealed class BurningLinkedCampsScriptRecord : ScriptRecordCardTemplate<ScriptBurningLinkedCampsPower, BurningLinkedCampsScript>
{
	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
		[HoverTipFactory.FromPower<BurningPower>()];
	public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/BurningLinkedCampsScript.png");
}

[RegisterCard(typeof(TokenCardPool), StableEntryStem = "script_record_one_man_one_city")]
public sealed class OneManOneCityScriptRecord : ScriptRecordCardTemplate<ScriptOneManOneCityPower, OneManOneCity>
{
	public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/OneManOneCity.png");
}

[RegisterCard(typeof(TokenCardPool), StableEntryStem = "script_record_chicken_foot_cheese")]
public sealed class ChickenFootCheeseScriptRecord : ScriptRecordCardTemplate<ScriptChickenFootCheesePower, ChickenFootCheeseScript>
{
	internal override bool HasUpgradedVersion => true;

	protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("FollowUpDamage", ScriptChickenFootCheesePower.BaseFollowUpDamage)];

	public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/ChickenFootCheeseScript.png");

	protected override void SyncDisplayVars() =>
		DynamicVars["FollowUpDamage"].BaseValue = SourceUpgraded
			? ScriptChickenFootCheesePower.UpgradedFollowUpDamage
			: ScriptChickenFootCheesePower.BaseFollowUpDamage;

	protected override void OnUpgrade() =>
		DynamicVars["FollowUpDamage"].UpgradeValueBy(
			ScriptChickenFootCheesePower.UpgradedFollowUpDamage
			- ScriptChickenFootCheesePower.BaseFollowUpDamage);

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		..SquCardHoverTipFactory.FromCardWithCardHoverTips<ChickenFootCheeseScriptStrikePreview>(
			this,
			SourceUpgraded),
	];
}

[RegisterCard(typeof(TokenCardPool), StableEntryStem = "script_record_execution_commander")]
public sealed class ExecutionCommanderScriptRecord : MaterialReuseScriptRecordCardTemplate<ScriptExecutionCommanderPower, ExecutionCommanderScript>
{
	protected override int InitialAmount => ExecutionCommanderScript.BaseBonusPercent;
	protected override int AmountPerUpgrade => 25;
	public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/ExecutionCommanderScript.png");
}

[RegisterCard(typeof(TokenCardPool), StableEntryStem = "script_record_headsman")]
public sealed class HeadsmanScriptRecord : MaterialReuseScriptRecordCardTemplate<ScriptHeadsmanPower, HeadsmanScript>
{
	protected override int InitialAmount => HeadsmanScript.BaseVigor;
	protected override int AmountPerUpgrade => 1;
	protected override string AmountVarName => nameof(VigorPower);
	protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<VigorPower>(HeadsmanScript.BaseVigor)];
	public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/HeadsmanScript.png");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		..NewsanguoDrunkenMightEffect.HoverTips(),
	];

	protected override void AddExtraArgsToDescription(LocString description)
	{
		base.AddExtraArgsToDescription(description);
		NewsanguoDrunkenMightEffect.AddOrText(description);
	}
}

[RegisterCard(typeof(TokenCardPool), StableEntryStem = "script_record_xiliang_savage")]
public sealed class XiliangSavageScriptRecord : ScriptRecordCardTemplate<ScriptXiliangSavagePower, XiliangSavageScript>
{
	public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/XiliangSavageScript.png");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		..NewsanguoDrunkenMightEffect.HoverTips(),
	];

	protected override void AddExtraArgsToDescription(LocString description)
	{
		base.AddExtraArgsToDescription(description);
		NewsanguoDrunkenMightEffect.AddOrText(description);
	}
}

[RegisterCard(typeof(TokenCardPool), StableEntryStem = "script_record_bombard_chibi")]
public sealed class BombardChibiScriptRecord : ScriptRecordCardTemplate<ScriptBombardChibiPower, BombardChibiScript>
{
	public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/BombardChibiScript.png");
}

[RegisterCard(typeof(TokenCardPool), StableEntryStem = "script_record_night_raid_wuchao")]
public sealed class NightRaidWuchaoScriptRecord : MaterialReuseScriptRecordCardTemplate<ScriptNightRaidWuchaoPower, NightRaidWuchaoScript>
{
	protected override int InitialAmount => NightRaidWuchaoScript.BaseBurning;
	protected override int AmountPerUpgrade => 2;

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		..SquCardHoverTipFactory.FromCardWithCardHoverTips<NightRaidWuchaoScriptStarryNightStrikePreview>(
			this,
			SourceUpgraded),
		HoverTipFactory.FromKeyword(SquKeywords.VariousStrikeCards),
	];

	public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/NightRaidWuchaoScript.png");

}

[RegisterCard(typeof(TokenCardPool), StableEntryStem = "script_record_eunuch")]
public sealed class EunuchScriptRecord : ScriptRecordCardTemplate<ScriptEunuchPower, EunuchScript>
{
	public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/EunuchScript.png");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromKeyword(CardKeyword.Ethereal),
		HoverTipFactory.FromKeyword(CardKeyword.Retain),
	];
}

[RegisterCard(typeof(TokenCardPool), StableEntryStem = "script_record_part_time_general")]
public sealed class PartTimeGeneralScriptRecord : ScriptRecordCardTemplate<ScriptPartTimeGeneralPower, PartTimeGeneralScript>
{
	[SavedProperty]
	public List<SerializableCard> RecordedCards { get; set; } = [];

	public override bool CanBeReinforced => true;

	protected override IEnumerable<DynamicVar> CanonicalVars => [new StringVar("RecordedCards", string.Empty)];

	public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/PartTimeGeneralScript.png");

	internal void RecordPlayedCards(IEnumerable<CardModel> cards)
	{
		RecordedCards.AddRange(cards.Select(card => card.ToSerializable()));
		SyncDisplayVars();
	}

	protected override void ConfigurePower(ScriptPartTimeGeneralPower power, PartTimeGeneralScript source, Player player) =>
		power.AttachRecord(this);

	internal void RemoveRecordedCardAt(int index)
	{
		if ((uint)index >= (uint)RecordedCards.Count)
		{
			return;
		}

		RecordedCards.RemoveAt(index);
		SyncDisplayVars();
	}

	protected override void SyncDisplayVars()
	{
		string names = string.Join("、", RecordedCards.Select(card => CardModel.FromSerializable(card).Title));
		((StringVar)DynamicVars["RecordedCards"]).StringValue = string.IsNullOrEmpty(names) ? "—" : names;
	}

	protected override void AddExtraArgsToDescription(LocString description)
	{
		base.AddExtraArgsToDescription(description);
		string recordDetails = string.Empty;
		if (IsMutable
			&& CombatManager.Instance.IsInProgress
			&& RecordedCards.Count > 0)
		{
			LocString details = new("cards", Id.Entry + ".recordDetails");
			details.Add("RecordedCards", DynamicVars["RecordedCards"].ToString());
			recordDetails = details.GetFormattedText();
		}

		description.Add("RecordDetails", recordDetails);
	}

	protected override void DeepCloneFields()
	{
		base.DeepCloneFields();
		RecordedCards = [..RecordedCards];
	}
}
