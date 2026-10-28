using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace VanillaBugFixes;

public class PrioritySorterFix : Fix
{
    private static PrioritySorterFix instance;

    public override string Id => "PrioritySorter";

    public override string Category => "VBF_CategoryJobs";

    protected override void Apply(Harmony harmony)
    {
        instance = this;
        harmony.Patch(AccessTools.Method(typeof(ThinkNode_PrioritySorter), nameof(ThinkNode_PrioritySorter.TryIssueJobPackage)),
            transpiler: new HarmonyMethod(typeof(PrioritySorterFix), nameof(Transpiler)));
    }

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        MethodInfo range = AccessTools.Method(typeof(Rand), nameof(Rand.Range), new[] { typeof(int), typeof(int) });
        MethodInfo insertIndex = AccessTools.Method(typeof(PrioritySorterFix), nameof(InsertIndex));
        var codes = new List<CodeInstruction>(instructions);
        int at = SingleMatch(codes, i => codes[i].Calls(range), "insert position");
        codes[at].operand = insertIndex;
        return codes;
    }

    public static int InsertIndex(int min, int lastIndex)
    {
        return Rand.Range(min, instance.On ? lastIndex + 2 : lastIndex);
    }
}
