using HarmonyLib;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace VanillaBugFixes;

public class JobClearFix : Fix
{
    private static JobClearFix instance;

    public override string Id => "JobClear";

    public override string Category => "VBF_CategoryJobs";

    protected override void Apply(Harmony harmony)
    {
        instance = this;
        harmony.Patch(AccessTools.Method(typeof(Job), nameof(Job.Clear)),
            postfix: new HarmonyMethod(typeof(JobClearFix), nameof(ClearPostfix)));
    }

    public static void ClearPostfix(Job __instance)
    {
        if (!instance.On)
        {
            return;
        }
        __instance.globalTarget = GlobalTargetInfo.Invalid;
        __instance.showCarryingInspectLine = true;
        __instance.expireOnEnemiesNearby = false;
        __instance.interactableIndex = -1;
    }
}
