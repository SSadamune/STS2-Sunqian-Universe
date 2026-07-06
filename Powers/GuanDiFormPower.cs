using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Content.Patches;

#nullable enable

namespace Squ.Powers;

[RegisterPower]
public sealed class GuanDiFormPower : ModPowerTemplate
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/GuanDiFormPower.png",
		BigIconPath: "res://images/powers/GuanDiFormPowerBig.png");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<DexterityPower>(),
		HoverTipFactory.FromPower<StrengthPower>(),
	];

	public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (Owner.IsDead || cardPlay.Card.Owner != Owner.Player)
		{
			return;
		}

		switch (cardPlay.Card.Type)
		{
			case CardType.Skill:
				Flash();
				await PowerCmd.Apply<TempDexFromGuanDiFormPower>(
					choiceContext, Owner, Amount, Owner, null);
				break;

			case CardType.Attack:
				Flash();
				await PowerCmd.Apply<TempStrFromGuanDiFormPower>(
					choiceContext, Owner, Amount, Owner, null);
				break;
		}
	}
}

[RegisterPower]
public sealed class TempDexFromGuanDiFormPower : TempDexPower<GuanDiFormPower> { }

[RegisterPower]
public sealed class TempStrFromGuanDiFormPower : TemporaryStrengthPower, IModPowerAssetOverrides
{
	private static readonly PowerModel SetupStrikePowerTemplate = ModelDb.Power<SetupStrikePower>();

	public override AbstractModel OriginModel => ModelDb.Card<GuanDiForm>();

	public PowerAssetProfile AssetProfile => new(
		IconPath: SetupStrikePowerTemplate.PackedIconPath,
		BigIconPath: SetupStrikePowerTemplate.ResolvedBigIconPath);

	public string? CustomIconPath => AssetProfile.IconPath;

	public string? CustomBigIconPath => AssetProfile.BigIconPath;
}
