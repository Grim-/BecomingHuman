using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace BecomingHuman
{
    public class BecomingHumanMod : Mod
    {
        public static BecomingHumanSettings settings;
        private Vector2 scrollPosition = Vector2.zero;
        private string searchText = "";
        private string arrestMultiplierBuffer;
        private string resistanceThresholdBuffer;

        private List<XenotypeDef> _allXenotypeDefs;
        private List<XenotypeDef> AllXenotypeDefs
        {
            get
            {
                if (_allXenotypeDefs == null)
                {
                    _allXenotypeDefs = DefDatabase<XenotypeDef>.AllDefs
                        .OrderBy(x => x.label)
                        .ToList();
                }
                return _allXenotypeDefs;
            }
        }

        public BecomingHumanMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<BecomingHumanSettings>();
            arrestMultiplierBuffer = settings.arrestToDetectionMultiplier.ToString("F2");
            resistanceThresholdBuffer = settings.resistanceDetectionThreshold.ToString("F2");

            LongEventHandler.QueueLongEvent(SyncSettingsWithGameComponent, "BecomingHuman_InitializeSettings", false, null);
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            DrawTitle(ref inRect);
            DrawArrestMultiplier(ref inRect);
            DrawResistanceThreshold(ref inRect);
            DrawXenotypeHeader(ref inRect);
            DrawSearchBox(ref inRect);
            DrawSelectionButtons(ref inRect);
            DrawXenotypeList(inRect);
        }

        public override string SettingsCategory() => "BecomingHuman.ModSettingsLabel".Translate();

        public override void WriteSettings()
        {
            base.WriteSettings();
            SyncSettingsWithGameComponent();
        }

        private void SyncSettingsWithGameComponent()
        {
            if (Current.Game != null)
            {
                XenotypeDiscoveryTracker tracker = Current.Game.GetComponent<XenotypeDiscoveryTracker>();
                if (tracker != null)
                {
                    tracker.whiteListedXenotypes.Clear();
                    foreach (XenotypeDef def in settings.WhitelistedXenotypeDefs)
                    {
                        tracker.AddToWhiteList(def);
                    }
                }
            }
        }

        private void DrawTitle(ref Rect inRect)
        {
            //Rect titleRect = inRect.TopPartPixels(30f);
            //Text.Font = GameFont.Medium;
            //Widgets.Label(titleRect, "BecomingHuman.SettingsTitle".Translate());
            //Text.Font = GameFont.Small;
            //inRect.yMin += 40f;
        }

        private void DrawArrestMultiplier(ref Rect inRect)
        {
            Rect multiplierRect = new Rect(inRect.x, inRect.y, inRect.width, 28f);
            Rect labelRect = multiplierRect.LeftPartPixels(300f);
            Widgets.Label(labelRect, "BecomingHuman.ArrestMultiplierLabel".Translate());

            Rect fieldRect = new Rect(labelRect.xMax + 10f, multiplierRect.y, 80f, multiplierRect.height);
            Widgets.TextFieldNumeric(fieldRect, ref settings.arrestToDetectionMultiplier, ref arrestMultiplierBuffer, 0.1f, 10f);
            TooltipHandler.TipRegion(new Rect(inRect.x, inRect.y, inRect.width, 28f),
                "BecomingHuman.ArrestMultiplierTooltip".Translate());

            inRect.yMin += 40f;
        }

        private void DrawResistanceThreshold(ref Rect inRect)
        {
            Rect thresholdRect = new Rect(inRect.x, inRect.y, inRect.width, 28f);
            Rect labelRect = thresholdRect.LeftPartPixels(300f);
            Widgets.Label(labelRect, "BecomingHuman.ResistanceThresholdLabel".Translate());

            Rect fieldRect = new Rect(labelRect.xMax + 10f, thresholdRect.y, 80f, thresholdRect.height);
            Widgets.TextFieldNumeric(fieldRect, ref settings.resistanceDetectionThreshold, ref resistanceThresholdBuffer, 0, 10f);
            TooltipHandler.TipRegion(new Rect(inRect.x, inRect.y, inRect.width, 28f),
                "BecomingHuman.ResistanceThresholdTooltip".Translate());

            inRect.yMin += 40f;
        }

        private void DrawXenotypeHeader(ref Rect inRect)
        {
            Rect headerRect = new Rect(inRect.x, inRect.y, inRect.width, 28f);
            Text.Font = GameFont.Medium;
            Widgets.Label(headerRect, "BecomingHuman.XenotypeWhitelistLabel".Translate());
            TooltipHandler.TipRegion(headerRect,
                "BecomingHuman.XenotypeWhitelistTooltip".Translate());
            Text.Font = GameFont.Small;
            inRect.yMin += 35f;
        }

        private void DrawSearchBox(ref Rect inRect)
        {
            Rect searchRect = new Rect(inRect.x, inRect.y, inRect.width, 28f);
            Widgets.Label(searchRect.LeftPartPixels(80f), "BecomingHuman.SearchLabel".Translate());
            searchText = Widgets.TextField(searchRect.RightPartPixels(inRect.width - 85f), searchText);
            TooltipHandler.TipRegion(searchRect, "BecomingHuman.SearchTooltip".Translate());
            inRect.yMin += 35f;
        }

        private void DrawSelectionButtons(ref Rect inRect)
        {
            Rect buttonsRect = new Rect(inRect.x, inRect.y, inRect.width, 28f);
            Rect selectAllRect = buttonsRect.LeftHalf();
            Rect selectNoneRect = buttonsRect.RightHalf();

            if (Widgets.ButtonText(selectAllRect, "BecomingHuman.SelectAllLabel".Translate()))
            {
                SelectAllXenotypes();
            }
            TooltipHandler.TipRegion(selectAllRect, "BecomingHuman.SelectAllTooltip".Translate());

            if (Widgets.ButtonText(selectNoneRect, "BecomingHuman.SelectNoneLabel".Translate()))
            {
                settings.whitelistedPawnKindDefNames.Clear();
            }
            TooltipHandler.TipRegion(selectNoneRect, "BecomingHuman.SelectNoneTooltip".Translate());

            inRect.yMin += 35f;
        }

        private void SelectAllXenotypes()
        {
            settings.whitelistedPawnKindDefNames.Clear();
            foreach (XenotypeDef def in AllXenotypeDefs)
            {
                settings.whitelistedPawnKindDefNames.Add(def.defName);
            }
        }

        private void DrawXenotypeList(Rect inRect)
        {
            List<XenotypeDef> filteredDefs = FilterXenotypes();
            float viewHeight = filteredDefs.Count * 30f;
            Rect viewRect = new Rect(0f, 0f, inRect.width - 20f, viewHeight);

            Widgets.BeginScrollView(inRect, ref scrollPosition, viewRect);
            DrawXenotypeRows(filteredDefs, viewRect);
            Widgets.EndScrollView();
        }

        private List<XenotypeDef> FilterXenotypes()
        {
            if (string.IsNullOrEmpty(searchText))
            {
                return AllXenotypeDefs;
            }

            string search = searchText.ToLower();
            return AllXenotypeDefs
                .Where(def => def.label.ToLower().Contains(search) || def.defName.ToLower().Contains(search))
                .ToList();
        }

        private void DrawXenotypeRows(List<XenotypeDef> xenotypes, Rect viewRect)
        {
            float curY = 0f;

            foreach (XenotypeDef def in xenotypes)
            {
                Rect rowRect = new Rect(0f, curY, viewRect.width, 30f);
                if (curY % 60f < 30f)
                {
                    Widgets.DrawLightHighlight(rowRect);
                }

                bool isWhitelisted = settings.whitelistedPawnKindDefNames.Contains(def.defName);
                DrawXenotypeRow(def, curY, viewRect.width, isWhitelisted);
                curY += 30f;
            }
        }

        private void DrawXenotypeRow(XenotypeDef def, float y, float width, bool isWhitelisted)
        {
            Rect rowRect = new Rect(0f, y, width, 30f);
            Rect iconRect = new Rect(5f, y, 30f, 30f);
            Rect checkboxRect = new Rect(40f, y, width - 45f, 30f);

            if (def.Icon != null)
            {
                GUI.DrawTexture(iconRect, def.Icon);
            }

            bool newChecked = isWhitelisted;
            Widgets.CheckboxLabeled(checkboxRect, $"{def.label} ({def.defName})", ref newChecked);

            if (newChecked != isWhitelisted)
            {
                if (newChecked)
                {
                    settings.whitelistedPawnKindDefNames.Add(def.defName);
                }
                else
                {
                    settings.whitelistedPawnKindDefNames.Remove(def.defName);
                }
            }

            // Generate tooltip text based on xenotype description if available
            string tooltipText = def.description;
            if (string.IsNullOrEmpty(tooltipText))
            {
                tooltipText = $"Toggle detection for {def.label} xenotype";
            }

            TooltipHandler.TipRegion(rowRect, tooltipText);
        }
    }
}