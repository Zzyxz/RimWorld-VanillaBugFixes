using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VanillaBugFixes;

public class InventoryKeepListFix : Fix
{
    private static InventoryKeepListFix instance;
    private static AccessTools.FieldRef<List<ThingDefCount>> itemsToKeep;

    public override string Id => "InventoryKeepList";

    public override string Category => "VBF_CategoryJobs";

    protected override void Apply(Harmony harmony)
    {
        itemsToKeep = AccessTools.StaticFieldRefAccess<List<ThingDefCount>>(
            AccessTools.Field(typeof(Pawn_InventoryTracker), "tmpItemsToKeep") ?? throw new Exception("Pawn_InventoryTracker.tmpItemsToKeep not found"));
        instance = this;
        harmony.Patch(AccessTools.PropertyGetter(typeof(Pawn_InventoryTracker), nameof(Pawn_InventoryTracker.FirstUnloadableThing))
                ?? throw new Exception("Pawn_InventoryTracker.FirstUnloadableThing not found"),
            prefix: new HarmonyMethod(typeof(InventoryKeepListFix), nameof(Prefix)));
    }

    public static void Prefix(Pawn_InventoryTracker __instance)
    {
        if (!instance.On)
        {
            return;
        }
        List<ThingDefCount> list = itemsToKeep();
        if (list.Count == 0)
        {
            return;
        }
        list.Clear();
    }
}
