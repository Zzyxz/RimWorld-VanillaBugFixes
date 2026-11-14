using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VanillaBugFixes;

public class PortalMultiSelectFix : Fix
{
    private static PortalMultiSelectFix instance;

    public override string Id => "PortalMultiSelect";

    public override string Category => "VBF_CategoryJobs";

    protected override void Apply(Harmony harmony)
    {
        instance = this;
        harmony.Patch(AccessTools.DeclaredMethod(typeof(FloatMenuOptionProvider_EnterMapPortal), "GetSingleOptionFor")
                ?? throw new Exception("FloatMenuOptionProvider_EnterMapPortal.GetSingleOptionFor not found"),
            transpiler: new HarmonyMethod(typeof(PortalMultiSelectFix), nameof(Transpiler)));
    }

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        MethodInfo current = AccessTools.PropertyGetter(typeof(IEnumerator<Pawn>), nameof(IEnumerator<Pawn>.Current));
        MethodInfo firstPawn = AccessTools.PropertyGetter(typeof(FloatMenuContext), nameof(FloatMenuContext.FirstSelectedPawn));
        var codes = new List<CodeInstruction>(instructions);
        int loop = SingleMatch(codes, i => i + 1 < codes.Count && codes[i].Calls(current) && codes[i + 1].IsStloc(), "selected pawn loop");
        CodeInstruction loadPawn = LoadOf(codes[loop + 1]);
        int check = codes.FindIndex(loop + 2, c => c.Calls(firstPawn));
        if (check < 0)
        {
            throw new Exception("pawn check not found");
        }
        codes.InsertRange(check + 1, new[]
        {
            loadPawn,
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PortalMultiSelectFix), nameof(PawnToCheck))),
        });
        return codes;
    }

    private static CodeInstruction LoadOf(CodeInstruction store)
    {
        if (store.opcode == OpCodes.Stloc_0) return new CodeInstruction(OpCodes.Ldloc_0);
        if (store.opcode == OpCodes.Stloc_1) return new CodeInstruction(OpCodes.Ldloc_1);
        if (store.opcode == OpCodes.Stloc_2) return new CodeInstruction(OpCodes.Ldloc_2);
        if (store.opcode == OpCodes.Stloc_3) return new CodeInstruction(OpCodes.Ldloc_3);
        return new CodeInstruction(OpCodes.Ldloc, store.operand);
    }

    public static Pawn PawnToCheck(Pawn firstSelected, Pawn pawn)
    {
        if (!instance.On || pawn == null)
        {
            return firstSelected;
        }
        return pawn;
    }
}
