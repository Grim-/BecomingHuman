using RimWorld;
using System.Collections.Generic;
using Verse;

namespace BecomingHuman
{
    public class BecomingHumanSettings : ModSettings
    {
        public List<string> whitelistedPawnKindDefNames = new List<string>();
        public float arrestToDetectionMultiplier = 1.5f;

        public List<XenotypeDef> WhitelistedXenotypeDefs
        {
            get
            {
                List<XenotypeDef> result = new List<XenotypeDef>();
                foreach (string defName in whitelistedPawnKindDefNames)
                {
                    XenotypeDef def = DefDatabase<XenotypeDef>.GetNamedSilentFail(defName);
                    if (def != null)
                    {
                        result.Add(def);
                    }
                }
                return result;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref whitelistedPawnKindDefNames, "whitelistedPawnKindDefNames", LookMode.Value);
            Scribe_Values.Look(ref arrestToDetectionMultiplier, "arrestToDetectionMultiplier", 1.5f);
            if (whitelistedPawnKindDefNames == null)
            {
                whitelistedPawnKindDefNames = new List<string>();
            }
        }
    }
}
