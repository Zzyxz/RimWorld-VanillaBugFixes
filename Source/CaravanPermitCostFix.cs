using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VanillaBugFixes;

public class CaravanPermitCostFix : Fix
{
    private static CaravanPermitCostFix instance;
    private static AccessTools.FieldRef<RoyalTitlePermitWorker_Targeted, bool> freeField;

    public override string Id => "CaravanPermitCost";

    public override string Category => "VBF_CategoryWorld";

    protected override void Apply(Harmony harmony)
    {
        instance = this;
        freeField = AccessTools.FieldRefAccess<RoyalTitlePermitWorker_Targeted, bool>("free");
        foreach (Type worker in new[] { typeof(RoyalTitlePermitWorker_DropResources), typeof(RoyalTitlePermitWorker_CallShuttle) })
        {
            harmony.Patch(AccessTools.DeclaredMethod(worker, nameof(RoyalTitlePermitWorker.GetCaravanGizmos))
                    ?? throw new Exception(worker.Name + ".GetCaravanGizmos not found"),
                postfix: new HarmonyMethod(typeof(CaravanPermitCostFix), nameof(GizmosPostfix)));
        }
    }

    public static IEnumerable<Gizmo> GizmosPostfix(IEnumerable<Gizmo> __result, RoyalTitlePermitWorker_Targeted __instance)
    {
        foreach (Gizmo gizmo in __result)
        {
            if (instance.On && gizmo is Command_Action command && command.action != null)
            {
                bool free = freeField(__instance);
                Action original = command.action;
                command.action = () =>
                {
                    freeField(__instance) = free;
                    original();
                };
            }
            yield return gizmo;
        }
    }
}
