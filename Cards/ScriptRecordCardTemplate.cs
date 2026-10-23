using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using Squ.Character;
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

	public sealed override string Title => TitleLocString.GetFormattedText();

	public sealed override CardPoolModel VisualCardPool => ModelDb.CardPool<SunqianCardPool>();

	public override IEnumerable<CardKeyword> CanonicalKeywords => [];

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
		if (!SourceUpgraded || IsUpgraded || !IsUpgradable)
		{
			return;
		}

		UpgradeInternal();
		FinalizeUpgradeInternal();
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

[RegisterCard(typeof(TokenCardPool), StableEntryStem = "script_record_neutral_ambush")]
public sealed class NeutralAmbushScriptRecord : ScriptRecordCardTemplate<ScriptNeutralAmbushPower, NeutralAmbushScript>
{
	public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/NeutralAmbushScript.png");
}

[RegisterCard(typeof(TokenCardPool), StableEntryStem = "script_record_servant")]
public sealed class ServantScriptRecord : ScriptRecordCardTemplate<ScriptServantPower, ServantScript>
{
	public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/ServantScript.png");
}

[RegisterCard(typeof(TokenCardPool), StableEntryStem = "script_record_two_word_poet")]
public sealed class TwoWordPoetScriptRecord : ReinforceableScriptRecordCardTemplate<ScriptTwoWordPoetPower, TwoWordPoetScript>
{
	public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/TwoWordPoetScript.png");
}

[RegisterCard(typeof(TokenCardPool), StableEntryStem = "script_record_sunqian")]
public sealed class SunqianScriptRecord : ScriptRecordCardTemplate<ScriptSunqianPower, SunqianScript>
{
	protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Dexterity", SunqianScript.BaseDexterity)];

	public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/SunqianScript.png");

	protected override void SyncDisplayVars() =>
		DynamicVars["Dexterity"].BaseValue = SourceUpgraded
			? SunqianScript.UpgradedDexterity
			: SunqianScript.BaseDexterity;
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
	protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("FollowUpDamage", ScriptChickenFootCheesePower.BaseFollowUpDamage)];

	public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/ChickenFootCheeseScript.png");

	protected override void SyncDisplayVars() =>
		DynamicVars["FollowUpDamage"].BaseValue = SourceUpgraded
			? ScriptChickenFootCheesePower.UpgradedFollowUpDamage
			: ScriptChickenFootCheesePower.BaseFollowUpDamage;
}

[RegisterCard(typeof(TokenCardPool), StableEntryStem = "script_record_execution_commander")]
public sealed class ExecutionCommanderScriptRecord : ReinforceableScriptRecordCardTemplate<ScriptExecutionCommanderPower, ExecutionCommanderScript>
{
	public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/ExecutionCommanderScript.png");
}

[RegisterCard(typeof(TokenCardPool), StableEntryStem = "script_record_headsman")]
public sealed class HeadsmanScriptRecord : ScriptRecordCardTemplate<ScriptHeadsmanPower, HeadsmanScript>
{
	public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/HeadsmanScript.png");
}

[RegisterCard(typeof(TokenCardPool), StableEntryStem = "script_record_xiliang_savage")]
public sealed class XiliangSavageScriptRecord : ScriptRecordCardTemplate<ScriptXiliangSavagePower, XiliangSavageScript>
{
	public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/XiliangSavageScript.png");
}

[RegisterCard(typeof(TokenCardPool), StableEntryStem = "script_record_bombard_chibi")]
public sealed class BombardChibiScriptRecord : ScriptRecordCardTemplate<ScriptBombardChibiPower, BombardChibiScript>
{
	public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/BombardChibiScript.png");
}

[RegisterCard(typeof(TokenCardPool), StableEntryStem = "script_record_night_raid_wuchao")]
public sealed class NightRaidWuchaoScriptRecord : ReinforceableScriptRecordCardTemplate<ScriptNightRaidWuchaoPower, NightRaidWuchaoScript>
{
	public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/NightRaidWuchaoScript.png");
}

[RegisterCard(typeof(TokenCardPool), StableEntryStem = "script_record_bowang_paradox")]
public sealed class BowangParadoxScriptRecord : ScriptRecordCardTemplate<ScriptBowangParadoxPower, BowangParadoxScript>
{
	public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/BowangParadoxScript.png");
}

[RegisterCard(typeof(TokenCardPool), StableEntryStem = "script_record_eunuch")]
public sealed class EunuchScriptRecord : ScriptRecordCardTemplate<ScriptEunuchPower, EunuchScript>
{
	public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/EunuchScript.png");
}

[RegisterCard(typeof(TokenCardPool), StableEntryStem = "script_record_part_time_general")]
public sealed class PartTimeGeneralScriptRecord : ScriptRecordCardTemplate<ScriptPartTimeGeneralPower, PartTimeGeneralScript>
{
	[SavedProperty]
	public List<SerializableCard> RecordedCards { get; set; } = [];

	protected override IEnumerable<DynamicVar> CanonicalVars => [new StringVar("RecordedCards", string.Empty)];

	public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/PartTimeGeneralScript.png");

	internal void RecordPlayedCards(IEnumerable<CardModel> cards)
	{
		RecordedCards = cards.Select(card => card.ToSerializable()).ToList();
		SyncDisplayVars();
	}

	protected override void ConfigurePower(ScriptPartTimeGeneralPower power, PartTimeGeneralScript source, Player player) =>
		power.RecordPlayedCards(RecordedCards.Select(CardModel.FromSerializable));

	protected override void SyncDisplayVars()
	{
		string names = string.Join("、", RecordedCards.Select(card => CardModel.FromSerializable(card).Title));
		((StringVar)DynamicVars["RecordedCards"]).StringValue = string.IsNullOrEmpty(names) ? "—" : names;
	}

	protected override void DeepCloneFields()
	{
		base.DeepCloneFields();
		RecordedCards = [..RecordedCards];
	}
}
