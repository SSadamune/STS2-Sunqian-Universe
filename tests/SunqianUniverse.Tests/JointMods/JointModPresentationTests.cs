using System.Reflection;
using Xunit;

namespace Squ.Tests.JointMods;

public sealed class JointModPresentationTests
{
    [Theory]
    [InlineData(false, false, false, false, false, "None")]
    [InlineData(false, true, true, true, true, "None")]
    [InlineData(true, false, false, false, false, "HoverOnly")]
    [InlineData(true, true, false, true, false, "HoverOnly")]
    [InlineData(true, true, false, false, false, "None")]
    [InlineData(true, true, true, true, false, "HoverOnly")]
    [InlineData(true, true, true, false, false, "None")]
    [InlineData(true, true, true, true, true, "InlineKeywords")]
    public void Resolver_follows_the_A_B_C_matrix(
        bool isModReady,
        bool isInRun,
        bool isInCombat,
        bool partyContainsJointModCharacter,
        bool localPlayerIsJointModCharacter,
        string expected)
    {
		Type presentation = typeof(SquMod).Assembly.GetType(
			"Squ.Interop.JointModPresentation",
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
				typeof(bool),
			]) ?? throw new MissingMethodException(presentation.FullName, "Resolve");
		object actual = resolve.Invoke(
			null,
			[
				isModReady,
				isInRun,
				isInCombat,
				partyContainsJointModCharacter,
				localPlayerIsJointModCharacter,
			])!;

		Assert.Equal(expected, actual.ToString());
    }
}
