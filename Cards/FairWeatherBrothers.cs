#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Squ.Character;
using Squ.Combat;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
namespace Squ.Cards;

[RegisterCard(typeof(LiubeiCardPool), StableEntryStem = "fair_weather_brothers")]
public sealed class FairWeatherBrothers : ModCardTemplate
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
		[new CompanionDamageVar(CompanionSummons.SecondBrotherId, 12)];
	public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/GuanDiForm.png");
	public FairWeatherBrothers() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }
	protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
	{
		var pet = CompanionSummons.GetCompanion(Owner, CompanionSummons.SecondBrotherId);
		if (pet is not { IsAlive: true } || play.Target == null) return;
		await DamageCmd.Attack(DynamicVars["CompanionDamage"].BaseValue).FromCompanion(pet, this, play)
			.Targeting(play.Target).WithHitFx("vfx/vfx_attack_blunt", tmpSfx: "blunt_attack.mp3").Execute(ctx);
		if (pet.IsAlive) await PowerCmd.Apply<BrotherEstrangementPower>(ctx, pet, 2, Owner.Creature, this);
	}
	protected override void OnUpgrade() => DynamicVars["CompanionDamage"].UpgradeValueBy(4);
}
