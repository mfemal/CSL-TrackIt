using System;
using System.Collections.Generic;
using System.Linq;
using ColossalFramework.UI;
using UnityEngine;
using TrackIt.API;

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

        // Resource categories to include in the radial chart (filtered to exclude )
        private static readonly IList<ResourceCategoryType> s_resourceCategories;

        static UIUtils()
        {
            // extract these once, they will never change
            s_resourceCategories = Enum.GetValues(typeof(ResourceCategoryType))
                .Cast<ResourceCategoryType>()
                .Except(new List<ResourceCategoryType>() { ResourceCategoryType.None })
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
            cargoChart.name = ConstructModUIComponentName(name);
            cargoChart.spriteName = "PieChartBg";
            cargoChart.size = new Vector2(90, 90);
            return cargoChart;
        }

        public static UILabel CreateLabel(UIComponent parent, string name, string text)
        {
            UILabel label = parent.AddUIComponent<UILabel>();
            label.name = ConstructModUIComponentName(name);
            label.text = text;

            return label;
        }

        public static UIPanel CreatePanel(UIComponent parent, string name)
        {
            UIPanel panel = parent.AddUIComponent<UIPanel>();
            panel.name = ConstructModUIComponentName(name);

            return panel;
        }

        /// <summary>
        /// Find a UIPanel component based on its name. The name can be determined by ILSpy or using mod tools. This method
        /// is useful to perform lookups on named objects without traversing the UI tree of objects directly.
        /// </summary>
        /// <typeparam name="T">The type of the panel to find (class found in a code inspector)</typeparam>
        /// <param name="name">The provided name of the panel.</param>
        /// <returns>The UI panel found or null (if not found or not initialized).</returns>
        public static UIPanel GetGameUIPanel<T>(string name)
        {
            if (name == null)
            {
                return null;
            }
            GameObject o = GameObject.Find(name);
            if (o == null)
            {
                return null;
            }
            T component = o.GetComponent<T>();
            if (component == null || !(component is UIPanel))
            {
                return null;
            }
            return component as UIPanel;
        }

        /// <summary>
        /// Construct a component name created by this mod by attempting to make it semi-unique and searchable by prepending
        /// the namespace prefix. This method can be safely called multiple times with 's'.
        /// </summary>
        /// <param name="s">Starting string (if null, null is returned) to prefix.</param>
        /// <returns>Null if s is null, otherwise a prefixed string using the namespace and the provided s.</returns>
        public static string ConstructModUIComponentName(string s)
        {
            if (s == null)
            {
                return null;
            }
            if (s.StartsWith(ModInfo.NamespacePrefix))
            {
                return s;
            }
            return ModInfo.NamespacePrefix + s;
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
    }
}
