using System;
using HarmonyLib;
using Verse;

namespace VanillaBugFixes;

public class SnowTotalAfterLoadFix : Fix
{
    private static SnowTotalAfterLoadFix instance;
    private static AccessTools.FieldRef<SnowGrid, Map> map;
    private static AccessTools.FieldRef<SnowGrid, double> totalDepth;

    private int corrected;

    public override string Id => "SnowTotalAfterLoad";

    public override string Category => "VBF_CategoryDailyLife";

    protected override void Apply(Harmony harmony)
    {
        map = AccessTools.FieldRefAccess<SnowGrid, Map>("map");
        totalDepth = AccessTools.FieldRefAccess<SnowGrid, double>("totalDepth");
        var expose = AccessTools.DeclaredMethod(typeof(SnowGrid), nameof(SnowGrid.ExposeData))
            ?? throw new Exception("SnowGrid.ExposeData not found");
        instance = this;
        harmony.Patch(expose, postfix: new HarmonyMethod(typeof(SnowTotalAfterLoadFix), nameof(ExposeDataPostfix)));
    }

    public static void ExposeDataPostfix(SnowGrid __instance)
    {
        if (Scribe.mode != LoadSaveMode.LoadingVars || !instance.On)
        {
            return;
        }
        Map gridMap = map(__instance);
        if (gridMap == null)
        {
            return;
        }
        double sum = 0;
        foreach (IntVec3 c in gridMap.AllCells)
        {
            sum += __instance.GetDepth(c);
        }
        totalDepth(__instance) = sum;
        if (sum > 0)
        {
            instance.corrected++;
        }
    }

    public override string Stats() => "VBF_SnowTotalAfterLoadStats".Translate(corrected);

    public override void ResetStats()
    {
        corrected = 0;
    }
}
