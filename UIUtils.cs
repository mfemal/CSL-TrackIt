using System;
using System.Collections.Generic;
using System.Linq;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using UnityEngine;
using TrackIt.API;
using static TrackIt.VehicleWorldInfoPanelMonitor;

namespace TrackIt
{
    // Some Create methods are re-used or modified from the Building Themes mod (boformer); although some credits were
    // also noted in that mod to SamSamTS and AcidFire.
    internal class UIUtils
    {
        /// <summary>
        /// Provides a convenience member to filter on appropriate cargo resource categories used throughout the UI.
        /// </summary>
        /// <returns>An unordered list of basic categories suitable for grouping (i.e. Ore, Agriculture, etc.), values in this list are immutable.</returns>
        public static IList<ResourceCategoryType> CargoBasicResourceGroups
        {
            get
            {
                return s_resourceCategories;
            }
        }

        // Resource categories to include in the radial chart (filtered to exclude None and unlicensed DLC)
        private static readonly IList<ResourceCategoryType> s_resourceCategories;

        static UIUtils()
        {
            // extract these once, they will never change
            s_resourceCategories = Enum.GetValues(typeof(ResourceCategoryType))
                .Cast<ResourceCategoryType>()
                .Where(t => IsResourceCategoryAvailable(t) && t != ResourceCategoryType.None)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// As the game UI is refreshed or changed, this method faciliates extracting style info for this mod.
        /// </summary>
        /// <param name="source">Source label to copy style (text, scale, and font) from (if null, dest is ignored).</param>
        /// <param name="dest">Destination label to copy style to (if null, method is a no-op).</param>
        public static void CopyTextStyleAttributes(UILabel source, UILabel dest)
        {
            if (source == null || dest == null)
            {
                return;
            }
            dest.textScale = source.textScale;
            dest.font = source.font;
            dest.color = source.color;
            dest.textColor = source.textColor;
        }

        public static UIButton CreateButton(UIComponent parent)
        {
            UIButton button = parent.AddUIComponent<UIButton>();

            button.size = new Vector2(90f, 30f);
            button.textScale = 0.9f;
            button.normalBgSprite = "ButtonMenu";
            button.hoveredBgSprite = "ButtonMenuHovered";
            button.pressedBgSprite = "ButtonMenuPressed";
            button.disabledBgSprite = "ButtonMenuDisabled";
            button.disabledTextColor = new Color32(128, 128, 128, 255);
            button.canFocus = false;

            return button;
        }

        /// <summary>
        /// Create a resource radial chart whose slices are depicted in the same manner as Outside Connections.
        /// </summary>
        /// <param name="parent">Parent component name to attach to.</param>
        /// <param name="name">Name of the UI component, useful for browsing the scene in the explorer.</param>
        /// <returns></returns>
        public static CargoUIChart CreateCargoGroupedResourceChart(UIComponent parent, string name)
        {
            CargoUIChart cargoChart = parent.AddUIComponent<CargoUIChart>();
            cargoChart.name = ConstructComponentName(name);
            cargoChart.spriteName = "PieChartBg";
            cargoChart.size = new Vector2(90, 90);
            return cargoChart;
        }

        /// <summary>
        /// Create a label field from the provided parameters.
        /// </summary>
        /// <param name="parent">Parent component the label field is added to.</param>
        /// <param name="name">Name of the label field to create (final name is constructed with a mod prefix).</param>
        /// <param name="s">The localization key or text (key is preferred, lookup is done first using CollossalFramework.Globalization).</param>
        /// <returns>The new label field constructed.</returns>
        public static UILabel CreateLabel(UIComponent parent, string name, string s)
        {
            UILabel label = parent.AddUIComponent<UILabel>();
            label.name = ConstructComponentName(name);

            if (!string.IsNullOrEmpty(s))
            {
                if (Locale.Exists(s))
                {
                    label.isLocalized = true;
                    label.localeID = s;
                }
                else
                {
                    string t = Localization.Get(s);
                    if (!string.IsNullOrEmpty(t))
                    {
                        label.text = t;
                    }
                    else
                    {
                        label.text = s;
                    }
                }
            }

            return label;
        }

        public static UIPanel CreatePanel(UIComponent parent, string name)
        {
            UIPanel panel = parent.AddUIComponent<UIPanel>();
            panel.name = ConstructComponentName(name);
            return panel;
        }

        public static UIProgressBar CreateCargoProgressBar(UIComponent parent, string name)
        {
            UIProgressBar progressBar = parent.AddUIComponent<UIProgressBar>();
            progressBar.name = ConstructComponentName(name);

            // Hard to see resource colors using GenericProgressBar and GenericProgressBarFill, so use:
            progressBar.backgroundSprite = "LevelBarBackground";
            progressBar.progressSprite = "LevelBarForeground";
            progressBar.minValue = 0;   // scale as a percent value
            progressBar.maxValue = 100;

            return progressBar;
        }

        public static UISprite CreateSprite(UIComponent parent, string name, string spriteName)
        {
            UISprite sprite = parent.AddUIComponent<UISprite>();
            sprite.name = ConstructComponentName(name);
            sprite.spriteName = spriteName;

            return sprite;
        }

        /// <summary>
        /// Create and add a new panel to a world info panel.
        /// </summary>
        /// <param name="parent">Parent world info panel</param>
        /// <param name="name">name of the new panel to add.</param>
        /// <param name="backgroundSprite">The background sprite (i.e. MenuPanel2, GenericTab, SubcategoriesPanel, etc.)</param>
        /// <returns>The newly created panel.</returns>
        public static UIPanel CreateWorldInfoCompanionPanel(UIComponent parent, string name, string backgroundSprite)
        {
            UIPanel panel = CreatePanel(parent, name);
            panel.backgroundSprite = backgroundSprite;
            panel.opacity = 0.90f;
            return panel;
        }

        /// <summary>
        /// Avoid showing a 0 integer, yet reflect data in the UI, if the total is "small" (perserve unit of measure kilo)
        /// </summary>
        /// <param name="v">Source value (in raw units) for formatting.</param>
        /// <returns>Formatted string suitable for display.</returns>
        public static string FormatCargoValue(long v)
        {
            return v > 0 && v < 1000 ?
                string.Format("{0:0.000#}{1}", v / 1000.0f, Localization.Get("KILO_UNITS")) :
                string.Format("{0:0}{1}", Mathf.Ceil(v / 1000.0f), Localization.Get("KILO_UNITS"));
        }

        /// <summary>
        /// Lookup a font based on its name.
        /// </summary>
        /// <param name="name">Name of the font (i.e. names used within UIDynamicFont)</param>
        /// <remarks>Credit to keallu and Show It!</remarks>
        /// <returns>Font reference if found (or null).</returns>
        public static UIFont GetUIFont(string name)
        {
            UIFont[] fonts = Resources.FindObjectsOfTypeAll<UIFont>();
            foreach (UIFont font in fonts)
            {
                if (font.name.CompareTo(name) == 0)
                {
                    return font;
                }
            }

            return null;
        }

        /// <summary>
        /// Construct a component name created by this mod by attempting to make it semi-unique and searchable by prepending
        /// the namespace prefix. This method can be safely called multiple times with 's'.
        /// </summary>
        /// <param name="s">Starting string (if null, null is returned) to prefix.</param>
        /// <returns>Null if s is null, otherwise a prefixed string using the namespace and the provided s.</returns>
        public static string ConstructComponentName(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return null;
            }
            if (s.StartsWith(ModInfo.NamespacePrefix))
            {
                return s;
            }
            return ModInfo.NamespacePrefix + s;
        }

        public static string GetLocaleID(ResourceCategoryType resourceCategoryType)
        {
            string key = null;
            switch (resourceCategoryType)
            {
                case ResourceCategoryType.Agriculture:
                    key = "INFO_CONNECTIONS_AGRICULTURE";
                    break;
                case ResourceCategoryType.Fish:
                    key = "INFO_CONNECTIONS_FISH";
                    break;
                case ResourceCategoryType.Forestry:
                    key = "INFO_CONNECTIONS_FORESTRY";
                    break;
                case ResourceCategoryType.Goods:
                    key = "INFO_CONNECTIONS_GOODS";
                    break;
                case ResourceCategoryType.Mail:
                    key = "INFO_CONNECTIONS_MAIL";
                    break;
                case ResourceCategoryType.Oil:
                    key = "INFO_CONNECTIONS_OIL";
                    break;
                case ResourceCategoryType.Ore:
                    key = "INFO_CONNECTIONS_ORE";
                    break;
            }
            return key;
        }

        /// <summary>
        /// Get the resource color for the resource tracked category type. These are the same colors used for import and export.
        /// Original concept for this in EOCV from rcav8tr (Extended Outside Connections View) although this is adapted appropriately
        /// for usage in this mod.
        /// </summary>
        public static Color GetResourceCategoryColor(ResourceCategoryType type)
        {
            int transferReason; // translate resource type to transfer reason
            switch (type)
            {
                // these are the reasons used in OutsideConnectionsInfoViewPanel in SetupImportLegend and SetupExportLegend
                case ResourceCategoryType.Goods:
                    transferReason = (int)TransferManager.TransferReason.Goods;
                    break;
                case ResourceCategoryType.Forestry:
                    transferReason = (int)TransferManager.TransferReason.Logs;
                    break;
                case ResourceCategoryType.Agriculture:
                    transferReason = (int)TransferManager.TransferReason.Grain;
                    break;
                case ResourceCategoryType.Ore:
                    transferReason = (int)TransferManager.TransferReason.Ore;
                    break;
                case ResourceCategoryType.Oil:
                    transferReason = (int)TransferManager.TransferReason.Oil;
                    break;
                case ResourceCategoryType.Mail:
                    transferReason = (int)TransferManager.TransferReason.Mail;
                    break;
                case ResourceCategoryType.Fish:
                    transferReason = (int)TransferManager.TransferReason.Fish;
                    break;
                default:
                    LogUtil.LogError($"Unable to translate resource category type [{type}] to resource color.");
                    return Color.black;
            }

            // do not get colors from color sprites because they might not be initialized yet
            return TransferManager.instance.m_properties.m_resourceColors[transferReason];
        }

        /// <summary>
        /// Checks if the given resource category type is available based on whether it has been purchased.
        /// </summary>
        /// <param name="resourceCategoryType">Resource category to check ('None' is not checked).</param>
        /// <returns>True if a resource category can be tracked or used on the UI.</returns>
        public static bool IsResourceCategoryAvailable(ResourceCategoryType resourceCategoryType)
        {
            if ((!SteamHelper.IsDLCOwned(SteamHelper.DLC.IndustryDLC) &&
                    resourceCategoryType == ResourceCategoryType.Mail) ||
                (!SteamHelper.IsDLCOwned(SteamHelper.DLC.UrbanDLC) &&
                    resourceCategoryType == ResourceCategoryType.Fish)) // Sunset Harbor
            {
                return false;
            }
            return true;
        }
    }
}
