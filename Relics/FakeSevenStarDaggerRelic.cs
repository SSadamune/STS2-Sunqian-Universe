using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using Squ.Audio;
using STS2RitsuLib.Interactions.RightClick;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Relics;

/// <summary>
/// 七星宝刀？？？：在战斗中右键点击，视为打出一张《七星》，然后永久失效。
/// </summary>
[RegisterRelic(typeof(EventRelicPool), StableEntryStem = "fake_seven_star_dagger")]
public sealed class FakeSevenStarDaggerRelic : ModRelicTemplate, IModRightClickableRelic
{
	private bool _wasUsed;

	public override RelicRarity Rarity => RelicRarity.Event;

	public override int MerchantCost => 50;

	public override bool IsUsedUp => WasUsed;

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromKeyword(SquKeywords.CountsAsPlayed),
		..HoverTipFactory.FromCardWithCardHoverTips<SevenStars>(),
	];

	public override RelicAssetProfile AssetProfile => new(
		IconPath: "res://images/relics/FakeSevenStarDaggerRelic.png",
		IconOutlinePath: "res://images/relics/FakeSevenStarDaggerRelicOutline.png",
		BigIconPath: "res://images/relics/FakeSevenStarDaggerRelicBig.png");

	[SavedProperty]
	public bool WasUsed
	{
		get => _wasUsed;
		set
		{
			AssertMutable();
			_wasUsed = value;
			if (IsUsedUp)
			{
				Status = RelicStatus.Disabled;
			}
		}
	}

	public bool CanHandleRightClickLocal(ModRightClickContext context) =>
		!IsUsedUp
		&& ReferenceEquals(context.Model, this)
		&& ReferenceEquals(context.Player, Owner)
		&& context.Player.Creature.CombatState is not null;

	public bool CanExecuteRightClick(ModRightClickExecutionContext context) =>
		!IsUsedUp
		&& ReferenceEquals(context.Model, this)
		&& ReferenceEquals(context.Player, Owner)
		&& context.PlayerChoiceContext is not null
		&& Owner.Creature.CombatState is not null;

	public async Task OnRightClick(ModRightClickExecutionContext context)
	{
		if (!CanExecuteRightClick(context)
			|| context.PlayerChoiceContext is not { } choiceContext
			|| Owner.Creature.CombatState is not { } combatState)
		{
			return;
		}

		CardModel sevenStars = combatState.CreateCard<SevenStars>(Owner);
		Flash();
		SquSfx.Play(SquSfx.FakeSevenStarDaggerEvent);
		await CardCmd.AutoPlay(choiceContext, sevenStars, target: null);
		WasUsed = true;
	}

	private const int FakeMerchantWeight = 2;

	private static RelicModel[] WithFakeSevenStarDagger(RelicModel[] inventory)
	{
		RelicModel relic = ModelDb.Relic<FakeSevenStarDaggerRelic>();
		if (inventory.Any(candidate => candidate.Id == relic.Id))
		{
			return inventory;
		}

		RelicModel[] expanded = new RelicModel[inventory.Length + FakeMerchantWeight];
		Array.Copy(inventory, expanded, inventory.Length);
		for (int i = 0; i < FakeMerchantWeight; i++)
		{
			expanded[inventory.Length + i] = relic;
		}

		return expanded;
	}

	private static IEnumerable<RelicModel> TakeUnique(IEnumerable<RelicModel> source, int count)
	{
		List<RelicModel> picked = [];
		foreach (RelicModel relic in source)
		{
			if (picked.Any(existing => existing.Id == relic.Id))
			{
				continue;
			}

			picked.Add(relic);
			if (picked.Count == count)
			{
				break;
			}
		}

		return picked;
	}

	[HarmonyPatch(typeof(FakeMerchant), "BeforeEventStarted")]
	private static class FakeMerchantInventoryPatch
	{
		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			FieldInfo inventoryField = AccessTools.Field(typeof(FakeMerchant), "_inventoryRelics");
			MethodInfo expand = AccessTools.Method(
				typeof(FakeSevenStarDaggerRelic),
				nameof(WithFakeSevenStarDagger));
			MethodInfo takeUnique = AccessTools.Method(
				typeof(FakeSevenStarDaggerRelic),
				nameof(TakeUnique));
			foreach (CodeInstruction instruction in instructions)
			{
				if (instruction.opcode == OpCodes.Call
					&& instruction.operand is MethodInfo method
					&& method.Name == nameof(Enumerable.Take)
					&& method.DeclaringType == typeof(Enumerable))
				{
					yield return new CodeInstruction(OpCodes.Call, takeUnique);
					continue;
				}

				yield return instruction;
				if (instruction.LoadsField(inventoryField))
				{
					yield return new CodeInstruction(OpCodes.Call, expand);
				}
			}
		}
	}
}
