using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

/// <summary>剧本：一人一城。激活时立即按正常规则触发一次覆甲。</summary>
[RegisterPower]
public sealed class ScriptOneManOneCityPower : ScriptPowerTemplate
{
	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/ScriptOneManOneCityPower.png",
		BigIconPath: "res://images/powers/ScriptOneManOneCityPowerBig.png");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<PlatingPower>(),
	];

	protected override async Task OnScriptApplied(Creature? applier, CardModel? cardSource)
	{
		PlatingPower? plating = Owner.GetPower<PlatingPower>();
		if (plating is null || plating.Amount <= 0)
		{
			return;
		}

		await PowerCmd.Decrement(plating);
		if (plating.Amount <= 0)
		{
			return;
		}

		Flash();
		await CreatureCmd.GainBlock(Owner, plating.Amount, ValueProp.Unpowered, null);
	}
}
