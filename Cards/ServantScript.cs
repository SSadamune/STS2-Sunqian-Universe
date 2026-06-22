using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Character;
using Squ.Powers;
using Squ.Script;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "servant_script")]
public sealed class ServantScript : ScriptCardTemplate
{
	public const string GeneratedCardVarName = "GeneratedCard";

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new BlockVar(6m, ValueProp.Move),
		new StringVar(GeneratedCardVarName, GeneratedCombatCards.GetDisplayTitle<ThrowHimOut>(upgraded: false)),
	];

	// 对齐 BEGONE：悬停预览实际生成的卡牌（含升级态）。
	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromCard<ThrowHimOut>(IsUpgraded),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/ServantScript.png");

	public ServantScript()
		: base(2, CardType.Skill, CardRarity.Common, TargetType.Self, false)
	{
	}

	protected override void AddExtraArgsToDescription(LocString description)
	{
		bool showUpgraded = IsUpgraded || DynamicVars.Block.WasJustUpgraded;
		((StringVar)DynamicVars[GeneratedCardVarName]).StringValue =
			$"[gold]{GeneratedCombatCards.GetDisplayTitle<ThrowHimOut>(showUpgraded)}[/gold]";
	}

	protected override async Task PlayScriptAsync(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

		await PowerCmd.Apply<ScriptServantPower>(
			choiceContext,
			Owner.Creature,
			1m,
			Owner.Creature,
			this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Block.UpgradeValueBy(3m);
	}
}
