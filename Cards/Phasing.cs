using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Character;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "phasing")]
public sealed class Phasing : ModCardTemplate
{
	public const string MinDexVarName = "MinDex";
	public const string MaxDexVarName = "MaxDex";

	public const int BaseMinDexterity = 1;
	public const int BaseMaxDexterity = 2;
	public const int UpgradedMaxDexterity = 4;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar(MinDexVarName, BaseMinDexterity),
		new DynamicVar(MaxDexVarName, BaseMaxDexterity),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<DexterityPower>(),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/Phasing.png");

	public Phasing()
		: base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		int minDexterity = (int)DynamicVars[MinDexVarName].BaseValue;
		int maxDexterity = (int)DynamicVars[MaxDexVarName].BaseValue;
		int dexterity = PhasingPower.RollTemporaryDexterity(Owner, minDexterity, maxDexterity);

		await PhasingPower.ApplyTemporaryDexterityAsync(
			choiceContext,
			Owner.Creature,
			Owner.Creature,
			this,
			dexterity);

		await PhasingPower.AddOrStackBoundsAsync(
			choiceContext,
			Owner.Creature,
			minDexterity,
			maxDexterity,
			Owner.Creature,
			this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars[MaxDexVarName].UpgradeValueBy(UpgradedMaxDexterity - BaseMaxDexterity);
	}
}
