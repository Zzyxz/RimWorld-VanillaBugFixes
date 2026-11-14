using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VanillaBugFixes;

public class WaterRaidMapFix : Fix
{
    private static WaterRaidMapFix instance;

    public override string Id => "WaterRaidMap";

    public override string Category => "VBF_CategoryCombat";

    protected override void Apply(Harmony harmony)
    {
        instance = this;
        Type worker = AccessTools.TypeByName("RimWorld.PawnsArrivalModeWorker_EmergeFromWater")
            ?? throw new Exception("PawnsArrivalModeWorker_EmergeFromWater not found");
        harmony.Patch(AccessTools.DeclaredMethod(worker, "Arrive")
                ?? throw new Exception("PawnsArrivalModeWorker_EmergeFromWater.Arrive not found"),
            transpiler: new HarmonyMethod(typeof(WaterRaidMapFix), nameof(Transpiler)));
    }

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        MethodInfo currentMap = AccessTools.PropertyGetter(typeof(Find), nameof(Find.CurrentMap));
        MethodInfo targetMap = AccessTools.Method(typeof(WaterRaidMapFix), nameof(TargetMap));
        var codes = new List<CodeInstruction>(instructions);
        int found = 0;
        for (int i = 0; i < codes.Count; i++)
        {
            if (!codes[i].Calls(currentMap))
            {
                continue;
            }
            codes[i] = new CodeInstruction(OpCodes.Ldarg_2).MoveLabelsFrom(codes[i]);
            codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, targetMap));
            found++;
        }
        if (found == 0)
        {
            throw new Exception("current map lookup not found");
        }
        return codes;
    }

    public static Map TargetMap(IncidentParms parms)
    {
        Map current = Find.CurrentMap;
        if (!instance.On || !(parms?.target is Map target) || target == current)
        {
            return current;
        }
        return target;
    }
}
