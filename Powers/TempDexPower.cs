using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Combat.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

[RegisterPower]
public sealed class TempDexPower : ModTemporaryPowerTemplate
{
	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/TempDexPower.png",
		BigIconPath: "res://images/powers/TempDexPowerBig.png");

	public override AbstractModel OriginModel => ModelDb.Power<HujinHuyuanPower>();

	public override PowerModel InternallyAppliedPower => ModelDb.Power<DexterityPower>();

	public override LocString Title => new("powers", "SUNQIAN_UNIVERSE_POWER_TEMP_DEX_POWER.title");

	public override LocString Description => new("powers", "SUNQIAN_UNIVERSE_POWER_TEMP_DEX_POWER.description");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<DexterityPower>(),
	];

	public static Task ApplyAsync(
		PlayerChoiceContext choiceContext,
		Creature target,
		Creature applier,
		CardModel? card,
		int amount)
	{
		if (amount <= 0)
		{
			return Task.CompletedTask;
		}

		return PowerCmd.Apply<TempDexPower>(
			choiceContext,
			target,
			amount,
			applier,
			card);
	}
}
