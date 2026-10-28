using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace VanillaBugFixes;

public class ShieldDomeReachFix : Fix
{
    private static ShieldDomeReachFix instance;

    public override string Id => "ShieldDomeReach";

    public override string Category => "VBF_CategoryCombat";

    protected override void Apply(Harmony harmony)
    {
        instance = this;
        harmony.Patch(AccessTools.Method(typeof(CompProjectileInterceptor), nameof(CompProjectileInterceptor.CheckIntercept)),
            transpiler: new HarmonyMethod(typeof(ShieldDomeReachFix), nameof(Transpiler)));
    }

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        MethodInfo speed = AccessTools.PropertyGetter(typeof(ProjectileProperties), nameof(ProjectileProperties.SpeedTilesPerTick));
        MethodInfo reach = AccessTools.Method(typeof(ShieldDomeReachFix), nameof(Reach));
        var codes = new List<CodeInstruction>(instructions);
        int at = SingleMatch(codes, i => codes[i].Calls(speed), "shield reach check");
        codes.InsertRange(at + 1, new[]
        {
            new CodeInstruction(OpCodes.Ldarg_2),
            new CodeInstruction(OpCodes.Ldarg_3),
            new CodeInstruction(OpCodes.Call, reach),
        });
        return codes;
    }

    public static float Reach(float speedPerTick, Vector3 lastExactPos, Vector3 newExactPos)
    {
        if (!instance.On)
        {
            return speedPerTick;
        }
        float dx = newExactPos.x - lastExactPos.x;
        float dz = newExactPos.z - lastExactPos.z;
        return Mathf.Max(speedPerTick, Mathf.Sqrt(dx * dx + dz * dz));
    }
}
