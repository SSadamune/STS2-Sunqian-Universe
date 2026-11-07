#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Squ.Character;
using Squ.Combat;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
namespace Squ.Cards;

[RegisterCard(typeof(LiubeiCardPool), StableEntryStem = "abduct_zilong")]
public sealed class AbductZilong : ModCardTemplate
{
    public AbductZilong() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }
    protected override HashSet<CardTag> CanonicalTags => [SquCardTags.Dragon];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Righteousness", 3)];
    public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/prisoner/AbductZilong.png");
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [SquKeywords.CreateRighteousnessHoverTip(DynamicVars["Righteousness"]), HoverTipFactory.FromKeyword(SquKeywords.VariousStrikeCards)];
    protected override void AddExtraArgsToDescription(LocString description) =>
        description.Add("EnchantmentQuality", new LocString("cards", Id.Entry + (IsUpgraded ? ".strongEnchantment" : ".randomEnchantment")));
    internal IEnumerable<MegaCrit.Sts2.Core.Models.CardModel> EligibleStrikes => CharacterCardPoolMembership.GetUnlockedCards(Owner,
        CharacterCardPoolMembership.GetCharacterPools(Owner).Where(pool => pool.Id != Owner.Character.CardPool.Id))
        .Where(card => card.Type == CardType.Attack && card.Tags.Contains(CardTag.Strike)).DistinctBy(card => card.Id);
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await PowerCmd.Apply<RighteousnessPower>(context, Owner.Creature, DynamicVars["Righteousness"].BaseValue, Owner.Creature, this);
        var rng = Owner.RunState.Rng.CombatCardGeneration;
        var options = CardFactory.GetDistinctForCombat(Owner, EligibleStrikes, 3, rng).ToList();
        foreach (var option in options)
            AttackEnchantmentPool.ApplySpec(option, ScriptPartTimeGeneralPower.RollEnchantment(option, IsUpgraded, rng));
        if (options.Count == 0) return;
        var selected = await CardSelectCmd.FromChooseACardScreen(context, options, Owner, canSkip: false);
        if (selected != null)
        {
            var result = await CardPileCmd.AddGeneratedCardToCombat(selected, PileType.Hand, Owner);
            if (result.success) EnchantedStrikeRewards.Record(result.cardAdded);
        }
        foreach (var option in options)
            if (option != selected) option.RemoveFromState();
    }
    protected override void OnUpgrade() => DynamicVars["Righteousness"].UpgradeValueBy(1);
}
