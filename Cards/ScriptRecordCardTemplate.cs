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
using Squ.Character;
using Squ.Combat;
using Squ.Interop;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>剧本集中展示并保存战斗内剧本状态的衍生牌。</summary>
public abstract class ScriptRecordCardTemplate : ModCardTemplate
{
	[SavedProperty]
	public int StoredAmount { get; set; }

	[SavedProperty]
	public bool SourceUpgraded { get; set; }

	public abstract Type PowerType { get; }

	public virtual bool CanBeReinforced => false;

	internal virtual bool HasUpgradedVersion => false;

	public sealed override string Title => HasUpgradedVersion
		? base.Title
		: TitleLocString.GetFormattedText();

	public sealed override CardPoolModel VisualCardPool => ModelDb.CardPool<SunqianCardPool>();

	public override IEnumerable<CardKeyword> CanonicalKeywords => [];

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
		if (!CanBeReinforced)
		{
			return;
		}

		StoredAmount += Math.Max(1, (int)amount);
		SourceUpgraded |= source.IsUpgraded;
		ApplyStoredUpgrade();
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

[RegisterCard(typeof(TokenCardPool), StableEntryStem = "script_record_security_guard")]
public sealed class SecurityGuardScriptRecord
	: ScriptRecordCardTemplate<ScriptSecurityGuardPower, SecurityGuardScript>
{
	public override CardAssetProfile AssetProfile => new();
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
public sealed class TwoWordPoetScriptRecord : ReinforceableScriptRecordCardTemplate<ScriptTwoWordPoetPower, TwoWordPoetScript>
{
	internal override bool HasUpgradedVersion => true;

	public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/TwoWordPoetScript.png");
}

[RegisterCard(typeof(TokenCardPool), StableEntryStem = "script_record_sunqian")]
public sealed class SunqianScriptRecord : ScriptRecordCardTemplate<ScriptSunqianPower, SunqianScript>
{
	internal override bool HasUpgradedVersion => true;

	protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Dexterity", SunqianScript.BaseDexterity)];

	public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/SunqianScript.png");

	protected override void SyncDisplayVars() =>
		DynamicVars["Dexterity"].BaseValue = SourceUpgraded
			? SunqianScript.UpgradedDexterity
			: SunqianScript.BaseDexterity;

	protected override void OnUpgrade() =>
		DynamicVars["Dexterity"].UpgradeValueBy(
			SunqianScript.UpgradedDexterity - SunqianScript.BaseDexterity);
}

[RegisterCard(typeof(TokenCardPool), StableEntryStem = "script_record_burning_linked_camps")]
public sealed class BurningLinkedCampsScriptRecord : ScriptRecordCardTemplate<ScriptBurningLinkedCampsPower, BurningLinkedCampsScript>
{
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
public sealed class ExecutionCommanderScriptRecord : ReinforceableScriptRecordCardTemplate<ScriptExecutionCommanderPower, ExecutionCommanderScript>
{
	internal override bool HasUpgradedVersion => true;

	public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/ExecutionCommanderScript.png");

	protected override void OnUpgrade() =>
		DynamicVars["ScriptAmount"].UpgradeValueBy(
			ExecutionCommanderScript.UpgradedBonusPercent
			- ExecutionCommanderScript.BaseBonusPercent);
}

[RegisterCard(typeof(TokenCardPool), StableEntryStem = "script_record_headsman")]
public sealed class HeadsmanScriptRecord : ScriptRecordCardTemplate<ScriptHeadsmanPower, HeadsmanScript>
{
	internal override bool HasUpgradedVersion => true;

	public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/HeadsmanScript.png");

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<VigorPower>(HeadsmanScript.BaseVigor),
	];

	protected override void SyncDisplayVars() =>
		DynamicVars[nameof(VigorPower)].BaseValue = SourceUpgraded
			? HeadsmanScript.UpgradedVigor
			: HeadsmanScript.BaseVigor;

	protected override void OnUpgrade() =>
		DynamicVars[nameof(VigorPower)].UpgradeValueBy(
			HeadsmanScript.UpgradedVigor - HeadsmanScript.BaseVigor);

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
	internal override bool HasUpgradedVersion => true;

	public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/XiliangSavageScript.png");

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<VigorPower>(XiliangSavageScript.BaseVigor),
	];

	protected override void SyncDisplayVars() =>
		DynamicVars[nameof(VigorPower)].BaseValue = SourceUpgraded
			? XiliangSavageScript.UpgradedVigor
			: XiliangSavageScript.BaseVigor;

	protected override void OnUpgrade() =>
		DynamicVars[nameof(VigorPower)].UpgradeValueBy(
			XiliangSavageScript.UpgradedVigor - XiliangSavageScript.BaseVigor);

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
public sealed class NightRaidWuchaoScriptRecord : ReinforceableScriptRecordCardTemplate<ScriptNightRaidWuchaoPower, NightRaidWuchaoScript>
{
	internal override bool HasUpgradedVersion => true;

	public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/NightRaidWuchaoScript.png");

	protected override void OnUpgrade() =>
		DynamicVars["ScriptAmount"].UpgradeValueBy(
			NightRaidWuchaoScript.UpgradedBurning - NightRaidWuchaoScript.BaseBurning);
}

[RegisterCard(typeof(TokenCardPool), StableEntryStem = "script_record_bowang_paradox")]
public sealed class BowangParadoxScriptRecord : ScriptRecordCardTemplate<ScriptBowangParadoxPower, BowangParadoxScript>
{
	internal override bool HasUpgradedVersion => true;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<VigorPower>(BowangParadoxScript.BaseVigor),
		new PowerVar<TinderPower>(BowangParadoxScript.BaseTinderStacks),
	];

	public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/BowangParadoxScript.png");

	protected override void SyncDisplayVars()
	{
		DynamicVars[nameof(VigorPower)].BaseValue = SourceUpgraded
			? BowangParadoxScript.UpgradedVigor
			: BowangParadoxScript.BaseVigor;
		DynamicVars[nameof(TinderPower)].BaseValue = SourceUpgraded
			? BowangParadoxScript.UpgradedTinderStacks
			: BowangParadoxScript.BaseTinderStacks;
	}

	protected override void OnUpgrade()
	{
		DynamicVars[nameof(VigorPower)].UpgradeValueBy(
			BowangParadoxScript.UpgradedVigor - BowangParadoxScript.BaseVigor);
		DynamicVars[nameof(TinderPower)].UpgradeValueBy(
			BowangParadoxScript.UpgradedTinderStacks - BowangParadoxScript.BaseTinderStacks);
	}

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<VigorPower>(),
		..HoverTipFactory.FromPowerWithPowerHoverTips<TinderPower>(),
		HoverTipFactory.FromPower<BurningPower>(),
		HoverTipFactory.FromKeyword(CardKeyword.Retain),
	];
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
