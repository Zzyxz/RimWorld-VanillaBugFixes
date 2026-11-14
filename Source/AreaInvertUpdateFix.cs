using System;
using HarmonyLib;
using Verse;

namespace VanillaBugFixes;

public class AreaInvertUpdateFix : Fix
{
    private static AreaInvertUpdateFix instance;
    private static Action<Area, IntVec3> markDirty;

    public override string Id => "AreaInvertUpdate";

    public override string Category => "VBF_CategoryJobs";

    protected override void Apply(Harmony harmony)
    {
        instance = this;
        markDirty = AccessTools.MethodDelegate<Action<Area, IntVec3>>(AccessTools.DeclaredMethod(typeof(Area), "MarkDirty")
            ?? throw new Exception("Area.MarkDirty not found"));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(Area), nameof(Area.Invert))
                ?? throw new Exception("Area.Invert not found"),
            postfix: new HarmonyMethod(typeof(AreaInvertUpdateFix), nameof(InvertPostfix)));
    }

    public static void InvertPostfix(Area __instance)
    {
        if (!instance.On)
        {
            return;
        }
        Map map = __instance.Map;
        if (map == null)
        {
            return;
        }
        foreach (IntVec3 cell in map.AllCells)
        {
            markDirty(__instance, cell);
        }
    }
}
