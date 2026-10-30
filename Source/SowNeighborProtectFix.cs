using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VanillaBugFixes;

public class SowNeighborProtectFix : Fix
{
    private static SowNeighborProtectFix instance;

    private int corrected;

    public override string Id => "SowNeighborProtect";

    public override string Category => "VBF_CategoryColony";

    protected override void Apply(Harmony harmony)
    {
        instance = this;
        harmony.Patch(AccessTools.Method(typeof(WorkGiver_GrowerSow), nameof(WorkGiver_GrowerSow.JobOnCell)),
            transpiler: new HarmonyMethod(typeof(SowNeighborProtectFix), nameof(Transpiler)));
    }

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        MethodInfo zonePlant = AccessTools.Method(typeof(Zone_Growing), nameof(Zone_Growing.GetPlantDefToGrow));
        MethodInfo protects = AccessTools.Method(typeof(SowNeighborProtectFix), nameof(Protects));
        var codes = new List<CodeInstruction>(instructions);
        int at = SingleMatch(codes, i => i + 1 < codes.Count && codes[i].Calls(zonePlant)
            && (codes[i + 1].opcode == OpCodes.Bne_Un || codes[i + 1].opcode == OpCodes.Bne_Un_S), "neighbor zone check");
        var skip = codes[at + 1];
        codes[at + 1] = new CodeInstruction(OpCodes.Call, protects).MoveLabelsFrom(skip);
        codes.Insert(at + 2, new CodeInstruction(OpCodes.Brfalse, skip.operand));
        return codes;
    }

    public static bool Protects(ThingDef plantDef, ThingDef zonePlantDef)
    {
        if (plantDef == zonePlantDef)
        {
            return true;
        }
        if (!instance.On)
        {
            return false;
        }
        instance.corrected++;
        return true;
    }

    public override string Stats() => "VBF_SowNeighborProtectStats".Translate(corrected);

    public override void ResetStats()
    {
        corrected = 0;
    }
}
