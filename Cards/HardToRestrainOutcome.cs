#nullable enable
using System;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Cards;

/// <summary>Informational cards, never played or added to a pile.</summary>
public abstract class HardToRestrainOutcome : ModCardTemplate
{
    protected HardToRestrainOutcome() : base(0, CardType.Skill, CardRarity.Token,
        TargetType.Self, showInCardLibrary: false) { }
    public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/PrisonerPlaceholder.png");
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) => Task.CompletedTask;

    internal static CardModel Create(ICombatState combat, Player player, HardToRestrainPower.Effect effect) => effect switch
    {
        HardToRestrainPower.Effect.Attack => combat.CreateCard<HardToRestrainAttack>(player),
        HardToRestrainPower.Effect.Burn => combat.CreateCard<HardToRestrainBurn>(player),
        HardToRestrainPower.Effect.StopLifeLoss => combat.CreateCard<HardToRestrainStopLifeLoss>(player),
        HardToRestrainPower.Effect.ConvertBlock => combat.CreateCard<HardToRestrainConvertBlock>(player),
        HardToRestrainPower.Effect.Wine => combat.CreateCard<HardToRestrainWine>(player),
        _ => throw new ArgumentOutOfRangeException(nameof(effect), effect, null)
    };

    internal static async Task Show(ICombatState combat, Player player, HardToRestrainPower.Effect effect)
    {
        if (TestMode.IsOn || NCombatRoom.Instance?.Ui.CardPreviewContainer is not { } container) return;
        var node = NCard.Create(Create(combat, player, effect));
        if (node == null) return;
        container.AddChild(node);
        try
        {
            node.UpdateVisuals(PileType.Play, CardPreviewMode.Normal);
            // Keep the informational beat at half a second even with instant animations enabled.
            await node.ToSignal(node.GetTree().CreateTimer(0.5), SceneTreeTimer.SignalName.Timeout);
        }
        finally
        {
            container.RemoveChild(node);
            node.QueueFree();
        }
    }
}

[RegisterCard(typeof(TokenCardPool), StableEntryStem = "hard_to_restrain_attack")]
public sealed class HardToRestrainAttack : HardToRestrainOutcome { }
[RegisterCard(typeof(TokenCardPool), StableEntryStem = "hard_to_restrain_burn")]
public sealed class HardToRestrainBurn : HardToRestrainOutcome { }
[RegisterCard(typeof(TokenCardPool), StableEntryStem = "hard_to_restrain_stop_life_loss")]
public sealed class HardToRestrainStopLifeLoss : HardToRestrainOutcome { }
[RegisterCard(typeof(TokenCardPool), StableEntryStem = "hard_to_restrain_convert_block")]
public sealed class HardToRestrainConvertBlock : HardToRestrainOutcome { }
[RegisterCard(typeof(TokenCardPool), StableEntryStem = "hard_to_restrain_wine")]
public sealed class HardToRestrainWine : HardToRestrainOutcome { }
