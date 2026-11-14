using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VanillaBugFixes;

public class ImplantRemovalFix : Fix
{
    private static ImplantRemovalFix instance;

    public override string Id => "ImplantRemoval";

    public override string Category => "VBF_CategoryDailyLife";

    protected override void Apply(Harmony harmony)
    {
        instance = this;
        harmony.Patch(AccessTools.DeclaredMethod(typeof(Recipe_RemoveImplant), nameof(Recipe_RemoveImplant.ApplyOnPawn))
                ?? throw new Exception("Recipe_RemoveImplant.ApplyOnPawn not found"),
            transpiler: new HarmonyMethod(typeof(ImplantRemovalFix), nameof(Transpiler)));
    }

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        MethodInfo linq = typeof(Enumerable).GetMethods()
            .First(m => m.Name == nameof(Enumerable.FirstOrDefault) && m.GetParameters().Length == 2
                && m.GetParameters()[1].ParameterType.IsGenericType
                && m.GetParameters()[1].ParameterType.GetGenericTypeDefinition() == typeof(Func<,>))
            .MakeGenericMethod(typeof(Hediff));
        MethodInfo verse = typeof(GenCollection).GetMethods()
            .FirstOrDefault(m => m.Name == nameof(GenCollection.FirstOrDefault) && m.GetParameters().Length == 2
                && m.GetParameters()[1].ParameterType.IsGenericType
                && m.GetParameters()[1].ParameterType.GetGenericTypeDefinition() == typeof(Predicate<>))
            ?.MakeGenericMethod(typeof(Hediff));
        var codes = new List<CodeInstruction>(instructions);
        int at = SingleMatch(codes, i => codes[i].Calls(linq) || (verse != null && codes[i].Calls(verse)), "implant lookup");
        MethodInfo helper = AccessTools.Method(typeof(ImplantRemovalFix), codes[at].Calls(linq) ? nameof(FirstAtPartFunc) : nameof(FirstAtPartPredicate));
        codes[at] = new CodeInstruction(OpCodes.Call, helper);
        codes.Insert(at, new CodeInstruction(OpCodes.Ldarg_2).MoveLabelsFrom(codes[at]));
        return codes;
    }

    public static Hediff FirstAtPartFunc(IEnumerable<Hediff> source, Func<Hediff, bool> predicate, BodyPartRecord part)
    {
        return Choose(source, predicate, part);
    }

    public static Hediff FirstAtPartPredicate(List<Hediff> source, Predicate<Hediff> predicate, BodyPartRecord part)
    {
        return Choose(source, h => predicate(h), part);
    }

    private static Hediff Choose(IEnumerable<Hediff> source, Func<Hediff, bool> predicate, BodyPartRecord part)
    {
        Hediff first = source.FirstOrDefault(predicate);
        if (!instance.On)
        {
            return first;
        }
        Hediff exact = source.FirstOrDefault(h => predicate(h) && h.Part == part);
        if (exact == null)
        {
            return first;
        }
        return exact;
    }
}
