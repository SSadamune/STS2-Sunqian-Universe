using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using Squ.Character;
using Squ.Combat;
using Squ.Monsters;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// 变异半人马：为一个非爪牙敌人召唤与其绑定的坐骑。坐骑承受的生命伤害与致命溢出
/// 会按倍率转化为绑定敌人的无视格挡、无修正伤害。
/// </summary>
// [RegisterCard(typeof(SunqianCardPool), StableEntryStem = "mutated_centaur")]
public sealed class MutatedCentaur : ModCardTemplate
{
	public const string DamagePercentVarName = "DamagePercent";
	public const int BaseDamagePercent = 125;
	public const int UpgradedDamagePercent = 150;
	public const decimal MountHpRatio = 0.4m;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar(DamagePercentVarName, BaseDamagePercent),
	];

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		CardKeyword.Exhaust,
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<MinionPower>(),
		new HoverTip(
			new LocString("cards", Id.Entry + ".mountTitle"),
			new LocString("cards", Id.Entry + ".mountDescription")),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/SendHorseWithoutHay.png");

	public MutatedCentaur()
		: base(1, CardType.Skill, CardRarity.Rare, SquTargetTypes.AnyRealEnemy)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
		ICombatState combatState = CombatState
			?? throw new InvalidOperationException("MutatedCentaur requires an active combat.");
		Creature boundEnemy = cardPlay.Target;
		MonsterModel sourceMonster = boundEnemy.Monster
			?? throw new InvalidOperationException("MutatedCentaur requires a monster target.");
		string visualsPath = sourceMonster.AssetPaths.First();
		int mountHp = Math.Max(1, (int)(boundEnemy.MaxHp * MountHpRatio));

		var mountModel = (MutatedCentaurMount)ModelDb.Monster<MutatedCentaurMount>().ToMutable();
		mountModel.Configure(boundEnemy.Name, visualsPath, mountHp);
		Creature mount = await CreatureCmd.Add(mountModel, combatState);

		await PowerCmd.Apply<MinionPower>(
			choiceContext,
			mount,
			1m,
			Owner.Creature,
			this);

		var linkPower = (MutatedCentaurLinkPower)ModelDb
			.Power<MutatedCentaurLinkPower>()
			.ToMutable();
		linkPower.Bind(
			boundEnemy,
			DynamicVars[DamagePercentVarName].BaseValue / 100m);
		await PowerCmd.Apply(
			choiceContext,
			linkPower,
			mount,
			1m,
			Owner.Creature,
			this);

		ApplyMountVisuals(mount);
	}

	protected override void OnUpgrade()
	{
		DynamicVars[DamagePercentVarName].UpgradeValueBy(
			UpgradedDamagePercent - BaseDamagePercent);
	}

	private static void ApplyMountVisuals(Creature mount)
	{
		NCreature? node = mount.GetCreatureNode();
		if (node is null)
		{
			return;
		}

		node.SetScaleAndHue(0.75f, 0f);
		Color modulate = node.Visuals.Modulate;
		modulate.A = 0.75f;
		node.Visuals.Modulate = modulate;
	}
}
