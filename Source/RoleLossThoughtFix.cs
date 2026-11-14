using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VanillaBugFixes;

public class RoleLossThoughtFix : Fix
{
    private static RoleLossThoughtFix instance;
    private static AccessTools.FieldRef<Precept_RoleMulti, List<IdeoRoleInstance>> cache;

    public override string Id => "RoleLossThought";

    public override string Category => "VBF_CategoryDailyLife";

    protected override void Apply(Harmony harmony)
    {
        instance = this;
        cache = AccessTools.FieldRefAccess<Precept_RoleMulti, List<IdeoRoleInstance>>("chosenPawnsCache");
        harmony.Patch(AccessTools.DeclaredMethod(typeof(Precept_RoleMulti), nameof(Precept_RoleMulti.Unassign))
                ?? throw new Exception("Precept_RoleMulti.Unassign not found"),
            prefix: new HarmonyMethod(typeof(RoleLossThoughtFix), nameof(Prefix)));
    }

    public static bool Prefix(Precept_RoleMulti __instance, Pawn p, bool generateThoughts)
    {
        if (generateThoughts || !instance.On)
        {
            return true;
        }
        IdeoRoleInstance role = __instance.chosenPawns.FirstOrDefault(c => c.pawn == p);
        if (role == null)
        {
            return false;
        }
        __instance.chosenPawns.Remove(role);
        cache(__instance).Add(role);
        __instance.Notify_PawnUnassigned(p);
        return false;
    }
}
