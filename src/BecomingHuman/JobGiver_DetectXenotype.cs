using Verse;
using Verse.AI;

namespace BecomingHuman
{
    public class JobGiver_DetectXenotype : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (!pawn.CanReach(pawn.mindState.duty.focus, PathEndMode.Touch, Danger.Some))
            {
                return null;
            }

            XenotypeDiscoveryTracker tracker = Current.Game.GetComponent<XenotypeDiscoveryTracker>();
            if (tracker == null || !tracker.HasResearch)
            {
                return null;
            }

            Pawn targetPawn = pawn.mindState.duty.focus.Thing as Pawn;
            if (targetPawn == null || tracker.IsXenotypeDiscovered(targetPawn))
            {
                return null;
            }

            if (!XenotypeDiscoveryUtility.CanAttemptXenotypeDiscovery(pawn, targetPawn))
            {
                return null;
            }

            return JobMaker.MakeJob(BecomeHumanDefOf.DXD_DetectXenotype, targetPawn);
        }
    }



}
