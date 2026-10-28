using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace VanillaBugFixes;

public class PanicFleeFix : Fix
{
    private static PanicFleeFix instance;

    private int corrected;

    public override string Id => "PanicFlee";

    public override string Category => "VBF_CategoryCombat";

    public override bool DefaultOn => false;

    protected override void Apply(Harmony harmony)
    {
        instance = this;
        harmony.Patch(AccessTools.Method(typeof(MentalStateHandler), nameof(MentalStateHandler.Notify_DamageTaken)),
            transpiler: new HarmonyMethod(typeof(PanicFleeFix), nameof(Transpiler)));
    }

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        MethodInfo hashOffset = AccessTools.Method(typeof(Gen), nameof(Gen.HashOffset), new[] { typeof(Thing) });
        MethodInfo roll = AccessTools.Method(typeof(PanicFleeFix), nameof(Roll));
        var codes = new List<CodeInstruction>(instructions);
        int at = SingleMatch(codes, i => i + 2 < codes.Count && codes[i].Calls(hashOffset) && codes[i + 1].LoadsConstant(100) && codes[i + 2].opcode == OpCodes.Rem, "flee roll");
        codes.Insert(at + 3, new CodeInstruction(OpCodes.Call, roll));
        return codes;
    }

    public static int Roll(int rest)
    {
        if (rest >= 0 || !instance.On)
        {
            return rest;
        }
        instance.corrected++;
        return -rest;
    }

    public override string Stats() => "VBF_PanicFleeStats".Translate(corrected);

    public override void ResetStats()
    {
        corrected = 0;
    }
}
