using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Events.Custom;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using Squ.Character;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Potions;

/// <summary>
/// 沛国佳酿：战斗中获得能量与活力；也可投掷给商人换取金币（参考 FoulPotion）。
/// </summary>
[RegisterPotion(typeof(SunqianPotionPool), StableEntryStem = "peiguo_brew")]
public sealed class PeiguoBrewPotion : ModPotionTemplate
{
	public const int EnergyAmount = 1;
	public const int VigorAmount = 6;
	public const int MerchantGold = 100;

	public override PotionRarity Rarity => PotionRarity.Uncommon;

	public override PotionUsage Usage => PotionUsage.AnyTime;

	public override TargetType TargetType
	{
		get
		{
			if (!CombatManager.Instance.IsInProgress)
			{
				return TargetType.TargetedNoCreature;
			}

			return TargetType.Self;
		}
	}

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new EnergyVar(EnergyAmount),
		new PowerVar<VigorPower>(VigorAmount),
		new GoldVar(MerchantGold),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.ForEnergy(this),
		HoverTipFactory.FromPower<VigorPower>(),
	];

	public override PotionAssetProfile AssetProfile => new(
		ImagePath: "res://images/potions/PeiguoBrew.png",
		OutlinePath: "res://images/potions/PeiguoBrewOutline.png");

	public override bool PassesCustomUsabilityCheck
	{
		get
		{
			if (CombatManager.Instance.IsInProgress)
			{
				return true;
			}

			if (Owner.RunState.CurrentRoom is MerchantRoom)
			{
				return FoulPotion.GetFoulPotionMerchantTarget(Owner.RunState.CurrentRoom).button != null;
			}

			if (Owner.RunState.CurrentRoom is EventRoom eventRoom
				&& eventRoom.CanonicalEvent is FakeMerchant)
			{
				return FoulPotion.GetFoulPotionMerchantTarget(Owner.RunState.CurrentRoom).button != null;
			}

			return false;
		}
	}

	protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
	{
		if (CombatManager.Instance.IsInProgress)
		{
			await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
			await PowerCmd.Apply<VigorPower>(
				choiceContext,
				Owner.Creature,
				DynamicVars[nameof(VigorPower)].BaseValue,
				Owner.Creature,
				null);
			return;
		}

		if (Owner.RunState.CurrentRoom is MerchantRoom)
		{
			NMerchantRoom? nMerchantRoom = NRun.Instance?.MerchantRoom;
			if (nMerchantRoom != null)
			{
				ShowPotionVfx(nMerchantRoom.MerchantButton);
				// NMerchantRoom.FoulPotionThrown only plays thank-you SFX/dialogue; gold is separate.
				nMerchantRoom.FoulPotionThrown(CreateFoulPotionProxy());
			}

			await PlayerCmd.GainGold(DynamicVars.Gold.BaseValue, Owner);
			return;
		}

		if (Owner.RunState.CurrentRoom is not EventRoom eventRoom
			|| eventRoom.CanonicalEvent is not FakeMerchant)
		{
			return;
		}

		EventModel? localMutableEvent = eventRoom.LocalMutableEvent;
		if (localMutableEvent?.Node is not NFakeMerchant nFakeMerchant)
		{
			return;
		}

		ShowPotionVfx(nFakeMerchant.MerchantButton);
		FoulPotion proxy = CreateFoulPotionProxy();
		List<Task> tasks = [];
		foreach (Player player in Owner.RunState.Players)
		{
			FakeMerchant fakeMerchant = (FakeMerchant)RunManager.Instance.EventSynchronizer.GetEventForPlayer(player);
			tasks.Add(fakeMerchant.FoulPotionThrown(proxy));
		}

		await Task.WhenAll(tasks);
	}

	private FoulPotion CreateFoulPotionProxy()
	{
		FoulPotion proxy = (FoulPotion)ModelDb.Potion<FoulPotion>().ToMutable();
		proxy.Owner = Owner;
		return proxy;
	}

	private void ShowPotionVfx(NMerchantButton? merchantButton)
	{
		if (TestMode.IsOn || merchantButton == null)
		{
			return;
		}

		string scenePath = SceneHelper.GetScenePath("vfx/vfx_slime_impact");
		Node2D vfx = PreloadManager.Cache.GetScene(scenePath).Instantiate<Node2D>();
		merchantButton.GetParent().AddChildSafely(vfx);
		vfx.GlobalPosition = merchantButton.GlobalPosition;
	}
}
