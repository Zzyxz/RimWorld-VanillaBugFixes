using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VanillaBugFixes;

public class TaleIgnoreRollFix : Fix
{
    private static TaleIgnoreRollFix instance;

    public override string Id => "TaleIgnoreRoll";

    public override string Category => "VBF_CategoryDailyLife";

    protected override void Apply(Harmony harmony)
    {
        instance = this;
        harmony.Patch(AccessTools.DeclaredMethod(typeof(TaleRecorder), nameof(TaleRecorder.RecordTale))
                ?? throw new Exception("TaleRecorder.RecordTale not found"),
            transpiler: new HarmonyMethod(typeof(TaleIgnoreRollFix), nameof(Transpiler)));
    }

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        MethodInfo value = AccessTools.PropertyGetter(typeof(Rand), nameof(Rand.Value));
        MethodInfo second = AccessTools.Method(typeof(TaleIgnoreRollFix), nameof(SecondRoll));
        var codes = new List<CodeInstruction>(instructions);
        var rolls = new List<int>();
        for (int i = 0; i < codes.Count; i++)
        {
            if (codes[i].Calls(value))
            {
                rolls.Add(i);
            }
        }
        if (rolls.Count != 2)
        {
            throw new Exception("expected two ignore rolls, found " + rolls.Count);
        }
        codes[rolls[1]] = new CodeInstruction(OpCodes.Call, second).MoveLabelsFrom(codes[rolls[1]]);
        return codes;
    }

    public static float SecondRoll()
    {
        if (!instance.On)
        {
            return Rand.Value;
        }
        return 2f;
    }
}
