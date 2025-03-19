using RimWorld;
using UnityEngine;
using Verse;

namespace BecomingHuman
{
    public static class XenotypeDiscoveryUtility
    {
        // Check if xenotype detection can be attempted
        public static bool CanAttemptXenotypeDiscovery(Pawn detector, Pawn target)
        {
            if (detector == null || target == null || target.genes?.Xenotype == null)
            {
                return false;
            }

            XenotypeDiscoveryTracker tracker = Current.Game.GetComponent<XenotypeDiscoveryTracker>();
            if (tracker == null || !tracker.HasResearch)
            {
                return false;
            }

            // Already discovered
            if (tracker.IsXenotypeDiscovered(target))
            {
                return false;
            }

            return true;
        }

        public static float GetDetectionChance(this Pawn detector)
        {
            float multiplier = BecomingHumanMod.settings.arrestToDetectionMultiplier;
            float arrestChance = detector.GetStatValue(StatDefOf.ArrestSuccessChance);
            float detectionChance = arrestChance * multiplier;

            return Mathf.Clamp(detectionChance, 0, 100);
        }
    }
}
