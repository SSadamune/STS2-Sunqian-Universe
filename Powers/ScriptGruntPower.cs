using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Audio;
using Squ.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

[RegisterPower]
public sealed class ScriptGruntPower : ScriptPowerTemplate
{
	private sealed class Data
	{
		public int BonusDamage = BaseBonusDamage;
	}

	public const int BaseBonusDamage = 3;
	public const int UpgradedBonusDamage = 4;

	public int BonusDamage => GetInternalData<Data>().BonusDamage;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/ScriptSentryPower.png",
		BigIconPath: "res://images/powers/ScriptSentryPowerBig.png");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromKeyword(SquKeywords.VariousStrikeCards),
	];

	public override LocString Description
	{
		get
		{
			LocString description = base.Description;
			description.Add(nameof(BonusDamage), BonusDamage);
			return description;
		}
	}

	protected override object InitInternalData() => new Data();

	protected override Task OnScriptApplied(Creature? applier, CardModel? cardSource)
	{
		SquSfx.Play(SquSfx.GruntScriptActivatedEvent);
		GetInternalData<Data>().BonusDamage = cardSource is GruntScript { IsUpgraded: true }
			? UpgradedBonusDamage
			: BaseBonusDamage;
		return Task.CompletedTask;
	}

	public override decimal ModifyDamageAdditive(
		Creature? target,
		decimal amount,
		ValueProp props,
		Creature? dealer,
		CardModel? card,
		CardPlay? cardPlay)
	{
		if (Owner != dealer || card is null || !props.IsPoweredAttack())
		{
			return 0m;
		}

		return card.Tags.Contains(CardTag.Strike) ? BonusDamage : 0m;
	}
}
