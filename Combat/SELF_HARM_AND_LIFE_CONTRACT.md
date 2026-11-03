# Self-harm and Life Contract

Self-harm is a parameterized text clause, like Summon. `SelfHarmCardTemplate`
resolves the current keyword before `PlayAfterSelfHarm`. Cards with intrinsic
self-harm provide a `SelfHarm` dynamic variable. Other cards can receive it via
`SelfHarmSystem.Grant`.

Eligibility depends on currently having self-harm and having strictly more HP
than its value. Ineligible plays preserve the keyword. An eligible play costs
zero energy and removes the keyword; another grant can restore it immediately.
There is no per-combat activation limit. Combat cleanup restores intrinsic
keywords and discards granted keywords. Spent text and hover tips disappear.

Manual payments snapshot eligibility before resource-spending hooks. HP payment
uses native damage mitigation and HP-loss hooks, including temporary HP. Only
actual HP lost is credited to the summoning card's Life Contract.

`BrotherSummonCard` shares the self-harm, summon, attack and upgrade behavior of
`SummonSecondBrother` and `SummonThirdBrother`. The former is a starter card with
Slight Revision into the latter, preserving upgrade state. The latter is a token
card and does not enter ordinary rewards. Neither changes the character's
summon binding.

`LifeContractSystem.AddContribution(player, amount, companionId)` credits a
specific companion. The optional ID defaults to Second Brother for existing
callers. Accounts are isolated by player and companion and last one combat.
Each living brother can claim only its own balance once at combat end. Death
does not reset that balance; revival before combat end preserves eligibility.

`CompanionAttacks.FromCompanion` preserves the native card attack pipeline but
uses the companion as dealer, including its Strength and Weakness.
