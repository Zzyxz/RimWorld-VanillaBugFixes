using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace VanillaBugFixes;

public class CrateMultiOpenFix : Fix
{
    private static CrateMultiOpenFix instance;
    private static MethodInfo makeJob;

    private int corrected;

    public override string Id => "CrateMultiOpen";

    public override string Category => "VBF_CategoryJobs";

    protected override void Apply(Harmony harmony)
    {
        instance = this;
        makeJob = AccessTools.Method(typeof(JobMaker), nameof(JobMaker.MakeJob), new[] { typeof(JobDef), typeof(LocalTargetInfo) })
            ?? throw new Exception("JobMaker.MakeJob not found");
        List<MethodInfo> actions = AccessTools.GetDeclaredMethods(typeof(Building_Crate))
            .Where(m => m.Name.Contains("GetMultiSelectFloatMenuOptions") && m.Name.Contains("b__")
                && PatchProcessor.ReadMethodBody(m).Any(code => Equals(code.Value, makeJob)))
            .ToList();
        if (actions.Count != 1)
        {
            throw new Exception("open action found " + actions.Count + " times");
        }
        harmony.Patch(actions[0], transpiler: new HarmonyMethod(typeof(CrateMultiOpenFix), nameof(Transpiler)));
    }

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);
        int at = SingleMatch(codes, i => codes[i].Calls(makeJob), "open job");
        codes[at] = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CrateMultiOpenFix), nameof(MakeOpenJob))).MoveLabelsFrom(codes[at]);
        return codes;
    }

    public static Job MakeOpenJob(JobDef def, LocalTargetInfo target)
    {
        Job job = JobMaker.MakeJob(def, target);
        if (!instance.On)
        {
            return job;
        }
        Thing thing = target.Thing;
        if (thing?.Map != null && thing.Map.designationManager.DesignationOn(thing, DesignationDefOf.Open) == null)
        {
            instance.corrected++;
        }
        job.ignoreDesignations = true;
        return job;
    }

    public override string Stats() => "VBF_CrateMultiOpenStats".Translate(corrected);

    public override void ResetStats()
    {
        corrected = 0;
    }
}
