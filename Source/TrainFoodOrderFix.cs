using System;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace VanillaBugFixes;

public class TrainFoodOrderFix : Fix
{
    private static TrainFoodOrderFix instance;
    private static AccessTools.FieldRef<string> tooRecentlyText;
    private static Func<WorkGiver_InteractAnimal, Pawn, Pawn, bool, bool> canInteract;
    private static Func<WorkGiver_InteractAnimal, Pawn, Pawn, bool> hasFood;

    public override string Id => "TrainFoodOrder";

    public override string Category => "VBF_CategoryAnimals";

    protected override void Apply(Harmony harmony)
    {
        tooRecentlyText = AccessTools.StaticFieldRefAccess<string>(AccessTools.Field(typeof(WorkGiver_InteractAnimal), "AnimalInteractedTooRecentlyTrans"));
        canInteract = AccessTools.MethodDelegate<Func<WorkGiver_InteractAnimal, Pawn, Pawn, bool, bool>>(
            AccessTools.Method(typeof(WorkGiver_InteractAnimal), "CanInteractWithAnimal", new[] { typeof(Pawn), typeof(Pawn), typeof(bool) }));
        hasFood = AccessTools.MethodDelegate<Func<WorkGiver_InteractAnimal, Pawn, Pawn, bool>>(
            AccessTools.Method(typeof(WorkGiver_InteractAnimal), "HasFoodToInteractAnimal"));
        instance = this;
        harmony.Patch(AccessTools.Method(typeof(WorkGiver_Train), nameof(WorkGiver_Train.JobOnThing)),
            prefix: new HarmonyMethod(typeof(TrainFoodOrderFix), nameof(JobOnThingPrefix)));
    }

    public static bool JobOnThingPrefix(WorkGiver_Train __instance, Pawn pawn, Thing t, bool forced, ref Job __result)
    {
        if (!instance.On || t is not Pawn animal || !animal.IsAnimal || animal.RaceProps.animalType == AnimalType.Dryad
            || animal.Faction != pawn.Faction || animal.training == null || animal.training.NextTrainableToTrain() == null
            || !TrainableUtility.TrainedTooRecently(animal) || !canInteract(__instance, pawn, animal, forced))
        {
            return true;
        }
        JobFailReason.Is(tooRecentlyText());
        __result = null;
        return false;
    }
}
