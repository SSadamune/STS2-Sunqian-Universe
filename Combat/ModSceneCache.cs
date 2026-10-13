using System.Collections.Generic;
using Godot;

#nullable enable

namespace Squ.Combat;

/// <summary>
/// Keeps mod-owned scenes alive across scene changes.
/// The game's <c>PreloadManager</c> drops scenes that are not part of the base game.
/// </summary>
internal static class ModSceneCache
{
	private static readonly Dictionary<string, PackedScene> Scenes = new();

	public static PackedScene? Get(string path)
	{
		if (Scenes.TryGetValue(path, out PackedScene? cached)
			&& GodotObject.IsInstanceValid(cached))
		{
			return cached;
		}

		if (!ResourceLoader.Exists(path))
		{
			Scenes.Remove(path);
			return null;
		}

		PackedScene? scene = ResourceLoader.Load<PackedScene>(path);
		if (scene == null)
		{
			Scenes.Remove(path);
			return null;
		}

		Scenes[path] = scene;
		return scene;
	}
}
