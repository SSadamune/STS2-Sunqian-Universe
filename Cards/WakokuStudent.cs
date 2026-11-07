using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Character;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Cards;

[RegisterCard(typeof(LiubeiCardPool), StableEntryStem = "wakoku_student")]
public sealed class WakokuStudent : ModCardTemplate
{
	protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("HpThreshold", 9)];
	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
		[HoverTipFactory.FromPower<DexterityPower>(), HoverTipFactory.FromPower<StrengthPower>()];
	public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/prisoner/WakokuStudent.png");
	public WakokuStudent() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }
	protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
	{
		var power = await PowerCmd.Apply<WakokuStudentPower>(context, Owner.Creature, 1, Owner.Creature, this);
		if (power is not null) power.Configure(DynamicVars["HpThreshold"].IntValue);
	}
	protected override void OnUpgrade() => DynamicVars["HpThreshold"].UpgradeValueBy(-2);
}
