# Self-harm and Shared Life

Self-harm is a parameterized clause, like Summon. `SelfHarmCardTemplate`
resolves intrinsic self-harm before `PlayAfterSelfHarm`. Other cards can receive
it through `SelfHarmSystem.Grant`.

Eligibility requires strictly more current HP than the self-harm amount. An
eligible play costs zero energy and removes the keyword. Ineligible plays spend
energy and preserve it. Combat cleanup clears granted self-harm. These payments
are unrelated to Shared Life unless a source explicitly credits the companion.

`SoulContract` replaces the two brother summon cards in Liu Bei's starting deck.
It costs one energy, offers a mandatory two-card choice, summons the chosen
brother for six, and unlocks Shared Life. Its upgrade adds Innate.

`CompanionSkills` keeps per-player, per-companion unlock order and restores
skills after summons. All unlocks create unique records in the combat-only
`CompanionSkillCollection` pile. Skills cannot upgrade; their max-HP activation
thresholds remain 0, 10, 20, etc. Ordinary summons do not unlock Shared Life.

The skill collection is inspectable at any time. When Shared Life can be used,
its button opens a cancelable selection screen. Confirmed activation is sent
through a managed combat action, with turn, ownership, unlock, skill activation,
and companion absence checked again during execution. Browsing and cancellation
have no gameplay effects. The activation loses eight HP, records the actual HP
lost, and summons the absent companion for eight if the player survives.

`SharedLifeSystem.AddContribution(player, amount, companionId)` records actual
HP lost for that companion, including Unstable Factor's losses for Third Brother.
Records are isolated by combat, player, and companion and survive pet death.
An active Shared Life heals once at combat end, capped by the companion's
remaining HP and that account's contributions. Generic previews use pronouns;
unlocked cards and powers show the companion's name and current healing cap.

`CompanionAttacks.FromCompanion` keeps the native card attack pipeline while
using the companion's Strength and Weakness.
