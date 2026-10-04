using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Squ;
using Squ.Audio;
using Squ.Character;
using Squ.Powers;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "flame_strike")]
public sealed class FlameStrike : ModCardTemplate
{
	private const string TargetPreviewVarName = "TargetPreview";
	private const int DamageTwicePreview = 1;
	private const int BurningTwicePreview = 2;

	public const int BaseDamage = 6;
	public const int UpgradedDamage = 8;
	public const int BaseBurning = 3;
	public const int UpgradedBurning = 4;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(BaseDamage, ValueProp.Move),
		new PowerVar<BurningPower>(BaseBurning),
		ModCardVars.Computed(TargetPreviewVarName, 0,
			static (CardModel? card, Creature? target) =>
			{
				if (!IsCombatTargetPreview(card, target))
				{
					return 0;
				}

				return target!.HasPower<BurningPower>()
					? DamageTwicePreview
					: BurningTwicePreview;
			}),
	];

	protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike, SquCardTags.Burning];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<BurningPower>(),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/FlameStrike.png");

	public FlameStrike()
		: base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
		SquSfx.Play(SquSfx.FlameStrikeEvent);
		bool targetWasBurning = cardPlay.Target.HasPower<BurningPower>();

		await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.WithHitCount(targetWasBurning ? 2 : 1)
			.FromCard(this, cardPlay)
			.Targeting(cardPlay.Target)
			.WithHitFx("vfx/vfx_attack_slash")
			.Execute(choiceContext);

		int burningApplications = targetWasBurning ? 1 : 2;
		for (int i = 0; i < burningApplications; i++)
		{
			await PowerCmd.Apply<BurningPower>(
				choiceContext,
				cardPlay.Target,
				DynamicVars[nameof(BurningPower)].BaseValue,
				Owner.Creature,
				this);
		}
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(UpgradedDamage - BaseDamage);
		DynamicVars[nameof(BurningPower)].UpgradeValueBy(UpgradedBurning - BaseBurning);
	}

	protected override void AddExtraArgsToDescription(LocString description)
	{
		string bodyKey = DynamicVars[TargetPreviewVarName].PreviewValue switch
		{
			DamageTwicePreview => "damageTwiceBody",
			BurningTwicePreview => "burningTwiceBody",
			_ => "normalBody",
		};

		LocString body = new("cards", Id.Entry + "." + bodyKey);
		body.Add(DynamicVars.Damage);
		body.Add(DynamicVars[nameof(BurningPower)]);
		description.Add("BodyText", body);
	}

	private static bool IsCombatTargetPreview(CardModel? card, Creature? target) =>
		card is FlameStrike { IsMutable: true, RunState: not null, CombatState: not null } flameStrike
		&& flameStrike.Owner?.PlayerCombatState is not null
		&& CombatManager.Instance.IsInProgress
		&& target is { IsAlive: true };
}
