using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace BecomingHuman
{
    public class BecomingHumanMod : Mod
    {
        public static BecomingHumanSettings settings;
        private Vector2 scrollPosition = Vector2.zero;
        private string searchText = "";
        private string multiplierBuffer;

        private List<XenotypeDef> _AllXenotypeDefs;
        private List<XenotypeDef> AllXenotypeDefs
        {
            get
            {
                if (_AllXenotypeDefs == null)
                {
                    _AllXenotypeDefs = DefDatabase<XenotypeDef>.AllDefs
                        .OrderBy(x => x.label)
                        .ToList();
                }
                return _AllXenotypeDefs;
            }
        }

        public BecomingHumanMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<BecomingHumanSettings>();
            multiplierBuffer = settings.arrestToDetectionMultiplier.ToString("F2");

            LongEventHandler.QueueLongEvent(() => {
                SyncSettingsWithGameComponent();
            }, "BecomingHuman_InitializeSettings", false, null);
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

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Rect titleRect = inRect.TopPartPixels(30f);
            Text.Font = GameFont.Medium;
            Widgets.Label(titleRect, "Becoming Human Settings");
            Text.Font = GameFont.Small;

            inRect.yMin += 40f;

            Rect multiplierRect = new Rect(inRect.x, inRect.y, inRect.width, 28f);
            Rect multiplierLabelRect = multiplierRect.LeftPartPixels(300f);
            Widgets.Label(multiplierLabelRect, "Arrest to Detection Multiplier:");

            Rect multiplierFieldRect = new Rect(multiplierLabelRect.xMax + 10f, multiplierRect.y, 80f, multiplierRect.height);
            Widgets.TextFieldNumeric(multiplierFieldRect, ref settings.arrestToDetectionMultiplier, ref multiplierBuffer, 0.1f, 10f);

            inRect.yMin += 40f;

            Rect xenoHeaderRect = new Rect(inRect.x, inRect.y, inRect.width, 28f);
            Text.Font = GameFont.Medium;
            Widgets.Label(xenoHeaderRect, "Xenotype Whitelist");
            Text.Font = GameFont.Small;

            inRect.yMin += 35f;

            Rect searchRect = new Rect(inRect.x, inRect.y, inRect.width, 28f);
            Widgets.Label(searchRect.LeftPartPixels(80f), "Search:");
            searchText = Widgets.TextField(searchRect.RightPartPixels(inRect.width - 85f), searchText);

            inRect.yMin += 35f;

            Rect buttonsRect = new Rect(inRect.x, inRect.y, inRect.width, 28f);
            if (Widgets.ButtonText(buttonsRect.LeftHalf(), "Select All"))
            {
                settings.whitelistedPawnKindDefNames.Clear();
                foreach (XenotypeDef def in AllXenotypeDefs)
                {
                    settings.whitelistedPawnKindDefNames.Add(def.defName);
                }
            }
            if (Widgets.ButtonText(buttonsRect.RightHalf(), "Select None"))
            {
                settings.whitelistedPawnKindDefNames.Clear();
            }

            inRect.yMin += 35f;

            Rect outRect = inRect;

            var filteredDefs = AllXenotypeDefs;
            if (!string.IsNullOrEmpty(searchText))
            {
                string search = searchText.ToLower();
                filteredDefs = AllXenotypeDefs
                    .Where(def => def.label.ToLower().Contains(search) || def.defName.ToLower().Contains(search))
                    .ToList();
            }

            float viewHeight = filteredDefs.Count * 30f;
            Rect viewRect = new Rect(0f, 0f, outRect.width - 20f, viewHeight);

            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);

            float curY = 0f;

            foreach (XenotypeDef def in filteredDefs)
            {
                Rect rowRect = new Rect(0f, curY, viewRect.width, 30f);
                if (curY % 60f < 30f)
                {
                    Widgets.DrawLightHighlight(rowRect);
                }

                bool isWhitelisted = settings.whitelistedPawnKindDefNames.Contains(def.defName);

                Rect iconRect = new Rect(5f, curY, 30f, 30f);
                Rect checkboxRect = new Rect(40f, curY, viewRect.width - 45f, 30f);

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

                curY += 30f;
            }

            Widgets.EndScrollView();
        }

        public override string SettingsCategory()
        {
            return "Becoming Human";
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
            SyncSettingsWithGameComponent();
        }
    }
}
