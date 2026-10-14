using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Cards;
using Squ.Interop;
using TestTheSpire;
using Xunit;

namespace Squ.Tests.JointMods;

public sealed class NewsanguoJointModTests : SunqianCombatTestSuite
{
    private static readonly ModelId CaoWeiCharacterId =
        new("CHARACTER", "NEWSANGUO_CHARACTER_CAO_WEI_CHARACTER");

    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        CharacterModel caoWei = ModelDb.GetById<CharacterModel>(CaoWeiCharacterId);
        battle
            .Player(caoWei)
            .AddEnemy<BigDummy>()
            .WithSeed("joint-newsanguo-wine");
    }

    [Fact]
    public async Task Wine_grants_real_Newsanguo_drunken_might_instead_of_vigor()
    {
        Assert.True(NewsanguoPublicApiInterop.IsReady);
        Assert.True(NewsanguoPublicApiInterop.IsNewsanguoCharacter(Player.Character));
        var card = await AddToHand<Wine>();

        await Play(card);

        Assert.Equal(Wine.BaseVigor, NewsanguoPublicApiInterop.GetDrunkenMight(Player));
        Assert.Null(Player.Creature.GetPower<VigorPower>());
    }
}
