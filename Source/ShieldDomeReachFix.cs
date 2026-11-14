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
            transpiler: new HarmonyMethod(typeof(ShieldDomeReachFix), nameof(Transpiler)),
            postfix: new HarmonyMethod(typeof(ShieldDomeReachFix), nameof(Postfix)));
    }

    public static void Postfix(CompProjectileInterceptor __instance, bool __result, Projectile projectile, Vector3 lastExactPos, Vector3 newExactPos)
    {
        if (!__result || !instance.On || projectile == null || !projectile.Spawned)
        {
            return;
        }
        float speedPerTick = projectile.def.projectile.SpeedTilesPerTick;
        float dx = newExactPos.x - lastExactPos.x;
        float dz = newExactPos.z - lastExactPos.z;
        float lengthSquared = dx * dx + dz * dz;
        float length = Mathf.Sqrt(lengthSquared);
        if (length <= speedPerTick * 1.01f)
        {
            return;
        }
        Vector3 center = __instance.parent.Position.ToVector3Shifted();
        float radius = __instance.Props.radius;
        float fx = lastExactPos.x - center.x;
        float fz = lastExactPos.z - center.z;
        float b = 2f * (fx * dx + fz * dz);
        float c = fx * fx + fz * fz - radius * radius;
        float discriminant = b * b - 4f * lengthSquared * c;
        if (discriminant < 0f)
        {
            return;
        }
        float root = Mathf.Sqrt(discriminant);
        float t = (-b - root) / (2f * lengthSquared);
        if (t < 0f || t > 1f)
        {
            t = (-b + root) / (2f * lengthSquared);
            if (t < 0f || t > 1f)
            {
                return;
            }
        }
        t = Mathf.Max(0f, t - speedPerTick / length);
        IntVec3 cell = new Vector3(lastExactPos.x + dx * t, 0f, lastExactPos.z + dz * t).ToIntVec3();
        if (cell.InBounds(projectile.Map))
        {
            projectile.Position = cell;
        }
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
