using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Character;
using Squ.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "salvo_strike")]
public sealed class SalvoStrike : ModCardTemplate, IRandomEnemyTargetCount
{
	public const int BaseDamage = 9;
	public const int UpgradedDamage = 12;
	public const int RandomEnemyTargetCount = 3;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(BaseDamage, ValueProp.Move),
	];

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		CardKeyword.Exhaust,
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/SalvoStrike.png");

	public override TargetType TargetType => SquTargetTypes.RandomEnemies;

	public SalvoStrike()
		: base(1, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy, showInCardLibrary: false)
	{
	}

	public int GetRandomEnemyTargetCount() => RandomEnemyTargetCount;

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await SquRandomEnemyTargeting.ExecuteDistinctRandomEnemyDamage(
			this,
			choiceContext,
			RandomEnemyTargetCount);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(UpgradedDamage - BaseDamage);
	}
}
