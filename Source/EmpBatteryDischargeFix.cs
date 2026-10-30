using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VanillaBugFixes;

public class EmpBatteryDischargeFix : Fix
{
    private static EmpBatteryDischargeFix instance;

    private int corrected;

    public override string Id => "EmpBatteryDischarge";

    public override string Category => "VBF_CategoryColony";

    protected override void Apply(Harmony harmony)
    {
        instance = this;
        harmony.Patch(AccessTools.Method(typeof(PowerNet), "ChangeStoredEnergy"),
            transpiler: new HarmonyMethod(typeof(EmpBatteryDischargeFix), nameof(Transpiler)));
    }

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        MethodInfo add = AccessTools.Method(typeof(List<CompPowerBattery>), nameof(List<CompPowerBattery>.Add));
        MethodInfo addGiving = AccessTools.Method(typeof(EmpBatteryDischargeFix), nameof(AddGivingBattery));
        var codes = new List<CodeInstruction>(instructions);
        int at = SingleMatch(codes, i => codes[i].Calls(add), "discharging battery list");
        codes[at] = new CodeInstruction(OpCodes.Call, addGiving).MoveLabelsFrom(codes[at]);
        return codes;
    }

    public static void AddGivingBattery(List<CompPowerBattery> givingBats, CompPowerBattery battery)
    {
        if (battery.StunnedByEMP && instance.On)
        {
            instance.corrected++;
            return;
        }
        givingBats.Add(battery);
    }

    public override string Stats() => "VBF_EmpBatteryDischargeStats".Translate(corrected);

    public override void ResetStats()
    {
        corrected = 0;
    }
}
