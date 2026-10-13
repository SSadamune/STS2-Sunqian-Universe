using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Audio;
using Squ.Combat;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(TokenCardPool), StableEntryStem = "salvo_strike")]
public sealed class SalvoStrike : ModCardTemplate, IRandomEnemyTargetCount
{
	public const int BaseDamage = 10;
	public const int UpgradedDamage = 14;
	public const int RandomEnemyTargetCount = 3;
	public const int BaseArrowRainLayers = 2;
	public const int BonusPercentPerArrowRainLayer = 50;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(BaseDamage, ValueProp.Move),
	];

	protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		CardKeyword.Exhaust,
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/SalvoStrike.png");

	public override TargetType TargetType => SquTargetTypes.RandomEnemies;

	public SalvoStrike()
		: base(1, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy)
	{
	}

	public int GetRandomEnemyTargetCount() => RandomEnemyTargetCount;

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		SquSfx.Play(SquSfx.ArrowCurtainEvent);
		await SquRandomEnemyTargeting.ExecuteDistinctRandomEnemyDamage(
			this,
			choiceContext,
			RandomEnemyTargetCount,
			hitFx: null,
			cardPlay: cardPlay,
			beforeHit: targets => SquHitVfx.ArrowRain(targets, GetArrowRainLayers()));
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(UpgradedDamage - BaseDamage);
	}

	private int GetArrowRainLayers()
	{
		if (CombatState?.HittableEnemies.Count != 1)
		{
			return BaseArrowRainLayers;
		}

		decimal bonusPercent = Owner.Creature
			.GetPower<ScriptExecutionCommanderPower>()?.Amount ?? 0m;
		int bonusLayers = bonusPercent > 0m
			? (int)decimal.Floor(bonusPercent / BonusPercentPerArrowRainLayer)
			: 0;
		return BaseArrowRainLayers + bonusLayers;
	}
}
