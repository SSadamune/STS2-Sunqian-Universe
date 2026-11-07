#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Character;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
namespace Squ.Cards;

[RegisterCard(typeof(LiubeiCardPool), StableEntryStem = "swordsmanship_review")]
public sealed class SwordsmanshipReview : ModCardTemplate
{
    public SwordsmanshipReview() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(7, ValueProp.Move)];
    public override bool GainsBlock => true;
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromKeyword(SquKeywords.VariousStrikeCards)];
    public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/prisoner/SwordsmanshipReview.png");
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
        var pile = PileType.Discard.GetPile(Owner);
        if (!pile.Cards.Any(card => card.Tags.Contains(CardTag.Strike))) return;
        var prefs = new CardSelectorPrefs(new LocString("cards", Id.Entry + ".selectionPrompt"), 1)
        { Cancelable = false, RequireManualConfirmation = true };
        var selected = (await CardSelectCmd.FromCombatPile(context, pile, Owner, prefs,
            card => card.Tags.Contains(CardTag.Strike))).SingleOrDefault();
        if (selected?.Pile?.Type != PileType.Discard) return;
        if (selected.IsUpgradable) CardCmd.Upgrade(selected);
        await CardPileCmd.Add(selected, PileType.Draw, CardPilePosition.Top);
    }
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3);
}
