using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;

namespace VanillaBugFixes;

public class SettlementInhabitantsFix : Fix
{
    private static SettlementInhabitantsFix instance;

    private int remembered;

    public override string Id => "SettlementInhabitants";

    public override string Category => "VBF_CategoryWorld";

    protected override void Apply(Harmony harmony)
    {
        instance = this;
        harmony.Patch(AccessTools.Method(typeof(PawnGenerator), "GenerateOrRedressPawnInternal"),
            transpiler: new HarmonyMethod(typeof(SettlementInhabitantsFix), nameof(Transpiler)));
    }

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        Type tile = AccessTools.TypeByName("RimWorld.Planet.PlanetTile") ?? throw new Exception("tile type not found");
        MethodInfo valid = AccessTools.PropertyGetter(tile, "Valid") ?? throw new Exception("tile check not found");
        MethodInfo skipStore = AccessTools.Method(typeof(SettlementInhabitantsFix), nameof(SkipStore));
        var codes = new List<CodeInstruction>(instructions);
        int found = SingleMatch(codes, i => i + 1 < codes.Count && codes[i].Calls(valid)
            && (codes[i + 1].opcode == OpCodes.Brtrue || codes[i + 1].opcode == OpCodes.Brtrue_S), "tile check");
        codes.Insert(found + 1, new CodeInstruction(OpCodes.Call, skipStore));
        return codes;
    }

    public static bool SkipStore(bool tileValid)
    {
        if (!instance.On)
        {
            return tileValid;
        }
        if (tileValid)
        {
            instance.remembered++;
        }
        return !tileValid;
    }

    public override string Stats() => "VBF_SettlementInhabitantsStats".Translate(remembered);

    public override void ResetStats()
    {
        remembered = 0;
    }
}
