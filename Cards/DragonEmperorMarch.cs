#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using Squ.Character;
using Squ.Combat;
using Squ.Powers;
using Squ.Script;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
namespace Squ.Cards;

[RegisterCard(typeof(LiubeiCardPool), StableEntryStem = "dragon_emperor_march")]
public sealed class DragonEmperorMarch : ModCardTemplate
{
    public DragonEmperorMarch() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self) { }
    protected override HashSet<CardTag> CanonicalTags => [SquCardTags.Dragon];
    public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/prisoner/DragonEmperorMarch.png");
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [SquCardHoverTipFactory.FromCard<EmperorsMarch>(this, IsUpgraded)];
    protected override void AddExtraArgsToDescription(LocString description) =>
        description.Add("RewardCard", GeneratedCombatCards.GetDisplayTitle<EmperorsMarch>(IsUpgraded));
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var power = await PowerCmd.Apply<DragonEmperorMarchPower>(context, Owner.Creature, 1, Owner.Creature, this);
        power?.AddSource(IsUpgraded);
    }
    protected override void OnUpgrade() { }
}
