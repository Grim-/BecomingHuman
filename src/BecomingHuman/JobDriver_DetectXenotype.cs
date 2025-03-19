using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace BecomingHuman
{
    public class JobDriver_DetectXenotype : JobDriver
    {
        private const int DetectionDuration = 300;

        public Pawn TargetPawn => job.targetA.Thing as Pawn;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            //this.FailOnDowned(TargetIndex.A);
            this.FailOnAggroMentalState(TargetIndex.A);

     
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);


            Toil detectXenotype = new Toil();
            detectXenotype.initAction = () => {
                pawn.pather.StopDead();
                pawn.rotationTracker.FaceTarget(TargetPawn);
                if (TargetPawn.stances?.stunner != null)
                {
                    TargetPawn.stances?.stunner.StunFor(300, pawn, false, false, true);
                }
            };
            detectXenotype.defaultCompleteMode = ToilCompleteMode.Delay;
            detectXenotype.defaultDuration = DetectionDuration;
            detectXenotype.WithProgressBarToilDelay(TargetIndex.A);
            detectXenotype.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            detectXenotype.FailOn(() => !XenotypeDiscoveryUtility.CanAttemptXenotypeDiscovery(pawn, TargetPawn));


            detectXenotype.WithEffect(() => BecomeHumanDefOf.DetectScan, TargetIndex.A);


            yield return detectXenotype;

            Toil finalizeDetection = new Toil();
            finalizeDetection.initAction = () => {
                XenotypeDiscoveryTracker tracker = Current.Game.GetComponent<XenotypeDiscoveryTracker>();
                if (tracker != null && tracker.CanBeDiscovered(pawn, TargetPawn))
                {
                    //Log.Message($"Attempting to discover xenotype for {TargetPawn.LabelShort}, before discovery status: {tracker.IsXenotypeDiscovered(TargetPawn)}");
                    tracker.DiscoverXenotype(TargetPawn);
                    if (!tracker.whiteListedXenotypes.Contains(TargetPawn.genes.Xenotype))
                    {
                        Messages.Message("BecomingHuman.DetectionMessage".Translate(TargetPawn.LabelShort, TargetPawn.genes.Xenotype.label),
                                        TargetPawn, MessageTypeDefOf.PositiveEvent);
                        BecomeHumanDefOf.DetectBeep.PlayOneShotOnCamera(TargetPawn.Map);
                    }

                }
                else
                {
                    Messages.Message("BecomingHuman.DetectionFailedMessage".Translate(TargetPawn.LabelShort),
                                    TargetPawn, MessageTypeDefOf.NeutralEvent);
                }
            };
            yield return finalizeDetection;
        }
    }



}
