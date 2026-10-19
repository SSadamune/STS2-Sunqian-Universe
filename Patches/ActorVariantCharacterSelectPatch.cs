#nullable enable
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.addons.mega_text;
using Squ.Character;
using Squ.Settings;
using STS2RitsuLib.Data;

namespace Squ.Patches;

/// <summary>
/// 将孙乾与刘备保留为两个真实角色模型，但在原版选角界面中合并到孙乾的同一个顶层标签。
/// </summary>
[HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.SelectCharacter))]
internal static class ActorVariantCharacterSelectPatch
{
	private const string UiName = "SquActorVariantSelector";
	private const string SunqianVariant = "sunqian";
	private const string LiubeiVariant = "liubei";
	private const float TabStripWidth = 360f;
	private const float TabButtonWidth = 170f;
	private const float TabHeight = 56f;
	private const float TabSeparation = 20f;
	private const float TabTopOffset = -68f;
	private const float ArrowWidth = 96f;
	private const float ArrowHeight = 117f;
	// InfoPanel 的 NinePatchRect 会在控件边界外再延伸约 87px；箭头需要越过这段视觉边缘。
	private const float ArrowHorizontalGap = 99f;
	private const int DuplicateWithoutSignals = 2 | 4 | 8;

	[ThreadStatic]
	private static CharacterModel? _pendingPreferredVariant;

	private sealed class UiState(
		Control root,
		NCharacterSelectButton anchor,
		Button sunqianButton,
		Button liubeiButton,
		NButton leftArrow,
		NButton rightArrow)
	{
		public Control Root { get; } = root;
		public NCharacterSelectButton Anchor { get; } = anchor;
		public Button SunqianButton { get; } = sunqianButton;
		public Button LiubeiButton { get; } = liubeiButton;
		public NButton LeftArrow { get; } = leftArrow;
		public NButton RightArrow { get; } = rightArrow;
		public bool IsSunqianSelected { get; set; }
	}

	private static readonly ConditionalWeakTable<NCharacterSelectScreen, UiState> UiStates = new();

	/// <summary>
	/// 进入“龙套演员”标签时先让原版加载孙乾背景；若上次使用刘备，则在原版流程结束后
	/// 只替换角色资料与大厅角色，不再次触碰背景节点。
	/// </summary>
	[HarmonyPrefix]
	[HarmonyPriority(Priority.First)]
	private static void SelectPreferredVariant(
		NCharacterSelectButton charSelectButton,
		CharacterModel characterModel)
	{
		_pendingPreferredVariant = null;

		if (charSelectButton.Character is SunqianCharacter && characterModel is SunqianCharacter)
		{
			CharacterModel preferred = GetPreferredCharacter();
			if (preferred is LiubeiCharacter)
			{
				_pendingPreferredVariant = preferred;
			}
		}
	}

	[HarmonyPostfix]
	private static void RefreshVariantSelector(
		NCharacterSelectScreen __instance,
		NCharacterSelectButton charSelectButton,
		CharacterModel characterModel)
	{
		CharacterModel selectedCharacter = characterModel;
		if (_pendingPreferredVariant is LiubeiCharacter pending
			&& charSelectButton.Character is SunqianCharacter
			&& characterModel is SunqianCharacter)
		{
			_pendingPreferredVariant = null;
			ApplyVariantWithoutChangingBackground(__instance, pending);
			selectedCharacter = pending;
		}

		if (selectedCharacter is not (SunqianCharacter or LiubeiCharacter))
		{
			Remove(__instance);
			return;
		}

		NCharacterSelectButton? anchor = charSelectButton.Character is SunqianCharacter
			? charSelectButton
			: FindSunqianButton(__instance);
		if (anchor is null)
		{
			SquMod.Logger.Warn("[CharacterSelect] Could not find the Supporting Actor selection button.");
			return;
		}

		SavePreferredCharacter(selectedCharacter);
		UiState state = EnsureSelector(__instance, anchor);
		RefreshAnchorIcon(state.Anchor, selectedCharacter);
		RefreshButtons(state, selectedCharacter);
	}

	internal static void Remove(NCharacterSelectScreen screen)
	{
		if (!UiStates.TryGetValue(screen, out UiState? state))
		{
			return;
		}

		if (GodotObject.IsInstanceValid(state.Anchor))
		{
			NodePath selfPath = state.Anchor.GetPath();
			state.Anchor.FocusNeighborTop = selfPath;
			state.Anchor.FocusNeighborBottom = selfPath;
		}

		if (GodotObject.IsInstanceValid(state.Root))
		{
			state.Root.QueueFree();
		}
		UiStates.Remove(screen);
	}

	private static UiState EnsureSelector(
		NCharacterSelectScreen screen,
		NCharacterSelectButton anchor)
	{
		if (UiStates.TryGetValue(screen, out UiState? existing)
			&& GodotObject.IsInstanceValid(existing.Root)
			&& existing.Anchor == anchor)
		{
			return existing;
		}

		Remove(screen);

		Control infoPanel = Traverse.Create(screen).Field<Control>("_infoPanel").Value;
		NAscensionPanel ascensionPanel = Traverse.Create(screen).Field<NAscensionPanel>("_ascensionPanel").Value;
		Control root = new()
		{
			Name = UiName,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			ZIndex = 20,
		};
		infoPanel.AddChild(root);
		root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

		HBoxContainer row = new()
		{
			AnchorLeft = 0.5f,
			AnchorRight = 0.5f,
			OffsetLeft = -TabStripWidth / 2f,
			OffsetTop = TabTopOffset,
			OffsetRight = TabStripWidth / 2f,
			OffsetBottom = TabTopOffset + TabHeight,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		row.AddThemeConstantOverride("separation", (int)TabSeparation);
		root.AddChild(row);

		Button sunqianButton = CreateVariantButton("SunqianVariantButton");
		Button liubeiButton = CreateVariantButton("LiubeiVariantButton");
		row.AddChild(sunqianButton);
		row.AddChild(liubeiButton);

		NButton leftArrow = CloneNativeArrow(
			ascensionPanel.GetNode<NButton>("HBoxContainer/LeftArrowContainer/LeftArrow"),
			"PreviousActorVariantButton");
		NButton rightArrow = CloneNativeArrow(
			ascensionPanel.GetNode<NButton>("HBoxContainer/RightArrowContainer/RightArrow"),
			"NextActorVariantButton");
		root.AddChild(leftArrow);
		root.AddChild(rightArrow);
		PositionArrow(leftArrow, isLeft: true);
		PositionArrow(rightArrow, isLeft: false);

		sunqianButton.Pressed += () => SelectVariant(screen, anchor, ModelDb.Character<SunqianCharacter>());
		liubeiButton.Pressed += () => SelectVariant(screen, anchor, ModelDb.Character<LiubeiCharacter>());
		leftArrow.Connect(
			NClickableControl.SignalName.Released,
			Callable.From<NButton>(_ => SelectAdjacentVariant(screen, anchor)));
		rightArrow.Connect(
			NClickableControl.SignalName.Released,
			Callable.From<NButton>(_ => SelectAdjacentVariant(screen, anchor)));

		NodePath anchorPath = anchor.GetPath();
		NodePath sunqianPath = sunqianButton.GetPath();
		NodePath liubeiPath = liubeiButton.GetPath();
		NodePath leftArrowPath = leftArrow.GetPath();
		NodePath rightArrowPath = rightArrow.GetPath();
		sunqianButton.FocusNeighborLeft = leftArrowPath;
		sunqianButton.FocusNeighborRight = liubeiPath;
		liubeiButton.FocusNeighborLeft = sunqianPath;
		liubeiButton.FocusNeighborRight = rightArrowPath;
		sunqianButton.FocusNeighborTop = sunqianPath;
		liubeiButton.FocusNeighborTop = liubeiPath;
		sunqianButton.FocusNeighborBottom = anchorPath;
		liubeiButton.FocusNeighborBottom = anchorPath;
		leftArrow.FocusNeighborLeft = leftArrowPath;
		leftArrow.FocusNeighborRight = sunqianPath;
		leftArrow.FocusNeighborTop = sunqianPath;
		leftArrow.FocusNeighborBottom = anchorPath;
		rightArrow.FocusNeighborLeft = liubeiPath;
		rightArrow.FocusNeighborRight = rightArrowPath;
		rightArrow.FocusNeighborTop = liubeiPath;
		rightArrow.FocusNeighborBottom = anchorPath;

		UiState state = new(root, anchor, sunqianButton, liubeiButton, leftArrow, rightArrow);
		UiStates.Add(screen, state);
		return state;
	}

	private static Button CreateVariantButton(string name)
	{
		Button button = new()
		{
			Name = name,
			CustomMinimumSize = new Vector2(TabButtonWidth, TabHeight),
			FocusMode = Control.FocusModeEnum.All,
			MouseFilter = Control.MouseFilterEnum.Stop,
		};
		button.AddThemeFontSizeOverride("font_size", 24);
		return button;
	}

	private static NButton CloneNativeArrow(NButton source, string name)
	{
		NButton arrow = (NButton)source.Duplicate(DuplicateWithoutSignals);
		arrow.Name = name;
		// 难度达到上限或下限时，原生箭头会被隐藏；角色切换箭头不能继承这个运行时状态。
		arrow.Visible = true;
		arrow.Modulate = Colors.White;
		arrow.SelfModulate = Colors.White;
		TextureRect icon = arrow.GetNode<TextureRect>("TextureRect");
		if (icon.Material is Material material)
		{
			icon.Material = (Material)material.Duplicate();
		}
		return arrow;
	}

	private static void PositionArrow(Control arrow, bool isLeft)
	{
		float anchor = isLeft ? 0f : 1f;
		float left = isLeft ? -ArrowHorizontalGap - ArrowWidth : ArrowHorizontalGap;
		float right = isLeft ? -ArrowHorizontalGap : ArrowHorizontalGap + ArrowWidth;
		arrow.AnchorLeft = anchor;
		arrow.AnchorTop = 0.5f;
		arrow.AnchorRight = anchor;
		arrow.AnchorBottom = 0.5f;
		arrow.OffsetLeft = left;
		arrow.OffsetTop = -ArrowHeight / 2f;
		arrow.OffsetRight = right;
		arrow.OffsetBottom = ArrowHeight / 2f;
	}

	private static void SelectAdjacentVariant(
		NCharacterSelectScreen screen,
		NCharacterSelectButton anchor)
	{
		if (!UiStates.TryGetValue(screen, out UiState? state))
		{
			return;
		}

		CharacterModel character = state.IsSunqianSelected
			? ModelDb.Character<LiubeiCharacter>()
			: ModelDb.Character<SunqianCharacter>();
		SelectVariant(screen, anchor, character);
	}

	private static void SelectVariant(
		NCharacterSelectScreen screen,
		NCharacterSelectButton anchor,
		CharacterModel character)
	{
		if (!GodotObject.IsInstanceValid(screen)
			|| !GodotObject.IsInstanceValid(anchor)
			|| anchor.IsLocked)
		{
			return;
		}

		ApplyVariantWithoutChangingBackground(screen, character);
		SavePreferredCharacter(character);
		if (UiStates.TryGetValue(screen, out UiState? state))
		{
			RefreshAnchorIcon(state.Anchor, character);
			RefreshButtons(state, character);
		}
	}

	private static void RefreshAnchorIcon(
		NCharacterSelectButton anchor,
		CharacterModel character)
	{
		TextureRect? icon = Traverse.Create(anchor).Field<TextureRect>("_icon").Value;
		if (icon is not null && GodotObject.IsInstanceValid(icon))
		{
			icon.Texture = character.CharacterSelectIcon;
		}
	}

	/// <summary>
	/// 同一选角标签内部切换版本。刻意不调用 NCharacterSelectScreen.SelectCharacter，
	/// 因为原版方法会清空并重建整个背景容器。
	/// </summary>
	private static void ApplyVariantWithoutChangingBackground(
		NCharacterSelectScreen screen,
		CharacterModel character)
	{
		Traverse fields = Traverse.Create(screen);
		MegaLabel name = fields.Field<MegaLabel>("_name").Value;
		MegaRichTextLabel description = fields.Field<MegaRichTextLabel>("_description").Value;
		MegaLabel hp = fields.Field<MegaLabel>("_hp").Value;
		MegaLabel gold = fields.Field<MegaLabel>("_gold").Value;
		MegaRichTextLabel relicTitle = fields.Field<MegaRichTextLabel>("_relicTitle").Value;
		MegaRichTextLabel relicDescription = fields.Field<MegaRichTextLabel>("_relicDescription").Value;
		TextureRect relicIcon = fields.Field<TextureRect>("_relicIcon").Value;
		TextureRect relicIconOutline = fields.Field<TextureRect>("_relicIconOutline").Value;
		NAscensionPanel ascensionPanel = fields.Field<NAscensionPanel>("_ascensionPanel").Value;

		name.SetTextAutoSize(new LocString("characters", character.CharacterSelectTitle).GetFormattedText());
		description.Text = new LocString("characters", character.CharacterSelectDesc).GetFormattedText();
		hp.SetTextAutoSize($"{character.StartingHp}/{character.StartingHp}");
		gold.SetTextAutoSize($"{character.StartingGold}");

		RelicModel relic = character.StartingRelics[0];
		relicTitle.Text = relic.Title.GetFormattedText();
		relicDescription.Text = relic.DynamicDescription.GetFormattedText();
		relicIcon.Texture = relic.Icon;
		relicIconOutline.Texture = relic.IconOutline;
		relicIcon.SelfModulate = Colors.White;
		relicIconOutline.SelfModulate = StsColors.halfTransparentBlack;

		screen.Lobby.SetLocalCharacter(character);
		if (!screen.Lobby.NetService.Type.IsMultiplayer())
		{
			ascensionPanel.AnimIn();
		}
	}

	private static void RefreshButtons(UiState state, CharacterModel selectedCharacter)
	{
		CharacterModel sunqian = ModelDb.Character<SunqianCharacter>();
		CharacterModel liubei = ModelDb.Character<LiubeiCharacter>();
		bool isSunqian = selectedCharacter is SunqianCharacter;

		state.SunqianButton.Text = FormatButtonText(sunqian, isSunqian);
		state.LiubeiButton.Text = FormatButtonText(liubei, !isSunqian);
		state.SunqianButton.SelfModulate = isSunqian ? StsColors.gold : Colors.White;
		state.LiubeiButton.SelfModulate = isSunqian ? Colors.White : StsColors.gold;
		state.IsSunqianSelected = isSunqian;
		state.Anchor.FocusNeighborTop = (isSunqian ? state.SunqianButton : state.LiubeiButton).GetPath();
	}

	private static string FormatButtonText(CharacterModel character, bool selected)
	{
		string title = new LocString("characters", character.CharacterSelectTitle).GetFormattedText();
		return selected ? $"◆ {title}" : title;
	}

	private static CharacterModel GetPreferredCharacter()
	{
		try
		{
			string preference = ModDataStore.For(SquMod.ModId)
				.Get<SquSettings>(SquSettings.DataKey)
				.PreferredActorVariant;
			if (string.Equals(preference, LiubeiVariant, StringComparison.OrdinalIgnoreCase))
			{
				return ModelDb.Character<LiubeiCharacter>();
			}
		}
		catch (Exception ex)
		{
			SquMod.Logger.Warn($"[CharacterSelect] Failed to read actor variant preference: {ex.Message}");
		}

		return ModelDb.Character<SunqianCharacter>();
	}

	private static void SavePreferredCharacter(CharacterModel character)
	{
		string preference = character is LiubeiCharacter ? LiubeiVariant : SunqianVariant;
		try
		{
			ModDataStore store = ModDataStore.For(SquMod.ModId);
			SquSettings settings = store.Get<SquSettings>(SquSettings.DataKey);
			if (string.Equals(settings.PreferredActorVariant, preference, StringComparison.Ordinal))
			{
				return;
			}

			store.Modify<SquSettings>(SquSettings.DataKey, value => value.PreferredActorVariant = preference);
			store.Save(SquSettings.DataKey);
		}
		catch (Exception ex)
		{
			SquMod.Logger.Warn($"[CharacterSelect] Failed to save actor variant preference: {ex.Message}");
		}
	}

	internal static NCharacterSelectButton? FindSunqianButton(NCharacterSelectScreen screen)
	{
		Control? container = Traverse.Create(screen).Field<Control>("_charButtonContainer").Value;
		return container?.GetChildren()
			.OfType<NCharacterSelectButton>()
			.FirstOrDefault(button => button.Character is SunqianCharacter);
	}
}

[HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.OnSubmenuClosed))]
internal static class ActorVariantCharacterSelectCleanupPatch
{
	[HarmonyPostfix]
	private static void Postfix(NCharacterSelectScreen __instance)
	{
		ActorVariantCharacterSelectPatch.Remove(__instance);
	}
}

/// <summary>
/// 原版按 CharacterModel 匹配联机选中轮廓；刘备隐藏后，将他的远端轮廓映射到孙乾的分组标签。
/// </summary>
[HarmonyPatch(typeof(NCharacterSelectScreen), "RefreshButtonSelectionForPlayer")]
internal static class ActorVariantRemoteSelectionPatch
{
	[HarmonyPostfix]
	private static void Postfix(NCharacterSelectScreen __instance, StartRunLobbyPlayer player)
	{
		if (player.character is not LiubeiCharacter)
		{
			return;
		}

		StartRunLobby? lobby = Traverse.Create(__instance).Field<StartRunLobby>("_lobby").Value;
		if (lobby is null || player.id == lobby.LocalPlayer.id)
		{
			return;
		}

		ActorVariantCharacterSelectPatch.FindSunqianButton(__instance)
			?.OnRemotePlayerSelected(player.id);
	}
}

[HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.RemotePlayerDisconnected))]
internal static class ActorVariantRemoteDisconnectPatch
{
	[HarmonyPostfix]
	private static void Postfix(NCharacterSelectScreen __instance, StartRunLobbyPlayer player)
	{
		if (player.character is LiubeiCharacter)
		{
			ActorVariantCharacterSelectPatch.FindSunqianButton(__instance)
				?.OnRemotePlayerDeselected(player.id);
		}
	}
}
