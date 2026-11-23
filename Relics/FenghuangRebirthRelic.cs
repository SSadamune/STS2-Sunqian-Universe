#nullable enable
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using Squ.Audio;

namespace Squ.Relics;

[RegisterRelic(typeof(SharedRelicPool), StableEntryStem = "fenghuang_rebirth")]
public sealed class FenghuangRebirthRelic : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Shop;
    public override Task AfterObtained()
    {
        SquSfx.Play(SquSfx.FenghuangRebirthObtainedEvent);
        return Task.CompletedTask;
    }
    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://images/relics/FenghuangRebirthRelic.png",
        IconOutlinePath: "res://images/relics/FenghuangRebirthRelicOutline.png",
        BigIconPath: "res://images/relics/FenghuangRebirthRelicBig.png");

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner) return;
        Flash();
        SquSfx.PlayRandom(Owner.RunState, SquSfx.FenghuangRebirthLubuEvent, SquSfx.FenghuangRebirthDiaochanEvent);
        await CreatureCmd.Damage(choiceContext, Owner.Creature, 1,
            ValueProp.Unblockable | ValueProp.Unpowered, Owner.Creature);
        await CreatureCmd.GainBlock(Owner.Creature, 7, ValueProp.Unpowered, null);
    }
}
