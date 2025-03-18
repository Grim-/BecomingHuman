using RimWorld;
using System.Collections.Generic;
using Verse;

namespace BecomingHuman
{
    public class XenotypeDiscoveryTracker : GameComponent
    {
        public List<XenotypeDef> whiteListedXenotypes = new List<XenotypeDef>();
        private Dictionary<Pawn, bool> discoveredData = new Dictionary<Pawn, bool>();

        public float arrestToDetectionMultiplier = 1.5f;

        public bool HasResearch => Current.Game.researchManager.GetProgress(BecomeHumanDefOf.XenotypeDetection) >= 1f;

        public XenotypeDiscoveryTracker(Game game) : base()
        {
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref discoveredData, "discoveredXenotypes", LookMode.Reference, LookMode.Value);
            if (discoveredData == null)
            {
                discoveredData = new Dictionary<Pawn, bool>();
            }
        }



        public void AddToWhiteList(XenotypeDef xenoDef)
        {
            if (!whiteListedXenotypes.Contains(xenoDef))
            {
                whiteListedXenotypes.Add(xenoDef);
            }
        }

        //
        public bool CanBeDiscovered(Pawn pawn)
        {
            if (!HasResearch)
            {
                return false;
            }

            if (pawn.GuestStatus == GuestStatus.Prisoner && pawn.guest.will <= 0)
            {
                return true;
            }
            else if (pawn.guest.GuestStatus != GuestStatus.Prisoner)
            {
                float arrestChance = pawn.GetStatValue(StatDefOf.ArrestSuccessChance);
                return Rand.Range(0, 1f) <= arrestChance * arrestToDetectionMultiplier;
            }

            return true;
        }

        public bool IsXenotypeDiscovered(Pawn pawn)
        {       
            // Non-xenotype pawns always show
            if (pawn?.genes?.Xenotype == null)
                return true;
            
            //return true while not playing
            if (Current.ProgramState != ProgramState.Playing)
            {
                return true;
            }

            if (discoveredData.ContainsKey(pawn))
            {
                Log.Message("adding pawn to discovered list");
                return true;
            }

            return false;
        }
        public static bool IsXenotypeDiscovere(Pawn pawn)
        {
            if (pawn?.genes?.Xenotype == null)
                return true;

            if (Current.Game == null)
                return true;

            XenotypeDiscoveryTracker tracker = Current.Game.GetComponent<XenotypeDiscoveryTracker>();
            if (tracker == null)
                return true;

            return tracker.IsXenotypeDiscovered(pawn);
        }
        public void DiscoverXenotype(Pawn pawn)
        {
            if (pawn != null && !discoveredData.ContainsKey(pawn))
            {
                Log.Message("adding pawn to discovered list");
                discoveredData.Add(pawn, true);
            }
        }
    }
}
