using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ;
using Squ.Cards;
using Squ.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Content.Patches;

#nullable enable

namespace Squ.Powers;

/// <summary>
/// 「天意沟通」的持续能力：每回合开始时[gold]预见[/gold] <see cref="ScryAmount"/>，
/// 因此置入弃牌堆的牌改为[gold]消耗[/gold]；每因此消耗一张[gold]攻击[/gold]/[gold]技能[/gold]牌，
/// 本回合获得 <see cref="Amount"/> 点[gold]力量[/gold]/[gold]敏捷[/gold]。
/// </summary>
[RegisterPower]
public sealed class TianyiGoutongPower : ModPowerTemplate
{
	private const decimal ScryAmount = 3m;

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/TianyiGoutongPower.png",
		BigIconPath: "res://images/powers/TianyiGoutongPowerBig.png");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromKeyword(SquKeywords.Scry),
		HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
		HoverTipFactory.FromPower<StrengthPower>(),
		HoverTipFactory.FromPower<DexterityPower>(),
	];

	public override async Task BeforeSideTurnStart(
		PlayerChoiceContext choiceContext,
		CombatSide side,
		IReadOnlyList<Creature> participants,
		ICombatState combatState)
	{
		if (!participants.Contains(Owner) || Owner.IsDead || Owner.Player is not { } player)
		{
			return;
		}

		Flash();
		await ScryCmd.Execute(choiceContext, player, (int)ScryAmount, HandleChosenCard);
	}

	private async Task HandleChosenCard(PlayerChoiceContext choiceContext, CardModel card)
	{
		Creature owner = card.Owner.Creature;

		await CardCmd.Exhaust(choiceContext, card);

		switch (card.Type)
		{
			case CardType.Attack:
				await PowerCmd.Apply<TempStrFromTianyiGoutongPower>(choiceContext, owner, Amount, owner, null);
				break;
			case CardType.Skill:
				await PowerCmd.Apply<TempDexFromTianyiGoutongPower>(choiceContext, owner, Amount, owner, null);
				break;
		}
	}
}

[RegisterPower]
public sealed class TempDexFromTianyiGoutongPower : TempDexPower<TianyiGoutongPower> { }

[RegisterPower]
public sealed class TempStrFromTianyiGoutongPower : TemporaryStrengthPower, IModPowerAssetOverrides
{
	private static readonly PowerModel SetupStrikePowerTemplate = ModelDb.Power<SetupStrikePower>();

	public override AbstractModel OriginModel => ModelDb.Card<TianyiGoutong>();

	public PowerAssetProfile AssetProfile => new(
		IconPath: SetupStrikePowerTemplate.PackedIconPath,
		BigIconPath: SetupStrikePowerTemplate.ResolvedBigIconPath);

	public string? CustomIconPath => AssetProfile.IconPath;

	public string? CustomBigIconPath => AssetProfile.BigIconPath;
}
