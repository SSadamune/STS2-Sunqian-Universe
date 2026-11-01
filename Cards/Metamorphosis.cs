#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using Squ.Audio;
using Squ.Character;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "metamorphosis")]
public sealed class Metamorphosis : ModCardTemplate
{
	public const int ChoiceCount = 3;
	public const int EnergyGain = 1;

	private static IMetamorphosisCandidateProvider _candidateProvider =
		new DefaultMetamorphosisCandidateProvider();

	/// <summary>
	/// The strategy used to construct the card choices. Kept replaceable so the temporary
	/// heuristic can later become a deck/hand-aware implementation.
	/// </summary>
	public static IMetamorphosisCandidateProvider CandidateProvider
	{
		get => _candidateProvider;
		set => _candidateProvider = value ?? throw new ArgumentNullException(nameof(value));
	}

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.ForEnergy(this),
		new HoverTip(
			new LocString("cards", Id.Entry + ".potentiallyUsefulTitle"),
			new LocString("cards", Id.Entry + ".potentiallyUsefulDescription")),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/Metamorphosis.png");

	public Metamorphosis()
		: base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		SquSfx.Play(SquSfx.MetamorphosisPlayEvent);

		List<MetamorphosisCandidate> candidates =
			CandidateProvider.BuildChoices(this, ChoiceCount).ToList();
		if (candidates.Count == 0)
		{
			return;
		}

		List<CardModel> choices = candidates.Select(candidate => candidate.Card).ToList();

		if (IsUpgraded)
		{
			foreach (CardModel choice in choices.Where(choice => choice.IsUpgradable))
			{
				choice.UpgradeInternal();
				choice.FinalizeUpgradeInternal();
			}
		}

		CardModel? selected = await CardSelectCmd.FromChooseACardScreen(
			choiceContext,
			choices,
			Owner,
			canSkip: false);
		selected ??= choices[0];
		MetamorphosisCandidate selectedCandidate = candidates.First(candidate =>
			ReferenceEquals(candidate.Card, selected) || candidate.Card.Id == selected.Id);

		await CardPileCmd.AddGeneratedCardToCombat(selected, PileType.Hand, Owner);
		await RecordSelectedCharacter(choiceContext, selectedCandidate.CharacterPool);
	}

	private async Task RecordSelectedCharacter(
		PlayerChoiceContext choiceContext,
		CardPoolModel? selectedPool)
	{
		if (selectedPool is null || !MetamorphosisCharacterCatalog.IsSupported(selectedPool))
		{
			// Once every supported character is recorded, only colorless cards are offered;
			// later plays deliberately leave the power unchanged.
			return;
		}

		MetamorphosisPower? existing = Owner.Creature.GetPower<MetamorphosisPower>();
		if (existing is null)
		{
			MetamorphosisPower created =
				(MetamorphosisPower)ModelDb.Power<MetamorphosisPower>().ToMutable();
			created.Record(selectedPool);
			await PowerCmd.Apply(
				choiceContext,
				created,
				Owner.Creature,
				1,
				Owner.Creature,
				this);
			Owner.Creature.GetPower<MetamorphosisPower>()
				?.SyncAmountAndTurnHistory();
			return;
		}

		if (!existing.Record(selectedPool))
		{
			return;
		}

		await PowerCmd.ModifyAmount(
			choiceContext,
			existing,
			1,
			Owner.Creature,
			this);
		existing.SyncAmountAndTurnHistory();
	}
}
