using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;

namespace BecomingHuman
{
    [StaticConstructorOnStartup]
    public static class BecomeHumanPatch
    {
        static BecomeHumanPatch()
        {
            var harmony = new Harmony("com.emo.becomehuman");
            harmony.PatchAll();
        }

        [HarmonyPatch(typeof(Pawn))]
        [HarmonyPatch("GetFloatMenuOptions")]
        public static class Pawn_GetFloatMenuOptions_Patch
        {
            [HarmonyPostfix]
            public static IEnumerable<FloatMenuOption> Postfix(IEnumerable<FloatMenuOption> __result, Pawn __instance, Pawn selPawn)
            {
                Pawn detectorPawn = selPawn;
                Pawn targetPawn = __instance;


                foreach (FloatMenuOption option in __result)
                {
                    yield return option;
                }

                XenotypeDiscoveryTracker tracker = Current.Game.GetComponent<XenotypeDiscoveryTracker>();
                if (tracker != null && tracker.HasResearch && targetPawn.genes?.Xenotype != null && !tracker.IsXenotypeDiscovered(targetPawn))
                {
                    if (!detectorPawn.Downed && !detectorPawn.InMentalState)
                    {
                        if (XenotypeDiscoveryUtility.CanAttemptXenotypeDiscovery(detectorPawn, targetPawn))
                        {
                            yield return new FloatMenuOption("BecomingHuman.FloatMenu".Translate(Mathf.Clamp(detectorPawn.GetDetectionChance() * 100f, 0, 100f)), delegate ()
                            {
                                Job job = JobMaker.MakeJob(BecomeHumanDefOf.DXD_DetectXenotype, targetPawn);
                                selPawn.jobs.TryTakeOrderedJob(job);
                            }, MenuOptionPriority.Default, null, null, 0f, null, null);
                        }
                        else
                        {
                            yield return new FloatMenuOption("BecomingHuman.CantStartDetection".Translate(), null, MenuOptionPriority.Default, null, null, 0f, null, null);
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(SkillUI))]
        [HarmonyPatch(typeof(SkillUI))]
        [HarmonyPatch("GetSkillDescription")]
        public static class SkillUI_GetSkillDescription_Patch
        {
            [HarmonyPrefix]
            public static bool Prefix(SkillRecord sk, ref string __result)
            {
                __result = GetModifiedSkillDescription(sk);
                return false;
            }


            private static string GetModifiedSkillDescription(SkillRecord sk)
            {
                bool xenotypeDiscovered = sk.Pawn == null ||
                    Current.Game.GetComponent<XenotypeDiscoveryTracker>().IsXenotypeDiscovered(sk.Pawn);

                StringBuilder stringBuilder = new StringBuilder();
                TaggedString taggedString = sk.def.LabelCap.AsTipTitle();
                if (sk.TotallyDisabled)
                {
                    taggedString += " (" + "DisabledLower".Translate() + ")";
                }
                stringBuilder.AppendLineTagged(taggedString);
                stringBuilder.AppendLineTagged(sk.def.description.Colorize(ColoredText.SubtleGrayColor)).AppendLine();
                stringBuilder.AppendLineTagged(string.Concat(new object[]
                {
                    ("SkillLevel".Translate().CapitalizeFirst() + ": ").AsTipTitle(),
                    sk.GetLevelForUI(true),
                    " - ",
                    sk.LevelDescriptor
                }));
                if (!sk.PermanentlyDisabled && sk.Aptitude != 0)
                {
                    stringBuilder.AppendLine("  - " + "LearnedLevel".Translate() + ": " + sk.GetLevel(false));
                    if (ModsConfig.BiotechActive && sk.Pawn.genes != null)
                    {
                        foreach (Gene gene in sk.Pawn.genes.GenesListForReading)
                        {
                            if (gene.Active)
                            {
                                int num = gene.def.AptitudeFor(sk.def);
                                if (num != 0)
                                {
                                    if (xenotypeDiscovered)
                                    {
                                        // Original code
                                        stringBuilder.AppendLine(string.Format("  - {0}: {1}",
                                            "GeneLabelWithDesc".Translate(gene.def.Named("GENE")).CapitalizeFirst(),
                                            num.ToStringWithSign()));
                                    }
                                    else
                                    {
                                        // Hidden version
                                        stringBuilder.AppendLine(string.Format("  - Hidden Gene: {0}",
                                            num.ToStringWithSign()));
                                    }
                                }
                            }
                        }
                    }
                    if (ModsConfig.AnomalyActive)
                    {
                        Pawn_StoryTracker story = sk.Pawn.story;
                        if (((story != null) ? story.traits : null) != null)
                        {
                            foreach (Trait trait in sk.Pawn.story.traits.allTraits)
                            {
                                if (!trait.Suppressed)
                                {
                                    int num2 = trait.CurrentData.AptitudeFor(sk.def);
                                    if (num2 != 0)
                                    {
                                        if (xenotypeDiscovered)
                                        {
                                            // Original code
                                            stringBuilder.AppendLine(string.Format("  - {0}: {1}",
                                                "TraitLabelWithDesc".Translate(trait.CurrentData.GetLabelFor(sk.Pawn).Named("TRAITLABEL")).CapitalizeFirst(),
                                                num2.ToStringWithSign()));
                                        }
                                        else
                                        {
                                            // Hidden version
                                            stringBuilder.AppendLine(string.Format("  - Hidden Trait: {0}",
                                                num2.ToStringWithSign()));
                                        }
                                    }
                                }
                            }
                        }
                        if (sk.Pawn.health.hediffSet != null)
                        {
                            foreach (Hediff hediff in sk.Pawn.health.hediffSet.hediffs)
                            {
                                int num3 = hediff.def.AptitudeFor(sk.def);
                                if (num3 != 0)
                                {
                                    if (xenotypeDiscovered)
                                    {
                                        // Original code
                                        stringBuilder.AppendLine("  - " + hediff.LabelCap + ": " + num3.ToStringWithSign());
                                    }
                                    else
                                    {
                                        // Hidden version
                                        stringBuilder.AppendLine("  - Hidden Hediff: " + num3.ToStringWithSign());
                                    }
                                }
                            }
                        }
                    }
                    int unclampedLevel = sk.GetUnclampedLevel();
                    if (unclampedLevel < 0)
                    {
                        stringBuilder.AppendLine("  - " + "NegativeLevelAdjustedToZero".Translate());
                    }
                    else if (unclampedLevel > 20)
                    {
                        stringBuilder.AppendLine("  - " + "LevelCappedAtMax".Translate(20));
                    }
                }
                if (Current.ProgramState == ProgramState.Playing)
                {
                    stringBuilder.AppendLine();
                    string str = (sk.GetLevel(false) == 20) ? "Experience".Translate() : "ProgressToNextLevel".Translate();
                    if (sk.PermanentlyDisabled)
                    {
                        stringBuilder.AppendLineTagged((str + ": ").AsTipTitle() + "0 / " + SkillRecord.XpRequiredToLevelUpFrom(0));
                    }
                    else
                    {
                        stringBuilder.AppendLineTagged(string.Concat(new object[]
                        {
                            (str + ": ").AsTipTitle(),
                            sk.xpSinceLastLevel.ToString("F0"),
                            " / ",
                            sk.XpRequiredForLevelUp
                        }));
                    }
                }
                if (!sk.TotallyDisabled)
                {
                    if (sk.LearningSaturatedToday)
                    {
                        stringBuilder.AppendLine();
                        stringBuilder.AppendLine("LearnedMaxToday".Translate(sk.xpSinceMidnight.ToString("F0"), 4000, 0.2f.ToStringPercent("F0")));
                    }
                    float statValue = sk.Pawn.GetStatValue(StatDefOf.GlobalLearningFactor, true, -1);
                    float num4 = 1f;
                    float num5 = statValue * sk.passion.GetLearningFactor();
                    if (sk.def == SkillDefOf.Animals)
                    {
                        num4 = sk.Pawn.GetStatValue(StatDefOf.AnimalsLearningFactor, true, -1);
                        num5 *= num4;
                    }
                    stringBuilder.AppendLine();
                    stringBuilder.AppendLineTagged(("LearningSpeed".Translate() + ": ").AsTipTitle() + num5.ToStringPercent());
                    stringBuilder.AppendLine("  - " + "StatsReport_BaseValue".Translate() + ": " + 1f.ToStringPercent());
                    stringBuilder.AppendLine("  - " + sk.passion.GetLabel() + ": x" + sk.passion.GetLearningFactor().ToStringPercent("F0"));
                    if (statValue != 1f)
                    {
                        stringBuilder.AppendLine("  - " + StatDefOf.GlobalLearningFactor.LabelCap + ": x" + statValue.ToStringPercent());
                    }
                    if (Math.Abs(num4 - 1f) > 1E-45f)
                    {
                        stringBuilder.AppendLine("  - " + StatDefOf.AnimalsLearningFactor.LabelCap + ": x" + num4.ToStringPercent());
                    }
                    StatRequest request = StatRequest.For(sk.Pawn);
                    StatDefOf.GlobalLearningFactor.Worker.GetBaseValueFor(request);
                    if (statValue != 1f)
                    {
                        float baseValueFor = StatDefOf.GlobalLearningFactor.Worker.GetBaseValueFor(StatRequest.For(sk.Pawn));
                        stringBuilder.AppendLine();
                        stringBuilder.AppendLineTagged((StatDefOf.GlobalLearningFactor.LabelCap + ": ").AsTipTitle() + statValue.ToStringPercent());
                        stringBuilder.AppendLine("  - " + "StatsReport_BaseValue".Translate() + ": " + baseValueFor.ToStringPercent());
                        ListGlobalLearningSpeedOffsets(sk, stringBuilder, xenotypeDiscovered);
                    }
                    if (ModsConfig.BiotechActive && sk.Pawn.DevelopmentalStage.Child())
                    {
                        stringBuilder.AppendLine();
                        stringBuilder.AppendLineTagged("ChildrenLearn".Translate().Colorize(ColoredText.SubtleGrayColor));
                    }
                }
                return stringBuilder.ToString().TrimEndNewlines();
            }

            private static void ListGlobalLearningSpeedOffsets(SkillRecord sk, StringBuilder sb, bool xenotypeDiscovered)
            {
                foreach (Hediff hediff in sk.Pawn.health.hediffSet.hediffs)
                {
                    if (hediff.Visible)
                    {
                        HediffStage curStage = hediff.CurStage;
                        if (curStage != null)
                        {
                            float statOffsetFromList = curStage.statOffsets.GetStatOffsetFromList(StatDefOf.GlobalLearningFactor);
                            if (statOffsetFromList != 0f)
                            {
                                if (xenotypeDiscovered)
                                {
                                    // Original code
                                    sb.AppendLine(string.Concat(new string[]
                                    {
                                        "  - ",
                                        hediff.LabelCap,
                                        ": ",
                                        (statOffsetFromList >= 0f) ? "+" : "",
                                        statOffsetFromList.ToStringPercent()
                                    }));
                                }
                                else
                                {
                                    // Hidden version
                                    sb.AppendLine(string.Concat(new string[]
                                    {
                                        "  - ",
                                        "????",
                                        ": ",
                                        (statOffsetFromList >= 0f) ? "+" : "",
                                        statOffsetFromList.ToStringPercent()
                                    }));
                                }
                            }
                        }
                    }
                }
                Pawn_StoryTracker story = sk.Pawn.story;
                if (((story != null) ? story.traits : null) != null)
                {
                    foreach (Trait trait in sk.Pawn.story.traits.allTraits)
                    {
                        if (!trait.Suppressed)
                        {
                            float statOffsetFromList2 = trait.CurrentData.statOffsets.GetStatOffsetFromList(StatDefOf.GlobalLearningFactor);
                            if (statOffsetFromList2 != 0f)
                            {
                                if (xenotypeDiscovered)
                                {
                                    // Original code
                                    sb.AppendLine(string.Concat(new string[]
                                    {
                                        "  - ",
                                        trait.CurrentData.LabelCap,
                                        ": ",
                                        (statOffsetFromList2 >= 0f) ? "+" : "",
                                        statOffsetFromList2.ToStringPercent()
                                    }));
                                }
                                else
                                {
                                    // Hidden version
                                    sb.AppendLine(string.Concat(new string[]
                                    {
                                        "  - ",
                                        "????",
                                        ": ",
                                        (statOffsetFromList2 >= 0f) ? "+" : "",
                                        statOffsetFromList2.ToStringPercent()
                                    }));
                                }
                            }
                        }
                    }
                }
                if (ModsConfig.BiotechActive && sk.Pawn.genes != null)
                {
                    foreach (Gene gene in sk.Pawn.genes.GenesListForReading)
                    {
                        if (gene.Active)
                        {
                            float statOffsetFromList3 = gene.def.statOffsets.GetStatOffsetFromList(StatDefOf.GlobalLearningFactor);
                            if (statOffsetFromList3 != 0f)
                            {
                                if (xenotypeDiscovered)
                                {
                                    // Original code
                                    sb.AppendLine("  - " + gene.def.LabelCap + ": " + ((statOffsetFromList3 >= 0f) ? "+" : "") + statOffsetFromList3.ToStringPercent());
                                }
                                else
                                {
                                    // Hidden version
                                    sb.AppendLine("  - ???? Gene: " + ((statOffsetFromList3 >= 0f) ? "+" : "") + statOffsetFromList3.ToStringPercent());
                                }
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Pawn_GeneTracker), "get_XenotypeLabelCap")]
        public static class Patch_XenotypeLabelCap
        {
            public static bool Prefix(Pawn_GeneTracker __instance, ref string __result)
            {
                if (__instance.pawn != null)
                {
                    var tracker = Current.Game?.GetComponent<XenotypeDiscoveryTracker>();


                    if (tracker != null)
                    {
                        if (tracker.IsXenotypeDiscovered(__instance.pawn))
                        {
                            return true;
                        }
                        else
                        {
                            __result = "BecomingHuman.UndiscoveredLabel".Translate();
                            return false;
                        }

                    }
                }
                return true;
            }
        }


        [HarmonyPatch(typeof(Pawn_GeneTracker), "get_XenotypeIcon")]
        public static class Patch_XenotypeIcon
        {
            public static bool Prefix(Pawn_GeneTracker __instance, ref Texture2D __result)
            {
                if (__instance.pawn != null)
                {
                    var tracker = Current.Game?.GetComponent<XenotypeDiscoveryTracker>();
                    if (tracker != null)
                    {
                        if (tracker.IsXenotypeDiscovered(__instance.pawn))
                        {
                            return true;
                        }
                        else
                        {
                            __result = BaseContent.ClearTex;
                            return false;
                        }

                    }
                }
                return true;
            }
        }


        [HarmonyPatch(typeof(Pawn_GeneTracker), "get_XenotypeDescShort")]
        public static class Patch_XenotypeDescShort
        {
            public static bool Prefix(Pawn_GeneTracker __instance, ref string __result)
            {
                if (__instance.pawn != null)
                {
                    var tracker = Current.Game?.GetComponent<XenotypeDiscoveryTracker>();
                    if (tracker != null && !tracker.IsXenotypeDiscovered(__instance.pawn))
                    {
                        __result = "BecomingHuman.XenotypeDescriptionShort".Translate();
                        return false;
                    }
                }
                return true;
            }
        }
    }

    [StaticConstructorOnStartup]
    public static class SocialCardUtility_HiddenOpinions
    {
        private static MethodInfo drawMyOpinionMethod;
        private static MethodInfo drawHisOpinionMethod;

        static SocialCardUtility_HiddenOpinions()
        {
            drawMyOpinionMethod = AccessTools.Method(typeof(SocialCardUtility), "DrawMyOpinion");
            drawHisOpinionMethod = AccessTools.Method(typeof(SocialCardUtility), "DrawHisOpinion");

            Harmony harmony = new Harmony("emo.becominghuman.socialcard");

            harmony.Patch(
                drawMyOpinionMethod,
                prefix: new HarmonyMethod(typeof(SocialCardUtility_HiddenOpinions), nameof(DrawMyOpinion_Prefix))
            );

            harmony.Patch(
                drawHisOpinionMethod,
                prefix: new HarmonyMethod(typeof(SocialCardUtility_HiddenOpinions), nameof(DrawHisOpinion_Prefix))
            );
        }


        public static bool DrawMyOpinion_Prefix(object entry, Rect rect, Pawn selPawnForSocialInfo)
        {
            try
            {
                // Get the fields via reflection
                Type entryType = entry.GetType();
                Pawn otherPawn = (Pawn)entryType.GetField("otherPawn").GetValue(entry);
                int opinionOfOtherPawn = (int)entryType.GetField("opinionOfOtherPawn").GetValue(entry);

                if (!otherPawn.RaceProps.Humanlike || !selPawnForSocialInfo.RaceProps.Humanlike)
                {
                    return false;
                }

      
                bool discovered = Current.Game.GetComponent<XenotypeDiscoveryTracker>().IsXenotypeDiscovered(otherPawn);


                if (discovered)
                {
                    if (Mathf.Abs(opinionOfOtherPawn) < 10)
                        GUI.color = Color.gray;
                    else if (opinionOfOtherPawn < 0)
                        GUI.color = ColorLibrary.RedReadable;
                    else
                        GUI.color = Color.green;

                    Widgets.Label(rect, opinionOfOtherPawn.ToStringWithSign());
                }
                else
                {
                    GUI.color = Color.gray;
                    Widgets.Label(rect, "???");
                }

                return false;
            }
            catch (Exception ex)
            {
                Log.Error("[BecomingHuman] Error in DrawMyOpinion_Prefix: " + ex.ToString());
                return true; 
            }
        }

        public static bool DrawHisOpinion_Prefix(object entry, Rect rect, Pawn selPawnForSocialInfo)
        {
            try
            {
                Type entryType = entry.GetType();
                Pawn otherPawn = (Pawn)entryType.GetField("otherPawn").GetValue(entry);
                int opinionOfMe = (int)entryType.GetField("opinionOfMe").GetValue(entry);

                if (!otherPawn.RaceProps.Humanlike || !selPawnForSocialInfo.RaceProps.Humanlike)
                {
                    return false;
                }

                bool discovered = Current.Game.GetComponent<XenotypeDiscoveryTracker>().IsXenotypeDiscovered(otherPawn);

                Color color;
                if (discovered)
                {
                    if (Mathf.Abs(opinionOfMe) < 10)
                        color = Color.gray;
                    else if (opinionOfMe < 0)
                        color = ColorLibrary.RedReadable;
                    else
                        color = Color.green;
                }
                else
                {
                    color = Color.gray;
                }

                GUI.color = new Color(color.r, color.g, color.b, 0.4f);

                if (discovered)
                {
                    Widgets.Label(rect, "(" + opinionOfMe.ToStringWithSign() + ")");
                }
                else
                {
                    Widgets.Label(rect, "(???)");
                }

                return false;
            }
            catch (Exception ex)
            {
                Log.Error("[BecomingHuman] Error in DrawHisOpinion_Prefix: " + ex.ToString());
                return true; 
            }
        }
    }
}