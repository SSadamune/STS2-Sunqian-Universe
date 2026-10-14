using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using TestTheSpire;

namespace Squ.Tests;

[ModInitializer(nameof(Init))]
public static class Entry
{
    public static void Init()
    {
        new Harmony("sunqian-universe.Tests.patches")
            .PatchAll(Assembly.GetExecutingAssembly());

        CombatTestBootstrap.Initialize(
            Assembly.GetExecutingAssembly(),
            new CombatTestOptions
            {
                LogPrefix = "sunqian-universe.Tests",
                ZeroActionBehavior = ZeroActionTestBehavior.EnqueueCleanupNoOp
            });

        Log.Info("[sunqian-universe.Tests] Mod initialized");
    }
}
