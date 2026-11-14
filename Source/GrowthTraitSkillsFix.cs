using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VanillaBugFixes;

public class GrowthTraitSkillsFix : Fix
{
    private static GrowthTraitSkillsFix instance;
    private static MethodInfo gainTrait;

    public override string Id => "GrowthTraitSkills";

    public override string Category => "VBF_CategoryDailyLife";

    protected override void Apply(Harmony harmony)
    {
        instance = this;
        gainTrait = AccessTools.Method(typeof(TraitSet), nameof(TraitSet.GainTrait), new[] { typeof(Trait), typeof(bool) })
            ?? throw new Exception("TraitSet.GainTrait not found");
        harmony.Patch(AccessTools.Method(typeof(Pawn_AgeTracker), "BirthdayBiological")
                ?? throw new Exception("Pawn_AgeTracker.BirthdayBiological not found"),
            transpiler: new HarmonyMethod(typeof(GrowthTraitSkillsFix), nameof(Transpiler)));
    }

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);
        int i = SingleMatch(codes, n => codes[n].Calls(gainTrait), "TraitSet.GainTrait call");
        codes[i] = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(GrowthTraitSkillsFix), nameof(GainTraitWithSkills)))
        {
            labels = codes[i].labels,
            blocks = codes[i].blocks
        };
        return codes;
    }

    public static void GainTraitWithSkills(TraitSet traits, Trait trait, bool suppressConflicts)
    {
        traits.GainTrait(trait, suppressConflicts);
        if (!instance.On || trait?.pawn == null || !traits.allTraits.Contains(trait))
        {
            return;
        }
        TraitUtility.ApplySkillGainFromTrait(trait.pawn, trait);
    }
}
