using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VanillaBugFixes;

public class MultiAttackTargetsFix : Fix
{
    private static MultiAttackTargetsFix instance;
    private static AccessTools.FieldRef<List<Pawn>> sharedPawns;

    public override string Id => "MultiAttackTargets";

    public override string Category => "VBF_CategoryCombat";

    protected override void Apply(Harmony harmony)
    {
        instance = this;
        Type provider = AccessTools.TypeByName("RimWorld.FloatMenuOptionProvider_DraftedAttack")
            ?? throw new Exception("FloatMenuOptionProvider_DraftedAttack not found");
        sharedPawns = AccessTools.StaticFieldRefAccess<List<Pawn>>(AccessTools.Field(provider, "tmpPawns")
            ?? throw new Exception("FloatMenuOptionProvider_DraftedAttack.tmpPawns not found"));
        harmony.Patch(AccessTools.DeclaredMethod(provider, "GetMultiselectAttackOption")
                ?? throw new Exception("FloatMenuOptionProvider_DraftedAttack.GetMultiselectAttackOption not found"),
            postfix: new HarmonyMethod(typeof(MultiAttackTargetsFix), nameof(OptionPostfix)));
    }

    public static void OptionPostfix(FloatMenuOption __result)
    {
        if (__result?.action == null || !instance.On)
        {
            return;
        }
        List<Pawn> snapshot = sharedPawns().ToList();
        Action original = __result.action;
        __result.action = () =>
        {
            List<Pawn> shared = sharedPawns();
            List<Pawn> saved = shared.ToList();
            shared.Clear();
            shared.AddRange(snapshot);
            try
            {
                original();
            }
            finally
            {
                shared.Clear();
                shared.AddRange(saved);
            }
        };
    }
}
