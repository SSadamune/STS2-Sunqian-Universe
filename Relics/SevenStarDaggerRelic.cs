using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.RelicPools;
using Squ.Audio;
using Squ.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Relics;

/// <summary>
/// 七星宝刀：每场战斗开始时，将一张具有保留和锋利 3 附魔的《光剑刺杀+》加入手牌。
/// </summary>
[RegisterRelic(typeof(SharedRelicPool), StableEntryStem = "seven_star_dagger")]
public sealed class SevenStarDaggerRelic : ModRelicTemplate
{
	public const int SharpAmount = 3;

	public override RelicRarity Rarity => RelicRarity.Uncommon;

	protected override IEnumerable<IHoverTip> AdditionalHoverTips
	{
		get
		{
			CardModel assassination = ModelDb.Card<LaserSwordAssassination>().ToMutable();
			ConfigureAssassination(assassination);

			// 百科悬停牌没有 RunState/CombatState。NCard.UpdateVisuals 会先清除 PreviewValue，
			// 随后 CardModel.UpdateDynamicVarPreview 会直接返回，无法重新计入锋利。
			// 只在这张展示副本上固化锋利加成；UpgradeValueBy 同时保留绿色高亮标记。
			// 实战生成牌仍为基础 4 + 锋利 3。
			assassination.DynamicVars.Damage.UpgradeValueBy(SharpAmount);

			yield return new CardHoverTip(assassination);
			foreach (IHoverTip hoverTip in assassination.HoverTips)
			{
				yield return hoverTip;
			}
		}
	}

	public override RelicAssetProfile AssetProfile => new(
		IconPath: "res://images/relics/SevenStarDaggerRelic.png",
		IconOutlinePath: "res://images/relics/SevenStarDaggerRelicOutline.png",
		BigIconPath: "res://images/relics/SevenStarDaggerRelicBig.png");

	public override async Task AfterSideTurnStart(
		CombatSide side,
		IReadOnlyList<Creature> participants,
		ICombatState combatState)
	{
		if (!participants.Contains(Owner.Creature) || Owner.PlayerCombatState?.TurnNumber > 1)
		{
			return;
		}

		CardModel assassination = combatState.CreateCard<LaserSwordAssassination>(Owner);
		ConfigureAssassination(assassination);

		Flash();
		SquSfx.Play(SquSfx.SevenStarDaggerEvent);
		await CardPileCmd.AddGeneratedCardToCombat(assassination, PileType.Hand, Owner);
	}

	private static void ConfigureAssassination(CardModel assassination)
	{
		assassination.UpgradeInternal();
		assassination.FinalizeUpgradeInternal();
		CardCmd.Enchant<Sharp>(assassination, SharpAmount);
		assassination.AddKeyword(CardKeyword.Retain);
	}
}
