using System;
using System.Collections;
using System.IO;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using Xunit;

namespace TestTheSpire;

public static class CardTestAssertions
{
	public static int PowerAmount<TPower>(Creature creature) where TPower : PowerModel
	{
		object obj = creature.GetPower<TPower>();
		if (obj == null)
		{
			return 0;
		}
		return ((PowerModel)obj).Amount;
	}

	public static void AssertPowerAmount<TPower>(Creature creature, int expected) where TPower : PowerModel
	{
		Assert.Equal<int>(expected, PowerAmount<TPower>(creature));
	}

	public static void AssertCurrentPortraitPathsExist(CardModel card, string? projectDirectory = null)
	{
		Assert.NotEmpty((IEnumerable)card.AllPortraitPaths);
		Assert.Contains<string>(card.PortraitPath, card.AllPortraitPaths);
		Assert.True(ProjectFileExists(card.PortraitPath, projectDirectory), card.PortraitPath + " should exist.");
		Assert.All<string>(card.AllPortraitPaths, (Action<string>)delegate(string path)
		{
			Assert.True(ProjectFileExists(path, projectDirectory), path + " should exist.");
		});
	}

	public static bool ProjectFileExists(string? resourcePath, string? projectDirectory = null)
	{
		if (string.IsNullOrWhiteSpace(resourcePath))
		{
			return false;
		}
		if (ResourceLoader.Exists(resourcePath, ""))
		{
			return true;
		}
		string text;
		if (!resourcePath.StartsWith("res://", StringComparison.Ordinal))
		{
			text = resourcePath;
		}
		else
		{
			int length = "res://".Length;
			text = resourcePath.Substring(length, resourcePath.Length - length);
		}
		string text2 = text;
		if (Path.IsPathRooted(text2))
		{
			return File.Exists(text2);
		}
		if (!string.IsNullOrWhiteSpace(projectDirectory))
		{
			return File.Exists(Path.Combine(projectDirectory, text2));
		}
		for (DirectoryInfo directoryInfo = new DirectoryInfo(AppContext.BaseDirectory); directoryInfo != null; directoryInfo = directoryInfo.Parent)
		{
			if (File.Exists(Path.Combine(directoryInfo.FullName, text2)))
			{
				return true;
			}
		}
		return false;
	}
}
