using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Combat;
using Squ.Interop;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(TokenCardPool), StableEntryStem = "script_record_grunt")]
public sealed class GruntScriptRecord
	: ScriptRecordCardTemplate<ScriptGruntPower, GruntScript>
{
	public override bool CanUpgradeScript => true;

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
	public override bool CanUpgradeScript => true;

	public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/ServantScript.png");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		SquCardHoverTipFactory.FromCard<ThrowHimOut>(this, SourceUpgraded),
	];
}

[RegisterCard(typeof(TokenCardPool), StableEntryStem = "script_record_two_word_poet")]
public sealed class TwoWordPoetScriptRecord : ScriptRecordCardTemplate<ScriptTwoWordPoetPower, TwoWordPoetScript>
{
	public override bool CanUpgradeScript => true;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(
			ScriptTwoWordPoetPower.ScriptDamageVarName,
			TwoWordPoetScript.CanonicalDamage,
			ValueProp.Move),
		new RepeatVar(TwoWordPoetScript.CanonicalHits),
	];

	public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/TwoWordPoetScript.png");

	internal void AddConsumedVigor(int amount)
	{
		StoredAmount += Math.Max(0, amount);
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
	public override bool CanUpgradeScript => true;

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
	public override bool CanUpgradeScript => true;

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
		..base.AdditionalHoverTips,
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
		..base.AdditionalHoverTips,
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

	public override bool CanUpgradeScript => true;

	protected override IEnumerable<DynamicVar> CanonicalVars => [new StringVar("RecordedCards", string.Empty)];

	public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/PartTimeGeneralScript.png");

	internal void RecordPlayedCards(IEnumerable<CardModel> cards)
	{
		RecordedCards.AddRange(cards.Select(card => card.ToSerializable()));
		SyncDisplayVars();
	}

	protected override void ConfigurePower(ScriptPartTimeGeneralPower power, PartTimeGeneralScript source, Player player) =>
		power.AttachRecord(this);

	protected override void SyncDisplayVars()
	{
		string names = string.Join("、", RecordedCards.Select(card => CardModel.FromSerializable(card).Title));
		((StringVar)DynamicVars["RecordedCards"]).StringValue = string.IsNullOrEmpty(names) ? "—" : names;
	}

	protected override void AddExtraArgsToDescription(LocString description)
	{
		base.AddExtraArgsToDescription(description);
		bool hasRecords = IsMutable && CombatManager.Instance.IsInProgress && RecordedCards.Count > 0;
		LocString text = new("cards", Id.Entry + (hasRecords ? ".smartDescription" : ".emptyDescription"));
		text.Add("RecordedCards", DynamicVars["RecordedCards"].ToString());
		description.Add("RecordDescription", text.GetFormattedText());
	}

	protected override void DeepCloneFields()
	{
		base.DeepCloneFields();
		RecordedCards = [..RecordedCards];
	}
}
