#nullable enable
using System;
using System.Linq;
using Godot;
using STS2RitsuLib;
using STS2RitsuLib.Data;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Ui.Shell.Theme;
using STS2RitsuLib.Utils.Persistence;

namespace Squ.Settings;

/// <summary>
/// 注册游戏内「设置 → Mod 设置」中的孙乾宇宙页面。
/// </summary>
public static class SquSettingsPage
{
	private static readonly IModSettingsValueBinding<int> SfxVolumePercentBinding =
		new DefaultModSettingsValueBinding<int>(
			new ModSettingsValueBinding<SquSettings, int>(
				SquMod.ModId,
				SquSettings.DataKey,
				SaveScope.Global,
				static s => Math.Clamp(s.SfxVolumePercent, SquSettings.MinSfxVolumePercent, SquSettings.MaxSfxVolumePercent),
				static (s, v) =>
				{
					s.SfxVolumePercent = Math.Clamp(
						v,
						SquSettings.MinSfxVolumePercent,
						SquSettings.MaxSfxVolumePercent);
					SquSfxPreview.PlaySelected();
				}),
			static () => SquSettings.DefaultSfxVolumePercent);

	private static readonly IModSettingsValueBinding<string> PreviewSfxBinding =
		new DefaultModSettingsValueBinding<string>(
			new ModSettingsValueBinding<SquSettings, string>(
				SquMod.ModId,
				SquSettings.DataKey,
				SaveScope.Global,
				static s => SquSfxPreview.NormalizeId(s.PreviewSfxId),
				static (s, v) =>
				{
					s.PreviewSfxId = SquSfxPreview.NormalizeId(v);
					SquSfxPreview.PlaySelected();
				}),
			static () => SquSfxPreview.DefaultId);

	public static void Register()
	{
		ModDataStore.For(SquMod.ModId).Register<SquSettings>(
			key: SquSettings.DataKey,
			fileName: SquSettings.FileName,
			scope: SaveScope.Global,
			defaultFactory: () => new SquSettings(),
			autoCreateIfMissing: true);

		RitsuLibFramework.RegisterModSettings(SquMod.ModId, page =>
		{
			page.WithModDisplayName(Loc("SUNQIAN_UNIVERSE_COMMON.SETTINGS.modDisplayName", "孙乾宇宙"))
				.WithTitle(Loc("SUNQIAN_UNIVERSE_COMMON.SETTINGS.pageTitle", "音效"))
				.WithDescription(Loc(
					"SUNQIAN_UNIVERSE_COMMON.SETTINGS.pageDescription",
					"调整本模组音效音量，叠加在游戏「音效」音量之上。"))
				.WithVisibleOnHostSurfaces(
					ModSettingsHostSurface.MainMenu
					| ModSettingsHostSurface.RunPause
					| ModSettingsHostSurface.CombatPause)
				.AddSection("sfx", section =>
				{
					section.WithTitle(Loc("SUNQIAN_UNIVERSE_COMMON.SETTINGS.sectionSfxTitle", "音效音量"))
						.AddIntSlider(
							id: "sfx_volume_percent",
							label: Loc("SUNQIAN_UNIVERSE_COMMON.SETTINGS.sfxVolumeLabel", "模组音效音量"),
							binding: SfxVolumePercentBinding,
							minValue: SquSettings.MinSfxVolumePercent,
							maxValue: SquSettings.MaxSfxVolumePercent,
							step: 5,
							valueFormatter: static v => $"{v}%")
						.AddCustom(
							"sfx_preview_clip",
							Loc("SUNQIAN_UNIVERSE_COMMON.SETTINGS.previewSfxLabel", "试听音效"),
							CreatePreviewSfxRow);
				});
		});
	}

	private static Control CreatePreviewSfxRow(IModSettingsUiActionHost host)
	{
		HBoxContainer row = new() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		row.AddThemeConstantOverride("separation", 10);

		var title = ModSettingsUiFactory.CreateHeaderLabel(
			Loc("SUNQIAN_UNIVERSE_COMMON.SETTINGS.previewSfxLabel", "试听音效").Resolve(),
			RitsuShellTheme.Current.Metric.FontSize.SettingLineTitle,
			HorizontalAlignment.Left,
			null,
			RitsuShellTheme.Current.Text.RichTitle);
		title.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		title.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;

		ModSettingsTextButton play = new(
			Loc("SUNQIAN_UNIVERSE_COMMON.SETTINGS.previewSfx.play", "播放").Resolve(),
			ModSettingsButtonTone.Accent,
			SquSfxPreview.PlaySelected);
		play.CustomMinimumSize = new Vector2(96f, RitsuShellTheme.Current.Metric.Entry.ValueMinHeight);

		ModSettingsDropdownChoiceControl<string> dropdown = new(
			PreviewOptions().Select(o => (o.Value, o.Label.Resolve())).ToArray(),
			PreviewSfxBinding.Read(),
			value =>
			{
				PreviewSfxBinding.Write(value);
				host.MarkDirty(PreviewSfxBinding);
			});

		row.AddChild(title);
		row.AddChild(play);
		row.AddChild(dropdown);
		return row;
	}

	private static ModSettingsChoiceOption<string>[] PreviewOptions() =>
	[
		new("eat", Loc("SUNQIAN_UNIVERSE_COMMON.SETTINGS.previewSfx.eat", "是啊吃什么")),
		new("yiling", Loc("SUNQIAN_UNIVERSE_COMMON.SETTINGS.previewSfx.yiling", "夷陵好火")),
		new("xiliang", Loc("SUNQIAN_UNIVERSE_COMMON.SETTINGS.previewSfx.xiliang", "西凉野人")),
		new("cybertron", Loc("SUNQIAN_UNIVERSE_COMMON.SETTINGS.previewSfx.cybertron", "赛博坦之力")),
		new("voice", Loc("SUNQIAN_UNIVERSE_COMMON.SETTINGS.previewSfx.voice", "变声期")),
		new("meteor", Loc("SUNQIAN_UNIVERSE_COMMON.SETTINGS.previewSfx.meteor", "飞火流星")),
		new("hole", Loc("SUNQIAN_UNIVERSE_COMMON.SETTINGS.previewSfx.hole", "透明窟窿")),
		new("poet", Loc("SUNQIAN_UNIVERSE_COMMON.SETTINGS.previewSfx.poet", "二言诗人剧本")),
	];

	private static ModSettingsText Loc(string key, string fallback) =>
		ModSettingsText.LocString(SquCommonL10n.Table, key, fallback);
}
