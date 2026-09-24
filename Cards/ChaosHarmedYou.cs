using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Squ;
using Squ.Audio;
using Squ.Character;
using Squ.Combat;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "chaos_harmed_you")]
public sealed class ChaosHarmedYou : ModCardTemplate
{
	public const int BaseDamage = 21;
	public const int UpgradedDamage = 28;
	public const int DrawOnKill = 1;
	public const int UpgradedDrawOnKill = 2;
	public const int BaseEnergyOnKill = 1;
	public const int UpgradedEnergyOnKill = 2;

	private static readonly ValueProp DamageProps = ValueProp.Move | ValueProp.Unpowered;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(BaseDamage, DamageProps),
		new CardsVar(DrawOnKill),
		new EnergyVar(BaseEnergyOnKill),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips
	{
		get
		{
			List<IHoverTip> tips =
			[
				HoverTipFactory.FromKeyword(SquKeywords.Environmental),
				HoverTipFactory.ForEnergy(this),
			];
			if (!ShouldShowAttackPlayNote())
			{
				return tips;
			}

			if (Owner.Creature.GetPower<LethalityPower>() is { Amount: > 0 })
			{
				tips.Add(HoverTipFactory.FromPower<LethalityPower>());
			}

			if (Owner.Creature.GetPower<KeepVigorPower>() is { Amount: > 0 })
			{
				tips.Add(HoverTipFactory.FromPower<KeepVigorPower>());
			}

			return tips;
		}
	}

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/ChaosHarmedYou.png");

	protected override bool ShouldGlowGoldInternal
	{
		get
		{
			ICombatState? combatState = CombatState;
			if (combatState == null)
			{
				return false;
			}

			foreach (Creature enemy in combatState.HittableEnemies)
			{
				if (enemy.IsAlive && WouldKill(combatState, enemy))
				{
					return true;
				}
			}

			return false;
		}
	}

	public ChaosHarmedYou()
		: base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
		Creature target = cardPlay.Target;
		ICombatState? combatState = CombatState;
		bool canKill = combatState is not null && WouldKill(combatState, target);
		SquSfx.Play(canKill
			? SquSfx.ChaosHarmedYouNotDieInVainEvent
			: SquSfx.ChaosHarmedYouNotAmanEvent);

		AttackCommand attackCommand = await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.WithDamageProps(DamageProps)
			.FromCard(this, cardPlay)
			.Targeting(target)
			.WithHitFx("vfx/vfx_attack_slash")
			.Execute(choiceContext);

		if (attackCommand.Results.SelectMany(results => results).Any(result => result.WasTargetKilled))
		{
			await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
			await PlayerCmd.GainEnergy((int)DynamicVars.Energy.BaseValue, Owner);
		}
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(UpgradedDamage - BaseDamage);
		DynamicVars.Cards.UpgradeValueBy(UpgradedDrawOnKill - DrawOnKill);
		DynamicVars.Energy.UpgradeValueBy(UpgradedEnergyOnKill - BaseEnergyOnKill);
	}

	protected override void AddExtraArgsToDescription(LocString description)
	{
		if (!ShouldShowAttackPlayNote())
		{
			description.Add("AttackPlayNote", string.Empty);
			return;
		}

		SquKeywords.AddNestedLoc(
			description,
			"AttackPlayNote",
			new LocString("cards", Id.Entry + ".attackPlayNote"));
	}

	internal static bool DoesNotConsumeAttackPlayTracking(CardModel? card) => card is ChaosHarmedYou;

	private bool ShouldShowAttackPlayNote()
	{
		if (!IsInCombat || Owner.Creature is not { } creature)
		{
			return false;
		}

		return creature.GetPower<LethalityPower>() is { Amount: > 0 }
			|| creature.GetPower<KeepVigorPower>() is { Amount: > 0 };
	}

	private bool WouldKill(ICombatState combatState, Creature enemy)
	{
		decimal damage = Hook.ModifyDamage(
			Owner.RunState,
			combatState,
			enemy,
			Owner.Creature,
			DynamicVars.Damage.BaseValue,
			DamageProps,
			this,
			null,
			ModifyDamageHookType.All,
			CardPreviewMode.None,
			out _);

		decimal blocked = DamageProps.HasFlag(ValueProp.Unblockable)
			? 0m
			: Math.Min((decimal)enemy.Block, damage);
		return damage - blocked >= enemy.CurrentHp;
	}
}
