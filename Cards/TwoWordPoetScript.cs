using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.ValueProps;
using Squ;
using Squ.Audio;
using Squ.Character;
using Squ.Combat;
using Squ.Interop;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "two_word_poet_script")]
public sealed class TwoWordPoetScript : ScriptCardTemplate<TwoWordPoetScriptRecord>
{
	public const int CanonicalDamage = 2;
	public const int CanonicalHits = 2;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(CanonicalDamage, ValueProp.Move),
		new RepeatVar(CanonicalHits),
	];

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		SquKeywords.Script,
		CardKeyword.Exhaust,
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/TwoWordPoetScript.png");

	public override TargetType TargetType => IsUpgraded ? TargetType.AllEnemies : TargetType.AnyEnemy;

	public TwoWordPoetScript()
		: base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy, true)
	{
	}

	protected override async Task PlayScriptAsync(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		SquSfx.Play(SquSfx.TwoWordWineAndSongEvent);
		int vigorToStore = SquVigorSnapshot.GetEffectiveAmount(Owner.Creature, this);
		int drunkenMightToStore = NewsanguoDrunkenMightEffect.ConsumedByAttack(Owner.Creature);
		int enchantedVigorToStore = GetVigorousEnchantmentAmount();
		var attack = DamageCmd.Attack(DynamicVars.Damage.BaseValue).WithHitCount(CanonicalHits).FromCard(this, cardPlay);
		if (IsUpgraded)
		{
			ArgumentNullException.ThrowIfNull(CombatState);
			await attack.TargetingAllOpponents(CombatState).Execute(choiceContext);
		}
		else
		{
			ArgumentNullException.ThrowIfNull(cardPlay.Target);
			await attack.Targeting(cardPlay.Target).Execute(choiceContext);
		}

		await UnlockScriptAsync(
			choiceContext,
			CanonicalDamage,
			configureRecord: record => StoreConsumedVigor(
				record,
				vigorToStore + drunkenMightToStore + enchantedVigorToStore));
	}

	private int GetVigorousEnchantmentAmount()
	{
		if (Enchantment is not Vigorous { Status: EnchantmentStatus.Normal } vigorous)
		{
			return 0;
		}

		return Math.Max(0, vigorous.Amount);
	}

	private void StoreConsumedVigor(TwoWordPoetScriptRecord record, int amount)
	{
		record.AddConsumedVigor(this, amount);
		Owner.Creature.GetPower<ScriptTwoWordPoetPower>()?.SyncStoredDamage(record.StoredAmount);
	}

	internal override int GetScriptPowerAmountForPreview() => CanonicalDamage;
}
