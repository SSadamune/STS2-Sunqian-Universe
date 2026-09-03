using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ;
using Squ.Audio;
using Squ.Character;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "bombard_chibi_script")]
public sealed class BombardChibiScript : ScriptCardTemplate
{
	public const int BaseBlock = 7;
	public const int UpgradedBlock = 10;
	public const int BaseBurning = 7;
	public const int UpgradedBurning = 10;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new BlockVar(BaseBlock, ValueProp.Move),
		new PowerVar<BurningPower>(BaseBurning),
	];

	public override bool GainsBlock => true;

	protected override HashSet<CardTag> CanonicalTags => [SquCardTags.Script, SquCardTags.Burning];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<BurningPower>(),
		HoverTipFactory.FromPower<StrengthPower>(),
	];

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		SquKeywords.Script,
		CardKeyword.Exhaust,
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/BombardChibiScript.png");

	public BombardChibiScript()
		: base(3, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies, true)
	{
	}

	protected override async Task PlayScriptAsync(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ICombatState? combatState = CombatState;
		ArgumentNullException.ThrowIfNull(combatState, nameof(combatState));

		SquSfx.Play(SquSfx.BombardChibiIgniteEvent);
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
		await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

		decimal burningStacks = DynamicVars[nameof(BurningPower)].BaseValue;
		foreach (Creature enemy in combatState.HittableEnemies)
		{
			if (!enemy.IsAlive)
			{
				continue;
			}

			await PowerCmd.Apply<BurningPower>(
				choiceContext,
				enemy,
				burningStacks,
				Owner.Creature,
				this);
		}

		await PowerCmd.Apply<ScriptBombardChibiPower>(
			choiceContext,
			Owner.Creature,
			1m,
			Owner.Creature,
			this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Block.UpgradeValueBy(UpgradedBlock - BaseBlock);
		DynamicVars[nameof(BurningPower)].UpgradeValueBy(UpgradedBurning - BaseBurning);
	}
}
