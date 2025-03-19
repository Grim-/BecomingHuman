using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace BecomingHuman
{
    public class XenotypeDiscoveryTracker : GameComponent
    {
        public List<XenotypeDef> whiteListedXenotypes = new List<XenotypeDef>();
        private Dictionary<Pawn, bool> discoveredData = new Dictionary<Pawn, bool>();

        public float arrestToDetectionMultiplier = 1.5f;
        private List<Pawn> pawnKeys;
        private List<bool> boolValues;
        public bool HasResearch => Current.Game.researchManager.GetProgress(BecomeHumanDefOf.XenotypeDetection) >= 1f;

        public XenotypeDiscoveryTracker(Game game) : base()
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();

            if (Scribe.mode == LoadSaveMode.Saving && discoveredData == null)
            {
                discoveredData = new Dictionary<Pawn, bool>();
            }

            Scribe_Collections.Look(
                ref discoveredData,
                "discoveredXenotypes",
                LookMode.Reference,
                LookMode.Value,
                ref pawnKeys,
                ref boolValues);
        }



        public void AddToWhiteList(XenotypeDef xenoDef)
        {
            if (!whiteListedXenotypes.Contains(xenoDef))
            {
                whiteListedXenotypes.Add(xenoDef);
            }
        }

        //
        public bool CanBeDiscovered(Pawn discoverer, Pawn pawn)
        {
            if (!HasResearch)
            {
                return false;
            }

            //only broken prisoners can be discovered against their will
            if (pawn.GuestStatus == GuestStatus.Prisoner && pawn.guest.will <= 0)
            {
                return true;
            }
            //anything else the chance is determined by arrest x 1.5 
            else if (pawn.guest.GuestStatus != GuestStatus.Prisoner)
            {
                float rolledValue = Rand.Range(0, 1f);
                float detectionChance = discoverer.GetDetectionChance();
                bool isDiscovered = rolledValue <= detectionChance;
                Log.Message($"Rolled {rolledValue} detectionChance{detectionChance} IsDiscovered {isDiscovered}");
                return isDiscovered;
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

            if (pawn.Faction != null && pawn.Faction == Faction.OfPlayer)
            {
                return true;
            }

            if (discoveredData.ContainsKey(pawn))
            {
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
