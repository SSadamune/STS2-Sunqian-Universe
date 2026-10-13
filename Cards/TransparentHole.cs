using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Audio;
using Squ.Character;
using Squ.Combat;
using STS2RitsuLib.Combat.AttackHits;
using STS2RitsuLib.Combat.CardTargeting;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "transparent_hole")]
public sealed class TransparentHole : ModCardTemplate, IRandomEnemyTargetCount, IAttackHitHookListener
{
	public const int BaseDamage = 7;

	private const float RepeatAttackDelaySeconds = 0.3f;

	public const decimal CascadeDamageMultiplier = 0.3m;

	private const string CascadeDamageMultiplierVarName = "CascadeDamageMultiplier";

	private AttackCommand? _cascadeAttack;

	private int _cascadeInitialCount;

	private decimal _cascadeDamage;

	protected override bool HasEnergyCostX => true;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(BaseDamage, ValueProp.Move),
		new DynamicVar(CascadeDamageMultiplierVarName, CascadeDamageMultiplier * 100m),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/TransparentHole.png");

	public override TargetType TargetType => SquTargetTypes.RandomEnemies;

	public TransparentHole()
		: base(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
	{
	}

	public int GetRandomEnemyTargetCount() => ResolveTargetCount();

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ICombatState? combatState = CombatState;
		if (combatState == null)
		{
			return;
		}

		int targetCount = ResolveTargetCount();
		if (targetCount <= 0)
		{
			return;
		}

		List<Creature> firstVolley = SquRandomEnemyTargeting.SelectRandomEnemies(this, targetCount);
		if (firstVolley.Count == 0)
		{
			return;
		}

		int wave = 1;
		AttackCommand attack = DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.FromCard(this, cardPlay)
			.TargetingAllOpponents(combatState)
			.TargetingFiltered(firstVolley)
			.WithHitCount(targetCount)
			.WithHitVfxNode(target => CreateStab(target, wave))
			.BeforeDamage(() =>
			{
				wave++;
				return Task.CompletedTask;
			});

		_cascadeAttack = attack;
		_cascadeInitialCount = targetCount;
		_cascadeDamage = DynamicVars.Damage.BaseValue;
		try
		{
			await attack.Execute(choiceContext);
		}
		finally
		{
			_cascadeAttack = null;
		}
	}

	public Task BeforeAttackHit(AttackHitContext context)
	{
		if (context.Attack != _cascadeAttack)
		{
			return Task.CompletedTask;
		}

		PlayTransparentHoleSfx();
		context.Damage = _cascadeDamage;
		return Task.CompletedTask;
	}

	public async Task AfterAttackHit(AttackHitContext context)
	{
		if (context.Attack != _cascadeAttack)
		{
			return;
		}

		int previousTargetCount = _cascadeInitialCount - context.HitIndex;
		int nextTargetCount = previousTargetCount - 1;
		int aliveEnemyCount = context.CombatState.HittableEnemies.Count(creature => creature.IsAlive);
		if (nextTargetCount <= 0 || aliveEnemyCount >= previousTargetCount)
		{
			context.Attack.TargetingFiltered([]);
			return;
		}

		_cascadeDamage = decimal.Floor(_cascadeDamage * (1m + CascadeDamageMultiplier));
		context.Attack.TargetingFiltered(
			SquRandomEnemyTargeting.SelectRandomEnemies(this, nextTargetCount));
		await Cmd.Wait(RepeatAttackDelaySeconds);
	}

	private static NStabVfx? CreateStab(Creature target, int wave)
	{
		NStabVfx? stab = NStabVfx.Create(target, facingEnemies: true);
		if (stab == null)
		{
			return null;
		}

		Color color = StabColorForWave(wave);
		Color highlight = wave >= 7 ? DeepPurpleHighlight : color.Lerp(Colors.White, 0.4f);
		stab.Connect(
			Node.SignalName.Ready,
			Callable.From(() => TintStab(stab, color, highlight)));
		return stab;
	}

	private static readonly Color DeepPurple = new("240046");

	private static readonly Color DeepPurpleHighlight = new("4A148C");

	private static Color StabColorForWave(int wave)
	{
		(int Wave, Color Color)[] stops =
		[
			(1, new Color("FFFFFF")),
			(3, new Color("FFE14A")),
			(5, new Color("FF0000")),
			(7, DeepPurple),
		];
		wave = Math.Max(wave, 1);
		if (wave >= stops[^1].Wave)
		{
			return stops[^1].Color;
		}

		for (int i = 0; i < stops.Length - 1; i++)
		{
			(int startWave, Color start) = stops[i];
			(int endWave, Color end) = stops[i + 1];
			if (wave > endWave)
			{
				continue;
			}

			float t = (wave - startWave) / (float)(endWave - startWave);
			return start.Lerp(end, t);
		}

		return stops[^1].Color;
	}

	private static void TintStab(NStabVfx stab, Color color, Color highlight)
	{
		if (stab.GetNodeOrNull<CanvasItem>("Primary") is { } primary)
		{
			primary.SelfModulate = color;
		}

		if (stab.GetNodeOrNull<CanvasItem>("%Secondary") is { } secondary)
		{
			secondary.SelfModulate = highlight;
		}
	}

	private void PlayTransparentHoleSfx()
	{
		SquSfx.PlayRandom(RunState, SquSfx.TransparentHoleEvents);
	}

	private int ResolveTargetCount()
	{
		int targetCount = ResolveEnergyXValue();
		if (IsUpgraded)
		{
			targetCount++;
		}

		return targetCount;
	}
}
