# Self-harm and Life Contract

Self-harm is a parameterized text clause, like Summon. Derive a card from
`SelfHarmCardTemplate`, include a `SelfHarm` dynamic variable, and expose
`SquKeywords.CreateSelfHarmHoverTip` in its hover tips. Put its effects in
`PlayAfterSelfHarm`.

Use `{SelfHarmText}` in the localized card description. The template renders the
clause normally before activation and in gray after self-harm has triggered for
that copy. Use `AddExtraSelfHarmArgs` for other description arguments; the base
template owns `AddExtraArgsToDescription` so spent-clause formatting is shared.

Each physical card gets one first-play opportunity per combat. It is eligible
only if the owner's current HP is strictly greater than the configured loss.
An ineligible first play still uses that opportunity. Replays do not pay again;
independently generated copies have independent opportunities. Auto-play also
counts as playing the card and resolves its first-play HP payment if eligible.

The payable cost is zero and uses the native decreased-cost green. The base cost
and other local cost modifiers are preserved. Manual payments snapshot the
eligibility before resource-spending hooks, so those hooks cannot grant an
unpaid free play or charge both resources. HP loss goes through native damage
mitigation and HP-loss hooks. The callback receives only actual HP lost by the
owner, including temporary HP.

`AwakenSecondBrother.AfterSelfHarmPaid` credits that actual loss through
`LifeContractSystem.AddContribution(player, amount)`. Future sources can call
the same API after determining their actual loss. Unrelated HP losses are not
automatically credited. Balances are player-scoped and last one combat.

Second Brother receives `LifeContractPower` when summoned. Its combat hover shows
the current balance. If it is alive at combat end, the balance heals its owner
once, subject to normal healing limits. Death does not reset the balance; a
Second Brother revived before combat ends can fulfill the same contract.

`CompanionAttacks.FromCompanion` preserves the native card attack pipeline but
sets the companion as the damage dealer, so its Strength and Weakness apply
rather than the owner's.
