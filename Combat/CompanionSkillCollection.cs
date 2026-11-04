#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Cards;
using Squ.Powers;
using STS2RitsuLib.CardPiles;
using STS2RitsuLib.Networking.ManagedActions;
namespace Squ.Combat;

public static class CompanionSkillCollection
{
    public static PileType Pile { get; private set; }
    private sealed record Activation(string CompanionId, int Turn);
    private static readonly HashSet<Player> Selecting = [];
    private static readonly RitsuLibManagedNetActionDescriptor<Activation> ActivateAction = new(
        SquMod.ModId, "shared_life_summon", value => JsonSerializer.SerializeToUtf8Bytes(value),
        bytes => JsonSerializer.Deserialize<Activation>(bytes)!,
        async context =>
        {
            if (context.Player.PlayerCombatState?.TurnNumber == context.Message.Turn)
                await Activate(context.PlayerChoiceContext, context.Player, context.Message.CompanionId);
        }, GameActionType.CombatPlayPhaseOnly);

    public static void Register()
    {
        Pile = ModCardPileRegistry.For(SquMod.ModId).RegisterOwned("companion_skills", new ModCardPileSpec
        {
            Scope = ModCardPileScope.CombatOnly, Style = ModCardPileUiStyle.BottomRight,
            Anchor = new ModCardPileAnchor(ModCardPileAnchorKind.BottomRightPrimary, new Vector2(100, -210)),
            IconPath = "res://images/powers/LifeContractPowerBig.png",
            VisibleWhen = context => context.Pile?.Cards.Count > 0,
            OnOpen = context =>
            {
                if (CanOpenActivation(context.Player)) _ = TaskHelper.RunSafely(Open(context.Player));
                else context.ShowDefaultPileScreen();
            }
        }).PileType;
        RitsuLibManagedNetActions.Register(ActivateAction);
    }

    internal static async Task Record(Player player, string id, PowerModel power)
    {
        string? skill = power switch
        {
            MakeHolesPower => "MakeHoles",
            SharedLifePower => "SharedLife", HardToRestrainPower => "HardToRestrain",
            SpareTheYoungAndOldPower => "SpareTheYoungAndOld", _ => null
        };
        if (skill == null || Pile.GetPile(player).Cards.OfType<CompanionSkillRecord>()
            .Any(card => card.CompanionId == id && card.Skill == skill)) return;
        var record = player.Creature.CombatState!.CreateCard<CompanionSkillRecord>(player);
        record.CompanionId = id;
        record.Skill = skill;
        await CardPileCmd.Add(record, Pile);
    }

    private static bool CanOpenActivation(Player player) => !Selecting.Contains(player)
        && Pile.GetPile(player).Cards.OfType<CompanionSkillRecord>().Any(card => CanActivate(player, card.CompanionId));

    internal static bool CanActivate(Player player, string id) => player.Creature.IsAlive
        && CombatManager.Instance.IsInProgress && !CombatManager.Instance.IsOverOrEnding
        && CombatManager.Instance.IsPartOfPlayerTurn(player)
        && CompanionSummons.GetCompanion(player, id) is not { IsAlive: true }
        && Pile.GetPile(player).Cards.OfType<CompanionSkillRecord>().Any(card => card.CompanionId == id && card.Skill == "SharedLife")
        && CompanionSummons.GetCompanion(player, id)?.GetPower<SharedLifePower>() is { IsActive: true };

    internal static async Task<bool> Activate(PlayerChoiceContext context, Player player, string id)
    {
        if (!CanActivate(player, id)) return false;
        var hits = await CreatureCmd.Damage(context, player.Creature, 8,
            ValueProp.Unblockable | ValueProp.Unpowered, null, null);
        SharedLifeSystem.AddContribution(player, hits.Where(hit => hit.Receiver == player.Creature).Sum(hit => hit.UnblockedDamage), id);
        if (player.Creature.IsAlive)
            await CompanionSummons.SummonSpecific(context, player, id, 8, null);
        return true;
    }

    internal static async Task Open(Player player)
    {
        if (!Selecting.Add(player)) return;
        try
        {
            var pile = Pile.GetPile(player);
            var prefs = new CardSelectorPrefs(new LocString("cards", "SUNQIAN_UNIVERSE_CARD_COMPANION_SKILL_RECORD.selectionPrompt"), 0, 1)
            { Cancelable = true, RequireManualConfirmation = true, PretendCardsCanBePlayed = true };
            IEnumerable<CardModel> selected;
            if ((CardSelectCmd.Selector ?? CardSelectCmd.LocalSelector) is { } selector)
                selected = await selector.GetSelectedCards(pile.Cards, 0, 1);
            else
            {
                NPlayerHand.Instance?.CancelAllCardPlay();
                var screen = NCombatPileCardSelectScreen.Create(pile, prefs, card => card is CompanionSkillRecord);
                NOverlayStack.Instance!.Push(screen);
                selected = await screen.CardsSelected();
            }
            if (selected.OfType<CompanionSkillRecord>().SingleOrDefault() is { Skill: "SharedLife" } record
                && CanActivate(player, record.CompanionId))
                RitsuLibManagedNetActions.Request(RunManager.Instance, ActivateAction,
                    new Activation(record.CompanionId, player.PlayerCombatState!.TurnNumber), player.NetId);
        }
        finally { Selecting.Remove(player); }
    }
}
