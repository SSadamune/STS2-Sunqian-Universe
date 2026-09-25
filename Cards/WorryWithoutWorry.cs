#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Character;
using Squ.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Cards;

/// <summary>无忧而虑：视敌方攻击意图决定第一段格挡立即获得还是延迟到下一回合。</summary>
[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "worry_without_worry")]
public sealed class WorryWithoutWorry : ModCardTemplate
{
	public const int ImmediateBlock = 5;
	public const int UpgradedImmediateBlock = 8;
	public const int NextTurnBlock = 10;
	public const int UpgradedNextTurnBlock = 13;

	private const string NextTurnBlockVar = "BlockNextTurn";

	private static readonly ValueProp BlockProps = ValueProp.Move;

	public override bool GainsBlock => true;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new BlockVar(ImmediateBlock, BlockProps),
		new BlockVar(NextTurnBlockVar, NextTurnBlock, BlockProps),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<BlockNextTurnPower>(),
	];

	public WorryWithoutWorry()
		: base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		Creature owner = Owner.Creature;
		bool enemyIntendsToAttack = CombatState?.HittableEnemies.Any(enemy =>
			enemy.IsAlive && SquEnemyIntent.IntendsToAttack(enemy)) == true;

		if (enemyIntendsToAttack)
		{
			await CreatureCmd.GainBlock(owner, DynamicVars.Block, cardPlay);
		}
		else
		{
			await ApplyBlockNextTurn(
				choiceContext,
				DynamicVars.Block,
				cardPlay);
		}

		await ApplyBlockNextTurn(
			choiceContext,
			(BlockVar)DynamicVars[NextTurnBlockVar],
			cardPlay);
	}

	private async Task ApplyBlockNextTurn(
		PlayerChoiceContext choiceContext,
		BlockVar blockVar,
		CardPlay cardPlay)
	{
		decimal amount = Hook.ModifyBlock(
			CombatState!,
			Owner.Creature,
			blockVar.BaseValue,
			blockVar.Props,
			this,
			cardPlay,
			out IEnumerable<AbstractModel> _);

		await PowerCmd.Apply<BlockNextTurnPower>(
			choiceContext,
			Owner.Creature,
			amount,
			Owner.Creature,
			this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Block.UpgradeValueBy(UpgradedImmediateBlock - ImmediateBlock);
		DynamicVars[NextTurnBlockVar]
			.UpgradeValueBy(UpgradedNextTurnBlock - NextTurnBlock);
	}
}
