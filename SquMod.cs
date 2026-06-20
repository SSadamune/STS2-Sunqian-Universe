using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace Squ;

[ModInitializer("ModLoaded")]
public static class SquMod
{
	public static void ModLoaded()
	{
		Log.Warn("sunqian-universe (SQU) mod loaded!");
	}
}
