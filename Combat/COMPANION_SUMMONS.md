# Companion summons

`CompanionSummons` redirects the shared `OstyCmd.Summon` entry point. Character
defaults are fixed: Liubei uses `second_brother`; characters without a registered
default keep vanilla Osty. Explicit summons and any-choice summons never change
this default. Osty, Second Brother and Third Brother can coexist.

## Card integration

Keep `new SummonVar(amount)` in `CanonicalVars`, so vanilla summon modifiers and
upgrade previews continue to work. Use these pairs for the two new clauses:

```csharp
// Summon[Second Brother]X
CompanionSummonKeywords.NamedCardText(CompanionSummons.SecondBrotherId, DynamicVars.Summon);
CompanionSummonKeywords.NamedHoverTip(CompanionSummons.SecondBrotherId, DynamicVars.Summon);
await CompanionSummons.SummonSpecific(choiceContext, Owner,
    CompanionSummons.SecondBrotherId, DynamicVars.Summon.BaseValue, this);

// Summon<Any>X
CompanionSummonKeywords.AnyCardText(DynamicVars.Summon);
CompanionSummonKeywords.AnyHoverTip(DynamicVars.Summon);
await CompanionSummons.SummonAny(choiceContext, Owner, DynamicVars.Summon.BaseValue, this);
```

The card-text helpers return localized `LocString` values. Insert them through
`SquKeywords.AddNestedLoc` in `AddExtraArgsToDescription`, and use the hover helpers
in `AdditionalHoverTips`. This preserves upgraded numeric previews.

The any-choice list contains Osty, the character's fixed default, and successful
explicit summons by that player in this combat. Entries are deduplicated and
cleared after combat; other players' discoveries are separate. Selection uses the
native synchronized card grid, which supports more than three candidates.

## Extending the registry

Register a `MonsterModel` with RitsuLib, then register its summon definition before
use. This does not put it into any character's default or any-choice list:

```csharp
CompanionSummons.Register<MyPet>("my_pet", "res://images/cards/MyPet.png");
CompanionSummons.RegisterDefault<MyCharacter>("my_pet");
```

Custom summons run `ModifySummonAmount` once and retain native summon history and
`AfterSummon` hooks. Existing living pets gain max HP and heal by the summon amount;
dead pets are revived at the new summon amount instead of duplicated. Zero or
prevented summons do not discover candidates or emit successful-summon hooks.

Second Brother and Third Brother use vanilla Osty's visuals, animator, size scaling, pet layout,
health bar and status display, and native `DieForYouPower` to
absorb its owner's unblocked powered attacks. It has no automatic attack. Methods
which explicitly command `Player.Osty` to attack remain Osty-specific; this system
redirects summoning, not every Osty-specific card effect or animation.

Third Brother is a separate registered monster with the same current native model,
guard and Life Contract abilities. Its explicit summons use `ThirdBrotherId` and
add it to that owner's any-choice list without changing the default binding.
