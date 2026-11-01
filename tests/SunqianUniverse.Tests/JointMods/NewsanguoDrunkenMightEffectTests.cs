using System.Reflection;
using Xunit;

namespace Squ.Tests.JointMods;

public sealed class NewsanguoDrunkenMightEffectJointModTests
{
	[Theory]
	[InlineData(false, false, false, false, "None")]
	[InlineData(false, true, true, true, "None")]
	[InlineData(true, false, false, false, "Hover")]
	[InlineData(true, false, true, true, "Hover")]
	[InlineData(true, true, false, false, "None")]
	[InlineData(true, true, true, false, "Inline")]
	[InlineData(true, true, false, true, "Inline")]
	[InlineData(true, true, true, true, "Inline")]
	public void Resolver_shows_hover_outside_combat_and_inline_text_for_the_local_fighter(
		bool isModReady,
		bool isInCombat,
		bool localPlayerIsNewsanguoCharacter,
		bool localPlayerHasDrunkenMight,
		string expected)
	{
		Type presentation = typeof(SquMod).Assembly.GetType(
			"Squ.Interop.NewsanguoDrunkenMightEffect",
			throwOnError: true)!;
		MethodInfo resolve = presentation.GetMethod(
			"Resolve",
			BindingFlags.Static | BindingFlags.NonPublic,
			types:
			[
				typeof(bool),
				typeof(bool),
				typeof(bool),
				typeof(bool),
			]) ?? throw new MissingMethodException(presentation.FullName, "Resolve");
		object actual = resolve.Invoke(
			null,
			[
				isModReady,
				isInCombat,
				localPlayerIsNewsanguoCharacter,
				localPlayerHasDrunkenMight,
			])!;

		Assert.Equal(expected, actual.ToString());
	}
}
